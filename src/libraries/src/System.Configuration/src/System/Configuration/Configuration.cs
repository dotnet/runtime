// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Configuration.Internal;
using System.IO;
using System.Runtime.Versioning;

namespace System.Configuration
{
    // An instance of the Configuration class represents a single level
    // in the configuration hierarchy. Its contents can be edited and
    // saved to disk.
    //
    // It is not thread safe for writing.
    public sealed class Configuration
    {
        private readonly MgmtConfigurationRecord _configRecord;
        private readonly object[] _hostInitConfigurationParams;
        private readonly Type _typeConfigHost;
        private Func<string, string> _assemblyStringTransformer;
        private ContextInformation _evalContext;
        private ConfigurationLocationCollection _locations;
        private ConfigurationSectionGroup _rootSectionGroup;
        private Stack _sectionsStack;
        private Func<string, string> _typeStringTransformer;

        internal Configuration(string locationSubPath, Type typeConfigHost, params object[] hostInitConfigurationParams)
        {
            _typeConfigHost = typeConfigHost;
            _hostInitConfigurationParams = hostInitConfigurationParams;

            IInternalConfigHost configHost = (IInternalConfigHost)TypeUtil.CreateInstance(typeConfigHost);

            // Wrap the host with the UpdateConfigHost to support SaveAs.
            UpdateConfigHost updateConfigHost = new UpdateConfigHost(configHost);

            // Now wrap in ImplicitMachineConfigHost so we can stub in a simple machine.config if needed.
            IInternalConfigHost implicitMachineConfigHost = new ImplicitMachineConfigHost(updateConfigHost);

            InternalConfigRoot configRoot = new InternalConfigRoot(this, updateConfigHost);
            ((IInternalConfigRoot)configRoot).Init(implicitMachineConfigHost, isDesignTime: true);

            // Set the configuration paths for this Configuration.
            //
            // We do this in a separate step so that the WebConfigurationHost
            // can use this object's _configRoot to get the <sites> section,
            // which is used in it's MapPath implementation.
            string configPath, locationConfigPath;
            implicitMachineConfigHost.InitForConfiguration(
                ref locationSubPath,
                out configPath,
                out locationConfigPath,
                configRoot,
                hostInitConfigurationParams);

            if (!string.IsNullOrEmpty(locationSubPath) && !implicitMachineConfigHost.SupportsLocation)
                throw ExceptionUtil.UnexpectedError("Configuration::ctor");

            if (string.IsNullOrEmpty(locationSubPath) != string.IsNullOrEmpty(locationConfigPath))
                throw ExceptionUtil.UnexpectedError("Configuration::ctor");

            // Get the configuration record for this config file.
            _configRecord = (MgmtConfigurationRecord)configRoot.GetConfigRecord(configPath);

            // Create another MgmtConfigurationRecord for the location that is a child of the above record.
            // Note that this does not match the resolution hiearchy that is used at runtime.
            if (!string.IsNullOrEmpty(locationSubPath))
            {
                _configRecord = MgmtConfigurationRecord.Create(
                    configRoot, _configRecord, locationConfigPath, locationSubPath);
            }

            // Throw if the config record we created contains global errors.
            _configRecord.ThrowIfInitErrors();
        }

        public AppSettingsSection AppSettings => (AppSettingsSection)GetSection("appSettings");

        public ConnectionStringsSection ConnectionStrings => (ConnectionStringsSection)GetSection("connectionStrings");

        public string FilePath => _configRecord.ConfigurationFilePath;

        public bool HasFile => _configRecord.HasStream;

        public ConfigurationLocationCollection Locations
            => _locations ?? (_locations = _configRecord.GetLocationCollection(this));

        public ContextInformation EvaluationContext
            => _evalContext ?? (_evalContext = new ContextInformation(_configRecord));

        public ConfigurationSectionGroup RootSectionGroup
        {
            get
            {
                if (_rootSectionGroup == null)
                {
                    _rootSectionGroup = new ConfigurationSectionGroup();
                    _rootSectionGroup.RootAttachToConfigurationRecord(_configRecord);
                }

                return _rootSectionGroup;
            }
        }

        public ConfigurationSectionCollection Sections => RootSectionGroup.Sections;

        public ConfigurationSectionGroupCollection SectionGroups => RootSectionGroup.SectionGroups;

        // Is the namespace declared in the file or not?
        //
        // ie. xmlns="http://schemas.microsoft.com/.NetConfiguration/v2.0"
        //     (currently this is the only one we allow)
        public bool NamespaceDeclared
        {
            get { return _configRecord.NamespacePresent; }
            set { _configRecord.NamespacePresent = value; }
        }

        public Func<string, string> TypeStringTransformer
        {
            get { return _typeStringTransformer; }
            set
            {
                if (_typeStringTransformer != value)
                {
                    TypeStringTransformerIsSet = value != null;
                    _typeStringTransformer = value;
                }
            }
        }

        public Func<string, string> AssemblyStringTransformer
        {
            get { return _assemblyStringTransformer; }
            set
            {
                if (_assemblyStringTransformer != value)
                {
                    AssemblyStringTransformerIsSet = value != null;
                    _assemblyStringTransformer = value;
                }
            }
        }

        public FrameworkName TargetFramework
        {
            get; set;
        } = null;

        internal bool TypeStringTransformerIsSet { get; private set; }

        internal bool AssemblyStringTransformerIsSet { get; private set; }

        internal Stack SectionsStack => _sectionsStack ?? (_sectionsStack = new Stack());

        // Create a new instance of Configuration for the locationSubPath,
        // with the initialization parameters that were used to create this configuration.
        internal Configuration OpenLocationConfiguration(string locationSubPath)
        {
            return new Configuration(locationSubPath, _typeConfigHost, _hostInitConfigurationParams);
        }

        // public methods
        public ConfigurationSection GetSection(string sectionName)
        {
            ConfigurationSection section = (ConfigurationSection)_configRecord.GetSection(sectionName);

            return section;
        }

        public ConfigurationSectionGroup GetSectionGroup(string sectionGroupName)
        {
            ConfigurationSectionGroup sectionGroup = _configRecord.GetSectionGroup(sectionGroupName);

            return sectionGroup;
        }

        public void Save()
        {
            SaveAsImpl(null, ConfigurationSaveMode.Modified, false);
        }

        public void Save(ConfigurationSaveMode saveMode)
        {
            SaveAsImpl(null, saveMode, false);
        }

        public void Save(ConfigurationSaveMode saveMode, bool forceSaveAll)
        {
            SaveAsImpl(null, saveMode, forceSaveAll);
        }

        public void SaveAs(string filename)
        {
            SaveAs(filename, ConfigurationSaveMode.Modified, false);
        }

        public void SaveAs(string filename, ConfigurationSaveMode saveMode)
        {
            SaveAs(filename, saveMode, false);
        }

        public void SaveAs(string filename, ConfigurationSaveMode saveMode, bool forceSaveAll)
        {
            if (string.IsNullOrEmpty(filename)) throw ExceptionUtil.ParameterNullOrEmpty(nameof(filename));

            SaveAsImpl(filename, saveMode, forceSaveAll);
        }

        private void SaveAsImpl(string filename, ConfigurationSaveMode saveMode, bool forceSaveAll)
        {
            filename = string.IsNullOrEmpty(filename) ? null : Path.GetFullPath(filename);

            if (forceSaveAll) ForceGroupsRecursive(RootSectionGroup);
            _configRecord.SaveAs(filename, saveMode, forceSaveAll);
        }

        // Force all sections and section groups to be instantiated.
        private void ForceGroupsRecursive(ConfigurationSectionGroup group)
        {
            foreach (ConfigurationSection configSection in group.Sections)
            {
                // Force the section to be read into the cache
                ConfigurationSection section = group.Sections[configSection.SectionInformation.Name];
            }

            foreach (ConfigurationSectionGroup sectionGroup in group.SectionGroups)
                ForceGroupsRecursive(group.SectionGroups[sectionGroup.Name]);
        }
    }
}