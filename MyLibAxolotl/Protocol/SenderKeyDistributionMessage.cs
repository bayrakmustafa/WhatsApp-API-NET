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
    public partial class SenderKeyDistributionMessage : CiphertextMessage
    {
        private readonly uint id;
        private readonly uint iteration;
        private readonly byte[] chainKey;
        private readonly ECPublicKey signatureKey;
        private readonly byte[] serialized;

        public SenderKeyDistributionMessage(uint id, uint iteration, byte[] chainKey, ECPublicKey signatureKey)
        {
            byte[] version = { ByteUtil.IntsToByteHighAndLow((int)CURRENT_VERSION, (int)CURRENT_VERSION) };
            byte[] protobuf = WhisperProtos.SenderKeyDistributionMessage.CreateBuilder()
                                                                        .SetId(id)
                                                                        .SetIteration(iteration)
                                                                        .SetChainKey(ByteString.CopyFrom(chainKey))
                                                                        .SetSigningKey(ByteString.CopyFrom(signatureKey.Serialize()))
                                                                        .Build().ToByteArray();

            this.id = id;
            this.iteration = iteration;
            this.chainKey = chainKey;
            this.signatureKey = signatureKey;
            this.serialized = ByteUtil.Combine(version, protobuf);
        }

        public SenderKeyDistributionMessage(byte[] serialized)
        {
            try
            {
                byte[][] messageParts = ByteUtil.Split(serialized, 1, serialized.Length - 1);
                byte version = messageParts[0][0];
                byte[] message = messageParts[1];

                if (ByteUtil.HighBitsToInt(version) < CiphertextMessage.CURRENT_VERSION)
                {
                    throw new LegacyMessageException("Legacy message: " + ByteUtil.HighBitsToInt(version));
                }

                if (ByteUtil.HighBitsToInt(version) > CURRENT_VERSION)
                {
                    throw new InvalidMessageException("Unknown version: " + ByteUtil.HighBitsToInt(version));
                }

                WhisperProtos.SenderKeyDistributionMessage distributionMessage = WhisperProtos.SenderKeyDistributionMessage.ParseFrom(message);

                if (!distributionMessage.HasId ||
                    !distributionMessage.HasIteration ||
                    !distributionMessage.HasChainKey ||
                    !distributionMessage.HasSigningKey)
                {
                    throw new InvalidMessageException("Incomplete message.");
                }

                this.serialized = serialized;
                this.id = distributionMessage.Id;
                this.iteration = distributionMessage.Iteration;
                this.chainKey = distributionMessage.ChainKey.ToByteArray();
                this.signatureKey = Curve.DecodePoint(distributionMessage.SigningKey.ToByteArray(), 0);
            }
            catch (Exception e)
            {
                //InvalidProtocolBufferException | InvalidKey
                throw new InvalidMessageException(e);
            }
        }

        public override byte[] Serialize()
        {
            return serialized;
        }

        public override uint GetKeyType()
        {
            return SENDERKEY_DISTRIBUTION_TYPE;
        }

        public uint GetIteration()
        {
            return iteration;
        }

        public byte[] GetChainKey()
        {
            return chainKey;
        }

        public ECPublicKey GetSignatureKey()
        {
            return signatureKey;
        }

        public uint GetId()
        {
            return id;
        }
    }
}