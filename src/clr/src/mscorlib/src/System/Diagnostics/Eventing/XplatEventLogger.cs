using System;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using Contract = System.Diagnostics.Contracts.Contract;

#if FEATURE_EVENTSOURCE_XPLAT

namespace System.Diagnostics.Tracing
{

    internal  class XplatEventLogger : EventListener
    {
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

            Contract.Assert(payloadName.Count == payload.Count);
            if(payloadName.Count != payload.Count)
            {
                return string.Empty;
            }
            
            var sb = StringBuilderCache.Acquire();

            sb.Append('{');
            for (int i = 0; i < payloadName.Count; i++)
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
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, null);
        }

        internal protected  override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            LogOnEventWritten(eventData);
        }

        [System.Security.SecuritySafeCritical]
        private void LogOnEventWritten(EventWrittenEventArgs eventData)
        {
            string payload = "";
            if (eventData.Payload != null)
            {
                payload = Serialize(eventData.PayloadNames, eventData.Payload);
            }

            LogEventSource( eventData.EventId, eventData.EventName,eventData.EventSource.Name,payload);
        }
    }
}
#endif //FEATURE_EVENTSOURCE_XPLAT
