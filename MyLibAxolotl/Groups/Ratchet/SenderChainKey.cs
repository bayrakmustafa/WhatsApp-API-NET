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

using Tr.Com.Eimza.LibAxolotl.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl.Groups.Ratchet
{
    /**
     * Each SenderKey is a "chain" of keys, each derived from the previous.
     *
     * At any given point in time, the state of a SenderKey can be represented
     * as the current chain key value, along with its iteration count.  From there,
     * subsequent iterations can be derived, as well as individual message keys from
     * each chain key.
     *
     * @author
    */
    public class SenderChainKey
    {

        private static readonly byte[] MESSAGE_KEY_SEED = { 0x01 };
        private static readonly byte[] CHAIN_KEY_SEED = { 0x02 };

        private readonly uint iteration;
        private readonly byte[] chainKey;

        public SenderChainKey(uint iteration, byte[] chainKey)
        {
            this.iteration = iteration;
            this.chainKey = chainKey;
        }

        public uint GetIteration()
        {
            return iteration;
        }

        public SenderMessageKey GetSenderMessageKey()
        {
            return new SenderMessageKey(iteration, GetDerivative(MESSAGE_KEY_SEED, chainKey));
        }

        public SenderChainKey GetNext()
        {
            return new SenderChainKey(iteration + 1, GetDerivative(CHAIN_KEY_SEED, chainKey));
        }

        public byte[] GetSeed()
        {
            return chainKey;
        }

        private byte[] GetDerivative(byte[] seed, byte[] key)
        {
            // try
            //{
            return Sign.Sha256sum(key, seed);
            /*}
            catch (NoSuchAlgorithmException | InvalidKeyException e) {
                throw new AssertionError(e);
            }*/
        }

    }
}
