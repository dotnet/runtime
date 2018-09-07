// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if FEATURE_PERFTRACING
using Internal.IO;
using Microsoft.Win32;
using System.IO;
using System.Globalization;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

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
        private const string NetPerfFileExtension = ".netperf";
        private const string MarkerFileExtension = ".ctl";
        private const int EnabledPollingIntervalMilliseconds = 1000; // 1 second
        private const int DisabledPollingIntervalMilliseconds = 10000; // 10 seconds
        private const uint DefaultCircularBufferMB = 1024; // 1 GB
        private static readonly char[] ProviderConfigDelimiter = new char[] { ',' };
        private static readonly char[] ConfigComponentDelimiter = new char[] { ':' };

        // The default set of providers/keywords/levels.  Used if an alternative configuration is not specified.
        private static readonly EventPipeProviderConfiguration[] DefaultProviderConfiguration = new EventPipeProviderConfiguration[]
        {
            new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntime", 0x4c14fccbd, 5),
            new EventPipeProviderConfiguration("Microsoft-Windows-DotNETRuntimePrivate", 0x4002000b, 5),
            new EventPipeProviderConfiguration("Microsoft-DotNETCore-SampleProfiler", 0x0, 5)
        };

        // Cache for COMPlus configuration variables.
        private static int s_Config_EnableEventPipe = -1;
        private static string s_Config_EventPipeConfig = null;
        private static uint s_Config_EventPipeCircularMB = 0;
        private static string s_Config_EventPipeOutputFile = null;

        // Singleton controller instance.
        private static EventPipeController s_controllerInstance = null;

        // Controller object state.
        private Timer m_timer;
        private string m_traceFilePath = null;
        private string m_markerFilePath = null;
        private bool m_markerFileExists = false;

        internal static void Initialize()
        {
            // Don't allow failures to propagate upstream.
            // Instead, ensure program correctness without tracing.
            try
            {
                if (s_controllerInstance == null)
                {
                    if(Config_EnableEventPipe == 4)
                    {
                        // Create a new controller to listen for commands.
                        s_controllerInstance = new EventPipeController();
                    }
                    else if (Config_EnableEventPipe > 0)
                    {
                        // Enable tracing immediately.
                        // It will be disabled automatically on shutdown.
                        EventPipe.Enable(GetConfiguration());
                    }
                }
            }
            catch { }
        }

        private EventPipeController()
        {
            // Initialize the timer to run once.  The timer will re-schedule itself after each poll operation.
            // This is done to ensure that there aren't multiple concurrent polling operations when an operation
            // takes longer than the polling interval (e.g. on disable/rundown).
            m_timer = new Timer(
                callback: new TimerCallback(PollForTracingCommand),
                state: null,
                dueTime: DisabledPollingIntervalMilliseconds,
                period: Timeout.Infinite,
                flowExecutionContext: false);
        }

        private void PollForTracingCommand(object state)
        {
            // Make sure that any transient errors don't cause the listener thread to exit.
            try
            {
                // Perform initialization when the timer fires for the first time.
                if (m_traceFilePath == null)
                {
                    // Set file paths.
                    m_traceFilePath = GetDisambiguatedTraceFilePath(Config_EventPipeOutputFile);
                    m_markerFilePath = MarkerFilePath;

                    // Marker file is assumed to not exist.
                    // This will be updated when the monitoring thread starts.
                    m_markerFileExists = false;
                }

                // Check for existence of the file.
                // If the existence of the file has changed since the last time we checked
                // this means that we need to act on that change.
                bool fileExists = File.Exists(m_markerFilePath);
                if (m_markerFileExists != fileExists)
                {
                    // Save the result.
                    m_markerFileExists = fileExists;

                    // Take the appropriate action.
                    if (fileExists)
                    {
                        // Enable tracing.
                        EventPipe.Enable(GetConfiguration());
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

        private static EventPipeConfiguration GetConfiguration()
        {
            // Create a new configuration object.
            EventPipeConfiguration config = new EventPipeConfiguration(
                GetDisambiguatedTraceFilePath(Config_EventPipeOutputFile),
                Config_EventPipeCircularMB);

            // Get the configuration.
            string strConfig = Config_EventPipeConfig;
            if (!string.IsNullOrEmpty(strConfig))
            {
                // String must be of the form "providerName:keywords:level,providerName:keywords:level..."
                string[] providers = strConfig.Split(ProviderConfigDelimiter);
                foreach (string provider in providers)
                {
                    string[] components = provider.Split(ConfigComponentDelimiter);
                    if (components.Length == 3)
                    {
                        string providerName = components[0];

                        // We use a try/catch block here because ulong.TryParse won't accept 0x at the beginning
                        // of a hex string.  Thus, we either need to conditionally strip it or handle the exception.
                        // Given that this is not a perf-critical path, catching the exception is the simpler code.
                        ulong keywords = 0;
                        try
                        {
                            keywords = Convert.ToUInt64(components[1], 16);
                        }
                        catch { }

                        uint level;
                        if (!uint.TryParse(components[2], out level))
                        {
                            level = 0;
                        }

                        config.EnableProvider(providerName, keywords, level);
                    }
                }
            }
            else
            {
                // Specify the default configuration.
                config.EnableProviderRange(DefaultProviderConfiguration);
            }

            return config;
        }

        /// <summary>
        /// Responsible for disambiguating the trace file path if the specified file already exists.
        /// This can happen if there are multiple applications with tracing enabled concurrently and COMPlus_EventPipeOutputFile
        /// is set to the same value for more than one concurrently running application.
        /// </summary>
        private static string GetDisambiguatedTraceFilePath(string inputPath)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }

            string filePath = inputPath;
            if (File.Exists(filePath))
            {
                string directoryName = Path.GetDirectoryName(filePath);
                string fileWithoutExtension = Path.GetFileName(filePath);
                string extension = Path.GetExtension(filePath);

                string newFileWithExtension = fileWithoutExtension + "." + Win32Native.GetCurrentProcessId() + extension;
                filePath = Path.Combine(directoryName, newFileWithExtension);
            }

            return filePath;
        }

        #region Configuration

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

        private static string Config_EventPipeOutputFile
        {
            get
            {
                if (s_Config_EventPipeOutputFile == null)
                {
                    s_Config_EventPipeOutputFile = CompatibilitySwitch.GetValueInternal("EventPipeOutputFile");
                    if (s_Config_EventPipeOutputFile == null)
                    {
                        s_Config_EventPipeOutputFile = "Process-" + Win32Native.GetCurrentProcessId() + NetPerfFileExtension;
                    }
                }

                return s_Config_EventPipeOutputFile;
            }
        }

        private static string MarkerFilePath
        {
            get
            {
                return Config_EventPipeOutputFile + MarkerFileExtension;
            }
        }

        #endregion Configuration
    }
}

#endif // FEATURE_PERFTRACING
