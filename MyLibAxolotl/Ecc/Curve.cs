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

namespace Tr.Com.Eimza.LibAxolotl.Ecc
{
    public class Curve
    {
        public const int DJB_TYPE = 0x05;

        public static bool IsNative()
        {
            return Curve25519.GetInstance(Curve25519ProviderType.BEST).IsNative();
        }

        public static ECKeyPair GenerateKeyPair()
        {
            Curve25519KeyPair keyPair = Curve25519.GetInstance(Curve25519ProviderType.BEST).GenerateKeyPair();
            return new ECKeyPair(new DjbECPublicKey(keyPair.GetPublicKey()), new DjbECPrivateKey(keyPair.GetPrivateKey()));
        }

        public static ECPublicKey DecodePoint(byte[] bytes, int offset)
        {
            int type = bytes[offset] & 0xFF;

            switch (type)
            {
                case Curve.DJB_TYPE:
                    byte[] keyBytes = new byte[32];
                    System.Buffer.BlockCopy(bytes, offset + 1, keyBytes, 0, keyBytes.Length);
                    return new DjbECPublicKey(keyBytes);

                default:
                    throw new InvalidKeyException("Bad key type: " + type);
            }
        }

        public static ECPrivateKey DecodePrivatePoint(byte[] bytes)
        {
            return new DjbECPrivateKey(bytes);
        }

        public static byte[] CalculateAgreement(ECPublicKey publicKey, ECPrivateKey privateKey)
        {
            if (publicKey.GetKeyType() != privateKey.GetKeyType())
            {
                throw new InvalidKeyException("Public and private keys must be of the same type!");
            }

            if (publicKey.GetKeyType() == DJB_TYPE)
            {
                return Curve25519.GetInstance(Curve25519ProviderType.BEST)
                                 .CalculateAgreement(((DjbECPublicKey)publicKey).GetPublicKey(),
                                                     ((DjbECPrivateKey)privateKey).GetPrivateKey());
            }
            else
            {
                throw new InvalidKeyException("Unknown type: " + publicKey.GetKeyType());
            }
        }

        public static bool VerifySignature(ECPublicKey signingKey, byte[] message, byte[] signature)
        {
            if (signingKey.GetKeyType() == DJB_TYPE)
            {
                return Curve25519.GetInstance(Curve25519ProviderType.BEST).VerifySignature(((DjbECPublicKey)signingKey).GetPublicKey(), message, signature);
            }
            else
            {
                throw new InvalidKeyException("Unknown type: " + signingKey.GetKeyType());
            }
        }

        public static byte[] CalculateSignature(ECPrivateKey signingKey, byte[] message)
        {
            if (signingKey.GetKeyType() == DJB_TYPE)
            {
                return Curve25519.GetInstance(Curve25519ProviderType.BEST).CalculateSignature(((DjbECPrivateKey)signingKey).GetPrivateKey(), message);
            }
            else
            {
                throw new InvalidKeyException("Unknown type: " + signingKey.GetKeyType());
            }
        }
    }
}