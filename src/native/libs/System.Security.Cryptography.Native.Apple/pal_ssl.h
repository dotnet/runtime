// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include <pal_ssl_types.h>
#include <Security/Security.h>
#include <Security/SecureTransport.h>

enum
{
    PAL_TlsHandshakeState_Unknown = 0,
    PAL_TlsHandshakeState_Complete = 1,
    PAL_TlsHandshakeState_WouldBlock = 2,
    PAL_TlsHandshakeState_ServerAuthCompleted = 3,
    PAL_TlsHandshakeState_ClientAuthCompleted = 4,
    PAL_TlsHandshakeState_ClientCertRequested = 5,
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

/*
Create an SSL context, for the Server or Client role as determined by isServer.

Returns NULL if an invalid boolean is given for isServer, an SSLContextRef otherwise.
*/
PALEXPORT SSLContextRef AppleCryptoNative_SslCreateContext(int32_t isServer);

/*
Data that is used to uniquely identify an SSL session.

Returns the result of SSLSetConnection
*/
PALEXPORT int32_t AppleCryptoNative_SslSetConnection(SSLContextRef sslContext, SSLConnectionRef sslConnection);

/*
Indicate that an SSL Context (in server mode) should allow a client to present a mutual auth cert.

Returns The result of SSLSetClientSideAuthenticate
*/
PALEXPORT int32_t AppleCryptoNative_SslSetAcceptClientCert(SSLContextRef sslContext);

/*
Assign a minimum to the TLS protocol version for this connection.

Returns the output of SSLSetProtocolVersionMin
*/
PALEXPORT int32_t AppleCryptoNative_SslSetMinProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol);

/*
Assign a maximum to the TLS protocol version for this connection.

Returns the output of SSLSetProtocolVersionMax
*/
PALEXPORT int32_t AppleCryptoNative_SslSetMaxProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol sslProtocol);

/*
Get the SecTrustRef from the SSL context which represents the certificte chain.

Returns 1 on success, 0 on failure, and other values on invalid state.

Output:
pChainOut: Receives the SecTrustRef representing the populated chain
pOSStatus: Receives the value returned by SSLCopyPeerTrust
*/
PALEXPORT int32_t
AppleCryptoNative_SslCopyCertChain(SSLContextRef sslContext, SecTrustRef* pChainOut, int32_t* pOSStatus);

/*
Get the list of DN values for acceptable issuers for this connection.

Returns 1 on success, 0 on OSStatus-error, other values for invalid state.

Output:
pChainOut: Receives an array of CFDataRef values representing the encoded X500 DistinguishedName
values sent by the server.

pOSStatus: Receives the output of SSLCopyDistinguishedNames.
*/
PALEXPORT int32_t
AppleCryptoNative_SslCopyCADistinguishedNames(SSLContextRef sslContext, CFArrayRef* pArrayOut, int32_t* pOSStatus);

/*
Sets the policy of whether or not to break when a server identity has been presented.

Returns 1 on success, 0 on failure, other values on invalid state.

Output:
pOSStatus: Receives the value returned by SSLSetSessionOption
*/
PALEXPORT int32_t
AppleCryptoNative_SslSetBreakOnServerAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus);

/*
Sets the policy of whether or not to break when certificate request was received on client.

Returns 1 on success, 0 on failure, other values on invalid state.

Output:
pOSStatus: Receives the value returned by SSLSetSessionOption
*/
PALEXPORT int32_t
AppleCryptoNative_SslSetBreakOnCertRequested(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus);

/*
Sets the policy of whether or not to break when a client identity has been presented.

Returns 1 on success, 0 on failure, other values on invalid state.

Output:
pOSStatus: Receives the value returned by SSLSetSessionOption
*/
PALEXPORT int32_t
AppleCryptoNative_SslSetBreakOnClientAuth(SSLContextRef sslContext, int32_t setBreak, int32_t* pOSStatus);

/*
Set the certificate chain for the ServerHello or ClientHello message.

certRefs should be an array of [ SecIdentityRef, SecCertificateRef* ], the 0 element being the
public/private pair for this entity, and all subsequent elements being the public element of an
intermediate (non-root) certificate.

Returns the output of SSLSetCertificate
*/
PALEXPORT int32_t AppleCryptoNative_SslSetCertificate(SSLContextRef sslContext, CFArrayRef certRefs);

/*
Set the target hostname for SNI. pszTargetName must already be converted for IDNA if required.

Returns 1 on success, 0 on failure, other values for invalid state.

Output:
pOSStatus: Receives the value for SSLSetPeerDomainName
*/
PALEXPORT int32_t AppleCryptoNative_SslSetTargetName(SSLContextRef sslContext,
                                                     const char* pszTargetName,
                                                     int32_t cbTargetName,
                                                     int32_t* pOSStatus);

/*
Set list of application protocols for ClientHello.

Returns 1 on success, 0 on failure, other values for invalid state.

Output:
pOSStatus: Receives the value from SSLSetALPNData()
*/
PALEXPORT int32_t AppleCryptoNative_SSLSetALPNProtocols(SSLContextRef sslContext, CFArrayRef protocols, int32_t* pOSStatus);

/*
Get negotiated protocol value from ServerHello.
*/
PALEXPORT int32_t AppleCryptoNative_SslGetAlpnSelected(SSLContextRef sslContext, CFDataRef *protocol);

/*
Register the callbacks for reading and writing data to the SSL context.

Returns the output of SSLSetIOFuncs.
*/
PALEXPORT int32_t
AppleCryptoNative_SslSetIoCallbacks(SSLContextRef sslContext, SSLReadFunc readFunc, SSLWriteFunc writeFunc);

/*
Pump the TLS handshake.

Returns an indication of what state the error is in. Any negative number means an error occurred.
*/
PALEXPORT PAL_TlsHandshakeState AppleCryptoNative_SslHandshake(SSLContextRef sslContext);

/*
Take bufLen bytes of cleartext data from buf and encrypt/frame the data.
Processed data will then be sent into the write callback.

Returns a PAL_TlsIo code indicitating how to proceed.

Output:
bytesWritten: When any value other than PAL_TlsIo_Success is returned, receives the number of bytes
which were read from buf. On PAL_TlsIo_Success the parameter is not written through (but must still
not be NULL)
*/
PALEXPORT PAL_TlsIo
AppleCryptoNative_SslWrite(SSLContextRef sslContext, const uint8_t* buf, uint32_t bufLen, uint32_t* bytesWritten);

/*
Read up to bufLen bytes of framed/encrypted data from the connection into buf.
Unless a holdover from a previous incomplete read is present this will invoke the read callback
to get data from "the connection".

Returns a PAL_TlsIo code indicating how to proceed.

Output:
written: Receives the number of bytes written into buf
*/
PALEXPORT PAL_TlsIo
AppleCryptoNative_SslRead(SSLContextRef sslContext, uint8_t* buf, uint32_t bufLen, uint32_t* written);

/*
Check to see if the server identity certificate for this connection matches the requested hostname.

notBefore: Specify the EE/leaf certificate's notBefore value to prevent a false negative due to
the certificate being expired (or not yet valid).

Returns 1 on match, 0 on mismatch, any other value indicates an invalid state.
*/
PALEXPORT int32_t
AppleCryptoNative_SslIsHostnameMatch(SSLContextRef sslContext, CFStringRef cfHostname, CFDateRef notBefore, int32_t* pOSStatus);

/*
Generate a TLS Close alert to terminate the session.

Returns the output of SSLClose
*/
PALEXPORT int32_t AppleCryptoNative_SslShutdown(SSLContextRef sslContext);

/*
Retrieve the TLS Protocol Version (e.g. TLS1.2) for the current session.

Returns the output of SSLGetNegotiatedProtocolVersion.

Output:
pProtocol: Receives the protocol ID. PAL_SslProtocol_None is issued on error or an unknown mapping.
*/
PALEXPORT int32_t AppleCryptoNative_SslGetProtocolVersion(SSLContextRef sslContext, PAL_SslProtocol* pProtocol);

/*
Retrieve the TLS Cipher Suite which was negotiated for the current session.

Returns the output of SSLGetNegotiatedCipher.

Output:
pProtocol: The TLS CipherSuite value (from the RFC), e.g. ((uint16_t)0xC030) for
TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
*/
PALEXPORT int32_t AppleCryptoNative_SslGetCipherSuite(SSLContextRef sslContext, uint16_t* pCipherSuiteOut);

/*
Sets enabled cipher suites for the current session.

Returns the output of SSLSetEnabledCiphers.
*/
PALEXPORT int32_t AppleCryptoNative_SslSetEnabledCipherSuites(SSLContextRef sslContext, const uint32_t* cipherSuites, int32_t numCipherSuites);

/*
Adds one or more certificates to a server's list of certification authorities (CAs) acceptable for client authentication.

Returns the output of SSLSetCertificateAuthorities.
*/
PALEXPORT int32_t AppleCryptoNative_SslSetCertificateAuthorities(SSLContextRef sslContext, CFArrayRef certificates, int32_t replaceExisting);
