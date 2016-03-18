#ifndef crypto_verify_32_H
#define crypto_verify_32_H

#define crypto_verify_32_ref_BYTES 32
/* Deviation from curve25519-java's JNI implementation.
	Author: Langboost
	Explanation: crypto_verify_32_ref is defined in compare.h and implemented
	in compare.cpp. This 2nd definition of crypto_verify_32_ref with the extern
	keyword is confusing the compiler. I'm not strong in C++, but I don't see the
	benefit of defining the function twice. As such, I'm removing this def'n
	for the curve25519-windows implementation.

	If there is a compelling reason to keep this, please submit a
	Github issue and explain further. Thanks!
*/
/*
#ifdef __cplusplus
#include <string>
extern "C" {
#endif
extern int crypto_verify_32_ref(const unsigned char *,const unsigned char *);
#ifdef __cplusplus
}
#endif
*/

#define crypto_verify_32 crypto_verify_32_ref
#define crypto_verify_32_BYTES crypto_verify_32_ref_BYTES
#define crypto_verify_32_IMPLEMENTATION "crypto_verify/32/ref"
#ifndef crypto_verify_32_ref_VERSION
#define crypto_verify_32_ref_VERSION "-"
#endif
#define crypto_verify_32_VERSION crypto_verify_32_ref_VERSION

#endif
