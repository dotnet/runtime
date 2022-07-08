//------------------------------------------------------------------------------
// <copyright file="ListenerElementsCollection.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------ 
using System.Configuration;
using System;
using System.Reflection;
using System.Globalization;
using System.Xml;
using System.Collections.Specialized;
using System.Collections;
using System.Security;
using System.Security.Permissions;

namespace System.Diagnostics {
    [ConfigurationCollection(typeof(ListenerElement))]
    internal class ListenerElementsCollection : ConfigurationElementCollection {

        new public ListenerElement this[string name] {
            get {
                return (ListenerElement) BaseGet(name);
            }
        }

        public override ConfigurationElementCollectionType CollectionType {
            get {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new ListenerElement(true);
        }

        protected override Object GetElementKey(ConfigurationElement element) {
            return ((ListenerElement) element).Name;
        }

        public TraceListenerCollection GetRuntimeObject() {
            TraceListenerCollection listeners = new TraceListenerCollection();
            bool _isDemanded = false;

            foreach(ListenerElement element in this) {
                
                // At some point, we need to pull out adding/removing the 'default' DefaultTraceListener  
                // code from here in favor of adding/not-adding after we load the config (in TraceSource 
                // and in static Trace) 
                
                if (!_isDemanded && !element._isAddedByDefault) {
                    // Do a full damand; This will disable partially trusted code from hooking up listeners
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                    _isDemanded = true;
                }



                listeners.Add(element.GetRuntimeObject());
            }

            return listeners;
        }

        protected override void InitializeDefault() {
            InitializeDefaultInternal();
        }
        
        internal void InitializeDefaultInternal() {
            ListenerElement defaultListener = new ListenerElement(false);
            defaultListener.Name = "Default";
            defaultListener.TypeName = typeof(DefaultTraceListener).FullName;
            defaultListener._isAddedByDefault = true;

            this.BaseAdd(defaultListener);
        }
        
        protected override void BaseAdd(ConfigurationElement element) {
            ListenerElement listenerElement = element as ListenerElement;
            
            Debug.Assert((listenerElement != null), "adding elements other than ListenerElement to ListenerElementsCollection?");
            
            if (listenerElement.Name.Equals("Default") && listenerElement.TypeName.Equals(typeof(DefaultTraceListener).FullName))
                BaseAdd(listenerElement, false);
            else 
                BaseAdd(listenerElement, ThrowOnDuplicate);
        }
    }

    // This is the collection used by the sharedListener section.  It is only slightly different from ListenerElementsCollection.
    // The differences are that it does not allow remove and clear, and that the ListenerElements it creates do not allow 
    // references. 
    [ConfigurationCollection(typeof(ListenerElement), AddItemName = "add",
     CollectionType = ConfigurationElementCollectionType.BasicMap)]    
    internal class SharedListenerElementsCollection : ListenerElementsCollection {

        public override ConfigurationElementCollectionType CollectionType {
            get {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new ListenerElement(false);
        }
        
        protected override string ElementName {
            get {
                return "add";
            }
        }
    }

    internal class ListenerElement : TypedElement {
        private static readonly ConfigurationProperty _propFilter = new ConfigurationProperty("filter", typeof(FilterElement), null, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty _propName = new ConfigurationProperty("name", typeof(string), null, ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
        private static readonly ConfigurationProperty _propOutputOpts = new ConfigurationProperty("traceOutputOptions", typeof(TraceOptions), TraceOptions.None, ConfigurationPropertyOptions.None);

        private ConfigurationProperty _propListenerTypeName;
        private bool _allowReferences;
        private Hashtable _attributes;
        internal bool _isAddedByDefault;

        static ListenerElement()   {
        }

        public ListenerElement(bool allowReferences) : base(typeof(TraceListener)) {
            _allowReferences = allowReferences;

            ConfigurationPropertyOptions flags = ConfigurationPropertyOptions.None;
            if (!_allowReferences)
                flags |= ConfigurationPropertyOptions.IsRequired;
            
            _propListenerTypeName = new ConfigurationProperty("type", typeof(string), null, flags);

            _properties.Remove("type");
            _properties.Add(_propListenerTypeName);
            _properties.Add(_propFilter);
            _properties.Add(_propName);
            _properties.Add(_propOutputOpts);
        }

        public Hashtable Attributes {
            get {
                if (_attributes == null)
                    _attributes = new Hashtable(StringComparer.OrdinalIgnoreCase);
                return _attributes;
            }
        }

        [ConfigurationProperty("filter")]
        public FilterElement Filter {
            get {
                return (FilterElement) this[_propFilter];
            }
        }

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name {
            get { 
                return (string) this[_propName]; 
            }
            set {
                this[_propName] = value;
            }
        }

        [ConfigurationProperty("traceOutputOptions", DefaultValue = (TraceOptions) TraceOptions.None)]
        public TraceOptions TraceOutputOptions {
            get { 
                return (TraceOptions) this[_propOutputOpts]; 
            }
            // This is useful when the OM becomes public. In the meantime, this can be utilized via reflection
            set {
                this[_propOutputOpts] = value;
            }

        }
        
        [ConfigurationProperty("type")]
        public override string TypeName {
            get { 
                return (string) this[_propListenerTypeName]; 
            }
            set {
                this[_propListenerTypeName] = value;
            }
        }

        public override bool Equals(object compareTo) {
            if (this.Name.Equals("Default") && this.TypeName.Equals(typeof(DefaultTraceListener).FullName)) {
                // This is a workaround to treat all DefaultTraceListener named 'Default' the same. 
                // This is needed for the Config.Save to work properly as otherwise config base layers 
                // above us would run into duplicate 'Default' listener element and perceive it as
                // error. 
                ListenerElement compareToElem = compareTo as ListenerElement;
                return (compareToElem != null) && compareToElem.Name.Equals("Default") 
                        && compareToElem.TypeName.Equals(typeof(DefaultTraceListener).FullName);
            }
            else 
                return base.Equals(compareTo);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
        
        public TraceListener GetRuntimeObject() {
            if (_runtimeObject != null)
                return (TraceListener) _runtimeObject;

            try {
                string className = TypeName;
                if (String.IsNullOrEmpty(className)) {
                    // Look it up in SharedListeners
                    Debug.Assert(_allowReferences, "_allowReferences must be true if type name is null");

                    if (_attributes != null || ElementInformation.Properties[_propFilter.Name].ValueOrigin == PropertyValueOrigin.SetHere || TraceOutputOptions != TraceOptions.None || !String.IsNullOrEmpty(InitData))
                        throw new ConfigurationErrorsException(SR.GetString(SR.Reference_listener_cant_have_properties, Name));
                        
                    if (DiagnosticsConfiguration.SharedListeners == null)
                        throw new ConfigurationErrorsException(SR.GetString(SR.Reference_to_nonexistent_listener, Name));
                    
                    ListenerElement sharedListener = DiagnosticsConfiguration.SharedListeners[Name];
                    if (sharedListener == null)
                        throw new ConfigurationErrorsException(SR.GetString(SR.Reference_to_nonexistent_listener, Name));
                    else {
                        _runtimeObject = sharedListener.GetRuntimeObject();
                        return (TraceListener) _runtimeObject;
                    }
                }
                else {
                    // create a new one
                    TraceListener newListener = (TraceListener) BaseGetRuntimeObject();
                    newListener.initializeData = InitData;
                    newListener.Name = Name;
                    newListener.SetAttributes(Attributes);
                    newListener.TraceOutputOptions = TraceOutputOptions;

                    if ((Filter != null) && (Filter.TypeName != null) && (Filter.TypeName.Length != 0)) {
                        newListener.Filter = Filter.GetRuntimeObject();
                        XmlWriterTraceListener listerAsXmlWriter = newListener as XmlWriterTraceListener;
                        if (listerAsXmlWriter != null) {
                            // This filter was added via configuration, which means we want the listener
                            // to respect it for TraceTransfer events.
                            listerAsXmlWriter.shouldRespectFilterOnTraceTransfer = true;
                        }
                    }

                    _runtimeObject = newListener;
                    return newListener;
                }
            }
            catch (ArgumentException e) {
                throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_create_listener, Name), e);
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
            ListenerElement le = sourceElement as ListenerElement; 
            if ((le != null) && (le._attributes != null)) 
                this._attributes = le._attributes;  
        }

        internal void ResetProperties() 
        {
            // blow away any UnrecognizedAttributes that we have deserialized earlier 
            if (_attributes != null) {
                _attributes.Clear();
                _properties.Clear();
                _properties.Add(_propListenerTypeName);
                _properties.Add(_propFilter);
                _properties.Add(_propName);
                _properties.Add(_propOutputOpts);
            }
        }

        internal TraceListener RefreshRuntimeObject(TraceListener listener) {
            _runtimeObject = null;
            try {
                string className = TypeName;
                if (String.IsNullOrEmpty(className)) {
                    // Look it up in SharedListeners and ask the sharedListener to refresh.
                    Debug.Assert(_allowReferences, "_allowReferences must be true if type name is null");

                    if (_attributes != null || ElementInformation.Properties[_propFilter.Name].ValueOrigin == PropertyValueOrigin.SetHere || TraceOutputOptions != TraceOptions.None || !String.IsNullOrEmpty(InitData))
                        throw new ConfigurationErrorsException(SR.GetString(SR.Reference_listener_cant_have_properties, Name));

                    if (DiagnosticsConfiguration.SharedListeners == null)
                        throw new ConfigurationErrorsException(SR.GetString(SR.Reference_to_nonexistent_listener, Name));
                    
                    ListenerElement sharedListener = DiagnosticsConfiguration.SharedListeners[Name];
                    if (sharedListener == null)
                        throw new ConfigurationErrorsException(SR.GetString(SR.Reference_to_nonexistent_listener, Name));
                    else {
                        _runtimeObject = sharedListener.RefreshRuntimeObject(listener);
                        return (TraceListener) _runtimeObject;
                    }
                }
                else {
                    // We're the element with the type and initializeData info.  First see if those two are the same as they were.
                    // If not, create a whole new object, otherwise, just update the other properties.
                    if (Type.GetType(className) != listener.GetType() || InitData != listener.initializeData) {
                        // type or initdata changed
                        return GetRuntimeObject();
                    }
                    else {
                        listener.SetAttributes(Attributes);
                        listener.TraceOutputOptions = TraceOutputOptions;
                   
                        if (listener.Filter != null ) {
                            if (ElementInformation.Properties[_propFilter.Name].ValueOrigin == PropertyValueOrigin.SetHere ||
                                ElementInformation.Properties[_propFilter.Name].ValueOrigin == PropertyValueOrigin.Inherited)
                                    listener.Filter = Filter.RefreshRuntimeObject(listener.Filter);
                            else
                                listener.Filter = null;
                        }

                        _runtimeObject = listener;
                        return listener;
                    }
                }
            }
            catch (ArgumentException e) {
                throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_create_listener, Name), e);
            }
            
        }
        
    }
}

