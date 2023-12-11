// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

#if FEATURE_EVENTSOURCE_XPLAT

namespace System.Diagnostics.Tracing
{
    internal sealed partial class XplatEventLogger : EventListener
    {
        public XplatEventLogger() { }

        private static readonly Lazy<string?> eventSourceNameFilter = new Lazy<string?>(() => CompatibilitySwitch.GetValueInternal("EventSourceFilter"));
        private static readonly Lazy<string?> eventSourceEventFilter = new Lazy<string?>(() => CompatibilitySwitch.GetValueInternal("EventNameFilter"));

        private static bool initializedPersistentListener;

        public static EventListener? InitializePersistentListener()
        {
            try
            {
                if (!initializedPersistentListener && XplatEventLogger.IsEventSourceLoggingEnabled())
                {
                    initializedPersistentListener = true;
                    return new XplatEventLogger();
                }
            }
            catch (Exception) { }

            return null;
        }

        [LibraryImport(RuntimeHelpers.QCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsEventSourceLoggingEnabled();

        [LibraryImport(RuntimeHelpers.QCall, StringMarshalling = StringMarshalling.Utf16)]
        private static partial void LogEventSource(int eventID, string? eventName, string eventSourceName, string payload);

        private static readonly List<char> escape_seq = new List<char> { '\b', '\f', '\n', '\r', '\t', '\"', '\\' };
        private static readonly Dictionary<char, string> seq_mapping = new Dictionary<char, string>()
        {
            {'\b', "b"},
            {'\f', "f"},
            {'\n', "n"},
            {'\r', "r"},
            {'\t', "t"},
            {'\"', "\\\""},
            {'\\', "\\\\"}
        };

        private static void MinimalJsonserializer(string payload, ref ValueStringBuilder sb)
        {
            foreach (var elem in payload)
            {
                if (escape_seq.Contains(elem))
                {
                    sb.Append("\\\\");
                    sb.Append(seq_mapping[elem]);
                }
                else
                {
                    sb.Append(elem);
                }
            }
        }

        private static string Serialize(ReadOnlyCollection<string>? payloadName, ReadOnlyCollection<object?>? payload, string? eventMessage)
        {
            if (payloadName == null || payload == null)
                return string.Empty;

            if (payloadName.Count == 0 || payload.Count == 0)
                return string.Empty;

            int eventDataCount = payloadName.Count;

            if (payloadName.Count != payload.Count)
            {
                eventDataCount = Math.Min(payloadName.Count, payload.Count);
            }

            var sb = new ValueStringBuilder(stackalloc char[256]);

            sb.Append('{');

            // If the event has a message, send that as well as a pseudo-field
            if (!string.IsNullOrEmpty(eventMessage))
            {
                sb.Append("\\\"EventSource_Message\\\":\\\"");
                MinimalJsonserializer(eventMessage, ref sb);
                sb.Append("\\\"");
                if (eventDataCount != 0)
                    sb.Append(", ");
            }

            for (int i = 0; i < eventDataCount; i++)
            {
                if (i != 0)
                    sb.Append(", ");

                var fieldstr = payloadName[i].ToString();

                sb.Append("\\\"");
                sb.Append(fieldstr);
                sb.Append("\\\"");
                sb.Append(':');

                switch (payload[i])
                {
                    case string str:
                    {
                        sb.Append("\\\"");
                        MinimalJsonserializer(str, ref sb);
                        sb.Append("\\\"");
                        break;
                    }
                    case byte[] byteArr:
                    {
                        sb.Append("\\\"");
                        AppendByteArrayAsHexString(ref sb, byteArr);
                        sb.Append("\\\"");
                        break;
                    }
                    default:
                    {
                        if (payload[i] is object o)
                        {
                            sb.Append(o.ToString());
                        }
                        break;
                    }
                }
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendByteArrayAsHexString(ref ValueStringBuilder builder, byte[] byteArray)
        {
            Debug.Assert(byteArray != null);

            HexConverter.EncodeToUtf16(byteArray, builder.AppendSpan(byteArray.Length * 2));
        }

        protected internal override void OnEventSourceCreated(EventSource eventSource)
        {

            string? eventSourceFilter = eventSourceNameFilter.Value;
            if (string.IsNullOrEmpty(eventSourceFilter) || (eventSource.Name.Contains(eventSourceFilter, StringComparison.OrdinalIgnoreCase)))
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, null);
            }
        }

        protected internal override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // Don't enable forwarding of NativeRuntimeEventSource events.
            // NativeRuntimeEventSource events are written to the native side via QCalls (see NativeRuntimeEventSource.cs/nativeeventsource.cpp)
            // and is forwarded back to this EventListener via NativeRuntimeEventSource.ProcessEvent. We need to filter these events here
            // because then these events can get logged back into the native runtime as EventSourceEvent events which we use to log
            // managed events from other EventSources to LTTng.
            if (eventData.EventSource.GetType() == typeof(NativeRuntimeEventSource))
            {
                return;
            }

            string? eventFilter = eventSourceEventFilter.Value;
            if (string.IsNullOrEmpty(eventFilter) || (eventData.EventName!.Contains(eventFilter, StringComparison.OrdinalIgnoreCase)))
            {
                LogOnEventWritten(eventData);
            }
        }

        private static void LogOnEventWritten(EventWrittenEventArgs eventData)
        {
            string payload = "";
            if (eventData.Payload != null)
            {
                try
                {
                    payload = Serialize(eventData.PayloadNames, eventData.Payload, eventData.Message);
                }
                catch (Exception ex)
                {
                    payload = "XplatEventLogger failed with Exception " + ex.ToString();
                }
            }

            LogEventSource(eventData.EventId, eventData.EventName, eventData.EventSource.Name, payload);
        }
    }
}
#endif //FEATURE_EVENTSOURCE_XPLAT
