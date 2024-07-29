// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Net
{
    [EventSource(Name = "Private.InternalDiagnostics.System.Net.Http")]
    internal sealed partial class NetEventSource
    {
        private const int UriBaseAddressId = NextAvailableEventId;
        private const int ContentNullId = UriBaseAddressId + 1;
        private const int HeadersInvalidValueId = ContentNullId + 1;
        private const int HandlerMessageId = HeadersInvalidValueId + 1;
        private const int AuthenticationInfoId = HandlerMessageId + 1;
        private const int AuthenticationErrorId = AuthenticationInfoId + 1;
        private const int HandlerErrorId = AuthenticationErrorId + 1;

        [NonEvent]
        public static void UriBaseAddress(object obj, Uri? baseAddress)
        {
            Debug.Assert(Log.IsEnabled());
            Log.UriBaseAddress(baseAddress?.ToString(), IdOf(obj));
        }

        [Event(UriBaseAddressId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void UriBaseAddress(string? uriBaseAddress, string objName) =>
            WriteEvent(UriBaseAddressId, uriBaseAddress, objName);

        [NonEvent]
        public static void ContentNull(object obj)
        {
            Debug.Assert(Log.IsEnabled());
            Log.ContentNull(IdOf(obj), GetHashCode(obj));
        }

        [Event(ContentNullId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void ContentNull(string objName, int objHash) =>
            WriteEvent(ContentNullId, objName, objHash);

        [Event(HeadersInvalidValueId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        public void HeadersInvalidValue(string name, string rawValue) =>
            WriteEvent(HeadersInvalidValueId, name, rawValue);

        [Event(HandlerMessageId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void HandlerMessage(int poolId, int workerId, int requestId, string? memberName, string? message) =>
            WriteEvent(HandlerMessageId, poolId, workerId, requestId, memberName, message);

        [Event(HandlerErrorId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        public void HandlerMessageError(int poolId, int workerId, int requestId, string? memberName, string message) =>
            WriteEvent(HandlerErrorId, poolId, workerId, requestId, memberName, message);

        [NonEvent]
        public static void AuthenticationInfo(Uri uri, string message)
        {
            Debug.Assert(Log.IsEnabled());
            Log.AuthenticationInfo(uri?.ToString(), message);
        }

        [Event(AuthenticationInfoId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void AuthenticationInfo(string? uri, string message) =>
            WriteEvent(AuthenticationInfoId, uri, message);

        [NonEvent]
        public static void AuthenticationError(Uri? uri, string message)
        {
            Debug.Assert(Log.IsEnabled());
            Log.AuthenticationError(uri?.ToString(), message);
        }

        [Event(AuthenticationErrorId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        public void AuthenticationError(string? uri, string message) =>
            WriteEvent(AuthenticationErrorId, uri, message);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3, string? arg4, string? arg5)
        {
            arg4 ??= "";
            arg5 ??= "";

            fixed (char* string4Bytes = arg4)
            fixed (char* string5Bytes = arg5)
            {
                const int NumEventDatas = 5;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(&arg1),
                    Size = sizeof(int)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(&arg2),
                    Size = sizeof(int)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(&arg3),
                    Size = sizeof(int)
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)string4Bytes,
                    Size = ((arg4.Length + 1) * 2)
                };
                descrs[4] = new EventData
                {
                    DataPointer = (IntPtr)string5Bytes,
                    Size = ((arg5.Length + 1) * 2)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }
    }
}
