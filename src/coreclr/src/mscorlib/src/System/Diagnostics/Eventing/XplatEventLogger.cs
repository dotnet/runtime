using System;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Contract = System.Diagnostics.Contracts.Contract;

#if FEATURE_EVENTSOURCE_XPLAT

namespace System.Diagnostics.Tracing
{

    internal  class XplatEventLogger : EventListener
    {
        private static Lazy<string> eventSourceNameFilter = new Lazy<string>(() => CompatibilitySwitch.GetValueInternal("EventSourceFilter"));
        private static Lazy<string> eventSourceEventFilter = new Lazy<string>(() => CompatibilitySwitch.GetValueInternal("EventNameFilter"));
        
        public XplatEventLogger() {}

        private static bool initializedPersistentListener = false;

        [System.Security.SecuritySafeCritical]
        public static EventListener InitializePersistentListener()
        {
            try{

                if (!initializedPersistentListener && XplatEventLogger.IsEventSourceLoggingEnabled())
                {
                    initializedPersistentListener = true;
                    return new XplatEventLogger();
                }
            }
            catch(Exception){}

            return null;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool IsEventSourceLoggingEnabled();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void LogEventSource(int eventID, string eventName, string eventSourceName, string payload);

        static List<char> escape_seq = new List<char> { '\b', '\f', '\n', '\r', '\t', '\"', '\\' };
        static Dictionary<char, string> seq_mapping = new Dictionary<char, string>()
        {
            {'\b', "b"},
            {'\f', "f"},
            {'\n', "n"},
            {'\r', "r"},
            {'\t', "t"},
            {'\"', "\\\""},
            {'\\', "\\\\"}
        };

        private static void minimalJsonserializer(string payload, StringBuilder sb)
        {
            foreach( var elem in payload)
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

        private static string Serialize(ReadOnlyCollection<string> payloadName, ReadOnlyCollection<object> payload, string sep = ", ")
        {

            if (payloadName == null || payload == null )
                return String.Empty;

            if (payloadName.Count == 0 || payload.Count == 0)
                return String.Empty;

            int eventDataCount = payloadName.Count;

            if(payloadName.Count != payload.Count)
            {
               eventDataCount = Math.Min(payloadName.Count, payload.Count);
            }

            var sb = StringBuilderCache.Acquire();

            sb.Append('{');
            for (int i = 0; i < eventDataCount; i++)
            {
                var fieldstr = payloadName[i].ToString();

                sb.Append("\\\"");
                sb.Append(fieldstr);
                sb.Append("\\\"");
                sb.Append(':');

                var valuestr = payload[i] as string;

                if( valuestr != null)
                {
                    sb.Append("\\\"");
                    minimalJsonserializer(valuestr,sb);
                    sb.Append("\\\"");
                }
                else
                {
                    sb.Append(payload[i].ToString());
                }

                sb.Append(sep);

            }

             sb.Length -= sep.Length;
             sb.Append('}');

             return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal protected  override void OnEventSourceCreated(EventSource eventSource)
        {
            string eventSourceFilter = eventSourceNameFilter.Value;
            if (String.IsNullOrEmpty(eventSourceFilter) || (eventSource.Name.IndexOf(eventSourceFilter, StringComparison.OrdinalIgnoreCase) >= 0))
            {   
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, null);
            }
        }

        internal protected  override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            string eventFilter = eventSourceEventFilter.Value;
            if (String.IsNullOrEmpty(eventFilter) || (eventData.EventName.IndexOf(eventFilter, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                LogOnEventWritten(eventData);
            }
        }

        [System.Security.SecuritySafeCritical]
        private void LogOnEventWritten(EventWrittenEventArgs eventData)
        {
            string payload = "";
            if (eventData.Payload != null)
            {
                try{
                    payload = Serialize(eventData.PayloadNames, eventData.Payload);
                }
                catch (Exception ex)
                {
                    payload = "XplatEventLogger failed with Exception " + ex.ToString();
                }
            }

            LogEventSource( eventData.EventId, eventData.EventName,eventData.EventSource.Name,payload);
        }
    }
}
#endif //FEATURE_EVENTSOURCE_XPLAT
