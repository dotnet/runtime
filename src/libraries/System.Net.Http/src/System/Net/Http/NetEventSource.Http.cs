// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [NonEvent]
        public static void UriBaseAddress(object obj, Uri baseAddress)
        {
            if (IsEnabled)
            {
                Log.UriBaseAddress(baseAddress?.ToString(), IdOf(obj), GetHashCode(obj));
            }
        }

        [Event(UriBaseAddressId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private unsafe void UriBaseAddress(string uriBaseAddress, string objName, int objHash) =>
            WriteEvent(UriBaseAddressId, uriBaseAddress, objName, objHash);

        [NonEvent]
        public static void ContentNull(object obj)
        {
            if (IsEnabled)
            {
                Log.ContentNull(IdOf(obj), GetHashCode(obj));
            }
        }

        [Event(ContentNullId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void ContentNull(string objName, int objHash) =>
            WriteEvent(ContentNullId, objName, objHash);

        [NonEvent]
        public static void ClientSendCompleted(HttpClient httpClient, HttpResponseMessage response, HttpRequestMessage request)
        {
            if (IsEnabled)
            {
                Log.ClientSendCompleted(response?.ToString(), GetHashCode(request), GetHashCode(response), GetHashCode(httpClient));
            }
        }

        [Event(ClientSendCompletedId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void ClientSendCompleted(string responseString, int httpRequestMessageHash, int httpResponseMessageHash, int httpClientHash) =>
            WriteEvent(ClientSendCompletedId, responseString, httpRequestMessageHash, httpResponseMessageHash, httpClientHash);

        [Event(HeadersInvalidValueId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void HeadersInvalidValue(string name, string rawValue) =>
            WriteEvent(HeadersInvalidValueId, name, rawValue);

        [Event(HandlerMessageId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        public void HandlerMessage(int handlerId, int workerId, int requestId, string memberName, string message) =>
            WriteEvent(HandlerMessageId, handlerId, workerId, requestId, memberName, message);

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

                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = sizeof(int);

                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = sizeof(int);

                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = sizeof(int);

                    descrs[3].DataPointer = (IntPtr)string4Bytes;
                    descrs[3].Size = ((arg4.Length + 1) * 2);

                    descrs[4].DataPointer = (IntPtr)string5Bytes;
                    descrs[4].Size = ((arg5.Length + 1) * 2);

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }
    }
}
