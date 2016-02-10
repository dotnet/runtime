// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private const int AssociateId = 1;
        private const int UriBaseAddressId = 2;
        private const int ContentNullId = 3;
        private const int ClientSendCompletedId = 4;
        private const int HeadersInvalidValueId = 5;
        private const int HandlerMessageId = 6;

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

        [Event(AssociateId, Keywords = Keywords.Default,
            Level = EventLevel.Informational, Message = "[{0}#{1}]<-->[{2}#{3}]")]
        internal unsafe void Associate(string objectA, int objectAHash, string objectB, int objectBHash)
        {
            const int SizeData = 4;
            fixed (char* arg1Ptr = objectA, arg2Ptr = objectB)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (objectA.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(&objectAHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[2].Size = (objectB.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&objectBHash);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(AssociateId, SizeData, dataDesc);
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

        [Event(UriBaseAddressId, Keywords = Keywords.Debug,
            Level = EventLevel.Informational)]
        internal unsafe void UriBaseAddress(string uriBaseAddress, string objName, int objHash)
        {
            const int SizeData = 3;
            fixed (char* arg1Ptr = uriBaseAddress, arg2Ptr = objName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[0].Size = (uriBaseAddress.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (objName.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(&objHash);
                dataDesc[2].Size = sizeof(int);

                WriteEventCore(UriBaseAddressId, SizeData, dataDesc);
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

        [Event(ContentNullId, Keywords = Keywords.Debug,
            Level = EventLevel.Informational)]
        internal void ContentNull(string objName, int objHash)
        {
            WriteEvent(ContentNullId, objName, objHash);
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

        [Event(ClientSendCompletedId, Keywords = Keywords.Debug,
            Level = EventLevel.Verbose)]
        internal unsafe void ClientSendCompleted(int httpRequestMessageHash, int httpResponseMessageHash, string responseString, int httpClientHash)
        {
            const int SizeData = 4;
            fixed (char* arg1Ptr = responseString)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];
                dataDesc[0].DataPointer = (IntPtr)(&httpRequestMessageHash);
                dataDesc[0].Size = sizeof(int);
                dataDesc[1].DataPointer = (IntPtr)(&httpResponseMessageHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg1Ptr);
                dataDesc[2].Size = (responseString.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&httpClientHash);
                dataDesc[3].Size = sizeof(int);

                WriteEventCore(ClientSendCompletedId, SizeData, dataDesc);
            }
        }

        [Event(HeadersInvalidValueId, Keywords = Keywords.Debug,
            Level = EventLevel.Verbose)]
        internal void HeadersInvalidValue(string name, string rawValue)
        {
            WriteEvent(HeadersInvalidValueId, name, rawValue);
        }

        [Event(HandlerMessageId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        internal unsafe void HandlerMessage(int workerId, int requestId, string memberName, string message)
        {
            if (memberName == null)
            {
                memberName = string.Empty;
            }

            if (message == null)
            {
                message = string.Empty;
            }

            const int SizeData = 4;
            fixed (char* memberNamePtr = memberName)
            fixed (char* messagePtr = message)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)(&workerId);
                dataDesc[0].Size = sizeof(int);

                dataDesc[1].DataPointer = (IntPtr)(&requestId);
                dataDesc[1].Size = sizeof(int);

                dataDesc[2].DataPointer = (IntPtr)(memberNamePtr);
                dataDesc[2].Size = (memberName.Length + 1) * sizeof(char);

                dataDesc[3].DataPointer = (IntPtr)(messagePtr);
                dataDesc[3].Size = (message.Length + 1) * sizeof(char);

                WriteEventCore(HandlerMessageId, SizeData, dataDesc);
            }
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }
    }
}
