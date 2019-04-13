// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ssl.h"
#include <dlfcn.h>

// TLS 1.3 is only defined with 10.13 headers, but we build on 10.12
#define kTLSProtocol13_ForwardDef 10

// 10.13.4 introduced public API but linking would fail on all prior versions.
// For that reason we use function pointers instead of direct call.
// This can be revisited after we drop support for 10.12.

static OSStatus (*SSLSetALPNProtocolsPtr)(SSLContextRef context, CFArrayRef protocols) = NULL;
static OSStatus (*SSLCopyALPNProtocolsPtr)(SSLContextRef context, CFArrayRef* protocols) = NULL;
// end of ALPN.

SSLContextRef AppleCryptoNative_SslCreateContext(int32_t isServer)
{
    if (isServer != 0 && isServer != 1)
        return NULL;

    return SSLCreateContext(NULL, isServer ? kSSLServerSide : kSSLClientSide, kSSLStreamType);
}

int32_t AppleCryptoNative_SslSetAcceptClientCert(SSLContextRef sslContext)
{
    // NULL and other illegal values are handled by the underlying API
    return SSLSetClientSideAuthenticate(sslContext, kTryAuthenticate);
}

static SSLProtocol PalSslProtocolToSslProtocol(PAL_SslProtocol palProtocolId)
{
    switch (palProtocolId)
    {
        case PAL_SslProtocol_Tls13:
            return kTLSProtocol13_ForwardDef;
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
    }
}

int32_t AppleCryptoNative_SslSetMinProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol)
{
    SSLProtocol protocol = PalSslProtocolToSslProtocol(sslProtocol);

    if (protocol == kSSLProtocolUnknown)
        return errSecParam;

    // NULL and other illegal values are handled by the underlying API
    return SSLSetProtocolVersionMin(sslContext, protocol);
}

int32_t AppleCryptoNative_SslSetMaxProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol)
{
    SSLProtocol protocol = PalSslProtocolToSslProtocol(sslProtocol);

    if (protocol == kSSLProtocolUnknown)
        return errSecParam;

    // NULL and other illegal values are handled by the underlying API
    return SSLSetProtocolVersionMax(sslContext, protocol);
}

int32_t AppleCryptoNative_SslCopyCertChain(SSLContextRef sslContext, SecTrustRef* pChainOut, int32_t* pOSStatus)
{
    if (pChainOut != NULL)
        *pChainOut = NULL;
    if (pOSStatus != NULL)
        *pOSStatus = noErr;

    if (sslContext == NULL || pChainOut == NULL || pOSStatus == NULL)
        return -1;

    *pOSStatus = SSLCopyPeerTrust(sslContext, pChainOut);
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

    *pOSStatus = SSLCopyDistinguishedNames(sslContext, pArrayOut);

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

    *pOSStatus = SSLSetSessionOption(sslContext, option, !!value);

    return *pOSStatus == noErr;
}

int32_t AppleCryptoNative_SslSetBreakOnServerAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus)
{
    return AppleCryptoNative_SslSetSessionOption(sslContext, kSSLSessionOptionBreakOnServerAuth, setBreak, pOSStatus);
}

int32_t AppleCryptoNative_SslSetBreakOnClientAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus)
{
    return AppleCryptoNative_SslSetSessionOption(sslContext, kSSLSessionOptionBreakOnClientAuth, setBreak, pOSStatus);
}

int32_t AppleCryptoNative_SslSetCertificate(SSLContextRef sslContext, CFArrayRef certRefs)
{
    // The underlying call handles NULL inputs, so just pass it through
    return SSLSetCertificate(sslContext, certRefs);
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
    *pOSStatus = SSLGetPeerDomainNameLength(sslContext, &currentLength);

    // We'll end up walking down the path that sets the hostname more than once during
    // the handshake dance. But once the handshake starts Secure Transport isn't willing to
    // listen to this.  So, if we've already set it, don't set it again.
    if (*pOSStatus == noErr && currentLength == 0)
    {
        *pOSStatus = SSLSetPeerDomainName(sslContext, pszTargetName, (size_t)cbTargetName);
    }

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
    return SSLSetIOFuncs(sslContext, readFunc, writeFunc);
}

PAL_TlsHandshakeState AppleCryptoNative_SslHandshake(SSLContextRef sslContext)
{
    if (sslContext == NULL)
        return PAL_TlsHandshakeState_Unknown;

    OSStatus osStatus = SSLHandshake(sslContext);

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

    OSStatus status = SSLWrite(sslContext, buf, expected, &totalWritten);

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

    OSStatus status = SSLRead(sslContext, buf, bufSize, &writtenSize);

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
        OSStatus localStatus = SSLGetSessionState(sslContext, &state);

        if (localStatus == noErr && state == kSSLHandshake)
        {
            return PAL_TlsIo_Renegotiate;
        }
    }

    return OSStatusToPAL_TlsIo(status);
}

int32_t AppleCryptoNative_SslIsHostnameMatch(SSLContextRef sslContext, CFStringRef cfHostname, CFDateRef notBefore)
{
    if (sslContext == NULL || notBefore == NULL)
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
    OSStatus osStatus = SSLCopyPeerTrust(sslContext, &existingTrust);

    if (osStatus != noErr)
    {
        CFRelease(certs);
        return -5;
    }

    CFMutableArrayRef anchors = CFArrayCreateMutable(kCFAllocatorDefault, 0, &kCFTypeArrayCallBacks);

    if (anchors == NULL)
    {
        CFRelease(certs);
        return -6;
    }

    CFIndex count = SecTrustGetCertificateCount(existingTrust);

    for (CFIndex i = 0; i < count; i++)
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

        osStatus = SecTrustEvaluate(trust, &trustResult);

        if (osStatus != noErr)
        {
            ret = -7;
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

    if (trust != NULL)
        CFRelease(trust);

    if (certs != NULL)
        CFRelease(certs);

    if (anchors != NULL)
        CFRelease(anchors);

    CFRelease(sslPolicy);
    return ret;
}

int32_t AppleCryptoNative_SslShutdown(SSLContextRef sslContext)
{
    return SSLClose(sslContext);
}

int32_t AppleCryptoNative_SslGetProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol* pProtocol)
{
    if (pProtocol != NULL)
        *pProtocol = 0;

    if (sslContext == NULL || pProtocol == NULL)
        return errSecParam;

    SSLProtocol protocol = kSSLProtocolUnknown;
    OSStatus osStatus = SSLGetNegotiatedProtocolVersion(sslContext, &protocol);

    if (osStatus == noErr)
    {
        PAL_SslProtocol matchedProtocol = PAL_SslProtocol_None;

        if (protocol == kTLSProtocol13_ForwardDef)
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

    return osStatus;
}

int32_t AppleCryptoNative_SslGetCipherSuite(SSLContextRef sslContext, uint16_t* pCipherSuiteOut)
{
    if (pCipherSuiteOut == NULL)
    {
        return errSecParam;
    }

    SSLCipherSuite cipherSuite;
    OSStatus status = SSLGetNegotiatedCipher(sslContext, &cipherSuite);
    *pCipherSuiteOut = (uint16_t)cipherSuite;

    return status;
}

int32_t AppleCryptoNative_SslSetEnabledCipherSuites(SSLContextRef sslContext, const uint32_t* cipherSuites, int32_t numCipherSuites)
{
    // Max numCipherSuites is 2^16 (all possible cipher suites)
    assert(numCipherSuites < (1 << 16));

    if (sizeof(SSLCipherSuite) == sizeof(uint32_t))
    {
        // macOS
        return SSLSetEnabledCiphers(sslContext, cipherSuites, (size_t)numCipherSuites);
    }
    else
    {
        // iOS, tvOS, watchOS
        SSLCipherSuite* cipherSuites16 = (SSLCipherSuite*)calloc((size_t)numCipherSuites, sizeof(SSLCipherSuite));

        if (cipherSuites16 == NULL)
        {
            return errSSLInternal;
        }

        for (int i = 0; i < numCipherSuites; i++)
        {
            cipherSuites16[i] = (SSLCipherSuite)cipherSuites[i];
        }

        OSStatus status = SSLSetEnabledCiphers(sslContext, cipherSuites16, (size_t)numCipherSuites);

        free(cipherSuites16);
        return status;
    }
}

__attribute__((constructor)) static void InitializeAppleCryptoSslShim()
{
    SSLSetALPNProtocolsPtr = (OSStatus(*)(SSLContextRef, CFArrayRef))dlsym(RTLD_DEFAULT, "SSLSetALPNProtocols");
    SSLCopyALPNProtocolsPtr = (OSStatus(*)(SSLContextRef, CFArrayRef*))dlsym(RTLD_DEFAULT, "SSLCopyALPNProtocols");
}
