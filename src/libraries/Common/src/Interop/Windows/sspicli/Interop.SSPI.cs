// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Security;
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
                get { return dwLower == IntPtr.Zero && dwUpper == IntPtr.Zero; }
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

            // minschannel.h
            SECPKG_ATTR_REMOTE_CERT_CONTEXT = 0x53,    // returns PCCERT_CONTEXT
            SECPKG_ATTR_LOCAL_CERT_CONTEXT = 0x54,     // returns PCCERT_CONTEXT
            SECPKG_ATTR_ROOT_STORE = 0x55,             // returns HCERTCONTEXT to the root store
            SECPKG_ATTR_ISSUER_LIST_EX = 0x59,         // returns SecPkgContext_IssuerListInfoEx
            SECPKG_ATTR_CONNECTION_INFO = 0x5A,        // returns SecPkgContext_ConnectionInfo
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
        internal unsafe struct SecPkgContext_IssuerListInfoEx
        {
            public SafeHandle aIssuers;
            public uint cIssuers;

            public unsafe SecPkgContext_IssuerListInfoEx(SafeHandle handle, byte[] nativeBuffer)
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
        internal struct SCHANNEL_CRED
        {
            public const int CurrentVersion = 0x4;

            public int dwVersion;
            public int cCreds;

            // ptr to an array of pointers
            // There is a hack done with this field.  AcquireCredentialsHandle requires an array of
            // certificate handles; we only ever use one.  In order to avoid pinning a one element array,
            // we copy this value onto the stack, create a pointer on the stack to the copied value,
            // and replace this field with the pointer, during the call to AcquireCredentialsHandle.
            // Then we fix it up afterwards.  Fine as long as all the SSPI credentials are not
            // supposed to be threadsafe.
            public IntPtr paCred;

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
        internal unsafe struct SecBuffer
        {
            public int cbBuffer;
            public SecurityBufferType BufferType;
            public IntPtr pvBuffer;

            public static readonly int Size = sizeof(SecBuffer);
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SEC_WINNT_AUTH_IDENTITY_W
        {
            internal string User;
            internal int UserLength;
            internal string Domain;
            internal int DomainLength;
            internal string Password;
            internal int PasswordLength;
            internal int Flags;
        }

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static extern int EncryptMessage(
              ref CredHandle contextHandle,
              [In] uint qualityOfProtection,
              [In, Out] ref SecBufferDesc inputOutput,
              [In] uint sequenceNumber
              );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static unsafe extern int DecryptMessage(
              [In] ref CredHandle contextHandle,
              [In, Out] ref SecBufferDesc inputOutput,
              [In] uint sequenceNumber,
                   uint* qualityOfProtection
              );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static extern int QuerySecurityContextToken(
            ref CredHandle phContext,
            [Out] out SecurityContextTokenHandle handle);

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static extern int FreeContextBuffer(
            [In] IntPtr contextBuffer);

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static extern int FreeCredentialsHandle(
              ref CredHandle handlePtr
              );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static extern int DeleteSecurityContext(
              ref CredHandle handlePtr
              );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int AcceptSecurityContext(
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

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int QueryContextAttributesW(
            ref CredHandle contextHandle,
            [In] ContextAttribute attribute,
            [In] void* buffer);

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int SetContextAttributesW(
            ref CredHandle contextHandle,
            [In] ContextAttribute attribute,
            [In] byte[] buffer,
            [In] int bufferSize);

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal static extern int EnumerateSecurityPackagesW(
            [Out] out int pkgnum,
            [Out] out SafeFreeContextBuffer_SECURITY handle);

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] ref SEC_WINNT_AUTH_IDENTITY_W authdata,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] IntPtr zero,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] SafeSspiAuthDataHandle authdata,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern int AcquireCredentialsHandleW(
                  [In] string principal,
                  [In] string moduleName,
                  [In] int usage,
                  [In] void* logonID,
                  [In] ref SCHANNEL_CRED authData,
                  [In] void* keyCallback,
                  [In] void* keyArgument,
                  ref CredHandle handlePtr,
                  [Out] out long timeStamp
                  );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int InitializeSecurityContextW(
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

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int CompleteAuthToken(
                  [In] void* inContextPtr,
                  [In, Out] ref SecBufferDesc inputBuffers
                  );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern int ApplyControlToken(
          [In] void* inContextPtr,
          [In, Out] ref SecBufferDesc inputBuffers
          );

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, SetLastError = true)]
        internal unsafe static extern SECURITY_STATUS SspiFreeAuthIdentity(
            [In] IntPtr authData);

        [DllImport(Interop.Libraries.Sspi, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal unsafe static extern SECURITY_STATUS SspiEncodeStringsAsAuthIdentity(
            [In] string userName,
            [In] string domainName,
            [In] string password,
            [Out] out SafeSspiAuthDataHandle authData);
    }
}
