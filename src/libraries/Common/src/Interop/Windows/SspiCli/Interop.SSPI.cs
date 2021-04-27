// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class SspiCli
    {
        internal const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001;

        internal const int SEC_I_RENEGOTIATE = 0x90321;

        internal const int SECPKG_NEGOTIATION_COMPLETE = 0;
        internal const int SECPKG_NEGOTIATION_OPTIMISTIC = 1;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct CredHandle
        {
            private IntPtr dwLower;
            private IntPtr dwUpper;

            public bool IsZero
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return dwLower == IntPtr.Zero && dwUpper == IntPtr.Zero;
                }
            }

            internal void SetToInvalid()
            {
                dwLower = IntPtr.Zero;
                dwUpper = IntPtr.Zero;
            }

            public override string ToString()
            {
                { return dwLower.ToString("x") + ":" + dwUpper.ToString("x"); }
            }
        }

        internal enum ContextAttribute
        {
            // sspi.h
            SECPKG_ATTR_SIZES = 0,
            SECPKG_ATTR_NAMES = 1,
            SECPKG_ATTR_LIFESPAN = 2,
            SECPKG_ATTR_DCE_INFO = 3,
            SECPKG_ATTR_STREAM_SIZES = 4,
            SECPKG_ATTR_AUTHORITY = 6,
            SECPKG_ATTR_PACKAGE_INFO = 10,
            SECPKG_ATTR_NEGOTIATION_INFO = 12,
            SECPKG_ATTR_UNIQUE_BINDINGS = 25,
            SECPKG_ATTR_ENDPOINT_BINDINGS = 26,
            SECPKG_ATTR_CLIENT_SPECIFIED_TARGET = 27,
            SECPKG_ATTR_APPLICATION_PROTOCOL = 35,

            // minschannel.h
            SECPKG_ATTR_REMOTE_CERT_CONTEXT = 0x53,    // returns PCCERT_CONTEXT
            SECPKG_ATTR_LOCAL_CERT_CONTEXT = 0x54,     // returns PCCERT_CONTEXT
            SECPKG_ATTR_ROOT_STORE = 0x55,             // returns HCERTCONTEXT to the root store
            SECPKG_ATTR_ISSUER_LIST_EX = 0x59,         // returns SecPkgContext_IssuerListInfoEx
            SECPKG_ATTR_CONNECTION_INFO = 0x5A,        // returns SecPkgContext_ConnectionInfo
            SECPKG_ATTR_CIPHER_INFO = 0x64,            // returns SecPkgContext_CipherInfo
            SECPKG_ATTR_UI_INFO = 0x68, // sets SEcPkgContext_UiInfo
        }

        // These values are defined within sspi.h as ISC_REQ_*, ISC_RET_*, ASC_REQ_* and ASC_RET_*.
        [Flags]
        internal enum ContextFlags
        {
            Zero = 0,
            // The server in the transport application can
            // build new security contexts impersonating the
            // client that will be accepted by other servers
            // as the client's contexts.
            Delegate = 0x00000001,
            // The communicating parties must authenticate
            // their identities to each other. Without MutualAuth,
            // the client authenticates its identity to the server.
            // With MutualAuth, the server also must authenticate
            // its identity to the client.
            MutualAuth = 0x00000002,
            // The security package detects replayed packets and
            // notifies the caller if a packet has been replayed.
            // The use of this flag implies all of the conditions
            // specified by the Integrity flag.
            ReplayDetect = 0x00000004,
            // The context must be allowed to detect out-of-order
            // delivery of packets later through the message support
            // functions. Use of this flag implies all of the
            // conditions specified by the Integrity flag.
            SequenceDetect = 0x00000008,
            // The context must protect data while in transit.
            // Confidentiality is supported for NTLM with Microsoft
            // Windows NT version 4.0, SP4 and later and with the
            // Kerberos protocol in Microsoft Windows 2000 and later.
            Confidentiality = 0x00000010,
            UseSessionKey = 0x00000020,
            AllocateMemory = 0x00000100,

            // Connection semantics must be used.
            Connection = 0x00000800,

            // Client applications requiring extended error messages specify the
            // ISC_REQ_EXTENDED_ERROR flag when calling the InitializeSecurityContext
            // Server applications requiring extended error messages set
            // the ASC_REQ_EXTENDED_ERROR flag when calling AcceptSecurityContext.
            InitExtendedError = 0x00004000,
            AcceptExtendedError = 0x00008000,
            // A transport application requests stream semantics
            // by setting the ISC_REQ_STREAM and ASC_REQ_STREAM
            // flags in the calls to the InitializeSecurityContext
            // and AcceptSecurityContext functions
            InitStream = 0x00008000,
            AcceptStream = 0x00010000,
            // Buffer integrity can be verified; however, replayed
            // and out-of-sequence messages will not be detected
            InitIntegrity = 0x00010000,       // ISC_REQ_INTEGRITY
            AcceptIntegrity = 0x00020000,       // ASC_REQ_INTEGRITY

            InitManualCredValidation = 0x00080000,   // ISC_REQ_MANUAL_CRED_VALIDATION
            InitUseSuppliedCreds = 0x00000080,   // ISC_REQ_USE_SUPPLIED_CREDS
            InitIdentify = 0x00020000,   // ISC_REQ_IDENTIFY
            AcceptIdentify = 0x00080000,   // ASC_REQ_IDENTIFY

            ProxyBindings = 0x04000000,   // ASC_REQ_PROXY_BINDINGS
            AllowMissingBindings = 0x10000000,   // ASC_REQ_ALLOW_MISSING_BINDINGS

            UnverifiedTargetName = 0x20000000,   // ISC_REQ_UNVERIFIED_TARGET_NAME
        }

        internal enum Endianness
        {
            SECURITY_NETWORK_DREP = 0x00,
            SECURITY_NATIVE_DREP = 0x10,
        }

        internal enum CredentialUse
        {
            SECPKG_CRED_INBOUND = 0x1,
            SECPKG_CRED_OUTBOUND = 0x2,
            SECPKG_CRED_BOTH = 0x3,
        }

        // wincrypt.h
        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_CHAIN_ELEMENT
        {
            public uint cbSize;
            public IntPtr pCertContext;
            // Since this structure is allocated by unmanaged code, we can
            // omit the fields below since we don't need to access them
            // CERT_TRUST_STATUS   TrustStatus;
            // IntPtr                pRevocationInfo;
            // IntPtr                pIssuanceUsage;
            // IntPtr                pApplicationUsage;
        }

        // schannel.h
        [StructLayout(LayoutKind.Sequential)]
        internal struct SecPkgContext_IssuerListInfoEx
        {
            public IntPtr aIssuers;
            public uint cIssuers;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SCHANNEL_CRED
        {
            public const int CurrentVersion = 0x4;

            public int dwVersion;
            public int cCreds;

            public Crypt32.CERT_CONTEXT** paCred;

            public IntPtr hRootStore;               // == always null, OTHERWISE NOT RELIABLE
            public int cMappers;
            public IntPtr aphMappers;               // == always null, OTHERWISE NOT RELIABLE
            public int cSupportedAlgs;
            public IntPtr palgSupportedAlgs;       // == always null, OTHERWISE NOT RELIABLE
            public int grbitEnabledProtocols;
            public int dwMinimumCipherStrength;
            public int dwMaximumCipherStrength;
            public int dwSessionLifespan;
            public SCHANNEL_CRED.Flags dwFlags;
            public int reserved;

            [Flags]
            public enum Flags
            {
                Zero = 0,
                SCH_CRED_NO_SYSTEM_MAPPER = 0x02,
                SCH_CRED_NO_SERVERNAME_CHECK = 0x04,
                SCH_CRED_MANUAL_CRED_VALIDATION = 0x08,
                SCH_CRED_NO_DEFAULT_CREDS = 0x10,
                SCH_CRED_AUTO_CRED_VALIDATION = 0x20,
                SCH_SEND_AUX_RECORD = 0x00200000,
                SCH_USE_STRONG_CRYPTO = 0x00400000,
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SCH_CREDENTIALS
        {
            public const int CurrentVersion = 0x5;

            public int dwVersion;
            public int dwCredformat;
            public int cCreds;

            public Crypt32.CERT_CONTEXT** paCred;

            public IntPtr hRootStore;               // == always null, OTHERWISE NOT RELIABLE
            public int cMappers;
            public IntPtr aphMappers;               // == always null, OTHERWISE NOT RELIABLE

            public int dwSessionLifespan;
            public SCH_CREDENTIALS.Flags dwFlags;
            public int cTlsParameters;
            public TLS_PARAMETERS* pTlsParameters;

            [Flags]
            public enum Flags
            {
                Zero = 0,
                SCH_CRED_NO_SYSTEM_MAPPER = 0x02,
                SCH_CRED_NO_SERVERNAME_CHECK = 0x04,
                SCH_CRED_MANUAL_CRED_VALIDATION = 0x08,
                SCH_CRED_NO_DEFAULT_CREDS = 0x10,
                SCH_CRED_AUTO_CRED_VALIDATION = 0x20,
                SCH_CRED_USE_DEFAULT_CREDS = 0x40,
                SCH_DISABLE_RECONNECTS = 0x80,
                SCH_CRED_REVOCATION_CHECK_END_CERT = 0x100,
                SCH_CRED_REVOCATION_CHECK_CHAIN = 0x200,
                SCH_CRED_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x400,
                SCH_CRED_IGNORE_NO_REVOCATION_CHECK = 0x800,
                SCH_CRED_IGNORE_REVOCATION_OFFLINE = 0x1000,
                SCH_CRED_CACHE_ONLY_URL_RETRIEVAL_ON_CREATE = 0x2000,
                SCH_SEND_ROOT_CERT = 0x40000,
                SCH_SEND_AUX_RECORD =   0x00200000,
                SCH_USE_STRONG_CRYPTO = 0x00400000,
                SCH_USE_PRESHAREDKEY_ONLY = 0x800000,
                SCH_ALLOW_NULL_ENCRYPTION = 0x02000000,
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct TLS_PARAMETERS
        {
            public int cAlpnIds;                        // Valid for server applications only. Must be zero otherwise. Number of ALPN IDs in rgstrAlpnIds; set to 0 if applies to all.
            public IntPtr rgstrAlpnIds;                 // Valid for server applications only. Must be NULL otherwise. Array of ALPN IDs that the following settings apply to; set to NULL if applies to all.
            public uint grbitDisabledProtocols;         // List protocols you DO NOT want negotiated.
            public int cDisabledCrypto;                 // Number of CRYPTO_SETTINGS structures; set to 0 if there are none.
            public CRYPTO_SETTINGS* pDisabledCrypto;    // Array of CRYPTO_SETTINGS structures; set to NULL if there are none;
            public TLS_PARAMETERS.Flags dwFlags;        // Optional flags to pass; set to 0 if there are none.

            [Flags]
            public enum Flags
            {
                Zero = 0,
                TLS_PARAMS_OPTIONAL = 0x01,     // Valid for server applications only. Must be zero otherwise.
                                                // TLS_PARAMETERS that will only be honored if they do not cause this server to terminate the handshake.
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CRYPTO_SETTINGS
        {
            public TlsAlgorithmUsage eAlgorithmUsage;   // How this algorithm is being used.
            public UNICODE_STRING* strCngAlgId;         // CNG algorithm identifier.
            public int cChainingModes;                  // Set to 0 if CNG algorithm does not have a chaining mode.
            public UNICODE_STRING* rgstrChainingModes;  // Set to NULL if CNG algorithm does not have a chaining mode.
            public int dwMinBitLength;                  // Minimum bit length for the specified CNG algorithm. Set to 0 if not defined or CNG algorithm implies bit length.
            public int dwMaxBitLength;                  // Maximum bit length for the specified CNG algorithm. Set to 0 if not defined or CNG algorithm implies bit length.

            public enum TlsAlgorithmUsage
            {
                TlsParametersCngAlgUsageKeyExchange,    // Key exchange algorithm. RSA, ECHDE, DHE, etc.
                TlsParametersCngAlgUsageSignature,      // Signature algorithm. RSA, DSA, ECDSA, etc.
                TlsParametersCngAlgUsageCipher,         // Encryption algorithm. AES, DES, RC4, etc.
                TlsParametersCngAlgUsageDigest,         // Digest of cipher suite. SHA1, SHA256, SHA384, etc.
                TlsParametersCngAlgUsageCertSig         // Signature and/or hash used to sign certificate. RSA, DSA, ECDSA, SHA1, SHA256, etc.
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecBuffer
        {
            public int cbBuffer;
            public SecurityBufferType BufferType;
            public IntPtr pvBuffer;

            public static readonly unsafe int Size = sizeof(SecBuffer);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SecBufferDesc
        {
            public readonly int ulVersion;
            public readonly int cBuffers;
            public void* pBuffers;

            public SecBufferDesc(int count)
            {
                ulVersion = 0;
                cBuffers = count;
                pBuffers = null;
            }
        }

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int EncryptMessage(
              ref CredHandle contextHandle,
              [In] uint qualityOfProtection,
              [In, Out] ref SecBufferDesc inputOutput,
              [In] uint sequenceNumber
              );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe int DecryptMessage(
              [In] ref CredHandle contextHandle,
              [In, Out] ref SecBufferDesc inputOutput,
              [In] uint sequenceNumber,
                   uint* qualityOfProtection
              );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int QuerySecurityContextToken(
            ref CredHandle phContext,
            [Out] out SecurityContextTokenHandle handle);

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int FreeContextBuffer(
            [In] IntPtr contextBuffer);

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int FreeCredentialsHandle(
              ref CredHandle handlePtr
              );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int DeleteSecurityContext(
              ref CredHandle handlePtr
              );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe int AcceptSecurityContext(
                  ref CredHandle credentialHandle,
                  [In] void* inContextPtr,
                  [In] SecBufferDesc* inputBuffer,
                  [In] ContextFlags inFlags,
                  [In] Endianness endianness,
                  ref CredHandle outContextPtr,
                  [In, Out] ref SecBufferDesc outputBuffer,
                  [In, Out] ref ContextFlags attributes,
                  out long timeStamp
                  );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe int QueryContextAttributesW(
            ref CredHandle contextHandle,
            [In] ContextAttribute attribute,
            [In] void* buffer);

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int SetContextAttributesW(
            ref CredHandle contextHandle,
            [In] ContextAttribute attribute,
            [In] byte[] buffer,
            [In] int bufferSize);

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern int EnumerateSecurityPackagesW(
            [Out] out int pkgnum,
            [Out] out SafeFreeContextBuffer_SECURITY handle);

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int AcquireCredentialsHandleW(
                  [In] string? principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] IntPtr zero,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int AcquireCredentialsHandleW(
                  [In] string? principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] SafeSspiAuthDataHandle authdata,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int AcquireCredentialsHandleW(
                  [In] string? principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] SCHANNEL_CRED* authData,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int AcquireCredentialsHandleW(
          [In] string? principal,
          [In] string moduleName,
          [In] int usage,
          [In] void* logonID,
          [In] SCH_CREDENTIALS* authData,
          [In] void* keyCallback,
          [In] void* keyArgument,
          ref CredHandle handlePtr,
          [Out] out long timeStamp
          );


        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe int InitializeSecurityContextW(
                  ref CredHandle credentialHandle,
                  [In] void* inContextPtr,
                  [In] byte* targetName,
                  [In] ContextFlags inFlags,
                  [In] int reservedI,
                  [In] Endianness endianness,
                  [In] SecBufferDesc* inputBuffer,
                  [In] int reservedII,
                  ref CredHandle outContextPtr,
                  [In, Out] ref SecBufferDesc outputBuffer,
                  [In, Out] ref ContextFlags attributes,
                  out long timeStamp
                  );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe int CompleteAuthToken(
                  [In] void* inContextPtr,
                  [In, Out] ref SecBufferDesc inputBuffers
                  );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe int ApplyControlToken(
          [In] void* inContextPtr,
          [In, Out] ref SecBufferDesc inputBuffers
          );

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, SetLastError = true)]
        internal static extern SECURITY_STATUS SspiFreeAuthIdentity(
            [In] IntPtr authData);

        [DllImport(Interop.Libraries.SspiCli, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SECURITY_STATUS SspiEncodeStringsAsAuthIdentity(
            [In] string userName,
            [In] string domainName,
            [In] string password,
            [Out] out SafeSspiAuthDataHandle authData);
    }
}
