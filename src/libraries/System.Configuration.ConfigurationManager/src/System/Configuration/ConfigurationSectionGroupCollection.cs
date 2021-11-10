// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;

namespace System.Configuration
{
    public sealed class ConfigurationSectionGroupCollection : NameObjectCollectionBase
    {
        private readonly ConfigurationSectionGroup _configSectionGroup;
        private MgmtConfigurationRecord _configRecord;

        internal ConfigurationSectionGroupCollection(MgmtConfigurationRecord configRecord,
            ConfigurationSectionGroup configSectionGroup) :
            base(StringComparer.Ordinal)
        {
            _configRecord = configRecord;
            _configSectionGroup = configSectionGroup;

            foreach (DictionaryEntry de in _configRecord.SectionGroupFactories)
            {
                FactoryId factoryId = (FactoryId)de.Value;
                if (factoryId.Group == _configSectionGroup.SectionGroupName) BaseAdd(factoryId.Name, factoryId.Name);
            }
        }

        // Indexer via name
        public ConfigurationSectionGroup this[string name] => Get(name);

        // Indexer via integer index.
        public ConfigurationSectionGroup this[int index] => Get(index);

        // Remove the collection from configuration system, and remove all entries
        // in the base collection so that enumeration will return an empty collection.
        internal void DetachFromConfigurationRecord()
        {
            _configRecord = null;
            BaseClear();
        }

        private void VerifyIsAttachedToConfigRecord()
        {
            if (_configRecord == null)
                throw new InvalidOperationException(SR.Config_cannot_edit_configurationsectiongroup_when_not_attached);
        }

        public void Add(string name, ConfigurationSectionGroup sectionGroup)
        {
            VerifyIsAttachedToConfigRecord();
            _configRecord.AddConfigurationSectionGroup(_configSectionGroup.SectionGroupName, name, sectionGroup);
            BaseAdd(name, name);
        }

        public void Clear()
        {
            VerifyIsAttachedToConfigRecord();

            // If this is the root section group, do not require the location section to be written
            // to the file.
            if (_configSectionGroup.IsRoot) _configRecord.RemoveLocationWriteRequirement();

            string[] allKeys = BaseGetAllKeys();
            foreach (string key in allKeys) Remove(key);
        }

        public void CopyTo(ConfigurationSectionGroup[] array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            int c = Count;
            if (array.Length < c + index) throw new ArgumentOutOfRangeException(nameof(index));

            for (int i = 0, j = index; i < c; i++, j++) array[j] = Get(i);
        }

        public ConfigurationSectionGroup Get(int index)
        {
            return Get(GetKey(index));
        }

        public ConfigurationSectionGroup Get(string name)
        {
            VerifyIsAttachedToConfigRecord();

            // validate name
            if (string.IsNullOrEmpty(name))
                throw ExceptionUtil.ParameterNullOrEmpty(nameof(name));

            // prevent GetConfig from returning config not in this collection
#if NETCOREAPP
            if (name.Contains('/'))
#else
            if (name.IndexOf('/') >= 0)
#endif
                return null;

            // get the section group
            string configKey = BaseConfigurationRecord.CombineConfigKey(_configSectionGroup.SectionGroupName, name);
            return _configRecord.GetSectionGroup(configKey);
        }

        public override IEnumerator GetEnumerator()
        {
            int c = Count;
            for (int i = 0; i < c; i++) yield return this[i];
        }

        public string GetKey(int index)
        {
            return BaseGetKey(index);
        }

        // Remove the declaration and definition of a section in this config file, including any
        // location sections in the file. This will also remove any descendant sections and
        // section groups.
        //
        // Note that if the section group is declared in a parent, we still remove the declaration and
        // definition, and the instance of ConfigurationSectionGroup will be detached from the collection.
        // However, the collection will still have a ConfigurationSectionGroup of that name in the collection,
        // only it will have the value of the immediate parent.
        public void Remove(string name)
        {
            VerifyIsAttachedToConfigRecord();

            _configRecord.RemoveConfigurationSectionGroup(_configSectionGroup.SectionGroupName, name);

            // Remove the section from the collection if it is no longer in the list of all SectionGroupFactories.
            string configKey = BaseConfigurationRecord.CombineConfigKey(_configSectionGroup.SectionGroupName, name);
            if (!_configRecord.SectionFactories.Contains(configKey)) BaseRemove(name);
        }

        public void RemoveAt(int index)
        {
            VerifyIsAttachedToConfigRecord();

            Remove(GetKey(index));
        }
    }
}
