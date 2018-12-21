// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.IO;
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;
    using System.Threading;
    using System.Diagnostics;
    using System.Collections.Generic;

    [XmlRoot("schema", Namespace = XmlSchema.Namespace)]
    public class XmlSchema : XmlSchemaObject
    {
        public const string Namespace = XmlReservedNs.NsXs;
        public const string InstanceNamespace = XmlReservedNs.NsXsi;

        private XmlSchemaForm _attributeFormDefault = XmlSchemaForm.None;
        private XmlSchemaForm _elementFormDefault = XmlSchemaForm.None;
        private XmlSchemaDerivationMethod _blockDefault = XmlSchemaDerivationMethod.None;
        private XmlSchemaDerivationMethod _finalDefault = XmlSchemaDerivationMethod.None;
        private string _targetNs;
        private string _version;
        private XmlSchemaObjectCollection _includes = new XmlSchemaObjectCollection();
        private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();
        private string _id;
        private XmlAttribute[] _moreAttributes;

        // compiled info
        private bool _isCompiled = false;
        private bool _isCompiledBySet = false;
        private bool _isPreprocessed = false;
        private bool _isRedefined = false;
        private int _errorCount = 0;
        private XmlSchemaObjectTable _attributes;
        private XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _elements = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _types = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _notations = new XmlSchemaObjectTable();
        private XmlSchemaObjectTable _identityConstraints = new XmlSchemaObjectTable();

        private static int s_globalIdCounter = -1;
        private ArrayList _importedSchemas;
        private ArrayList _importedNamespaces;

        private int _schemaId = -1; //Not added to a set
        private Uri _baseUri;
        private bool _isChameleon;
        private Hashtable _ids = new Hashtable();
        private XmlDocument _document;
        private XmlNameTable _nameTable;

        public XmlSchema() { }

        public static XmlSchema Read(TextReader reader, ValidationEventHandler validationEventHandler)
        {
            return Read(new XmlTextReader(reader), validationEventHandler);
        }

        public static XmlSchema Read(Stream stream, ValidationEventHandler validationEventHandler)
        {
            return Read(new XmlTextReader(stream), validationEventHandler);
        }

        public static XmlSchema Read(XmlReader reader, ValidationEventHandler validationEventHandler)
        {
            XmlNameTable nameTable = reader.NameTable;
            Parser parser = new Parser(SchemaType.XSD, nameTable, new SchemaNames(nameTable), validationEventHandler);
            try
            {
                parser.Parse(reader, null);
            }
            catch (XmlSchemaException e)
            {
                if (validationEventHandler != null)
                {
                    validationEventHandler(null, new ValidationEventArgs(e));
                }
                else
                {
                    throw;
                }
                return null;
            }
            return parser.XmlSchema;
        }

        public void Write(Stream stream)
        {
            Write(stream, null);
        }

        public void Write(Stream stream, XmlNamespaceManager namespaceManager)
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(stream, null);
            xmlWriter.Formatting = Formatting.Indented;
            Write(xmlWriter, namespaceManager);
        }

        public void Write(TextWriter writer)
        {
            Write(writer, null);
        }

        public void Write(TextWriter writer, XmlNamespaceManager namespaceManager)
        {
            XmlTextWriter xmlWriter = new XmlTextWriter(writer);
            xmlWriter.Formatting = Formatting.Indented;
            Write(xmlWriter, namespaceManager);
        }

        public void Write(XmlWriter writer)
        {
            Write(writer, null);
        }

        public void Write(XmlWriter writer, XmlNamespaceManager namespaceManager)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlSchema));
            XmlSerializerNamespaces ns;

            if (namespaceManager != null)
            {
                ns = new XmlSerializerNamespaces();
                bool ignoreXS = false;
                if (this.Namespaces != null)
                { //User may have set both nsManager and Namespaces property on the XmlSchema object
                    ignoreXS = this.Namespaces.Namespaces.ContainsKey("xs") || this.Namespaces.Namespaces.ContainsValue(XmlReservedNs.NsXs);
                }
                if (!ignoreXS && namespaceManager.LookupPrefix(XmlReservedNs.NsXs) == null &&
                    namespaceManager.LookupNamespace("xs") == null)
                {
                    ns.Add("xs", XmlReservedNs.NsXs);
                }
                foreach (string prefix in namespaceManager)
                {
                    if (prefix != "xml" && prefix != "xmlns")
                    {
                        ns.Add(prefix, namespaceManager.LookupNamespace(prefix));
                    }
                }
            }
            else if (this.Namespaces != null && this.Namespaces.Count > 0)
            {
                Dictionary<string, string> serializerNS = this.Namespaces.Namespaces;
                if (!serializerNS.ContainsKey("xs") && !serializerNS.ContainsValue(XmlReservedNs.NsXs))
                { //Prefix xs not defined AND schema namespace not already mapped to a prefix
                    serializerNS.Add("xs", XmlReservedNs.NsXs);
                }
                ns = this.Namespaces;
            }
            else
            {
                ns = new XmlSerializerNamespaces();
                ns.Add("xs", XmlSchema.Namespace);
                if (_targetNs != null && _targetNs.Length != 0)
                {
                    ns.Add("tns", _targetNs);
                }
            }
            serializer.Serialize(writer, this, ns);
        }

        [Obsolete("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void Compile(ValidationEventHandler validationEventHandler)
        {
            SchemaInfo sInfo = new SchemaInfo();
            sInfo.SchemaType = SchemaType.XSD;
            CompileSchema(null, null, sInfo, null, validationEventHandler, NameTable, false);
        }

        [Obsolete("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. https://go.microsoft.com/fwlink/?linkid=14202")]
        public void Compile(ValidationEventHandler validationEventHandler, XmlResolver resolver)
        {
            SchemaInfo sInfo = new SchemaInfo();
            sInfo.SchemaType = SchemaType.XSD;
            CompileSchema(null, resolver, sInfo, null, validationEventHandler, NameTable, false);
        }

#pragma warning disable 618
        internal bool CompileSchema(XmlSchemaCollection xsc, XmlResolver resolver, SchemaInfo schemaInfo, string ns, ValidationEventHandler validationEventHandler, XmlNameTable nameTable, bool CompileContentModel)
        {
            //Need to lock here to prevent multi-threading problems when same schema is added to set and compiled
            lock (this)
            {
                //Preprocessing
                SchemaCollectionPreprocessor prep = new SchemaCollectionPreprocessor(nameTable, null, validationEventHandler);
                prep.XmlResolver = resolver;
                if (!prep.Execute(this, ns, true, xsc))
                {
                    return false;
                }

                //Compilation
                SchemaCollectionCompiler compiler = new SchemaCollectionCompiler(nameTable, validationEventHandler);
                _isCompiled = compiler.Execute(this, schemaInfo, CompileContentModel);
                this.SetIsCompiled(_isCompiled);
                return _isCompiled;
            }
        }
#pragma warning restore 618

        internal void CompileSchemaInSet(XmlNameTable nameTable, ValidationEventHandler eventHandler, XmlSchemaCompilationSettings compilationSettings)
        {
            Debug.Assert(_isPreprocessed);
            Compiler setCompiler = new Compiler(nameTable, eventHandler, null, compilationSettings);
            setCompiler.Prepare(this, true);
            _isCompiledBySet = setCompiler.Compile();
        }

        [XmlAttribute("attributeFormDefault"), DefaultValue(XmlSchemaForm.None)]
        public XmlSchemaForm AttributeFormDefault
        {
            get { return _attributeFormDefault; }
            set { _attributeFormDefault = value; }
        }

        [XmlAttribute("blockDefault"), DefaultValue(XmlSchemaDerivationMethod.None)]
        public XmlSchemaDerivationMethod BlockDefault
        {
            get { return _blockDefault; }
            set { _blockDefault = value; }
        }

        [XmlAttribute("finalDefault"), DefaultValue(XmlSchemaDerivationMethod.None)]
        public XmlSchemaDerivationMethod FinalDefault
        {
            get { return _finalDefault; }
            set { _finalDefault = value; }
        }

        [XmlAttribute("elementFormDefault"), DefaultValue(XmlSchemaForm.None)]
        public XmlSchemaForm ElementFormDefault
        {
            get { return _elementFormDefault; }
            set { _elementFormDefault = value; }
        }

        [XmlAttribute("targetNamespace", DataType = "anyURI")]
        public string TargetNamespace
        {
            get { return _targetNs; }
            set { _targetNs = value; }
        }

        [XmlAttribute("version", DataType = "token")]
        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        [XmlElement("include", typeof(XmlSchemaInclude)),
         XmlElement("import", typeof(XmlSchemaImport)),
         XmlElement("redefine", typeof(XmlSchemaRedefine))]
        public XmlSchemaObjectCollection Includes
        {
            get { return _includes; }
        }

        [XmlElement("annotation", typeof(XmlSchemaAnnotation)),
         XmlElement("attribute", typeof(XmlSchemaAttribute)),
         XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroup)),
         XmlElement("complexType", typeof(XmlSchemaComplexType)),
         XmlElement("simpleType", typeof(XmlSchemaSimpleType)),
         XmlElement("element", typeof(XmlSchemaElement)),
         XmlElement("group", typeof(XmlSchemaGroup)),
         XmlElement("notation", typeof(XmlSchemaNotation))]
        public XmlSchemaObjectCollection Items
        {
            get { return _items; }
        }

        // Compiled info
        [XmlIgnore]
        public bool IsCompiled
        {
            get
            {
                return _isCompiled || _isCompiledBySet;
            }
        }

        [XmlIgnore]
        internal bool IsCompiledBySet
        {
            get { return _isCompiledBySet; }
            set { _isCompiledBySet = value; }
        }

        [XmlIgnore]
        internal bool IsPreprocessed
        {
            get { return _isPreprocessed; }
            set { _isPreprocessed = value; }
        }

        [XmlIgnore]
        internal bool IsRedefined
        {
            get { return _isRedefined; }
            set { _isRedefined = value; }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new XmlSchemaObjectTable();
                }
                return _attributes;
            }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable AttributeGroups
        {
            get
            {
                if (_attributeGroups == null)
                {
                    _attributeGroups = new XmlSchemaObjectTable();
                }
                return _attributeGroups;
            }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable SchemaTypes
        {
            get
            {
                if (_types == null)
                {
                    _types = new XmlSchemaObjectTable();
                }
                return _types;
            }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable Elements
        {
            get
            {
                if (_elements == null)
                {
                    _elements = new XmlSchemaObjectTable();
                }
                return _elements;
            }
        }

        [XmlAttribute("id", DataType = "ID")]
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        [XmlAnyAttribute]
        public XmlAttribute[] UnhandledAttributes
        {
            get { return _moreAttributes; }
            set { _moreAttributes = value; }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable Groups
        {
            get { return _groups; }
        }

        [XmlIgnore]
        public XmlSchemaObjectTable Notations
        {
            get { return _notations; }
        }

        [XmlIgnore]
        internal XmlSchemaObjectTable IdentityConstraints
        {
            get { return _identityConstraints; }
        }

        [XmlIgnore]
        internal Uri BaseUri
        {
            get { return _baseUri; }
            set
            {
                _baseUri = value;
            }
        }

        [XmlIgnore]
        // Please be careful with this property. Since it lazy initialized and its value depends on a global state
        //   if it gets called on multiple schemas in a different order the schemas will end up with different IDs
        //   Unfortunately the IDs are used to sort the schemas in the schema set and thus changing the IDs might change
        //   the order which would be a breaking change!!
        // Simply put if you are planning to add or remove a call to this getter you need to be extra carefull
        //   or better don't do it at all.
        internal int SchemaId
        {
            get
            {
                if (_schemaId == -1)
                {
                    _schemaId = Interlocked.Increment(ref s_globalIdCounter);
                }
                return _schemaId;
            }
        }

        [XmlIgnore]
        internal bool IsChameleon
        {
            get { return _isChameleon; }
            set { _isChameleon = value; }
        }

        [XmlIgnore]
        internal Hashtable Ids
        {
            get { return _ids; }
        }

        [XmlIgnore]
        internal XmlDocument Document
        {
            get { if (_document == null) _document = new XmlDocument(); return _document; }
        }

        [XmlIgnore]
        internal int ErrorCount
        {
            get { return _errorCount; }
            set { _errorCount = value; }
        }

        internal new XmlSchema Clone()
        {
            XmlSchema that = new XmlSchema();
            that._attributeFormDefault = _attributeFormDefault;
            that._elementFormDefault = _elementFormDefault;
            that._blockDefault = _blockDefault;
            that._finalDefault = _finalDefault;
            that._targetNs = _targetNs;
            that._version = _version;
            that._includes = _includes;

            that.Namespaces = this.Namespaces;
            that._items = _items;
            that.BaseUri = this.BaseUri;

            SchemaCollectionCompiler.Cleanup(that);
            return that;
        }

        internal XmlSchema DeepClone()
        {
            XmlSchema that = new XmlSchema();
            that._attributeFormDefault = _attributeFormDefault;
            that._elementFormDefault = _elementFormDefault;
            that._blockDefault = _blockDefault;
            that._finalDefault = _finalDefault;
            that._targetNs = _targetNs;
            that._version = _version;
            that._isPreprocessed = _isPreprocessed;
            //that.IsProcessing           = this.IsProcessing; //Not sure if this is needed

            //Clone its Items
            for (int i = 0; i < _items.Count; ++i)
            {
                XmlSchemaObject newItem;

                XmlSchemaComplexType complexType;
                XmlSchemaElement element;
                XmlSchemaGroup group;

                if ((complexType = _items[i] as XmlSchemaComplexType) != null)
                {
                    newItem = complexType.Clone(this);
                }
                else if ((element = _items[i] as XmlSchemaElement) != null)
                {
                    newItem = element.Clone(this);
                }
                else if ((group = _items[i] as XmlSchemaGroup) != null)
                {
                    newItem = group.Clone(this);
                }
                else
                {
                    newItem = _items[i].Clone();
                }
                that.Items.Add(newItem);
            }

            //Clone Includes
            for (int i = 0; i < _includes.Count; ++i)
            {
                XmlSchemaExternal newInclude = (XmlSchemaExternal)_includes[i].Clone();
                that.Includes.Add(newInclude);
            }
            that.Namespaces = this.Namespaces;
            //that.includes               = this.includes; //Need to verify this is OK for redefines
            that.BaseUri = this.BaseUri;
            return that;
        }

        [XmlIgnore]
        internal override string IdAttribute
        {
            get { return Id; }
            set { Id = value; }
        }

        internal void SetIsCompiled(bool isCompiled)
        {
            _isCompiled = isCompiled;
        }

        internal override void SetUnhandledAttributes(XmlAttribute[] moreAttributes)
        {
            _moreAttributes = moreAttributes;
        }
        internal override void AddAnnotation(XmlSchemaAnnotation annotation)
        {
            _items.Add(annotation);
        }

        internal XmlNameTable NameTable
        {
            get { if (_nameTable == null) _nameTable = new System.Xml.NameTable(); return _nameTable; }
        }

        internal ArrayList ImportedSchemas
        {
            get
            {
                if (_importedSchemas == null)
                {
                    _importedSchemas = new ArrayList();
                }
                return _importedSchemas;
            }
        }

        internal ArrayList ImportedNamespaces
        {
            get
            {
                if (_importedNamespaces == null)
                {
                    _importedNamespaces = new ArrayList();
                }
                return _importedNamespaces;
            }
        }

        internal void GetExternalSchemasList(IList extList, XmlSchema schema)
        {
            Debug.Assert(extList != null && schema != null);
            if (extList.Contains(schema))
            {
                return;
            }
            extList.Add(schema);
            for (int i = 0; i < schema.Includes.Count; ++i)
            {
                XmlSchemaExternal ext = (XmlSchemaExternal)schema.Includes[i];
                if (ext.Schema != null)
                {
                    GetExternalSchemasList(extList, ext.Schema);
                }
            }
        }

#if TRUST_COMPILE_STATE
        internal void AddCompiledInfo(SchemaInfo schemaInfo) {
            XmlQualifiedName itemName;
            foreach (XmlSchemaElement element in elements.Values) {
                itemName = element.QualifiedName;
                schemaInfo.TargetNamespaces[itemName.Namespace] = true;
                if (schemaInfo.ElementDecls[itemName] == null) {
                    schemaInfo.ElementDecls.Add(itemName, element.ElementDecl);
                }
            }
            foreach (XmlSchemaAttribute attribute in attributes.Values) {
                itemName = attribute.QualifiedName;
                schemaInfo.TargetNamespaces[itemName.Namespace] = true;
                if (schemaInfo.ElementDecls[itemName] == null) {
                    schemaInfo.AttributeDecls.Add(itemName, attribute.AttDef);
                }
            }    
            foreach (XmlSchemaType type in types.Values) {
                itemName = type.QualifiedName;
                schemaInfo.TargetNamespaces[itemName.Namespace] = true;
                XmlSchemaComplexType complexType = type as XmlSchemaComplexType;
                if ((complexType == null || type != XmlSchemaComplexType.AnyType) && schemaInfo.ElementDeclsByType[itemName] == null) {
                    schemaInfo.ElementDeclsByType.Add(itemName, type.ElementDecl);
                }
            }
            foreach (XmlSchemaNotation notation in notations.Values) {
                itemName = notation.QualifiedName;
                schemaInfo.TargetNamespaces[itemName.Namespace] = true;
                SchemaNotation no = new SchemaNotation(itemName);
                no.SystemLiteral = notation.System;
                no.Pubid = notation.Public;
                if (schemaInfo.Notations[itemName.Name] == null) {
                    schemaInfo.Notations.Add(itemName.Name, no);
                }
            }
        }
#endif//TRUST_COMPILE_STATE
    }
}
