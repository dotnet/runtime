// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace System.Configuration
{
    /// <summary>
    /// This class abstracts the details of config system away from the LocalFileSettingsProvider. It talks to 
    /// the configuration API and the relevant Sections to read and write settings. 
    /// It understands sections of type ClientSettingsSection.
    ///
    /// NOTE: This API supports reading from app.exe.config and user.config, but writing only to 
    ///       user.config.
    /// </summary>
    internal sealed class ClientSettingsStore
    {
        private const string ApplicationSettingsGroupName = "applicationSettings";
        private const string UserSettingsGroupName = "userSettings";
        private const string ApplicationSettingsGroupPrefix = ApplicationSettingsGroupName + "/";
        private const string UserSettingsGroupPrefix = UserSettingsGroupName + "/";

        private Configuration GetUserConfig(bool isRoaming)
        {
            ConfigurationUserLevel userLevel = isRoaming ? ConfigurationUserLevel.PerUserRoaming :
                                                           ConfigurationUserLevel.PerUserRoamingAndLocal;

            return ClientSettingsConfigurationHost.OpenExeConfiguration(userLevel);
        }

        private ClientSettingsSection GetConfigSection(Configuration config, string sectionName, bool declare)
        {
            string fullSectionName = UserSettingsGroupPrefix + sectionName;
            ClientSettingsSection section = null;

            if (config != null)
            {
                section = config.GetSection(fullSectionName) as ClientSettingsSection;

                if (section == null && declare)
                {
                    // Looks like the section isn't declared - let's declare it and try again.
                    DeclareSection(config, sectionName);
                    section = config.GetSection(fullSectionName) as ClientSettingsSection;
                }
            }

            return section;
        }

        // Declares the section handler of a given section in its section group, if a declaration isn't already
        // present. 
        private void DeclareSection(Configuration config, string sectionName)
        {
            ConfigurationSectionGroup settingsGroup = config.GetSectionGroup(UserSettingsGroupName);

            if (settingsGroup == null)
            {
                //Declare settings group
                ConfigurationSectionGroup group = new UserSettingsGroup();
                config.SectionGroups.Add(UserSettingsGroupName, group);
            }

            settingsGroup = config.GetSectionGroup(UserSettingsGroupName);

            Debug.Assert(settingsGroup != null, "Failed to declare settings group");

            if (settingsGroup != null)
            {
                ConfigurationSection section = settingsGroup.Sections[sectionName];
                if (section == null)
                {
                    section = new ClientSettingsSection();
                    section.SectionInformation.AllowExeDefinition = ConfigurationAllowExeDefinition.MachineToLocalUser;
                    section.SectionInformation.RequirePermission = false;
                    settingsGroup.Sections.Add(sectionName, section);
                }
            }
        }

        internal IDictionary ReadSettings(string sectionName, bool isUserScoped)
        {
            IDictionary settings = new Hashtable();

            if (isUserScoped && !ConfigurationManagerInternalFactory.Instance.SupportsUserConfig)
            {
                return settings;
            }

            string prefix = isUserScoped ? UserSettingsGroupPrefix : ApplicationSettingsGroupPrefix;
            ConfigurationManager.RefreshSection(prefix + sectionName);
            ClientSettingsSection section = ConfigurationManager.GetSection(prefix + sectionName) as ClientSettingsSection;

            if (section != null)
            {
                foreach (SettingElement setting in section.Settings)
                {
                    settings[setting.Name] = new StoredSetting(setting.SerializeAs, setting.Value.ValueXml);
                }
            }

            return settings;
        }

        internal static IDictionary ReadSettingsFromFile(string configFileName, string sectionName, bool isUserScoped)
        {
            IDictionary settings = new Hashtable();

            if (isUserScoped && !ConfigurationManagerInternalFactory.Instance.SupportsUserConfig)
            {
                return settings;
            }

            string prefix = isUserScoped ? UserSettingsGroupPrefix : ApplicationSettingsGroupPrefix;
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();

            // NOTE: When isUserScoped is true, we don't care if configFileName represents a roaming file or
            //       a local one. All we want is three levels of configuration. So, we use the PerUserRoaming level. 
            ConfigurationUserLevel userLevel = isUserScoped ? ConfigurationUserLevel.PerUserRoaming : ConfigurationUserLevel.None;

            if (isUserScoped)
            {
                fileMap.ExeConfigFilename = ConfigurationManagerInternalFactory.Instance.ApplicationConfigUri;
                fileMap.RoamingUserConfigFilename = configFileName;
            }
            else
            {
                fileMap.ExeConfigFilename = configFileName;
            }

            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, userLevel);
            ClientSettingsSection section = config.GetSection(prefix + sectionName) as ClientSettingsSection;

            if (section != null)
            {
                foreach (SettingElement setting in section.Settings)
                {
                    settings[setting.Name] = new StoredSetting(setting.SerializeAs, setting.Value.ValueXml);
                }
            }

            return settings;
        }

        internal ConnectionStringSettingsCollection ReadConnectionStrings()
        {
            return PrivilegedConfigurationManager.ConnectionStrings;
        }

        internal void RevertToParent(string sectionName, bool isRoaming)
        {
            if (!ConfigurationManagerInternalFactory.Instance.SupportsUserConfig)
            {
                throw new ConfigurationErrorsException(SR.UserSettingsNotSupported);
            }

            Configuration config = GetUserConfig(isRoaming);
            ClientSettingsSection section = GetConfigSection(config, sectionName, false);

            // If the section is null, there is nothing to revert.
            if (section != null)
            {
                section.SectionInformation.RevertToParent();
                config.Save();
            }
        }

        internal void WriteSettings(string sectionName, bool isRoaming, IDictionary newSettings)
        {
            if (!ConfigurationManagerInternalFactory.Instance.SupportsUserConfig)
            {
                throw new ConfigurationErrorsException(SR.UserSettingsNotSupported);
            }

            Configuration config = GetUserConfig(isRoaming);
            ClientSettingsSection section = GetConfigSection(config, sectionName, true);

            if (section != null)
            {
                SettingElementCollection sec = section.Settings;
                foreach (DictionaryEntry entry in newSettings)
                {
                    SettingElement se = sec.Get((string)entry.Key);

                    if (se == null)
                    {
                        se = new SettingElement();
                        se.Name = (string)entry.Key;
                        sec.Add(se);
                    }

                    StoredSetting ss = (StoredSetting)entry.Value;
                    se.SerializeAs = ss.SerializeAs;
                    se.Value.ValueXml = ss.Value;
                }

                try
                {
                    config.Save();
                }
                catch (ConfigurationErrorsException ex)
                {
                    // We wrap this in an exception with our error message and throw again.
                    throw new ConfigurationErrorsException(string.Format(SR.SettingsSaveFailed, ex.Message), ex);
                }
            }
            else
            {
                throw new ConfigurationErrorsException(SR.SettingsSaveFailedNoSection);
            }
        }

        /// <summary>
        /// A private configuration host that we use to write settings to config. We need this so we
        /// can enforce a quota on the size of stuff written out.
        /// </summary>
        private sealed class ClientSettingsConfigurationHost : DelegatingConfigHost
        {
            private const string ClientConfigurationHostTypeName = "System.Configuration.ClientConfigurationHost, " + TypeUtil.ConfigurationManagerAssemblyName;
            private const string InternalConfigConfigurationFactoryTypeName = "System.Configuration.Internal.InternalConfigConfigurationFactory, " + TypeUtil.ConfigurationManagerAssemblyName;
            private static volatile IInternalConfigConfigurationFactory s_configFactory;

            /// <summary>
            /// ClientConfigurationHost implements this - a way of getting some info from it without
            /// depending too much on its internals.
            /// </summary>
            private IInternalConfigClientHost ClientHost
            {
                get
                {
                    return (IInternalConfigClientHost)Host;
                }
            }

            internal static IInternalConfigConfigurationFactory ConfigFactory
            {
                get
                {
                    if (s_configFactory == null)
                    {
                        s_configFactory = TypeUtil.CreateInstance<IInternalConfigConfigurationFactory>(InternalConfigConfigurationFactoryTypeName);
                    }
                    return s_configFactory;
                }
            }

            private ClientSettingsConfigurationHost() { }

            public override void Init(IInternalConfigRoot configRoot, params object[] hostInitParams)
            {
                Debug.Fail("Did not expect to get called here");
            }

            /// <summary>
            /// We delegate this to the ClientConfigurationHost. The only thing we need to do here is to 
            /// build a configPath from the ConfigurationUserLevel we get passed in.
            /// </summary>
            public override void InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath,
                    IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams)
            {

                ConfigurationUserLevel userLevel = (ConfigurationUserLevel)hostInitConfigurationParams[0];
                string desiredConfigPath = null;
                Host = TypeUtil.CreateInstance<IInternalConfigHost>(ClientConfigurationHostTypeName);

                switch (userLevel)
                {
                    case ConfigurationUserLevel.None:
                        desiredConfigPath = ClientHost.GetExeConfigPath();
                        break;

                    case ConfigurationUserLevel.PerUserRoaming:
                        desiredConfigPath = ClientHost.GetRoamingUserConfigPath();
                        break;

                    case ConfigurationUserLevel.PerUserRoamingAndLocal:
                        desiredConfigPath = ClientHost.GetLocalUserConfigPath();
                        break;

                    default:
                        throw new ArgumentException(SR.UnknownUserLevel);
                }


                Host.InitForConfiguration(ref locationSubPath, out configPath, out locationConfigPath, configRoot, null, null, desiredConfigPath);
            }

            private bool IsKnownConfigFile(string filename)
            {
                return
                  string.Equals(filename, ConfigurationManagerInternalFactory.Instance.MachineConfigPath, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(filename, ConfigurationManagerInternalFactory.Instance.ApplicationConfigUri, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(filename, ConfigurationManagerInternalFactory.Instance.ExeLocalConfigPath, StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(filename, ConfigurationManagerInternalFactory.Instance.ExeRoamingConfigPath, StringComparison.OrdinalIgnoreCase);

            }

            internal static Configuration OpenExeConfiguration(ConfigurationUserLevel userLevel)
            {
                return ConfigFactory.Create(typeof(ClientSettingsConfigurationHost), userLevel);
            }

            /// <summary>
            /// If the stream we are asked for represents a config file that we know about, we ask 
            /// the host to assert appropriate permissions.
            /// </summary>
            public override Stream OpenStreamForRead(string streamName)
            {
                if (IsKnownConfigFile(streamName))
                {
                    return Host.OpenStreamForRead(streamName, true);
                }
                else
                {
                    return Host.OpenStreamForRead(streamName);
                }
            }

            public override Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext)
            {
                // On NetFX (Desktop CLR) we do a bunch of work here around ensuring permissions and quotas
                return Host.OpenStreamForWrite(streamName, templateStreamName, ref writeContext);
            }

            /// <summary>
            /// If this is a stream that represents a user.config file that we know about, we ask 
            /// the host to assert appropriate permissions.
            /// </summary>
            public override void WriteCompleted(string streamName, bool success, object writeContext)
            {
                if (string.Equals(streamName, ConfigurationManagerInternalFactory.Instance.ExeLocalConfigPath, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(streamName, ConfigurationManagerInternalFactory.Instance.ExeRoamingConfigPath, StringComparison.OrdinalIgnoreCase))
                {

                    Host.WriteCompleted(streamName, success, writeContext, true);
                }
                else
                {
                    Host.WriteCompleted(streamName, success, writeContext);
                }
            }
        }
    }

    /// <summary>
    /// The ClientSettingsStore talks to the LocalFileSettingsProvider through a dictionary which maps from
    /// setting names to StoredSetting structs. This struct contains the relevant information.
    /// </summary>
    internal struct StoredSetting
    {
        internal StoredSetting(SettingsSerializeAs serializeAs, XmlNode value)
        {
            SerializeAs = serializeAs;
            Value = value;
        }
        internal SettingsSerializeAs SerializeAs;
        internal XmlNode Value;
    }
}
