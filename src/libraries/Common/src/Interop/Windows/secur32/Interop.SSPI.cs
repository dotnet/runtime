// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Secur32
    {
        // TODO (Issue #3114): Throughout the entire file: replace with OS names from <sspi.h> and <schannel.h>
        internal const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001;

        internal const int SEC_I_RENEGOTIATE = 0x90321;

        internal const int SECPKG_NEGOTIATION_COMPLETE = 0;
        internal const int SECPKG_NEGOTIATION_OPTIMISTIC = 1;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SSPIHandle
        {
            private IntPtr _handleHi;
            private IntPtr _handleLo;

            public bool IsZero
            {
                get { return _handleHi == IntPtr.Zero && HandleLo1 == IntPtr.Zero; }
            }

            public IntPtr HandleLo1
            {
                get
                {
                    return _handleLo;
                }

                set
                {
                    _handleLo = value;
                }
            }

            internal void SetToInvalid()
            {
                _handleHi = IntPtr.Zero;
                HandleLo1 = IntPtr.Zero;
            }

            public override string ToString()
            {
                { return _handleHi.ToString("x") + ":" + HandleLo1.ToString("x"); }
            }
        }

        internal enum ContextAttribute
        {
            Sizes = 0x00,
            Names = 0x01,
            Lifespan = 0x02,
            DceInfo = 0x03,
            StreamSizes = 0x04,
            //KeyInfo             = 0x05, must not be used, see ConnectionInfo instead
            Authority = 0x06,
            // SECPKG_ATTR_PROTO_INFO          = 7,
            // SECPKG_ATTR_PASSWORD_EXPIRY     = 8,
            // SECPKG_ATTR_SESSION_KEY         = 9,
            PackageInfo = 0x0A,
            // SECPKG_ATTR_USER_FLAGS          = 11,
            NegotiationInfo = 0x0C,
            // SECPKG_ATTR_NATIVE_NAMES        = 13,
            // SECPKG_ATTR_FLAGS               = 14,
            // SECPKG_ATTR_USE_VALIDATED       = 15,
            // SECPKG_ATTR_CREDENTIAL_NAME     = 16,
            // SECPKG_ATTR_TARGET_INFORMATION  = 17,
            // SECPKG_ATTR_ACCESS_TOKEN        = 18,
            // SECPKG_ATTR_TARGET              = 19,
            // SECPKG_ATTR_AUTHENTICATION_ID   = 20,
            UniqueBindings = 0x19,
            EndpointBindings = 0x1A,
            ClientSpecifiedSpn = 0x1B, // SECPKG_ATTR_CLIENT_SPECIFIED_TARGET = 27
            RemoteCertificate = 0x53,
            LocalCertificate = 0x54,
            RootStore = 0x55,
            IssuerListInfoEx = 0x59,
            ConnectionInfo = 0x5A,
            // SECPKG_ATTR_EAP_KEY_BLOCK        0x5b   // returns SecPkgContext_EapKeyBlock  
            // SECPKG_ATTR_MAPPED_CRED_ATTR     0x5c   // returns SecPkgContext_MappedCredAttr  
            // SECPKG_ATTR_SESSION_INFO         0x5d   // returns SecPkgContext_SessionInfo  
            // SECPKG_ATTR_APP_DATA             0x5e   // sets/returns SecPkgContext_SessionAppData  
            // SECPKG_ATTR_REMOTE_CERTIFICATES  0x5F   // returns SecPkgContext_Certificates  
            // SECPKG_ATTR_CLIENT_CERT_POLICY   0x60   // sets    SecPkgCred_ClientCertCtlPolicy  
            // SECPKG_ATTR_CC_POLICY_RESULT     0x61   // returns SecPkgContext_ClientCertPolicyResult  
            // SECPKG_ATTR_USE_NCRYPT           0x62   // Sets the CRED_FLAG_USE_NCRYPT_PROVIDER FLAG on cred group  
            // SECPKG_ATTR_LOCAL_CERT_INFO      0x63   // returns SecPkgContext_CertInfo  
            // SECPKG_ATTR_CIPHER_INFO          0x64   // returns new CNG SecPkgContext_CipherInfo  
            // SECPKG_ATTR_EAP_PRF_INFO         0x65   // sets    SecPkgContext_EapPrfInfo  
            // SECPKG_ATTR_SUPPORTED_SIGNATURES 0x66   // returns SecPkgContext_SupportedSignatures  
            // SECPKG_ATTR_REMOTE_CERT_CHAIN    0x67   // returns PCCERT_CONTEXT  
            UiInfo = 0x68, // sets SEcPkgContext_UiInfo  
        }

        // #define ISC_REQ_DELEGATE                0x00000001
        // #define ISC_REQ_MUTUAL_AUTH             0x00000002
        // #define ISC_REQ_REPLAY_DETECT           0x00000004
        // #define ISC_REQ_SEQUENCE_DETECT         0x00000008
        // #define ISC_REQ_CONFIDENTIALITY         0x00000010
        // #define ISC_REQ_USE_SESSION_KEY         0x00000020
        // #define ISC_REQ_PROMPT_FOR_CREDS        0x00000040
        // #define ISC_REQ_USE_SUPPLIED_CREDS      0x00000080
        // #define ISC_REQ_ALLOCATE_MEMORY         0x00000100
        // #define ISC_REQ_USE_DCE_STYLE           0x00000200
        // #define ISC_REQ_DATAGRAM                0x00000400
        // #define ISC_REQ_CONNECTION              0x00000800
        // #define ISC_REQ_CALL_LEVEL              0x00001000
        // #define ISC_REQ_FRAGMENT_SUPPLIED       0x00002000
        // #define ISC_REQ_EXTENDED_ERROR          0x00004000
        // #define ISC_REQ_STREAM                  0x00008000
        // #define ISC_REQ_INTEGRITY               0x00010000
        // #define ISC_REQ_IDENTIFY                0x00020000
        // #define ISC_REQ_NULL_SESSION            0x00040000
        // #define ISC_REQ_MANUAL_CRED_VALIDATION  0x00080000
        // #define ISC_REQ_RESERVED1               0x00100000
        // #define ISC_REQ_FRAGMENT_TO_FIT         0x00200000
        // #define ISC_REQ_HTTP                    0x10000000
        // Win7 SP1 +
        // #define ISC_REQ_UNVERIFIED_TARGET_NAME  0x20000000  

        // #define ASC_REQ_DELEGATE                0x00000001
        // #define ASC_REQ_MUTUAL_AUTH             0x00000002
        // #define ASC_REQ_REPLAY_DETECT           0x00000004
        // #define ASC_REQ_SEQUENCE_DETECT         0x00000008
        // #define ASC_REQ_CONFIDENTIALITY         0x00000010
        // #define ASC_REQ_USE_SESSION_KEY         0x00000020
        // #define ASC_REQ_ALLOCATE_MEMORY         0x00000100
        // #define ASC_REQ_USE_DCE_STYLE           0x00000200
        // #define ASC_REQ_DATAGRAM                0x00000400
        // #define ASC_REQ_CONNECTION              0x00000800
        // #define ASC_REQ_CALL_LEVEL              0x00001000
        // #define ASC_REQ_EXTENDED_ERROR          0x00008000
        // #define ASC_REQ_STREAM                  0x00010000
        // #define ASC_REQ_INTEGRITY               0x00020000
        // #define ASC_REQ_LICENSING               0x00040000
        // #define ASC_REQ_IDENTIFY                0x00080000
        // #define ASC_REQ_ALLOW_NULL_SESSION      0x00100000
        // #define ASC_REQ_ALLOW_NON_USER_LOGONS   0x00200000
        // #define ASC_REQ_ALLOW_CONTEXT_REPLAY    0x00400000
        // #define ASC_REQ_FRAGMENT_TO_FIT         0x00800000
        // #define ASC_REQ_FRAGMENT_SUPPLIED       0x00002000
        // #define ASC_REQ_NO_TOKEN                0x01000000
        // #define ASC_REQ_HTTP                    0x10000000

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
            Network = 0x00,
            Native = 0x10,
        }

        internal enum CredentialUse
        {
            Inbound = 0x1,
            Outbound = 0x2,
            Both = 0x3,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _CERT_CHAIN_ELEMENT
        {
            public uint cbSize;
            public IntPtr pCertContext;
            // Since this structure is allocated by unmanaged code, we can
            // omit the fileds below since we don't need to access them
            // CERT_TRUST_STATUS   TrustStatus;
            // IntPtr                pRevocationInfo;
            // IntPtr                pIssuanceUsage;
            // IntPtr                pApplicationUsage;
        }

        // SecPkgContext_IssuerListInfoEx
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IssuerListInfoEx
        {
            public SafeHandle aIssuers;
            public uint cIssuers;

            public unsafe IssuerListInfoEx(SafeHandle handle, byte[] nativeBuffer)
            {
                aIssuers = handle;
                fixed (byte* voidPtr = nativeBuffer)
                {
                    // TODO (Issue #3114): Properly marshal the struct instead of assuming no padding.
                    cIssuers = *((uint*)(voidPtr + IntPtr.Size));
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecureCredential
        {
            /*
            typedef struct _SCHANNEL_CRED
            {
                DWORD           dwVersion;      // always SCHANNEL_CRED_VERSION
                DWORD           cCreds;
                PCCERT_CONTEXT *paCred;
                HCERTSTORE      hRootStore;

                DWORD           cMappers;
                struct _HMAPPER **aphMappers;

                DWORD           cSupportedAlgs;
                ALG_ID *        palgSupportedAlgs;

                DWORD           grbitEnabledProtocols;
                DWORD           dwMinimumCipherStrength;
                DWORD           dwMaximumCipherStrength;
                DWORD           dwSessionLifespan;
                DWORD           dwFlags;
                DWORD           reserved;
            } SCHANNEL_CRED, *PSCHANNEL_CRED;
            */

            public const int CurrentVersion = 0x4;

            public int version;
            public int cCreds;

            // ptr to an array of pointers
            // There is a hack done with this field.  AcquireCredentialsHandle requires an array of
            // certificate handles; we only ever use one.  In order to avoid pinning a one element array,
            // we copy this value onto the stack, create a pointer on the stack to the copied value,
            // and replace this field with the pointer, during the call to AcquireCredentialsHandle.
            // Then we fix it up afterwards.  Fine as long as all the SSPI credentials are not
            // supposed to be threadsafe.
            public IntPtr certContextArray;

            public IntPtr rootStore;               // == always null, OTHERWISE NOT RELIABLE
            public int cMappers;
            public IntPtr phMappers;               // == always null, OTHERWISE NOT RELIABLE
            public int cSupportedAlgs;
            public IntPtr palgSupportedAlgs;       // == always null, OTHERWISE NOT RELIABLE
            public int grbitEnabledProtocols;
            public int dwMinimumCipherStrength;
            public int dwMaximumCipherStrength;
            public int dwSessionLifespan;
            public SecureCredential.Flags dwFlags;
            public int reserved;

            [Flags]
            public enum Flags
            {
                Zero = 0,
                NoSystemMapper = 0x02,
                NoNameCheck = 0x04,
                ValidateManual = 0x08,
                NoDefaultCred = 0x10,
                ValidateAuto = 0x20,
                UseStrongCrypto = 0x00400000,
            }
        } // SecureCredential

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SecurityBufferStruct
        {
            public int count;
            public SecurityBufferType type;
            public IntPtr token;

            public static readonly int Size = sizeof(SecurityBufferStruct);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe class SecurityBufferDescriptor
        {
            /*
            typedef struct _SecBufferDesc {
                ULONG        ulVersion;
                ULONG        cBuffers;
                PSecBuffer   pBuffers;
            } SecBufferDesc, * PSecBufferDesc;
            */
            public readonly int Version;
            public readonly int Count;
            public void* UnmanagedPointer;

            public SecurityBufferDescriptor(int count)
            {
                Version = 0;
                Count = count;
                UnmanagedPointer = null;
            }
        } // SecurityBufferDescriptor

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct AuthIdentity
        {
            // see SEC_WINNT_AUTH_IDENTITY_W
            internal string UserName;
            internal int UserNameLength;
            internal string Domain;
            internal int DomainLength;
            internal string Password;
            internal int PasswordLength;
            internal int Flags;
        }

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static extern int EncryptMessage(
              ref SSPIHandle contextHandle,
              [In] uint qualityOfProtection,
              [In, Out] SecurityBufferDescriptor inputOutput,
              [In] uint sequenceNumber
              );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static unsafe extern int DecryptMessage(
              [In] ref SSPIHandle contextHandle,
              [In, Out] SecurityBufferDescriptor inputOutput,
              [In] uint sequenceNumber,
                   uint* qualityOfProtection
              );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static extern int QuerySecurityContextToken(
            ref SSPIHandle phContext,
            [Out] out SecurityContextTokenHandle handle);

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static extern int FreeContextBuffer(
            [In] IntPtr contextBuffer);

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static extern int FreeCredentialsHandle(
              ref SSPIHandle handlePtr
              );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static extern int DeleteSecurityContext(
              ref SSPIHandle handlePtr
              );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int AcceptSecurityContext(
                  ref SSPIHandle credentialHandle,
                  [In] void* inContextPtr,
                  [In] SecurityBufferDescriptor inputBuffer,
                  [In] ContextFlags inFlags,
                  [In] Endianness endianness,
                  ref SSPIHandle outContextPtr,
                  [In, Out] SecurityBufferDescriptor outputBuffer,
                  [In, Out] ref ContextFlags attributes,
                  out long timeStamp
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int QueryContextAttributesW(
            ref SSPIHandle contextHandle,
            [In] ContextAttribute attribute,
            [In] void* buffer);

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int SetContextAttributesW(
            ref SSPIHandle contextHandle,
            [In] ContextAttribute attribute,
            [In] byte[] buffer,
            [In] int bufferSize);

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal static extern int EnumerateSecurityPackagesW(
            [Out] out int pkgnum,
            [Out] out SafeFreeContextBuffer_SECURITY handle);

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] ref AuthIdentity authdata,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref SSPIHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] IntPtr zero,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref SSPIHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] SafeSspiAuthDataHandle authdata,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref SSPIHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] ref SecureCredential authData,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref SSPIHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int InitializeSecurityContextW(
                  ref SSPIHandle credentialHandle,
                  [In] void* inContextPtr,
                  [In] byte* targetName,
                  [In] ContextFlags inFlags,
                  [In] int reservedI,
                  [In] Endianness endianness,
                  [In] SecurityBufferDescriptor inputBuffer,
                  [In] int reservedII,
                  ref SSPIHandle outContextPtr,
                  [In, Out] SecurityBufferDescriptor outputBuffer,
                  [In, Out] ref ContextFlags attributes,
                  out long timeStamp
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int CompleteAuthToken(
                  [In] void* inContextPtr,
                  [In, Out] SecurityBufferDescriptor inputBuffers
                  );

        [DllImport(Interop.Libraries.Secur32, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern SecurityStatus SspiFreeAuthIdentity(
            [In] IntPtr authData);
    }
}
