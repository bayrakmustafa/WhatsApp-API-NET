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
using static Tr.Com.Eimza.LibAxolotl.State.StorageProtos;

namespace Tr.Com.Eimza.LibAxolotl
{
    /**
     * Holder for public and private identity key pair.
     *
     * @author
     */

    public class IdentityKeyPair
    {
        private readonly IdentityKey publicKey;
        private readonly ECPrivateKey privateKey;

        public IdentityKeyPair(IdentityKey publicKey, ECPrivateKey privateKey)
        {
            this.publicKey = publicKey;
            this.privateKey = privateKey;
        }

        public IdentityKeyPair(byte[] serialized)
        {
            try
            {
                IdentityKeyPairStructure structure = IdentityKeyPairStructure.ParseFrom(serialized);
                this.publicKey = new IdentityKey(structure.PublicKey.ToByteArray(), 0);
                this.privateKey = Curve.DecodePrivatePoint(structure.PrivateKey.ToByteArray());
            }
            catch (InvalidProtocolBufferException e)
            {
                throw new InvalidKeyException(e);
            }
        }

        public IdentityKey GetPublicKey()
        {
            return publicKey;
        }

        public ECPrivateKey GetPrivateKey()
        {
            return privateKey;
        }

        public byte[] Serialize()
        {
            return IdentityKeyPairStructure.CreateBuilder()
                                           .SetPublicKey(ByteString.CopyFrom(publicKey.Serialize()))
                                           .SetPrivateKey(ByteString.CopyFrom(privateKey.Serialize()))
                                           .Build().ToByteArray();
        }
    }
}