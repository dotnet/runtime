// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    internal static class DiagnosticsConfiguration
    {
        private static volatile SystemDiagnosticsSection s_configSection;
        private static volatile InitState s_initState = InitState.NotInitialized;

        // Setting for Switch.switchSetting
        internal static SwitchElementsCollection SwitchSettings
        {
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                return configSectionSav?.Switches;
            }
        }

        internal static string ConfigFilePath
        {
            [ResourceExposure(ResourceScope.Machine)]
            [ResourceConsumption(ResourceScope.Machine)]
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                if (configSectionSav != null)
                {
                    return configSectionSav.ElementInformation.Source;
                }

                return string.Empty; // the default
            }
        }

        // Setting for TraceInternal.AutoFlush
        internal static bool AutoFlush
        {
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                if (configSectionSav != null && configSectionSav.Trace != null)
                {
                    return configSectionSav.Trace.AutoFlush;
                }

                return false; // the default
            }
        }

        // Setting for TraceInternal.UseGlobalLock
        internal static bool UseGlobalLock
        {
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                if (configSectionSav != null && configSectionSav.Trace != null)
                {
                    return configSectionSav.Trace.UseGlobalLock;
                }

                return true; // the default
            }
        }

        // Setting for TraceInternal.IndentSize
        internal static int IndentSize
        {
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                if (configSectionSav != null && configSectionSav.Trace != null)
                {
                    return configSectionSav.Trace.IndentSize;
                }

                return 4; // the default
            }
        }

        internal static ListenerElementsCollection SharedListeners
        {
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                return configSectionSav?.SharedListeners;
            }
        }

        internal static SourceElementsCollection Sources
        {
            get
            {
                Initialize();
                SystemDiagnosticsSection configSectionSav = s_configSection;
                return configSectionSav?.Sources;
            }
        }

        internal static SystemDiagnosticsSection SystemDiagnosticsSection
        {
            get
            {
                Initialize();
                return s_configSection;
            }
        }

        private static SystemDiagnosticsSection GetConfigSection()
        {
            return s_configSection ??= (SystemDiagnosticsSection)PrivilegedConfigurationManager.GetSection("system.diagnostics");
        }

        internal static bool IsInitializing() => s_initState == InitState.Initializing;
        internal static bool IsInitialized() => s_initState == InitState.Initialized;

        internal static bool CanInitialize() => (s_initState != InitState.Initializing) &&
            !ConfigurationManagerInternalFactory.Instance.SetConfigurationSystemInProgress;

        internal static void Initialize()
        {
            // Ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/DiagnosticsConfiguration.cs,188
            // This port removed the lock on TraceInternal.critSec since that is now in a separate assembly and TraceInternal
            // is internal and because GetConfigSection() is not locked elsewhere such as for connection strings.

            // Because some of the code used to load config also uses diagnostics
            // we can't block them while we initialize from config. Therefore we just
            // return immediately and they just use the default values.
            if (s_initState != InitState.NotInitialized ||
                ConfigurationManagerInternalFactory.Instance.SetConfigurationSystemInProgress)
            {
                return;
            }

            s_initState = InitState.Initializing; // used for preventing recursion
            try
            {
                s_configSection = GetConfigSection();
            }
            finally
            {
                s_initState = InitState.Initialized;
            }
        }

        internal static void Refresh()
        {
            ConfigurationManager.RefreshSection("system.diagnostics");

            // There might still be some persistant state left behind for
            // ConfigPropertyCollection (for ex, swtichelements), probably for perf.
            // We need to explicitly cleanup any unrecognized attributes that we
            // have added during last deserialization, so that they are re-added
            // during the next Config.GetSection properly and we get a chance to
            // populate the Attributes collection for re-deserialization.
            // Another alternative could be to expose the properties collection
            // directly as Attributes collection (currently we keep a local
            // hashtable which we explicitly need to keep in sycn and hence the
            // cleanup logic below) but the down side of that would be we need to
            // explicitly compute what is recognized Vs unrecognized from that
            // collection when we expose the unrecognized Attributes publically
            SystemDiagnosticsSection configSectionSav = s_configSection;
            if (configSectionSav != null)
            {
                if (configSectionSav.Switches != null)
                {
                    foreach (SwitchElement swelem in configSectionSav.Switches)
                    {
                        swelem.ResetProperties();
                    }
                }

                if (configSectionSav.SharedListeners != null)
                {
                    foreach (ListenerElement lnelem in configSectionSav.SharedListeners)
                    {
                        lnelem.ResetProperties();
                    }
                }

                if (configSectionSav.Sources != null)
                {
                    foreach (SourceElement srelem in configSectionSav.Sources)
                    {
                        srelem.ResetProperties();
                    }
                }
            }

            s_configSection = null;

            s_initState = InitState.NotInitialized;
            Initialize();
        }

        private enum InitState
        {
            NotInitialized,
            Initializing,
            Initialized
        }
    }
}
