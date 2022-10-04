// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

internal static partial class Interop
{
    internal static partial class HttpApi
    {
        internal static readonly HTTPAPI_VERSION s_version = new HTTPAPI_VERSION() { HttpApiMajorVersion = 2, HttpApiMinorVersion = 0 };
        internal static readonly bool s_supported = InitHttpApi(s_version);
        internal static IPEndPoint s_any = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
        internal static IPEndPoint s_ipv6Any = new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort);
        internal const int IPv4AddressSize = 16;
        internal const int IPv6AddressSize = 28;

        private static bool InitHttpApi(HTTPAPI_VERSION version)
        {
            uint statusCode = HttpInitialize(version, (uint)HTTP_FLAGS.HTTP_INITIALIZE_SERVER, IntPtr.Zero);
            return statusCode == ERROR_SUCCESS;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_VERSION
        {
            internal ushort MajorVersion;
            internal ushort MinorVersion;
        }

        internal enum HTTP_RESPONSE_INFO_TYPE
        {
            HttpResponseInfoTypeMultipleKnownHeaders,
            HttpResponseInfoTypeAuthenticationProperty,
            HttpResponseInfoTypeQosProperty,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_RESPONSE_INFO
        {
            internal HTTP_RESPONSE_INFO_TYPE Type;
            internal uint Length;
            internal void* pInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_RESPONSE_HEADERS
        {
            internal ushort UnknownHeaderCount;
            internal HTTP_UNKNOWN_HEADER* pUnknownHeaders;
            internal ushort TrailerCount;
            internal HTTP_UNKNOWN_HEADER* pTrailers;
            internal HTTP_KNOWN_HEADER KnownHeaders;
            internal HTTP_KNOWN_HEADER KnownHeaders_02;
            internal HTTP_KNOWN_HEADER KnownHeaders_03;
            internal HTTP_KNOWN_HEADER KnownHeaders_04;
            internal HTTP_KNOWN_HEADER KnownHeaders_05;
            internal HTTP_KNOWN_HEADER KnownHeaders_06;
            internal HTTP_KNOWN_HEADER KnownHeaders_07;
            internal HTTP_KNOWN_HEADER KnownHeaders_08;
            internal HTTP_KNOWN_HEADER KnownHeaders_09;
            internal HTTP_KNOWN_HEADER KnownHeaders_10;
            internal HTTP_KNOWN_HEADER KnownHeaders_11;
            internal HTTP_KNOWN_HEADER KnownHeaders_12;
            internal HTTP_KNOWN_HEADER KnownHeaders_13;
            internal HTTP_KNOWN_HEADER KnownHeaders_14;
            internal HTTP_KNOWN_HEADER KnownHeaders_15;
            internal HTTP_KNOWN_HEADER KnownHeaders_16;
            internal HTTP_KNOWN_HEADER KnownHeaders_17;
            internal HTTP_KNOWN_HEADER KnownHeaders_18;
            internal HTTP_KNOWN_HEADER KnownHeaders_19;
            internal HTTP_KNOWN_HEADER KnownHeaders_20;
            internal HTTP_KNOWN_HEADER KnownHeaders_21;
            internal HTTP_KNOWN_HEADER KnownHeaders_22;
            internal HTTP_KNOWN_HEADER KnownHeaders_23;
            internal HTTP_KNOWN_HEADER KnownHeaders_24;
            internal HTTP_KNOWN_HEADER KnownHeaders_25;
            internal HTTP_KNOWN_HEADER KnownHeaders_26;
            internal HTTP_KNOWN_HEADER KnownHeaders_27;
            internal HTTP_KNOWN_HEADER KnownHeaders_28;
            internal HTTP_KNOWN_HEADER KnownHeaders_29;
            internal HTTP_KNOWN_HEADER KnownHeaders_30;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_KNOWN_HEADER
        {
            internal ushort RawValueLength;
            internal sbyte* pRawValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_UNKNOWN_HEADER
        {
            internal ushort NameLength;
            internal ushort RawValueLength;
            internal sbyte* pName;
            internal sbyte* pRawValue;
        }

        internal enum HTTP_DATA_CHUNK_TYPE : int
        {
            HttpDataChunkFromMemory = 0,
            HttpDataChunkFromFileHandle = 1,
            HttpDataChunkFromFragmentCache = 2,
            HttpDataChunkMaximum = 3,
        }

        [StructLayout(LayoutKind.Sequential, Size = 32)]
        internal unsafe struct HTTP_DATA_CHUNK
        {
            internal HTTP_DATA_CHUNK_TYPE DataChunkType;
            internal uint p0;
            internal byte* pBuffer;
            internal uint BufferLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_RESPONSE
        {
            internal uint Flags;
            internal HTTP_VERSION Version;
            internal ushort StatusCode;
            internal ushort ReasonLength;
            internal sbyte* pReason;
            internal HTTP_RESPONSE_HEADERS Headers;
            internal ushort EntityChunkCount;
            internal HTTP_DATA_CHUNK* pEntityChunks;
            internal ushort ResponseInfoCount;
            internal HTTP_RESPONSE_INFO* pResponseInfo;
        }

        internal enum HTTP_VERB : int
        {
            HttpVerbUnparsed = 0,
            HttpVerbUnknown = 1,
            HttpVerbInvalid = 2,
            HttpVerbOPTIONS = 3,
            HttpVerbGET = 4,
            HttpVerbHEAD = 5,
            HttpVerbPOST = 6,
            HttpVerbPUT = 7,
            HttpVerbDELETE = 8,
            HttpVerbTRACE = 9,
            HttpVerbCONNECT = 10,
            HttpVerbTRACK = 11,
            HttpVerbMOVE = 12,
            HttpVerbCOPY = 13,
            HttpVerbPROPFIND = 14,
            HttpVerbPROPPATCH = 15,
            HttpVerbMKCOL = 16,
            HttpVerbLOCK = 17,
            HttpVerbUNLOCK = 18,
            HttpVerbSEARCH = 19,
            HttpVerbMaximum = 20,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR
        {
            internal ushort sa_family;
            internal byte sa_data;
            internal byte sa_data_02;
            internal byte sa_data_03;
            internal byte sa_data_04;
            internal byte sa_data_05;
            internal byte sa_data_06;
            internal byte sa_data_07;
            internal byte sa_data_08;
            internal byte sa_data_09;
            internal byte sa_data_10;
            internal byte sa_data_11;
            internal byte sa_data_12;
            internal byte sa_data_13;
            internal byte sa_data_14;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_TRANSPORT_ADDRESS
        {
            internal SOCKADDR* pRemoteAddress;
            internal SOCKADDR* pLocalAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_REQUEST_HEADERS
        {
            internal ushort UnknownHeaderCount;
            internal HTTP_UNKNOWN_HEADER* pUnknownHeaders;
            internal ushort TrailerCount;
            internal HTTP_UNKNOWN_HEADER* pTrailers;
            internal HTTP_KNOWN_HEADER KnownHeaders;
            internal HTTP_KNOWN_HEADER KnownHeaders_02;
            internal HTTP_KNOWN_HEADER KnownHeaders_03;
            internal HTTP_KNOWN_HEADER KnownHeaders_04;
            internal HTTP_KNOWN_HEADER KnownHeaders_05;
            internal HTTP_KNOWN_HEADER KnownHeaders_06;
            internal HTTP_KNOWN_HEADER KnownHeaders_07;
            internal HTTP_KNOWN_HEADER KnownHeaders_08;
            internal HTTP_KNOWN_HEADER KnownHeaders_09;
            internal HTTP_KNOWN_HEADER KnownHeaders_10;
            internal HTTP_KNOWN_HEADER KnownHeaders_11;
            internal HTTP_KNOWN_HEADER KnownHeaders_12;
            internal HTTP_KNOWN_HEADER KnownHeaders_13;
            internal HTTP_KNOWN_HEADER KnownHeaders_14;
            internal HTTP_KNOWN_HEADER KnownHeaders_15;
            internal HTTP_KNOWN_HEADER KnownHeaders_16;
            internal HTTP_KNOWN_HEADER KnownHeaders_17;
            internal HTTP_KNOWN_HEADER KnownHeaders_18;
            internal HTTP_KNOWN_HEADER KnownHeaders_19;
            internal HTTP_KNOWN_HEADER KnownHeaders_20;
            internal HTTP_KNOWN_HEADER KnownHeaders_21;
            internal HTTP_KNOWN_HEADER KnownHeaders_22;
            internal HTTP_KNOWN_HEADER KnownHeaders_23;
            internal HTTP_KNOWN_HEADER KnownHeaders_24;
            internal HTTP_KNOWN_HEADER KnownHeaders_25;
            internal HTTP_KNOWN_HEADER KnownHeaders_26;
            internal HTTP_KNOWN_HEADER KnownHeaders_27;
            internal HTTP_KNOWN_HEADER KnownHeaders_28;
            internal HTTP_KNOWN_HEADER KnownHeaders_29;
            internal HTTP_KNOWN_HEADER KnownHeaders_30;
            internal HTTP_KNOWN_HEADER KnownHeaders_31;
            internal HTTP_KNOWN_HEADER KnownHeaders_32;
            internal HTTP_KNOWN_HEADER KnownHeaders_33;
            internal HTTP_KNOWN_HEADER KnownHeaders_34;
            internal HTTP_KNOWN_HEADER KnownHeaders_35;
            internal HTTP_KNOWN_HEADER KnownHeaders_36;
            internal HTTP_KNOWN_HEADER KnownHeaders_37;
            internal HTTP_KNOWN_HEADER KnownHeaders_38;
            internal HTTP_KNOWN_HEADER KnownHeaders_39;
            internal HTTP_KNOWN_HEADER KnownHeaders_40;
            internal HTTP_KNOWN_HEADER KnownHeaders_41;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_SSL_CLIENT_CERT_INFO
        {
            internal uint CertFlags;
            internal uint CertEncodedSize;
            internal byte* pCertEncoded;
            internal void* Token;
            internal byte CertDeniedByMapper;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_SSL_INFO
        {
            internal ushort ServerCertKeySize;
            internal ushort ConnectionKeySize;
            internal uint ServerCertIssuerSize;
            internal uint ServerCertSubjectSize;
            internal sbyte* pServerCertIssuer;
            internal sbyte* pServerCertSubject;
            internal HTTP_SSL_CLIENT_CERT_INFO* pClientCertInfo;
            internal uint SslClientCertNegotiated;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_REQUEST
        {
            internal uint Flags;
            internal ulong ConnectionId;
            internal ulong RequestId;
            internal ulong UrlContext;
            internal HTTP_VERSION Version;
            internal HTTP_VERB Verb;
            internal ushort UnknownVerbLength;
            internal ushort RawUrlLength;
            internal sbyte* pUnknownVerb;
            internal sbyte* pRawUrl;
            internal HTTP_COOKED_URL CookedUrl;
            internal HTTP_TRANSPORT_ADDRESS Address;
            internal HTTP_REQUEST_HEADERS Headers;
            internal ulong BytesReceived;
            internal ushort EntityChunkCount;
            internal HTTP_DATA_CHUNK* pEntityChunks;
            internal ulong RawConnectionId;
            internal HTTP_SSL_INFO* pSslInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HTTP_COOKED_URL
        {
            internal ushort FullUrlLength;
            internal ushort HostLength;
            internal ushort AbsPathLength;
            internal ushort QueryStringLength;
            internal ushort* pFullUrl;
            internal ushort* pHost;
            internal ushort* pAbsPath;
            internal ushort* pQueryString;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_REQUEST_CHANNEL_BIND_STATUS
        {
            internal IntPtr ServiceName;
            internal IntPtr ChannelToken;
            internal uint ChannelTokenSize;
            internal uint Flags;
        }

        internal enum HTTP_SERVER_PROPERTY
        {
            HttpServerAuthenticationProperty,
            HttpServerLoggingProperty,
            HttpServerQosProperty,
            HttpServerTimeoutsProperty,
            HttpServerQueueLengthProperty,
            HttpServerStateProperty,
            HttpServer503VerbosityProperty,
            HttpServerBindingProperty,
            HttpServerExtendedAuthenticationProperty,
            HttpServerListenEndpointProperty,
            HttpServerChannelBindProperty,
            HttpServerProtectionLevelProperty,
        }

        internal enum TOKENBINDING_HASH_ALGORITHM : byte
        {
            TOKENBINDING_HASH_ALGORITHM_SHA256 = 4,
        }

        internal enum TOKENBINDING_SIGNATURE_ALGORITHM : byte
        {
            TOKENBINDING_SIGNATURE_ALGORITHM_RSA = 1,
            TOKENBINDING_SIGNATURE_ALGORITHM_ECDSAP256 = 3,
        }

        internal enum TOKENBINDING_TYPE : byte
        {
            TOKENBINDING_TYPE_PROVIDED = 0,
            TOKENBINDING_TYPE_REFERRED = 1,
        }

        internal enum TOKENBINDING_EXTENSION_FORMAT
        {
            TOKENBINDING_EXTENSION_FORMAT_UNDEFINED = 0,
        }

        [Flags]
        internal enum HTTP_FLAGS : uint
        {
            NONE = 0x00000000,
            HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY = 0x00000001,
            HTTP_RECEIVE_SECURE_CHANNEL_TOKEN = 0x00000001,
            HTTP_SEND_RESPONSE_FLAG_DISCONNECT = 0x00000001,
            HTTP_SEND_RESPONSE_FLAG_MORE_DATA = 0x00000002,
            HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA = 0x00000004,
            HTTP_SEND_RESPONSE_FLAG_RAW_HEADER = 0x00000004,
            HTTP_SEND_REQUEST_FLAG_MORE_DATA = 0x00000001,
            HTTP_PROPERTY_FLAG_PRESENT = 0x00000001,
            HTTP_INITIALIZE_SERVER = 0x00000001,
            HTTP_INITIALIZE_CBT = 0x00000004,
            HTTP_SEND_RESPONSE_FLAG_OPAQUE = 0x00000040,
        }

        private const int HttpHeaderRequestMaximum = (int)HttpRequestHeader.UserAgent + 1;

        internal static class HTTP_REQUEST_HEADER_ID
        {
            internal static string ToString(int position)
            {
                return s_strings[position];
            }

            private static readonly string[] s_strings = {
                    "Cache-Control",
                    "Connection",
                    "Date",
                    "Keep-Alive",
                    "Pragma",
                    "Trailer",
                    "Transfer-Encoding",
                    "Upgrade",
                    "Via",
                    "Warning",

                    "Allow",
                    "Content-Length",
                    "Content-Type",
                    "Content-Encoding",
                    "Content-Language",
                    "Content-Location",
                    "Content-MD5",
                    "Content-Range",
                    "Expires",
                    "Last-Modified",

                    "Accept",
                    "Accept-Charset",
                    "Accept-Encoding",
                    "Accept-Language",
                    "Authorization",
                    "Cookie",
                    "Expect",
                    "From",
                    "Host",
                    "If-Match",

                    "If-Modified-Since",
                    "If-None-Match",
                    "If-Range",
                    "If-Unmodified-Since",
                    "Max-Forwards",
                    "Proxy-Authorization",
                    "Referer",
                    "Range",
                    "Te",
                    "Translate",
                    "User-Agent",
                };
        }

        internal enum HTTP_TIMEOUT_TYPE
        {
            EntityBody,
            DrainEntityBody,
            RequestQueue,
            IdleConnection,
            HeaderWait,
            MinSendRate,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_TIMEOUT_LIMIT_INFO
        {
            internal HTTP_FLAGS Flags;
            internal ushort EntityBody;
            internal ushort DrainEntityBody;
            internal ushort RequestQueue;
            internal ushort IdleConnection;
            internal ushort HeaderWait;
            internal uint MinSendRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTPAPI_VERSION
        {
            internal ushort HttpApiMajorVersion;
            internal ushort HttpApiMinorVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HTTP_BINDING_INFO
        {
            internal HTTP_FLAGS Flags;
            internal IntPtr RequestQueueHandle;
        }


        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static partial uint HttpInitialize(HTTPAPI_VERSION version, uint flags, IntPtr pReserved);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpSetUrlGroupProperty(ulong urlGroupId, HTTP_SERVER_PROPERTY serverProperty, void* pPropertyInfo, uint propertyInfoLength);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpCreateServerSession(HTTPAPI_VERSION version, ulong* serverSessionId, uint reserved);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpCreateUrlGroup(ulong serverSessionId, ulong* urlGroupId, uint reserved);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static partial uint HttpCloseUrlGroup(ulong urlGroupId);

        [LibraryImport(Libraries.HttpApi,  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial uint HttpCreateRequestQueue(HTTPAPI_VERSION version, string pName,
            Interop.Kernel32.SECURITY_ATTRIBUTES* pSecurityAttributes, uint flags, out HttpRequestQueueV2Handle pReqQueueHandle);

        [LibraryImport(Libraries.HttpApi,  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint HttpAddUrlToUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, ulong context, uint pReserved);

        [LibraryImport(Libraries.HttpApi,  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint HttpRemoveUrlFromUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, uint flags);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpReceiveHttpRequest(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_REQUEST* pRequestBuffer, uint requestBufferLength, uint* pBytesReturned, NativeOverlapped* pOverlapped);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpSendHttpResponse(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_RESPONSE* pHttpResponse, void* pCachePolicy, uint* pBytesSent, SafeLocalAllocHandle pRequestBuffer, uint requestBufferLength, NativeOverlapped* pOverlapped, void* pLogData);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpWaitForDisconnect(SafeHandle requestQueueHandle, ulong connectionId, NativeOverlapped* pOverlapped);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpReceiveRequestEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, void* pEntityBuffer, uint entityBufferLength, out uint bytesReturned, NativeOverlapped* pOverlapped);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpSendResponseEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, ushort entityChunkCount, HTTP_DATA_CHUNK* pEntityChunks, uint* pBytesSent, SafeLocalAllocHandle pRequestBuffer, uint requestBufferLength, NativeOverlapped* pOverlapped, void* pLogData);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static partial uint HttpCloseRequestQueue(IntPtr pReqQueueHandle);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static partial uint HttpCancelHttpRequest(SafeHandle requestQueueHandle, ulong requestId, IntPtr pOverlapped);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static partial uint HttpCloseServerSession(ulong serverSessionId);

        internal sealed class SafeLocalFreeChannelBinding : ChannelBinding
        {
            private int _size;

            public SafeLocalFreeChannelBinding() { }

            public override int Size
            {
                get { return _size; }
            }

            public static SafeLocalFreeChannelBinding LocalAlloc(int cb)
            {
                SafeLocalFreeChannelBinding result = new SafeLocalFreeChannelBinding();
                result.SetHandle(Marshal.AllocHGlobal(cb));
                result._size = cb;
                return result;
            }

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, HTTP_SSL_CLIENT_CERT_INFO* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, NativeOverlapped* pOverlapped);

        [LibraryImport(Libraries.HttpApi, SetLastError = true)]
        internal static unsafe partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, byte* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, NativeOverlapped* pOverlapped);

        internal static readonly string?[] HttpVerbs = new string?[]
        {
            null,
            "Unknown",
            "Invalid",
            "OPTIONS",
            "GET",
            "HEAD",
            "POST",
            "PUT",
            "DELETE",
            "TRACE",
            "CONNECT",
            "TRACK",
            "MOVE",
            "COPY",
            "PROPFIND",
            "PROPPATCH",
            "MKCOL",
            "LOCK",
            "UNLOCK",
            "SEARCH",
        };

        internal static class HTTP_RESPONSE_HEADER_ID
        {
            internal enum Enum
            {
                HttpHeaderCacheControl = 0,    // general-header [section 4.5]
                HttpHeaderConnection = 1,    // general-header [section 4.5]
                HttpHeaderDate = 2,    // general-header [section 4.5]
                HttpHeaderKeepAlive = 3,    // general-header [not in rfc]
                HttpHeaderPragma = 4,    // general-header [section 4.5]
                HttpHeaderTrailer = 5,    // general-header [section 4.5]
                HttpHeaderTransferEncoding = 6,    // general-header [section 4.5]
                HttpHeaderUpgrade = 7,    // general-header [section 4.5]
                HttpHeaderVia = 8,    // general-header [section 4.5]
                HttpHeaderWarning = 9,    // general-header [section 4.5]

                HttpHeaderAllow = 10,   // entity-header  [section 7.1]
                HttpHeaderContentLength = 11,   // entity-header  [section 7.1]
                HttpHeaderContentType = 12,   // entity-header  [section 7.1]
                HttpHeaderContentEncoding = 13,   // entity-header  [section 7.1]
                HttpHeaderContentLanguage = 14,   // entity-header  [section 7.1]
                HttpHeaderContentLocation = 15,   // entity-header  [section 7.1]
                HttpHeaderContentMd5 = 16,   // entity-header  [section 7.1]
                HttpHeaderContentRange = 17,   // entity-header  [section 7.1]
                HttpHeaderExpires = 18,   // entity-header  [section 7.1]
                HttpHeaderLastModified = 19,   // entity-header  [section 7.1]


                // Response Headers

                HttpHeaderAcceptRanges = 20,   // response-header [section 6.2]
                HttpHeaderAge = 21,   // response-header [section 6.2]
                HttpHeaderEtag = 22,   // response-header [section 6.2]
                HttpHeaderLocation = 23,   // response-header [section 6.2]
                HttpHeaderProxyAuthenticate = 24,   // response-header [section 6.2]
                HttpHeaderRetryAfter = 25,   // response-header [section 6.2]
                HttpHeaderServer = 26,   // response-header [section 6.2]
                HttpHeaderSetCookie = 27,   // response-header [not in rfc]
                HttpHeaderVary = 28,   // response-header [section 6.2]
                HttpHeaderWwwAuthenticate = 29,   // response-header [section 6.2]

                HttpHeaderResponseMaximum = 30,


                HttpHeaderMaximum = 41
            }

            private static readonly string[] s_strings =
            {
                "Cache-Control",
                "Connection",
                "Date",
                "Keep-Alive",
                "Pragma",
                "Trailer",
                "Transfer-Encoding",
                "Upgrade",
                "Via",
                "Warning",

                "Allow",
                "Content-Length",
                "Content-Type",
                "Content-Encoding",
                "Content-Language",
                "Content-Location",
                "Content-MD5",
                "Content-Range",
                "Expires",
                "Last-Modified",

                "Accept-Ranges",
                "Age",
                "ETag",
                "Location",
                "Proxy-Authenticate",
                "Retry-After",
                "Server",
                "Set-Cookie",
                "Vary",
                "WWW-Authenticate",
            };

            private static readonly Dictionary<string, int> s_hashtable = CreateTable();

            private static Dictionary<string, int> CreateTable()
            {
                var table = new Dictionary<string, int>((int)Enum.HttpHeaderResponseMaximum);
                for (int i = 0; i < (int)Enum.HttpHeaderResponseMaximum; i++)
                {
                    table.Add(s_strings[i], i);
                }
                return table;
            }

            internal static int IndexOfKnownHeader(string headerName)
            {
                int index;
                return s_hashtable.TryGetValue(headerName, out index) ? index : -1;
            }
        }

        private static unsafe string? GetKnownHeader(HTTP_REQUEST* request, long fixup, int headerIndex)
        {
            string? header = null;

            HTTP_KNOWN_HEADER* pKnownHeader = (&request->Headers.KnownHeaders) + headerIndex;

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(null, $"HttpApi::GetKnownHeader() pKnownHeader:0x{(IntPtr)pKnownHeader}");
                NetEventSource.Info(null, $"HttpApi::GetKnownHeader() pRawValue:0x{(IntPtr)pKnownHeader->pRawValue} RawValueLength:{pKnownHeader->RawValueLength}");
            }

            // For known headers, when header value is empty, RawValueLength will be 0 and
            // pRawValue will point to empty string
            if (pKnownHeader->pRawValue != null)
            {
                header = new string(pKnownHeader->pRawValue + fixup, 0, pKnownHeader->RawValueLength);
            }

            return header;
        }

        internal static unsafe string? GetKnownHeader(HTTP_REQUEST* request, int headerIndex)
        {
            return GetKnownHeader(request, 0, headerIndex);
        }

        private static unsafe string? GetVerb(HTTP_REQUEST* request, long fixup)
        {
            string? verb = null;

            if ((int)request->Verb > (int)HTTP_VERB.HttpVerbUnknown && (int)request->Verb < (int)HTTP_VERB.HttpVerbMaximum)
            {
                verb = HttpVerbs[(int)request->Verb];
            }
            else if (request->Verb == HTTP_VERB.HttpVerbUnknown && request->pUnknownVerb != null)
            {
                verb = new string(request->pUnknownVerb + fixup, 0, request->UnknownVerbLength);
            }

            return verb;
        }

        internal static unsafe string? GetVerb(HTTP_REQUEST* request)
        {
            return GetVerb(request, 0);
        }

        internal static unsafe string? GetVerb(IntPtr memoryBlob, IntPtr originalAddress)
        {
            return GetVerb((HTTP_REQUEST*)memoryBlob.ToPointer(), (byte*)memoryBlob - (byte*)originalAddress);
        }

        // Server API

        internal static unsafe WebHeaderCollection GetHeaders(IntPtr memoryBlob, IntPtr originalAddress)
        {
            // Return value.
            WebHeaderCollection headerCollection = new WebHeaderCollection();
            byte* pMemoryBlob = (byte*)memoryBlob;
            HTTP_REQUEST* request = (HTTP_REQUEST*)pMemoryBlob;
            long fixup = pMemoryBlob - (byte*)originalAddress;
            int index;

            // unknown headers
            if (request->Headers.UnknownHeaderCount != 0)
            {
                HTTP_UNKNOWN_HEADER* pUnknownHeader = (HTTP_UNKNOWN_HEADER*)(fixup + (byte*)request->Headers.pUnknownHeaders);
                for (index = 0; index < request->Headers.UnknownHeaderCount; index++)
                {
                    // For unknown headers, when header value is empty, RawValueLength will be 0 and
                    // pRawValue will be null.
                    if (pUnknownHeader->pName != null && pUnknownHeader->NameLength > 0)
                    {
                        string headerName = new string(pUnknownHeader->pName + fixup, 0, pUnknownHeader->NameLength);
                        string headerValue;
                        if (pUnknownHeader->pRawValue != null && pUnknownHeader->RawValueLength > 0)
                        {
                            headerValue = new string(pUnknownHeader->pRawValue + fixup, 0, pUnknownHeader->RawValueLength);
                        }
                        else
                        {
                            headerValue = string.Empty;
                        }
                        headerCollection.Add(headerName, headerValue);
                    }
                    pUnknownHeader++;
                }
            }

            // known headers
            HTTP_KNOWN_HEADER* pKnownHeader = &request->Headers.KnownHeaders;
            for (index = 0; index < HttpHeaderRequestMaximum; index++)
            {
                // For known headers, when header value is empty, RawValueLength will be 0 and
                // pRawValue will point to empty string ("\0")
                if (pKnownHeader->pRawValue != null)
                {
                    string headerValue = new string(pKnownHeader->pRawValue + fixup, 0, pKnownHeader->RawValueLength);
                    headerCollection.Add(HTTP_REQUEST_HEADER_ID.ToString(index), headerValue);
                }
                pKnownHeader++;
            }

            return headerCollection;
        }

        internal static unsafe uint GetChunks(IntPtr memoryBlob, IntPtr originalAddress, ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size)
        {
            // Return value.
            uint dataRead = 0;
            byte* pMemoryBlob = (byte*)memoryBlob;
            HTTP_REQUEST* request = (HTTP_REQUEST*)pMemoryBlob;
            long fixup = pMemoryBlob - (byte*)originalAddress;

            if (request->EntityChunkCount > 0 && dataChunkIndex < request->EntityChunkCount && dataChunkIndex != -1)
            {
                HTTP_DATA_CHUNK* pDataChunk = (HTTP_DATA_CHUNK*)(fixup + (byte*)&request->pEntityChunks[dataChunkIndex]);

                fixed (byte* pReadBuffer = buffer)
                {
                    byte* pTo = &pReadBuffer[offset];

                    while (dataChunkIndex < request->EntityChunkCount && dataRead < size)
                    {
                        if (dataChunkOffset >= pDataChunk->BufferLength)
                        {
                            dataChunkOffset = 0;
                            dataChunkIndex++;
                            pDataChunk++;
                        }
                        else
                        {
                            byte* pFrom = pDataChunk->pBuffer + dataChunkOffset + fixup;

                            uint bytesToRead = pDataChunk->BufferLength - (uint)dataChunkOffset;
                            if (bytesToRead > (uint)size)
                            {
                                bytesToRead = (uint)size;
                            }
                            for (uint i = 0; i < bytesToRead; i++)
                            {
                                *(pTo++) = *(pFrom++);
                            }
                            dataRead += bytesToRead;
                            dataChunkOffset += bytesToRead;
                        }
                    }
                }
            }
            //we're finished.
            if (dataChunkIndex == request->EntityChunkCount)
            {
                dataChunkIndex = -1;
            }

            return dataRead;
        }

        internal static unsafe HTTP_VERB GetKnownVerb(IntPtr memoryBlob, IntPtr originalAddress)
        {
            // Return value.
            HTTP_VERB verb = HTTP_VERB.HttpVerbUnknown;

            HTTP_REQUEST* request = (HTTP_REQUEST*)memoryBlob.ToPointer();
            if ((int)request->Verb > (int)HTTP_VERB.HttpVerbUnparsed && (int)request->Verb < (int)HTTP_VERB.HttpVerbMaximum)
            {
                verb = request->Verb;
            }

            return verb;
        }

        internal static unsafe IPEndPoint? GetRemoteEndPoint(IntPtr memoryBlob, IntPtr originalAddress)
        {
            SocketAddress? v4address = new SocketAddress(AddressFamily.InterNetwork, IPv4AddressSize);
            SocketAddress? v6address = new SocketAddress(AddressFamily.InterNetworkV6, IPv6AddressSize);

            byte* pMemoryBlob = (byte*)memoryBlob;
            HTTP_REQUEST* request = (HTTP_REQUEST*)pMemoryBlob;
            IntPtr address = request->Address.pRemoteAddress != null ? (IntPtr)(pMemoryBlob - (byte*)originalAddress + (byte*)request->Address.pRemoteAddress) : IntPtr.Zero;
            CopyOutAddress(address, ref v4address, ref v6address);

            IPEndPoint? endpoint = null;
            if (v4address != null)
            {
                endpoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort).Create(v4address) as IPEndPoint;
            }
            else if (v6address != null)
            {
                endpoint = new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort).Create(v6address) as IPEndPoint;
            }

            return endpoint;
        }

        internal static unsafe IPEndPoint? GetLocalEndPoint(IntPtr memoryBlob, IntPtr originalAddress)
        {
            SocketAddress? v4address = new SocketAddress(AddressFamily.InterNetwork, IPv4AddressSize);
            SocketAddress? v6address = new SocketAddress(AddressFamily.InterNetworkV6, IPv6AddressSize);

            byte* pMemoryBlob = (byte*)memoryBlob;
            HTTP_REQUEST* request = (HTTP_REQUEST*)pMemoryBlob;
            IntPtr address = request->Address.pLocalAddress != null ? (IntPtr)(pMemoryBlob - (byte*)originalAddress + (byte*)request->Address.pLocalAddress) : IntPtr.Zero;
            CopyOutAddress(address, ref v4address, ref v6address);

            IPEndPoint? endpoint = null;
            if (v4address != null)
            {
                endpoint = s_any.Create(v4address) as IPEndPoint;
            }
            else if (v6address != null)
            {
                endpoint = s_ipv6Any.Create(v6address) as IPEndPoint;
            }

            return endpoint;
        }

        private static unsafe void CopyOutAddress(IntPtr address, ref SocketAddress? v4address, ref SocketAddress? v6address)
        {
            if (address != IntPtr.Zero)
            {
                ushort addressFamily = *((ushort*)address);
                if (addressFamily == (ushort)AddressFamily.InterNetwork)
                {
                    v6address = null;
                    for (int index = 2; index < IPv4AddressSize; index++)
                    {
                        v4address![index] = ((byte*)address)[index];
                    }
                    return;
                }
                if (addressFamily == (ushort)AddressFamily.InterNetworkV6)
                {
                    v4address = null;
                    for (int index = 2; index < IPv6AddressSize; index++)
                    {
                        v6address![index] = ((byte*)address)[index];
                    }
                    return;
                }
            }

            v4address = null;
            v6address = null;
        }
    }
}
