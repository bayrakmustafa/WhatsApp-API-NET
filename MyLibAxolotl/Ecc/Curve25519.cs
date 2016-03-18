/** 
 * Copyright (C) 2015 smndtrl, langboost
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

using System.Security.Cryptography;
using Tr.Com.Eimza.LibAxolotl.Ecc.Impl;

namespace Tr.Com.Eimza.LibAxolotl.Ecc
{
	/// <summary>
	/// Choose between various implementations of Curve25519 (native, managed, etc).
	/// </summary>
	public enum Curve25519ProviderType
	{
		/// <summary>
		/// Attempt to provide a native implementation. If one is not available, error out (TODO, could add managed fallback once implemented)
		/// </summary>
		BEST = 0x05,
		/// <summary>
		/// Explicitly use the native implementation
		/// </summary>
		NATIVE
	}

	class Curve25519
	{
		private static Curve25519 instance;
		private ICurve25519Provider provider;

		private Curve25519() { }

		/// <summary>
		/// Accesses the currently in use Curve25519 provider, according to the type requested.
		/// </summary>
		/// <param name="type">Type of provider requested.</param>
		/// <returns>Provider</returns>
		public static Curve25519 GetInstance(Curve25519ProviderType type)
		{
			if (instance == null)
			{
				instance = new Curve25519();
				instance.provider = (ICurve25519Provider)new Curve25519NativeProvider();
			}
			return instance;
		}

		/// <summary>
		/// <see cref="Curve25519" /> is backed by a WinRT implementation of curve25519. Returns true for native.
		/// </summary>
		/// <returns>True. Backed by a native provider.</returns>
		public bool IsNative()
		{
			return provider.IsNative();
		}

		/// <summary>
		/// Generates a Curve25519 keypair.
		/// </summary>
		/// <returns>A randomly generated Curve25519 keypair.</returns>
		public Curve25519KeyPair generateKeyPair()
		{
            RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            byte[] random = new byte[32];
            rng.GetBytes(random);

            byte[] privateKey = provider.GeneratePrivateKey(random);
			byte[] publicKey  = provider.GeneratePublicKey(privateKey);

			return new Curve25519KeyPair(publicKey, privateKey);
		}

		/// <summary>
		/// Calculates an ECDH agreement.
		/// </summary>
		/// <param name="publicKey">The Curve25519 (typically remote party's) public key.</param>
		/// <param name="privateKey">The Curve25519 (typically yours) private key.</param>
		/// <returns>A 32-byte shared secret.</returns>
		public byte[] CalculateAgreement(byte[] publicKey, byte[] privateKey)
		{
			return provider.CalculateAgreement(privateKey, publicKey);
		}

		/// <summary>
		/// Calculates a Curve25519 signature.
		/// </summary>
		/// <param name="privateKey">The private Curve25519 key to create the signature with.</param>
		/// <param name="message">The message to sign.</param>
		/// <returns>64 byte signature</returns>
		public byte[] CalculateSignature(byte[] privateKey, byte[] message)
		{
            RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            byte[] random = new byte[32];
            rng.GetBytes(random);
            return provider.CalculateSignature(random, privateKey, message);
		}

		/// <summary>
		/// Verify a Curve25519 signature.
		/// </summary>
		/// <param name="publicKey">The Curve25519 public key the signature belongs to.</param>
		/// <param name="message">The message that was signed.</param>
		/// <param name="signature">The signature to verify.</param>
		/// <returns>Boolean for if valid</returns>
		public bool VerifySignature(byte[] publicKey, byte[] message, byte[] signature)
		{
			return provider.VerifySignature(publicKey, message, signature);
		}
	}

	/// <summary>
	/// Curve25519 public and private key stored together.
	/// </summary>
	public class Curve25519KeyPair
	{

		private readonly byte[] publicKey;
		private readonly byte[] privateKey;

		/// <summary>
		/// Create a curve 25519 keypair from a public and private keys.
		/// </summary>
		/// <param name="publicKey">32 byte public key</param>
		/// <param name="privateKey">32 byte private key</param>
		public Curve25519KeyPair(byte[] publicKey, byte[] privateKey)
		{
			this.publicKey = publicKey;
			this.privateKey = privateKey;
		}

		/// <summary>
		/// Curve25519 public key
		/// </summary>
		/// <returns></returns>
		public byte[] getPublicKey()
		{
			return publicKey;
		}

		/// <summary>
		/// Curve25519 private key
		/// </summary>
		/// <returns></returns>
		public byte[] getPrivateKey()
		{
			return privateKey;
		}
	}
}
