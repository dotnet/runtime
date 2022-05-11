// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif
using System.Text;

internal static partial class Interop
{
    internal static partial class WinHttp
    {
        public const uint ERROR_SUCCESS = 0;
        public const uint ERROR_FILE_NOT_FOUND = 2;
        public const uint ERROR_INVALID_HANDLE = 6;
        public const uint ERROR_INVALID_PARAMETER = 87;
        public const uint ERROR_INSUFFICIENT_BUFFER = 122;
        public const uint ERROR_NOT_FOUND = 1168;
        public const uint ERROR_WINHTTP_INVALID_OPTION = 12009;
        public const uint ERROR_WINHTTP_LOGIN_FAILURE = 12015;
        public const uint ERROR_WINHTTP_OPERATION_CANCELLED = 12017;
        public const uint ERROR_WINHTTP_INCORRECT_HANDLE_STATE = 12019;
        public const uint ERROR_WINHTTP_CONNECTION_ERROR = 12030;
        public const uint ERROR_WINHTTP_RESEND_REQUEST = 12032;
        public const uint ERROR_WINHTTP_CLIENT_AUTH_CERT_NEEDED = 12044;
        public const uint ERROR_WINHTTP_HEADER_NOT_FOUND = 12150;
        public const uint ERROR_WINHTTP_SECURE_FAILURE = 12175;
        public const uint ERROR_WINHTTP_AUTODETECTION_FAILED = 12180;

        public const uint WINHTTP_OPTION_PROXY = 38;
        public const uint WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;
        public const uint WINHTTP_ACCESS_TYPE_NO_PROXY = 1;
        public const uint WINHTTP_ACCESS_TYPE_NAMED_PROXY = 3;
        public const uint WINHTTP_ACCESS_TYPE_AUTOMATIC_PROXY = 4;

        public const uint WINHTTP_AUTOPROXY_AUTO_DETECT = 0x00000001;
        public const uint WINHTTP_AUTOPROXY_CONFIG_URL = 0x00000002;
        public const uint WINHTTP_AUTOPROXY_HOST_KEEPCASE = 0x00000004;
        public const uint WINHTTP_AUTOPROXY_HOST_LOWERCASE = 0x00000008;
        public const uint WINHTTP_AUTOPROXY_RUN_INPROCESS = 0x00010000;
        public const uint WINHTTP_AUTOPROXY_RUN_OUTPROCESS_ONLY = 0x00020000;
        public const uint WINHTTP_AUTOPROXY_NO_DIRECTACCESS = 0x00040000;
        public const uint WINHTTP_AUTOPROXY_NO_CACHE_CLIENT = 0x00080000;
        public const uint WINHTTP_AUTOPROXY_NO_CACHE_SVC = 0x00100000;
        public const uint WINHTTP_AUTOPROXY_SORT_RESULTS = 0x00400000;

        public const uint WINHTTP_AUTO_DETECT_TYPE_DHCP = 0x00000001;
        public const uint WINHTTP_AUTO_DETECT_TYPE_DNS_A = 0x00000002;

        public const string WINHTTP_NO_PROXY_NAME = null;
        public const string WINHTTP_NO_PROXY_BYPASS = null;

        public const uint WINHTTP_ADDREQ_FLAG_ADD = 0x20000000;
        public const uint WINHTTP_ADDREQ_FLAG_REPLACE = 0x80000000;

        public const string WINHTTP_NO_REFERER = null;
        public const string WINHTTP_DEFAULT_ACCEPT_TYPES = null;

        public const ushort INTERNET_DEFAULT_PORT = 0;
        public const ushort INTERNET_DEFAULT_HTTP_PORT = 80;
        public const ushort INTERNET_DEFAULT_HTTPS_PORT = 443;

        public const uint WINHTTP_FLAG_SECURE = 0x00800000;
        public const uint WINHTTP_FLAG_ESCAPE_DISABLE = 0x00000040;
        public const uint WINHTTP_FLAG_AUTOMATIC_CHUNKING = 0x00000200;

        public const uint WINHTTP_QUERY_FLAG_NUMBER = 0x20000000;
        public const uint WINHTTP_QUERY_VERSION = 18;
        public const uint WINHTTP_QUERY_STATUS_CODE = 19;
        public const uint WINHTTP_QUERY_STATUS_TEXT = 20;
        public const uint WINHTTP_QUERY_RAW_HEADERS = 21;
        public const uint WINHTTP_QUERY_RAW_HEADERS_CRLF = 22;
        public const uint WINHTTP_QUERY_FLAG_TRAILERS = 0x02000000;
        public const uint WINHTTP_QUERY_CONTENT_ENCODING = 29;
        public const uint WINHTTP_QUERY_SET_COOKIE = 43;
        public const uint WINHTTP_QUERY_CUSTOM = 65535;
        public const string WINHTTP_HEADER_NAME_BY_INDEX = null;
        public const byte[] WINHTTP_NO_OUTPUT_BUFFER = null;

        public const uint WINHTTP_OPTION_DECOMPRESSION = 118;
        public const uint WINHTTP_DECOMPRESSION_FLAG_GZIP = 0x00000001;
        public const uint WINHTTP_DECOMPRESSION_FLAG_DEFLATE = 0x00000002;
        public const uint WINHTTP_DECOMPRESSION_FLAG_ALL = WINHTTP_DECOMPRESSION_FLAG_GZIP | WINHTTP_DECOMPRESSION_FLAG_DEFLATE;

        public const uint WINHTTP_OPTION_REDIRECT_POLICY = 88;
        public const uint WINHTTP_OPTION_REDIRECT_POLICY_NEVER = 0;
        public const uint WINHTTP_OPTION_REDIRECT_POLICY_DISALLOW_HTTPS_TO_HTTP = 1;
        public const uint WINHTTP_OPTION_REDIRECT_POLICY_ALWAYS = 2;
        public const uint WINHTTP_OPTION_MAX_HTTP_AUTOMATIC_REDIRECTS = 89;

        public const uint WINHTTP_OPTION_MAX_CONNS_PER_SERVER = 73;
        public const uint WINHTTP_OPTION_MAX_CONNS_PER_1_0_SERVER = 74;

        public const uint WINHTTP_OPTION_DISABLE_FEATURE = 63;
        public const uint WINHTTP_DISABLE_COOKIES = 0x00000001;
        public const uint WINHTTP_DISABLE_REDIRECTS = 0x00000002;
        public const uint WINHTTP_DISABLE_AUTHENTICATION = 0x00000004;
        public const uint WINHTTP_DISABLE_KEEP_ALIVE = 0x00000008;

        public const uint WINHTTP_OPTION_ENABLE_FEATURE = 79;
        public const uint WINHTTP_ENABLE_SSL_REVOCATION = 0x00000001;

        public const uint WINHTTP_OPTION_CLIENT_CERT_CONTEXT = 47;
        public const uint WINHTTP_OPTION_CLIENT_CERT_ISSUER_LIST = 94;
        public const uint WINHTTP_OPTION_SERVER_CERT_CONTEXT = 78;
        public const uint WINHTTP_OPTION_SECURITY_FLAGS = 31;
        public const uint WINHTTP_OPTION_SECURE_PROTOCOLS = 84;
        public const uint WINHTTP_FLAG_SECURE_PROTOCOL_SSL2 = 0x00000008;
        public const uint WINHTTP_FLAG_SECURE_PROTOCOL_SSL3 = 0x00000020;
        public const uint WINHTTP_FLAG_SECURE_PROTOCOL_TLS1 = 0x00000080;
        public const uint WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_1 = 0x00000200;
        public const uint WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_2 = 0x00000800;
        public const uint WINHTTP_FLAG_SECURE_PROTOCOL_TLS1_3 = 0x00002000;

        public const uint SECURITY_FLAG_IGNORE_UNKNOWN_CA = 0x00000100;
        public const uint SECURITY_FLAG_IGNORE_CERT_DATE_INVALID = 0x00002000;
        public const uint SECURITY_FLAG_IGNORE_CERT_CN_INVALID = 0x00001000;
        public const uint SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE = 0x00000200;

        public const uint WINHTTP_OPTION_AUTOLOGON_POLICY = 77;
        public const uint WINHTTP_AUTOLOGON_SECURITY_LEVEL_MEDIUM = 0; // default creds only sent to intranet servers (default)
        public const uint WINHTTP_AUTOLOGON_SECURITY_LEVEL_LOW = 1; // default creds set to all servers
        public const uint WINHTTP_AUTOLOGON_SECURITY_LEVEL_HIGH = 2; // default creds never sent

        public const uint WINHTTP_AUTH_SCHEME_BASIC = 0x00000001;
        public const uint WINHTTP_AUTH_SCHEME_NTLM = 0x00000002;
        public const uint WINHTTP_AUTH_SCHEME_PASSPORT = 0x00000004;
        public const uint WINHTTP_AUTH_SCHEME_DIGEST = 0x00000008;
        public const uint WINHTTP_AUTH_SCHEME_NEGOTIATE = 0x00000010;

        public const uint WINHTTP_AUTH_TARGET_SERVER = 0x00000000;
        public const uint WINHTTP_AUTH_TARGET_PROXY = 0x00000001;

        public const uint WINHTTP_OPTION_USERNAME = 0x1000;
        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. It is property descriptor, not secret value.")]
        public const uint WINHTTP_OPTION_PASSWORD = 0x1001;
        public const uint WINHTTP_OPTION_PROXY_USERNAME = 0x1002;
        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. It is property descriptor, not secret value.")]
        public const uint WINHTTP_OPTION_PROXY_PASSWORD = 0x1003;

        public const uint WINHTTP_OPTION_SERVER_SPN_USED = 106;
        public const uint WINHTTP_OPTION_SERVER_CBT = 108;

        public const uint WINHTTP_OPTION_CONNECT_TIMEOUT = 3;
        public const uint WINHTTP_OPTION_SEND_TIMEOUT = 5;
        public const uint WINHTTP_OPTION_RECEIVE_TIMEOUT = 6;

        public const uint WINHTTP_OPTION_URL = 34;

        public const uint WINHTTP_OPTION_MAX_RESPONSE_HEADER_SIZE = 91;
        public const uint WINHTTP_OPTION_MAX_RESPONSE_DRAIN_SIZE = 92;
        public const uint WINHTTP_OPTION_CONNECTION_INFO = 93;

        public const uint WINHTTP_OPTION_ASSURED_NON_BLOCKING_CALLBACKS = 111;

        public const uint WINHTTP_OPTION_ENABLE_HTTP2_PLUS_CLIENT_CERT = 161;
        public const uint WINHTTP_OPTION_ENABLE_HTTP_PROTOCOL = 133;
        public const uint WINHTTP_OPTION_HTTP_PROTOCOL_USED = 134;
        public const uint WINHTTP_PROTOCOL_FLAG_HTTP2 = 0x1;
        public const uint WINHTTP_HTTP2_PLUS_CLIENT_CERT_FLAG = 0x1;
        public const uint WINHTTP_OPTION_DISABLE_STREAM_QUEUE = 139;

        public const uint WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET = 114;
        public const uint WINHTTP_OPTION_WEB_SOCKET_CLOSE_TIMEOUT = 115;
        public const uint WINHTTP_OPTION_WEB_SOCKET_KEEPALIVE_INTERVAL = 116;

        public const uint WINHTTP_OPTION_WEB_SOCKET_RECEIVE_BUFFER_SIZE = 122;
        public const uint WINHTTP_OPTION_WEB_SOCKET_SEND_BUFFER_SIZE = 123;

        public const uint WINHTTP_OPTION_TCP_KEEPALIVE = 152;
        public const uint WINHTTP_OPTION_STREAM_ERROR_CODE = 159;
        public const uint WINHTTP_OPTION_REQUIRE_STREAM_END = 160;

        public enum WINHTTP_WEB_SOCKET_BUFFER_TYPE
        {
            WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE = 0,
            WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE = 1,
            WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE = 2,
            WINHTTP_WEB_SOCKET_UTF8_FRAGMENT_BUFFER_TYPE = 3,
            WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE = 4
        }

        public const uint WINHTTP_OPTION_CONTEXT_VALUE = 45;

        public const uint WINHTTP_FLAG_ASYNC = 0x10000000;

        public const uint WINHTTP_CALLBACK_STATUS_RESOLVING_NAME = 0x00000001;
        public const uint WINHTTP_CALLBACK_STATUS_NAME_RESOLVED = 0x00000002;
        public const uint WINHTTP_CALLBACK_STATUS_CONNECTING_TO_SERVER = 0x00000004;
        public const uint WINHTTP_CALLBACK_STATUS_CONNECTED_TO_SERVER = 0x00000008;
        public const uint WINHTTP_CALLBACK_STATUS_SENDING_REQUEST = 0x00000010;
        public const uint WINHTTP_CALLBACK_STATUS_REQUEST_SENT = 0x00000020;
        public const uint WINHTTP_CALLBACK_STATUS_RECEIVING_RESPONSE = 0x00000040;
        public const uint WINHTTP_CALLBACK_STATUS_RESPONSE_RECEIVED = 0x00000080;
        public const uint WINHTTP_CALLBACK_STATUS_CLOSING_CONNECTION = 0x00000100;
        public const uint WINHTTP_CALLBACK_STATUS_CONNECTION_CLOSED = 0x00000200;
        public const uint WINHTTP_CALLBACK_STATUS_HANDLE_CREATED = 0x00000400;
        public const uint WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING = 0x00000800;
        public const uint WINHTTP_CALLBACK_STATUS_DETECTING_PROXY = 0x00001000;
        public const uint WINHTTP_CALLBACK_STATUS_REDIRECT = 0x00004000;
        public const uint WINHTTP_CALLBACK_STATUS_INTERMEDIATE_RESPONSE = 0x00008000;
        public const uint WINHTTP_CALLBACK_STATUS_SECURE_FAILURE = 0x00010000;
        public const uint WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE = 0x00020000;
        public const uint WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE = 0x00040000;
        public const uint WINHTTP_CALLBACK_STATUS_READ_COMPLETE = 0x00080000;
        public const uint WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE = 0x00100000;
        public const uint WINHTTP_CALLBACK_STATUS_REQUEST_ERROR = 0x00200000;
        public const uint WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE = 0x00400000;
        public const uint WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE = 0x01000000;
        public const uint WINHTTP_CALLBACK_STATUS_CLOSE_COMPLETE = 0x02000000;
        public const uint WINHTTP_CALLBACK_STATUS_SHUTDOWN_COMPLETE = 0x04000000;

        public const uint WINHTTP_CALLBACK_FLAG_SEND_REQUEST =
            WINHTTP_CALLBACK_STATUS_SENDING_REQUEST |
            WINHTTP_CALLBACK_STATUS_REQUEST_SENT;
        public const uint WINHTTP_CALLBACK_FLAG_HANDLES =
            WINHTTP_CALLBACK_STATUS_HANDLE_CREATED |
            WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING;
        public const uint WINHTTP_CALLBACK_FLAG_REDIRECT = WINHTTP_CALLBACK_STATUS_REDIRECT;
        public const uint WINHTTP_CALLBACK_FLAG_SECURE_FAILURE = WINHTTP_CALLBACK_STATUS_SECURE_FAILURE;
        public const uint WINHTTP_CALLBACK_FLAG_SENDREQUEST_COMPLETE = WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE;
        public const uint WINHTTP_CALLBACK_FLAG_HEADERS_AVAILABLE = WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE;
        public const uint WINHTTP_CALLBACK_FLAG_DATA_AVAILABLE = WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE;
        public const uint WINHTTP_CALLBACK_FLAG_READ_COMPLETE = WINHTTP_CALLBACK_STATUS_READ_COMPLETE;
        public const uint WINHTTP_CALLBACK_FLAG_WRITE_COMPLETE = WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE;
        public const uint WINHTTP_CALLBACK_FLAG_REQUEST_ERROR = WINHTTP_CALLBACK_STATUS_REQUEST_ERROR;
        public const uint WINHTTP_CALLBACK_FLAG_GETPROXYFORURL_COMPLETE = WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE;
        public const uint WINHTTP_CALLBACK_FLAG_ALL_COMPLETIONS =
            WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE |
            WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE |
            WINHTTP_CALLBACK_STATUS_DATA_AVAILABLE |
            WINHTTP_CALLBACK_STATUS_READ_COMPLETE |
            WINHTTP_CALLBACK_STATUS_WRITE_COMPLETE |
            WINHTTP_CALLBACK_STATUS_REQUEST_ERROR |
            WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE;
        public const uint WINHTTP_CALLBACK_FLAG_ALL_NOTIFICATIONS = 0xFFFFFFFF;

        public const uint WININET_E_CONNECTION_RESET = 0x80072EFF;

        public const int WINHTTP_INVALID_STATUS_CALLBACK = -1;
        public delegate void WINHTTP_STATUS_CALLBACK(
            IntPtr handle,
            IntPtr context,
            uint internetStatus,
            IntPtr statusInformation,
            uint statusInformationLength);

#if NET7_0_OR_GREATER
        [NativeMarshalling(typeof(Native))]
#endif
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINHTTP_AUTOPROXY_OPTIONS
        {
            public uint Flags;
            public uint AutoDetectFlags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? AutoConfigUrl;
            public IntPtr Reserved1;
            public uint Reserved2;
            [MarshalAs(UnmanagedType.Bool)]
            public bool AutoLoginIfChallenged;
#if NET7_0_OR_GREATER
            [CustomTypeMarshaller(typeof(WINHTTP_AUTOPROXY_OPTIONS), Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            public struct Native
            {
                private uint Flags;
                private uint AutoDetectFlags;
                private IntPtr AutoConfigUrl;
                private IntPtr Reserved1;
                private uint Reserved2;
                private int AutoLoginIfChallenged;

                public Native(WINHTTP_AUTOPROXY_OPTIONS managed)
                {
                    Flags = managed.Flags;
                    AutoDetectFlags = managed.AutoDetectFlags;
                    AutoConfigUrl = managed.AutoConfigUrl is not null ? Marshal.StringToCoTaskMemUni(managed.AutoConfigUrl) : IntPtr.Zero;
                    Reserved1 = managed.Reserved1;
                    Reserved2 = managed.Reserved2;
                    AutoLoginIfChallenged = managed.AutoLoginIfChallenged ? 1 : 0;
                }

                public WINHTTP_AUTOPROXY_OPTIONS ToManaged()
                {
                    return new WINHTTP_AUTOPROXY_OPTIONS
                    {
                        Flags = Flags,
                        AutoDetectFlags = AutoDetectFlags,
                        AutoConfigUrl = AutoConfigUrl != IntPtr.Zero ? Marshal.PtrToStringUni(AutoConfigUrl) : null,
                        Reserved1 = Reserved1,
                        Reserved2 = Reserved2,
                        AutoLoginIfChallenged = AutoLoginIfChallenged != 0
                    };
                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(AutoConfigUrl);
                }
            }
#endif
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINHTTP_CURRENT_USER_IE_PROXY_CONFIG
        {
            public int AutoDetect;
            public IntPtr AutoConfigUrl;
            public IntPtr Proxy;
            public IntPtr ProxyBypass;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINHTTP_PROXY_INFO
        {
            public uint AccessType;
            public IntPtr Proxy;
            public IntPtr ProxyBypass;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINHTTP_ASYNC_RESULT
        {
            public IntPtr dwResult;
            public uint dwError;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct tcp_keepalive
        {
            public uint onoff;
            public uint keepalivetime;
            public uint keepaliveinterval;
        }

        public const uint API_RECEIVE_RESPONSE = 1;
        public const uint API_QUERY_DATA_AVAILABLE = 2;
        public const uint API_READ_DATA = 3;
        public const uint API_WRITE_DATA = 4;
        public const uint API_SEND_REQUEST = 5;

        public enum WINHTTP_WEB_SOCKET_OPERATION
        {
            WINHTTP_WEB_SOCKET_SEND_OPERATION = 0,
            WINHTTP_WEB_SOCKET_RECEIVE_OPERATION = 1,
            WINHTTP_WEB_SOCKET_CLOSE_OPERATION = 2,
            WINHTTP_WEB_SOCKET_SHUTDOWN_OPERATION = 3
        }
    }
}
