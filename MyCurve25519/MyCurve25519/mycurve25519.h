// mycurve25519.h
#pragma once

#define CURVE25519_SHARED_KEY_LEN 32
#define CURVE25519_PRIV_KEY_LEN 32
#define CURVE25519_PUB_KEY_LEN 32
#define CURVE25519_SIG_LEN 64
#define CURVE25519_PRIV_KEY_LEN_ERR_MSG "CURVE25519_PRIV_KEY_LEN_ERR_MSG"
#define CURVE25519_PUB_KEY_LEN_ERR_MSG "CURVE25519_PUB_KEY_LEN_ERR_MSG"
#define CURVE25519_SIG_FAILED_MSG "CURVE25519_SIG_FAILED_MSG"

using namespace System;

namespace MyCurve25519
{
	public ref class Curve25519Native
	{
	public:
		Curve25519Native();

		//This class implements the interface for Curve25519Provider from curve25519-java's code.
		//Note: In C# code, "Platform::Array<byte>^" should be treated/cast as signed bytes (sbyte) to match with Java's byte type.
		bool IsNative();
		array<Byte>^ CalculateAgreement(array<Byte>^ privateKey, array<Byte>^ publicKey);
		array<Byte>^ GeneratePublicKey(array<Byte>^ privateKey);

		array<Byte>^ GeneratePrivateKey();

		array<Byte>^ GeneratePrivateKey(array<Byte>^ random);
		array<Byte>^ CalculateSignature(array<Byte>^ random, array<Byte>^ privateKey, array<Byte>^ message);
		bool VerifySignature(array<Byte>^ publicKey, array<Byte>^ message, array<Byte>^ signature);

		//Having some problems passing in a SecureRandomProvider. For now, I'm just going to use the built-in, unaudited
		//Windows.Security.Cryptography.CryptographicBuffer class. Later, we'll solve this with reflection and dynamic
		//loading of a C# assembly which provides the random data through some open source means.
		//void setRandomProvider(SecureRandomProvider provider);
	private:
	};
}
