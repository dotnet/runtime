using System;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace EventPipe.Issue22247
{
    public enum EventPipeSerializationFormat
    {
        NetPerf,
        NetTrace
    }

    public sealed class TraceConfiguration
    {
        private ConstructorInfo m_configurationCtor;
        private MethodInfo m_enableProviderMethod;
        private MethodInfo m_setProfilerSamplingRateMethod;

        private object m_configurationObject;

        public TraceConfiguration(
            string outputFile,
            uint circularBufferMB)
        {
            // Initialize reflection references.
            if (!Initialize())
            {
                throw new InvalidOperationException("Reflection failed.");
            }

            m_configurationObject = m_configurationCtor.Invoke(
                new object[]
                {
                    outputFile,
                    EventPipeSerializationFormat.NetTrace,
                    circularBufferMB
                });
        }

        public void EnableProvider(
            string providerName,
            UInt64 keywords,
            uint level)
        {
            m_enableProviderMethod.Invoke(
                m_configurationObject,
                new object[]
                {
                    providerName,
                    keywords,
                    level
                });
        }

        internal object ConfigurationObject
        {
            get { return m_configurationObject; }
        }

        public void SetSamplingRate(TimeSpan minDelayBetweenSamples)
        {
            m_setProfilerSamplingRateMethod.Invoke(
                m_configurationObject,
                new object[]
                {
                    minDelayBetweenSamples
                });
        }

        private bool Initialize()
        {
            Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
            if (SPC == null)
            {
                Console.WriteLine("System.Private.CoreLib assembly == null");
                return false;
            }

            Type configurationType = SPC.GetType("System.Diagnostics.Tracing.EventPipeConfiguration");
            if (configurationType == null)
            {
                Console.WriteLine("configurationType == null");
                return false;
            }
            Type formatType = SPC.GetType("System.Diagnostics.Tracing.EventPipeSerializationFormat");
            if (formatType == null)
            {
                Console.WriteLine("formatType == null");
                return false;
            }

            m_configurationCtor = configurationType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(string), formatType, typeof(uint) },
                null);
            if (m_configurationCtor == null)
            {
                Console.WriteLine("configurationCtor == null");
                return false;
            }

            m_enableProviderMethod = configurationType.GetMethod(
                "EnableProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_enableProviderMethod == null)
            {
                Console.WriteLine("enableProviderMethod == null");
                return false;
            }

            m_setProfilerSamplingRateMethod = configurationType.GetMethod(
                "SetProfilerSamplingRate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_setProfilerSamplingRateMethod == null)
            {
                Console.WriteLine("setProfilerSamplingRate == null");
                return false;
            }

            return true;
        }
    }

    class Program
    {
        private static MethodInfo m_enableMethod;
        private static MethodInfo m_disableMethod;

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

        static int Main(string[] args)
        {
            TimeSpan profSampleDelay = TimeSpan.FromMilliseconds(1);
            string outputFile = "default.netperf";

            Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
            Type eventPipeType = SPC.GetType("System.Diagnostics.Tracing.EventPipe");
            m_enableMethod = eventPipeType.GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Static);
            m_disableMethod = eventPipeType.GetMethod("Disable", BindingFlags.NonPublic | BindingFlags.Static);

            // Setup the configuration values.
            uint circularBufferMB = 1024; // 1 GB
            uint level = 5; // Verbose

            // Create a new instance of EventPipeConfiguration.
            TraceConfiguration config = new TraceConfiguration(outputFile, circularBufferMB);
            // Setup the provider values.
            // Public provider.
            string providerName = "Microsoft-Windows-DotNETRuntime";
            UInt64 keywords = 0x4c14fccbd;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Private provider.
            providerName = "Microsoft-Windows-DotNETRuntimePrivate";
            keywords = 0x4002000b;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Sample profiler.
            providerName = "Microsoft-DotNETCore-SampleProfiler";
            keywords = 0x0;

            // Enable the provider.
            config.EnableProvider(providerName, keywords, level);

            // Set the sampling rate.
            config.SetSamplingRate(profSampleDelay);

            // Enable tracing.
            Enable(config);
            Disable();
            Enable(config);

            return 100;
        }
    }
}
