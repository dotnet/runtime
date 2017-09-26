// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Configuration;
    using System.Threading;
    using System.Runtime.Versioning;
    
    internal enum InitState {
        NotInitialized,
        Initializing,
        Initialized
    }

    internal static class DiagnosticsConfiguration {
        private static volatile SystemDiagnosticsSection configSection;
        private static volatile InitState initState = InitState.NotInitialized;

#if !FEATURE_PAL // perfcounter
        internal static int PerformanceCountersFileMappingSize {
            get {                                                 
                for (int retryCount = 0; !CanInitialize() && retryCount <= 5; ++retryCount) {
                    if (retryCount == 5)
                        return SharedPerformanceCounter.DefaultCountersFileMappingSize;
                        
                    System.Threading.Thread.Sleep(200);
                }                    
                    
                Initialize();
                SystemDiagnosticsSection configSectionSav = configSection;
                if (configSectionSav != null && configSectionSav.PerfCounters != null) {
                    int size = configSectionSav.PerfCounters.FileMappingSize;
                    if (size < SharedPerformanceCounter.MinCountersFileMappingSize)
                        size = SharedPerformanceCounter.MinCountersFileMappingSize;
                                            
                    if (size > SharedPerformanceCounter.MaxCountersFileMappingSize)
                        size = SharedPerformanceCounter.MaxCountersFileMappingSize;

                    return size;
              	} 
                else
                    return SharedPerformanceCounter.DefaultCountersFileMappingSize;
            }                
        }
#endif // !FEATURE_PAL

        internal static string ConfigFilePath {
            get { 
                Initialize();
                SystemDiagnosticsSection configSectionSav = configSection;
                if (configSectionSav != null) 
                    return configSectionSav.ElementInformation.Source;
                else
                    return string.Empty; // the default
            }
        }

        private static SystemDiagnosticsSection GetConfigSection() {
            SystemDiagnosticsSection configSection = (SystemDiagnosticsSection) ConfigurationManager.GetSection("system.diagnostics");
            return configSection;
        }

        internal static bool CanInitialize() {
            bool setConfigurationSystemInProgress = (bool)(typeof(ConfigurationManager).GetProperty("SetConfigurationSystemInProgress", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
            return  (initState != InitState.Initializing) && 
                    !setConfigurationSystemInProgress;
        }
        
        internal static void Initialize() {
            // Initialize() is also called by other components outside of Trace (such as PerformanceCounter)
            // as a result using one lock for this critical section and another for Trace API critical sections  
            // (such as Trace.WriteLine) could potentially lead to deadlock between 2 threads that are 
            // executing these critical sections (and consequently obtaining the 2 locks) in the reverse order. 
            // Using the same lock for DiagnosticsConfiguration as well as TraceInternal avoids this issue. 
            // Sequential locks on TraceInternal.critSec by the same thread is a non issue for this critical section.
            lock (TraceInternal.critSec) {

                // because some of the code used to load config also uses diagnostics
                // we can't block them while we initialize from config. Therefore we just
                // return immediately and they just use the default values.
                bool setConfigurationSystemInProgress = (bool)(typeof(ConfigurationManager).GetProperty("SetConfigurationSystemInProgress", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                if (    initState != InitState.NotInitialized || 
                        setConfigurationSystemInProgress) {

                    return;
                }

                initState = InitState.Initializing; // used for preventing recursion
                try {
                    configSection = GetConfigSection();
                }
                finally {
                    initState = InitState.Initialized;
                }
            }
        }
    }
}

