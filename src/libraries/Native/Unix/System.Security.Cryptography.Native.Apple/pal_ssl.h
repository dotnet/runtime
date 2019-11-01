// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include <Security/Security.h>

enum
{
    PAL_TlsHandshakeState_Unknown = 0,
    PAL_TlsHandshakeState_Complete = 1,
    PAL_TlsHandshakeState_WouldBlock = 2,
    PAL_TlsHandshakeState_ServerAuthCompleted = 3,
    PAL_TlsHandshakeState_ClientAuthCompleted = 4,
};
typedef int32_t PAL_TlsHandshakeState;

enum
{
    PAL_TlsIo_Unknown = 0,
    PAL_TlsIo_Success = 1,
    PAL_TlsIo_WouldBlock = 2,
    PAL_TlsIo_ClosedGracefully = 3,
    PAL_TlsIo_Renegotiate = 4,
};
typedef int32_t PAL_TlsIo;

enum
{
    PAL_SslProtocol_None = 0,
    PAL_SslProtocol_Ssl2 = 12,
    PAL_SslProtocol_Ssl3 = 48,
    PAL_SslProtocol_Tls10 = 192,
    PAL_SslProtocol_Tls11 = 768,
    PAL_SslProtocol_Tls12 = 3072,
    PAL_SslProtocol_Tls13 = 12288,
};
typedef int32_t PAL_SslProtocol;

/*
Create an SSL context, for the Server or Client role as determined by isServer.

Returns NULL if an invalid boolean is given for isServer, an SSLContextRef otherwise.
*/
DLLEXPORT SSLContextRef AppleCryptoNative_SslCreateContext(int32_t isServer);

/*
Indicate that an SSL Context (in server mode) should allow a client to present a mutual auth cert.

Returns The result of SSLSetClientSideAuthenticate
*/
DLLEXPORT int32_t AppleCryptoNative_SslSetAcceptClientCert(SSLContextRef sslContext);

/*
Assign a minimum to the TLS protocol version for this connection.

Returns the output of SSLSetProtocolVersionMin
*/
DLLEXPORT int32_t AppleCryptoNative_SslSetMinProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol);

/*
Assign a maximum to the TLS protocol version for this connection.

Returns the output of SSLSetProtocolVersionMax
*/
DLLEXPORT int32_t AppleCryptoNative_SslSetMaxProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol);

/*
Get the SecTrustRef from the SSL context which represents the certificte chain.

Returns 1 on success, 0 on failure, and other values on invalid state.

Output:
pChainOut: Receives the SecTrustRef representing the populated chain
pOSStatus: Receives the value returned by SSLCopyPeerTrust
*/
DLLEXPORT int32_t
AppleCryptoNative_SslCopyCertChain(SSLContextRef sslContext, SecTrustRef* pChainOut, int32_t* pOSStatus);

/*
Get the list of DN values for acceptable issuers for this connection.

Returns 1 on success, 0 on OSStatus-error, other values for invalid state.

Output:
pChainOut: Receives an array of CFDataRef values representing the encoded X500 DistinguishedName
values sent by the server.

pOSStatus: Receives the output of SSLCopyDistinguishedNames.
*/
DLLEXPORT int32_t
AppleCryptoNative_SslCopyCADistinguishedNames(SSLContextRef sslContext, CFArrayRef* pArrayOut, int32_t* pOSStatus);

/*
Sets the policy of whether or not to break when a server identity has been presented.

Returns 1 on success, 0 on failure, other values on invalid state.

Output:
pOSStatus: Receives the value returned by SSLSetSessionOption
*/
DLLEXPORT int32_t
AppleCryptoNative_SslSetBreakOnServerAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus);

/*
Sets the policy of whether or not to break when a client identity has been presented.

Returns 1 on success, 0 on failure, other values on invalid state.

Output:
pOSStatus: Receives the value returned by SSLSetSessionOption
*/
DLLEXPORT int32_t
AppleCryptoNative_SslSetBreakOnClientAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus);

/*
Set the certificate chain for the ServerHello or ClientHello message.

certRefs should be an array of [ SecIdentityRef, SecCertificateRef* ], the 0 element being the
public/private pair for this entity, and all subsequent elements being the public element of an
intermediate (non-root) certificate.

Returns the output of SSLSetCertificate
*/
DLLEXPORT int32_t AppleCryptoNative_SslSetCertificate(SSLContextRef sslContext, CFArrayRef certRefs);

/*
Set the target hostname for SNI. pszTargetName must already be converted for IDNA if required.

Returns 1 on success, 0 on failure, other values for invalid state.

Output:
pOSStatus: Receives the value for SSLSetPeerDomainName
*/
DLLEXPORT int32_t AppleCryptoNative_SslSetTargetName(SSLContextRef sslContext,
                                                     const char* pszTargetName,
                                                     int32_t cbTargetName,
                                                     int32_t* pOSStatus);

/*
Set list of application protocols for ClientHello.

Returns 1 on success, 0 on failure, other values for invalid state.

Output:
pOSStatus: Receives the value from SSLSetALPNData()
*/
DLLEXPORT int32_t AppleCryptoNative_SSLSetALPNProtocols(SSLContextRef sslContext, CFArrayRef protocols, int32_t* pOSStatus);

/*
Get negotiated protocol value from ServerHello.
*/
DLLEXPORT int32_t AppleCryptoNative_SslGetAlpnSelected(SSLContextRef sslContext, CFDataRef *protocol);

/*
Register the callbacks for reading and writing data to the SSL context.

Returns the output of SSLSetIOFuncs.
*/
DLLEXPORT int32_t
AppleCryptoNative_SslSetIoCallbacks(SSLContextRef sslContext, SSLReadFunc readFunc, SSLWriteFunc writeFunc);

/*
Pump the TLS handshake.

Returns an indication of what state the error is in. Any negative number means an error occurred.
*/
DLLEXPORT PAL_TlsHandshakeState AppleCryptoNative_SslHandshake(SSLContextRef sslContext);

/*
Take bufLen bytes of cleartext data from buf and encrypt/frame the data.
Processed data will then be sent into the write callback.

Returns a PAL_TlsIo code indicitating how to proceed.

Output:
bytesWritten: When any value other than PAL_TlsIo_Success is returned, receives the number of bytes
which were read from buf. On PAL_TlsIo_Success the parameter is not written through (but must still
not be NULL)
*/
DLLEXPORT PAL_TlsIo
AppleCryptoNative_SslWrite(SSLContextRef sslContext, const uint8_t* buf, uint32_t bufLen, uint32_t* bytesWritten);

/*
Read up to bufLen bytes of framed/encrypted data from the connection into buf.
Unless a holdover from a previous incomplete read is present this will invoke the read callback
to get data from "the connection".

Returns a PAL_TlsIo code indicating how to proceed.

Output:
written: Receives the number of bytes written into buf
*/
DLLEXPORT PAL_TlsIo
AppleCryptoNative_SslRead(SSLContextRef sslContext, uint8_t* buf, uint32_t bufLen, uint32_t* written);

/*
Check to see if the server identity certificate for this connection matches the requested hostname.

notBefore: Specify the EE/leaf certificate's notBefore value to prevent a false negative due to
the certificate being expired (or not yet valid).

Returns 1 on match, 0 on mismatch, any other value indicates an invalid state.
*/
DLLEXPORT int32_t
AppleCryptoNative_SslIsHostnameMatch(SSLContextRef sslContext, CFStringRef cfHostname, CFDateRef notBefore);

/*
Generate a TLS Close alert to terminate the session.

Returns the output of SSLClose
*/
DLLEXPORT int32_t AppleCryptoNative_SslShutdown(SSLContextRef sslContext);

/*
Retrieve the TLS Protocol Version (e.g. TLS1.2) for the current session.

Returns the output of SSLGetNegotiatedProtocolVersion.

Output:
pProtocol: Receives the protocol ID. PAL_SslProtocol_None is issued on error or an unknown mapping.
*/
DLLEXPORT int32_t AppleCryptoNative_SslGetProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol* pProtocol);

/*
Retrieve the TLS Cipher Suite which was negotiated for the current session.

Returns the output of SSLGetNegotiatedCipher.

Output:
pProtocol: The TLS CipherSuite value (from the RFC), e.g. ((uint16_t)0xC030) for
TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
*/
DLLEXPORT int32_t AppleCryptoNative_SslGetCipherSuite(SSLContextRef sslContext, uint16_t* pCipherSuiteOut);

/*
Sets enabled cipher suites for the current session.

Returns the output of SSLSetEnabledCiphers.
*/
DLLEXPORT int32_t AppleCryptoNative_SslSetEnabledCipherSuites(SSLContextRef sslContext, const uint32_t* cipherSuites, int32_t numCipherSuites);
