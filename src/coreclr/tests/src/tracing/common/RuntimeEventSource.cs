using System;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace Tracing.Tests.Common
{
    public static class RuntimeEventSource
    {
        private static FieldInfo m_staticLogField;

        public static EventSource Log
        {
            get
            {
                return (EventSource) m_staticLogField.GetValue(null);
            }
        }

        static RuntimeEventSource()
        {
            if(!Initialize())
            {
                throw new InvalidOperationException("Reflection failed.");
            }
        }

        private static bool Initialize()
        {
           Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
           if(SPC == null)
           {
               Console.WriteLine("System.Private.CoreLib assembly == null");
               return false;
           }
           Type runtimeEventSourceType = SPC.GetType("System.Diagnostics.Tracing.RuntimeEventSource");
           if(runtimeEventSourceType == null)
           {
               Console.WriteLine("System.Diagnostics.Tracing.RuntimeEventSource type == null");
               return false;
           }
           m_staticLogField = runtimeEventSourceType.GetField("Log", BindingFlags.NonPublic | BindingFlags.Static);
           if(m_staticLogField == null)
           {
               Console.WriteLine("RuntimeEventSource.Log field == null");
               return false;
           }

           return true;
        }

    }
}
