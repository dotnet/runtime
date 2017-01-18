// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;

namespace System.Configuration
{
    public sealed class ProviderSettings : ConfigurationElement
    {
        private readonly ConfigurationPropertyCollection _properties;

        private readonly ConfigurationProperty _propName =
            new ConfigurationProperty("name",
                typeof(string),
                null, // no reasonable default
                null, // use default converter
                ConfigurationProperty.s_nonEmptyStringValidator,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);

        private readonly ConfigurationProperty _propType = new ConfigurationProperty("type", typeof(string), "",
            ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsTypeStringTransformationRequired);

        private NameValueCollection _propertyNameCollection;

        public ProviderSettings()
        {
            _properties = new ConfigurationPropertyCollection { _propName, _propType };
            _propertyNameCollection = null;
        }

        public ProviderSettings(string name, string type) : this()
        {
            Name = name;
            Type = type;
        }

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                UpdatePropertyCollection();
                return _properties;
            }
        }

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)base[_propName]; }
            set { base[_propName] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get { return (string)base[_propType]; }
            set { base[_propType] = value; }
        }

        public NameValueCollection Parameters
        {
            get
            {
                if (_propertyNameCollection != null) return _propertyNameCollection;

                lock (this)
                {
                    _propertyNameCollection = new NameValueCollection(StringComparer.Ordinal);

                    foreach (object de in _properties)
                    {
                        ConfigurationProperty prop = (ConfigurationProperty)de;
                        if ((prop.Name != "name") && (prop.Name != "type"))
                            _propertyNameCollection.Add(prop.Name, (string)base[prop]);
                    }
                }
                return _propertyNameCollection;
            }
        }

        protected internal override void Unmerge(ConfigurationElement sourceElement,
            ConfigurationElement parentElement,
            ConfigurationSaveMode saveMode)
        {
            ProviderSettings parentProviders = parentElement as ProviderSettings;
            parentProviders?.UpdatePropertyCollection(); // before reseting make sure the bag is filled in

            ProviderSettings sourceProviders = sourceElement as ProviderSettings;
            sourceProviders?.UpdatePropertyCollection(); // before reseting make sure the bag is filled in

            base.Unmerge(sourceElement, parentElement, saveMode);
            UpdatePropertyCollection();
        }

        protected internal override void Reset(ConfigurationElement parentElement)
        {
            ProviderSettings parentProviders = parentElement as ProviderSettings;
            parentProviders?.UpdatePropertyCollection(); // before reseting make sure the bag is filled in

            base.Reset(parentElement);
        }

        internal bool UpdatePropertyCollection()
        {
            bool bIsModified = false;
            ArrayList removeList = null;

            if (_propertyNameCollection != null)
            {
                // remove any data that has been delete from the collection
                foreach (ConfigurationProperty prop in _properties)
                    if ((prop.Name != "name") && (prop.Name != "type"))
                    {
                        if (_propertyNameCollection.Get(prop.Name) != null) continue;
                        if (removeList == null)
                            removeList = new ArrayList();

                        if ((Values.GetConfigValue(prop.Name).ValueFlags & ConfigurationValueFlags.Locked) != 0)
                            continue;
                        removeList.Add(prop.Name);
                        bIsModified = true;
                    }

                if (removeList != null)
                {
                    foreach (string propName in removeList)
                        _properties.Remove(propName);
                }

                // then copy any data that has been changed in the collection
                foreach (string key in _propertyNameCollection)
                {
                    string valueInCollection = _propertyNameCollection[key];
                    string valueInBag = GetProperty(key);

                    if ((valueInBag == null) || (valueInCollection != valueInBag)) // add new property
                    {
                        SetProperty(key, valueInCollection);
                        bIsModified = true;
                    }
                }
            }
            _propertyNameCollection = null;
            return bIsModified;
        }

        protected internal override bool IsModified()
        {
            return UpdatePropertyCollection() || base.IsModified();
        }


        private string GetProperty(string propName)
        {
            if (_properties.Contains(propName))
            {
                ConfigurationProperty prop = _properties[propName];
                if (prop != null)
                    return (string)base[prop];
            }
            return null;
        }

        private void SetProperty(string propName, string value)
        {
            ConfigurationProperty setPropName;
            if (_properties.Contains(propName))
                setPropName = _properties[propName];
            else
            {
                setPropName = new ConfigurationProperty(propName, typeof(string), null);
                _properties.Add(setPropName);
            }

            if (setPropName == null) return;
            base[setPropName] = value;
        }

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            ConfigurationProperty propName = new ConfigurationProperty(name, typeof(string), value);
            _properties.Add(propName);
            base[propName] = value; // Add them to the property bag
            Parameters[name] = value;
            return true;
        }
    }
}