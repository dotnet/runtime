//------------------------------------------------------------------------------
// <copyright file="SwitchElementsCollection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System.Configuration;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

namespace System.Diagnostics {
    [ConfigurationCollection(typeof(SwitchElement))]
    internal class SwitchElementsCollection : ConfigurationElementCollection {

        new public SwitchElement this[string name] {
            get {
                return (SwitchElement) BaseGet(name);
            }
        }
        
        public override ConfigurationElementCollectionType CollectionType {
            get {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }
    
        protected override ConfigurationElement CreateNewElement() {
            return new SwitchElement();
        }
    
        protected override Object GetElementKey(ConfigurationElement element) {
            return ((SwitchElement) element).Name;
        }
    }
    
    internal class SwitchElement : ConfigurationElement {
        private static readonly ConfigurationPropertyCollection _properties;
        private static readonly ConfigurationProperty _propName = new ConfigurationProperty("name", typeof(string), "", ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
        private static readonly ConfigurationProperty _propValue = new ConfigurationProperty("value", typeof(string), null, ConfigurationPropertyOptions.IsRequired);

        private Hashtable _attributes;

        static SwitchElement(){
            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propName);
            _properties.Add(_propValue);
        }

        public Hashtable Attributes {
            get {
                if (_attributes == null)
                    _attributes = new Hashtable(StringComparer.OrdinalIgnoreCase);
                return _attributes;
            }
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = true)]
        public string Name {
            get { 
                return (string) this[_propName]; 
            }
        }
        
        protected override ConfigurationPropertyCollection Properties {
            get {
                return _properties;
            }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public string Value {
            get { 
                return (string) this[_propValue]; 
            }
        }

        // Our optional attributes implementation is little convoluted as there is 
        // no such firsclass mechanism from the config system. We basically cache 
        // any "unrecognized" attribute here and serialize it out later. 
        protected override bool OnDeserializeUnrecognizedAttribute(String name, String value) {
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
        protected override void PreSerialize(XmlWriter writer) {
            if (_attributes != null) {
                IDictionaryEnumerator e = _attributes.GetEnumerator();
                while (e.MoveNext()) {
                    string xmlValue = (string)e.Value;
                    string xmlName = (string)e.Key;

                    if ((xmlValue != null) && (writer != null)) {
                        writer.WriteAttributeString(xmlName, xmlValue);
                    }
                }
            }
        }

        // Account for optional attributes from custom listeners. 
        protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey) {
            bool DataToWrite = base.SerializeElement(writer, serializeCollectionKey);
            DataToWrite = DataToWrite || ((_attributes != null) && (_attributes.Count > 0));
            return DataToWrite;
        }

        protected override void Unmerge(ConfigurationElement sourceElement,
                                                ConfigurationElement parentElement,
                                                ConfigurationSaveMode saveMode) {
            base.Unmerge(sourceElement, parentElement, saveMode);
            
            // Unmerge the optional attributes cache as well
            SwitchElement le = sourceElement as SwitchElement; 
            if ((le != null) && (le._attributes != null)) 
                this._attributes = le._attributes;  
        }

        internal void ResetProperties() 
        {
            // blow away any UnrecognizedAttributes that we have deserialized earlier 
            if (_attributes != null) {
                _attributes.Clear();
                _properties.Clear();
                _properties.Add(_propName);
                _properties.Add(_propValue);
            }
        }
    }
}

