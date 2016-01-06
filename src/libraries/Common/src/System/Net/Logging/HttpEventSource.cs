// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Globalization;

namespace System.Net
{
    //TODO: If localization resources are not found, logging does not work. Issue #5126.
    [EventSource(Name = "Microsoft-System-Net-Http",
        Guid = "bdd9a83e-1929-5482-0d73-2fe5e1c0e16d",
        LocalizationResources = "FxResources.System.Net.Http.SR")]
    internal sealed class HttpEventSource : EventSource
    {
        private const int ASSOCIATE_ID = 1;
        private const int URI_BASE_ADDRESS_ID = 2;
        private const int CONTENT_NULL_ID = 3;
        private const int CLIENT_SEND_COMPLETED = 4;
        private const int HEADERS_INVALID_VALUE_ID = 5;

        private readonly static HttpEventSource s_log = new HttpEventSource();
        private HttpEventSource() { }
        public static HttpEventSource Log
        {
            get
            {
                return s_log;
            }
        }

        [NonEvent]
        internal static void Associate(object objA, object objB)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            s_log.Associate(LoggingHash.GetObjectName(objA),
                            LoggingHash.HashInt(objA),
                            LoggingHash.GetObjectName(objB),
                            LoggingHash.HashInt(objB));
        }

        [Event(ASSOCIATE_ID, Keywords = Keywords.Default,
            Level = EventLevel.Informational, Message = "[{0}#{1}]<-->[{2}#{3}]")]
        internal unsafe void Associate(string objectA, int objectAHash, string objectB, int objectBHash)
        {
            const int SIZEDATA = 4;
            fixed (char* arg1Ptr = objectA, arg2Ptr = objectB)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];

                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (objectA.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(&objectAHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[2].Size = (objectB.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&objectBHash);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(ASSOCIATE_ID, SIZEDATA, dataDesc);
            }
        }

        [NonEvent]
        internal static void UriBaseAddress(object obj, string baseAddress)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            s_log.UriBaseAddress(baseAddress, LoggingHash.GetObjectName(obj), LoggingHash.HashInt(obj));
        }

        [Event(URI_BASE_ADDRESS_ID, Keywords = Keywords.Debug,
            Level = EventLevel.Informational)]
        internal unsafe void UriBaseAddress(string uriBaseAddress, string objName, int objHash)
        {
            const int SIZEDATA = 3;
            fixed (char* arg1Ptr = uriBaseAddress, arg2Ptr = objName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];

                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (uriBaseAddress.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (objName.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&objHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(URI_BASE_ADDRESS_ID, SIZEDATA, dataDesc);
            }
        }

        [NonEvent]
        internal static void ContentNull(object obj)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            s_log.ContentNull(LoggingHash.GetObjectName(obj), LoggingHash.HashInt(obj));
        }

        [Event(CONTENT_NULL_ID, Keywords = Keywords.Debug,
            Level = EventLevel.Informational)]
        internal void ContentNull(string objName, int objHash)
        {
            WriteEvent(CONTENT_NULL_ID, objName, objHash);
        }

        [NonEvent]
        internal static void ClientSendCompleted(HttpClient httpClient, HttpResponseMessage response, HttpRequestMessage request)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            string responseString = "";
            if (response != null)
            {
                responseString = response.ToString();
            }
            s_log.ClientSendCompleted(LoggingHash.HashInt(request), LoggingHash.HashInt(response), responseString, LoggingHash.HashInt(httpClient));
        }

        [Event(CLIENT_SEND_COMPLETED, Keywords = Keywords.Debug,
            Level = EventLevel.Verbose)]
        internal unsafe void ClientSendCompleted(int httpRequestMessageHash, int httpResponseMessageHash, string responseString, int httpClientHash)
        {
            const int SIZEDATA = 4;
            fixed (char* arg1Ptr = responseString)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];
                dataDesc[0].DataPointer = (IntPtr)(&httpRequestMessageHash);
                dataDesc[0].Size = sizeof(int);
                dataDesc[1].DataPointer = (IntPtr)(&httpResponseMessageHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[2].Size = (responseString.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&httpClientHash);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(CLIENT_SEND_COMPLETED, SIZEDATA, dataDesc);
            }
        }

        [Event(HEADERS_INVALID_VALUE_ID, Keywords = Keywords.Debug,
            Level = EventLevel.Verbose)]
        internal void HeadersInvalidValue(string name, string rawValue)
        {
            WriteEvent(HEADERS_INVALID_VALUE_ID, name, rawValue);
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
