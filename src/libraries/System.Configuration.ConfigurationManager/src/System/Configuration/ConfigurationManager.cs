// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Configuration.Internal;

namespace System.Configuration
{
    public static class ConfigurationManager
    {
        // The Configuration System
        private static volatile IInternalConfigSystem s_configSystem;

        // Initialization state
        private static volatile InitState s_initState;
        private static readonly object s_initLock;
        private static volatile Exception s_initError;

        static ConfigurationManager()
        {
            s_initState = InitState.NotStarted;
            s_initLock = new object();
        }

        // to be used by System.Diagnostics to avoid false config results during config init
        internal static bool SetConfigurationSystemInProgress
            => (InitState.NotStarted < s_initState) && (s_initState < InitState.Completed);

        internal static bool SupportsUserConfig
        {
            get
            {
                PrepareConfigSystem();

                return s_configSystem.SupportsUserConfig;
            }
        }

        public static NameValueCollection AppSettings
        {
            get
            {
                object section = GetSection("appSettings");
                if (!(section is NameValueCollection))
                {
                    // If config is null or not the type we expect, the declaration was changed. 
                    // Treat it as a configuration error.
                    throw new ConfigurationErrorsException(SR.Config_appsettings_declaration_invalid);
                }

                return (NameValueCollection)section;
            }
        }

        public static ConnectionStringSettingsCollection ConnectionStrings
        {
            get
            {
                object section = GetSection("connectionStrings");

                // Verify type, and return the collection 
                if ((section == null) || (section.GetType() != typeof(ConnectionStringsSection)))
                {
                    // If config is null or not the type we expect, the declaration was changed. 
                    // Treat it as a configuration error.
                    throw new ConfigurationErrorsException(SR.Config_connectionstrings_declaration_invalid);
                }

                ConnectionStringsSection connectionStringsSection = (ConnectionStringsSection)section;
                return connectionStringsSection.ConnectionStrings;
            }
        }

        // Called by ASP.NET to allow hierarchical configuration settings and ASP.NET specific extenstions.
        internal static void SetConfigurationSystem(IInternalConfigSystem configSystem, bool initComplete)
        {
            lock (s_initLock)
            {
                // It is an error if the configuration system has already been set.
                if (s_initState != InitState.NotStarted)
                    throw new InvalidOperationException(SR.Config_system_already_set);

                s_configSystem = configSystem;
                s_initState = initComplete ? InitState.Completed : InitState.Usable;
            }
        }

        private static void EnsureConfigurationSystem()
        {
            // If a configuration system has not yet been set, 
            // create the DefaultConfigurationSystem for exe's.
            lock (s_initLock)
            {
                if (s_initState >= InitState.Usable) return;

                s_initState = InitState.Started;
                try
                {
                    try
                    {
                        // Create the system, but let it initialize itself when GetConfig is called,
                        // so that it can handle its own re-entrancy issues during initialization.
                        //
                        // When initialization is complete, the DefaultConfigurationSystem will call
                        // CompleteConfigInit to mark initialization as having completed.
                        //
                        // Note: the ClientConfigurationSystem has a 2-stage initialization,
                        // and that's why s_initState isn't set to InitState.Completed yet.
                        s_configSystem = new ClientConfigurationSystem();
                        s_initState = InitState.Usable;
                    }
                    catch (Exception e)
                    {
                        s_initError =
                            new ConfigurationErrorsException(SR.Config_client_config_init_error, e);
                        throw s_initError;
                    }
                }
                catch
                {
                    s_initState = InitState.Completed;
                    throw;
                }
            }
        }

        // Set the initialization error.
        internal static void SetInitError(Exception initError)
        {
            s_initError = initError;
        }

        // Mark intiailization as having completed.
        internal static void CompleteConfigInit()
        {
            lock (s_initLock)
            {
                s_initState = InitState.Completed;
            }
        }


        private static void PrepareConfigSystem()
        {
            // Ensure the configuration system is usable.
            if (s_initState < InitState.Usable) EnsureConfigurationSystem();

            // If there was an initialization error, throw it.
            if (s_initError != null) throw s_initError;
        }

        public static object GetSection(string sectionName)
        {
            // Avoid unintended AV's by ensuring sectionName is not empty.
            // For compatibility, we cannot throw an InvalidArgumentException.
            if (string.IsNullOrEmpty(sectionName)) return null;

            PrepareConfigSystem();

            object section = s_configSystem.GetSection(sectionName);
            return section;
        }

        public static void RefreshSection(string sectionName)
        {
            // Avoid unintended AV's by ensuring sectionName is not empty.
            // For consistency with GetSection, we should not throw an InvalidArgumentException.
            if (string.IsNullOrEmpty(sectionName)) return;

            PrepareConfigSystem();

            s_configSystem.RefreshConfig(sectionName);
        }

        public static Configuration OpenMachineConfiguration()
        {
            return OpenExeConfigurationImpl(null, true, ConfigurationUserLevel.None, null);
        }

        public static Configuration OpenMappedMachineConfiguration(ConfigurationFileMap fileMap)
        {
            return OpenExeConfigurationImpl(fileMap, true, ConfigurationUserLevel.None, null);
        }

        public static Configuration OpenExeConfiguration(ConfigurationUserLevel userLevel)
        {
            return OpenExeConfigurationImpl(null, false, userLevel, null);
        }

        public static Configuration OpenExeConfiguration(string exePath)
        {
            return OpenExeConfigurationImpl(null, false, ConfigurationUserLevel.None, exePath);
        }

        public static Configuration OpenMappedExeConfiguration(ExeConfigurationFileMap fileMap,
            ConfigurationUserLevel userLevel)
        {
            return OpenExeConfigurationImpl(fileMap, false, userLevel, null);
        }

        public static Configuration OpenMappedExeConfiguration(ExeConfigurationFileMap fileMap,
            ConfigurationUserLevel userLevel, bool preLoad)
        {
            return OpenExeConfigurationImpl(fileMap, false, userLevel, null, preLoad);
        }

        private static Configuration OpenExeConfigurationImpl(ConfigurationFileMap fileMap, bool isMachine,
            ConfigurationUserLevel userLevel, string exePath, bool preLoad = false)
        {
            // exePath must be specified if not running inside ClientConfigurationSystem
            if (!isMachine &&
                (((fileMap == null) && (exePath == null)) ||
                ((fileMap != null) && (((ExeConfigurationFileMap)fileMap).ExeConfigFilename == null))))
            {
                if ((s_configSystem != null) &&
                    (s_configSystem.GetType() != typeof(ClientConfigurationSystem)))
                    throw new ArgumentException(SR.Config_configmanager_open_noexe);
            }

            Configuration config = ClientConfigurationHost.OpenExeConfiguration(fileMap, isMachine, userLevel, exePath);
            if (preLoad) PreloadConfiguration(config);
            return config;
        }

        /// <summary>
        ///     Recursively loads configuration section groups and sections belonging to a configuration object.
        /// </summary>
        private static void PreloadConfiguration(Configuration configuration)
        {
            if (null == configuration) return;

            // Preload root-level sections.
            foreach (ConfigurationSection section in configuration.Sections) { }

            // Recursively preload all section groups and sections.
            foreach (ConfigurationSectionGroup sectionGroup in configuration.SectionGroups)
                PreloadConfigurationSectionGroup(sectionGroup);
        }

        private static void PreloadConfigurationSectionGroup(ConfigurationSectionGroup sectionGroup)
        {
            if (null == sectionGroup) return;

            // Preload sections just by iterating.
            foreach (ConfigurationSection section in sectionGroup.Sections) { }

            // Load child section groups.
            foreach (ConfigurationSectionGroup childSectionGroup in sectionGroup.SectionGroups)
                PreloadConfigurationSectionGroup(childSectionGroup);
        }

        private enum InitState
        {
            // Initialization has not yet started.
            NotStarted = 0,

            // Initialization has started.
            Started,

            // The config system can be used, but initialization is not yet complete.
            Usable,

            // The config system has been completely initialized.
            Completed
        };
    }
}