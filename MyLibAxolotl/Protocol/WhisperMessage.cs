/** 
 * Copyright (C) 2015 smndtrl
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Google.ProtocolBuffers;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl.Protocol
{
    public partial class WhisperMessage : CiphertextMessage
    {

        private static readonly int MAC_LENGTH = 8;

        private readonly uint messageVersion;
        private readonly ECPublicKey senderRatchetKey;
        private readonly uint counter;
        private readonly uint previousCounter;
        private readonly byte[] ciphertext;
        private readonly byte[] serialized;

        public WhisperMessage(byte[] serialized)
        {
            try
            {
                byte[][] messageParts = ByteUtil.Split(serialized, 1, serialized.Length - 1 - MAC_LENGTH, MAC_LENGTH);
                byte version = messageParts[0][0];
                byte[] message = messageParts[1];
                byte[] mac = messageParts[2];

                if (ByteUtil.HighBitsToInt(version) <= CiphertextMessage.UNSUPPORTED_VERSION)
                {
                    throw new LegacyMessageException("Legacy message: " + ByteUtil.HighBitsToInt(version));
                }

                if (ByteUtil.HighBitsToInt(version) > CURRENT_VERSION)
                {
                    throw new InvalidMessageException("Unknown version: " + ByteUtil.HighBitsToInt(version));
                }

                WhisperProtos.WhisperMessage whisperMessage = WhisperProtos.WhisperMessage.ParseFrom(message);

                if (!whisperMessage.HasCiphertext ||
                    !whisperMessage.HasCounter ||
                    !whisperMessage.HasRatchetKey)
                {
                    throw new InvalidMessageException("Incomplete message.");
                }

                this.serialized = serialized;
                this.senderRatchetKey = Curve.DecodePoint(whisperMessage.RatchetKey.ToByteArray(), 0);
                this.messageVersion = (uint)ByteUtil.HighBitsToInt(version);
                this.counter = whisperMessage.Counter;
                this.previousCounter = whisperMessage.PreviousCounter;
                this.ciphertext = whisperMessage.Ciphertext.ToByteArray();
            }
            catch (/*InvalidProtocolBufferException | InvalidKeyException | Parse*/Exception e)
            {
                throw new InvalidMessageException(e);
            }
        }

        public WhisperMessage(uint messageVersion, byte[] macKey, ECPublicKey senderRatchetKey,
                              uint counter, uint previousCounter, byte[] ciphertext,
                              IdentityKey senderIdentityKey,
                              IdentityKey receiverIdentityKey)
        {
            byte[] version = { ByteUtil.IntsToByteHighAndLow((int)messageVersion, (int)CURRENT_VERSION) };
            byte[] message = WhisperProtos.WhisperMessage.CreateBuilder()
                                           .SetRatchetKey(ByteString.CopyFrom(senderRatchetKey.Serialize()))
                                           .SetCounter(counter)
                                           .SetPreviousCounter(previousCounter)
                                           .SetCiphertext(ByteString.CopyFrom(ciphertext))
                                           .Build().ToByteArray();

            byte[] mac = GetMac(messageVersion, senderIdentityKey, receiverIdentityKey, macKey,
                                    ByteUtil.Combine(version, message));

            this.serialized = ByteUtil.Combine(version, message, mac);
            this.senderRatchetKey = senderRatchetKey;
            this.counter = counter;
            this.previousCounter = previousCounter;
            this.ciphertext = ciphertext;
            this.messageVersion = messageVersion;
        }

        public ECPublicKey GetSenderRatchetKey()
        {
            return senderRatchetKey;
        }

        public uint GetMessageVersion()
        {
            return messageVersion;
        }

        public uint GetCounter()
        {
            return counter;
        }

        public byte[] GetBody()
        {
            return ciphertext;
        }

        public void VerifyMac(uint messageVersion, IdentityKey senderIdentityKey,
                        IdentityKey receiverIdentityKey, byte[] macKey)
        {
            byte[][] parts = ByteUtil.Split(serialized, serialized.Length - MAC_LENGTH, MAC_LENGTH);
            byte[] ourMac = GetMac(messageVersion, senderIdentityKey, receiverIdentityKey, macKey, parts[0]);
            byte[] theirMac = parts[1];

            if (!Enumerable.SequenceEqual(ourMac, theirMac))
            {
                throw new InvalidMessageException("Bad Mac!");
            }
        }

        private byte[] GetMac(uint messageVersion,
                        IdentityKey senderIdentityKey,
                        IdentityKey receiverIdentityKey,
                        byte[] macKey, byte[] serialized)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                if (messageVersion >= 3)
                {
                    byte[] sik = senderIdentityKey.GetPublicKey().Serialize();
                    stream.Write(sik, 0, sik.Length);
                    byte[] rik = receiverIdentityKey.GetPublicKey().Serialize();
                    stream.Write(rik, 0, rik.Length);
                }

                stream.Write(serialized, 0, serialized.Length);
                byte[] fullMac = Sign.Sha256sum(macKey, stream.ToArray());
                return ByteUtil.Trim(fullMac, MAC_LENGTH);
            }
            catch (/*NoSuchAlgorithmException | java.security.InvalidKey*/Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public override byte[] Serialize()
        {
            return serialized;
        }

        public override uint GetKeyType()
        {
            return CiphertextMessage.WHISPER_TYPE;
        }

        public static bool IsLegacy(byte[] message)
        {
            return message != null && message.Length >= 1 &&
                ByteUtil.HighBitsToInt(message[0]) <= CiphertextMessage.UNSUPPORTED_VERSION;
        }

    }
}
