// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Diagnostics;

namespace System.Configuration
{
    public sealed class SectionInformation
    {
        // Flags
        private const int FlagAttached = 0x00000001;
        private const int FlagDeclared = 0x00000002;
        private const int FlagDeclarationRequired = 0x00000004;
        private const int FlagAllowLocation = 0x00000008;
        private const int FlagRestartOnExternalChanges = 0x00000010;
        private const int FlagRequirePermission = 0x00000020;
        private const int FlagLocationLocked = 0x00000040;
        private const int FlagChildrenLocked = 0x00000080;
        private const int FlagInheritInChildApps = 0x00000100;
        private const int FlagIsParentSection = 0x00000200;
        private const int FlagRemoved = 0x00000400;
        private const int FlagProtectionProviderDetermined = 0x00000800;
        private const int FlagForceSave = 0x00001000;
        private const int FlagIsUndeclared = 0x00002000;
        private const int FlagChildrenLockWithoutFileInput = 0x00004000;

        private const int FlagAllowExeDefinitionModified = 0x00010000;
        private const int FlagAllowDefinitionModified = 0x00020000;
        private const int FlagConfigSourceModified = 0x00040000;
        private const int FlagProtectionProviderModified = 0x00080000;
        private const int FlagOverrideModeDefaultModified = 0x00100000;
        private const int FlagOverrideModeModified = 0x00200000; // Used only for modified tracking

        private readonly ConfigurationSection _configurationSection;
        private ConfigurationAllowDefinition _allowDefinition;
        private ConfigurationAllowExeDefinition _allowExeDefinition;
        private MgmtConfigurationRecord _configRecord;
        private string _configSource;
        private SafeBitVector32 _flags;
        private SimpleBitVector32 _modifiedFlags;
        private OverrideModeSetting _overrideMode; // The override mode at the current config path
        private OverrideModeSetting _overrideModeDefault; // The default mode for the section in _configurationSection
        private ProtectedConfigurationProvider _protectionProvider;
        private string _typeName;

        internal SectionInformation(ConfigurationSection associatedConfigurationSection)
        {
            ConfigKey = string.Empty;
            Name = string.Empty;

            _configurationSection = associatedConfigurationSection;
            _allowDefinition = ConfigurationAllowDefinition.Everywhere;
            _allowExeDefinition = ConfigurationAllowExeDefinition.MachineToApplication;
            _overrideModeDefault = OverrideModeSetting.s_sectionDefault;
            _overrideMode = OverrideModeSetting.s_locationDefault;

            _flags[FlagAllowLocation] = true;
            _flags[FlagRestartOnExternalChanges] = true;
            _flags[FlagRequirePermission] = true;
            _flags[FlagInheritInChildApps] = true;
            _flags[FlagForceSave] = false;

            _modifiedFlags = new SimpleBitVector32();
        }

        private bool IsRuntime => _flags[FlagAttached] &&
            (_configRecord == null);

        internal bool Attached => _flags[FlagAttached];

        internal string ConfigKey { get; private set; }

        internal bool Removed
        {
            get { return _flags[FlagRemoved]; }
            set { _flags[FlagRemoved] = value; }
        }

        public string SectionName => ConfigKey;

        public string Name { get; private set; }

        public ConfigurationAllowDefinition AllowDefinition
        {
            get { return _allowDefinition; }
            set
            {
                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow AllowDefinition to be different from current type,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if ((factoryRecord != null) && (factoryRecord.AllowDefinition != value))
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, ConfigKey));

                _allowDefinition = value;
                _modifiedFlags[FlagAllowDefinitionModified] = true;
            }
        }

        internal bool AllowDefinitionModified => _modifiedFlags[FlagAllowDefinitionModified];

        public ConfigurationAllowExeDefinition AllowExeDefinition
        {
            get { return _allowExeDefinition; }
            set
            {
                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow AllowDefinition to be different from current type,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if ((factoryRecord != null) && (factoryRecord.AllowExeDefinition != value))
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, ConfigKey));

                _allowExeDefinition = value;
                _modifiedFlags[FlagAllowExeDefinitionModified] = true;
            }
        }

        internal bool AllowExeDefinitionModified => _modifiedFlags[FlagAllowExeDefinitionModified];

        public OverrideMode OverrideModeDefault
        {
            get { return _overrideModeDefault.OverrideMode; }
            set
            {
                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow OverrideModeDefault to be different from current value,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if ((factoryRecord != null) && (factoryRecord.OverrideModeDefault.OverrideMode != value))
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, ConfigKey));

                // Threat "Inherit" as "Allow" as "Inherit" does not make sense as a default
                if (value == OverrideMode.Inherit) value = OverrideMode.Allow;

                _overrideModeDefault.OverrideMode = value;
                _modifiedFlags[FlagOverrideModeDefaultModified] = true;
            }
        }

        internal OverrideModeSetting OverrideModeDefaultSetting => _overrideModeDefault;

        internal bool OverrideModeDefaultModified => _modifiedFlags[FlagOverrideModeDefaultModified];

        public bool AllowLocation
        {
            get { return _flags[FlagAllowLocation]; }
            set
            {
                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow AllowLocation to be different from current type,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if ((factoryRecord != null) && (factoryRecord.AllowLocation != value))
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, ConfigKey));

                _flags[FlagAllowLocation] = value;
                _modifiedFlags[FlagAllowLocation] = true;
            }
        }

        internal bool AllowLocationModified => _modifiedFlags[FlagAllowLocation];

        public bool AllowOverride
        {
            get { return _overrideMode.AllowOverride; }
            set
            {
                VerifyIsEditable();
                VerifySupportsLocation();
                _overrideMode.AllowOverride = value;
                _modifiedFlags[FlagOverrideModeModified] = true;
            }
        }

        public OverrideMode OverrideMode
        {
            get { return _overrideMode.OverrideMode; }
            set
            {
                VerifyIsEditable();
                VerifySupportsLocation();
                _overrideMode.OverrideMode = value;
                _modifiedFlags[FlagOverrideModeModified] = true;

                // Modify the state of OverrideModeEffective according to the changes to the current mode
                switch (value)
                {
                    case OverrideMode.Inherit:
                        // Wehen the state is changing to inherit - apply the value from parent which does not
                        // include the file input lock mode
                        _flags[FlagChildrenLocked] = _flags[FlagChildrenLockWithoutFileInput];
                        break;
                    case OverrideMode.Allow:
                        _flags[FlagChildrenLocked] = false;
                        break;
                    case OverrideMode.Deny:
                        _flags[FlagChildrenLocked] = true;
                        break;
                    default:
                        Debug.Fail("Unexpected value for OverrideMode");
                        break;
                }
            }
        }

        public OverrideMode OverrideModeEffective => _flags[FlagChildrenLocked] ? OverrideMode.Deny : OverrideMode.Allow
            ;

        internal OverrideModeSetting OverrideModeSetting => _overrideMode;


        // LocationAttributesAreDefault
        //
        // Are the location attributes for this section set to the
        // default settings?
        internal bool LocationAttributesAreDefault => _overrideMode.IsDefaultForLocationTag &&
            _flags[FlagInheritInChildApps];

        public string ConfigSource
        {
            get { return _configSource ?? string.Empty; }
            set
            {
                VerifyIsEditable();

                string configSource = !string.IsNullOrEmpty(value)
                    ? BaseConfigurationRecord.NormalizeConfigSource(value, null)
                    : null;

                // return early if there is no change
                if (configSource == _configSource)
                    return;

                _configRecord?.ChangeConfigSource(this, _configSource, ConfigSourceStreamName, configSource);

                _configSource = configSource;
                _modifiedFlags[FlagConfigSourceModified] = true;
            }
        }

        internal bool ConfigSourceModified => _modifiedFlags[FlagConfigSourceModified];

        internal string ConfigSourceStreamName { get; set; }

        public bool InheritInChildApplications
        {
            get { return _flags[FlagInheritInChildApps]; }
            set
            {
                VerifyIsEditable();
                VerifySupportsLocation();
                _flags[FlagInheritInChildApps] = value;
            }
        }

        // True if the section is declared at the current level
        public bool IsDeclared
        {
            get
            {
                VerifyNotParentSection();

                return _flags[FlagDeclared];
            }
        }

        // Is the Declaration Required.  It is required if it is not set
        // in a parent, or the parent entry does not have the type
        public bool IsDeclarationRequired
        {
            get
            {
                VerifyNotParentSection();

                return _flags[FlagDeclarationRequired];
            }
        }

        // Is the Definition Allowed at this point.  This is all depending
        // on the Definition that is allowed, and what context we are 
        // writing the file
        private bool IsDefinitionAllowed
            => (_configRecord == null) || _configRecord.IsDefinitionAllowed(_allowDefinition, _allowExeDefinition);

        public bool IsLocked => _flags[FlagLocationLocked] || !IsDefinitionAllowed ||
            _configurationSection.ElementInformation.IsLocked;

        public bool IsProtected => ProtectionProvider != null;

        public ProtectedConfigurationProvider ProtectionProvider
        {
            get
            {
                if (!_flags[FlagProtectionProviderDetermined] && (_configRecord != null))
                {
                    _protectionProvider = _configRecord.GetProtectionProviderFromName(ProtectionProviderName, false);
                    _flags[FlagProtectionProviderDetermined] = true;
                }

                return _protectionProvider;
            }
        }

        internal string ProtectionProviderName { get; private set; }

        public bool RestartOnExternalChanges
        {
            get { return _flags[FlagRestartOnExternalChanges]; }
            set
            {
                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow RestartOnExternalChanges to be different from current type,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if ((factoryRecord != null) && (factoryRecord.RestartOnExternalChanges != value))
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, ConfigKey));

                _flags[FlagRestartOnExternalChanges] = value;
                _modifiedFlags[FlagRestartOnExternalChanges] = true;
            }
        }

        internal bool RestartOnExternalChangesModified => _modifiedFlags[FlagRestartOnExternalChanges];

        public bool RequirePermission
        {
            get { return _flags[FlagRequirePermission]; }
            set
            {
                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow RequirePermission to be different from current type,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if ((factoryRecord != null) && (factoryRecord.RequirePermission != value))
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined, ConfigKey));

                _flags[FlagRequirePermission] = value;
                _modifiedFlags[FlagRequirePermission] = true;
            }
        }

        internal bool RequirePermissionModified => _modifiedFlags[FlagRequirePermission];


        public string Type
        {
            get { return _typeName; }
            set
            {
                if (string.IsNullOrEmpty(value)) throw ExceptionUtil.PropertyNullOrEmpty(nameof(Type));

                VerifyIsEditable();
                VerifyIsEditableFactory();

                // allow type to be different from current type,
                // so long as it doesn't conflict with a type already defined
                FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                if (factoryRecord != null)
                {
                    IInternalConfigHost host = null;
                    if (_configRecord != null) host = _configRecord.Host;

                    if (!factoryRecord.IsEquivalentType(host, value))
                    {
                        throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined,
                            ConfigKey));
                    }
                }

                _typeName = value;
            }
        }

        internal string RawXml { get; set; }

        // True if the section will be serialized to the current hierarchy level, regardless of 
        // ConfigurationSaveMode.
        public bool ForceSave
        {
            get { return _flags[FlagForceSave]; }
            set
            {
                VerifyIsEditable();

                _flags[FlagForceSave] = value;
            }
        }

        internal void ResetModifiedFlags()
        {
            _modifiedFlags = new SimpleBitVector32();
        }

        internal bool IsModifiedFlags()
        {
            return _modifiedFlags.Data != 0;
        }

        // for instantiation of a ConfigurationSection from GetConfig
        internal void AttachToConfigurationRecord(MgmtConfigurationRecord configRecord, FactoryRecord factoryRecord,
            SectionRecord sectionRecord)
        {
            SetRuntimeConfigurationInformation(configRecord, factoryRecord, sectionRecord);
            _configRecord = configRecord;
        }

        internal void SetRuntimeConfigurationInformation(BaseConfigurationRecord configRecord,
            FactoryRecord factoryRecord, SectionRecord sectionRecord)
        {
            _flags[FlagAttached] = true;

            // factory info
            ConfigKey = factoryRecord.ConfigKey;
            Name = factoryRecord.Name;
            _typeName = factoryRecord.FactoryTypeName;
            _allowDefinition = factoryRecord.AllowDefinition;
            _allowExeDefinition = factoryRecord.AllowExeDefinition;
            _flags[FlagAllowLocation] = factoryRecord.AllowLocation;
            _flags[FlagRestartOnExternalChanges] = factoryRecord.RestartOnExternalChanges;
            _flags[FlagRequirePermission] = factoryRecord.RequirePermission;
            _overrideModeDefault = factoryRecord.OverrideModeDefault;

            if (factoryRecord.IsUndeclared)
            {
                _flags[FlagIsUndeclared] = true;
                _flags[FlagDeclared] = false;
                _flags[FlagDeclarationRequired] = false;
            }
            else
            {
                _flags[FlagIsUndeclared] = false;
                _flags[FlagDeclared] = configRecord.GetFactoryRecord(factoryRecord.ConfigKey, false) != null;
                _flags[FlagDeclarationRequired] = configRecord.IsRootDeclaration(factoryRecord.ConfigKey, false);
            }

            // section info
            _flags[FlagLocationLocked] = sectionRecord.Locked;
            _flags[FlagChildrenLocked] = sectionRecord.LockChildren;
            _flags[FlagChildrenLockWithoutFileInput] = sectionRecord.LockChildrenWithoutFileInput;

            if (sectionRecord.HasFileInput)
            {
                SectionInput fileInput = sectionRecord.FileInput;

                _flags[FlagProtectionProviderDetermined] = fileInput.IsProtectionProviderDetermined;
                _protectionProvider = fileInput.ProtectionProvider;

                SectionXmlInfo sectionXmlInfo = fileInput.SectionXmlInfo;

                _configSource = sectionXmlInfo.ConfigSource;
                ConfigSourceStreamName = sectionXmlInfo.ConfigSourceStreamName;
                _overrideMode = sectionXmlInfo.OverrideModeSetting;
                _flags[FlagInheritInChildApps] = !sectionXmlInfo.SkipInChildApps;
                ProtectionProviderName = sectionXmlInfo.ProtectionProviderName;
            }
            else
            {
                _flags[FlagProtectionProviderDetermined] = false;
                _protectionProvider = null;
            }

            // element context information
            _configurationSection.AssociateContext(configRecord);
        }

        internal void DetachFromConfigurationRecord()
        {
            RevertToParent();
            _flags[FlagAttached] = false;
            _configRecord = null;
        }

        private void VerifyDesigntime()
        {
            if (IsRuntime) throw new InvalidOperationException(SR.Config_operation_not_runtime);
        }

        private void VerifyIsAttachedToConfigRecord()
        {
            if (_configRecord == null)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_when_not_attached);
        }

        // VerifyIsEditable 
        //
        // Verify the section is Editable.  
        // It may not be editable for the following reasons:
        //   - We are in Runtime mode, not Design time
        //   - The section is not attached to a _configRecord.
        //   - We are locked (ie. allowOveride = false )
        //   - We are a parent section (ie. Retrieved from GetParentSection)
        //
        internal void VerifyIsEditable()
        {
            VerifyDesigntime();

            if (IsLocked)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_when_locked);

            if (_flags[FlagIsParentSection])
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_parentsection);

            if (!_flags[FlagAllowLocation] &&
                (_configRecord != null) &&
                _configRecord.IsLocationConfig)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_when_location_locked);
        }

        private void VerifyNotParentSection()
        {
            if (_flags[FlagIsParentSection])
                throw new InvalidOperationException(SR.Config_configsection_parentnotvalid);
        }

        // Verify that we support the location tag.  This is true either
        // in machine.config, or in any place for the web config system
        private void VerifySupportsLocation()
        {
            if ((_configRecord != null) &&
                !_configRecord.RecordSupportsLocation)
                throw new InvalidOperationException(SR.Config_cannot_edit_locationattriubtes);
        }

        internal void VerifyIsEditableFactory()
        {
            if ((_configRecord != null) && _configRecord.IsLocationConfig)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_in_location_config);

            // Can't edit factory if the section is built-in
            if (BaseConfigurationRecord.IsImplicitSection(ConfigKey))
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_when_it_is_implicit);

            // Can't edit undeclared section
            if (_flags[FlagIsUndeclared])
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsection_when_it_is_undeclared);
        }

        private FactoryRecord FindParentFactoryRecord(bool permitErrors)
        {
            FactoryRecord factoryRecord = null;

            if ((_configRecord != null) && !_configRecord.Parent.IsRootConfig)
                factoryRecord = _configRecord.Parent.FindFactoryRecord(ConfigKey, permitErrors);

            return factoryRecord;
        }

        // Force the section declaration to be written out during Save.
        public void ForceDeclaration()
        {
            ForceDeclaration(true);
        }

        // If force==false, it actually means don't declare it at
        // the current level.
        public void ForceDeclaration(bool force)
        {
            VerifyIsEditable();

            if ((force == false) &&
                _flags[FlagDeclarationRequired])
            {
                // Since it is required, we can not remove it
            }
            else
            {
                // CONSIDER: There is no apriori way to determine if a section
                // is implicit or undeclared. Would it be better to simply
                // fail silently so that app code can easily loop through sections
                // and declare all of them?
                if (force && BaseConfigurationRecord.IsImplicitSection(SectionName))
                    throw new ConfigurationErrorsException(SR.Format(SR.Cannot_declare_or_remove_implicit_section, SectionName));

                if (force && _flags[FlagIsUndeclared])
                {
                    throw new ConfigurationErrorsException(
                        SR.Config_cannot_edit_configurationsection_when_it_is_undeclared);
                }

                _flags[FlagDeclared] = force;
            }
        }

        // method to cause a section to be protected using the specified provider
        public void ProtectSection(string protectionProvider)
        {
            ProtectedConfigurationProvider protectedConfigurationProvider;

            VerifyIsEditable();

            // Do not encrypt sections that will be read by a native reader.
            // These sections are be marked with allowLocation=false.
            // Also, the configProtectedData section cannot be encrypted!
            if (!AllowLocation || (ConfigKey == BaseConfigurationRecord.ReservedSectionProtectedConfiguration))
                throw new InvalidOperationException(SR.Config_not_allowed_to_encrypt_this_section);

            if (_configRecord != null)
            {
                if (string.IsNullOrEmpty(protectionProvider)) protectionProvider = _configRecord.DefaultProviderName;

                protectedConfigurationProvider = _configRecord.GetProtectionProviderFromName(protectionProvider, true);
            }
            else throw new InvalidOperationException(SR.Must_add_to_config_before_protecting_it);

            ProtectionProviderName = protectionProvider;
            _protectionProvider = protectedConfigurationProvider;

            _flags[FlagProtectionProviderDetermined] = true;
            _modifiedFlags[FlagProtectionProviderModified] = true;
        }

        public void UnprotectSection()
        {
            VerifyIsEditable();

            _protectionProvider = null;
            ProtectionProviderName = null;
            _flags[FlagProtectionProviderDetermined] = true;
            _modifiedFlags[FlagProtectionProviderModified] = true;
        }

        public ConfigurationSection GetParentSection()
        {
            VerifyDesigntime();

            if (_flags[FlagIsParentSection])
                throw new InvalidOperationException(SR.Config_getparentconfigurationsection_first_instance);

            // if a users create a configsection with : sectionType sec  = new sectionType();
            // the config record will be null.  Return null for the parent in this case.
            ConfigurationSection ancestor = null;
            if (_configRecord != null)
            {
                ancestor = _configRecord.FindAndCloneImmediateParentSection(_configurationSection);
                if (ancestor != null)
                {
                    ancestor.SectionInformation._flags[FlagIsParentSection] = true;
                    ancestor.SetReadOnly();
                }
            }

            return ancestor;
        }

        public string GetRawXml()
        {
            VerifyDesigntime();
            VerifyNotParentSection();

            return RawXml ?? _configRecord?.GetRawXml(ConfigKey);
        }

        public void SetRawXml(string rawXml)
        {
            VerifyIsEditable();

            if (_configRecord != null) _configRecord.SetRawXml(_configurationSection, rawXml);
            else RawXml = string.IsNullOrEmpty(rawXml) ? null : rawXml;
        }

        public void RevertToParent()
        {
            VerifyIsEditable();
            VerifyIsAttachedToConfigRecord();

            _configRecord.RevertToParent(_configurationSection);
        }
    }
}
