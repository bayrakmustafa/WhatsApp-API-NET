using Strilanc.Value;
using System;

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

namespace Tr.Com.Eimza.LibAxolotl.Ratchet
{
    public class AliceAxolotlParameters
    {
        private readonly IdentityKeyPair ourIdentityKey;
        private readonly ECKeyPair ourBaseKey;

        private readonly IdentityKey theirIdentityKey;
        private readonly ECPublicKey theirSignedPreKey;
        private readonly May<ECPublicKey> theirOneTimePreKey;
        private readonly ECPublicKey theirRatchetKey;

        private AliceAxolotlParameters(IdentityKeyPair ourIdentityKey, ECKeyPair ourBaseKey,
                                       IdentityKey theirIdentityKey, ECPublicKey theirSignedPreKey,
                                       ECPublicKey theirRatchetKey, May<ECPublicKey> theirOneTimePreKey)
        {
            this.ourIdentityKey = ourIdentityKey;
            this.ourBaseKey = ourBaseKey;
            this.theirIdentityKey = theirIdentityKey;
            this.theirSignedPreKey = theirSignedPreKey;
            this.theirRatchetKey = theirRatchetKey;
            this.theirOneTimePreKey = theirOneTimePreKey;

            if (ourIdentityKey == null || ourBaseKey == null || theirIdentityKey == null ||
                theirSignedPreKey == null || theirRatchetKey == null || theirOneTimePreKey == null)
            {
                throw new Exception("Null values!");
            }
        }

        public IdentityKeyPair GetOurIdentityKey()
        {
            return ourIdentityKey;
        }

        public ECKeyPair GetOurBaseKey()
        {
            return ourBaseKey;
        }

        public IdentityKey GetTheirIdentityKey()
        {
            return theirIdentityKey;
        }

        public ECPublicKey GetTheirSignedPreKey()
        {
            return theirSignedPreKey;
        }

        public May<ECPublicKey> GetTheirOneTimePreKey()
        {
            return theirOneTimePreKey;
        }

        public static Builder NewBuilder()
        {
            return new Builder();
        }

        public ECPublicKey GetTheirRatchetKey()
        {
            return theirRatchetKey;
        }

        public class Builder
        {
            private IdentityKeyPair ourIdentityKey;
            private ECKeyPair ourBaseKey;

            private IdentityKey theirIdentityKey;
            private ECPublicKey theirSignedPreKey;
            private ECPublicKey theirRatchetKey;
            private May<ECPublicKey> theirOneTimePreKey;

            public Builder SetOurIdentityKey(IdentityKeyPair ourIdentityKey)
            {
                this.ourIdentityKey = ourIdentityKey;
                return this;
            }

            public Builder SetOurBaseKey(ECKeyPair ourBaseKey)
            {
                this.ourBaseKey = ourBaseKey;
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

            public Builder SetTheirSignedPreKey(ECPublicKey theirSignedPreKey)
            {
                this.theirSignedPreKey = theirSignedPreKey;
                return this;
            }

            public Builder SetTheirOneTimePreKey(May<ECPublicKey> theirOneTimePreKey)
            {
                this.theirOneTimePreKey = theirOneTimePreKey;
                return this;
            }

            public AliceAxolotlParameters Create()
            {
                return new AliceAxolotlParameters(ourIdentityKey, ourBaseKey, theirIdentityKey,
                                                  theirSignedPreKey, theirRatchetKey, theirOneTimePreKey);
            }
        }
    }
}