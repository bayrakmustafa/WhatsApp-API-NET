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
using Tr.Com.Eimza.LibAxolotl.Kdf;
using Tr.Com.Eimza.LibAxolotl.State;
using Tr.Com.Eimza.LibAxolotl.Util;
using Strilanc.Value;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tr.Com.Eimza.LibAxolotl.Ratchet
{
    public class RatchetingSession
    {

        public static void InitializeSession(SessionState sessionState,
                                             uint sessionVersion,
                                             SymmetricAxolotlParameters parameters)
        {
            if (IsAlice(parameters.GetOurBaseKey().GetPublicKey(), parameters.GetTheirBaseKey()))
            {
                AliceAxolotlParameters.Builder aliceParameters = AliceAxolotlParameters.NewBuilder();

                aliceParameters.SetOurBaseKey(parameters.GetOurBaseKey())
                               .SetOurIdentityKey(parameters.GetOurIdentityKey())
                               .SetTheirRatchetKey(parameters.GetTheirRatchetKey())
                               .SetTheirIdentityKey(parameters.GetTheirIdentityKey())
                               .SetTheirSignedPreKey(parameters.GetTheirBaseKey())
                               .SetTheirOneTimePreKey(May<ECPublicKey>.NoValue);

                RatchetingSession.InitializeSession(sessionState, sessionVersion, aliceParameters.Create());
            }
            else
            {
                BobAxolotlParameters.Builder bobParameters = BobAxolotlParameters.NewBuilder();

                bobParameters.SetOurIdentityKey(parameters.GetOurIdentityKey())
                             .SetOurRatchetKey(parameters.GetOurRatchetKey())
                             .SetOurSignedPreKey(parameters.GetOurBaseKey())
                             .SetOurOneTimePreKey(May<ECKeyPair>.NoValue)
                             .SetTheirBaseKey(parameters.GetTheirBaseKey())
                             .SetTheirIdentityKey(parameters.GetTheirIdentityKey());

                RatchetingSession.InitializeSession(sessionState, sessionVersion, bobParameters.Create());
            }
        }

        public static void InitializeSession(SessionState sessionState,
                                             uint sessionVersion,
                                             AliceAxolotlParameters parameters)

        {
            try
            {
                sessionState.SetSessionVersion(sessionVersion);
                sessionState.SetRemoteIdentityKey(parameters.GetTheirIdentityKey());
                sessionState.SetLocalIdentityKey(parameters.GetOurIdentityKey().GetPublicKey());

                ECKeyPair sendingRatchetKey = Curve.GenerateKeyPair();
                MemoryStream secrets = new MemoryStream();

                if (sessionVersion >= 3)
                {
                    byte[] discontinuityBytes = GetDiscontinuityBytes();
                    secrets.Write(discontinuityBytes, 0, discontinuityBytes.Length);
                }

                byte[] agree1 = Curve.CalculateAgreement(parameters.GetTheirSignedPreKey(),
                                                       parameters.GetOurIdentityKey().GetPrivateKey());
                byte[] agree2 = Curve.CalculateAgreement(parameters.GetTheirIdentityKey().GetPublicKey(),
                                                        parameters.GetOurBaseKey().GetPrivateKey());
                byte[] agree3 = Curve.CalculateAgreement(parameters.GetTheirSignedPreKey(),
                                                       parameters.GetOurBaseKey().GetPrivateKey());

                secrets.Write(agree1, 0, agree1.Length);
                secrets.Write(agree2, 0, agree2.Length);
                secrets.Write(agree3, 0, agree3.Length);


                if (sessionVersion >= 3 && parameters.GetTheirOneTimePreKey().HasValue)
                {
                    byte[] agree4 = Curve.CalculateAgreement(parameters.GetTheirOneTimePreKey().ForceGetValue(),
                                                           parameters.GetOurBaseKey().GetPrivateKey());
                    secrets.Write(agree4, 0, agree4.Length);
                }

                DerivedKeys derivedKeys = CalculateDerivedKeys(sessionVersion, secrets.ToArray());
                Pair<RootKey, ChainKey> sendingChain = derivedKeys.GetRootKey().CreateChain(parameters.GetTheirRatchetKey(), sendingRatchetKey);

                sessionState.AddReceiverChain(parameters.GetTheirRatchetKey(), derivedKeys.GetChainKey());
                sessionState.SetSenderChain(sendingRatchetKey, sendingChain.Second());
                sessionState.SetRootKey(sendingChain.First());
            }
            catch (IOException e)
            {
                throw new Exception(e.Message);
            }
        }

        public static void InitializeSession(SessionState sessionState,
                                             uint sessionVersion,
                                             BobAxolotlParameters parameters)
        {

            try
            {
                sessionState.SetSessionVersion(sessionVersion);
                sessionState.SetRemoteIdentityKey(parameters.GetTheirIdentityKey());
                sessionState.SetLocalIdentityKey(parameters.GetOurIdentityKey().GetPublicKey());

                MemoryStream secrets = new MemoryStream();

                if (sessionVersion >= 3)
                {
                    byte[] discontinuityBytes = GetDiscontinuityBytes();
                    secrets.Write(discontinuityBytes, 0, discontinuityBytes.Length);
                }

                byte[] agree1 = Curve.CalculateAgreement(parameters.GetTheirIdentityKey().GetPublicKey(),
                                                       parameters.GetOurSignedPreKey().GetPrivateKey());
                byte[] agree2 = Curve.CalculateAgreement(parameters.GetTheirBaseKey(),
                                                       parameters.GetOurIdentityKey().GetPrivateKey());
                byte[] agree3 = Curve.CalculateAgreement(parameters.GetTheirBaseKey(),
                                                       parameters.GetOurSignedPreKey().GetPrivateKey());
                secrets.Write(agree1, 0, agree1.Length);
                secrets.Write(agree2, 0, agree2.Length);
                secrets.Write(agree3, 0, agree3.Length);

                if (sessionVersion >= 3 && parameters.GetOurOneTimePreKey().HasValue)
                {
                    byte[] agree4 = Curve.CalculateAgreement(parameters.GetTheirBaseKey(),
                                                           parameters.GetOurOneTimePreKey().ForceGetValue().GetPrivateKey());
                    secrets.Write(agree4, 0, agree4.Length);
                }

                DerivedKeys derivedKeys = CalculateDerivedKeys(sessionVersion, secrets.ToArray());

                sessionState.SetSenderChain(parameters.GetOurRatchetKey(), derivedKeys.GetChainKey());
                sessionState.SetRootKey(derivedKeys.GetRootKey());
            }
            catch (IOException e)
            {
                throw new Exception(e.Message);
            }
        }

        private static byte[] GetDiscontinuityBytes()
        {
            byte[] discontinuity = new byte[32];
            //Arrays.fill(discontinuity, (byte)0xFF);
            for (int i = 0; i < discontinuity.Length; i++)
            {
                discontinuity[i] = 0xFF;
            }
            return discontinuity;
        }

        private static DerivedKeys CalculateDerivedKeys(uint sessionVersion, byte[] masterSecret)
        {
            HKDF kdf = HKDF.CreateFor(sessionVersion);
            byte[] derivedSecretBytes = kdf.DeriveSecrets(masterSecret, Encoding.UTF8.GetBytes("WhisperText"), 64);
            byte[][] derivedSecrets = ByteUtil.Split(derivedSecretBytes, 32, 32);

            return new DerivedKeys(new RootKey(kdf, derivedSecrets[0]),
                                   new ChainKey(kdf, derivedSecrets[1], 0));
        }

        private static bool IsAlice(ECPublicKey ourKey, ECPublicKey theirKey)
        {
            return ourKey.CompareTo(theirKey) < 0;
        }

        public class DerivedKeys
        {
            private readonly RootKey rootKey;
            private readonly ChainKey chainKey;

            internal DerivedKeys(RootKey rootKey, ChainKey chainKey)
            {
                this.rootKey = rootKey;
                this.chainKey = chainKey;
            }

            public RootKey GetRootKey()
            {
                return rootKey;
            }

            public ChainKey GetChainKey()
            {
                return chainKey;
            }
        }
    }
}
