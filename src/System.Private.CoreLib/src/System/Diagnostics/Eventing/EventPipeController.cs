// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if FEATURE_PERFTRACING
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// Simple out-of-process listener for controlling EventPipe.
    /// The following environment variables are used to configure EventPipe:
    ///  - COMPlus_EnableEventPipe=1 : Enable EventPipe immediately for the life of the process.
    ///  - COMPlus_EventPipeConfig : Provides the configuration in xperf string form for which providers/keywords/levels to be enabled.
    ///                              If not specified, the default configuration is used.
    ///  - COMPlus_EventPipeOutputFile : The full path to the netperf file to be written.
    ///  - COMPlus_EventPipeCircularMB : The size in megabytes of the circular buffer.
    /// </summary>
    internal static class EventPipeController
    {
        // Miscellaneous constants.
        private const string DefaultAppName = "app";
        private const string NetPerfFileExtension = ".netperf";
        private const string NetTraceFileExtension = ".nettrace";
        private const uint DefaultCircularBufferMB = 256; // MB (PerfView and dotnet-trace default)
        private const char ProviderConfigDelimiter = ',';
        private const char ConfigComponentDelimiter = ':';

        // The default set of providers/keywords/levels.  Used if an alternative configuration is not specified.
        private static EventPipeProviderConfiguration[] DefaultProviderConfiguration => new EventPipeProviderConfiguration[]
        {
            new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntime", 0x4c14fccbd, 5, null),
            new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntimePrivate", 0x4002000b, 5, null),
            new EventPipeProviderConfiguration("Microsoft-DotNETCore-SampleProfiler", 0x0, 5, null),
        };

        private static bool IsControllerInitialized { get; set; } = false;

        internal static void Initialize()
        {
            // Don't allow failures to propagate upstream.  Ensure program correctness without tracing.
            try
            {
                if (!IsControllerInitialized)
                {
                    if (Config_EnableEventPipe > 0)
                    {
                        // Enable tracing immediately.
                        // It will be disabled automatically on shutdown.
                        EventPipe.Enable(BuildConfigFromEnvironment());
                    }

                    RuntimeEventSource.Initialize();

                    IsControllerInitialized = true;
                }
            }
            catch { }
        }

        private static EventPipeConfiguration BuildConfigFromEnvironment()
        {
            // Build the full path to the trace file.
            string traceFileName = BuildTraceFileName();
            string outputFilePath = Path.Combine(Config_EventPipeOutputPath, traceFileName);

            // Create a new configuration object.
            EventPipeConfiguration config = new EventPipeConfiguration(
                outputFilePath,
                (Config_NetTraceFormat != 0) ? EventPipeSerializationFormat.NetTrace : EventPipeSerializationFormat.NetPerf,
                Config_EventPipeCircularMB);

            // Get the configuration.
            string? strConfig = Config_EventPipeConfig;
            if (!string.IsNullOrEmpty(strConfig))
            {
                // If the configuration is specified, parse it and save it to the config object.
                SetProviderConfiguration(strConfig, config);
            }
            else
            {
                // Specify the default configuration.
                config.EnableProviderRange(DefaultProviderConfiguration);
            }

            return config;
        }

        private static string BuildTraceFileName()
        {
            return GetAppName() + "." + Interop.GetCurrentProcessId().ToString() +
              ((Config_NetTraceFormat != 0) ? NetTraceFileExtension : NetPerfFileExtension);
        }

        private static string GetAppName()
        {
            string? appName = null;
            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                AssemblyName? assemblyName = entryAssembly.GetName();
                if (assemblyName != null)
                {
                    appName = assemblyName.Name;
                }
            }

            if (string.IsNullOrEmpty(appName))
            {
                appName = DefaultAppName;
            }

            return appName;
        }

        private static void SetProviderConfiguration(string strConfig, EventPipeConfiguration config)
        {
            if (string.IsNullOrEmpty(strConfig))
            {
                throw new ArgumentNullException(nameof(strConfig));
            }

            // Provider format: "(GUID|KnownProviderName)[:Flags[:Level][:KeyValueArgs]]"
            // where KeyValueArgs are of the form: "[key1=value1][;key2=value2]"
            // `strConfig` must be of the form "Provider[,Provider]"
            string[] providers = strConfig.Split(
                ProviderConfigDelimiter,
                StringSplitOptions.RemoveEmptyEntries); // Remove "empty" providers.
            foreach (string provider in providers)
            {
                // Split expecting a maximum of four tokens.
                string[] components = provider.Split(
                    ConfigComponentDelimiter,
                    4, // if there is ':' in the parameters then anything after it will not be ignored.
                    StringSplitOptions.None); // Keep empty tokens

                string? providerName = components.Length > 0 ? components[0] : null;
                if (string.IsNullOrEmpty(providerName))
                    continue;  // No provider name specified.

                ulong keywords = ulong.MaxValue;
                if (components.Length > 1)
                {
                    // We use a try/catch block here because ulong.TryParse won't accept 0x at the beginning
                    // of a hex string.  Thus, we either need to conditionally strip it or handle the exception.
                    // Given that this is not a perf-critical path, catching the exception is the simpler code.
                    try
                    {
                        keywords = Convert.ToUInt64(components[1], 16);
                    }
                    catch
                    {
                    }
                }

                uint level = 5; // Verbose
                if (components.Length > 2)
                {
                    uint.TryParse(components[2], out level);
                }

                string? filterData = components.Length > 3 ? components[3] : null;

                config.EnableProviderWithFilter(providerName, keywords, level, filterData);
            }
        }

        /// <summary>
        /// Returns -1 if the EnableEventPipe environment variable is not set at all (or is illegal)
        /// </summary>
        private static int Config_EnableEventPipe
        {
            get
            {
                string? stringValue = CompatibilitySwitch.GetValueInternal("EnableEventPipe");
                if ((stringValue == null) || (!int.TryParse(stringValue, out int value)))
                {
                    value = -1;     // Indicates no value (or is illegal)
                }

                return value;
            }
        }

        private static int Config_NetTraceFormat
        {
            get
            {
                string? stringValue = CompatibilitySwitch.GetValueInternal("EventPipeNetTraceFormat");
                if ((stringValue == null) || (!int.TryParse(stringValue, out int value)))
                {
                    value = -1;     // Indicates no value (or is illegal)
                }

                return value;
            }
        }

        private static string? Config_EventPipeConfig => CompatibilitySwitch.GetValueInternal("EventPipeConfig");

        private static uint Config_EventPipeCircularMB
        {
            get
            {
                string? stringValue = CompatibilitySwitch.GetValueInternal("EventPipeCircularMB");
                if ((stringValue == null) || (!uint.TryParse(stringValue, out uint value)))
                {
                    value = DefaultCircularBufferMB;
                }

                return value;
            }
        }

        private static string Config_EventPipeOutputPath
        {
            get
            {
                string? stringValue = CompatibilitySwitch.GetValueInternal("EventPipeOutputPath");
                if (stringValue == null)
                {
                    stringValue = ".";
                }

                return stringValue;
            }
        }
    }
}

#endif // FEATURE_PERFTRACING
