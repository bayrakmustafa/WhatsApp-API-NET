#include "pch.h"
#include "Windows.h"
#include "mycurve25519.h"
#include "Curve25519_Internal.h" //Curve25519_Donna
#include "ed25519\additions\curve_sigs.h" //Curve25519_Sign

using namespace MyCurve25519;
using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Security::Cryptography;

#ifdef _WIN32
# define RtlGenRandom SystemFunction036
# if defined(__cplusplus)
extern "C"
# endif
BOOLEAN NTAPI RtlGenRandom(PVOID RandomBuffer, ULONG RandomBufferLength);
# pragma comment(lib, "advapi32.lib")
#endif

Curve25519Native::Curve25519Native()
{
}

bool MyCurve25519::Curve25519Native::IsNative()
{
	return true;
}

array<Byte>^ Curve25519Native::CalculateAgreement(array<Byte>^  privateKey, array<Byte>^  publicKey)
{
	if (privateKey->Length != (int)CURVE25519_PRIV_KEY_LEN)
	{
		throw gcnew Exception(gcnew System::String(CURVE25519_PRIV_KEY_LEN_ERR_MSG));
	}

	if (publicKey->Length != CURVE25519_PUB_KEY_LEN)
	{
		throw gcnew Exception(gcnew System::String(CURVE25519_PUB_KEY_LEN_ERR_MSG));
	}

	Byte* sharedKeyBytes = new Byte[CURVE25519_SHARED_KEY_LEN];
	ZeroMemory(sharedKeyBytes, (sizeof(Byte) * CURVE25519_SHARED_KEY_LEN));

	pin_ptr<byte> privateKeyBytes = &privateKey[0];
	pin_ptr<byte> publicKeyBytes = &publicKey[0];

	curve25519_donna(sharedKeyBytes, privateKeyBytes, publicKeyBytes);

	array<Byte>^ sharedKey = gcnew array<Byte>(CURVE25519_SHARED_KEY_LEN);
	Marshal::Copy((IntPtr)sharedKeyBytes, sharedKey, 0, CURVE25519_SHARED_KEY_LEN);
	delete sharedKeyBytes;

	return sharedKey;
}

array<Byte>^ MyCurve25519::Curve25519Native::GeneratePublicKey(array<Byte>^ privateKey)
{
	if (privateKey->Length != CURVE25519_PRIV_KEY_LEN)
	{
		throw gcnew Exception(gcnew System::String(CURVE25519_PRIV_KEY_LEN_ERR_MSG));
	}

	static const Byte basepoint[32] = { 9 };

	Byte* publicKeyBytes = new Byte[CURVE25519_PUB_KEY_LEN];
	ZeroMemory(publicKeyBytes, (sizeof(Byte) * CURVE25519_PUB_KEY_LEN));
	pin_ptr<byte> privateKeyBytes = &privateKey[0];

	curve25519_donna(publicKeyBytes, privateKeyBytes, basepoint);

	array<Byte>^ publicKey = gcnew array<Byte>(CURVE25519_PUB_KEY_LEN);
	Marshal::Copy((IntPtr)publicKeyBytes, publicKey, 0, CURVE25519_PUB_KEY_LEN);
	delete publicKeyBytes;
	return publicKey;
}

array<Byte>^ MyCurve25519::Curve25519Native::GeneratePrivateKey()
{
	Byte* buffer = new Byte[CURVE25519_PRIV_KEY_LEN];
	ZeroMemory(buffer, (sizeof(Byte) * CURVE25519_PRIV_KEY_LEN));

	RtlGenRandom(buffer, CURVE25519_PRIV_KEY_LEN);
	array<Byte>^ random = gcnew array<Byte>(CURVE25519_PRIV_KEY_LEN);
	Marshal::Copy((IntPtr)buffer, random, 0, CURVE25519_PRIV_KEY_LEN);
	delete buffer;

	return GeneratePrivateKey(random);
}

array<Byte>^ MyCurve25519::Curve25519Native::GeneratePrivateKey(array<Byte>^ random)
{
	/*if (random->Length != CURVE25519_PRIV_KEY_LEN)
	{
	throw ref new Exception(NTE_BAD_LEN, CURVE25519_PRIV_KEY_LEN_ERR_MSG);
	}*/

	array<Byte>^ privateKey = gcnew array<Byte>(CURVE25519_PRIV_KEY_LEN);
	for (int i = 0; i < CURVE25519_PRIV_KEY_LEN; i++)
	{
		privateKey[i] = random[i];
	}

	//These appear to be performance related adjustments for Curve25519
	//http://crypto.stackexchange.com/questions/11810/when-using-curve25519-why-does-the-private-key-always-have-a-fixed-bit-at-2254
	privateKey[0] &= 248;
	privateKey[31] &= 127;
	privateKey[31] |= 64;

	return privateKey;
}

array<Byte>^ MyCurve25519::Curve25519Native::CalculateSignature(array<Byte>^ random, array<Byte>^ privateKey, array<Byte>^ message)
{
	if (privateKey->Length != CURVE25519_PRIV_KEY_LEN) {
		throw gcnew Exception(gcnew System::String(CURVE25519_PRIV_KEY_LEN_ERR_MSG));
	}

	Byte* signatureBytes = new Byte[CURVE25519_SIG_LEN];
	ZeroMemory(signatureBytes, (sizeof(Byte) * CURVE25519_SIG_LEN));

	pin_ptr<byte> privateKeyBytes = &privateKey[0];
	pin_ptr<byte> randomBytes = &random[0];
	pin_ptr<byte> messageBytes = &message[0];

	unsigned long messageLength = message->Length;

	int result = curve25519_sign(signatureBytes, privateKeyBytes, messageBytes, messageLength, randomBytes);

	array<Byte>^ signature = gcnew array<Byte>(CURVE25519_SIG_LEN);
	Marshal::Copy((IntPtr)signatureBytes, signature, 0, CURVE25519_SIG_LEN);
	delete signatureBytes;

	if (result == 0)
	{
		return signature;
	}
	else
	{
		throw gcnew Exception(gcnew System::String(CURVE25519_SIG_FAILED_MSG));
	}
}

bool MyCurve25519::Curve25519Native::VerifySignature(array<Byte>^ publicKey, array<Byte>^ message, array<Byte>^ signature)
{
	pin_ptr<byte> publicKeyBytes = &publicKey[0];
	pin_ptr<byte> messageBytes = &message[0];
	pin_ptr<byte> signatureBytes = &signature[0];
	unsigned long messageLength = message->Length;

	int iVerify = curve25519_verify(signatureBytes, publicKeyBytes, messageBytes, messageLength);
	bool verified = (iVerify == 0);

	return verified;
}