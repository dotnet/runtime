// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Http;

namespace System.Net
{
    //TODO: If localization resources are not found, logging does not work. Issue #5126.
    [EventSource(Name = "Microsoft-System-Net-Http", LocalizationResources = "FxResources.System.Net.Http.SR")]
    internal sealed partial class NetEventSource : EventSource
    {
        private const int UriBaseAddressId = NextAvailableEventId;
        private const int ContentNullId = UriBaseAddressId + 1;
        private const int ClientSendCompletedId = ContentNullId + 1;
        private const int HeadersInvalidValueId = ClientSendCompletedId + 1;
        private const int HandlerMessageId = HeadersInvalidValueId + 1;
        private const int AuthenticationInfoId = HandlerMessageId + 1;
        private const int AuthenticationErrorId = AuthenticationInfoId + 1;
        private const int HandlerErrorId = AuthenticationErrorId + 1;

        [NonEvent]
        public static void UriBaseAddress(object obj, Uri baseAddress)
        {
            Debug.Assert(IsEnabled);
            Log.UriBaseAddress(baseAddress?.ToString(), IdOf(obj), GetHashCode(obj));
        }

        [Event(UriBaseAddressId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private unsafe void UriBaseAddress(string uriBaseAddress, string objName, int objHash) =>
            WriteEvent(UriBaseAddressId, uriBaseAddress, objName, objHash);

        [NonEvent]
        public static void ContentNull(object obj)
        {
            Debug.Assert(IsEnabled);
            Log.ContentNull(IdOf(obj), GetHashCode(obj));
        }

        [Event(ContentNullId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void ContentNull(string objName, int objHash) =>
            WriteEvent(ContentNullId, objName, objHash);

        [NonEvent]
        public static void ClientSendCompleted(HttpClient httpClient, HttpResponseMessage response, HttpRequestMessage request)
        {
            Debug.Assert(IsEnabled);
            Log.ClientSendCompleted(response?.ToString(), GetHashCode(request), GetHashCode(response), GetHashCode(httpClient));
        }

        [Event(ClientSendCompletedId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void ClientSendCompleted(string responseString, int httpRequestMessageHash, int httpResponseMessageHash, int httpClientHash) =>
            WriteEvent(ClientSendCompletedId, responseString, httpRequestMessageHash, httpResponseMessageHash, httpClientHash);

        [Event(HeadersInvalidValueId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        public void HeadersInvalidValue(string name, string rawValue) =>
            WriteEvent(HeadersInvalidValueId, name, rawValue);

        [Event(HandlerMessageId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void HandlerMessage(int poolId, int workerId, int requestId, string memberName, string message) =>
            WriteEvent(HandlerMessageId, poolId, workerId, requestId, memberName, message);
            //Console.WriteLine($"{poolId}/{workerId}/{requestId}: ({memberName}): {message}");  // uncomment for debugging only

        [Event(HandlerErrorId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        public void HandlerMessageError(int poolId, int workerId, int requestId, string memberName, string message) =>
            WriteEvent(HandlerErrorId, poolId, workerId, requestId, memberName, message);
            //Console.WriteLine($"{poolId}/{workerId}/{requestId}: ({memberName}): {message}");  // uncomment for debugging only

        [NonEvent]
        public static void AuthenticationInfo(Uri uri, string message)
        {
            Debug.Assert(IsEnabled);
            Log.AuthenticationInfo(uri?.ToString(), message);
        }

        [Event(AuthenticationInfoId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void AuthenticationInfo(string uri, string message) =>
            WriteEvent(AuthenticationInfoId, uri, message);

        [NonEvent]
        public static void AuthenticationError(Uri uri, string message)
        {
            Debug.Assert(IsEnabled);
            Log.AuthenticationError(uri?.ToString(), message);
        }

        [Event(AuthenticationErrorId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        public void AuthenticationError(string uri, string message) =>
            WriteEvent(AuthenticationErrorId, uri, message);

        [NonEvent]
        private unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3, string arg4, string arg5)
        {
            if (IsEnabled())
            {
                if (arg4 == null) arg4 = "";
                if (arg5 == null) arg5 = "";

                fixed (char* string4Bytes = arg4)
                fixed (char* string5Bytes = arg5)
                {
                    const int NumEventDatas = 5;
                    var descrs = stackalloc EventData[NumEventDatas];

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
}
