// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Versioning;

namespace System.Configuration
{
    public class ConfigurationSectionGroup
    {
        private MgmtConfigurationRecord _configRecord;
        private ConfigurationSectionGroupCollection _configSectionGroups;
        private ConfigurationSectionCollection _configSections;
        private string _typeName;

        internal bool Attached => _configRecord != null;

        public bool IsDeclared { get; private set; }

        // Is the Declaration Required.  It is required if it is not set
        // int a parent, or the parent entry does not have the type
        public bool IsDeclarationRequired { get; private set; }

        public string SectionGroupName { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string Type
        {
            get { return _typeName; }
            set
            {
                if (IsRoot)
                    throw new InvalidOperationException(SR.Config_root_section_group_cannot_be_edited);

                // Since type is optional for a section group, allow it to be removed.
                // Note that a typename of "" is not permitted in the config file.
                string typeName = value;
                if (string.IsNullOrEmpty(typeName)) typeName = null;

                if (_configRecord != null)
                {
                    if (_configRecord.IsLocationConfig)
                    {
                        throw new InvalidOperationException(
                            SR.Config_cannot_edit_configurationsectiongroup_in_location_config);
                    }

                    // allow type to be different from current type,
                    // so long as it doesn't conflict with a type already defined
                    if (typeName != null)
                    {
                        FactoryRecord factoryRecord = FindParentFactoryRecord(false);
                        if ((factoryRecord != null) && !factoryRecord.IsEquivalentType(_configRecord.Host, typeName))
                        {
                            throw new ConfigurationErrorsException(SR.Format(SR.Config_tag_name_already_defined,
                                SectionGroupName));
                        }
                    }
                }

                _typeName = typeName;
            }
        }

        public ConfigurationSectionCollection Sections
        {
            get
            {
                if (_configSections == null)
                {
                    VerifyIsAttachedToConfigRecord();
                    _configSections = new ConfigurationSectionCollection(_configRecord, this);
                }

                return _configSections;
            }
        }

        public ConfigurationSectionGroupCollection SectionGroups
        {
            get
            {
                if (_configSectionGroups == null)
                {
                    VerifyIsAttachedToConfigRecord();
                    _configSectionGroups = new ConfigurationSectionGroupCollection(_configRecord, this);
                }

                return _configSectionGroups;
            }
        }

        internal bool IsRoot { get; private set; }

        internal void AttachToConfigurationRecord(MgmtConfigurationRecord configRecord, FactoryRecord factoryRecord)
        {
            _configRecord = configRecord;
            SectionGroupName = factoryRecord.ConfigKey;
            Name = factoryRecord.Name;
            _typeName = factoryRecord.FactoryTypeName;

            if (_typeName != null)
            {
                FactoryRecord parentFactoryRecord = null;
                if (!configRecord.Parent.IsRootConfig)
                    parentFactoryRecord = configRecord.Parent.FindFactoryRecord(factoryRecord.ConfigKey, true);

                IsDeclarationRequired = parentFactoryRecord?.FactoryTypeName == null;
                IsDeclared = configRecord.GetFactoryRecord(factoryRecord.ConfigKey, true) != null;
            }
        }

        internal void RootAttachToConfigurationRecord(MgmtConfigurationRecord configRecord)
        {
            _configRecord = configRecord;

            IsRoot = true;
        }

        internal void DetachFromConfigurationRecord()
        {
            _configSections?.DetachFromConfigurationRecord();
            _configSectionGroups?.DetachFromConfigurationRecord();
            _configRecord = null;
        }

        private FactoryRecord FindParentFactoryRecord(bool permitErrors)
        {
            FactoryRecord factoryRecord = null;

            if ((_configRecord != null) && !_configRecord.Parent.IsRootConfig)
                factoryRecord = _configRecord.Parent.FindFactoryRecord(SectionGroupName, permitErrors);

            return factoryRecord;
        }

        private void VerifyIsAttachedToConfigRecord()
        {
            if (_configRecord == null)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsectiongroup_when_not_attached);
        }

        public void ForceDeclaration()
        {
            ForceDeclaration(true);
        }

        // Force the declaration to be written.  If this is false, it
        // may be ignored depending on if it is Required
        public void ForceDeclaration(bool force)
        {
            if (IsRoot)
                throw new InvalidOperationException(SR.Config_root_section_group_cannot_be_edited);

            if ((_configRecord != null) && _configRecord.IsLocationConfig)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsectiongroup_in_location_config);

            if (!force && IsDeclarationRequired)
            {
                // Since it is required, we can not remove it
            }
            else IsDeclared = force;
        }

        protected internal virtual bool ShouldSerializeSectionGroupInTargetVersion(FrameworkName targetFramework)
        {
            return true;
        }
    }
}
