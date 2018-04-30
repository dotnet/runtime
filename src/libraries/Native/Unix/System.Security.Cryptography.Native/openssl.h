// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#pragma once

#include <openssl/x509.h>
#include <openssl/x509v3.h>

extern "C" int32_t CryptoNative_GetX509Thumbprint(X509* x509, uint8_t* pBuf, int32_t cBuf);

extern "C" ASN1_TIME* CryptoNative_GetX509NotBefore(X509* x509);

extern "C" ASN1_TIME* CryptoNative_GetX509NotAfter(X509* x509);

extern "C" ASN1_TIME* CryptoNative_GetX509CrlNextUpdate(X509_CRL* crl);

extern "C" int32_t CryptoNative_GetX509Version(X509* x509);

extern "C" ASN1_OBJECT* CryptoNative_GetX509PublicKeyAlgorithm(X509* x509);

extern "C" ASN1_OBJECT* CryptoNative_GetX509SignatureAlgorithm(X509* x509);

extern "C" int32_t CryptoNative_GetX509PublicKeyParameterBytes(X509* x509, uint8_t* pBuf, int32_t cBuf);

extern "C" ASN1_BIT_STRING* CryptoNative_GetX509PublicKeyBytes(X509* x509);

extern "C" int32_t CryptoNative_GetAsn1StringBytes(ASN1_STRING* asn1, uint8_t* pBuf, int32_t cBuf);

extern "C" int32_t CryptoNative_GetX509NameRawBytes(X509_NAME* x509Name, uint8_t* pBuf, int32_t cBuf);

extern "C" int32_t CryptoNative_GetX509EkuFieldCount(EXTENDED_KEY_USAGE* eku);

extern "C" ASN1_OBJECT* CryptoNative_GetX509EkuField(EXTENDED_KEY_USAGE* eku, int32_t loc);

extern "C" BIO* CryptoNative_GetX509NameInfo(X509* x509, int32_t nameType, int32_t forIssuer);

extern "C" int32_t CryptoNative_CheckX509Hostname(X509* x509, const char* hostname, int32_t cchHostname);

extern "C" int32_t CryptoNative_CheckX509IpAddress(
    X509* x509, const uint8_t* addressBytes, int32_t addressBytesLen, const char* hostname, int32_t cchHostname);

extern "C" int32_t CryptoNative_GetX509StackFieldCount(STACK_OF(X509) * stack);

extern "C" X509* CryptoNative_GetX509StackField(STACK_OF(X509) * stack, int loc);

extern "C" void CryptoNative_RecursiveFreeX509Stack(STACK_OF(X509) * stack);

extern "C" int32_t CryptoNative_SetX509ChainVerifyTime(
    X509_STORE_CTX* ctx, int32_t year, int32_t month, int32_t day, int32_t hour, int32_t minute, int32_t second, int32_t isDst);

extern "C" X509* CryptoNative_ReadX509AsDerFromBio(BIO* bio);

extern "C" int32_t CryptoNative_BioTell(BIO* bio);

extern "C" int32_t CryptoNative_BioSeek(BIO* bio, int32_t ofs);

extern "C" STACK_OF(X509) * CryptoNative_NewX509Stack(void);

extern "C" int32_t CryptoNative_PushX509StackField(STACK_OF(X509) * stack, X509* x509);

extern "C" int32_t CryptoNative_GetRandomBytes(uint8_t* buf, int32_t num);

extern "C" int32_t CryptoNative_LookupFriendlyNameByOid(const char* oidValue, const char** friendlyName);

extern "C" int32_t CryptoNative_EnsureOpenSslInitialized(void);

extern "C" char* CryptoNative_SSLEayVersion(void);
