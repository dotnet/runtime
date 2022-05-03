// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Internal;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace System.Configuration
{
    internal sealed class MgmtConfigurationRecord : BaseConfigurationRecord
    {
        private const int DefaultIndent = 4;
        private const int MaxIndent = 10;

        private static readonly SimpleBitVector32 s_mgmtClassFlags = new SimpleBitVector32(
            ClassSupportsKeepInputs
            | ClassIgnoreLocalErrors);

        private Hashtable _locationTags;
        private Hashtable _removedSectionGroups;
        private Hashtable _removedSections;
        private Hashtable _sectionFactories;
        private Hashtable _sectionGroupFactories;
        private Hashtable _sectionGroups;
        private HybridDictionary _streamInfoUpdates;

        // don't allow instantiation except by Create
        private MgmtConfigurationRecord() { }

        // The parent config record cast to this type
        private MgmtConfigurationRecord MgmtParent => (MgmtConfigurationRecord)_parent;

        // The IInternalConfigHost cast to UpdateConfigHost.
        private UpdateConfigHost UpdateConfigHost => _configRoot.UpdateConfigHost;

        protected override SimpleBitVector32 ClassFlags => s_mgmtClassFlags;

        private Hashtable SectionGroups => _sectionGroups ?? (_sectionGroups = new Hashtable());

        private Hashtable RemovedSections => _removedSections ?? (_removedSections = new Hashtable());

        private Hashtable RemovedSectionGroups => _removedSectionGroups ?? (_removedSectionGroups = new Hashtable());

        internal Hashtable SectionFactories => _sectionFactories ?? (_sectionFactories = GetAllFactories(false));

        internal Hashtable SectionGroupFactories
            => _sectionGroupFactories ?? (_sectionGroupFactories = GetAllFactories(true));

        internal string ConfigurationFilePath => UpdateConfigHost.GetNewStreamname(ConfigStreamInfo.StreamName) ?? string.Empty;

        private bool HasRemovedSectionsOrGroups => ((_removedSections != null) && (_removedSections.Count > 0))
            || ((_removedSectionGroups != null) && (_removedSectionGroups.Count > 0));

        private bool HasRemovedSections => (_removedSections != null) && (_removedSections.Count > 0);

        internal bool NamespacePresent
        {
            get { return _flags[NamespacePresentCurrent]; }
            set { _flags[NamespacePresentCurrent] = value; }
        }

        // On Update, do we need to add the namespace, remove it, or do nothing?
        private NamespaceChange NamespaceChangeNeeded
        {
            get
            {
                if (_flags[NamespacePresentCurrent] == _flags[NamespacePresentInFile]) return NamespaceChange.None;
                return _flags[NamespacePresentCurrent] ? NamespaceChange.Add : NamespaceChange.Remove;
            }
        }

        // Outside the scope of the sections and there definitions, does
        // the record itself require an update.
        private bool RecordItselfRequiresUpdates => NamespaceChangeNeeded != NamespaceChange.None;

        internal static MgmtConfigurationRecord Create(
            IInternalConfigRoot configRoot,
            IInternalConfigRecord parent,
            string configPath,
            string locationSubPath)
        {
            MgmtConfigurationRecord configRecord = new MgmtConfigurationRecord();
            configRecord.Init(configRoot, parent, configPath, locationSubPath);
            return configRecord;
        }

        private void Init(
            IInternalConfigRoot configRoot,
            IInternalConfigRecord parent,
            string configPath,
            string locationSubPath)
        {
            base.Init(configRoot, (BaseConfigurationRecord)parent, configPath, locationSubPath);

            if (IsLocationConfig &&
                ((MgmtParent._locationTags == null) || !MgmtParent._locationTags.Contains(_locationSubPath)))
            {
                // By instantiating a "new" LocationSubPath class, we have implicitly
                // asked for one to be created
                _flags[ForceLocationWritten] = true;
            }

            // Copy all stream information so that we can model changes to ConfigSource
            InitStreamInfoUpdates();
        }

        private void InitStreamInfoUpdates()
        {
            _streamInfoUpdates = new HybridDictionary(true);
            if (ConfigStreamInfo.HasStreamInfos)
            {
                foreach (StreamInfo streamInfo in ConfigStreamInfo.StreamInfos.Values)
                    _streamInfoUpdates.Add(streamInfo.StreamName, streamInfo.Clone());
            }
        }

        protected override object CreateSectionFactory(FactoryRecord factoryRecord)
        {
            // Get the type of the factory
            Type type = TypeUtil.GetType(Host, factoryRecord.FactoryTypeName, throwOnError: true);

            // If the type is not a ConfigurationSection, use the DefaultSection if the type
            // implements IConfigurationSectionHandler.
            if (!typeof(ConfigurationSection).IsAssignableFrom(type))
            {
                TypeUtil.VerifyAssignableType(typeof(IConfigurationSectionHandler), type, true);
                type = typeof(DefaultSection);
            }

            ConstructorInfo ctor = TypeUtil.GetConstructor(type, typeof(ConfigurationSection), true);

            return ctor;
        }

        protected override object CreateSection(bool inputIsTrusted, FactoryRecord factoryRecord,
            SectionRecord sectionRecord, object parentConfig, ConfigXmlReader reader)
        {
            // Create an instance of the ConfigurationSection
            ConstructorInfo ctor = (ConstructorInfo)factoryRecord.Factory;
            ConfigurationSection configSection =
                (ConfigurationSection)ctor.Invoke(null);

            // Attach the ConfigurationSection to this record
            configSection.SectionInformation.AttachToConfigurationRecord(this, factoryRecord, sectionRecord);
            configSection.CallInit();

            // Initialize the ConfigurationSection with XML or just its parent.
            ConfigurationSection parentConfigSection = (ConfigurationSection)parentConfig;
            configSection.Reset(parentConfigSection);
            if (reader != null) configSection.DeserializeSection(reader);

            // Clear the modified bit.
            configSection.ResetModified();

            return configSection;
        }

        private ConstructorInfo CreateSectionGroupFactory(FactoryRecord factoryRecord)
        {
            Type type = string.IsNullOrEmpty(factoryRecord.FactoryTypeName)
                ? typeof(ConfigurationSectionGroup)
                : TypeUtil.GetType(Host, factoryRecord.FactoryTypeName, true);

            ConstructorInfo ctor = TypeUtil.GetConstructor(type,
                typeof(ConfigurationSectionGroup), true);

            return ctor;
        }

        private ConstructorInfo EnsureSectionGroupFactory(FactoryRecord factoryRecord)
        {
            ConstructorInfo factory = (ConstructorInfo)factoryRecord.Factory;
            if (factory == null)
            {
                factory = CreateSectionGroupFactory(factoryRecord);
                factoryRecord.Factory = factory;
            }

            return factory;
        }


        // Create a new ConfigurationSection with the same values as the parent.
        // We must use a different instance than the parent, as the parent is cached
        // by the config system and the child ConfigurationSection may change due to
        // user interaction.
        protected override object UseParentResult(string configKey, object parentResult, SectionRecord sectionRecord)
        {
            FactoryRecord factoryRecord = FindFactoryRecord(configKey, false);
            if (factoryRecord == null)
            {
                throw new ConfigurationErrorsException(SR.Format(SR.Config_unrecognized_configuration_section,
                    configKey));
            }

            object result = CallCreateSection(false, factoryRecord, sectionRecord, parentResult, null, null, -1);
            return result;
        }

        // There is no runtime object at designtime - always return the result.
        protected override object GetRuntimeObject(object result)
        {
            return result;
        }

        // Return the section result cast to a ConfigurationSection,
        // or null if the section does not exist or has not been evaluated.
        private ConfigurationSection GetConfigSection(string configKey)
        {
            SectionRecord sectionRecord = GetSectionRecord(configKey, false);
            if ((sectionRecord != null) && sectionRecord.HasResult) return (ConfigurationSection)sectionRecord.Result;
            return null;
        }

        // Lookup a section group. Return null if it doesn't exist or hasn't been evaluated.
        internal ConfigurationSectionGroup LookupSectionGroup(string configKey)
        {
            ConfigurationSectionGroup configSectionGroup = null;
            if (_sectionGroups != null) configSectionGroup = (ConfigurationSectionGroup)_sectionGroups[configKey];

            return configSectionGroup;
        }

        // Returns the ConfigurationSectionGroup of the configKey.
        // The ConfigurationSectionGroup is created if it doesn't exist.
        // This method only returns null if a FactoryRecord does not exist for the
        // desired configKey.
        internal ConfigurationSectionGroup GetSectionGroup(string configKey)
        {
            ConfigurationSectionGroup configSectionGroup = LookupSectionGroup(configKey);
            if (configSectionGroup != null) return configSectionGroup;

            FactoryRecord factoryRecord = FindFactoryRecord(configKey, false, out _);
            if (factoryRecord == null) return null;

            if (!factoryRecord.IsGroup) throw ExceptionUtil.ParameterInvalid("sectionGroupName");

            if (factoryRecord.FactoryTypeName == null)
            {
                // If no type is defined for the section group, return a base ConfigurationSectionGroup.
                // For example:
                //  <configSections>
                //      <sectionGroup name="mySectionGroup" />
                //  </configSections>
                configSectionGroup = new ConfigurationSectionGroup();
            }
            else
            {
                // Create the section group of the desired type.
                // For example:
                //  <configSections>
                //      <sectionGroup name="mySectionGroup" type="MySectionGroupType, acme" />
                //  </configSections>
                ConstructorInfo ctor = EnsureSectionGroupFactory(factoryRecord);

                try
                {
                    configSectionGroup =
                        (ConfigurationSectionGroup)ctor.Invoke(null);
                }
                catch (Exception e)
                {
                    throw new ConfigurationErrorsException(
                        SR.Format(SR.Config_exception_creating_section_handler, factoryRecord.ConfigKey),
                        e, factoryRecord);
                }
            }

            configSectionGroup.AttachToConfigurationRecord(this, factoryRecord);

            // Add it to the collection
            SectionGroups[configKey] = configSectionGroup;

            return configSectionGroup;
        }

        // Create a collection of all location tags encountered in the file.
        internal ConfigurationLocationCollection GetLocationCollection(Configuration config)
        {
            ArrayList locations = new ArrayList();

            // Now add the other empty location sections we recorded
            if (_locationTags != null)
            {
                foreach (string subPath in _locationTags.Values)
                    locations.Add(new ConfigurationLocation(config, subPath));
            }

            return new ConfigurationLocationCollection(locations);
        }

        // Record all location tags in the config file, even if they are empty.
        protected override void AddLocation(string locationSubPath)
        {
            if (_locationTags == null) _locationTags = new Hashtable(StringComparer.OrdinalIgnoreCase);

            _locationTags[locationSubPath] = locationSubPath;
        }

        // Get all the factories available, both in this file and inherited.
        private Hashtable GetAllFactories(bool isGroup)
        {
            Hashtable factories = new Hashtable();

            MgmtConfigurationRecord configRecord = this;
            do
            {
                if (configRecord._factoryRecords != null)
                {
                    foreach (FactoryRecord factoryRecord in configRecord._factoryRecords.Values)
                        if (factoryRecord.IsGroup == isGroup)
                        {
                            string configKey = factoryRecord.ConfigKey;
                            factories[configKey] = new FactoryId(factoryRecord.ConfigKey, factoryRecord.Group,
                                factoryRecord.Name);
                        }
                }

                configRecord = configRecord.MgmtParent;
            } while (!configRecord.IsRootConfig);

            return factories;
        }

        internal ConfigurationSection FindImmediateParentSection(ConfigurationSection section)
        {
            ConfigurationSection result = null;

            string configKey = section.SectionInformation.SectionName;
            SectionRecord sectionRecord = GetSectionRecord(configKey, false);
            if (sectionRecord.HasLocationInputs)
            {
                SectionInput input = sectionRecord.LastLocationInput;
                Debug.Assert(input.HasResult, "input.HasResult");
                result = (ConfigurationSection)input.Result;
            }
            else
            {
                if (sectionRecord.HasIndirectLocationInputs)
                {
                    Debug.Assert(IsLocationConfig, "Indirect location inputs exist only in location config record");
                    SectionInput input = sectionRecord.LastIndirectLocationInput;
                    Debug.Assert(input != null);
                    Debug.Assert(input.HasResult, "input.HasResult");
                    result = (ConfigurationSection)input.Result;
                }
                else
                {
                    if (IsRootDeclaration(configKey, true))
                    {
                        FactoryRecord factoryRecord = GetFactoryRecord(configKey, false);

                        object resultObject;
                        CreateSectionDefault(configKey, false, factoryRecord, null, out resultObject,
                            out _);
                        result = (ConfigurationSection)resultObject;
                    }
                    else
                    {
                        MgmtConfigurationRecord current = MgmtParent;
                        while (!current.IsRootConfig)
                        {
                            sectionRecord = current.GetSectionRecord(configKey, false);
                            if ((sectionRecord != null) && sectionRecord.HasResult)
                            {
                                result = (ConfigurationSection)sectionRecord.Result;
                                break;
                            }

                            current = current.MgmtParent;
                        }

                        Debug.Assert(!current.IsRootConfig, "An immediate parent result should have been found");
                    }
                }
            }

            if (!result.IsReadOnly()) result.SetReadOnly();

            return result;
        }

        internal ConfigurationSection FindAndCloneImmediateParentSection(ConfigurationSection configSection)
        {
            string configKey = configSection.SectionInformation.ConfigKey;
            ConfigurationSection parentSection = FindImmediateParentSection(configSection);
            SectionRecord sectionRecord = GetSectionRecord(configKey, false);
            ConfigurationSection clone = (ConfigurationSection)UseParentResult(configKey, parentSection, sectionRecord);
            return clone;
        }

        internal void RevertToParent(ConfigurationSection configSection)
        {
            // Remove any RawXml set by ConfigurationSection.SetRawXml
            configSection.SectionInformation.RawXml = null;

            try
            {
                // Reset to parent value
                ConfigurationSection parentConfigSection = FindImmediateParentSection(configSection);
                configSection.Reset(parentConfigSection);

                // Consider it to be unmodified
                configSection.ResetModified();
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Config_exception_in_config_section_handler,
                        configSection.SectionInformation.SectionName),
                    e, ConfigStreamInfo.StreamName, 0);
            }

            // Record that the section is to be removed.
            configSection.SectionInformation.Removed = true;
        }

        // Return the outer XML of a section as a string.
        // Return null if the section does not exist in the file.
        internal string GetRawXml(string configKey)
        {
            // Get the section record created during Init
            SectionRecord sectionRecord = GetSectionRecord(configKey, false);
            if ((sectionRecord == null) || !sectionRecord.HasFileInput) return null;

            // The section exists, so find and return its RawXml.
            string[] keys = configKey.Split(s_configPathSeparatorParams);
            ConfigXmlReader reader = GetSectionXmlReader(keys, sectionRecord.FileInput);

            return reader.RawXml;
        }

        // Update the section with the XML provided.
        //
        // This method will throw out any changes made to the section up to this point.
        //
        // If xmlElement is null or empty, it is equivalent to calling RevertToParent
        internal void SetRawXml(ConfigurationSection configSection, string xmlElement)
        {
            // Null or empty is equivalent to RevertToParent().
            if (string.IsNullOrEmpty(xmlElement))
            {
                RevertToParent(configSection);
                return;
            }

            ValidateSectionXml(xmlElement, configSection.SectionInformation.Name);

            // Reset the ConfigurationSection with the XML.
            ConfigurationSection parentConfigSection = FindImmediateParentSection(configSection);
            ConfigXmlReader reader = new ConfigXmlReader(xmlElement, null, 0);

            // Store the raw XML.
            configSection.SectionInformation.RawXml = xmlElement;

            // Update the section with the xml
            try
            {
                try
                {
                    bool wasPresent = configSection.ElementPresent;
                    PropertySourceInfo saveInfo = configSection.ElementInformation.PropertyInfoInternal();

                    configSection.Reset(parentConfigSection);
                    configSection.DeserializeSection(reader);
                    configSection.ResetModified();

                    configSection.ElementPresent = wasPresent;
                    configSection.ElementInformation.ChangeSourceAndLineNumber(saveInfo);
                }
                catch
                {
                    configSection.SectionInformation.RawXml = null;
                    throw;
                }
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Config_exception_in_config_section_handler,
                        configSection.SectionInformation.SectionName),
                    e, null, 0);
            }

            // Ignore previous attempts to remove the section.
            configSection.SectionInformation.Removed = false;
        }

        // Return true if a stream is being used by a configSource directive in other input.
        private bool IsStreamUsed(string oldStreamName)
        {
            MgmtConfigurationRecord current = this;
            if (IsLocationConfig)
            {
                // For a location configuration, the input we need to check
                // are the section records and location sections in the file,
                // which are available in the parent record.
                current = MgmtParent;

                // Check whether a file section is using the configsource directive.
                if (current._sectionRecords != null)
                {
                    foreach (SectionRecord sectionRecord in current._sectionRecords.Values)
                        if (sectionRecord.HasFileInput &&
                            StringUtil.EqualsIgnoreCase(sectionRecord.FileInput.SectionXmlInfo.ConfigSourceStreamName,
                                oldStreamName))
                            return true;
                }
            }

            // Check whether a location is using the configsource directive.
            if (current._locationSections == null) return false;

            foreach (LocationSectionRecord locationSectionRecord in current._locationSections)
                if (StringUtil.EqualsIgnoreCase(locationSectionRecord.SectionXmlInfo.ConfigSourceStreamName,
                    oldStreamName))
                    return true;

            return false;
        }

        // Set the configSource attribute on a ConfigurationSection
        internal void ChangeConfigSource(
            SectionInformation sectionInformation,
            string oldConfigSource,
            string oldConfigSourceStreamName,
            string newConfigSource)
        {
            if (string.IsNullOrEmpty(oldConfigSource)) oldConfigSource = null;

            if (string.IsNullOrEmpty(newConfigSource)) newConfigSource = null;

            // Check if there is a change to config source
            if (StringUtil.EqualsIgnoreCase(oldConfigSource, newConfigSource))
                return;

            if (string.IsNullOrEmpty(ConfigStreamInfo.StreamName))
                throw new ConfigurationErrorsException(SR.Config_source_requires_file);

            string newConfigSourceStreamName = null;
            if (newConfigSource != null)
            {
                newConfigSourceStreamName = Host.GetStreamNameForConfigSource(ConfigStreamInfo.StreamName,
                    newConfigSource);
            }

            // Add the stream to the updates
            if (newConfigSourceStreamName != null)
            {
                // Ensure that no parent is using the same config source stream
                ValidateUniqueChildConfigSource(sectionInformation.ConfigKey, newConfigSourceStreamName, newConfigSource,
                    null);

                StreamInfo streamInfo = (StreamInfo)_streamInfoUpdates[newConfigSourceStreamName];
                if (streamInfo != null)
                {
                    // Detect if another section in this file is using the same configSource
                    // with has a different section name.
                    if (streamInfo.SectionName != sectionInformation.ConfigKey)
                    {
                        throw new ConfigurationErrorsException(
                            SR.Format(SR.Config_source_cannot_be_shared, newConfigSource));
                    }
                }
                else
                {
                    // Add stream to updates
                    streamInfo = new StreamInfo(sectionInformation.ConfigKey, newConfigSource, newConfigSourceStreamName);
                    _streamInfoUpdates.Add(newConfigSourceStreamName, streamInfo);
                }
            }

            // remove old streamname if no longer referenced
            if ((oldConfigSourceStreamName != null) && !IsStreamUsed(oldConfigSourceStreamName))
                _streamInfoUpdates.Remove(oldConfigSourceStreamName);

            // update the configSourceStreamName
            sectionInformation.ConfigSourceStreamName = newConfigSourceStreamName;
        }

        // Verify that the string is valid xml, begins with the expected section name,
        // and contains no more or less than a single element.
        //
        // Throws a ConfigurationErrorsException if there is an error.
        private static void ValidateSectionXml(string xmlElement, string configKey)
        {
            if (string.IsNullOrEmpty(xmlElement))
                return;

            XmlTextReader reader = null;
            try
            {
                XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.Default, Encoding.Unicode);
                reader = new XmlTextReader(xmlElement, XmlNodeType.Element, context);

                // Verify that the it is an element
                reader.Read();
                if (reader.NodeType != XmlNodeType.Element)
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_unexpected_node_type, reader.NodeType));

                // Verify the name of the element is a section
                string group, name;
                SplitConfigKey(configKey, out group, out name);
                if (reader.Name != name)
                    throw new ConfigurationErrorsException(SR.Format(SR.Config_unexpected_element_name, reader.Name));

                while (true)
                {
                    if (!reader.Read())
                    {
                        // ensure there is a matching end element
                        if (reader.Depth != 0)
                        {
                            throw new ConfigurationErrorsException(SR.Config_unexpected_element_end,
                                reader);
                        }

                        break;
                    }

                    switch (reader.NodeType)
                    {
                        // disallowed node types within a section
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.DocumentType:
                            throw new ConfigurationErrorsException(SR.Config_invalid_node_type, reader);
                    }


                    // don't allow XML after the end element
                    if ((reader.Depth <= 0) && (reader.NodeType != XmlNodeType.EndElement))
                        throw new ConfigurationErrorsException(SR.Config_more_data_than_expected, reader);
                }
            }
            finally
            {
                reader?.Close();
            }
        }

        // Add a new configuration section to this config file.
        // This adds both the section declaration and definition to the config file.
        //
        // Called from ConfigurationSectionCollection.Add().
        // Note this method DOES NOT update the associated ConfigurationSectionCollection.
        internal void AddConfigurationSection(string group, string name, ConfigurationSection configSection)
        {
            // <configSections> is not permitted within a <location> tag.
            if (IsLocationConfig)
                throw new InvalidOperationException(SR.Config_add_configurationsection_in_location_config);

            VerifySectionName(name, null, false);

            if (configSection == null) throw new ArgumentNullException(nameof(configSection));

            // Ensure the section is not already part of the configuration hierarchy.
            if (configSection.SectionInformation.Attached)
                throw new InvalidOperationException(SR.Config_add_configurationsection_already_added);

            string configKey = CombineConfigKey(group, name);

            // Ensure the section is not already declared.
            FactoryRecord factoryRecord = FindFactoryRecord(configKey, true);
            if (factoryRecord != null)
                throw new ArgumentException(SR.Config_add_configurationsection_already_exists);

            // Add the configSource if needed.
            if (!string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource))
            {
                ChangeConfigSource(configSection.SectionInformation, null, null,
                    configSection.SectionInformation.ConfigSource);
            }

            // Add to list of all sections.
            _sectionFactories?.Add(configKey, new FactoryId(configKey, group, name));

            // Get the type name.
            string typeName = configSection.SectionInformation.Type ?? Host.GetConfigTypeName(configSection.GetType());

            // Add a factory record for the section.
            factoryRecord = new FactoryRecord(configKey,
                group,
                name,
                typeName,
                configSection.SectionInformation.AllowLocation,
                configSection.SectionInformation.AllowDefinition,
                configSection.SectionInformation.AllowExeDefinition,
                configSection.SectionInformation.OverrideModeDefaultSetting,
                configSection.SectionInformation.RestartOnExternalChanges,
                configSection.SectionInformation.RequirePermission,
                _flags[IsTrusted],
                false,
                ConfigStreamInfo.StreamName,
                -1)
            {
                Factory = TypeUtil.GetConstructor(
                        configSection.GetType(), typeof(ConfigurationSection), true),
            };

            // Construct a factory for the section
            EnsureFactories()[configKey] = factoryRecord;

            // Add a section record for the section.
            // Since we are adding a new definition, it cannot be locked.
            SectionRecord sectionRecord = EnsureSectionRecordUnsafe(configKey, false);
            sectionRecord.Result = configSection;
            sectionRecord.ResultRuntimeObject = configSection;

            // Undo any previous removals of the section.
            _removedSections?.Remove(configKey);

            // Attach the section to the configuration record.
            configSection.SectionInformation.AttachToConfigurationRecord(this, factoryRecord, sectionRecord);

            // If there is rawXml, set it now. Note this will override any other changes to the section
            // definition made after the call to SetXml.
            string rawXml = configSection.SectionInformation.RawXml;
            if (!string.IsNullOrEmpty(rawXml))
            {
                configSection.SectionInformation.RawXml = null;
                configSection.SectionInformation.SetRawXml(rawXml);
            }
        }

        // Remove a configuration section from this config file.
        // This removes both the section declaration and definition from the config file.
        // Note, however, that if a parent config file declares the section,
        // a new instance of the section can be retrieved having the value of the
        // immediate parent.
        //
        // Called from ConfigurationSectionCollection.Remove().
        // Note this method DOES NOT update the associated ConfigurationSectionCollection.
        internal void RemoveConfigurationSection(string group, string name)
        {
            bool sectionIsUsed = false; // Is section used in our record

            VerifySectionName(name, null, true);

            string configKey = CombineConfigKey(group, name);

            // If it's already removed, don't try to remove it again.
            if (RemovedSections.Contains(configKey)) return;

            // If it's not a registered section, there's nothing to do.
            if (FindFactoryRecord(configKey, true) == null) return;

            // Detach from this record
            ConfigurationSection configSection = GetConfigSection(configKey);
            configSection?.SectionInformation.DetachFromConfigurationRecord();

            // Remove from list of all sections if this is the root declaration.
            bool isRootDeclaration = IsRootDeclaration(configKey, false);
            if ((_sectionFactories != null) && isRootDeclaration) _sectionFactories.Remove(configKey);

            // Remove from collection of factory records.
            if (!IsLocationConfig && (_factoryRecords != null) && _factoryRecords.Contains(configKey))
            {
                sectionIsUsed = true;
                _factoryRecords.Remove(configKey);
            }

            // Remove from collection of section records.
            if ((_sectionRecords != null) && _sectionRecords.Contains(configKey))
            {
                sectionIsUsed = true;
                _sectionRecords.Remove(configKey);
            }

            // Remove all location section records for this section in this file.
            if (_locationSections != null)
            {
                int i = 0;
                while (i < _locationSections.Count)
                {
                    LocationSectionRecord locationSectionRecord = (LocationSectionRecord)_locationSections[i];
                    if (locationSectionRecord.ConfigKey != configKey) i++;
                    else
                    {
                        sectionIsUsed = true;
                        _locationSections.RemoveAt(i);
                    }
                }
            }

            if (sectionIsUsed)
            {
                // Add to RemovedSections since we need to remove
                // it from the file later.
                RemovedSections.Add(configKey, configKey);
            }

            // Remove all references from configSource
            // Note that we can't remove an item while enumerating it.
            List<string> streamsToRemove = new List<string>();
            foreach (StreamInfo streamInfo in _streamInfoUpdates.Values)
                if (streamInfo.SectionName == configKey) streamsToRemove.Add(streamInfo.StreamName);

            foreach (string stream in streamsToRemove) _streamInfoUpdates.Remove(stream);
        }

        // Add a new configuration section group to this config file.
        //
        // Called from ConfigurationSectionGroupCollection.Add().
        // Note this method DOES NOT update the associated ConfigurationSectionGroupCollection.
        internal void AddConfigurationSectionGroup(string group, string name,
            ConfigurationSectionGroup configSectionGroup)
        {
            // <location> tags can't have a <configSections> declaration.
            if (IsLocationConfig)
                throw new InvalidOperationException(SR.Config_add_configurationsectiongroup_in_location_config);

            // Validate name argument.
            VerifySectionName(name, null, false);

            // Validate configSectionGroup argument.
            if (configSectionGroup == null) throw ExceptionUtil.ParameterInvalid(nameof(name));

            // A section group can only belong to one section group collection.
            if (configSectionGroup.Attached)
                throw new InvalidOperationException(SR.Config_add_configurationsectiongroup_already_added);
            string configKey = CombineConfigKey(group, name);

            // Do not add if the section group already exists, even if it is of a different type.
            FactoryRecord factoryRecord = FindFactoryRecord(configKey, true);
            if (factoryRecord != null)
                throw new ArgumentException(SR.Config_add_configurationsectiongroup_already_exists);

            // Add to list of all section groups.
            _sectionGroupFactories?.Add(configKey, new FactoryId(configKey, group, name));

            // Get the type name - if it is not specified explicitly, get it from the type of the object.
            string typeName = configSectionGroup.Type ?? Host.GetConfigTypeName(configSectionGroup.GetType());

            // Create a factory record and add it to the collection of factory records.
            factoryRecord = new FactoryRecord(configKey, group, name, typeName, ConfigStreamInfo.StreamName, -1);
            EnsureFactories()[configKey] = factoryRecord;

            // Add it to list of evaluated configuration section groups.
            SectionGroups[configKey] = configSectionGroup;

            // Remove it from RemovedSectionGroups if it was previously removed.
            _removedSectionGroups?.Remove(configKey);

            // Attach to the configuration record.
            configSectionGroup.AttachToConfigurationRecord(this, factoryRecord);
        }

        // Return a list of all FactoryRecords of sections that are descendents of
        // a section group.
        private ArrayList GetDescendentSectionFactories(string configKey)
        {
            ArrayList sectionGroups = new ArrayList();

            string configKeyAncestor;
            if (configKey.Length == 0) configKeyAncestor = string.Empty;
            else configKeyAncestor = configKey + "/";

            foreach (FactoryId factoryId in SectionFactories.Values)
                if ((factoryId.Group == configKey) || StringUtil.StartsWithOrdinal(factoryId.Group, configKeyAncestor))
                    sectionGroups.Add(factoryId);

            return sectionGroups;
        }

        // Return a list of all FactoryRecords of section groups that are descendents of
        // a section group, including the section group itself.
        private ArrayList GetDescendentSectionGroupFactories(string configKey)
        {
            ArrayList sectionGroups = new ArrayList();

            string configKeyAncestor;
            if (configKey.Length == 0) configKeyAncestor = string.Empty;
            else configKeyAncestor = configKey + "/";

            foreach (FactoryId factoryId in SectionGroupFactories.Values)
                if ((factoryId.ConfigKey == configKey) ||
                    StringUtil.StartsWithOrdinal(factoryId.ConfigKey, configKeyAncestor))
                    sectionGroups.Add(factoryId);

            return sectionGroups;
        }

        // Remove a configuration section group from this config file.
        // This removes both the section group declaration and definition from the config file,
        // along with all descendent groups and sections.
        //
        // Note, however, that if a parent config file declares the section group,
        // a new instance of the section can be retrieved having the value of the
        // immediate parent.
        //
        // Called from ConfigurationSectionGroupCollection.Remove().
        // Note this method DOES NOT update the associated ConfigurationSectionCollection.
        internal void RemoveConfigurationSectionGroup(string group, string name)
        {
            // Validate arguments
            VerifySectionName(name, null, false);

            string configKey = CombineConfigKey(group, name);

            // If it's not a registered section, there's nothing to do.
            if (FindFactoryRecord(configKey, true) == null) return;

            // Remove all descendent sections.
            ArrayList sections = GetDescendentSectionFactories(configKey);
            foreach (FactoryId descendent in sections) RemoveConfigurationSection(descendent.Group, descendent.Name);

            // Remove all descendent sections groups, including the configKey group.
            ArrayList sectionGroups = GetDescendentSectionGroupFactories(configKey);
            foreach (FactoryId descendent in sectionGroups)
            {
                //
                // If it's already removed, don't try to remove it again.
                // We don't do this test above the loop for configKey, because
                // the section groups contained within the section group may
                // be changed by the user once added.
                //
                if (RemovedSectionGroups.Contains(descendent.ConfigKey)) continue;

                // If the section group has been evaluated, detatch it.
                ConfigurationSectionGroup sectionGroup = LookupSectionGroup(descendent.ConfigKey);
                sectionGroup?.DetachFromConfigurationRecord();

                // Remove from list of all section group factories if this is the root declaration.
                bool isRootDeclaration = IsRootDeclaration(descendent.ConfigKey, false);
                if ((_sectionGroupFactories != null) && isRootDeclaration)
                    _sectionGroupFactories.Remove(descendent.ConfigKey);

                // Remove from list of factory records.
                if (!IsLocationConfig) _factoryRecords?.Remove(descendent.ConfigKey);

                // Remove from evaluated section groups.
                _sectionGroups?.Remove(descendent.ConfigKey);

                // Add to list of section groups that are removed
                // Note that this will add section groups that might not be used
                // in this config file. That just results in some extra work during
                // save, it is not harmful.
                RemovedSectionGroups.Add(descendent.ConfigKey, descendent.ConfigKey);
            }
        }

        // Update the config file with the changes in each ConfigurationSection
        internal void SaveAs(string filename, ConfigurationSaveMode saveMode, bool forceUpdateAll)
        {
            // Get the updates.
            SectionUpdates declarationUpdates = GetConfigDeclarationUpdates(saveMode);

            ConfigDefinitionUpdates definitionUpdates;
            ArrayList configSourceUpdates;
            bool checkedConfigForUpdates = false;
            bool requireUpdates = filename != null;
            GetConfigDefinitionUpdates(requireUpdates, saveMode, forceUpdateAll, out definitionUpdates,
                out configSourceUpdates);

            if (filename != null)
            {
                Debug.Assert(filename.Length > 0, "The caller should make sure that filename is not empty");

                // Verify that the filename is not being used.
                //
                // Note that if we are using a remote host, all the streamName's in _streamInfoUpdates
                // are actually fullpaths on the remote machine.  In this case there is no way to
                // detect if we have a conflict or not.
                if (!Host.IsRemote && _streamInfoUpdates.Contains(filename))
                    throw new ArgumentException(SR.Format(SR.Filename_in_SaveAs_is_used_already, filename));

                // If there was no config file for this config record,
                // record the new stream name and version.
                if (string.IsNullOrEmpty(ConfigStreamInfo.StreamName))
                {
                    StreamInfo streamInfo = new StreamInfo(null, null, filename);
                    _streamInfoUpdates.Add(filename, streamInfo);

                    ConfigStreamInfo.StreamName = filename;
                    ConfigStreamInfo.StreamVersion = MonitorStream(null, null, ConfigStreamInfo.StreamName);
                }

                // Update the host to redirect filenames
                UpdateConfigHost.AddStreamname(ConfigStreamInfo.StreamName, filename, Host.IsRemote);

                // Redirect also all configSource filenames
                foreach (StreamInfo streamInfo in _streamInfoUpdates.Values)
                    if (!string.IsNullOrEmpty(streamInfo.SectionName))
                    {
                        // Get the new configSource streamName based on the new filename path
                        string newStreamName = InternalConfigHost.StaticGetStreamNameForConfigSource(
                            filename, streamInfo.ConfigSource);

                        // Ask UpdateConfigHost to intercept them.
                        UpdateConfigHost.AddStreamname(streamInfo.StreamName, newStreamName, Host.IsRemote);
                    }
            }

            if (!requireUpdates)
            {
                // Check if there are any updates needed for the
                // configuration record itself.
                requireUpdates = RecordItselfRequiresUpdates;
            }

            if ((declarationUpdates != null) || (definitionUpdates != null) || requireUpdates)
            {
                // Copy the input stream before opening the output stream.
                byte[] readBuffer = null;
                Encoding encoding = null;
                if (ConfigStreamInfo.HasStream)
                {
                    using (Stream streamRead = Host.OpenStreamForRead(ConfigStreamInfo.StreamName))
                    {
                        if (streamRead == null)
                        {
                            throw new ConfigurationErrorsException(SR.Config_file_has_changed,
                                ConfigStreamInfo.StreamName, 0);
                        }

                        readBuffer = new byte[streamRead.Length];
                        int count = streamRead.Read(readBuffer, 0, (int)streamRead.Length);
                        if (count != streamRead.Length)
                            throw new ConfigurationErrorsException(SR.Config_data_read_count_mismatch);
                    }

                    // Read the first byte so that we can determine the encoding.
                    try
                    {
                        using (StreamReader reader = new StreamReader(ConfigStreamInfo.StreamName))
                        {
                            if (reader.Peek() >= 0) reader.Read();

                            // Handle only UTF-16 explicitly, so that handling of other encodings are not affected.
                            if (reader.CurrentEncoding is UnicodeEncoding) encoding = reader.CurrentEncoding;
                        }
                    }
                    catch
                    {
                        // Ignore any errors, encoding will remain null.
                    }
                }

                string changedStreamName = FindChangedConfigurationStream();
                if (changedStreamName != null)
                    throw new ConfigurationErrorsException(SR.Config_file_has_changed, changedStreamName, 0);

                checkedConfigForUpdates = true;

                // Write the changes to the output stream.
                object writeContext = null;
                bool streamOpened = false;
                try
                {
                    try
                    {
                        using (Stream streamWrite = Host.OpenStreamForWrite(ConfigStreamInfo.StreamName, null,
                            ref writeContext))
                        {
                            streamOpened = true;
                            // Use the default StreamWriter constructor if encoding is null,
                            // otherwise specify the encoding.
                            using (
                                StreamWriter streamWriter = encoding == null
                                    ? new StreamWriter(streamWrite)
                                    : new StreamWriter(streamWrite, encoding))
                            {
                                XmlUtilWriter utilWriter = new XmlUtilWriter(streamWriter, true);
                                if (ConfigStreamInfo.HasStream)
                                {
                                    CopyConfig(declarationUpdates, definitionUpdates, readBuffer,
                                        ConfigStreamInfo.StreamName, NamespaceChangeNeeded, utilWriter);
                                }
                                else
                                {
                                    CreateNewConfig(declarationUpdates, definitionUpdates, NamespaceChangeNeeded,
                                        utilWriter);
                                }
                            }
                        }
                    }
                    catch
                    {
                        if (streamOpened) Host.WriteCompleted(ConfigStreamInfo.StreamName, false, writeContext);

                        throw;
                    }
                }
                catch (Exception e)
                {
                    // Guarantee that exceptions contain at least the name of the stream by wrapping them
                    // in a ConfigurationException.
                    throw ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e,
                        ConfigStreamInfo.StreamName, 0);
                }

                Host.WriteCompleted(ConfigStreamInfo.StreamName, true, writeContext);

                // Update stream information for the config file
                ConfigStreamInfo.HasStream = true;
                ConfigStreamInfo.ClearStreamInfos();
                ConfigStreamInfo.StreamVersion = MonitorStream(null, null, ConfigStreamInfo.StreamName);
            }

            if (configSourceUpdates != null)
            {
                // If we haven't checked before, check now
                if (!checkedConfigForUpdates)
                {
                    string changedStreamName = FindChangedConfigurationStream();
                    if (changedStreamName != null)
                    {
                        throw new ConfigurationErrorsException(SR.Config_file_has_changed,
                            changedStreamName, 0);
                    }
                }

                // write updates
                foreach (DefinitionUpdate update in configSourceUpdates) SaveConfigSource(update);
            }

            // Update state to reflect the changes to the config file
            UpdateRecords();
        }

        private static bool AreDeclarationAttributesModified(FactoryRecord factoryRecord,
            ConfigurationSection configSection)
        {
            return (factoryRecord.FactoryTypeName != configSection.SectionInformation.Type)
                || (factoryRecord.AllowLocation != configSection.SectionInformation.AllowLocation)
                || (factoryRecord.RestartOnExternalChanges != configSection.SectionInformation.RestartOnExternalChanges)
                || (factoryRecord.RequirePermission != configSection.SectionInformation.RequirePermission)
                || (factoryRecord.AllowDefinition != configSection.SectionInformation.AllowDefinition)
                || (factoryRecord.AllowExeDefinition != configSection.SectionInformation.AllowExeDefinition)
                ||
                (factoryRecord.OverrideModeDefault.OverrideMode !=
                configSection.SectionInformation.OverrideModeDefaultSetting.OverrideMode) // Compare the value only
                || configSection.SectionInformation.IsModifiedFlags();
        }

        private static void AppendAttribute(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            sb.Append("=\"");
            sb.Append(value);
            sb.Append("\" ");
        }

        private string GetUpdatedSectionDeclarationXml(FactoryRecord factoryRecord, ConfigurationSection configSection,
            ConfigurationSaveMode saveMode)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('<');
            sb.Append(SectionTag);
            sb.Append(' ');
            string type = configSection.SectionInformation.Type ?? factoryRecord.FactoryTypeName;
            if (TypeStringTransformerIsSet)
                type = TypeStringTransformer(type);

            AppendAttribute(sb, SectionNameAttribute, configSection.SectionInformation.Name);
            AppendAttribute(sb, SectionTypeAttribute, type);

            if (!configSection.SectionInformation.AllowLocation ||
                (saveMode == ConfigurationSaveMode.Full) ||
                ((saveMode == ConfigurationSaveMode.Modified) &&
                configSection.SectionInformation.AllowLocationModified))
            {
                AppendAttribute(sb,
                    SectionAllowLocationAttribute,
                    configSection.SectionInformation.AllowLocation
                        ? KeywordTrue
                        : KeywordFalse);
            }

            if ((configSection.SectionInformation.AllowDefinition != ConfigurationAllowDefinition.Everywhere) ||
                (saveMode == ConfigurationSaveMode.Full) ||
                ((saveMode == ConfigurationSaveMode.Modified) &&
                configSection.SectionInformation.AllowDefinitionModified))
            {
                string v = null;
                switch (configSection.SectionInformation.AllowDefinition)
                {
                    case ConfigurationAllowDefinition.Everywhere:
                        v = AllowDefinitionEverywhere;
                        break;
                    case ConfigurationAllowDefinition.MachineOnly:
                        v = AllowDefinitionMachineOnly;
                        break;
                    case ConfigurationAllowDefinition.MachineToWebRoot:
                        v = AllowDefinitionMachineToWebRoot;
                        break;
                    case ConfigurationAllowDefinition.MachineToApplication:
                        v = AllowDefinitionMachineToApplication;
                        break;
                }

                AppendAttribute(sb, SectionAllowDefinitionAttribute, v);
            }

            if ((configSection.SectionInformation.AllowExeDefinition !=
                ConfigurationAllowExeDefinition.MachineToApplication) ||
                (saveMode == ConfigurationSaveMode.Full) ||
                ((saveMode == ConfigurationSaveMode.Modified) &&
                configSection.SectionInformation.AllowExeDefinitionModified))
            {
                AppendAttribute(sb,
                    SectionAllowExeDefinitionAttribute,
                    ExeDefinitionToString(
                        configSection.SectionInformation.AllowExeDefinition)
                    );
            }

            if ((configSection.SectionInformation.OverrideModeDefaultSetting.IsDefaultForSection == false) ||
                (saveMode == ConfigurationSaveMode.Full) ||
                ((saveMode == ConfigurationSaveMode.Modified) &&
                configSection.SectionInformation.OverrideModeDefaultModified))
            {
                AppendAttribute(sb,
                    SectionOverrideModeDefaultAttribute,
                    configSection.SectionInformation.OverrideModeDefaultSetting.OverrideModeXmlValue);
            }

            if (!configSection.SectionInformation.RestartOnExternalChanges)
                AppendAttribute(sb, SectionRestartonExternalChangesAttribute, KeywordFalse);
            else
            {
                if ((saveMode == ConfigurationSaveMode.Full) ||
                    ((saveMode == ConfigurationSaveMode.Modified) &&
                    configSection.SectionInformation.RestartOnExternalChangesModified))
                    AppendAttribute(sb, SectionRestartonExternalChangesAttribute, KeywordTrue);
            }

            if (!configSection.SectionInformation.RequirePermission)
            {
                AppendAttribute(sb, SectionRequirePermissionAttribute, KeywordFalse);
            }
            else
            {
                if ((saveMode == ConfigurationSaveMode.Full) ||
                    ((saveMode == ConfigurationSaveMode.Modified) &&
                    configSection.SectionInformation.RequirePermissionModified))
                    AppendAttribute(sb, SectionRequirePermissionAttribute, KeywordTrue);
            }

            sb.Append("/>");

            return sb.ToString();
        }

        // ExeDefinitionToString
        //
        // Take an ExeDefinition and translate it to a string
        //
        private static string ExeDefinitionToString(ConfigurationAllowExeDefinition allowDefinition) =>
            allowDefinition switch
            {
                ConfigurationAllowExeDefinition.MachineOnly => AllowDefinitionMachineOnly,
                ConfigurationAllowExeDefinition.MachineToApplication => AllowDefinitionMachineToApplication,
                ConfigurationAllowExeDefinition.MachineToRoamingUser => AllowExeDefinitionMachineToRoaming,
                ConfigurationAllowExeDefinition.MachineToLocalUser => AllowExeDefinitionMachineToLocal,
                _ => throw ExceptionUtil.PropertyInvalid("AllowExeDefinition"),
            };

        private string GetUpdatedSectionGroupDeclarationXml(FactoryRecord factoryRecord,
            ConfigurationSectionGroup configSectionGroup)
        {
            if ((TargetFramework != null) &&
                !configSectionGroup.ShouldSerializeSectionGroupInTargetVersion(TargetFramework))
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append('<');
            sb.Append(SectionGroupTag);
            sb.Append(' ');
            AppendAttribute(sb, SectionGroupNameAttribute, configSectionGroup.Name);
            string type = configSectionGroup.Type ?? factoryRecord.FactoryTypeName;
            if (TypeStringTransformerIsSet)
                type = TypeStringTransformer(type);

            AppendAttribute(sb, SectionGroupTypeAttribute, type);

            sb.Append('>');

            return sb.ToString();
        }

        // Gather all the updates to the configuration section declarations.
        private SectionUpdates GetConfigDeclarationUpdates(ConfigurationSaveMode saveMode)
        {
            if (IsLocationConfig)
                return null;

            // hasChanged will be set to true if there is any change that will impact the current config file.
            bool hasChanged = HasRemovedSectionsOrGroups;
            SectionUpdates sectionUpdates = new SectionUpdates(string.Empty);

            if (_factoryRecords != null)
            {
                foreach (FactoryRecord factoryRecord in _factoryRecords.Values)
                    if (!factoryRecord.IsGroup)
                    {
                        string updatedXml = null;

                        // Never write out an undeclared section.
                        if (factoryRecord.IsUndeclared) continue;

                        // Note that GetConfigSection will return only those sections that have a sectionRecord
                        // and has a result.  In another word, only sections that have been accessed.
                        ConfigurationSection configSection = GetConfigSection(factoryRecord.ConfigKey);

                        if (configSection != null)
                        {
                            // We should skip this section declaration only if all below hold true:
                            // 1. The section should not be declared at this level.  Reasons:
                            //      i. The section is originally not declared at this level, or
                            //      ii. The user calls SectionInformation.ForceDeclaration(false)
                            // 2. It's not machine.config.  Otherwise we must declare it even if the user called ForceDeclaration(false)
                            // 3. It's already declared higher up.
                            // 4. It's not valid in the current Target Framework version
                            if (!configSection.SectionInformation.IsDeclared
                                && !MgmtParent.IsRootConfig
                                && (MgmtParent.FindFactoryRecord(factoryRecord.ConfigKey, false) != null))
                            {
                                if (factoryRecord.HasFile) hasChanged = true;

                                continue;
                            }
                            if ((TargetFramework != null) &&
                                !configSection.ShouldSerializeSectionInTargetVersion(TargetFramework))
                                continue;

                            if (AreDeclarationAttributesModified(factoryRecord, configSection) || !factoryRecord.HasFile)
                            {
                                updatedXml = GetUpdatedSectionDeclarationXml(factoryRecord, configSection, saveMode);
                                if (!string.IsNullOrEmpty(updatedXml))
                                    hasChanged = true;
                            }
                        }

                        DeclarationUpdate update = new DeclarationUpdate(factoryRecord.ConfigKey, !factoryRecord.HasFile,
                            updatedXml);
                        sectionUpdates.AddSection(update);
                    }
                    else
                    {
                        bool addGroupUpdate = false;

                        // LookupSectionGroup will return an object only if the group has been accessed
                        ConfigurationSectionGroup configSectionGroup = LookupSectionGroup(factoryRecord.ConfigKey);

                        if (!factoryRecord.HasFile)
                        {
                            // Not in the file, so it means the group is added programmatically.
                            addGroupUpdate = true;
                        }
                        else
                        {
                            if ((configSectionGroup != null) && configSectionGroup.IsDeclarationRequired)
                            {
                                // The section group is declared in this config file
                                addGroupUpdate = true;
                            }
                            else
                            {
                                if ((factoryRecord.FactoryTypeName != null) || (configSectionGroup != null))
                                {
                                    FactoryRecord parentFactoryRecord = null;
                                    if (!MgmtParent.IsRootConfig)
                                    {
                                        parentFactoryRecord = MgmtParent.FindFactoryRecord(factoryRecord.ConfigKey,
                                            false);
                                    }

                                    // Add it if declaration is required.  Please note this check is identical to the check
                                    // for _declarationRequired in ConfigurationSectionGroup.AttachToConfigurationRecord.
                                    addGroupUpdate = parentFactoryRecord?.FactoryTypeName == null;
                                }
                            }
                        }

                        if (addGroupUpdate)
                        {
                            string updatedXml = null;

                            if (!factoryRecord.HasFile
                                ||
                                ((configSectionGroup != null) &&
                                (configSectionGroup.Type != factoryRecord.FactoryTypeName)))
                            {
                                updatedXml = GetUpdatedSectionGroupDeclarationXml(factoryRecord, configSectionGroup);
                                if (!string.IsNullOrEmpty(updatedXml)) hasChanged = true;
                            }

                            Debug.Assert(!factoryRecord.IsUndeclared, "!factoryRecord.IsUndeclared");
                            Debug.Assert(!IsImplicitSection(factoryRecord.ConfigKey),
                                "We should never write out an implicit section");

                            DeclarationUpdate update = new DeclarationUpdate(factoryRecord.ConfigKey,
                                !factoryRecord.HasFile, updatedXml);
                            sectionUpdates.AddSectionGroup(update);
                        }
                    }
            }

            if (_sectionRecords != null)
            {
                foreach (SectionRecord sectionRecord in _sectionRecords.Values)
                {
                    if ((GetFactoryRecord(sectionRecord.ConfigKey, false) != null) || !sectionRecord.HasResult)
                    {
                        // Skip because this factory is defined locally ( in
                        // which case we handled above), or it was not used
                        continue;
                    }

                    ConfigurationSection configSection = (ConfigurationSection)sectionRecord.Result;
                    FactoryRecord factoryRecord = MgmtParent.FindFactoryRecord(sectionRecord.ConfigKey, false);

                    // Add this section declaration if:
                    // 1. The section is not declared locally (otherwise it's handled above)
                    // 2. SectionInformation.IsDeclared is true (i.e. user called SectionInformation.ForceDeclaration(true))
                    if (configSection.SectionInformation.IsDeclared)
                    {
                        Debug.Assert(!IsImplicitSection(sectionRecord.ConfigKey),
                            "We should never write out an implicit section");
                        Debug.Assert(!factoryRecord.IsUndeclared, "!factoryRecord.IsUndeclared");
                        string updatedXml = GetUpdatedSectionDeclarationXml(factoryRecord, configSection, saveMode);
                        if (!string.IsNullOrEmpty(updatedXml))
                        {
                            hasChanged = true;
                            DeclarationUpdate update = new DeclarationUpdate(factoryRecord.ConfigKey, true, updatedXml);
                            sectionUpdates.AddSection(update);
                        }
                    }
                }
            }

            if (_sectionGroups != null)
            {
                foreach (ConfigurationSectionGroup configSectionGroup in _sectionGroups.Values)
                {
                    if (GetFactoryRecord(configSectionGroup.SectionGroupName, false) != null) continue;

                    FactoryRecord factoryRecord = MgmtParent.FindFactoryRecord(configSectionGroup.SectionGroupName,
                        false);
                    if (configSectionGroup.IsDeclared ||
                        ((factoryRecord != null) && (configSectionGroup.Type != factoryRecord.FactoryTypeName)))
                    {
                        string updatedXml = GetUpdatedSectionGroupDeclarationXml(factoryRecord, configSectionGroup);
                        if (!string.IsNullOrEmpty(updatedXml))
                        {
                            hasChanged = true;
                            DeclarationUpdate update = new DeclarationUpdate(factoryRecord.ConfigKey, true, updatedXml);
                            sectionUpdates.AddSectionGroup(update);
                        }
                    }
                }
            }

            if (hasChanged) return sectionUpdates;
            else return null;
        }

        private static bool AreLocationAttributesModified(SectionRecord sectionRecord, ConfigurationSection configSection)
        {
            OverrideModeSetting overrideMode = OverrideModeSetting.s_locationDefault;
            bool inheritInChildApplications = true;

            if (sectionRecord.HasFileInput)
            {
                SectionXmlInfo sectionXmlInfo = sectionRecord.FileInput.SectionXmlInfo;
                overrideMode = sectionXmlInfo.OverrideModeSetting;
                inheritInChildApplications = !sectionXmlInfo.SkipInChildApps;
            }

            // We will use IsSameForLocation tag so that we flag modes that cant go into the same location tag
            // as different. If we don't do that it will appear like the mode was not changed which will
            // case conflict later when determining if the section is moved ( when writing the new config updates )

            return
                !OverrideModeSetting.CanUseSameLocationTag(overrideMode,
                    configSection.SectionInformation.OverrideModeSetting)
                || (inheritInChildApplications != configSection.SectionInformation.InheritInChildApplications);
        }

        private static bool AreSectionAttributesModified(SectionRecord sectionRecord, ConfigurationSection configSection)
        {
            string configSource;
            string protectionProviderName;

            if (sectionRecord.HasFileInput)
            {
                SectionXmlInfo sectionXmlInfo = sectionRecord.FileInput.SectionXmlInfo;
                configSource = sectionXmlInfo.ConfigSource;
                protectionProviderName = sectionXmlInfo.ProtectionProviderName;
            }
            else
            {
                configSource = null;
                protectionProviderName = null;
            }

            return
                !StringUtil.EqualsOrBothNullOrEmpty(configSource, configSection.SectionInformation.ConfigSource)
                ||
                !StringUtil.EqualsOrBothNullOrEmpty(protectionProviderName,
                    configSection.SectionInformation.ProtectionProviderName)
                || AreLocationAttributesModified(sectionRecord, configSection);
        }

        private static bool IsConfigSectionMoved(SectionRecord sectionRecord, ConfigurationSection configSection)
        {
            if (!sectionRecord.HasFileInput)
                return true;

            return AreLocationAttributesModified(sectionRecord, configSection);
        }

        // Gather all the updates to the configuration section definitions.
        private void GetConfigDefinitionUpdates(
            bool requireUpdates, ConfigurationSaveMode saveMode, bool forceSaveAll,
            out ConfigDefinitionUpdates definitionUpdates, out ArrayList configSourceUpdates)
        {
            definitionUpdates = new ConfigDefinitionUpdates();
            configSourceUpdates = null;
            bool hasChanged = HasRemovedSections;

            // Loop through all the section records.
            if (_sectionRecords != null)
            {
                InitProtectedConfigurationSection();
                // Make sure we have the initialized the protected config section, otherwise the foreach loop may blow up
                foreach (DictionaryEntry de in _sectionRecords)
                {
                    string configKey = (string)de.Key;
                    SectionRecord sectionRecord = (SectionRecord)de.Value;
                    sectionRecord.AddUpdate = false;
                    bool addUpdate = sectionRecord.HasFileInput;
                    // If true, add this section to definitionUpdates, and optinally to configSourceUpdates
                    OverrideModeSetting overrideMode = OverrideModeSetting.s_locationDefault;
                    bool inheritInChildApplications = true;
                    bool moved = false;
                    string updatedXml = null;
                    bool addToConfigSourceUpdates = false;
                    // If true, we have to update the external config file for this section

                    if (!sectionRecord.HasResult)
                    {
                        if (sectionRecord.HasFileInput)
                        {
                            SectionXmlInfo sectionXmlInfo = sectionRecord.FileInput.SectionXmlInfo;
                            overrideMode = sectionXmlInfo.OverrideModeSetting;
                            inheritInChildApplications = !sectionXmlInfo.SkipInChildApps;
                            addToConfigSourceUpdates = requireUpdates &&
                                !string.IsNullOrEmpty(sectionXmlInfo.ConfigSource);
                        }
                    }
                    else
                    {
                        ConfigurationSection configSection = (ConfigurationSection)sectionRecord.Result;

                        if ((TargetFramework != null) &&
                            !configSection.ShouldSerializeSectionInTargetVersion(TargetFramework))
                            continue;

                        overrideMode = configSection.SectionInformation.OverrideModeSetting;
                        inheritInChildApplications = configSection.SectionInformation.InheritInChildApplications;

                        // it is an error to require a location section when the type doesn't allow locations.
                        if (!configSection.SectionInformation.AllowLocation &&
                            (!overrideMode.IsDefaultForLocationTag || !inheritInChildApplications))
                        {
                            throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_inconsistent_location_attributes, configKey));
                        }

                        addToConfigSourceUpdates = requireUpdates &&
                            !string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource);
                        try
                        {
                            bool isModified = configSection.SectionInformation.ForceSave ||
                                configSection.IsModified() ||
                                (forceSaveAll && !configSection.SectionInformation.IsLocked);

                            bool sectionAttributesModified = AreSectionAttributesModified(sectionRecord, configSection);
                            bool sectionContentModified = isModified ||
                                (configSection.SectionInformation.RawXml != null);

                            // Get the updated XML if the section has been modified.
                            if (sectionContentModified || sectionAttributesModified)
                            {
                                configSection.SectionInformation.VerifyIsEditable();
                                configSection.SectionInformation.Removed = false;
                                addUpdate = true;
                                moved = IsConfigSectionMoved(sectionRecord, configSection);

                                if (!addToConfigSourceUpdates)
                                {
                                    addToConfigSourceUpdates =
                                        !string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource)
                                        &&
                                        (sectionContentModified || configSection.SectionInformation.ConfigSourceModified);
                                }

                                if (isModified ||
                                    (configSection.SectionInformation.RawXml == null) ||
                                    (saveMode == ConfigurationSaveMode.Full))
                                {
                                    // Note: we won't use RawXml if saveMode == Full because Full means we want to
                                    // write all properties, and RawXml may not have all properties.
                                    ConfigurationSection parentConfigSection = FindImmediateParentSection(configSection);
                                    updatedXml = configSection.SerializeSection(parentConfigSection,
                                        configSection.SectionInformation.Name, saveMode);
                                    ValidateSectionXml(updatedXml, configKey);
                                }
                                else updatedXml = configSection.SectionInformation.RawXml;

                                if (string.IsNullOrEmpty(updatedXml))
                                {
                                    // We always need to emit a section, even if empty, when:
                                    // * The section has configSoure
                                    // * The section is in a location section that has non-default attributes
                                    // * The section is encrypted.
                                    if (!string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource) ||
                                        !configSection.SectionInformation.LocationAttributesAreDefault ||
                                        (configSection.SectionInformation.ProtectionProvider != null))
                                        updatedXml = WriteEmptyElement(configSection.SectionInformation.Name);
                                }

                                if (string.IsNullOrEmpty(updatedXml))
                                {
                                    configSection.SectionInformation.Removed = true;
                                    updatedXml = null;
                                    addUpdate = false;
                                    if (sectionRecord.HasFileInput)
                                    {
                                        hasChanged = true;

                                        // When a section is to be removed, its corresponding file
                                        // input should be cleared as well so this section will be indicated as "moved"
                                        // next time something is added back to the section.  Without marking it as "moved",
                                        // adding new content to a removed section fails.
                                        sectionRecord.RemoveFileInput();
                                    }
                                }
                                else
                                {
                                    // configSection.ElementPresent = true;
                                    if (sectionAttributesModified || moved ||
                                        string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource))
                                        hasChanged = true;

                                    // Encrypt if required.
                                    if (configSection.SectionInformation.ProtectionProvider != null)
                                    {
                                        ProtectedConfigurationSection protectedConfig =
                                            GetSection(ReservedSectionProtectedConfiguration) as
                                                ProtectedConfigurationSection;
                                        try
                                        {
                                            string encryptedSection = Host.EncryptSection(updatedXml,
                                                configSection.SectionInformation.ProtectionProvider, protectedConfig);

                                            // VsWhidbey 495120: The config host is responsible for encrypting a section, but it is the job of
                                            // System.Configuration to format an encrypted section during write (and to detect an encrypted section during read.)
                                            updatedXml =
                                                ProtectedConfigurationSection.FormatEncryptedSection(encryptedSection,
                                                    configSection.SectionInformation.Name,
                                                    configSection.SectionInformation.ProtectionProvider.Name);
                                        }
                                        catch (Exception e)
                                        {
                                            throw new ConfigurationErrorsException(
                                                SR.Format(SR.Encryption_failed,
                                                    configSection.SectionInformation.SectionName,
                                                    configSection.SectionInformation.ProtectionProvider.Name, e.Message),
                                                e);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (configSection.SectionInformation.Removed)
                                {
                                    addUpdate = false;
                                    if (sectionRecord.HasFileInput) hasChanged = true;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_exception_in_config_section_handler,
                                    configSection.SectionInformation.SectionName), e);
                        }
                    }

                    if (!addUpdate) continue;

                    // Make sure we are not addingh a definition of a locked section
                    if (GetSectionLockedMode(sectionRecord.ConfigKey) == OverrideMode.Deny)
                        throw new ConfigurationErrorsException(SR.Config_section_locked, (IConfigErrorInfo)null);

                    sectionRecord.AddUpdate = true;
                    DefinitionUpdate definitionUpdate = definitionUpdates.AddUpdate(overrideMode,
                        inheritInChildApplications, moved, updatedXml, sectionRecord);

                    if (addToConfigSourceUpdates)
                    {
                        if (configSourceUpdates == null) configSourceUpdates = new ArrayList();
                        configSourceUpdates.Add(definitionUpdate);
                    }
                }
            }

            if (_flags[ForceLocationWritten])
            {
                // We must write the location tag
                hasChanged = true;
                definitionUpdates.RequireLocation = true;
            }

            if (_flags[SuggestLocationRemoval])
            {
                // We should try to remove location
                hasChanged = true;
            }

            if (hasChanged) definitionUpdates.CompleteUpdates();
            else definitionUpdates = null;
        }

        // Take an element name, and create an xml string that contains
        // that element in an empty state
        private static string WriteEmptyElement(string elementName)
        {
            StringBuilder sb = new StringBuilder();

            // Create element
            sb.Append('<');
            sb.Append(elementName);
            sb.Append(" />");

            return sb.ToString();
        }

        // After the config file has been written out, update the section records
        // to reflect changes that were made in the config file.
        private void UpdateRecords()
        {
            if (_factoryRecords != null)
            {
                foreach (FactoryRecord factoryRecord in _factoryRecords.Values)
                {
                    // Update stream information
                    if (string.IsNullOrEmpty(factoryRecord.Filename))
                        factoryRecord.Filename = ConfigStreamInfo.StreamName;

                    factoryRecord.LineNumber = 0;

                    ConfigurationSection configSection = GetConfigSection(factoryRecord.ConfigKey);
                    if (configSection == null) continue;

                    if (configSection.SectionInformation.Type != null)
                        factoryRecord.FactoryTypeName = configSection.SectionInformation.Type;

                    factoryRecord.AllowLocation = configSection.SectionInformation.AllowLocation;
                    factoryRecord.RestartOnExternalChanges =
                        configSection.SectionInformation.RestartOnExternalChanges;
                    factoryRecord.RequirePermission = configSection.SectionInformation.RequirePermission;
                    factoryRecord.AllowDefinition = configSection.SectionInformation.AllowDefinition;
                    factoryRecord.AllowExeDefinition = configSection.SectionInformation.AllowExeDefinition;
                }
            }

            if (_sectionRecords != null)
            {
                string definitionConfigPath = IsLocationConfig ? _parent.ConfigPath : ConfigPath;
                foreach (SectionRecord sectionRecord in _sectionRecords.Values)
                {
                    string configSource;
                    string configSourceStreamName;
                    object configSourceStreamVersion;
                    ConfigurationSection configSection;

                    if (sectionRecord.HasResult)
                    {
                        configSection = (ConfigurationSection)sectionRecord.Result;
                        configSource = configSection.SectionInformation.ConfigSource;
                        if (string.IsNullOrEmpty(configSource)) configSource = null;

                        configSourceStreamName = configSection.SectionInformation.ConfigSourceStreamName;
                        if (string.IsNullOrEmpty(configSourceStreamName)) configSourceStreamName = null;
                    }
                    else
                    {
                        configSection = null;
                        configSource = null;
                        configSourceStreamName = null;

                        // If there is no result, then the only way there could be a
                        // section record is:
                        // 1. For there to be input in the file.
                        // 2. A location tag applies to this record
                        Debug.Assert(sectionRecord.HasFileInput || sectionRecord.HasLocationInputs,
                            "sectionRecord.HasFileInput || sectionRecord.HasLocationInputs");

                        // Note that if it's a location input, we don't need to monitor the configSource because
                        // that stream is monitored by one of our parent's config record
                        if (sectionRecord.HasFileInput)
                        {
                            SectionXmlInfo sectionXmlInfo = sectionRecord.FileInput.SectionXmlInfo;
                            configSource = sectionXmlInfo.ConfigSource;
                            configSourceStreamName = sectionXmlInfo.ConfigSourceStreamName;
                        }
                    }

                    if (!string.IsNullOrEmpty(configSource))
                    {
                        configSourceStreamVersion = MonitorStream(sectionRecord.ConfigKey, configSource,
                            configSourceStreamName);
                    }
                    else configSourceStreamVersion = null;

                    if (!sectionRecord.HasResult)
                    {
                        Debug.Assert(sectionRecord.HasFileInput || sectionRecord.HasLocationInputs,
                            "sectionRecord.HasFileInput || sectionRecord.HasLocationInputs");

                        // Note that if it's a location input, we don't need to monitor the configSource because
                        // that stream is monitored by one of our parent's config record
                        if (sectionRecord.HasFileInput)
                        {
                            SectionXmlInfo sectionXmlInfo = sectionRecord.FileInput.SectionXmlInfo;
                            sectionXmlInfo.StreamVersion = ConfigStreamInfo.StreamVersion;
                        }
                    }
                    else
                    {
                        configSection.SectionInformation.RawXml = null;
                        bool addUpdate = sectionRecord.AddUpdate;
                        sectionRecord.AddUpdate = false;

                        if (addUpdate)
                        {
                            SectionInput fileInput = sectionRecord.FileInput;
                            if (fileInput == null)
                            {
                                SectionXmlInfo sectionXmlInfo = new SectionXmlInfo(
                                    sectionRecord.ConfigKey, definitionConfigPath, _configPath, _locationSubPath,
                                    ConfigStreamInfo.StreamName, 0, ConfigStreamInfo.StreamVersion, null,
                                    configSource, configSourceStreamName, configSourceStreamVersion,
                                    configSection.SectionInformation.ProtectionProviderName,
                                    configSection.SectionInformation.OverrideModeSetting,
                                    !configSection.SectionInformation.InheritInChildApplications);

                                fileInput = new SectionInput(sectionXmlInfo, null)
                                {
                                    Result = configSection,
                                    ResultRuntimeObject = configSection
                                };
                                sectionRecord.AddFileInput(fileInput);
                            }
                            else
                            {
                                SectionXmlInfo sectionXmlInfo = fileInput.SectionXmlInfo;
                                sectionXmlInfo.LineNumber = 0;
                                sectionXmlInfo.StreamVersion = ConfigStreamInfo.StreamVersion;

                                sectionXmlInfo.RawXml = null;
                                sectionXmlInfo.ConfigSource = configSource;
                                sectionXmlInfo.ConfigSourceStreamName = configSourceStreamName;
                                sectionXmlInfo.ProtectionProviderName =
                                    configSection.SectionInformation.ProtectionProviderName;
                                sectionXmlInfo.OverrideModeSetting =
                                    configSection.SectionInformation.OverrideModeSetting;
                                sectionXmlInfo.SkipInChildApps =
                                    !configSection.SectionInformation.InheritInChildApplications;
                            }

                            fileInput.ProtectionProvider = configSection.SectionInformation.ProtectionProvider;
                        }

                        try
                        {
                            configSection.ResetModified();
                        }
                        catch (Exception e)
                        {
                            throw new ConfigurationErrorsException(
                                SR.Format(SR.Config_exception_in_config_section_handler, sectionRecord.ConfigKey), e,
                                ConfigStreamInfo.StreamName, 0);
                        }
                    }
                }
            }

            // Copy remaining stream updates, which correspond to streams used by location sections
            foreach (StreamInfo streamInfo in _streamInfoUpdates.Values)
                if (!ConfigStreamInfo.StreamInfos.Contains(streamInfo.StreamName))
                    MonitorStream(streamInfo.SectionName, streamInfo.ConfigSource, streamInfo.StreamName);

            // reinitialize _streamInfoUpdates
            InitStreamInfoUpdates();

            // Update namespace value
            _flags[NamespacePresentInFile] = _flags[NamespacePresentCurrent];

            // You only have one chance to force the location config, now you
            // will have to recreate the object
            _flags[ForceLocationWritten] = false;
            _flags[SuggestLocationRemoval] = false;

            // Handle removed location sections
            if (!IsLocationConfig && (_locationSections != null) && (_removedSections != null) &&
                (_removedSections.Count > 0))
            {
                int i = 0;
                while (i < _locationSections.Count)
                {
                    LocationSectionRecord locationSectionRecord = (LocationSectionRecord)_locationSections[i];
                    if (_removedSections.Contains(locationSectionRecord.ConfigKey)) _locationSections.RemoveAt(i);
                    else i++;
                }
            }

            _removedSections = null;
            _removedSectionGroups = null;
        }

        // Create a new config file.
        private void CreateNewConfig(
            SectionUpdates declarationUpdates,
            ConfigDefinitionUpdates definitionUpdates,
            NamespaceChange namespaceChange,
            XmlUtilWriter utilWriter)
        {
            // Write Header
            utilWriter.Write(string.Format(CultureInfo.InvariantCulture,
                FormatNewConfigFile,
                ConfigStreamInfo.StreamEncoding.WebName));

            // Write <configuration> tag
            if (namespaceChange == NamespaceChange.Add)
            {
                utilWriter.Write(string.Format(CultureInfo.InvariantCulture,
                    FormatConfigurationNamespace,
                    ConfigurationNamespace));
            }
            else utilWriter.Write(FormatConfiguration);

            const int LineIndent = DefaultIndent + 1;

            if (declarationUpdates != null)
                WriteNewConfigDeclarations(declarationUpdates, utilWriter, LineIndent, DefaultIndent, false);

            WriteNewConfigDefinitions(definitionUpdates, utilWriter, LineIndent, DefaultIndent);

            utilWriter.Write(FormatConfigurationEndElement);
        }

        private void WriteNewConfigDeclarations(SectionUpdates declarationUpdates, XmlUtilWriter utilWriter,
            int linePosition, int indent, bool skipFirstIndent)
        {
            if (!skipFirstIndent) utilWriter.AppendSpacesToLinePosition(linePosition);

            utilWriter.Write("<configSections>\r\n");
            WriteUnwrittenConfigDeclarations(declarationUpdates, utilWriter, linePosition + indent, indent, false);
            utilWriter.AppendSpacesToLinePosition(linePosition);
            utilWriter.Write("</configSections>\r\n");

            if (skipFirstIndent) utilWriter.AppendSpacesToLinePosition(linePosition);
        }

        private void WriteUnwrittenConfigDeclarations(SectionUpdates declarationUpdates, XmlUtilWriter utilWriter,
            int linePosition, int indent, bool skipFirstIndent)
        {
            WriteUnwrittenConfigDeclarationsRecursive(declarationUpdates, utilWriter, linePosition, indent,
                skipFirstIndent);
        }

        private void WriteUnwrittenConfigDeclarationsRecursive(SectionUpdates declarationUpdates,
            XmlUtilWriter utilWriter, int linePosition, int indent, bool skipFirstIndent)
        {
            string[] unretrievedSectionNames = declarationUpdates.GetUnretrievedSectionNames();
            if (unretrievedSectionNames != null)
            {
                foreach (string configKey in unretrievedSectionNames)
                {
                    Debug.Assert(!IsImplicitSection(configKey), "We should never write out an implicit section");
                    if (!skipFirstIndent) utilWriter.AppendSpacesToLinePosition(linePosition);
                    skipFirstIndent = false;

                    DeclarationUpdate update = declarationUpdates.GetDeclarationUpdate(configKey);
                    if (!string.IsNullOrEmpty(update?.UpdatedXml))
                    {
                        utilWriter.Write(update.UpdatedXml);
                        utilWriter.AppendNewLine();
                    }
                }
            }

            string[] unretrievedGroupNames = declarationUpdates.GetUnretrievedGroupNames();
            if (unretrievedGroupNames == null) return;

            foreach (string group in unretrievedGroupNames)
            {
                if (TargetFramework != null)
                {
                    ConfigurationSectionGroup g = GetSectionGroup(group);
                    if ((g != null) && !g.ShouldSerializeSectionGroupInTargetVersion(TargetFramework))
                    {
                        declarationUpdates.MarkGroupAsRetrieved(group);
                        continue;
                    }
                }
                if (!skipFirstIndent) utilWriter.AppendSpacesToLinePosition(linePosition);
                skipFirstIndent = false;

                SectionUpdates declarationUpdatesChild = declarationUpdates.GetSectionUpdatesForGroup(group);
                DeclarationUpdate groupUpdate = declarationUpdatesChild.GetSectionGroupUpdate();
                if (groupUpdate == null) utilWriter.Write("<sectionGroup name=\"" + group + "\">");
                else utilWriter.Write(groupUpdate.UpdatedXml);
                utilWriter.AppendNewLine();

                WriteUnwrittenConfigDeclarationsRecursive(declarationUpdatesChild, utilWriter, linePosition + indent,
                    indent, false);
                utilWriter.AppendSpacesToLinePosition(linePosition);
                utilWriter.Write("</sectionGroup>\r\n");
            }
        }

        private void WriteNewConfigDefinitions(ConfigDefinitionUpdates configDefinitionUpdates, XmlUtilWriter utilWriter,
            int linePosition, int indent)
        {
            if (configDefinitionUpdates == null)
                return;

            foreach (LocationUpdates locationUpdates in configDefinitionUpdates.LocationUpdatesList)
            {
                SectionUpdates sectionUpdates = locationUpdates.SectionUpdates;
                if (sectionUpdates.IsEmpty || !sectionUpdates.IsNew)
                    continue;

                configDefinitionUpdates.FlagLocationWritten();
                bool writeLocationTag = (_locationSubPath != null) || !locationUpdates.IsDefault;
                int recurseLinePosition = linePosition;

                utilWriter.AppendSpacesToLinePosition(linePosition);

                if (writeLocationTag)
                {
                    // write the <location> start tag
                    if (_locationSubPath == null)
                    {
                        utilWriter.Write(string.Format(CultureInfo.InvariantCulture, FormatLocationNoPath,
                            locationUpdates.OverrideMode.LocationTagXmlString,
                            BoolToString(locationUpdates.InheritInChildApps)));
                    }
                    else
                    {
                        utilWriter.Write(string.Format(CultureInfo.InvariantCulture, FormatLocationPath,
                            locationUpdates.OverrideMode.LocationTagXmlString,
                            BoolToString(locationUpdates.InheritInChildApps), _locationSubPath));
                    }

                    recurseLinePosition += indent;
                    utilWriter.AppendSpacesToLinePosition(recurseLinePosition);
                }

                // Invoke the recursive write.
                WriteNewConfigDefinitionsRecursive(utilWriter, locationUpdates.SectionUpdates, recurseLinePosition,
                    indent, true);

                if (writeLocationTag)
                {
                    // Write the location end tag
                    utilWriter.AppendSpacesToLinePosition(linePosition);
                    utilWriter.Write(FormatLocationEndElement);
                    utilWriter.AppendNewLine();
                }
            }

            if (configDefinitionUpdates.RequireLocation)
            {
                Debug.Assert(IsLocationConfig, "IsLocationConfig");

                // If we still require this to be written, then we must write it out now
                configDefinitionUpdates.FlagLocationWritten();

                utilWriter.AppendSpacesToLinePosition(linePosition);

                utilWriter.Write(string.Format(CultureInfo.InvariantCulture, FormatLocationPath,
                    OverrideModeSetting.s_locationDefault.LocationTagXmlString, KeywordTrue, _locationSubPath));
                utilWriter.AppendSpacesToLinePosition(linePosition);
                utilWriter.Write(FormatLocationEndElement);
                utilWriter.AppendNewLine();
            }
        }

        // Recursively write new sections for each section group.
        private bool WriteNewConfigDefinitionsRecursive(XmlUtilWriter utilWriter, SectionUpdates sectionUpdates,
            int linePosition, int indent, bool skipFirstIndent)
        {
            bool wroteASection = false;

            string[] movedSectionNames = sectionUpdates.GetMovedSectionNames();
            if (movedSectionNames != null)
            {
                wroteASection = true;
                foreach (string configKey in movedSectionNames)
                {
                    DefinitionUpdate update = sectionUpdates.GetDefinitionUpdate(configKey);
                    WriteSectionUpdate(utilWriter, update, linePosition, indent, skipFirstIndent);
                    utilWriter.AppendNewLine();
                    skipFirstIndent = false;
                }
            }

            string[] newGroupNames = sectionUpdates.GetNewGroupNames();
            if (newGroupNames != null)
            {
                foreach (string group in newGroupNames)
                {
                    if (TargetFramework != null)
                    {
                        ConfigurationSectionGroup g = GetSectionGroup(group);
                        if ((g != null) && !g.ShouldSerializeSectionGroupInTargetVersion(TargetFramework))
                        {
                            sectionUpdates.MarkGroupAsRetrieved(group);
                            continue;
                        }
                    }

                    if (!skipFirstIndent) utilWriter.AppendSpacesToLinePosition(linePosition);
                    skipFirstIndent = false;

                    utilWriter.Write("<" + group + ">\r\n");
                    bool recurseWroteASection = WriteNewConfigDefinitionsRecursive(
                        utilWriter, sectionUpdates.GetSectionUpdatesForGroup(group), linePosition + indent, indent,
                        false);

                    if (recurseWroteASection) wroteASection = true;

                    utilWriter.AppendSpacesToLinePosition(linePosition);
                    utilWriter.Write("</" + group + ">\r\n");
                }
            }

            sectionUpdates.IsNew = false;

            return wroteASection;
        }

        private static void CheckPreamble(byte[] preamble, XmlUtilWriter utilWriter, byte[] buffer)
        {
            bool hasByteOrderMark = false;
            using (Stream preambleStream = new MemoryStream(buffer))
            {
                byte[] streamStart = new byte[preamble.Length];
                if (preambleStream.Read(streamStart, 0, streamStart.Length) == streamStart.Length)
                {
                    hasByteOrderMark = true;
                    for (int i = 0; i < streamStart.Length; i++)
                        if (streamStart[i] != preamble[i])
                        {
                            hasByteOrderMark = false;
                            break;
                        }
                }
            }

            if (!hasByteOrderMark)
            {
                // Force the writer to emit byte order mark, then reset the stream
                // so that it is written over.
                object checkpoint = utilWriter.CreateStreamCheckpoint();
                utilWriter.Write('x');
                utilWriter.RestoreStreamCheckpoint(checkpoint);
            }
        }

        //
        // Calculate a new indent based on the position of the parent element and the current node.
        //
        private static int UpdateIndent(int oldIndent, XmlUtil xmlUtil, XmlUtilWriter utilWriter, int parentLinePosition)
        {
            int indent = oldIndent;
            if ((xmlUtil.Reader.NodeType == XmlNodeType.Element) && utilWriter.IsLastLineBlank)
            {
                int childLinePosition = xmlUtil.TrueLinePosition;
                if ((parentLinePosition < childLinePosition) && (childLinePosition <= parentLinePosition + MaxIndent))
                    indent = childLinePosition - parentLinePosition;
            }

            return indent;
        }

        // Copy a config file, replacing sections with updates.
        private void CopyConfig(SectionUpdates declarationUpdates, ConfigDefinitionUpdates definitionUpdates,
            byte[] buffer, string filename, NamespaceChange namespaceChange, XmlUtilWriter utilWriter)
        {
            CheckPreamble(ConfigStreamInfo.StreamEncoding.GetPreamble(), utilWriter, buffer);

            using (Stream stream = new MemoryStream(buffer))
            {
                using (XmlUtil xmlUtil = new XmlUtil(stream, filename, false))
                {
                    // copy up to the <configuration> node
                    XmlTextReader reader = xmlUtil.Reader;
                    reader.WhitespaceHandling = WhitespaceHandling.All;
                    reader.Read();
                    xmlUtil.CopyReaderToNextElement(utilWriter, false);

                    Debug.Assert((reader.NodeType == XmlNodeType.Element) && (reader.Name == ConfigurationTag),
                        "reader.NodeType == XmlNodeType.Element && reader.Name == KEYWORD_CONFIGURATION");

                    int indent = DefaultIndent;
                    int configurationElementLinePosition = xmlUtil.TrueLinePosition;
                    bool isEmptyConfigurationElement = reader.IsEmptyElement;

                    // copy <configuration> node
                    // if the node is an empty element, we may need to open it.
                    string configurationStartElement;
                    if (namespaceChange == NamespaceChange.Add)
                    {
                        configurationStartElement = string.Format(
                            CultureInfo.InvariantCulture, FormatConfigurationNamespace,
                            ConfigurationNamespace);
                    }
                    else
                    {
                        configurationStartElement = namespaceChange == NamespaceChange.Remove
                            ? FormatConfiguration
                            : null;
                    }

                    bool needsChildren = (declarationUpdates != null) || (definitionUpdates != null);
                    string configurationEndElement = xmlUtil.UpdateStartElement(utilWriter, configurationStartElement,
                        needsChildren, configurationElementLinePosition, indent);

                    bool foundConfigSectionsElement = false;
                    if (!isEmptyConfigurationElement)
                    {
                        // copy up to the first element under <configuration>
                        xmlUtil.CopyReaderToNextElement(utilWriter, true);

                        // updateIndent
                        indent = UpdateIndent(indent, xmlUtil, utilWriter, configurationElementLinePosition);

                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == ConfigSectionsTag))
                        {
                            foundConfigSectionsElement = true;

                            int configSectionsElementLinePosition = xmlUtil.TrueLinePosition;
                            bool isEmptyConfigSectionsElement = reader.IsEmptyElement;

                            // if no updates, copy the entire <configSections> element
                            if (declarationUpdates == null) xmlUtil.CopyOuterXmlToNextElement(utilWriter, true);
                            else
                            {
                                // copy <configSections>, and open it if it is an empty element
                                string configSectionsEndElement = xmlUtil.UpdateStartElement(
                                    utilWriter, null, true, configSectionsElementLinePosition, indent);

                                if (!isEmptyConfigSectionsElement)
                                {
                                    // copy to next element under <configSections>, or up to closing </configSections>
                                    xmlUtil.CopyReaderToNextElement(utilWriter, true);

                                    // copy config declarations
                                    CopyConfigDeclarationsRecursive(declarationUpdates, xmlUtil, utilWriter,
                                        string.Empty,
                                        configSectionsElementLinePosition, indent);

                                    Debug.Assert(
                                        (reader.NodeType == XmlNodeType.EndElement) &&
                                        (reader.Name == ConfigSectionsTag),
                                        "reader.NodeType == XmlNodeType.EndElement && reader.Name == \"KEYWORD_CONFIGSECTIONS\"");
                                }

                                // write declarations not written by above copy
                                if (declarationUpdates.HasUnretrievedSections())
                                {
                                    // determine the line position of the end element
                                    int endElementLinePosition = 0;
                                    if (configSectionsEndElement == null)
                                        endElementLinePosition = xmlUtil.TrueLinePosition;

                                    // indent a new line
                                    if (!utilWriter.IsLastLineBlank) utilWriter.AppendNewLine();

                                    WriteUnwrittenConfigDeclarations(declarationUpdates, utilWriter,
                                        configSectionsElementLinePosition + indent, indent, false);

                                    // restore spaces to end element
                                    if (configSectionsEndElement == null)
                                        utilWriter.AppendSpacesToLinePosition(endElementLinePosition);
                                }

                                // Copy the </configSections> element
                                if (configSectionsEndElement == null) xmlUtil.CopyXmlNode(utilWriter);
                                else
                                {
                                    // note that configSectionsEndElement already contains the proper indenting
                                    utilWriter.Write(configSectionsEndElement);
                                }

                                // copy up to the next element under <configuration>, or up to closing </configSections>
                                xmlUtil.CopyReaderToNextElement(utilWriter, true);
                            }
                        }
                    }

                    // Write new declarations
                    if (!foundConfigSectionsElement && (declarationUpdates != null))
                    {
                        bool skipFirstIndent = (reader.Depth > 0) && (reader.NodeType == XmlNodeType.Element);
                        int newConfigSectionsLinePosition;
                        if (skipFirstIndent) newConfigSectionsLinePosition = xmlUtil.TrueLinePosition;
                        else newConfigSectionsLinePosition = configurationElementLinePosition + indent;

                        WriteNewConfigDeclarations(declarationUpdates, utilWriter, newConfigSectionsLinePosition, indent,
                            skipFirstIndent);
                    }

                    if (definitionUpdates != null)
                    {
                        // Copy sections recursively. In the file we copy we start out at
                        //     location path="." allowOverride="true" inheritInChildApps="true"
                        bool locationPathApplies = false;
                        LocationUpdates locationUpdates = null;
                        SectionUpdates sectionUpdates = null;
                        if (!IsLocationConfig)
                        {
                            locationPathApplies = true;
                            locationUpdates = definitionUpdates.FindLocationUpdates(
                                OverrideModeSetting.s_locationDefault, true);
                            if (locationUpdates != null) sectionUpdates = locationUpdates.SectionUpdates;
                        }

                        CopyConfigDefinitionsRecursive(definitionUpdates, xmlUtil, utilWriter, locationPathApplies,
                            locationUpdates, sectionUpdates, true, string.Empty, configurationElementLinePosition,
                            indent);

                        // Write new config sections from new groups.
                        WriteNewConfigDefinitions(definitionUpdates, utilWriter,
                            configurationElementLinePosition + indent, indent);

#if DEBUG
                        Debug.Assert(
                            (configurationEndElement != null) ||
                            ((reader.NodeType == XmlNodeType.EndElement) && (reader.Name == ConfigurationTag)),
                            "configurationEndElement != null || (reader.NodeType == XmlNodeType.EndElement && reader.Name == KEYWORD_CONFIGURATION)");
                        foreach (LocationUpdates l in definitionUpdates.LocationUpdatesList)
                            Debug.Assert(!l.SectionUpdates.HasUnretrievedSections(),
                                "!l.SectionUpdates.HasUnretrievedSections()");
#endif
                    }


                    if (configurationEndElement != null)
                    {
                        // If we have to add closing config tag, then do it now
                        // before copying extra whitespace/comments
                        if (!utilWriter.IsLastLineBlank) utilWriter.AppendNewLine();

                        utilWriter.Write(configurationEndElement);
                    }

                    // Copy the remainder of the file, the closing </configuration> node plus any whitespace
                    // and comments
                    while (xmlUtil.CopyXmlNode(utilWriter)) { }
                }
            }
        }

        private bool CopyConfigDeclarationsRecursive(
            SectionUpdates declarationUpdates, XmlUtil xmlUtil, XmlUtilWriter utilWriter, string group,
            int parentLinePosition, int parentIndent)
        {
            bool wroteASection = false;
            XmlTextReader reader = xmlUtil.Reader;
            int linePosition;
            int indent;
            int startingLinePosition;

            indent = UpdateIndent(parentIndent, xmlUtil, utilWriter, parentLinePosition);

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    linePosition = xmlUtil.TrueLinePosition;
                    startingLinePosition = linePosition;
                    break;
                case XmlNodeType.EndElement:
                    linePosition = parentLinePosition + indent;
                    startingLinePosition = utilWriter.IsLastLineBlank ? xmlUtil.TrueLinePosition : parentLinePosition;
                    break;
                default:
                    linePosition = parentLinePosition + indent;
                    startingLinePosition = 0;
                    break;
            }

            // Write any new section declarations that apply to this group
            string[] movedSectionNames = declarationUpdates?.GetMovedSectionNames();

            if (movedSectionNames != null)
            {
                if (!utilWriter.IsLastLineBlank) utilWriter.AppendNewLine();

                foreach (string configKey in movedSectionNames)
                {
                    DeclarationUpdate sectionUpdate = declarationUpdates.GetDeclarationUpdate(configKey);
                    Debug.Assert(!IsImplicitSection(configKey), "We should never write out an implicit section");

                    // Write the one line section declaration.
                    utilWriter.AppendSpacesToLinePosition(linePosition);
                    utilWriter.Write(sectionUpdate.UpdatedXml);
                    utilWriter.AppendNewLine();

                    wroteASection = true;
                }

                // Restore the whitespace we used for the first element, which is either a start or an end element.
                utilWriter.AppendSpacesToLinePosition(startingLinePosition);
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                // For each element at this depth, either:
                // - Write the element verbatim and recurse due to a group hierarchy element.
                // - Write the element verbatim because it is unchanged
                // - Write the updated XML for the section.
                // - Skip it because the section has been removed.
                int depth = reader.Depth;
                while (reader.Depth == depth)
                {
                    bool recurse = false;
                    DeclarationUpdate sectionUpdate = null;
                    DeclarationUpdate groupUpdate = null;
                    SectionUpdates declarationUpdatesChild = null;
                    SectionUpdates recurseDeclarationUpdates = declarationUpdates;
                    string recurseGroup = group;

                    // update the lineposition and indent for each element
                    indent = UpdateIndent(indent, xmlUtil, utilWriter, parentLinePosition);
                    linePosition = xmlUtil.TrueLinePosition;

                    string directive = reader.Name;
                    string name = reader.GetAttribute(SectionGroupNameAttribute);
                    string configKey = CombineConfigKey(group, name);
                    if (directive == SectionGroupTag)
                    {
                        // it's a group - get the updates for children
                        declarationUpdatesChild = declarationUpdates.GetSectionUpdatesForGroup(name);
                        if (declarationUpdatesChild != null)
                        {
                            // get the group update
                            groupUpdate = declarationUpdatesChild.GetSectionGroupUpdate();

                            // recurse if there are more sections to copy
                            if (declarationUpdatesChild.HasUnretrievedSections())
                            {
                                recurse = true;
                                recurseGroup = configKey;
                                recurseDeclarationUpdates = declarationUpdatesChild;
                            }
                        }
                    }
                    else
                    {
                        // it is a section - get the update
                        Debug.Assert(!IsImplicitSection(configKey), "We should never write out an implicit section");
                        sectionUpdate = declarationUpdates.GetDeclarationUpdate(configKey);
                    }

                    bool writeGroupUpdate = groupUpdate?.UpdatedXml != null;
                    if (recurse)
                    {
                        // create a checkpoint that we can revert to if no children are written
                        object checkpoint = utilWriter.CreateStreamCheckpoint();
                        string closingElement = null;

                        // Copy this element node and up to the first subelement
                        if (writeGroupUpdate)
                        {
                            // replace the element with the updated xml
                            utilWriter.Write(groupUpdate.UpdatedXml);

                            // skip over the start element
                            reader.Read();
                        }
                        else closingElement = xmlUtil.UpdateStartElement(utilWriter, null, true, linePosition, indent);

                        if (closingElement == null)
                        {
                            // Only if there is a closing element should
                            // we move to it
                            xmlUtil.CopyReaderToNextElement(utilWriter, true);
                        }

                        // Recurse
                        bool recurseWroteASection = CopyConfigDeclarationsRecursive(
                            recurseDeclarationUpdates, xmlUtil, utilWriter, recurseGroup, linePosition, indent);

                        if (closingElement != null)
                        {
                            utilWriter.AppendSpacesToLinePosition(linePosition);
                            utilWriter.Write(closingElement);

                            // Since we already got to </configSections> in reader, lets
                            // indent so we can copy the element in the right place
                            utilWriter.AppendSpacesToLinePosition(parentLinePosition);
                        }
                        else
                        {
                            // Copy the end element
                            xmlUtil.CopyXmlNode(utilWriter);
                        }

                        if (recurseWroteASection || writeGroupUpdate) wroteASection = true;
                        else
                        {
                            // back out the change
                            utilWriter.RestoreStreamCheckpoint(checkpoint);
                        }

                        // Copy up to the next element, or exit this level.
                        xmlUtil.CopyReaderToNextElement(utilWriter, true);
                    }
                    else
                    {
                        bool skip;
                        bool skipChildElements = false;
                        if (sectionUpdate == null)
                        {
                            skip = true;
                            if (writeGroupUpdate)
                            {
                                // Insert an empty <sectionGroup type="typename" > node, to introduce the type
                                wroteASection = true;
                                utilWriter.Write(groupUpdate.UpdatedXml);
                                utilWriter.AppendNewLine();
                                utilWriter.AppendSpacesToLinePosition(linePosition);
                                utilWriter.Write(FormatSectionGroupEndElement);
                                utilWriter.AppendNewLine();
                                utilWriter.AppendSpacesToLinePosition(linePosition);
                            }
                            else
                            {
                                if (groupUpdate != null)
                                {
                                    // If groupUpdate exists, that means we've decided in GetConfigDeclarationUpdates
                                    // that the section group should stay in the file.
                                    Debug.Assert(groupUpdate.UpdatedXml == null, "groupUpdate.UpdatedXml == null");
                                    Debug.Assert(!declarationUpdatesChild.HasUnretrievedSections(),
                                        "If the group has any unretrieved section, we should have chosen the recursive code path above.");

                                    wroteASection = true;
                                    skip = false;

                                    // We should skip all the child sections.  If we indeed need to keep any child
                                    // section, we should have chosen the recursive code path above.
                                    skipChildElements = true;
                                }
                            }
                        }
                        else
                        {
                            wroteASection = true;
                            if (sectionUpdate.UpdatedXml == null) skip = false;
                            else
                            {
                                skip = true;

                                // Write the updated XML on a single line
                                utilWriter.Write(sectionUpdate.UpdatedXml);
                            }
                        }

                        if (skip)
                        {
                            // Skip over the existing element, then
                            // copy up to the next element, or exit this level.
                            xmlUtil.SkipAndCopyReaderToNextElement(utilWriter, true);
                        }
                        else
                        {
                            if (skipChildElements) xmlUtil.SkipChildElementsAndCopyOuterXmlToNextElement(utilWriter);
                            else
                            {
                                // Copy this entire contents of this element and then to the next element, or exit this level.
                                xmlUtil.CopyOuterXmlToNextElement(utilWriter, true);
                            }
                        }
                    }
                }
            }

            return wroteASection;
        }

        // Copy configuration sections from the original configuration file.
        private bool CopyConfigDefinitionsRecursive(
            ConfigDefinitionUpdates configDefinitionUpdates, XmlUtil xmlUtil, XmlUtilWriter utilWriter,
            bool locationPathApplies, LocationUpdates locationUpdates, SectionUpdates sectionUpdates,
            bool addNewSections, string group, int parentLinePosition, int parentIndent)
        {
            bool wroteASection = false;
            XmlTextReader reader = xmlUtil.Reader;
            int linePosition;
            int indent;
            int startingLinePosition;

            indent = UpdateIndent(parentIndent, xmlUtil, utilWriter, parentLinePosition);

            if (reader.NodeType == XmlNodeType.Element)
            {
                linePosition = xmlUtil.TrueLinePosition;
                startingLinePosition = linePosition;
            }
            else
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    linePosition = parentLinePosition + indent;
                    startingLinePosition = utilWriter.IsLastLineBlank ? xmlUtil.TrueLinePosition : parentLinePosition;
                }
                else
                {
                    linePosition = parentLinePosition + indent;
                    startingLinePosition = 0;
                }
            }

            // Write any new sections that apply to this group
            if ((sectionUpdates != null) && addNewSections)
            {
                // Remove newness, so we won't write again
                sectionUpdates.IsNew = false;

                Debug.Assert(locationPathApplies, nameof(locationPathApplies));
                string[] movedSectionNames = sectionUpdates.GetMovedSectionNames();
                if (movedSectionNames != null)
                {
                    if (!utilWriter.IsLastLineBlank) utilWriter.AppendNewLine();

                    utilWriter.AppendSpacesToLinePosition(linePosition);
                    bool skipFirstIndent = true;

                    foreach (string configKey in movedSectionNames)
                    {
                        DefinitionUpdate update = sectionUpdates.GetDefinitionUpdate(configKey);

                        WriteSectionUpdate(utilWriter, update, linePosition, indent, skipFirstIndent);
                        skipFirstIndent = false;
                        utilWriter.AppendNewLine();
                        wroteASection = true;
                    }

                    // Restore the whitespace we used for the first element, which is either a start or an end element.
                    utilWriter.AppendSpacesToLinePosition(startingLinePosition);
                }
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                // For each element at this depth, either:
                // - Write the element verbatim and recurse due to a location section or group hierarchy element.
                // - Write the element verbatim because it is unchanged, or because the current location does
                //   not apply.
                // - Write the updated XML for the section.
                // - Skip it because the section has been removed.
                int depth = reader.Depth;
                while (reader.Depth == depth)
                {
                    bool recurse = false;
                    DefinitionUpdate update = null;
                    bool elementLocationPathApplies = locationPathApplies;
                    LocationUpdates recurseLocationUpdates = locationUpdates;
                    SectionUpdates recurseSectionUpdates = sectionUpdates;
                    bool recurseAddNewSections = addNewSections;
                    string recurseGroup = group;
                    bool removedSectionOrGroup = false;

                    // update the lineposition and indent for each element
                    indent = UpdateIndent(indent, xmlUtil, utilWriter, parentLinePosition);
                    linePosition = xmlUtil.TrueLinePosition;

                    string elementName = reader.Name;
                    if (elementName == LocationTag)
                    {
                        string locationSubPathAttribute = reader.GetAttribute(LocationPathAttribute);
                        locationSubPathAttribute = NormalizeLocationSubPath(locationSubPathAttribute, xmlUtil);
                        OverrideModeSetting overrideMode = OverrideModeSetting.s_locationDefault;
                        bool inheritInChildApps = true;

                        if (IsLocationConfig)
                        {
                            // For location config we will compare config paths instead of location strings
                            // so that we dont end up comparing "1" with "Default Web Site" and ending up with the wrong result
                            if (locationSubPathAttribute == null) elementLocationPathApplies = false;
                            else
                            {
                                elementLocationPathApplies = StringUtil.EqualsIgnoreCase(ConfigPath,
                                    Host.GetConfigPathFromLocationSubPath(Parent.ConfigPath, locationSubPathAttribute));
                            }
                        }
                        else
                        {
                            Debug.Assert(LocationSubPath == null);

                            // This is the same as doing StringUtil.EqualsIgnoreCase(_locationSubPath, locationSubPathAttribute)
                            // but remember the first one is null. Also remember locationSubPathAttribute is already normalized
                            elementLocationPathApplies = locationSubPathAttribute == null;
                        }

                        if (elementLocationPathApplies)
                        {
                            // Retrieve overrideMode and InheritInChildApps
                            string allowOverrideAttribute = reader.GetAttribute(LocationAllowOverrideAttribute);
                            if (allowOverrideAttribute != null)
                            {
                                overrideMode =
                                    OverrideModeSetting.CreateFromXmlReadValue(bool.Parse(allowOverrideAttribute));
                            }

                            string overrideModeAttribute = reader.GetAttribute(LocationOverrideModeAttribute);
                            if (overrideModeAttribute != null)
                            {
                                overrideMode =
                                    OverrideModeSetting.CreateFromXmlReadValue(
                                        OverrideModeSetting.ParseOverrideModeXmlValue(overrideModeAttribute, null));

                                Debug.Assert(allowOverrideAttribute == null,
                                    "allowOverride and overrideMode both detected in a <location> tag");
                            }

                            string inheritInChildAppsAttribute =
                                reader.GetAttribute(LocationInheritInChildApplicationsAttribute);
                            if (inheritInChildAppsAttribute != null)
                                inheritInChildApps = bool.Parse(inheritInChildAppsAttribute);

                            // Flag that we already have one of these locations
                            configDefinitionUpdates.FlagLocationWritten();
                        }

                        if (reader.IsEmptyElement)
                        {
                            if (elementLocationPathApplies &&
                                (configDefinitionUpdates.FindLocationUpdates(overrideMode,
                                    inheritInChildApps) != null))
                            {
                                // If we are going to make updates here, then
                                // delete the one that is here (so we can update later)
                                elementLocationPathApplies = true;
                            }
                            else
                            {
                                // If not lets leave it
                                elementLocationPathApplies = false;
                            }
                        }
                        else
                        {
                            // recurse if this location applies to us
                            if (elementLocationPathApplies)
                            {
                                if (configDefinitionUpdates != null)
                                {
                                    recurseLocationUpdates = configDefinitionUpdates.FindLocationUpdates(overrideMode,
                                        inheritInChildApps);
                                    if (recurseLocationUpdates != null)
                                    {
                                        recurse = true;
                                        recurseSectionUpdates = recurseLocationUpdates.SectionUpdates;

                                        // If this is <location path=".">, we don't want to add moved sections
                                        // to it.
                                        if ((_locationSubPath == null) && recurseLocationUpdates.IsDefault)
                                            recurseAddNewSections = false;
                                    }
                                }
                            }
                            else
                            {
                                // recurse if necessary to remove items in _removedSections and _removedGroups
                                if (HasRemovedSectionsOrGroups && !IsLocationConfig && Host.SupportsLocation)
                                {
                                    recurse = true;
                                    recurseLocationUpdates = null;
                                    recurseSectionUpdates = null;
                                    recurseAddNewSections = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        string configKey = CombineConfigKey(group, elementName);
                        FactoryRecord factoryRecord = FindFactoryRecord(configKey, false);
                        if (factoryRecord == null)
                        {
                            // The factory was deleted, so regardless of whether this is a
                            // section or sectionGroup, it can be skipped.
                            if (!elementLocationPathApplies && !IsLocationConfig) removedSectionOrGroup = true;
                        }
                        else
                        {
                            if (factoryRecord.IsGroup)
                            {
                                if (reader.IsEmptyElement)
                                {
                                    if (!elementLocationPathApplies && !IsLocationConfig) removedSectionOrGroup = true;
                                }
                                else
                                {
                                    // if the location path applies, recurse if there are updates
                                    if (sectionUpdates != null)
                                    {
                                        SectionUpdates sectionUpdatesChild =
                                            sectionUpdates.GetSectionUpdatesForGroup(elementName);
                                        if (sectionUpdatesChild != null)
                                        {
                                            recurse = true;
                                            recurseGroup = configKey;
                                            recurseSectionUpdates = sectionUpdatesChild;
                                        }
                                    }
                                    else
                                    {
                                        if (!elementLocationPathApplies && !IsLocationConfig)
                                        {
                                            if ((_removedSectionGroups != null) &&
                                                _removedSectionGroups.Contains(configKey))
                                                removedSectionOrGroup = true;
                                            else
                                            {
                                                recurse = true;
                                                recurseGroup = configKey;
                                                recurseLocationUpdates = null;
                                                recurseSectionUpdates = null;
                                                recurseAddNewSections = false;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // it is a section - get the update
                                if (sectionUpdates != null) update = sectionUpdates.GetDefinitionUpdate(configKey);
                                else
                                {
                                    if (!elementLocationPathApplies && !IsLocationConfig)
                                    {
                                        if ((_removedSections != null) && _removedSections.Contains(configKey))
                                            removedSectionOrGroup = true;
                                    }
                                }
                            }
                        }
                    }

                    if (recurse)
                    {
                        // flush, and get length of underlying stream
                        object checkpoint = utilWriter.CreateStreamCheckpoint();

                        // Copy this element node and up to the first subelement
                        xmlUtil.CopyXmlNode(utilWriter);
                        xmlUtil.CopyReaderToNextElement(utilWriter, true);

                        // Recurse
                        bool recurseWroteASection = CopyConfigDefinitionsRecursive(
                            configDefinitionUpdates, xmlUtil, utilWriter, elementLocationPathApplies,
                            recurseLocationUpdates, recurseSectionUpdates,
                            recurseAddNewSections, recurseGroup, linePosition, indent);

                        // Copy the end element
                        xmlUtil.CopyXmlNode(utilWriter);

                        if (recurseWroteASection) wroteASection = true;
                        else
                        {
                            // back out the change
                            utilWriter.RestoreStreamCheckpoint(checkpoint);
                        }

                        // Copy up to the next element, or exit this level.
                        xmlUtil.CopyReaderToNextElement(utilWriter, true);
                    }
                    else
                    {
                        bool skip;
                        if (update == null)
                        {
                            // remove the section from the file if we're in the correct location,
                            // or if the section or group should be removed from all locations
                            skip = elementLocationPathApplies || removedSectionOrGroup;
                        }
                        else
                        {
                            // replace the section if the xml for it has been updated
                            // if it is a configSource, don't write it unless the configSource parameters have changed
                            skip = false;
                            if (update.UpdatedXml != null)
                            {
                                ConfigurationSection configSection = (ConfigurationSection)update.SectionRecord.Result;
                                if (string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource) ||
                                    configSection.SectionInformation.ConfigSourceModified)
                                {
                                    skip = true;
                                    WriteSectionUpdate(utilWriter, update, linePosition, indent, true);
                                    wroteASection = true;
                                }
                            }
                        }

                        if (skip)
                        {
                            // Skip over the existing element, then
                            // copy up to the next element, or exit this level.
                            xmlUtil.SkipAndCopyReaderToNextElement(utilWriter, true);
                        }
                        else
                        {
                            // Copy this entire contents of this element and then to the next element, or exit this level.
                            xmlUtil.CopyOuterXmlToNextElement(utilWriter, true);
                            wroteASection = true;
                        }
                    }
                }
            }

            // Write new section groups
            if ((sectionUpdates != null) && addNewSections && sectionUpdates.HasNewSectionGroups())
            {
                // Add whitespace to align us with the other elements in this group
                linePosition = parentLinePosition + indent;
                if (reader.NodeType == XmlNodeType.EndElement)
                    startingLinePosition = utilWriter.IsLastLineBlank ? xmlUtil.TrueLinePosition : parentLinePosition;
                else startingLinePosition = 0;

                utilWriter.AppendSpacesToLinePosition(linePosition);

                bool wroteNewSection = WriteNewConfigDefinitionsRecursive(utilWriter, sectionUpdates, linePosition,
                    indent, true);
                if (wroteNewSection) wroteASection = true;

                // Restore the whitespace of the end element
                utilWriter.AppendSpacesToLinePosition(startingLinePosition);
            }

            return wroteASection;
        }

        private static void WriteSectionUpdate(XmlUtilWriter utilWriter, DefinitionUpdate update, int linePosition, int indent,
            bool skipFirstIndent)
        {
            ConfigurationSection configSection = (ConfigurationSection)update.SectionRecord.Result;
            string updatedXml;

            if (!string.IsNullOrEmpty(configSection.SectionInformation.ConfigSource))
            {
                updatedXml = string.Format(CultureInfo.InvariantCulture, FormatSectionConfigSource,
                    configSection.SectionInformation.Name, configSection.SectionInformation.ConfigSource);
            }
            else updatedXml = update.UpdatedXml;

            string formattedXml = XmlUtil.FormatXmlElement(updatedXml, linePosition, indent, skipFirstIndent);
            utilWriter.Write(formattedXml);
        }

        // SaveConfigSource
        private void SaveConfigSource(DefinitionUpdate update)
        {
            string configSourceStreamName;

            if (update.SectionRecord.HasResult)
            {
                ConfigurationSection configSection = (ConfigurationSection)update.SectionRecord.Result;
                configSourceStreamName = configSection.SectionInformation.ConfigSourceStreamName;
            }
            else
            {
                Debug.Assert(update.SectionRecord.HasFileInput, "update.SectionRecord.HasFileInput");
                SectionInput fileInput = update.SectionRecord.FileInput;
                configSourceStreamName = fileInput.SectionXmlInfo.ConfigSourceStreamName;
            }

            // Copy the input stream before opening the output stream.
            byte[] readBuffer = null;
            using (Stream streamRead = Host.OpenStreamForRead(configSourceStreamName))
            {
                if (streamRead != null)
                {
                    readBuffer = new byte[streamRead.Length];
                    int count = streamRead.Read(readBuffer, 0, (int)streamRead.Length);
                    if (count != streamRead.Length) throw new ConfigurationErrorsException();
                }
            }

            // Write the changes to the output stream.
            bool hasFile = readBuffer != null;
            object writeContext = null;
            bool streamOpened = false;

            try
            {
                try
                {
                    // templateStreamName is used by OpenStreamForWrite for copying file attributes during saving.
                    // (for details, see WriteFileContext.Complete.)
                    //
                    // If we're using a remote host, then ConfigStreamInfo.StreamName is actually pointing to a
                    // full filepath on a remote machine.  In this case, it's impossible to copy the attributes
                    // over, and thus we won't do it.
                    string templateStreamName = Host.IsRemote ? null : ConfigStreamInfo.StreamName;

                    using (
                        Stream streamWrite = Host.OpenStreamForWrite(configSourceStreamName, templateStreamName,
                            ref writeContext))
                    {
                        streamOpened = true;
                        if (update.UpdatedXml == null)
                        {
                            Debug.Assert(hasFile, "hasFile");
                            if (hasFile) streamWrite.Write(readBuffer, 0, readBuffer.Length);
                        }
                        else
                        {
                            using (StreamWriter streamWriter = new StreamWriter(streamWrite))
                            {
                                XmlUtilWriter utilWriter = new XmlUtilWriter(streamWriter, true);
                                if (hasFile)
                                    CopyConfigSource(utilWriter, update.UpdatedXml, configSourceStreamName, readBuffer);
                                else CreateNewConfigSource(utilWriter, update.UpdatedXml, DefaultIndent);
                            }
                        }
                    }
                }
                catch
                {
                    if (streamOpened) Host.WriteCompleted(configSourceStreamName, false, writeContext);
                    throw;
                }
            }
            catch (Exception e)
            {
                // Guarantee that exceptions contain at least the name of the stream by wrapping them
                // in a ConfigurationException.
                throw ExceptionUtil.WrapAsConfigException(SR.Config_error_loading_XML_file, e, configSourceStreamName, 0);
            }

            Host.WriteCompleted(configSourceStreamName, true, writeContext);
        }

        private void CopyConfigSource(XmlUtilWriter utilWriter, string updatedXml, string configSourceStreamName,
            byte[] buffer)
        {
            // only copy the byte order mark if it exists in the current web.config
            byte[] preamble;
            using (Stream stream = new MemoryStream(buffer))
            {
                using (new XmlUtil(stream, configSourceStreamName, true))
                {
                    preamble = ConfigStreamInfo.StreamEncoding.GetPreamble();
                }
            }

            CheckPreamble(preamble, utilWriter, buffer);

            using (Stream stream = new MemoryStream(buffer))
            {
                using (XmlUtil xmlUtil = new XmlUtil(stream, configSourceStreamName, false))
                {
                    XmlTextReader reader = xmlUtil.Reader;

                    // copy up to the first element
                    reader.WhitespaceHandling = WhitespaceHandling.All;
                    reader.Read();

                    // determine the indent to use for the element
                    int indent = DefaultIndent;
                    int linePosition = 1;
                    bool hasElement = xmlUtil.CopyReaderToNextElement(utilWriter, false);
                    if (hasElement)
                    {
                        // find the indent of the first attribute, if any
                        int lineNumber = reader.LineNumber;
                        linePosition = reader.LinePosition - 1;
                        int attributeIndent = 0;
                        while (reader.MoveToNextAttribute())
                            if (reader.LineNumber > lineNumber)
                            {
                                attributeIndent = reader.LinePosition - linePosition;
                                break;
                            }

                        // find the indent of the first sub element, if any
                        int elementIndent = 0;
                        reader.Read();
                        while (reader.Depth >= 1)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                elementIndent = reader.LinePosition - 1 - linePosition;
                                break;
                            }

                            reader.Read();
                        }

                        if (elementIndent > 0) indent = elementIndent;
                        else
                        {
                            if (attributeIndent > 0) indent = attributeIndent;
                        }
                    }

                    // Write the config source
                    string formattedXml = XmlUtil.FormatXmlElement(updatedXml, linePosition, indent, true);
                    utilWriter.Write(formattedXml);

                    // Copy remaining contents
                    if (hasElement)
                    {
                        // Skip over the existing element
                        while (reader.Depth > 0) reader.Read();

                        if (reader.IsEmptyElement || (reader.NodeType == XmlNodeType.EndElement)) reader.Read();

                        // Copy remainder of file
                        while (xmlUtil.CopyXmlNode(utilWriter)) { }
                    }
                }
            }
        }

        private void CreateNewConfigSource(XmlUtilWriter utilWriter, string updatedXml, int indent)
        {
            string formattedXml = XmlUtil.FormatXmlElement(updatedXml, 0, indent, true);
            utilWriter.Write(string.Format(CultureInfo.InvariantCulture, FormatConfigSourceFile,
                ConfigStreamInfo.StreamEncoding.WebName));
            utilWriter.Write(formattedXml + NewLine);
        }

        private static string BoolToString(bool v)
        {
            return v ? KeywordTrue : KeywordFalse;
        }

        // It is possible that we have set the flag to force this location
        // to be written out.  Allow a way to remove that
        internal void RemoveLocationWriteRequirement()
        {
            if (IsLocationConfig)
            {
                _flags[ForceLocationWritten] = false;
                _flags[SuggestLocationRemoval] = true;
            }
        }
    }
}
