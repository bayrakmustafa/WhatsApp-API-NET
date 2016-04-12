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
using System;
using Tr.Com.Eimza.LibAxolotl.Ecc;
using Tr.Com.Eimza.LibAxolotl.Util;

namespace Tr.Com.Eimza.LibAxolotl.Protocol
{
    public partial class SenderKeyGroupData : CipherTextMessage
    {
        private readonly byte[] message;
        private readonly SenderKeyGroupMessage senderKey;

        private readonly byte[] serialized;

        public SenderKeyGroupData(byte[] message, SenderKeyGroupMessage senderKey)
        {
            byte[] version = { ByteUtil.IntsToByteHighAndLow((int)CURRENT_VERSION, (int)CURRENT_VERSION) };

            WhisperProtos.SenderKeyGroupMessage wSenderKey = WhisperProtos.SenderKeyGroupMessage.CreateBuilder()
                .SetGroupId(ByteString.CopyFrom(senderKey.GetGroupId()))
                .SetSenderKey(ByteString.CopyFrom(senderKey.GetSenderKey()))
                .Build();

            byte[] protobuf = WhisperProtos.SenderKeyGroupData.CreateBuilder()
                                                                        .SetMessage(ByteString.CopyFrom(message))
                                                                        .SetSenderKey(wSenderKey)
                                                                        .Build().ToByteArray();

            this.message = message;
            this.senderKey = senderKey;
            this.serialized = ByteUtil.Combine(version, protobuf);
        }

        public SenderKeyGroupData(byte[] serialized)
        {
            try
            {
                byte[][] messageParts = ByteUtil.Split(serialized, 1, serialized.Length - 1);
                byte version = messageParts[0][0];
                byte[] message = messageParts[1];

                if (ByteUtil.HighBitsToInt(version) < CipherTextMessage.CURRENT_VERSION)
                {
                    throw new LegacyMessageException("Legacy message: " + ByteUtil.HighBitsToInt(version));
                }

                if (ByteUtil.HighBitsToInt(version) > CURRENT_VERSION)
                {
                    throw new InvalidMessageException("Unknown version: " + ByteUtil.HighBitsToInt(version));
                }

                WhisperProtos.SenderKeyGroupData senderKeyGroupMessage = WhisperProtos.SenderKeyGroupData.ParseFrom(message);

                if (!senderKeyGroupMessage.HasMessage ||
                    !senderKeyGroupMessage.HasSenderKey)
                {
                    throw new InvalidMessageException("Incomplete message.");
                }

                this.serialized = serialized;
                this.message = senderKeyGroupMessage.Message.ToByteArray();
                this.senderKey = new SenderKeyGroupMessage(senderKeyGroupMessage.SenderKey.ToByteArray());
            }
            catch (Exception e)
            {
                throw new InvalidMessageException(e);
            }
        }

        public override byte[] Serialize()
        {
            return serialized;
        }

        public override uint GetKeyType()
        {
            return SENDERKEY_GROUP_DATA_TYPE;
        }

        public byte[] GetMessage()
        {
            return message;
        }

        public SenderKeyGroupMessage GetSenderKey()
        {
            return senderKey;
        }
    }
}