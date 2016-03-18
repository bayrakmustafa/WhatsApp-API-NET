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

using Tr.Com.Eimza.LibAxolotl.Ecc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl.Ratchet
{
    public class SymmetricAxolotlParameters
    {

        private readonly ECKeyPair ourBaseKey;
        private readonly ECKeyPair ourRatchetKey;
        private readonly IdentityKeyPair ourIdentityKey;

        private readonly ECPublicKey theirBaseKey;
        private readonly ECPublicKey theirRatchetKey;
        private readonly IdentityKey theirIdentityKey;

        SymmetricAxolotlParameters(ECKeyPair ourBaseKey, ECKeyPair ourRatchetKey,
                                   IdentityKeyPair ourIdentityKey, ECPublicKey theirBaseKey,
                                   ECPublicKey theirRatchetKey, IdentityKey theirIdentityKey)
        {
            this.ourBaseKey = ourBaseKey;
            this.ourRatchetKey = ourRatchetKey;
            this.ourIdentityKey = ourIdentityKey;
            this.theirBaseKey = theirBaseKey;
            this.theirRatchetKey = theirRatchetKey;
            this.theirIdentityKey = theirIdentityKey;

            if (ourBaseKey == null || ourRatchetKey == null || ourIdentityKey == null ||
                theirBaseKey == null || theirRatchetKey == null || theirIdentityKey == null)
            {
                throw new Exception("Null values!");
            }
        }

        public ECKeyPair GetOurBaseKey()
        {
            return ourBaseKey;
        }

        public ECKeyPair GetOurRatchetKey()
        {
            return ourRatchetKey;
        }

        public IdentityKeyPair GetOurIdentityKey()
        {
            return ourIdentityKey;
        }

        public ECPublicKey GetTheirBaseKey()
        {
            return theirBaseKey;
        }

        public ECPublicKey GetTheirRatchetKey()
        {
            return theirRatchetKey;
        }

        public IdentityKey GetTheirIdentityKey()
        {
            return theirIdentityKey;
        }

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        public class Builder
        {
            private ECKeyPair ourBaseKey;
            private ECKeyPair ourRatchetKey;
            private IdentityKeyPair ourIdentityKey;

            private ECPublicKey theirBaseKey;
            private ECPublicKey theirRatchetKey;
            private IdentityKey theirIdentityKey;

            public Builder SetOurBaseKey(ECKeyPair ourBaseKey)
            {
                this.ourBaseKey = ourBaseKey;
                return this;
            }

            public Builder SetOurRatchetKey(ECKeyPair ourRatchetKey)
            {
                this.ourRatchetKey = ourRatchetKey;
                return this;
            }

            public Builder SetOurIdentityKey(IdentityKeyPair ourIdentityKey)
            {
                this.ourIdentityKey = ourIdentityKey;
                return this;
            }

            public Builder SetTheirBaseKey(ECPublicKey theirBaseKey)
            {
                this.theirBaseKey = theirBaseKey;
                return this;
            }

            public Builder SetTheirRatchetKey(ECPublicKey theirRatchetKey)
            {
                this.theirRatchetKey = theirRatchetKey;
                return this;
            }

            public Builder SetTheirIdentityKey(IdentityKey theirIdentityKey)
            {
                this.theirIdentityKey = theirIdentityKey;
                return this;
            }

            public SymmetricAxolotlParameters Create()
            {
                return new SymmetricAxolotlParameters(ourBaseKey, ourRatchetKey, ourIdentityKey,
                                                      theirBaseKey, theirRatchetKey, theirIdentityKey);
            }
        }
    }
}
