// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ssl.h"

extern "C" SSLContextRef AppleCryptoNative_SslCreateContext(int32_t isServer)
{
    if (isServer != 0 && isServer != 1)
        return nullptr;

    return SSLCreateContext(nullptr, isServer ? kSSLServerSide : kSSLClientSide, kSSLStreamType);
}

extern "C" int32_t AppleCryptoNative_SslSetAcceptClientCert(SSLContextRef sslContext)
{
    // NULL and other illegal values are handled by the underlying API
    return SSLSetClientSideAuthenticate(sslContext, kTryAuthenticate);
}

static SSLProtocol PalSslProtocolToSslProtocol(PAL_SslProtocol palProtocolId)
{
    switch (palProtocolId)
    {
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

extern "C" int32_t AppleCryptoNative_SslSetMinProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol)
{
    SSLProtocol protocol = PalSslProtocolToSslProtocol(sslProtocol);

    if (protocol == kSSLProtocolUnknown)
        return errSecParam;

    // NULL and other illegal values are handled by the underlying API
    return SSLSetProtocolVersionMin(sslContext, protocol);
}

extern "C" int32_t AppleCryptoNative_SslSetMaxProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol)
{
    SSLProtocol protocol = PalSslProtocolToSslProtocol(sslProtocol);

    if (protocol == kSSLProtocolUnknown)
        return errSecParam;

    // NULL and other illegal values are handled by the underlying API
    return SSLSetProtocolVersionMax(sslContext, protocol);
}

extern "C" int32_t
AppleCryptoNative_SslCopyCertChain(SSLContextRef sslContext, SecTrustRef* pChainOut, int32_t* pOSStatus)
{
    if (pChainOut != nullptr)
        *pChainOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (sslContext == nullptr || pChainOut == nullptr || pOSStatus == nullptr)
        return -1;

    *pOSStatus = SSLCopyPeerTrust(sslContext, pChainOut);
    return *pOSStatus == noErr;
}

extern "C" int32_t
AppleCryptoNative_SslCopyCADistinguishedNames(SSLContextRef sslContext, CFArrayRef* pArrayOut, int32_t* pOSStatus)
{
    if (pArrayOut != nullptr)
        *pArrayOut = nullptr;
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (sslContext == nullptr || pArrayOut == nullptr || pOSStatus == nullptr)
        return -1;

    *pOSStatus = SSLCopyDistinguishedNames(sslContext, pArrayOut);

    return *pOSStatus == noErr;
}

static int32_t AppleCryptoNative_SslSetSessionOption(SSLContextRef sslContext,
                                                     SSLSessionOption option,
                                                     int32_t value,
                                                     int32_t* pOSStatus)
{
    if (sslContext == nullptr)
        return -1;

    if (value != 0 && value != 1)
        return -2;

    *pOSStatus = SSLSetSessionOption(sslContext, option, !!value);

    return *pOSStatus == noErr;
}

extern "C" int32_t
AppleCryptoNative_SslSetBreakOnServerAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus)
{
    return AppleCryptoNative_SslSetSessionOption(sslContext, kSSLSessionOptionBreakOnServerAuth, setBreak, pOSStatus);
}

extern "C" int32_t
AppleCryptoNative_SslSetBreakOnClientAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus)
{
    return AppleCryptoNative_SslSetSessionOption(sslContext, kSSLSessionOptionBreakOnClientAuth, setBreak, pOSStatus);
}

extern "C" int32_t AppleCryptoNative_SslSetCertificate(SSLContextRef sslContext, CFArrayRef certRefs)
{
    // The underlying call handles NULL inputs, so just pass it through
    return SSLSetCertificate(sslContext, certRefs);
}

extern "C" int32_t AppleCryptoNative_SslSetTargetName(SSLContextRef sslContext,
                                                      const char* pszTargetName,
                                                      int32_t cbTargetName,
                                                      int32_t* pOSStatus)
{
    if (pOSStatus != nullptr)
        *pOSStatus = noErr;

    if (sslContext == nullptr || pszTargetName == nullptr || pOSStatus == nullptr)
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
        *pOSStatus = SSLSetPeerDomainName(sslContext, pszTargetName, static_cast<size_t>(cbTargetName));
    }

    return *pOSStatus == noErr;
}

extern "C" int32_t
AppleCryptoNative_SslSetIoCallbacks(SSLContextRef sslContext, SSLReadFunc readFunc, SSLWriteFunc writeFunc)
{
    return SSLSetIOFuncs(sslContext, readFunc, writeFunc);
}

extern "C" PAL_TlsHandshakeState AppleCryptoNative_SslHandshake(SSLContextRef sslContext)
{
    if (sslContext == nullptr)
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

extern "C" PAL_TlsIo
AppleCryptoNative_SslWrite(SSLContextRef sslContext, const uint8_t* buf, uint32_t bufLen, uint32_t* bytesWritten)
{
    if (bytesWritten == nullptr)
        return PAL_TlsIo_Unknown;

    size_t expected = static_cast<size_t>(bufLen);
    size_t totalWritten;

    OSStatus status = SSLWrite(sslContext, buf, expected, &totalWritten);

    if (status != noErr)
    {
        *bytesWritten = static_cast<uint32_t>(totalWritten);
        return OSStatusToPAL_TlsIo(status);
    }

    return PAL_TlsIo_Success;
}

extern "C" PAL_TlsIo
AppleCryptoNative_SslRead(SSLContextRef sslContext, uint8_t* buf, uint32_t bufLen, uint32_t* written)
{
    if (written == nullptr)
        return PAL_TlsIo_Unknown;

    size_t writtenSize = 0;
    size_t bufSize = static_cast<size_t>(bufLen);

    OSStatus status = SSLRead(sslContext, buf, bufSize, &writtenSize);

    if (writtenSize > UINT_MAX)
    {
        // This shouldn't happen, because we passed a uint32_t as the initial buffer size.
        // But, just in case it does, report back that we're no longer in a known state.
        return PAL_TlsIo_Unknown;
    }

    *written = static_cast<uint32_t>(writtenSize);

    if (writtenSize == 0 && status == errSSLWouldBlock)
    {
        SSLSessionState state = {};
        OSStatus localStatus = SSLGetSessionState(sslContext, &state);

        if (localStatus == noErr && state == kSSLHandshake)
        {
            return PAL_TlsIo_Renegotiate;
        }
    }

    return OSStatusToPAL_TlsIo(status);
}

extern "C" int32_t
AppleCryptoNative_SslIsHostnameMatch(SSLContextRef sslContext, CFStringRef cfHostname, CFDateRef notBefore)
{
    if (sslContext == nullptr || notBefore == nullptr)
        return -1;
    if (cfHostname == nullptr)
        return -2;

    SecPolicyRef sslPolicy = SecPolicyCreateSSL(true, cfHostname);

    if (sslPolicy == nullptr)
        return -3;

    CFMutableArrayRef certs = CFArrayCreateMutable(kCFAllocatorDefault, 0, &kCFTypeArrayCallBacks);

    if (certs == nullptr)
        return -4;

    SecTrustRef existingTrust = nullptr;
    OSStatus osStatus = SSLCopyPeerTrust(sslContext, &existingTrust);

    if (osStatus != noErr)
    {
        CFRelease(certs);
        return -5;
    }

    CFMutableArrayRef anchors = CFArrayCreateMutable(kCFAllocatorDefault, 0, &kCFTypeArrayCallBacks);

    if (anchors == nullptr)
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

    SecTrustRef trust = nullptr;
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
        SecTrustResultType trustResult = {};

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

    if (trust != nullptr)
        CFRelease(trust);

    if (certs != nullptr)
        CFRelease(certs);

    if (anchors != nullptr)
        CFRelease(anchors);

    CFRelease(sslPolicy);
    return ret;
}

extern "C" int32_t AppleCryptoNative_SslShutdown(SSLContextRef sslContext)
{
    return SSLClose(sslContext);
}

extern "C" int32_t AppleCryptoNative_SslGetProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol* pProtocol)
{
    if (pProtocol != nullptr)
        *pProtocol = 0;

    if (sslContext == nullptr || pProtocol == nullptr)
        return errSecParam;

    SSLProtocol protocol = kSSLProtocolUnknown;
    OSStatus osStatus = SSLGetNegotiatedProtocolVersion(sslContext, &protocol);

    if (osStatus == noErr)
    {
        PAL_SslProtocol matchedProtocol = PAL_SslProtocol_None;

        if (protocol == kTLSProtocol12)
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

extern "C" int32_t AppleCryptoNative_SslGetCipherSuite(SSLContextRef sslContext, uint32_t* pCipherSuiteOut)
{
    if (pCipherSuiteOut == nullptr)
        *pCipherSuiteOut = 0;

    return SSLGetNegotiatedCipher(sslContext, pCipherSuiteOut);
}
