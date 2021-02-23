// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)

#include "../../AnyOS/entrypoints.h"

// Include System.Security.Cryptography.Native.Apple headers
#include "pal_digest.h"
#include "pal_ecc.h"
#include "pal_hmac.h"
#include "pal_keychain.h"
#include "pal_random.h"
#include "pal_rsa.h"
#include "pal_sec.h"
#include "pal_seckey.h"
#include "pal_signverify.h"
#include "pal_ssl.h"
#include "pal_symmetric.h"
#include "pal_trust.h"
#include "pal_x509.h"
#include "pal_x509chain.h"
#include "pal_keyderivation.h"

static const Entry s_cryptoAppleNative[] =
{
    DllImportEntry(AppleCryptoNative_DigestFree)
    DllImportEntry(AppleCryptoNative_DigestCreate)
    DllImportEntry(AppleCryptoNative_DigestUpdate)
    DllImportEntry(AppleCryptoNative_DigestFinal)
    DllImportEntry(AppleCryptoNative_DigestCurrent)
    DllImportEntry(AppleCryptoNative_DigestOneShot)
    DllImportEntry(AppleCryptoNative_EccGenerateKey)
    DllImportEntry(AppleCryptoNative_EccGetKeySizeInBits)
    DllImportEntry(AppleCryptoNative_HmacFree)
    DllImportEntry(AppleCryptoNative_HmacCreate)
    DllImportEntry(AppleCryptoNative_HmacInit)
    DllImportEntry(AppleCryptoNative_HmacUpdate)
    DllImportEntry(AppleCryptoNative_HmacFinal)
    DllImportEntry(AppleCryptoNative_HmacCurrent)
    DllImportEntry(AppleCryptoNative_SecKeychainItemCopyKeychain)
    DllImportEntry(AppleCryptoNative_SecKeychainCreate)
    DllImportEntry(AppleCryptoNative_SecKeychainDelete)
    DllImportEntry(AppleCryptoNative_SecKeychainCopyDefault)
    DllImportEntry(AppleCryptoNative_SecKeychainOpen)
    DllImportEntry(AppleCryptoNative_SecKeychainUnlock)
    DllImportEntry(AppleCryptoNative_SetKeychainNeverLock)
    DllImportEntry(AppleCryptoNative_SecKeychainEnumerateIdentities)
    DllImportEntry(AppleCryptoNative_GetRandomBytes)
    DllImportEntry(AppleCryptoNative_RsaGenerateKey)
    DllImportEntry(AppleCryptoNative_RsaDecryptOaep)
    DllImportEntry(AppleCryptoNative_RsaDecryptPkcs)
    DllImportEntry(AppleCryptoNative_RsaEncryptOaep)
    DllImportEntry(AppleCryptoNative_RsaEncryptPkcs)
    DllImportEntry(AppleCryptoNative_RsaSignaturePrimitive)
    DllImportEntry(AppleCryptoNative_RsaDecryptionPrimitive)
    DllImportEntry(AppleCryptoNative_RsaEncryptionPrimitive)
    DllImportEntry(AppleCryptoNative_RsaVerificationPrimitive)
    DllImportEntry(AppleCryptoNative_SecCopyErrorMessageString)
    DllImportEntry(AppleCryptoNative_SecKeyExport)
    DllImportEntry(AppleCryptoNative_SecKeyImportEphemeral)
    DllImportEntry(AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes)
    DllImportEntry(AppleCryptoNative_GenerateSignature)
    DllImportEntry(AppleCryptoNative_GenerateSignatureWithHashAlgorithm)
    DllImportEntry(AppleCryptoNative_VerifySignatureWithHashAlgorithm)
    DllImportEntry(AppleCryptoNative_VerifySignature)
    DllImportEntry(AppleCryptoNative_SslCreateContext)
    DllImportEntry(AppleCryptoNative_SslSetAcceptClientCert)
    DllImportEntry(AppleCryptoNative_SslSetMinProtocolVersion)
    DllImportEntry(AppleCryptoNative_SslSetMaxProtocolVersion)
    DllImportEntry(AppleCryptoNative_SslSetCertificate)
    DllImportEntry(AppleCryptoNative_SslSetTargetName)
    DllImportEntry(AppleCryptoNative_SSLSetALPNProtocols)
    DllImportEntry(AppleCryptoNative_SslGetAlpnSelected)
    DllImportEntry(AppleCryptoNative_SslHandshake)
    DllImportEntry(AppleCryptoNative_SslShutdown)
    DllImportEntry(AppleCryptoNative_SslGetProtocolVersion)
    DllImportEntry(AppleCryptoNative_SslGetCipherSuite)
    DllImportEntry(AppleCryptoNative_SslSetEnabledCipherSuites)
    DllImportEntry(AppleCryptoNative_CryptorFree)
    DllImportEntry(AppleCryptoNative_CryptorCreate)
    DllImportEntry(AppleCryptoNative_CryptorUpdate)
    DllImportEntry(AppleCryptoNative_CryptorFinal)
    DllImportEntry(AppleCryptoNative_CryptorReset)
    DllImportEntry(AppleCryptoNative_StoreEnumerateUserRoot)
    DllImportEntry(AppleCryptoNative_StoreEnumerateMachineRoot)
    DllImportEntry(AppleCryptoNative_StoreEnumerateUserDisallowed)
    DllImportEntry(AppleCryptoNative_StoreEnumerateMachineDisallowed)
    DllImportEntry(AppleCryptoNative_X509GetContentType)
    DllImportEntry(AppleCryptoNative_X509CopyCertFromIdentity)
    DllImportEntry(AppleCryptoNative_X509CopyPrivateKeyFromIdentity)
    DllImportEntry(AppleCryptoNative_X509ImportCollection)
    DllImportEntry(AppleCryptoNative_X509ImportCertificate)
    DllImportEntry(AppleCryptoNative_X509ExportData)
    DllImportEntry(AppleCryptoNative_X509GetRawData)
    DllImportEntry(AppleCryptoNative_X509CopyWithPrivateKey)
    DllImportEntry(AppleCryptoNative_X509MoveToKeychain)
    DllImportEntry(AppleCryptoNative_X509ChainCreateDefaultPolicy)
    DllImportEntry(AppleCryptoNative_X509ChainCreateRevocationPolicy)
    DllImportEntry(AppleCryptoNative_X509ChainEvaluate)
    DllImportEntry(AppleCryptoNative_X509ChainGetChainSize)
    DllImportEntry(AppleCryptoNative_X509ChainGetCertificateAtIndex)
    DllImportEntry(AppleCryptoNative_X509ChainGetTrustResults)
    DllImportEntry(AppleCryptoNative_X509ChainGetStatusAtIndex)
    DllImportEntry(AppleCryptoNative_GetOSStatusForChainStatus)
    DllImportEntry(AppleCryptoNative_X509ChainSetTrustAnchorCertificates)
    DllImportEntry(AppleCryptoNative_Pbkdf2)
};

EXTERN_C const void* CryptoAppleResolveDllImport(const char* name);

EXTERN_C const void* CryptoAppleResolveDllImport(const char* name)
{
    return ResolveDllImport(s_cryptoAppleNative, lengthof(s_cryptoAppleNative), name);
}

#endif // !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
