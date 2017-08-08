using System;
using System.Reflection;

namespace Tracing.Tests.Common
{
    public static class TraceControl
    {
        private static MethodInfo m_enableMethod;
        private static MethodInfo m_disableMethod;

        public static void EnableDefault()
        {
            EnableDefault(TimeSpan.FromMilliseconds(1));
        }

        public static void EnableDefault(string outputFile)
        {
            EnableDefault(TimeSpan.FromMilliseconds(1), outputFile);
        }

        public static void EnableDefault(TimeSpan profSampleDelay, string outputFile="default.netperf")
        {
            // Setup the configuration values.
            uint circularBufferMB = 1024; // 1 GB
            uint level = 5; // Verbose

            // Create a new instance of EventPipeConfiguration.
            TraceConfiguration config = new TraceConfiguration(outputFile, circularBufferMB);
            // Setup the provider values.
            // Public provider.
            string providerName = "e13c0d23-ccbc-4e12-931b-d9cc2eee27e4";
            UInt64 keywords = 0x4c14fccbd;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Private provider.
            providerName = "763fd754-7086-4dfe-95eb-c01a46faf4ca";
            keywords = 0x4002000b;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Sample profiler.
            providerName = "3c530d44-97ae-513a-1e6d-783e8f8e03a9";
            keywords = 0x0;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Set the sampling rate.
            config.SetSamplingRate(profSampleDelay);

            // Enable tracing.
            Enable(config);
        }

        public static void Enable(TraceConfiguration traceConfig)
        {
            m_enableMethod.Invoke(
                null,
                new object[]
                {
                    traceConfig.ConfigurationObject
                });
        }

        public static void Disable()
        {
            m_disableMethod.Invoke(
                null,
                null);
        }

        static TraceControl()
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
           Type eventPipeType = SPC.GetType("System.Diagnostics.Tracing.EventPipe");
           if(eventPipeType == null)
           {
               Console.WriteLine("System.Diagnostics.Tracing.EventPipe type == null");
               return false;
           }
           m_enableMethod = eventPipeType.GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Static);
           if(m_enableMethod == null)
           {
               Console.WriteLine("EventPipe.Enable method == null");
               return false;
           }
           m_disableMethod = eventPipeType.GetMethod("Disable", BindingFlags.NonPublic | BindingFlags.Static);
           if(m_disableMethod == null)
           {
               Console.WriteLine("EventPipe.Disable method == null");
               return false;
           }

           return true;
        }

    }
}
