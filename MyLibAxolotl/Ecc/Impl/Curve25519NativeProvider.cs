/** 
 * Copyright (C) 2015 langboost
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



namespace Tr.Com.Eimza.LibAxolotl.Ecc.Impl
{
	class Curve25519NativeProvider : ICurve25519Provider
	{
		private mycurve25519.Curve25519Native native = new mycurve25519.Curve25519Native();

		public byte[] CalculateAgreement(byte[] ourPrivate, byte[] theirPublic)
		{
			return native.CalculateAgreement(ourPrivate, theirPublic);
		}

		public byte[] CalculateSignature(byte[] random, byte[] privateKey, byte[] message)
		{
			return native.CalculateSignature(random, privateKey, message);
		}

		public byte[] GeneratePrivateKey(byte[] random)
		{
			return native.GeneratePrivateKey(random);
		}

		public byte[] GeneratePublicKey(byte[] privateKey)
		{
			return native.GeneratePublicKey(privateKey);
		}

		public bool IsNative()
		{
			return native.IsNative();
		}

		public bool VerifySignature(byte[] publicKey, byte[] message, byte[] signature)
		{
			return native.VerifySignature(publicKey, message, signature);
		}
	}
}
