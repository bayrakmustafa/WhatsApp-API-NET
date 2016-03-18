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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tr.Com.Eimza.LibAxolotl.State.StorageProtos;

namespace Tr.Com.Eimza.LibAxolotl.State
{
    public class PreKeyRecord
    {

        private PreKeyRecordStructure structure;

        public PreKeyRecord(uint id, ECKeyPair keyPair)
        {
            this.structure = PreKeyRecordStructure.CreateBuilder()
                                                  .SetId(id)
                                                  .SetPublicKey(ByteString.CopyFrom(keyPair.GetPublicKey()
                                                                                           .Serialize()))
                                                  .SetPrivateKey(ByteString.CopyFrom(keyPair.GetPrivateKey()
                                                                                            .Serialize()))
                                                  .Build();
        }

        public PreKeyRecord(byte[] serialized)
        {
            this.structure = PreKeyRecordStructure.ParseFrom(serialized);
        }



        public uint GetId()
        {
            return this.structure.Id;
        }

        public ECKeyPair GetKeyPair()
        {
            try
            {
                ECPublicKey publicKey = Curve.DecodePoint(this.structure.PublicKey.ToByteArray(), 0);
                ECPrivateKey privateKey = Curve.DecodePrivatePoint(this.structure.PrivateKey.ToByteArray());

                return new ECKeyPair(publicKey, privateKey);
            }
            catch (InvalidKeyException e)
            {
                throw new Exception(e.Message);
            }
        }

        public byte[] Serialize()
        {
            return this.structure.ToByteArray();
        }
    }
}
