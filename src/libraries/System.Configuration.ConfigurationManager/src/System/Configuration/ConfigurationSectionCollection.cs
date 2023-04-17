// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

namespace System.Configuration
{
    public sealed class ConfigurationSectionCollection : NameObjectCollectionBase
    {
        private readonly ConfigurationSectionGroup _configSectionGroup;
        private MgmtConfigurationRecord _configRecord;

        internal ConfigurationSectionCollection(MgmtConfigurationRecord configRecord,
            ConfigurationSectionGroup configSectionGroup) :
            base(StringComparer.Ordinal)
        {
            _configRecord = configRecord;
            _configSectionGroup = configSectionGroup;

            foreach (DictionaryEntry de in _configRecord.SectionFactories)
            {
                FactoryId factoryId = (FactoryId)de.Value;
                if (factoryId.Group == _configSectionGroup.SectionGroupName) BaseAdd(factoryId.Name, factoryId.Name);
            }
        }

        public ConfigurationSection this[string name] => Get(name);

        public ConfigurationSection this[int index] => Get(index);

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

        // Add a new section to the collection. This will result in a new declaration and definition.
        // It is an error if the section already exists.
        public void Add(string name, ConfigurationSection section)
        {
            VerifyIsAttachedToConfigRecord();

            _configRecord.AddConfigurationSection(_configSectionGroup.SectionGroupName, name, section);
            BaseAdd(name, name);
        }

        public void Clear()
        {
            VerifyIsAttachedToConfigRecord();

            string[] allKeys = BaseGetAllKeys();
            foreach (string key in allKeys) Remove(key);
        }

        public void CopyTo(ConfigurationSection[] array, int index)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            int c = Count;
            if (array.Length < c + index) throw new ArgumentOutOfRangeException(nameof(index));

            for (int i = 0, j = index; i < c; i++, j++) array[j] = Get(i);
        }

        public ConfigurationSection Get(int index)
        {
            return Get(GetKey(index));
        }

        public ConfigurationSection Get(string name)
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

            // get the section from the config record
            string configKey = BaseConfigurationRecord.CombineConfigKey(_configSectionGroup.SectionGroupName, name);
            return (ConfigurationSection)_configRecord.GetSection(configKey);
        }

        // Get an enumerator
        public override IEnumerator GetEnumerator()
        {
            int c = Count;
            for (int i = 0; i < c; i++) yield return this[i];
        }

        // Get the string key at a given index.
        public string GetKey(int index)
        {
            return BaseGetKey(index);
        }

        // Remove the declaration and definition of a section in this config file, including any
        // location sections in the file.
        //
        // Note that if the section is declared in a parent, we still remove the declaration and
        // definition, and the instance of ConfigurationSection will be detached from the collection.
        // However, the collection will still have a ConfigurationSection of that name in the collection,
        // only it will have the value of the immediate parent.
        public void Remove(string name)
        {
            VerifyIsAttachedToConfigRecord();

            // Remove the factory and section from this record, so that when config is written,
            // it will contain neither a declaration or definition.
            _configRecord.RemoveConfigurationSection(_configSectionGroup.SectionGroupName, name);

            // Remove the section from the collection if it is no longer in the list of all SectionFactories.
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
