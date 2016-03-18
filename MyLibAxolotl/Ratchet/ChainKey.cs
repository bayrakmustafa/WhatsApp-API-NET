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

using Tr.Com.Eimza.LibAxolotl.Kdf;
using Tr.Com.Eimza.LibAxolotl.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl.Ratchet
{
    public class ChainKey
    {

        private static readonly byte[] MESSAGE_KEY_SEED = { 0x01 };
        private static readonly byte[] CHAIN_KEY_SEED = { 0x02 };

        private readonly HKDF kdf;
        private readonly byte[] key;
        private readonly uint index;

        public ChainKey(HKDF kdf, byte[] key, uint index)
        {
            this.kdf = kdf;
            this.key = key;
            this.index = index;
        }

        public byte[] GetKey()
        {
            return key;
        }

        public uint GetIndex()
        {
            return index;
        }

        public ChainKey GetNextChainKey()
        {
            byte[] nextKey = GetBaseMaterial(CHAIN_KEY_SEED);
            return new ChainKey(kdf, nextKey, index + 1);
        }

        public MessageKeys GetMessageKeys()
        {
            byte[] inputKeyMaterial = GetBaseMaterial(MESSAGE_KEY_SEED);
            byte[] keyMaterialBytes = kdf.DeriveSecrets(inputKeyMaterial, Encoding.UTF8.GetBytes("WhisperMessageKeys"), DerivedMessageSecrets.SIZE);
            DerivedMessageSecrets keyMaterial = new DerivedMessageSecrets(keyMaterialBytes);

            return new MessageKeys(keyMaterial.GetCipherKey(), keyMaterial.GetMacKey(), keyMaterial.GetIv(), index);
        }

        private byte[] GetBaseMaterial(byte[] seed)
        {
            try
            {
                return Sign.Sha256sum(key, seed);
            }
            catch (InvalidKeyException e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
