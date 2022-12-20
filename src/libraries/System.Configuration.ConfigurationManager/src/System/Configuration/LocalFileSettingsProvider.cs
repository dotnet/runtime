// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

namespace System.Configuration
{
    /// <summary>
    /// This is a provider used to store configuration settings locally for client applications.
    /// </summary>
    public class LocalFileSettingsProvider : SettingsProvider, IApplicationSettingsProvider
    {
        private string _appName = string.Empty;
        private ClientSettingsStore _store;
        private string _prevLocalConfigFileName;
        private string _prevRoamingConfigFileName;
        private XmlEscaper _escaper;

        /// <summary>
        /// Abstract SettingsProvider property.
        /// </summary>
        public override string ApplicationName
        {
            get
            {
                return _appName;
            }
            set
            {
                _appName = value;
            }
        }

        private XmlEscaper Escaper => _escaper ??= new XmlEscaper();

        /// <summary>
        /// We maintain a single instance of the ClientSettingsStore per instance of provider.
        /// </summary>
        private ClientSettingsStore Store => _store ??= new ClientSettingsStore();

        /// <summary>
        /// Abstract ProviderBase method.
        /// </summary>
        public override void Initialize(string name, NameValueCollection values)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "LocalFileSettingsProvider";
            }

            base.Initialize(name, values);
        }

        /// <summary>
        /// Abstract SettingsProvider method
        /// </summary>
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            string sectionName = GetSectionName(context);

            // Look for this section in both applicationSettingsGroup and userSettingsGroup
            IDictionary appSettings = ClientSettingsStore.ReadSettings(sectionName, false);
            IDictionary userSettings = ClientSettingsStore.ReadSettings(sectionName, true);
            ConnectionStringSettingsCollection connStrings = ClientSettingsStore.ReadConnectionStrings();

            // Now map each SettingProperty to the right StoredSetting and deserialize the value if found.
            foreach (SettingsProperty setting in properties)
            {
                string settingName = setting.Name;
                SettingsPropertyValue value = new SettingsPropertyValue(setting);

                // First look for and handle "special" settings
                SpecialSettingAttribute attr = setting.Attributes[typeof(SpecialSettingAttribute)] as SpecialSettingAttribute;
                bool isConnString = (attr != null) ? (attr.SpecialSetting == SpecialSetting.ConnectionString) : false;

                if (isConnString)
                {
                    string connStringName = sectionName + "." + settingName;
                    if (connStrings != null && connStrings[connStringName] != null)
                    {
                        value.PropertyValue = connStrings[connStringName].ConnectionString;
                    }
                    else if (setting.DefaultValue != null && setting.DefaultValue is string)
                    {
                        value.PropertyValue = setting.DefaultValue;
                    }
                    else
                    {
                        //No value found and no default specified
                        value.PropertyValue = string.Empty;
                    }

                    value.IsDirty = false; //reset IsDirty so that it is correct when SetPropertyValues is called
                    values.Add(value);
                    continue;
                }

                // Not a "special" setting
                bool isUserSetting = IsUserSetting(setting);

                if (isUserSetting && !ConfigurationManagerInternalFactory.Instance.SupportsUserConfig)
                {
                    // We encountered a user setting, but the current configuration system does not support
                    // user settings.
                    throw new ConfigurationErrorsException(SR.UserSettingsNotSupported);
                }

                IDictionary settings = isUserSetting ? userSettings : appSettings;

                if (settings.Contains(settingName))
                {
                    StoredSetting ss = (StoredSetting)settings[settingName];
                    string valueString = ss.Value.InnerXml;

                    // We need to un-escape string serialized values
                    if (ss.SerializeAs == SettingsSerializeAs.String)
                    {
                        valueString = Escaper.Unescape(valueString);
                    }

                    value.SerializedValue = valueString;
                }
                else if (setting.DefaultValue != null)
                {
                    value.SerializedValue = setting.DefaultValue;
                }
                else
                {
                    //No value found and no default specified
                    value.PropertyValue = null;
                }

                value.IsDirty = false; //reset IsDirty so that it is correct when SetPropertyValues is called
                values.Add(value);
            }

            return values;
        }

        /// <summary>
        ///     Abstract SettingsProvider method
        /// </summary>
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        {
            string sectionName = GetSectionName(context);
            IDictionary roamingUserSettings = new Hashtable();
            IDictionary localUserSettings = new Hashtable();

            foreach (SettingsPropertyValue value in values)
            {
                SettingsProperty setting = value.Property;
                bool isUserSetting = IsUserSetting(setting);

                if (value.IsDirty)
                {
                    if (isUserSetting)
                    {
                        bool isRoaming = IsRoamingSetting(setting);
                        StoredSetting ss = new StoredSetting(setting.SerializeAs, SerializeToXmlElement(setting, value));

                        if (isRoaming)
                        {
                            roamingUserSettings[setting.Name] = ss;
                        }
                        else
                        {
                            localUserSettings[setting.Name] = ss;
                        }

                        value.IsDirty = false; //reset IsDirty
                    }
                    else
                    {
                        // This is an app-scoped or connection string setting that has been written to.
                        // We don't support saving these.
                    }
                }
            }

            // Semi-hack: If there are roamable settings, let's write them before local settings so if a handler
            // declaration is necessary, it goes in the roaming config file in preference to the local config file.
            if (roamingUserSettings.Count > 0)
            {
                ClientSettingsStore.WriteSettings(sectionName, true, roamingUserSettings);
            }

            if (localUserSettings.Count > 0)
            {
                ClientSettingsStore.WriteSettings(sectionName, false, localUserSettings);
            }
        }

        /// <summary>
        ///     Implementation of IClientSettingsProvider.Reset. Resets user scoped settings to the values
        ///     in app.exe.config, does nothing for app scoped settings.
        /// </summary>
        public void Reset(SettingsContext context)
        {
            string sectionName = GetSectionName(context);

            // First revert roaming, then local
            ClientSettingsStore.RevertToParent(sectionName, true);
            ClientSettingsStore.RevertToParent(sectionName, false);
        }

        /// <summary>
        ///    Implementation of IClientSettingsProvider.Upgrade.
        ///    Tries to locate a previous version of the user.config file. If found, it migrates matching settings.
        ///    If not, it does nothing.
        /// </summary>
        public void Upgrade(SettingsContext context, SettingsPropertyCollection properties)
        {
            // Separate the local and roaming settings and upgrade them separately.

            SettingsPropertyCollection local = new SettingsPropertyCollection();
            SettingsPropertyCollection roaming = new SettingsPropertyCollection();

            foreach (SettingsProperty sp in properties)
            {
                bool isRoaming = IsRoamingSetting(sp);

                if (isRoaming)
                {
                    roaming.Add(sp);
                }
                else
                {
                    local.Add(sp);
                }
            }

            if (roaming.Count > 0)
            {
                Upgrade(context, roaming, true);
            }

            if (local.Count > 0)
            {
                Upgrade(context, local, false);
            }
        }

        /// <summary>
        /// Implementation of IClientSettingsProvider.GetPreviousVersion.
        /// </summary>
        public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property)
        {
            bool isRoaming = IsRoamingSetting(property);
            string prevConfig = GetPreviousConfigFileName(isRoaming);

            if (!string.IsNullOrEmpty(prevConfig))
            {
                SettingsPropertyCollection properties = new SettingsPropertyCollection();
                properties.Add(property);
                SettingsPropertyValueCollection values = GetSettingValuesFromFile(prevConfig, GetSectionName(context), true, properties);
                return values[property.Name];
            }
            else
            {
                SettingsPropertyValue value = new SettingsPropertyValue(property);
                value.PropertyValue = null;
                return value;
            }
        }

        /// <summary>
        /// Locates the previous version of user.config, if present. The previous version is determined
        /// by walking up one directory level in the *UserConfigPath and searching for the highest version
        /// number less than the current version.
        /// </summary>
        private string GetPreviousConfigFileName(bool isRoaming)
        {
            if (!ConfigurationManagerInternalFactory.Instance.SupportsUserConfig)
            {
                throw new ConfigurationErrorsException(SR.UserSettingsNotSupported);
            }

            string prevConfigFile = isRoaming ? _prevRoamingConfigFileName : _prevLocalConfigFileName;

            if (string.IsNullOrEmpty(prevConfigFile))
            {
                string userConfigPath = isRoaming
                    ? ConfigurationManagerInternalFactory.Instance.ExeRoamingConfigDirectory
                    : ConfigurationManagerInternalFactory.Instance.ExeLocalConfigDirectory;

                Version currentVersion;
                if (!Version.TryParse(ConfigurationManagerInternalFactory.Instance.ExeProductVersion, out currentVersion))
                {
                    return null;
                }

                Version previousVersion = null;
                DirectoryInfo previousDirectory = null;
                string file = null;

                DirectoryInfo parentDirectory = Directory.GetParent(userConfigPath);

                if (parentDirectory.Exists)
                {
                    foreach (DirectoryInfo directory in parentDirectory.GetDirectories())
                    {
                        Version tempVersion;

                        if (Version.TryParse(directory.Name, out tempVersion) && tempVersion < currentVersion)
                        {
                            if (previousVersion == null)
                            {
                                previousVersion = tempVersion;
                                previousDirectory = directory;
                            }
                            else if (tempVersion > previousVersion)
                            {
                                previousVersion = tempVersion;
                                previousDirectory = directory;
                            }
                        }
                    }

                    if (previousDirectory != null)
                    {
                        file = Path.Combine(previousDirectory.FullName, ConfigurationManagerInternalFactory.Instance.UserConfigFilename);
                    }

                    if (File.Exists(file))
                    {
                        prevConfigFile = file;
                    }
                }

                // Cache for future use.
                if (isRoaming)
                {
                    _prevRoamingConfigFileName = prevConfigFile;
                }
                else
                {
                    _prevLocalConfigFileName = prevConfigFile;
                }
            }

            return prevConfigFile;
        }

        /// <summary>
        /// Gleans information from the SettingsContext and determines the name of the config section.
        /// </summary>
        private static string GetSectionName(SettingsContext context)
        {
            string groupName = (string)context["GroupName"];
            string key = (string)context["SettingsKey"];

            Debug.Assert(groupName != null, "SettingsContext did not have a GroupName!");

            string sectionName = groupName;

            if (!string.IsNullOrEmpty(key))
            {
                sectionName = sectionName + "." + key;
            }

            return XmlConvert.EncodeLocalName(sectionName);
        }

        /// <summary>
        /// Retrieves the values of settings from the given config file (as opposed to using
        /// the configuration for the current context)
        /// </summary>
        private SettingsPropertyValueCollection GetSettingValuesFromFile(string configFileName, string sectionName, bool userScoped, SettingsPropertyCollection properties)
        {
            SettingsPropertyValueCollection values = new SettingsPropertyValueCollection();
            IDictionary settings = ClientSettingsStore.ReadSettingsFromFile(configFileName, sectionName, userScoped);

            // Map each SettingProperty to the right StoredSetting and deserialize the value if found.
            foreach (SettingsProperty setting in properties)
            {
                string settingName = setting.Name;
                SettingsPropertyValue value = new SettingsPropertyValue(setting);

                if (settings.Contains(settingName))
                {
                    StoredSetting ss = (StoredSetting)settings[settingName];
                    string valueString = ss.Value.InnerXml;

                    // We need to un-escape string serialized values
                    if (ss.SerializeAs == SettingsSerializeAs.String)
                    {
                        valueString = Escaper.Unescape(valueString);
                    }

                    value.SerializedValue = valueString;
                    value.IsDirty = true;
                    values.Add(value);
                }
            }

            return values;
        }

        /// <summary>
        /// Indicates whether a setting is roaming or not.
        /// </summary>
        private static bool IsRoamingSetting(SettingsProperty setting)
        {
            SettingsManageabilityAttribute manageAttr = setting.Attributes[typeof(SettingsManageabilityAttribute)] as SettingsManageabilityAttribute;
            return manageAttr != null && ((manageAttr.Manageability & SettingsManageability.Roaming) == SettingsManageability.Roaming);
        }

        /// <summary>
        /// This provider needs settings to be marked with either the UserScopedSettingAttribute or the
        /// ApplicationScopedSettingAttribute. This method determines whether this setting is user-scoped
        /// or not. It will throw if none or both of the attributes are present.
        /// </summary>
        private static bool IsUserSetting(SettingsProperty setting)
        {
            bool isUser = setting.Attributes[typeof(UserScopedSettingAttribute)] is UserScopedSettingAttribute;
            bool isApp = setting.Attributes[typeof(ApplicationScopedSettingAttribute)] is ApplicationScopedSettingAttribute;

            if (isUser && isApp)
            {
                throw new ConfigurationErrorsException(SR.Format(SR.BothScopeAttributes, setting.Name));
            }
            else if (!(isUser || isApp))
            {
                throw new ConfigurationErrorsException(SR.Format(SR.NoScopeAttributes, setting.Name));
            }

            return isUser;
        }

        private XmlNode SerializeToXmlElement(SettingsProperty setting, SettingsPropertyValue value)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement valueXml = doc.CreateElement(nameof(value));

            string serializedValue = value.SerializedValue as string;

#pragma warning disable CS0618 // Type or member is obsolete
            if (serializedValue == null && setting.SerializeAs == SettingsSerializeAs.Binary)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                // SettingsPropertyValue returns a byte[] in the binary serialization case. We need to
                // encode this - we use base64 since SettingsPropertyValue understands it and we won't have
                // to special case while deserializing.

                byte[] buffer = value.SerializedValue as byte[];
                if (buffer != null)
                {
                    serializedValue = Convert.ToBase64String(buffer);
                }
            }

            serializedValue ??= string.Empty;

            // We need to escape string serialized values
            if (setting.SerializeAs == SettingsSerializeAs.String)
            {
                serializedValue = Escaper.Escape(serializedValue);
            }

            valueXml.InnerXml = serializedValue;

            // Hack to remove the XmlDeclaration that the XmlSerializer adds.
            XmlNode unwanted = null;
            foreach (XmlNode child in valueXml.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.XmlDeclaration)
                {
                    unwanted = child;
                    break;
                }
            }
            if (unwanted != null)
            {
                valueXml.RemoveChild(unwanted);
            }

            return valueXml;
        }

        /// <summary>
        /// Private version of upgrade that uses isRoaming to determine which config file to use.
        /// </summary>
        private void Upgrade(SettingsContext context, SettingsPropertyCollection properties, bool isRoaming)
        {
            string prevConfig = GetPreviousConfigFileName(isRoaming);

            if (!string.IsNullOrEmpty(prevConfig))
            {
                //Filter the settings properties to exclude those that have a NoSettingsVersionUpgradeAttribute on them.
                SettingsPropertyCollection upgradeProperties = new SettingsPropertyCollection();
                foreach (SettingsProperty sp in properties)
                {
                    if (!(sp.Attributes[typeof(NoSettingsVersionUpgradeAttribute)] is NoSettingsVersionUpgradeAttribute))
                    {
                        upgradeProperties.Add(sp);
                    }
                }

                SettingsPropertyValueCollection values = GetSettingValuesFromFile(prevConfig, GetSectionName(context), true, upgradeProperties);
                SetPropertyValues(context, values);
            }
        }

        private sealed class XmlEscaper
        {
            private readonly XmlDocument document;
            private readonly XmlElement tempElement;

            internal XmlEscaper()
            {
                document = new XmlDocument();
                tempElement = document.CreateElement("temp");
            }

            internal string Escape(string xmlString)
            {
                if (string.IsNullOrEmpty(xmlString))
                {
                    return xmlString;
                }

                tempElement.InnerText = xmlString;
                return tempElement.InnerXml;
            }

            internal string Unescape(string escapedString)
            {
                if (string.IsNullOrEmpty(escapedString))
                {
                    return escapedString;
                }

                tempElement.InnerXml = escapedString;
                return tempElement.InnerText;
            }
        }
    }
}
