// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Xml;

namespace System.Diagnostics
{
    [ConfigurationCollection(typeof(SourceElement),
        AddItemName = "source",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    internal sealed class SourceElementsCollection : ConfigurationElementCollection
    {
        public new SourceElement this[string name] => (SourceElement)BaseGet(name);

        protected override string ElementName => "source";

        public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

        protected override ConfigurationElement CreateNewElement()
        {
            SourceElement se = new SourceElement();
            se.Listeners.InitializeDefaultInternal();
            return se;
        }

        protected override object GetElementKey(ConfigurationElement element) => ((SourceElement)element).Name;
    }


    internal sealed class SourceElement : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection _properties = new();
        private static readonly ConfigurationProperty _propName = new("name", typeof(string), "", ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationProperty _propSwitchName = new("switchName", typeof(string), null, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSwitchValue = new("switchValue", typeof(string), null, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propSwitchType = new("switchType", typeof(string), null, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propListeners = new("listeners", typeof(ListenerElementsCollection), new ListenerElementsCollection(), ConfigurationPropertyOptions.None);

        private StringDictionary _attributes;

        static SourceElement()
        {
            _properties.Add(_propName);
            _properties.Add(_propSwitchName);
            _properties.Add(_propSwitchValue);
            _properties.Add(_propSwitchType);
            _properties.Add(_propListeners);
        }

        public StringDictionary Attributes => _attributes ??= new StringDictionary();

        [ConfigurationProperty("listeners")]
        public ListenerElementsCollection Listeners => (ListenerElementsCollection)this[_propListeners];

        [ConfigurationProperty("name", IsRequired = true, DefaultValue = "")]
        public string Name => (string)this[_propName];

        protected internal override ConfigurationPropertyCollection Properties => _properties;

        [ConfigurationProperty("switchName")]
        public string SwitchName => (string)this[_propSwitchName];

        [ConfigurationProperty("switchValue")]
        public string SwitchValue => (string)this[_propSwitchValue];

        [ConfigurationProperty("switchType")]
        public string SwitchType => (string)this[_propSwitchType];

        protected internal override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            base.DeserializeElement(reader, serializeCollectionKey);

            if (!string.IsNullOrEmpty(SwitchName) && !string.IsNullOrEmpty(SwitchValue))
                throw new ConfigurationErrorsException(SR.Format(SR.Only_specify_one, Name));
        }

        // Our optional attributes implementation is little convoluted as there is
        // no such first class mechanism from the config system. We basically cache
        // any "unrecognized" attribute here and serialize it out later.
        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            Attributes.Add(name, value);
            return true;
        }

        // We need to serialize optional attributes here, a better place would have
        // been inside SerializeElement but the base class implementation from
        // ConfigurationElement doesn't take into account for derived class doing
        // extended serialization, it basically writes out child element that
        // forces the element closing syntax, so any attribute serialization needs
        // to happen before normal element serialization from ConfigurationElement.
        // This means we would write out custom attributes ahead of normal ones.
        // The other alternative would be to re-implement the entire routine here
        // which is an overkill and a maintenance issue.
        protected override void PreSerialize(XmlWriter writer)
        {
            if (_attributes != null)
            {
                IDictionaryEnumerator e = (IDictionaryEnumerator)_attributes.GetEnumerator();
                while (e.MoveNext())
                {
                    string xmlValue = (string)e.Value;
                    string xmlName = (string)e.Key;

                    if ((xmlValue != null) && (writer != null))
                    {
                        writer.WriteAttributeString(xmlName, xmlValue);
                    }
                }
            }
        }

        // Account for optional attributes from custom listeners.
        protected internal override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
        {
            bool DataToWrite = base.SerializeElement(writer, serializeCollectionKey);
            DataToWrite = DataToWrite || ((_attributes != null) && (_attributes.Count > 0));
            return DataToWrite;
        }

        protected internal override void Unmerge(ConfigurationElement sourceElement,
                                                 ConfigurationElement parentElement,
                                                 ConfigurationSaveMode saveMode)
        {
            base.Unmerge(sourceElement, parentElement, saveMode);

            // Unmerge the optional attributes cache as well
            SourceElement le = sourceElement as SourceElement;
            if ((le != null) && (le._attributes != null))
                this._attributes = le._attributes;
        }

        internal void ResetProperties()
        {
            // Blow away any UnrecognizedAttributes that we have deserialized earlier.
            if (_attributes != null)
            {
                _attributes.Clear();
                _properties.Clear();
                _properties.Add(_propName);
                _properties.Add(_propSwitchName);
                _properties.Add(_propSwitchValue);
                _properties.Add(_propSwitchType);
                _properties.Add(_propListeners);
            }
        }
    }
}
