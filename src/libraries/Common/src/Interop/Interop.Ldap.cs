// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif
using System.Security.Authentication;


internal static partial class Interop
{
    public const int SEC_WINNT_AUTH_IDENTITY_UNICODE = 0x2;
    public const int SEC_WINNT_AUTH_IDENTITY_VERSION = 0x200;
    public const string MICROSOFT_KERBEROS_NAME_W = "Kerberos";
    public const uint LDAP_SASL_QUIET = 2;
    public const string KerberosDefaultMechanism = "GSSAPI";
}

namespace System.DirectoryServices.Protocols
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal readonly struct Luid
    {
        private readonly int _lowPart;
        private readonly int _highPart;

        public int LowPart => _lowPart;
        public int HighPart => _highPart;
    }

#if NET7_0_OR_GREATER
    [NativeMarshalling(typeof(Marshaller))]
#endif
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SEC_WINNT_AUTH_IDENTITY_EX
    {
        public int version;
        public int length;
        public string user;
        public int userLength;
        public string domain;
        public int domainLength;
        public string password;
        public int passwordLength;
        public int flags;
        public string packageList;
        public int packageListLength;

#if NET7_0_OR_GREATER
        [CustomMarshaller(typeof(SEC_WINNT_AUTH_IDENTITY_EX), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
        internal static class Marshaller
        {
            public static Native ConvertToUnmanaged(SEC_WINNT_AUTH_IDENTITY_EX managed)
            {
                Native n = default;
                n.version = managed.version;
                n.length = managed.length;
                n.user = Marshal.StringToCoTaskMemUni(managed.user);
                n.userLength = managed.userLength;
                n.domain = Marshal.StringToCoTaskMemUni(managed.domain);
                n.domainLength = managed.domainLength;
                n.password = Marshal.StringToCoTaskMemUni(managed.password);
                n.passwordLength = managed.passwordLength;
                n.flags = managed.flags;
                n.packageList = Marshal.StringToCoTaskMemUni(managed.packageList);
                n.packageListLength = managed.packageListLength;
                return n;
            }

            public static void Free(Native native)
            {
                Marshal.FreeCoTaskMem(native.user);
                Marshal.FreeCoTaskMem(native.domain);
                Marshal.FreeCoTaskMem(native.password);
                Marshal.FreeCoTaskMem(native.packageList);
            }
        }
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Native
        {
            public int version;
            public int length;
            public IntPtr user;
            public int userLength;
            public IntPtr domain;
            public int domainLength;
            public IntPtr password;
            public int passwordLength;
            public int flags;
            public IntPtr packageList;
            public int packageListLength;
        }
    }

    internal enum BindMethod : uint // Not Supported in Linux
    {
        LDAP_AUTH_OTHERKIND = 0x86,
        LDAP_AUTH_SICILY = LDAP_AUTH_OTHERKIND | 0x0200,
        LDAP_AUTH_MSN = LDAP_AUTH_OTHERKIND | 0x0800,
        LDAP_AUTH_NTLM = LDAP_AUTH_OTHERKIND | 0x1000,
        LDAP_AUTH_DPA = LDAP_AUTH_OTHERKIND | 0x2000,
        LDAP_AUTH_NEGOTIATE = LDAP_AUTH_OTHERKIND | 0x0400,
        LDAP_AUTH_SSPI = LDAP_AUTH_NEGOTIATE,
        LDAP_AUTH_DIGEST = LDAP_AUTH_OTHERKIND | 0x4000,
        LDAP_AUTH_EXTERNAL = LDAP_AUTH_OTHERKIND | 0x0020,
        LDAP_AUTH_KRBV4 = 0xFF,
        LDAP_AUTH_SIMPLE = 0x80
    }

    internal enum LdapOption
    {
        LDAP_OPT_DESC = 0x01,
        LDAP_OPT_DEREF = 0x02,
        LDAP_OPT_SIZELIMIT = 0x03,
        LDAP_OPT_TIMELIMIT = 0x04,
        LDAP_OPT_REFERRALS = 0x08,
        LDAP_OPT_RESTART = 0x09,
        LDAP_OPT_SSL = 0x0a, // Not Supported in Linux
        LDAP_OPT_REFERRAL_HOP_LIMIT = 0x10, // Not Supported in Linux
        LDAP_OPT_VERSION = 0x11,
        LDAP_OPT_SERVER_CONTROLS = 0x12, // Not Supported in Windows
        LDAP_OPT_CLIENT_CONTROLS = 0x13, // Not Supported in Windows
        LDAP_OPT_API_FEATURE_INFO = 0x15,
        LDAP_OPT_HOST_NAME = 0x30,
        LDAP_OPT_ERROR_NUMBER = 0x31, // aka LDAP_OPT_RESULT_CODE
        LDAP_OPT_ERROR_STRING = 0x32, // aka LDAP_OPT_DIAGNOSTIC_MESSAGE
        // This one is overloaded between Windows and Linux servers:
        // in OpenLDAP, LDAP_OPT_MATCHED_DN = 0x33
        LDAP_OPT_SERVER_ERROR = 0x33, // Not Supported in Linux
        LDAP_OPT_SERVER_EXT_ERROR = 0x34, // Not Supported in Linux
        LDAP_OPT_HOST_REACHABLE = 0x3E, // Not Supported in Linux
        LDAP_OPT_PING_KEEP_ALIVE = 0x36, // Not Supported in Linux
        LDAP_OPT_PING_WAIT_TIME = 0x37, // Not Supported in Linux
        LDAP_OPT_PING_LIMIT = 0x38, // Not Supported in Linux
        LDAP_OPT_DNSDOMAIN_NAME = 0x3B, // Not Supported in Linux
        LDAP_OPT_GETDSNAME_FLAGS = 0x3D, // Not Supported in Linux
        LDAP_OPT_PROMPT_CREDENTIALS = 0x3F, // Not Supported in Linux
        LDAP_OPT_TCP_KEEPALIVE = 0x40, // Not Supported in Linux
        LDAP_OPT_FAST_CONCURRENT_BIND = 0x41, // Not Supported in Linux
        LDAP_OPT_SEND_TIMEOUT = 0x42, // Not Supported in Linux
        LDAP_OPT_REFERRAL_CALLBACK = 0x70, // Not Supported in Linux
        LDAP_OPT_CLIENT_CERTIFICATE = 0x80, // Not Supported in Linux
        LDAP_OPT_SERVER_CERTIFICATE = 0x81, // Not Supported in Linux
        LDAP_OPT_AUTO_RECONNECT = 0x91, // Not Supported in Linux
        LDAP_OPT_SSPI_FLAGS = 0x92,
        LDAP_OPT_SSL_INFO = 0x93, // Not Supported in Linux
        LDAP_OPT_SIGN = 0x95,
        LDAP_OPT_ENCRYPT = 0x96,
        LDAP_OPT_SASL_METHOD = 0x97,
        LDAP_OPT_AREC_EXCLUSIVE = 0x98, // Not Supported in Linux
        LDAP_OPT_SECURITY_CONTEXT = 0x99,
        LDAP_OPT_ROOTDSE_CACHE = 0x9a, // Not Supported in Linux
        LDAP_OPT_DEBUG_LEVEL = 0x5001,
        LDAP_OPT_URI = 0x5006, // Not Supported in Windows
        LDAP_OPT_X_SASL_REALM = 0x6101,
        LDAP_OPT_X_SASL_AUTHCID = 0x6102,
        LDAP_OPT_X_SASL_AUTHZID = 0x6103
    }

    internal enum ResultAll
    {
        LDAP_MSG_ALL = 1,
        LDAP_MSG_RECEIVED = 2,
        LDAP_MSG_POLLINGALL = 3 // Not Supported in Linux
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LDAP_TIMEVAL
    {
        public int tv_sec;
        public int tv_usec;
    }

#if NET7_0_OR_GREATER
    [NativeMarshalling(typeof(PinningMarshaller))]
#endif
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class BerVal
    {
        public int bv_len;
        public IntPtr bv_val = IntPtr.Zero;

#if NET7_0_OR_GREATER
        [CustomMarshaller(typeof(BerVal), MarshalMode.ManagedToUnmanagedIn, typeof(PinningMarshaller))]
        internal static unsafe class PinningMarshaller
        {
            public static ref int GetPinnableReference(BerVal managed) => ref (managed is null ? ref Unsafe.NullRef<int>() : ref managed.bv_len);

            // All usages in our currently supported scenarios will always go through GetPinnableReference
            public static int* ConvertToUnmanaged(BerVal managed) => throw new UnreachableException();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class LdapControl
    {
        public IntPtr ldctl_oid = IntPtr.Zero;
        public BerVal ldctl_value;
        public bool ldctl_iscritical;

        public LdapControl() { }
    }

#if NET7_0_OR_GREATER
    [NativeMarshalling(typeof(Marshaller))]
#endif
    [StructLayout(LayoutKind.Sequential)]
    internal struct LdapReferralCallback
    {
        public int sizeofcallback;
        public QUERYFORCONNECTIONInternal query;
        public NOTIFYOFNEWCONNECTIONInternal notify;
        public DEREFERENCECONNECTIONInternal dereference;
#if NET7_0_OR_GREATER
        public static readonly unsafe int Size = sizeof(Marshaller.MarshalValue.Native);

        [CustomMarshaller(typeof(LdapReferralCallback), MarshalMode.ManagedToUnmanagedIn, typeof(MarshalValue))]
        [CustomMarshaller(typeof(LdapReferralCallback), MarshalMode.ManagedToUnmanagedRef, typeof(MarshalValue))]
        [CustomMarshaller(typeof(LdapReferralCallback), MarshalMode.ManagedToUnmanagedOut, typeof(MarshalValue))]
        public static class Marshaller
        {
            public unsafe struct MarshalValue
            {
                public unsafe struct Native
                {
                    public int sizeofcallback;
                    public IntPtr query;
                    public IntPtr notify;
                    public IntPtr dereference;
                }

                private LdapReferralCallback _managed;
                private Native _native;

                public void FromManaged(LdapReferralCallback managed)
                {
                    _managed = managed;
                    _native.sizeofcallback = sizeof(Native);
                    _native.query = managed.query is not null ? Marshal.GetFunctionPointerForDelegate(managed.query) : IntPtr.Zero;
                    _native.notify = managed.notify is not null ? Marshal.GetFunctionPointerForDelegate(managed.notify) : IntPtr.Zero;
                    _native.dereference = managed.dereference is not null ? Marshal.GetFunctionPointerForDelegate(managed.dereference) : IntPtr.Zero;
                }

                public Native ToUnmanaged() => _native;

                public void FromUnmanaged(Native value) => _native = value;

                public LdapReferralCallback ToManaged()
                {
                    return new LdapReferralCallback()
                    {
                        sizeofcallback = _native.sizeofcallback,
                        query = _native.query != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<QUERYFORCONNECTIONInternal>(_native.query) : null,
                        notify = _native.notify != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<NOTIFYOFNEWCONNECTIONInternal>(_native.notify) : null,
                        dereference = _native.dereference != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<DEREFERENCECONNECTIONInternal>(_native.dereference) : null
                    };
                }

                public void OnInvoked() => GC.KeepAlive(_managed);
            }
        }
#else
        public static readonly unsafe int Size = Marshal.SizeOf<LdapReferralCallback>();
#endif
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CRYPTOAPI_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SecPkgContext_IssuerListInfoEx
    {
        public IntPtr aIssuers;
        public int cIssuers;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal sealed class LdapMod
    {
        public int type;
        public IntPtr attribute = IntPtr.Zero;
        public IntPtr values = IntPtr.Zero;

        ~LdapMod()
        {
            if (attribute != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(attribute);
            }

            if (values != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(values);
            }
        }
    }
}
