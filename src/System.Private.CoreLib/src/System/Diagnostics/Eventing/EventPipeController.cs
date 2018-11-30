// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if FEATURE_PERFTRACING
using Internal.IO;
using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// Simple out-of-process listener for controlling EventPipe.
    /// The following environment variables are used to configure EventPipe:
    ///  - COMPlus_EnableEventPipe=1 : Enable EventPipe immediately for the life of the process.
    ///  - COMPlus_EnableEventPipe=4 : Enables this controller and creates a thread to listen for enable/disable events.
    ///  - COMPlus_EventPipeConfig : Provides the configuration in xperf string form for which providers/keywords/levels to be enabled.
    ///                              If not specified, the default configuration is used.
    ///  - COMPlus_EventPipeOutputFile : The full path to the netperf file to be written.
    ///  - COMPlus_EventPipeCircularMB : The size in megabytes of the circular buffer.
    /// Once the configuration is set and this controller is enabled, tracing is enabled by creating a marker file that this controller listens for.
    /// Tracing is disabled by deleting the marker file.  The marker file is the target trace file path with ".ctl" appended to it.  For example,
    /// if the trace file is /path/to/trace.netperf then the marker file is /path/to/trace.netperf.ctl.
    /// This listener does not poll very often, and thus takes time to enable and disable tracing.  This is by design to ensure that the listener does
    /// not starve other threads on the system.
    /// NOTE: If COMPlus_EnableEventPipe != 4 then this listener is not created and does not add any overhead to the process.
    /// </summary>
    internal sealed class EventPipeController
    {
        // Miscellaneous constants.
        private const string DefaultAppName = "app";
        private const string NetPerfFileExtension = ".netperf";
        private const string ConfigFileSuffix = ".eventpipeconfig";
        private const int EnabledPollingIntervalMilliseconds = 1000; // 1 second
        private const int DisabledPollingIntervalMilliseconds = 5000; // 5 seconds
        private const uint DefaultCircularBufferMB = 1024; // 1 GB
        private static readonly char[] ProviderConfigDelimiter = new char[] { ',' };
        private static readonly char[] ConfigComponentDelimiter = new char[] { ':' };
        private static readonly string[] ConfigFileLineDelimiters = new string[] { "\r\n", "\n" };
        private const char ConfigEntryDelimiter = '=';

        // Config file keys.
        private const string ConfigKey_Providers = "Providers";
        private const string ConfigKey_CircularMB = "CircularMB";
        private const string ConfigKey_OutputPath = "OutputPath";
        private const string ConfigKey_ProcessID = "ProcessID";
        private const string ConfigKey_MultiFileSec = "MultiFileSec";

        // The default set of providers/keywords/levels.  Used if an alternative configuration is not specified.
        private static readonly EventPipeProviderConfiguration[] DefaultProviderConfiguration = new EventPipeProviderConfiguration[]
        {
            new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntime", 0x4c14fccbd, 5, null),
            new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntimePrivate", 0x4002000b, 5, null),
            new EventPipeProviderConfiguration("Microsoft-DotNETCore-SampleProfiler", 0x0, 5, null),
        };

        // Singleton controller instance.
        private static EventPipeController s_controllerInstance = null;

        // Initialization flag used to avoid initializing FrameworkEventSource on the startup path.
        private static bool s_initializing = false;

        // Controller object state.
        private Timer m_timer;
        private string m_configFilePath;
        private DateTime m_configFileUpdateTime;
        private string m_traceFilePath = null;
        private bool m_configFileExists = false;

        internal static bool Initializing
        {
            get { return s_initializing; }
        }

        internal static void Initialize()
        {
            // Don't allow failures to propagate upstream.  Ensure program correctness without tracing.
            try
            {
                s_initializing = true;

                if (s_controllerInstance == null)
                {
                    if (Config_EnableEventPipe > 0)
                    {
                        // Enable tracing immediately.
                        // It will be disabled automatically on shutdown.
                        EventPipe.Enable(BuildConfigFromEnvironment());
                    }
                    else
                    {
                        // Create a new controller to listen for commands.
                        s_controllerInstance = new EventPipeController();
                    }
                }
            }
            catch { }
            finally
            {
                s_initializing = false;
            }
        }

        private EventPipeController()
        {
            // Set the config file path.
            m_configFilePath = Path.Combine(AppContext.BaseDirectory, BuildConfigFileName());

            // Initialize the timer, but don't set it to run.
            // The timer will be set to run each time PollForTracingCommand is called.
            m_timer = new Timer(
                callback: new TimerCallback(PollForTracingCommand),
                state: null,
                dueTime: Timeout.Infinite,
                period: Timeout.Infinite,
                flowExecutionContext: false);

            // Trigger the first poll operation on the start-up path.
            PollForTracingCommand(null);
        }

        private void PollForTracingCommand(object state)
        {
            // Make sure that any transient errors don't cause the listener thread to exit.
            try
            {
                // Check for existence of the config file.
                // If the existence of the file has changed since the last time we checked or the update time has changed
                // this means that we need to act on that change.
                bool fileExists = File.Exists(m_configFilePath);
                if (m_configFileExists != fileExists)
                {
                    // Save the result.
                    m_configFileExists = fileExists;

                    // Take the appropriate action.
                    if (fileExists)
                    {
                        // Enable tracing.
                        // Check for null here because it's possible that the configuration contains a process filter
                        // that doesn't match the current process.  IF this occurs, we should't enable tracing.
                        EventPipeConfiguration config = BuildConfigFromFile(m_configFilePath);
                        if (config != null)
                        {
                            EventPipe.Enable(config);
                        }
                    }
                    else
                    {
                        // Disable tracing.
                        EventPipe.Disable();
                    }
                }

                // Schedule the timer to run again.
                m_timer.Change(fileExists ? EnabledPollingIntervalMilliseconds : DisabledPollingIntervalMilliseconds, Timeout.Infinite);
            }
            catch { }
        }

        private static EventPipeConfiguration BuildConfigFromFile(string configFilePath)
        {
            // Read the config file in once call.
            byte[] configContents = File.ReadAllBytes(configFilePath);

            // Convert the contents to a string.
            string strConfigContents = Encoding.UTF8.GetString(configContents);

            // Read all of the config options.
            string outputPath = null;
            string strProviderConfig = null;
            string strCircularMB = null;
            string strProcessID = null;
            string strMultiFileSec = null;

            // Split the configuration entries by line.
            string[] configEntries = strConfigContents.Split(ConfigFileLineDelimiters, StringSplitOptions.RemoveEmptyEntries);
            foreach (string configEntry in configEntries)
            {
                //`Split the key and value by '='.
                string[] entryComponents = configEntry.Split(
                    ConfigEntryDelimiter,
                    2,  // Stop split on first occurrence of the separator.
                    StringSplitOptions.RemoveEmptyEntries);
                if (entryComponents.Length == 2)
                {
                    string key = entryComponents[0];
                    if (key.Equals(ConfigKey_Providers))
                    {
                        strProviderConfig = entryComponents[1];
                    }
                    else if (key.Equals(ConfigKey_OutputPath))
                    {
                        outputPath = entryComponents[1];
                    }
                    else if (key.Equals(ConfigKey_CircularMB))
                    {
                        strCircularMB = entryComponents[1];
                    }
                    else if (key.Equals(ConfigKey_ProcessID))
                    {
                        strProcessID = entryComponents[1];
                    }
                    else if (key.Equals(ConfigKey_MultiFileSec))
                    {
                        strMultiFileSec = entryComponents[1];
                    }
                }
            }

            // Check the process ID filter if it is set.
            if (!string.IsNullOrEmpty(strProcessID))
            {
                // If set, bail out early if the specified process does not match the current process.
                int processID = Convert.ToInt32(strProcessID);
                if (processID != Win32Native.GetCurrentProcessId())
                {
                    return null;
                }
            }

            // Ensure that the output path is set.
            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            // Check to see if MultiFileSec is specified.
            ulong multiFileSec = 0;
            if (!string.IsNullOrEmpty(strMultiFileSec))
            {
                multiFileSec = Convert.ToUInt64(strMultiFileSec);
            }

            // Build the full path to the trace file.
            string traceFileName = BuildTraceFileName();
            string outputFile = Path.Combine(outputPath, traceFileName);

            // Get the circular buffer size.
            uint circularMB = DefaultCircularBufferMB;
            if (!string.IsNullOrEmpty(strCircularMB))
            {
                circularMB = Convert.ToUInt32(strCircularMB);
            }

            // Initialize a new configuration object.
            EventPipeConfiguration config = new EventPipeConfiguration(outputFile, circularMB);
            config.SetMultiFileTraceLength(multiFileSec);

            // Set the provider configuration if specified.
            if (!string.IsNullOrEmpty(strProviderConfig))
            {
                SetProviderConfiguration(strProviderConfig, config);
            }
            else
            {
                // If the provider configuration isn't specified, use the default.
                config.EnableProviderRange(DefaultProviderConfiguration);
            }

            return config;
        }

        private static EventPipeConfiguration BuildConfigFromEnvironment()
        {
            // Build the full path to the trace file.
            string traceFileName = BuildTraceFileName();
            string outputFilePath = Path.Combine(Config_EventPipeOutputPath, traceFileName);

            // Create a new configuration object.
            EventPipeConfiguration config = new EventPipeConfiguration(
                outputFilePath,
                Config_EventPipeCircularMB);

            // Get the configuration.
            string strConfig = Config_EventPipeConfig;
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

        private static string BuildConfigFileName()
        {
            return GetAppName() + ConfigFileSuffix;
        }

        private static string BuildTraceFileName()
        {
            return GetAppName() + "." + Win32Native.GetCurrentProcessId() + NetPerfFileExtension;
        }

        private static string GetAppName()
        {
            string appName = null;
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                AssemblyName assemblyName = entryAssembly.GetName();
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

                string providerName = components.Length > 0 ? components[0] : null;
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

                string filterData = components.Length > 3 ? components[3] : null;

                config.EnableProviderWithFilter(providerName, keywords, level, filterData);
            }
        }

        #region Configuration

        // Cache for COMPlus configuration variables.
        private static int s_Config_EnableEventPipe = -1;
        private static string s_Config_EventPipeConfig = null;
        private static uint s_Config_EventPipeCircularMB = 0;
        private static string s_Config_EventPipeOutputPath = null;

        private static int Config_EnableEventPipe
        {
            get
            {
                if (s_Config_EnableEventPipe == -1)
                {
                    string strEnabledValue = CompatibilitySwitch.GetValueInternal("EnableEventPipe");
                    if ((strEnabledValue == null) || (!int.TryParse(strEnabledValue, out s_Config_EnableEventPipe)))
                    {
                        s_Config_EnableEventPipe = 0;
                    }
                }

                return s_Config_EnableEventPipe;
            }
        }

        private static string Config_EventPipeConfig
        {
            get
            {
                if (s_Config_EventPipeConfig == null)
                {
                    s_Config_EventPipeConfig = CompatibilitySwitch.GetValueInternal("EventPipeConfig");
                }

                return s_Config_EventPipeConfig;
            }
        }

        private static uint Config_EventPipeCircularMB
        {
            get
            {
                if (s_Config_EventPipeCircularMB == 0)
                {
                    string strCircularMB = CompatibilitySwitch.GetValueInternal("EventPipeCircularMB");
                    if ((strCircularMB == null) || (!uint.TryParse(strCircularMB, out s_Config_EventPipeCircularMB)))
                    {
                        s_Config_EventPipeCircularMB = DefaultCircularBufferMB;
                    }
                }

                return s_Config_EventPipeCircularMB;
            }
        }

        private static string Config_EventPipeOutputPath
        {
            get
            {
                if (s_Config_EventPipeOutputPath == null)
                {
                    s_Config_EventPipeOutputPath = CompatibilitySwitch.GetValueInternal("EventPipeOutputPath");
                    if (s_Config_EventPipeOutputPath == null)
                    {
                        s_Config_EventPipeOutputPath = ".";
                    }
                }

                return s_Config_EventPipeOutputPath;
            }
        }

        #endregion Configuration
    }
}

#endif // FEATURE_PERFTRACING
