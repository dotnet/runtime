// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ssl.h"
#include <dlfcn.h>

// 10.13.4 introduced public API but linking would fail on all prior versions.
// For that reason we use function pointers instead of direct call.
// This can be revisited after we drop support for 10.12 and iOS 10

static OSStatus (*SSLSetALPNProtocolsPtr)(SSLContextRef context, CFArrayRef protocols) = NULL;
static OSStatus (*SSLCopyALPNProtocolsPtr)(SSLContextRef context, CFArrayRef* protocols) = NULL;
// end of ALPN.

SSLContextRef AppleCryptoNative_SslCreateContext(int32_t isServer)
{
    if (isServer != 0 && isServer != 1)
        return NULL;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    return SSLCreateContext(NULL, isServer ? kSSLServerSide : kSSLClientSide, kSSLStreamType);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslSetAcceptClientCert(SSLContextRef sslContext)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    // NULL and other illegal values are handled by the underlying API
    return SSLSetClientSideAuthenticate(sslContext, kTryAuthenticate);
#pragma clang diagnostic pop
}

static SSLProtocol PalSslProtocolToSslProtocol(PAL_SslProtocol palProtocolId)
{
    switch (palProtocolId)
    {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        case PAL_SslProtocol_Tls13:
            return kTLSProtocol13;
        case PAL_SslProtocol_Tls12: 
            return kTLSProtocol12;
        case PAL_SslProtocol_Tls11:
            return kTLSProtocol11;
        case PAL_SslProtocol_Tls10:
            return kTLSProtocol1;
        case PAL_SslProtocol_Ssl3:
            return kSSLProtocol3;
        case PAL_SslProtocol_Ssl2:
            return kSSLProtocol2;
        case PAL_SslProtocol_None:
        default:
            return kSSLProtocolUnknown;
#pragma clang diagnostic pop
    }
}

int32_t AppleCryptoNative_SslSetMinProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol)
{
    SSLProtocol protocol = PalSslProtocolToSslProtocol(sslProtocol);

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    if (protocol == kSSLProtocolUnknown)
        return errSecParam;

    // NULL and other illegal values are handled by the underlying API
    return SSLSetProtocolVersionMin(sslContext, protocol);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslSetMaxProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol)
{
    SSLProtocol protocol = PalSslProtocolToSslProtocol(sslProtocol);

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    if (protocol == kSSLProtocolUnknown)
        return errSecParam;

    // NULL and other illegal values are handled by the underlying API
    return SSLSetProtocolVersionMax(sslContext, protocol);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslCopyCertChain(SSLContextRef sslContext, SecTrustRef* pChainOut, int32_t* pOSStatus)
{
    if (pChainOut != NULL)
        *pChainOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (sslContext == NULL || pChainOut == NULL || pOSStatus == NULL)
        return -1;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    *pOSStatus = SSLCopyPeerTrust(sslContext, pChainOut);
#pragma clang diagnostic pop
    return *pOSStatus == noErr;
}

int32_t
AppleCryptoNative_SslCopyCADistinguishedNames(SSLContextRef sslContext, CFArrayRef* pArrayOut, int32_t* pOSStatus)
{
    if (pArrayOut != NULL)
        *pArrayOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (sslContext == NULL || pArrayOut == NULL || pOSStatus == NULL)
        return -1;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    *pOSStatus = SSLCopyDistinguishedNames(sslContext, pArrayOut);
#pragma clang diagnostic pop

    return *pOSStatus == noErr;
}

static int32_t AppleCryptoNative_SslSetSessionOption(SSLContextRef sslContext,
                                                     SSLSessionOption option,
                                                     int32_t value,
                                                     int32_t* pOSStatus)
{
    if (sslContext == NULL)
        return -1;

    if (value != 0 && value != 1)
        return -2;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    *pOSStatus = SSLSetSessionOption(sslContext, option, !!value);
#pragma clang diagnostic pop

    return *pOSStatus == noErr;
}

int32_t AppleCryptoNative_SslSetBreakOnServerAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    return AppleCryptoNative_SslSetSessionOption(sslContext, kSSLSessionOptionBreakOnServerAuth, setBreak, pOSStatus);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslSetBreakOnClientAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    return AppleCryptoNative_SslSetSessionOption(sslContext, kSSLSessionOptionBreakOnClientAuth, setBreak, pOSStatus);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslSetCertificate(SSLContextRef sslContext, CFArrayRef certRefs)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    // The underlying call handles NULL inputs, so just pass it through
    return SSLSetCertificate(sslContext, certRefs);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslSetTargetName(SSLContextRef sslContext,
                                           const char* pszTargetName,
                                           int32_t cbTargetName,
                                           int32_t* pOSStatus)
{
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (sslContext == NULL || pszTargetName == NULL || pOSStatus == NULL)
        return -1;

    if (cbTargetName < 0)
        return -2;

    size_t currentLength;
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    *pOSStatus = SSLGetPeerDomainNameLength(sslContext, &currentLength);

    // We'll end up walking down the path that sets the hostname more than once during
    // the handshake dance. But once the handshake starts Secure Transport isn't willing to
    // listen to this.  So, if we've already set it, don't set it again.
    if (*pOSStatus == noErr && currentLength == 0)
    {
        *pOSStatus = SSLSetPeerDomainName(sslContext, pszTargetName, (size_t)cbTargetName);
    }
#pragma clang diagnostic pop

    return *pOSStatus == noErr;
}

int32_t AppleCryptoNative_SSLSetALPNProtocols(SSLContextRef sslContext,
                                                        CFArrayRef protocols,
                                                        int32_t* pOSStatus)
{
    if (sslContext == NULL || protocols == NULL || pOSStatus == NULL)
        return -1;

    if (!SSLSetALPNProtocolsPtr)
    {
        // not available.
        *pOSStatus = 0;
        return 1;
    }
    // The underlying call handles NULL inputs, so just pass it through
    *pOSStatus = (*SSLSetALPNProtocolsPtr)(sslContext, protocols);
    return *pOSStatus == noErr;
}

int32_t AppleCryptoNative_SslGetAlpnSelected(SSLContextRef sslContext, CFDataRef* protocol)
{
    if (sslContext == NULL || protocol == NULL)
        return -1;

    *protocol = NULL;
    if (!SSLCopyALPNProtocolsPtr)
    {
        // not available.
        return 0;
    }

    CFArrayRef protocols = NULL;
    OSStatus osStatus = (*SSLCopyALPNProtocolsPtr)(sslContext, &protocols);

    if (osStatus == noErr && protocols != NULL && CFArrayGetCount(protocols) > 0)
    {
        *protocol =
            CFStringCreateExternalRepresentation(NULL, CFArrayGetValueAtIndex(protocols, 0), kCFStringEncodingASCII, 0);
    }

    if (protocols)
        CFRelease(protocols);

    return *protocol != NULL;
}

int32_t AppleCryptoNative_SslSetIoCallbacks(SSLContextRef sslContext, SSLReadFunc readFunc, SSLWriteFunc writeFunc)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    return SSLSetIOFuncs(sslContext, readFunc, writeFunc);
#pragma clang diagnostic pop
}

PAL_TlsHandshakeState AppleCryptoNative_SslHandshake(SSLContextRef sslContext)
{
    if (sslContext == NULL)
        return PAL_TlsHandshakeState_Unknown;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    OSStatus osStatus = SSLHandshake(sslContext);
#pragma clang diagnostic pop

    switch (osStatus)
    {
        case noErr:
            return PAL_TlsHandshakeState_Complete;
        case errSSLWouldBlock:
            return PAL_TlsHandshakeState_WouldBlock;
        case errSSLServerAuthCompleted:
            return PAL_TlsHandshakeState_ServerAuthCompleted;
        default:
            return osStatus;
    }
}

static PAL_TlsIo OSStatusToPAL_TlsIo(OSStatus status)
{
    switch (status)
    {
        case noErr:
            return PAL_TlsIo_Success;
        case errSSLWouldBlock:
            return PAL_TlsIo_WouldBlock;
        case errSSLClosedGraceful:
            return PAL_TlsIo_ClosedGracefully;
        default:
            return status;
    }
}

PAL_TlsIo
AppleCryptoNative_SslWrite(SSLContextRef sslContext, const uint8_t* buf, uint32_t bufLen, uint32_t* bytesWritten)
{
    if (bytesWritten == NULL)
        return PAL_TlsIo_Unknown;

    size_t expected = (size_t)bufLen;
    size_t totalWritten;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    OSStatus status = SSLWrite(sslContext, buf, expected, &totalWritten);
#pragma clang diagnostic pop

    if (status != noErr)
    {
        *bytesWritten = (uint32_t)totalWritten;
        return OSStatusToPAL_TlsIo(status);
    }

    return PAL_TlsIo_Success;
}

PAL_TlsIo AppleCryptoNative_SslRead(SSLContextRef sslContext, uint8_t* buf, uint32_t bufLen, uint32_t* written)
{
    if (written == NULL)
        return PAL_TlsIo_Unknown;

    size_t writtenSize = 0;
    size_t bufSize = (size_t)bufLen;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    OSStatus status = SSLRead(sslContext, buf, bufSize, &writtenSize);
#pragma clang diagnostic pop

    if (writtenSize > UINT_MAX)
    {
        // This shouldn't happen, because we passed a uint32_t as the initial buffer size.
        // But, just in case it does, report back that we're no longer in a known state.
        return PAL_TlsIo_Unknown;
    }

    *written = (uint32_t)writtenSize;

    if (writtenSize == 0 && status == errSSLWouldBlock)
    {
        SSLSessionState state;
        memset(&state, 0, sizeof(SSLSessionState));
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        OSStatus localStatus = SSLGetSessionState(sslContext, &state);

        if (localStatus == noErr && state == kSSLHandshake)
        {
            return PAL_TlsIo_Renegotiate;
        }
#pragma clang diagnostic pop
    }

    return OSStatusToPAL_TlsIo(status);
}

int32_t AppleCryptoNative_SslIsHostnameMatch(SSLContextRef sslContext, CFStringRef cfHostname, CFDateRef notBefore, int32_t* pOSStatus)
{
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (sslContext == NULL || notBefore == NULL || pOSStatus == NULL)
        return -1;

    if (cfHostname == NULL)
        return -2;

    SecPolicyRef sslPolicy = SecPolicyCreateSSL(true, cfHostname);

    if (sslPolicy == NULL)
        return -3;

    CFMutableArrayRef certs = CFArrayCreateMutable(kCFAllocatorDefault, 0, &kCFTypeArrayCallBacks);

    if (certs == NULL)
        return -4;

    SecTrustRef existingTrust = NULL;
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    OSStatus osStatus = SSLCopyPeerTrust(sslContext, &existingTrust);
#pragma clang diagnostic pop

    if (osStatus != noErr)
    {
        CFRelease(certs);
        *pOSStatus = osStatus;
        return -5;
    }

    CFMutableArrayRef anchors = CFArrayCreateMutable(kCFAllocatorDefault, 0, &kCFTypeArrayCallBacks);

    if (anchors == NULL)
    {
        CFRelease(certs);
        CFRelease(existingTrust);
        return -6;
    }

    CFIndex certificateCount = SecTrustGetCertificateCount(existingTrust);

    for (CFIndex i = 0; i < certificateCount; i++)
    {
        SecCertificateRef item = SecTrustGetCertificateAtIndex(existingTrust, i);
        CFArrayAppendValue(certs, item);

        // Copy the EE cert into the anchors set, this will make the chain part
        // always return true.
        if (i == 0)
        {
            CFArrayAppendValue(anchors, item);
        }
    }

    SecTrustRef trust = NULL;
    osStatus = SecTrustCreateWithCertificates(certs, sslPolicy, &trust);
    int32_t ret = INT_MIN;

    if (osStatus == noErr)
    {
        osStatus = SecTrustSetAnchorCertificates(trust, anchors);
    }

    if (osStatus == noErr)
    {
        osStatus = SecTrustSetVerifyDate(trust, notBefore);
    }

    if (osStatus == noErr)
    {
        SecTrustResultType trustResult;
        memset(&trustResult, 0, sizeof(SecTrustResultType));

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        osStatus = SecTrustEvaluate(trust, &trustResult);

        if (trustResult == kSecTrustResultRecoverableTrustFailure && osStatus == noErr && certificateCount > 1)
        {
            // If we get recoverable failure, let's try it again with full anchor list.
            // We already stored just the first certificate into anchors; now we store the rest.
            for (CFIndex i = 1; i < certificateCount; i++)
            {
                CFArrayAppendValue(anchors, SecTrustGetCertificateAtIndex(existingTrust, i));
            }

            osStatus = SecTrustSetAnchorCertificates(trust, anchors);
            if (osStatus == noErr)
            {
                memset(&trustResult, 0, sizeof(SecTrustResultType));
                osStatus = SecTrustEvaluate(trust, &trustResult);
            }
        }
#pragma clang diagnostic pop

        if (osStatus == noErr && trustResult != kSecTrustResultUnspecified && trustResult != kSecTrustResultProceed)
        {
            // If evaluation succeeded but result is not trusted try to get details.
            CFDictionaryRef detailsAndStuff = SecTrustCopyResult(trust);

            if (detailsAndStuff != NULL)
            {
                CFArrayRef details = CFDictionaryGetValue(detailsAndStuff, CFSTR("TrustResultDetails"));

                if (details != NULL && CFArrayGetCount(details) > 0)
                {
                    CFArrayRef statusCodes = CFDictionaryGetValue(CFArrayGetValueAtIndex(details,0), CFSTR("StatusCodes"));

                    OSStatus status = 0;
                    // look for first failure to keep it simple. Normally, there will be exactly one.
                    for (int i = 0; i < CFArrayGetCount(statusCodes); i++)
                    {
                        CFNumberGetValue(CFArrayGetValueAtIndex(statusCodes, i), kCFNumberSInt32Type, &status);
                        if (status != noErr)
                        {
                            *pOSStatus = status;
                            break;
                        }
                    }
                }

                CFRelease(detailsAndStuff);
            }
        }

        if (osStatus != noErr)
        {
            ret = -7;
            *pOSStatus = osStatus;
        }
        else if (trustResult == kSecTrustResultUnspecified || trustResult == kSecTrustResultProceed)
        {
            ret = 1;
        }
        else if (trustResult == kSecTrustResultDeny || trustResult == kSecTrustResultRecoverableTrustFailure)
        {
            ret = 0;
        }
        else
        {
            ret = -8;
        }
    }
    else
    {
        *pOSStatus = osStatus;
    }

    if (trust != NULL)
        CFRelease(trust);

    if (certs != NULL)
        CFRelease(certs);

    if (anchors != NULL)
        CFRelease(anchors);

    if (existingTrust != NULL)
        CFRelease(existingTrust);

    CFRelease(sslPolicy);
    return ret;
}

int32_t AppleCryptoNative_SslShutdown(SSLContextRef sslContext)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    return SSLClose(sslContext);
#pragma clang diagnostic pop
}

int32_t AppleCryptoNative_SslGetProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol* pProtocol)
{
    if (pProtocol != NULL)
        *pProtocol = 0;

    if (sslContext == NULL || pProtocol == NULL)
        return errSecParam;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    SSLProtocol protocol = kSSLProtocolUnknown;
    OSStatus osStatus = SSLGetNegotiatedProtocolVersion(sslContext, &protocol);

    if (osStatus == noErr)
    {
        PAL_SslProtocol matchedProtocol = PAL_SslProtocol_None;

        if (protocol == kTLSProtocol13)
            matchedProtocol = PAL_SslProtocol_Tls13;
        else if (protocol == kTLSProtocol12)
            matchedProtocol = PAL_SslProtocol_Tls12;
        else if (protocol == kTLSProtocol11)
            matchedProtocol = PAL_SslProtocol_Tls11;
        else if (protocol == kTLSProtocol1)
            matchedProtocol = PAL_SslProtocol_Tls10;
        else if (protocol == kSSLProtocol3)
            matchedProtocol = PAL_SslProtocol_Ssl3;
        else if (protocol == kSSLProtocol2)
            matchedProtocol = PAL_SslProtocol_Ssl2;

        *pProtocol = matchedProtocol;
    }
#pragma clang diagnostic pop

    return osStatus;
}

int32_t AppleCryptoNative_SslGetCipherSuite(SSLContextRef sslContext, uint16_t* pCipherSuiteOut)
{
    if (pCipherSuiteOut == NULL)
    {
        return errSecParam;
    }

    SSLCipherSuite cipherSuite;
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    OSStatus status = SSLGetNegotiatedCipher(sslContext, &cipherSuite);
#pragma clang diagnostic pop
    *pCipherSuiteOut = (uint16_t)cipherSuite;

    return status;
}

int32_t AppleCryptoNative_SslSetEnabledCipherSuites(SSLContextRef sslContext, const uint32_t* cipherSuites, int32_t numCipherSuites)
{
    // Max numCipherSuites is 2^16 (all possible cipher suites)
    assert(numCipherSuites < (1 << 16));

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
    if (sizeof(SSLCipherSuite) == sizeof(uint32_t))
    {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        // macOS
        return SSLSetEnabledCiphers(sslContext, (const SSLCipherSuite *)cipherSuites, (size_t)numCipherSuites);
#pragma clang diagnostic pop   
    }
    else
#endif
    {
        // MacCatalyst, iOS, tvOS, watchOS
        SSLCipherSuite* cipherSuites16 = (SSLCipherSuite*)calloc((size_t)numCipherSuites, sizeof(SSLCipherSuite));

        if (cipherSuites16 == NULL)
        {
            return errSSLInternal;
        }

        for (int i = 0; i < numCipherSuites; i++)
        {
            cipherSuites16[i] = (SSLCipherSuite)cipherSuites[i];
        }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
        OSStatus status = SSLSetEnabledCiphers(sslContext, cipherSuites16, (size_t)numCipherSuites);
#pragma clang diagnostic pop

        free(cipherSuites16);
        return status;
    }
}

__attribute__((constructor)) static void InitializeAppleCryptoSslShim()
{
    SSLSetALPNProtocolsPtr = (OSStatus(*)(SSLContextRef, CFArrayRef))dlsym(RTLD_DEFAULT, "SSLSetALPNProtocols");
    SSLCopyALPNProtocolsPtr = (OSStatus(*)(SSLContextRef, CFArrayRef*))dlsym(RTLD_DEFAULT, "SSLCopyALPNProtocols");
}
