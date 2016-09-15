// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    public enum ConformanceLevel
    {
        Auto = 0,
        Document = 2,
        Fragment = 1,
    }
    public enum DtdProcessing
    {
        Ignore = 1,
        Parse = 2,
        Prohibit = 0,
    }
    public enum EntityHandling
    {
        ExpandCharEntities = 2,
        ExpandEntities = 1,
    }
    public enum Formatting
    {
        Indented = 1,
        None = 0,
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
    public partial interface IApplicationResourceStreamResolver
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        System.IO.Stream GetApplicationResourceStream(System.Uri relativeUri);
    }
    public partial interface IHasXmlNode
    {
        System.Xml.XmlNode GetNode();
    }
    public partial interface IXmlLineInfo
    {
        int LineNumber { get; }
        int LinePosition { get; }
        bool HasLineInfo();
    }
    public partial interface IXmlNamespaceResolver
    {
        System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope);
        string LookupNamespace(string prefix);
        string LookupPrefix(string namespaceName);
    }
    [System.FlagsAttribute]
    public enum NamespaceHandling
    {
        Default = 0,
        OmitDuplicates = 1,
    }
    public partial class NameTable : System.Xml.XmlNameTable
    {
        public NameTable() { }
        public override string Add(char[] key, int start, int len) { return default(string); }
        public override string Add(string key) { return default(string); }
        public override string Get(char[] key, int start, int len) { return default(string); }
        public override string Get(string value) { return default(string); }
    }
    public enum NewLineHandling
    {
        Entitize = 1,
        None = 2,
        Replace = 0,
    }
    public enum ReadState
    {
        Closed = 4,
        EndOfFile = 3,
        Error = 2,
        Initial = 0,
        Interactive = 1,
    }
    public enum ValidationType
    {
        [System.ObsoleteAttribute("Validation type should be specified as DTD or Schema.")]
        Auto = 1,
        DTD = 2,
        None = 0,
        Schema = 4,
        [System.ObsoleteAttribute("XDR Validation through XmlValidatingReader is obsoleted")]
        XDR = 3,
    }
    public enum WhitespaceHandling
    {
        All = 0,
        None = 2,
        Significant = 1,
    }
    public enum WriteState
    {
        Attribute = 3,
        Closed = 5,
        Content = 4,
        Element = 2,
        Error = 6,
        Prolog = 1,
        Start = 0,
    }
    public partial class XmlAttribute : System.Xml.XmlNode
    {
        protected internal XmlAttribute(string prefix, string localName, string namespaceURI, System.Xml.XmlDocument doc) { }
        public override string BaseURI { get { return default(string); } }
        public override string InnerText { set { } }
        public override string InnerXml { set { } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override string NamespaceURI { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlDocument OwnerDocument { get { return default(System.Xml.XmlDocument); } }
        public virtual System.Xml.XmlElement OwnerElement { get { return default(System.Xml.XmlElement); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override string Prefix { get { return default(string); } set { } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public virtual bool Specified { get { return default(bool); } }
        public override string Value { get { return default(string); } set { } }
        public override System.Xml.XmlNode AppendChild(System.Xml.XmlNode newChild) { return default(System.Xml.XmlNode); }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override System.Xml.XmlNode InsertAfter(System.Xml.XmlNode newChild, System.Xml.XmlNode refChild) { return default(System.Xml.XmlNode); }
        public override System.Xml.XmlNode InsertBefore(System.Xml.XmlNode newChild, System.Xml.XmlNode refChild) { return default(System.Xml.XmlNode); }
        public override System.Xml.XmlNode PrependChild(System.Xml.XmlNode newChild) { return default(System.Xml.XmlNode); }
        public override System.Xml.XmlNode RemoveChild(System.Xml.XmlNode oldChild) { return default(System.Xml.XmlNode); }
        public override System.Xml.XmlNode ReplaceChild(System.Xml.XmlNode newChild, System.Xml.XmlNode oldChild) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public sealed partial class XmlAttributeCollection : System.Xml.XmlNamedNodeMap, System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal XmlAttributeCollection() { }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Xml.XmlAttribute this[int i] { get { return default(System.Xml.XmlAttribute); } }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Xml.XmlAttribute this[string name] { get { return default(System.Xml.XmlAttribute); } }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Xml.XmlAttribute this[string localName, string namespaceURI] { get { return default(System.Xml.XmlAttribute); } }
        int System.Collections.ICollection.Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public System.Xml.XmlAttribute Append(System.Xml.XmlAttribute node) { return default(System.Xml.XmlAttribute); }
        public void CopyTo(System.Xml.XmlAttribute[] array, int index) { }
        public System.Xml.XmlAttribute InsertAfter(System.Xml.XmlAttribute newNode, System.Xml.XmlAttribute refNode) { return default(System.Xml.XmlAttribute); }
        public System.Xml.XmlAttribute InsertBefore(System.Xml.XmlAttribute newNode, System.Xml.XmlAttribute refNode) { return default(System.Xml.XmlAttribute); }
        public System.Xml.XmlAttribute Prepend(System.Xml.XmlAttribute node) { return default(System.Xml.XmlAttribute); }
        public System.Xml.XmlAttribute Remove(System.Xml.XmlAttribute node) { return default(System.Xml.XmlAttribute); }
        public void RemoveAll() { }
        public System.Xml.XmlAttribute RemoveAt(int i) { return default(System.Xml.XmlAttribute); }
        public override System.Xml.XmlNode SetNamedItem(System.Xml.XmlNode node) { return default(System.Xml.XmlNode); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public partial class XmlCDataSection : System.Xml.XmlCharacterData
    {
        protected internal XmlCDataSection(string data, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode PreviousText { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public abstract partial class XmlCharacterData : System.Xml.XmlLinkedNode
    {
        protected internal XmlCharacterData(string data, System.Xml.XmlDocument doc) { }
        public virtual string Data { get { return default(string); } set { } }
        public override string InnerText { get { return default(string); } set { } }
        public virtual int Length { get { return default(int); } }
        public override string Value { get { return default(string); } set { } }
        public virtual void AppendData(string strData) { }
        public virtual void DeleteData(int offset, int count) { }
        public virtual void InsertData(int offset, string strData) { }
        public virtual void ReplaceData(int offset, int count, string strData) { }
        public virtual string Substring(int offset, int count) { return default(string); }
    }
    public partial class XmlComment : System.Xml.XmlCharacterData
    {
        protected internal XmlComment(string comment, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlConvert
    {
        public XmlConvert() { }
        public static string DecodeName(string name) { return default(string); }
        public static string EncodeLocalName(string name) { return default(string); }
        public static string EncodeName(string name) { return default(string); }
        public static string EncodeNmToken(string name) { return default(string); }
        public static bool IsNCNameChar(char ch) { return default(bool); }
        public static bool IsPublicIdChar(char ch) { return default(bool); }
        public static bool IsStartNCNameChar(char ch) { return default(bool); }
        public static bool IsWhitespaceChar(char ch) { return default(bool); }
        public static bool IsXmlChar(char ch) { return default(bool); }
        public static bool IsXmlSurrogatePair(char lowChar, char highChar) { return default(bool); }
        public static bool ToBoolean(string s) { return default(bool); }
        public static byte ToByte(string s) { return default(byte); }
        public static char ToChar(string s) { return default(char); }
        [System.ObsoleteAttribute("Use XmlConvert.ToDateTime() that takes in XmlDateTimeSerializationMode")]
        public static System.DateTime ToDateTime(string s) { return default(System.DateTime); }
        public static System.DateTime ToDateTime(string s, string format) { return default(System.DateTime); }
        public static System.DateTime ToDateTime(string s, string[] formats) { return default(System.DateTime); }
        public static System.DateTime ToDateTime(string s, System.Xml.XmlDateTimeSerializationMode dateTimeOption) { return default(System.DateTime); }
        public static System.DateTimeOffset ToDateTimeOffset(string s) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset ToDateTimeOffset(string s, string format) { return default(System.DateTimeOffset); }
        public static System.DateTimeOffset ToDateTimeOffset(string s, string[] formats) { return default(System.DateTimeOffset); }
        public static decimal ToDecimal(string s) { return default(decimal); }
        public static double ToDouble(string s) { return default(double); }
        public static System.Guid ToGuid(string s) { return default(System.Guid); }
        public static short ToInt16(string s) { return default(short); }
        public static int ToInt32(string s) { return default(int); }
        public static long ToInt64(string s) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string s) { return default(sbyte); }
        public static float ToSingle(string s) { return default(float); }
        public static string ToString(bool value) { return default(string); }
        public static string ToString(byte value) { return default(string); }
        public static string ToString(char value) { return default(string); }
        [System.ObsoleteAttribute("Use XmlConvert.ToString() that takes in XmlDateTimeSerializationMode")]
        public static string ToString(System.DateTime value) { return default(string); }
        public static string ToString(System.DateTime value, string format) { return default(string); }
        public static string ToString(System.DateTime value, System.Xml.XmlDateTimeSerializationMode dateTimeOption) { return default(string); }
        public static string ToString(System.DateTimeOffset value) { return default(string); }
        public static string ToString(System.DateTimeOffset value, string format) { return default(string); }
        public static string ToString(decimal value) { return default(string); }
        public static string ToString(double value) { return default(string); }
        public static string ToString(System.Guid value) { return default(string); }
        public static string ToString(short value) { return default(string); }
        public static string ToString(int value) { return default(string); }
        public static string ToString(long value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value) { return default(string); }
        public static string ToString(float value) { return default(string); }
        public static string ToString(System.TimeSpan value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value) { return default(string); }
        public static System.TimeSpan ToTimeSpan(string s) { return default(System.TimeSpan); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string s) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string s) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string s) { return default(ulong); }
        public static string VerifyName(string name) { return default(string); }
        public static string VerifyNCName(string name) { return default(string); }
        public static string VerifyNMTOKEN(string name) { return default(string); }
        public static string VerifyPublicId(string publicId) { return default(string); }
        public static string VerifyTOKEN(string token) { return default(string); }
        public static string VerifyWhitespace(string content) { return default(string); }
        public static string VerifyXmlChars(string content) { return default(string); }
    }
    public enum XmlDateTimeSerializationMode
    {
        Local = 0,
        RoundtripKind = 3,
        Unspecified = 2,
        Utc = 1,
    }
    public partial class XmlDeclaration : System.Xml.XmlLinkedNode
    {
        protected internal XmlDeclaration(string version, string encoding, string standalone, System.Xml.XmlDocument doc) { }
        public string Encoding { get { return default(string); } set { } }
        public override string InnerText { get { return default(string); } set { } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string Standalone { get { return default(string); } set { } }
        public override string Value { get { return default(string); } set { } }
        public string Version { get { return default(string); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlDocument : System.Xml.XmlNode
    {
        public XmlDocument() { }
        protected internal XmlDocument(System.Xml.XmlImplementation imp) { }
        public XmlDocument(System.Xml.XmlNameTable nt) { }
        public override string BaseURI { get { return default(string); } }
        public System.Xml.XmlElement DocumentElement { get { return default(System.Xml.XmlElement); } }
        public virtual System.Xml.XmlDocumentType DocumentType { get { return default(System.Xml.XmlDocumentType); } }
        public System.Xml.XmlImplementation Implementation { get { return default(System.Xml.XmlImplementation); } }
        public override string InnerText { set { } }
        public override string InnerXml { get { return default(string); } set { } }
        public override bool IsReadOnly { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlDocument OwnerDocument { get { return default(System.Xml.XmlDocument); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public bool PreserveWhitespace { get { return default(bool); } set { } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public System.Xml.Schema.XmlSchemaSet Schemas { get { return default(System.Xml.Schema.XmlSchemaSet); } set { } }
        public virtual System.Xml.XmlResolver XmlResolver { set { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeChanged { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeChanging { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeInserted { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeInserting { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeRemoved { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeRemoving { add { } remove { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public System.Xml.XmlAttribute CreateAttribute(string name) { return default(System.Xml.XmlAttribute); }
        public System.Xml.XmlAttribute CreateAttribute(string qualifiedName, string namespaceURI) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlAttribute CreateAttribute(string prefix, string localName, string namespaceURI) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlCDataSection CreateCDataSection(string data) { return default(System.Xml.XmlCDataSection); }
        public virtual System.Xml.XmlComment CreateComment(string data) { return default(System.Xml.XmlComment); }
        protected internal virtual System.Xml.XmlAttribute CreateDefaultAttribute(string prefix, string localName, string namespaceURI) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlDocumentFragment CreateDocumentFragment() { return default(System.Xml.XmlDocumentFragment); }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
        public virtual System.Xml.XmlDocumentType CreateDocumentType(string name, string publicId, string systemId, string internalSubset) { return default(System.Xml.XmlDocumentType); }
        public System.Xml.XmlElement CreateElement(string name) { return default(System.Xml.XmlElement); }
        public System.Xml.XmlElement CreateElement(string qualifiedName, string namespaceURI) { return default(System.Xml.XmlElement); }
        public virtual System.Xml.XmlElement CreateElement(string prefix, string localName, string namespaceURI) { return default(System.Xml.XmlElement); }
        public virtual System.Xml.XmlEntityReference CreateEntityReference(string name) { return default(System.Xml.XmlEntityReference); }
        public override System.Xml.XPath.XPathNavigator CreateNavigator() { return default(System.Xml.XPath.XPathNavigator); }
        protected internal virtual System.Xml.XPath.XPathNavigator CreateNavigator(System.Xml.XmlNode node) { return default(System.Xml.XPath.XPathNavigator); }
        public virtual System.Xml.XmlNode CreateNode(string nodeTypeString, string name, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode CreateNode(System.Xml.XmlNodeType type, string name, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode CreateNode(System.Xml.XmlNodeType type, string prefix, string name, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlProcessingInstruction CreateProcessingInstruction(string target, string data) { return default(System.Xml.XmlProcessingInstruction); }
        public virtual System.Xml.XmlSignificantWhitespace CreateSignificantWhitespace(string text) { return default(System.Xml.XmlSignificantWhitespace); }
        public virtual System.Xml.XmlText CreateTextNode(string text) { return default(System.Xml.XmlText); }
        public virtual System.Xml.XmlWhitespace CreateWhitespace(string text) { return default(System.Xml.XmlWhitespace); }
        public virtual System.Xml.XmlDeclaration CreateXmlDeclaration(string version, string encoding, string standalone) { return default(System.Xml.XmlDeclaration); }
        public virtual System.Xml.XmlElement GetElementById(string elementId) { return default(System.Xml.XmlElement); }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string name) { return default(System.Xml.XmlNodeList); }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string localName, string namespaceURI) { return default(System.Xml.XmlNodeList); }
        public virtual System.Xml.XmlNode ImportNode(System.Xml.XmlNode node, bool deep) { return default(System.Xml.XmlNode); }
        public virtual void Load(System.IO.Stream inStream) { }
        public virtual void Load(System.IO.TextReader txtReader) { }
        public virtual void Load(string filename) { }
        public virtual void Load(System.Xml.XmlReader reader) { }
        public virtual void LoadXml(string xml) { }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
        public virtual System.Xml.XmlNode ReadNode(System.Xml.XmlReader reader) { return default(System.Xml.XmlNode); }
        public virtual void Save(System.IO.Stream outStream) { }
        public virtual void Save(System.IO.TextWriter writer) { }
        public virtual void Save(string filename) { }
        public virtual void Save(System.Xml.XmlWriter w) { }
        public void Validate(System.Xml.Schema.ValidationEventHandler validationEventHandler) { }
        public void Validate(System.Xml.Schema.ValidationEventHandler validationEventHandler, System.Xml.XmlNode nodeToValidate) { }
        public override void WriteContentTo(System.Xml.XmlWriter xw) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlDocumentFragment : System.Xml.XmlNode
    {
        protected internal XmlDocumentFragment(System.Xml.XmlDocument ownerDocument) { }
        public override string InnerXml { get { return default(string); } set { } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlDocument OwnerDocument { get { return default(System.Xml.XmlDocument); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlDocumentType : System.Xml.XmlLinkedNode
    {
        protected internal XmlDocumentType(string name, string publicId, string systemId, string internalSubset, System.Xml.XmlDocument doc) { }
        public System.Xml.XmlNamedNodeMap Entities { get { return default(System.Xml.XmlNamedNodeMap); } }
        public string InternalSubset { get { return default(string); } }
        public override bool IsReadOnly { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public System.Xml.XmlNamedNodeMap Notations { get { return default(System.Xml.XmlNamedNodeMap); } }
        public string PublicId { get { return default(string); } }
        public string SystemId { get { return default(string); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlElement : System.Xml.XmlLinkedNode
    {
        protected internal XmlElement(string prefix, string localName, string namespaceURI, System.Xml.XmlDocument doc) { }
        public override System.Xml.XmlAttributeCollection Attributes { get { return default(System.Xml.XmlAttributeCollection); } }
        public virtual bool HasAttributes { get { return default(bool); } }
        public override string InnerText { get { return default(string); } set { } }
        public override string InnerXml { get { return default(string); } set { } }
        public bool IsEmpty { get { return default(bool); } set { } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override string NamespaceURI { get { return default(string); } }
        public override System.Xml.XmlNode NextSibling { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlDocument OwnerDocument { get { return default(System.Xml.XmlDocument); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override string Prefix { get { return default(string); } set { } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public virtual string GetAttribute(string name) { return default(string); }
        public virtual string GetAttribute(string localName, string namespaceURI) { return default(string); }
        public virtual System.Xml.XmlAttribute GetAttributeNode(string name) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlAttribute GetAttributeNode(string localName, string namespaceURI) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string name) { return default(System.Xml.XmlNodeList); }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string localName, string namespaceURI) { return default(System.Xml.XmlNodeList); }
        public virtual bool HasAttribute(string name) { return default(bool); }
        public virtual bool HasAttribute(string localName, string namespaceURI) { return default(bool); }
        public override void RemoveAll() { }
        public virtual void RemoveAllAttributes() { }
        public virtual void RemoveAttribute(string name) { }
        public virtual void RemoveAttribute(string localName, string namespaceURI) { }
        public virtual System.Xml.XmlNode RemoveAttributeAt(int i) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlAttribute RemoveAttributeNode(string localName, string namespaceURI) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlAttribute RemoveAttributeNode(System.Xml.XmlAttribute oldAttr) { return default(System.Xml.XmlAttribute); }
        public virtual void SetAttribute(string name, string value) { }
        public virtual string SetAttribute(string localName, string namespaceURI, string value) { return default(string); }
        public virtual System.Xml.XmlAttribute SetAttributeNode(string localName, string namespaceURI) { return default(System.Xml.XmlAttribute); }
        public virtual System.Xml.XmlAttribute SetAttributeNode(System.Xml.XmlAttribute newAttr) { return default(System.Xml.XmlAttribute); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlEntity : System.Xml.XmlNode
    {
        internal XmlEntity() { }
        public override string BaseURI { get { return default(string); } }
        public override string InnerText { get { return default(string); } set { } }
        public override string InnerXml { get { return default(string); } set { } }
        public override bool IsReadOnly { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string NotationName { get { return default(string); } }
        public override string OuterXml { get { return default(string); } }
        public string PublicId { get { return default(string); } }
        public string SystemId { get { return default(string); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlEntityReference : System.Xml.XmlLinkedNode
    {
        protected internal XmlEntityReference(string name, System.Xml.XmlDocument doc) { }
        public override string BaseURI { get { return default(string); } }
        public override bool IsReadOnly { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override string Value { get { return default(string); } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlException : System.SystemException
    {
        public XmlException() { }
        protected XmlException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlException(string message) { }
        public XmlException(string message, System.Exception innerException) { }
        public XmlException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public override string Message { get { return default(string); } }
        public string SourceUri { get { return default(string); } }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class XmlImplementation
    {
        public XmlImplementation() { }
        public XmlImplementation(System.Xml.XmlNameTable nt) { }
        public virtual System.Xml.XmlDocument CreateDocument() { return default(System.Xml.XmlDocument); }
        public bool HasFeature(string strFeature, string strVersion) { return default(bool); }
    }
    public abstract partial class XmlLinkedNode : System.Xml.XmlNode
    {
        internal XmlLinkedNode() { }
        public override System.Xml.XmlNode NextSibling { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode PreviousSibling { get { return default(System.Xml.XmlNode); } }
    }
    public partial class XmlNamedNodeMap : System.Collections.IEnumerable
    {
        internal XmlNamedNodeMap() { }
        public virtual int Count { get { return default(int); } }
        public virtual System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public virtual System.Xml.XmlNode GetNamedItem(string name) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode GetNamedItem(string localName, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode Item(int index) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode RemoveNamedItem(string name) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode RemoveNamedItem(string localName, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode SetNamedItem(System.Xml.XmlNode node) { return default(System.Xml.XmlNode); }
    }
    public partial class XmlNamespaceManager : System.Collections.IEnumerable, System.Xml.IXmlNamespaceResolver
    {
        public XmlNamespaceManager(System.Xml.XmlNameTable nameTable) { }
        public virtual string DefaultNamespace { get { return default(string); } }
        public virtual System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        public virtual void AddNamespace(string prefix, string uri) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public virtual System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { return default(System.Collections.Generic.IDictionary<string, string>); }
        public virtual bool HasNamespace(string prefix) { return default(bool); }
        public virtual string LookupNamespace(string prefix) { return default(string); }
        public virtual string LookupPrefix(string uri) { return default(string); }
        public virtual bool PopScope() { return default(bool); }
        public virtual void PushScope() { }
        public virtual void RemoveNamespace(string prefix, string uri) { }
    }
    public enum XmlNamespaceScope
    {
        All = 0,
        ExcludeXml = 1,
        Local = 2,
    }
    public abstract partial class XmlNameTable
    {
        protected XmlNameTable() { }
        public abstract string Add(char[] array, int offset, int length);
        public abstract string Add(string array);
        public abstract string Get(char[] array, int offset, int length);
        public abstract string Get(string array);
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    public abstract partial class XmlNode : System.Collections.IEnumerable, System.ICloneable, System.Xml.XPath.IXPathNavigable
    {
        internal XmlNode() { }
        public virtual System.Xml.XmlAttributeCollection Attributes { get { return default(System.Xml.XmlAttributeCollection); } }
        public virtual string BaseURI { get { return default(string); } }
        public virtual System.Xml.XmlNodeList ChildNodes { get { return default(System.Xml.XmlNodeList); } }
        public virtual System.Xml.XmlNode FirstChild { get { return default(System.Xml.XmlNode); } }
        public virtual bool HasChildNodes { get { return default(bool); } }
        public virtual string InnerText { get { return default(string); } set { } }
        public virtual string InnerXml { get { return default(string); } set { } }
        public virtual bool IsReadOnly { get { return default(bool); } }
        public virtual System.Xml.XmlElement this[string name] { get { return default(System.Xml.XmlElement); } }
        public virtual System.Xml.XmlElement this[string localname, string ns] { get { return default(System.Xml.XmlElement); } }
        public virtual System.Xml.XmlNode LastChild { get { return default(System.Xml.XmlNode); } }
        public abstract string LocalName { get; }
        public abstract string Name { get; }
        public virtual string NamespaceURI { get { return default(string); } }
        public virtual System.Xml.XmlNode NextSibling { get { return default(System.Xml.XmlNode); } }
        public abstract System.Xml.XmlNodeType NodeType { get; }
        public virtual string OuterXml { get { return default(string); } }
        public virtual System.Xml.XmlDocument OwnerDocument { get { return default(System.Xml.XmlDocument); } }
        public virtual System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public virtual string Prefix { get { return default(string); } set { } }
        public virtual System.Xml.XmlNode PreviousSibling { get { return default(System.Xml.XmlNode); } }
        public virtual System.Xml.XmlNode PreviousText { get { return default(System.Xml.XmlNode); } }
        public virtual System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public virtual string Value { get { return default(string); } set { } }
        public virtual System.Xml.XmlNode AppendChild(System.Xml.XmlNode newChild) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode Clone() { return default(System.Xml.XmlNode); }
        public abstract System.Xml.XmlNode CloneNode(bool deep);
        public virtual System.Xml.XPath.XPathNavigator CreateNavigator() { return default(System.Xml.XPath.XPathNavigator); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public virtual string GetNamespaceOfPrefix(string prefix) { return default(string); }
        public virtual string GetPrefixOfNamespace(string namespaceURI) { return default(string); }
        public virtual System.Xml.XmlNode InsertAfter(System.Xml.XmlNode newChild, System.Xml.XmlNode refChild) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode InsertBefore(System.Xml.XmlNode newChild, System.Xml.XmlNode refChild) { return default(System.Xml.XmlNode); }
        public virtual void Normalize() { }
        public virtual System.Xml.XmlNode PrependChild(System.Xml.XmlNode newChild) { return default(System.Xml.XmlNode); }
        public virtual void RemoveAll() { }
        public virtual System.Xml.XmlNode RemoveChild(System.Xml.XmlNode oldChild) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode ReplaceChild(System.Xml.XmlNode newChild, System.Xml.XmlNode oldChild) { return default(System.Xml.XmlNode); }
        public System.Xml.XmlNodeList SelectNodes(string xpath) { return default(System.Xml.XmlNodeList); }
        public System.Xml.XmlNodeList SelectNodes(string xpath, System.Xml.XmlNamespaceManager nsmgr) { return default(System.Xml.XmlNodeList); }
        public System.Xml.XmlNode SelectSingleNode(string xpath) { return default(System.Xml.XmlNode); }
        public System.Xml.XmlNode SelectSingleNode(string xpath, System.Xml.XmlNamespaceManager nsmgr) { return default(System.Xml.XmlNode); }
        public virtual bool Supports(string feature, string version) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        object System.ICloneable.Clone() { return default(object); }
        public abstract void WriteContentTo(System.Xml.XmlWriter w);
        public abstract void WriteTo(System.Xml.XmlWriter w);
    }
    public enum XmlNodeChangedAction
    {
        Change = 2,
        Insert = 0,
        Remove = 1,
    }
    public partial class XmlNodeChangedEventArgs : System.EventArgs
    {
        public XmlNodeChangedEventArgs(System.Xml.XmlNode node, System.Xml.XmlNode oldParent, System.Xml.XmlNode newParent, string oldValue, string newValue, System.Xml.XmlNodeChangedAction action) { }
        public System.Xml.XmlNodeChangedAction Action { get { return default(System.Xml.XmlNodeChangedAction); } }
        public System.Xml.XmlNode NewParent { get { return default(System.Xml.XmlNode); } }
        public string NewValue { get { return default(string); } }
        public System.Xml.XmlNode Node { get { return default(System.Xml.XmlNode); } }
        public System.Xml.XmlNode OldParent { get { return default(System.Xml.XmlNode); } }
        public string OldValue { get { return default(string); } }
    }
    public delegate void XmlNodeChangedEventHandler(object sender, System.Xml.XmlNodeChangedEventArgs e);
    public abstract partial class XmlNodeList : System.Collections.IEnumerable, System.IDisposable
    {
        protected XmlNodeList() { }
        public abstract int Count { get; }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public virtual System.Xml.XmlNode this[int i] { get { return default(System.Xml.XmlNode); } }
        public abstract System.Collections.IEnumerator GetEnumerator();
        public abstract System.Xml.XmlNode Item(int index);
        protected virtual void PrivateDisposeNodeList() { }
        void System.IDisposable.Dispose() { }
    }
    public enum XmlNodeOrder
    {
        After = 1,
        Before = 0,
        Same = 2,
        Unknown = 3,
    }
    public partial class XmlNodeReader : System.Xml.XmlReader, System.Xml.IXmlNamespaceResolver
    {
        public XmlNodeReader(System.Xml.XmlNode node) { }
        public override int AttributeCount { get { return default(int); } }
        public override string BaseURI { get { return default(string); } }
        public override bool CanReadBinaryContent { get { return default(bool); } }
        public override bool CanResolveEntity { get { return default(bool); } }
        public override int Depth { get { return default(int); } }
        public override bool EOF { get { return default(bool); } }
        public override bool HasAttributes { get { return default(bool); } }
        public override bool HasValue { get { return default(bool); } }
        public override bool IsDefault { get { return default(bool); } }
        public override bool IsEmptyElement { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override string NamespaceURI { get { return default(string); } }
        public override System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override string Prefix { get { return default(string); } }
        public override System.Xml.ReadState ReadState { get { return default(System.Xml.ReadState); } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public override string Value { get { return default(string); } }
        public override string XmlLang { get { return default(string); } }
        public override System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public override void Close() { }
        public override string GetAttribute(int attributeIndex) { return default(string); }
        public override string GetAttribute(string name) { return default(string); }
        public override string GetAttribute(string name, string namespaceURI) { return default(string); }
        public override string LookupNamespace(string prefix) { return default(string); }
        public override void MoveToAttribute(int attributeIndex) { }
        public override bool MoveToAttribute(string name) { return default(bool); }
        public override bool MoveToAttribute(string name, string namespaceURI) { return default(bool); }
        public override bool MoveToElement() { return default(bool); }
        public override bool MoveToFirstAttribute() { return default(bool); }
        public override bool MoveToNextAttribute() { return default(bool); }
        public override bool Read() { return default(bool); }
        public override bool ReadAttributeValue() { return default(bool); }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public override string ReadString() { return default(string); }
        public override void ResolveEntity() { }
        public override void Skip() { }
        System.Collections.Generic.IDictionary<string, string> System.Xml.IXmlNamespaceResolver.GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { return default(System.Collections.Generic.IDictionary<string, string>); }
        string System.Xml.IXmlNamespaceResolver.LookupNamespace(string prefix) { return default(string); }
        string System.Xml.IXmlNamespaceResolver.LookupPrefix(string namespaceName) { return default(string); }
    }
    public enum XmlNodeType
    {
        Attribute = 2,
        CDATA = 4,
        Comment = 8,
        Document = 9,
        DocumentFragment = 11,
        DocumentType = 10,
        Element = 1,
        EndElement = 15,
        EndEntity = 16,
        Entity = 6,
        EntityReference = 5,
        None = 0,
        Notation = 12,
        ProcessingInstruction = 7,
        SignificantWhitespace = 14,
        Text = 3,
        Whitespace = 13,
        XmlDeclaration = 17,
    }
    public partial class XmlNotation : System.Xml.XmlNode
    {
        internal XmlNotation() { }
        public override string InnerXml { get { return default(string); } set { } }
        public override bool IsReadOnly { get { return default(bool); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override string OuterXml { get { return default(string); } }
        public string PublicId { get { return default(string); } }
        public string SystemId { get { return default(string); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public enum XmlOutputMethod
    {
        AutoDetect = 3,
        Html = 1,
        Text = 2,
        Xml = 0,
    }
    public partial class XmlParserContext
    {
        public XmlParserContext(System.Xml.XmlNameTable nt, System.Xml.XmlNamespaceManager nsMgr, string docTypeName, string pubId, string sysId, string internalSubset, string baseURI, string xmlLang, System.Xml.XmlSpace xmlSpace) { }
        public XmlParserContext(System.Xml.XmlNameTable nt, System.Xml.XmlNamespaceManager nsMgr, string docTypeName, string pubId, string sysId, string internalSubset, string baseURI, string xmlLang, System.Xml.XmlSpace xmlSpace, System.Text.Encoding enc) { }
        public XmlParserContext(System.Xml.XmlNameTable nt, System.Xml.XmlNamespaceManager nsMgr, string xmlLang, System.Xml.XmlSpace xmlSpace) { }
        public XmlParserContext(System.Xml.XmlNameTable nt, System.Xml.XmlNamespaceManager nsMgr, string xmlLang, System.Xml.XmlSpace xmlSpace, System.Text.Encoding enc) { }
        public string BaseURI { get { return default(string); } set { } }
        public string DocTypeName { get { return default(string); } set { } }
        public System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } set { } }
        public string InternalSubset { get { return default(string); } set { } }
        public System.Xml.XmlNamespaceManager NamespaceManager { get { return default(System.Xml.XmlNamespaceManager); } set { } }
        public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } set { } }
        public string PublicId { get { return default(string); } set { } }
        public string SystemId { get { return default(string); } set { } }
        public string XmlLang { get { return default(string); } set { } }
        public System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } set { } }
    }
    public partial class XmlProcessingInstruction : System.Xml.XmlLinkedNode
    {
        protected internal XmlProcessingInstruction(string target, string data, System.Xml.XmlDocument doc) { }
        public string Data { get { return default(string); } set { } }
        public override string InnerText { get { return default(string); } set { } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string Target { get { return default(string); } }
        public override string Value { get { return default(string); } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlQualifiedName
    {
        public static readonly System.Xml.XmlQualifiedName Empty;
        public XmlQualifiedName() { }
        public XmlQualifiedName(string name) { }
        public XmlQualifiedName(string name, string ns) { }
        public bool IsEmpty { get { return default(bool); } }
        public string Name { get { return default(string); } }
        public string Namespace { get { return default(string); } }
        public override bool Equals(object other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Xml.XmlQualifiedName a, System.Xml.XmlQualifiedName b) { return default(bool); }
        public static bool operator !=(System.Xml.XmlQualifiedName a, System.Xml.XmlQualifiedName b) { return default(bool); }
        public override string ToString() { return default(string); }
        public static string ToString(string name, string ns) { return default(string); }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    public abstract partial class XmlReader : System.IDisposable
    {
        protected XmlReader() { }
        public abstract int AttributeCount { get; }
        public abstract string BaseURI { get; }
        public virtual bool CanReadBinaryContent { get { return default(bool); } }
        public virtual bool CanReadValueChunk { get { return default(bool); } }
        public virtual bool CanResolveEntity { get { return default(bool); } }
        public abstract int Depth { get; }
        public abstract bool EOF { get; }
        public virtual bool HasAttributes { get { return default(bool); } }
        public virtual bool HasValue { get { return default(bool); } }
        public virtual bool IsDefault { get { return default(bool); } }
        public abstract bool IsEmptyElement { get; }
        public virtual string this[int i] { get { return default(string); } }
        public virtual string this[string name] { get { return default(string); } }
        public virtual string this[string name, string namespaceURI] { get { return default(string); } }
        public abstract string LocalName { get; }
        public virtual string Name { get { return default(string); } }
        public abstract string NamespaceURI { get; }
        public abstract System.Xml.XmlNameTable NameTable { get; }
        public abstract System.Xml.XmlNodeType NodeType { get; }
        public abstract string Prefix { get; }
        public virtual char QuoteChar { get { return default(char); } }
        public abstract System.Xml.ReadState ReadState { get; }
        public virtual System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public virtual System.Xml.XmlReaderSettings Settings { get { return default(System.Xml.XmlReaderSettings); } }
        public abstract string Value { get; }
        public virtual System.Type ValueType { get { return default(System.Type); } }
        public virtual string XmlLang { get { return default(string); } }
        public virtual System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public virtual void Close() { }
        public static System.Xml.XmlReader Create(System.IO.Stream input) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings settings, string baseUri) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings settings, System.Xml.XmlParserContext inputContext) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings settings, string baseUri) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings settings, System.Xml.XmlParserContext inputContext) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(string inputUri) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(string inputUri, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(string inputUri, System.Xml.XmlReaderSettings settings, System.Xml.XmlParserContext inputContext) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.Xml.XmlReader reader, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract string GetAttribute(int i);
        public abstract string GetAttribute(string name);
        public abstract string GetAttribute(string name, string namespaceURI);
        public virtual System.Threading.Tasks.Task<string> GetValueAsync() { return default(System.Threading.Tasks.Task<string>); }
        public static bool IsName(string str) { return default(bool); }
        public static bool IsNameToken(string str) { return default(bool); }
        public virtual bool IsStartElement() { return default(bool); }
        public virtual bool IsStartElement(string name) { return default(bool); }
        public virtual bool IsStartElement(string localname, string ns) { return default(bool); }
        public abstract string LookupNamespace(string prefix);
        public virtual void MoveToAttribute(int i) { }
        public abstract bool MoveToAttribute(string name);
        public abstract bool MoveToAttribute(string name, string ns);
        public virtual System.Xml.XmlNodeType MoveToContent() { return default(System.Xml.XmlNodeType); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Xml.XmlNodeType> MoveToContentAsync() { return default(System.Threading.Tasks.Task<System.Xml.XmlNodeType>); }
        public abstract bool MoveToElement();
        public abstract bool MoveToFirstAttribute();
        public abstract bool MoveToNextAttribute();
        public abstract bool Read();
        public virtual System.Threading.Tasks.Task<bool> ReadAsync() { return default(System.Threading.Tasks.Task<bool>); }
        public abstract bool ReadAttributeValue();
        public virtual object ReadContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadContentAsAsync(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(System.Threading.Tasks.Task<object>); }
        public virtual int ReadContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual int ReadContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual bool ReadContentAsBoolean() { return default(bool); }
        public virtual System.DateTime ReadContentAsDateTime() { return default(System.DateTime); }
        public virtual System.DateTimeOffset ReadContentAsDateTimeOffset() { return default(System.DateTimeOffset); }
        public virtual decimal ReadContentAsDecimal() { return default(decimal); }
        public virtual double ReadContentAsDouble() { return default(double); }
        public virtual float ReadContentAsFloat() { return default(float); }
        public virtual int ReadContentAsInt() { return default(int); }
        public virtual long ReadContentAsLong() { return default(long); }
        public virtual object ReadContentAsObject() { return default(object); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadContentAsObjectAsync() { return default(System.Threading.Tasks.Task<object>); }
        public virtual string ReadContentAsString() { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadContentAsStringAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual object ReadElementContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
        public virtual object ReadElementContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI) { return default(object); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadElementContentAsAsync(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(System.Threading.Tasks.Task<object>); }
        public virtual int ReadElementContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual bool ReadElementContentAsBoolean() { return default(bool); }
        public virtual bool ReadElementContentAsBoolean(string localName, string namespaceURI) { return default(bool); }
        public virtual System.DateTime ReadElementContentAsDateTime() { return default(System.DateTime); }
        public virtual System.DateTime ReadElementContentAsDateTime(string localName, string namespaceURI) { return default(System.DateTime); }
        public virtual decimal ReadElementContentAsDecimal() { return default(decimal); }
        public virtual decimal ReadElementContentAsDecimal(string localName, string namespaceURI) { return default(decimal); }
        public virtual double ReadElementContentAsDouble() { return default(double); }
        public virtual double ReadElementContentAsDouble(string localName, string namespaceURI) { return default(double); }
        public virtual float ReadElementContentAsFloat() { return default(float); }
        public virtual float ReadElementContentAsFloat(string localName, string namespaceURI) { return default(float); }
        public virtual int ReadElementContentAsInt() { return default(int); }
        public virtual int ReadElementContentAsInt(string localName, string namespaceURI) { return default(int); }
        public virtual long ReadElementContentAsLong() { return default(long); }
        public virtual long ReadElementContentAsLong(string localName, string namespaceURI) { return default(long); }
        public virtual object ReadElementContentAsObject() { return default(object); }
        public virtual object ReadElementContentAsObject(string localName, string namespaceURI) { return default(object); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadElementContentAsObjectAsync() { return default(System.Threading.Tasks.Task<object>); }
        public virtual string ReadElementContentAsString() { return default(string); }
        public virtual string ReadElementContentAsString(string localName, string namespaceURI) { return default(string); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> ReadElementContentAsStringAsync() { return default(System.Threading.Tasks.Task<string>); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public virtual string ReadElementString() { return default(string); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public virtual string ReadElementString(string name) { return default(string); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public virtual string ReadElementString(string localname, string ns) { return default(string); }
        public virtual void ReadEndElement() { }
        public virtual string ReadInnerXml() { return default(string); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> ReadInnerXmlAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual string ReadOuterXml() { return default(string); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> ReadOuterXmlAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual void ReadStartElement() { }
        public virtual void ReadStartElement(string name) { }
        public virtual void ReadStartElement(string localname, string ns) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public virtual string ReadString() { return default(string); }
        public virtual System.Xml.XmlReader ReadSubtree() { return default(System.Xml.XmlReader); }
        public virtual bool ReadToDescendant(string name) { return default(bool); }
        public virtual bool ReadToDescendant(string localName, string namespaceURI) { return default(bool); }
        public virtual bool ReadToFollowing(string name) { return default(bool); }
        public virtual bool ReadToFollowing(string localName, string namespaceURI) { return default(bool); }
        public virtual bool ReadToNextSibling(string name) { return default(bool); }
        public virtual bool ReadToNextSibling(string localName, string namespaceURI) { return default(bool); }
        public virtual int ReadValueChunk(char[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadValueChunkAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public abstract void ResolveEntity();
        public virtual void Skip() { }
        public virtual System.Threading.Tasks.Task SkipAsync() { return default(System.Threading.Tasks.Task); }
    }
    public sealed partial class XmlReaderSettings
    {
        public XmlReaderSettings() { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public XmlReaderSettings(System.Xml.XmlResolver resolver) { }
        public bool Async { get { return default(bool); } set { } }
        public bool CheckCharacters { get { return default(bool); } set { } }
        public bool CloseInput { get { return default(bool); } set { } }
        public System.Xml.ConformanceLevel ConformanceLevel { get { return default(System.Xml.ConformanceLevel); } set { } }
        public System.Xml.DtdProcessing DtdProcessing { get { return default(System.Xml.DtdProcessing); } set { } }
        public bool IgnoreComments { get { return default(bool); } set { } }
        public bool IgnoreProcessingInstructions { get { return default(bool); } set { } }
        public bool IgnoreWhitespace { get { return default(bool); } set { } }
        public int LineNumberOffset { get { return default(int); } set { } }
        public int LinePositionOffset { get { return default(int); } set { } }
        public long MaxCharactersFromEntities { get { return default(long); } set { } }
        public long MaxCharactersInDocument { get { return default(long); } set { } }
        public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } set { } }
        [System.ObsoleteAttribute("Use XmlReaderSettings.DtdProcessing property instead.")]
        public bool ProhibitDtd { get { return default(bool); } set { } }
        public System.Xml.Schema.XmlSchemaSet Schemas { get { return default(System.Xml.Schema.XmlSchemaSet); } set { } }
        public System.Xml.Schema.XmlSchemaValidationFlags ValidationFlags { get { return default(System.Xml.Schema.XmlSchemaValidationFlags); } set { } }
        public System.Xml.ValidationType ValidationType { get { return default(System.Xml.ValidationType); } set { } }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public System.Xml.XmlReaderSettings Clone() { return default(System.Xml.XmlReaderSettings); }
        public void Reset() { }
    }
    public abstract partial class XmlResolver
    {
        protected XmlResolver() { }
        public virtual System.Net.ICredentials Credentials { set { } }
        public abstract object GetEntity(System.Uri absoluteUri, string role, System.Type ofObjectToReturn);
        public virtual System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(System.Threading.Tasks.Task<object>); }
        public virtual System.Uri ResolveUri(System.Uri baseUri, string relativeUri) { return default(System.Uri); }
        public virtual bool SupportsType(System.Uri absoluteUri, System.Type type) { return default(bool); }
    }
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    public partial class XmlSecureResolver : System.Xml.XmlResolver
    {
        public XmlSecureResolver(System.Xml.XmlResolver resolver, System.Security.PermissionSet permissionSet) { }
        public XmlSecureResolver(System.Xml.XmlResolver resolver, System.Security.Policy.Evidence evidence) { }
        public XmlSecureResolver(System.Xml.XmlResolver resolver, string securityUrl) { }
        public override System.Net.ICredentials Credentials { set { } }
        public static System.Security.Policy.Evidence CreateEvidenceForUrl(string securityUrl) { return default(System.Security.Policy.Evidence); }
        public override object GetEntity(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(object); }
        public override System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(System.Threading.Tasks.Task<object>); }
        public override System.Uri ResolveUri(System.Uri baseUri, string relativeUri) { return default(System.Uri); }
    }
    public partial class XmlSignificantWhitespace : System.Xml.XmlCharacterData
    {
        protected internal XmlSignificantWhitespace(string strData, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode PreviousText { get { return default(System.Xml.XmlNode); } }
        public override string Value { get { return default(string); } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public enum XmlSpace
    {
        Default = 1,
        None = 0,
        Preserve = 2,
    }
    public partial class XmlText : System.Xml.XmlCharacterData
    {
        protected internal XmlText(string strData, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode PreviousText { get { return default(System.Xml.XmlNode); } }
        public override string Value { get { return default(string); } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlText SplitText(int offset) { return default(System.Xml.XmlText); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    public partial class XmlTextReader : System.Xml.XmlReader, System.Xml.IXmlLineInfo, System.Xml.IXmlNamespaceResolver
    {
        protected XmlTextReader() { }
        public XmlTextReader(System.IO.Stream input) { }
        public XmlTextReader(System.IO.Stream input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(System.IO.Stream xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext context) { }
        public XmlTextReader(System.IO.TextReader input) { }
        public XmlTextReader(System.IO.TextReader input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string url) { }
        public XmlTextReader(string url, System.IO.Stream input) { }
        public XmlTextReader(string url, System.IO.Stream input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string url, System.IO.TextReader input) { }
        public XmlTextReader(string url, System.IO.TextReader input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string url, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext context) { }
        protected XmlTextReader(System.Xml.XmlNameTable nt) { }
        public override int AttributeCount { get { return default(int); } }
        public override string BaseURI { get { return default(string); } }
        public override bool CanReadBinaryContent { get { return default(bool); } }
        public override bool CanReadValueChunk { get { return default(bool); } }
        public override bool CanResolveEntity { get { return default(bool); } }
        public override int Depth { get { return default(int); } }
        public System.Xml.DtdProcessing DtdProcessing { get { return default(System.Xml.DtdProcessing); } set { } }
        public System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } }
        public System.Xml.EntityHandling EntityHandling { get { return default(System.Xml.EntityHandling); } set { } }
        public override bool EOF { get { return default(bool); } }
        public override bool HasValue { get { return default(bool); } }
        public override bool IsDefault { get { return default(bool); } }
        public override bool IsEmptyElement { get { return default(bool); } }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public bool Namespaces { get { return default(bool); } set { } }
        public override string NamespaceURI { get { return default(string); } }
        public override System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public bool Normalization { get { return default(bool); } set { } }
        public override string Prefix { get { return default(string); } }
        [System.ObsoleteAttribute("Use DtdProcessing property instead.")]
        public bool ProhibitDtd { get { return default(bool); } set { } }
        public override char QuoteChar { get { return default(char); } }
        public override System.Xml.ReadState ReadState { get { return default(System.Xml.ReadState); } }
        public override string Value { get { return default(string); } }
        public System.Xml.WhitespaceHandling WhitespaceHandling { get { return default(System.Xml.WhitespaceHandling); } set { } }
        public override string XmlLang { get { return default(string); } }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public override System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public override void Close() { }
        public override string GetAttribute(int i) { return default(string); }
        public override string GetAttribute(string name) { return default(string); }
        public override string GetAttribute(string localName, string namespaceURI) { return default(string); }
        public System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { return default(System.Collections.Generic.IDictionary<string, string>); }
        public System.IO.TextReader GetRemainder() { return default(System.IO.TextReader); }
        public bool HasLineInfo() { return default(bool); }
        public override string LookupNamespace(string prefix) { return default(string); }
        public override void MoveToAttribute(int i) { }
        public override bool MoveToAttribute(string name) { return default(bool); }
        public override bool MoveToAttribute(string localName, string namespaceURI) { return default(bool); }
        public override bool MoveToElement() { return default(bool); }
        public override bool MoveToFirstAttribute() { return default(bool); }
        public override bool MoveToNextAttribute() { return default(bool); }
        public override bool Read() { return default(bool); }
        public override bool ReadAttributeValue() { return default(bool); }
        public int ReadBase64(byte[] array, int offset, int len) { return default(int); }
        public int ReadBinHex(byte[] array, int offset, int len) { return default(int); }
        public int ReadChars(char[] buffer, int index, int count) { return default(int); }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public override string ReadString() { return default(string); }
        public void ResetState() { }
        public override void ResolveEntity() { }
        public override void Skip() { }
        System.Collections.Generic.IDictionary<string, string> System.Xml.IXmlNamespaceResolver.GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { return default(System.Collections.Generic.IDictionary<string, string>); }
        string System.Xml.IXmlNamespaceResolver.LookupNamespace(string prefix) { return default(string); }
        string System.Xml.IXmlNamespaceResolver.LookupPrefix(string namespaceName) { return default(string); }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public partial class XmlTextWriter : System.Xml.XmlWriter
    {
        public XmlTextWriter(System.IO.Stream w, System.Text.Encoding encoding) { }
        public XmlTextWriter(System.IO.TextWriter w) { }
        public XmlTextWriter(string filename, System.Text.Encoding encoding) { }
        public System.IO.Stream BaseStream { get { return default(System.IO.Stream); } }
        public System.Xml.Formatting Formatting { get { return default(System.Xml.Formatting); } set { } }
        public int Indentation { get { return default(int); } set { } }
        public char IndentChar { get { return default(char); } set { } }
        public bool Namespaces { get { return default(bool); } set { } }
        public char QuoteChar { get { return default(char); } set { } }
        public override System.Xml.WriteState WriteState { get { return default(System.Xml.WriteState); } }
        public override string XmlLang { get { return default(string); } }
        public override System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public override void Close() { }
        public override void Flush() { }
        public override string LookupPrefix(string ns) { return default(string); }
        public override void WriteBase64(byte[] buffer, int index, int count) { }
        public override void WriteBinHex(byte[] buffer, int index, int count) { }
        public override void WriteCData(string text) { }
        public override void WriteCharEntity(char ch) { }
        public override void WriteChars(char[] buffer, int index, int count) { }
        public override void WriteComment(string text) { }
        public override void WriteDocType(string name, string pubid, string sysid, string subset) { }
        public override void WriteEndAttribute() { }
        public override void WriteEndDocument() { }
        public override void WriteEndElement() { }
        public override void WriteEntityRef(string name) { }
        public override void WriteFullEndElement() { }
        public override void WriteName(string name) { }
        public override void WriteNmToken(string name) { }
        public override void WriteProcessingInstruction(string name, string text) { }
        public override void WriteQualifiedName(string localName, string ns) { }
        public override void WriteRaw(char[] buffer, int index, int count) { }
        public override void WriteRaw(string data) { }
        public override void WriteStartAttribute(string prefix, string localName, string ns) { }
        public override void WriteStartDocument() { }
        public override void WriteStartDocument(bool standalone) { }
        public override void WriteStartElement(string prefix, string localName, string ns) { }
        public override void WriteString(string text) { }
        public override void WriteSurrogateCharEntity(char lowChar, char highChar) { }
        public override void WriteWhitespace(string ws) { }
    }
    public enum XmlTokenizedType
    {
        CDATA = 0,
        ENTITIES = 5,
        ENTITY = 4,
        ENUMERATION = 9,
        ID = 1,
        IDREF = 2,
        IDREFS = 3,
        NCName = 11,
        NMTOKEN = 6,
        NMTOKENS = 7,
        None = 12,
        NOTATION = 8,
        QName = 10,
    }
    public partial class XmlUrlResolver : System.Xml.XmlResolver
    {
        public XmlUrlResolver() { }
        public System.Net.Cache.RequestCachePolicy CachePolicy { set { } }
        public override System.Net.ICredentials Credentials { set { } }
        public System.Net.IWebProxy Proxy { set { } }
        public override object GetEntity(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(object); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(System.Threading.Tasks.Task<object>); }
        [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
        public override System.Uri ResolveUri(System.Uri baseUri, string relativeUri) { return default(System.Uri); }
    }
    [System.ObsoleteAttribute("Use XmlReader created by XmlReader.Create() method using appropriate XmlReaderSettings instead. http://go.microsoft.com/fwlink/?linkid=14202")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    public partial class XmlValidatingReader : System.Xml.XmlReader, System.Xml.IXmlLineInfo, System.Xml.IXmlNamespaceResolver
    {
        public XmlValidatingReader(System.IO.Stream xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext context) { }
        public XmlValidatingReader(string xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext context) { }
        public XmlValidatingReader(System.Xml.XmlReader reader) { }
        public override int AttributeCount { get { return default(int); } }
        public override string BaseURI { get { return default(string); } }
        public override bool CanReadBinaryContent { get { return default(bool); } }
        public override bool CanResolveEntity { get { return default(bool); } }
        public override int Depth { get { return default(int); } }
        public System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } }
        public System.Xml.EntityHandling EntityHandling { get { return default(System.Xml.EntityHandling); } set { } }
        public override bool EOF { get { return default(bool); } }
        public override bool HasValue { get { return default(bool); } }
        public override bool IsDefault { get { return default(bool); } }
        public override bool IsEmptyElement { get { return default(bool); } }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public bool Namespaces { get { return default(bool); } set { } }
        public override string NamespaceURI { get { return default(string); } }
        public override System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override string Prefix { get { return default(string); } }
        public override char QuoteChar { get { return default(char); } }
        public System.Xml.XmlReader Reader { get { return default(System.Xml.XmlReader); } }
        public override System.Xml.ReadState ReadState { get { return default(System.Xml.ReadState); } }
        public System.Xml.Schema.XmlSchemaCollection Schemas { get { return default(System.Xml.Schema.XmlSchemaCollection); } }
        public object SchemaType { get { return default(object); } }
        public System.Xml.ValidationType ValidationType { get { return default(System.Xml.ValidationType); } set { } }
        public override string Value { get { return default(string); } }
        public override string XmlLang { get { return default(string); } }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public override System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public override void Close() { }
        public override string GetAttribute(int i) { return default(string); }
        public override string GetAttribute(string name) { return default(string); }
        public override string GetAttribute(string localName, string namespaceURI) { return default(string); }
        public bool HasLineInfo() { return default(bool); }
        public override string LookupNamespace(string prefix) { return default(string); }
        public override void MoveToAttribute(int i) { }
        public override bool MoveToAttribute(string name) { return default(bool); }
        public override bool MoveToAttribute(string localName, string namespaceURI) { return default(bool); }
        public override bool MoveToElement() { return default(bool); }
        public override bool MoveToFirstAttribute() { return default(bool); }
        public override bool MoveToNextAttribute() { return default(bool); }
        public override bool Read() { return default(bool); }
        public override bool ReadAttributeValue() { return default(bool); }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public override string ReadString() { return default(string); }
        public object ReadTypedValue() { return default(object); }
        public override void ResolveEntity() { }
        System.Collections.Generic.IDictionary<string, string> System.Xml.IXmlNamespaceResolver.GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { return default(System.Collections.Generic.IDictionary<string, string>); }
        string System.Xml.IXmlNamespaceResolver.LookupNamespace(string prefix) { return default(string); }
        string System.Xml.IXmlNamespaceResolver.LookupPrefix(string namespaceName) { return default(string); }
    }
    public partial class XmlWhitespace : System.Xml.XmlCharacterData
    {
        protected internal XmlWhitespace(string strData, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlNode ParentNode { get { return default(System.Xml.XmlNode); } }
        public override System.Xml.XmlNode PreviousText { get { return default(System.Xml.XmlNode); } }
        public override string Value { get { return default(string); } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public abstract partial class XmlWriter : System.IDisposable
    {
        protected XmlWriter() { }
        public virtual System.Xml.XmlWriterSettings Settings { get { return default(System.Xml.XmlWriterSettings); } }
        public abstract System.Xml.WriteState WriteState { get; }
        public virtual string XmlLang { get { return default(string); } }
        public virtual System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public virtual void Close() { }
        public static System.Xml.XmlWriter Create(System.IO.Stream output) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.IO.Stream output, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.IO.TextWriter output) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.IO.TextWriter output, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(string outputFileName) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(string outputFileName, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.Text.StringBuilder output) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.Text.StringBuilder output, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.Xml.XmlWriter output) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.Xml.XmlWriter output, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void Flush();
        public virtual System.Threading.Tasks.Task FlushAsync() { return default(System.Threading.Tasks.Task); }
        public abstract string LookupPrefix(string ns);
        public virtual void WriteAttributes(System.Xml.XmlReader reader, bool defattr) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task WriteAttributesAsync(System.Xml.XmlReader reader, bool defattr) { return default(System.Threading.Tasks.Task); }
        public void WriteAttributeString(string localName, string value) { }
        public void WriteAttributeString(string localName, string ns, string value) { }
        public void WriteAttributeString(string prefix, string localName, string ns, string value) { }
        public System.Threading.Tasks.Task WriteAttributeStringAsync(string prefix, string localName, string ns, string value) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteBase64(byte[] buffer, int index, int count);
        public virtual System.Threading.Tasks.Task WriteBase64Async(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteBinHex(byte[] buffer, int index, int count) { }
        public virtual System.Threading.Tasks.Task WriteBinHexAsync(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteCData(string text);
        public virtual System.Threading.Tasks.Task WriteCDataAsync(string text) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteCharEntity(char ch);
        public virtual System.Threading.Tasks.Task WriteCharEntityAsync(char ch) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteChars(char[] buffer, int index, int count);
        public virtual System.Threading.Tasks.Task WriteCharsAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteComment(string text);
        public virtual System.Threading.Tasks.Task WriteCommentAsync(string text) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteDocType(string name, string pubid, string sysid, string subset);
        public virtual System.Threading.Tasks.Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset) { return default(System.Threading.Tasks.Task); }
        public void WriteElementString(string localName, string value) { }
        public void WriteElementString(string localName, string ns, string value) { }
        public void WriteElementString(string prefix, string localName, string ns, string value) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task WriteElementStringAsync(string prefix, string localName, string ns, string value) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteEndAttribute();
        protected internal virtual System.Threading.Tasks.Task WriteEndAttributeAsync() { return default(System.Threading.Tasks.Task); }
        public abstract void WriteEndDocument();
        public virtual System.Threading.Tasks.Task WriteEndDocumentAsync() { return default(System.Threading.Tasks.Task); }
        public abstract void WriteEndElement();
        public virtual System.Threading.Tasks.Task WriteEndElementAsync() { return default(System.Threading.Tasks.Task); }
        public abstract void WriteEntityRef(string name);
        public virtual System.Threading.Tasks.Task WriteEntityRefAsync(string name) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteFullEndElement();
        public virtual System.Threading.Tasks.Task WriteFullEndElementAsync() { return default(System.Threading.Tasks.Task); }
        public virtual void WriteName(string name) { }
        public virtual System.Threading.Tasks.Task WriteNameAsync(string name) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteNmToken(string name) { }
        public virtual System.Threading.Tasks.Task WriteNmTokenAsync(string name) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteNode(System.Xml.XmlReader reader, bool defattr) { }
        public virtual void WriteNode(System.Xml.XPath.XPathNavigator navigator, bool defattr) { }
        public virtual System.Threading.Tasks.Task WriteNodeAsync(System.Xml.XmlReader reader, bool defattr) { return default(System.Threading.Tasks.Task); }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task WriteNodeAsync(System.Xml.XPath.XPathNavigator navigator, bool defattr) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteProcessingInstruction(string name, string text);
        public virtual System.Threading.Tasks.Task WriteProcessingInstructionAsync(string name, string text) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteQualifiedName(string localName, string ns) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task WriteQualifiedNameAsync(string localName, string ns) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteRaw(char[] buffer, int index, int count);
        public abstract void WriteRaw(string data);
        public virtual System.Threading.Tasks.Task WriteRawAsync(char[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteRawAsync(string data) { return default(System.Threading.Tasks.Task); }
        public void WriteStartAttribute(string localName) { }
        public void WriteStartAttribute(string localName, string ns) { }
        public abstract void WriteStartAttribute(string prefix, string localName, string ns);
        protected internal virtual System.Threading.Tasks.Task WriteStartAttributeAsync(string prefix, string localName, string ns) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteStartDocument();
        public abstract void WriteStartDocument(bool standalone);
        public virtual System.Threading.Tasks.Task WriteStartDocumentAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task WriteStartDocumentAsync(bool standalone) { return default(System.Threading.Tasks.Task); }
        public void WriteStartElement(string localName) { }
        public void WriteStartElement(string localName, string ns) { }
        public abstract void WriteStartElement(string prefix, string localName, string ns);
        public virtual System.Threading.Tasks.Task WriteStartElementAsync(string prefix, string localName, string ns) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteString(string text);
        public virtual System.Threading.Tasks.Task WriteStringAsync(string text) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteSurrogateCharEntity(char lowChar, char highChar);
        public virtual System.Threading.Tasks.Task WriteSurrogateCharEntityAsync(char lowChar, char highChar) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteValue(bool value) { }
        public virtual void WriteValue(System.DateTime value) { }
        public virtual void WriteValue(System.DateTimeOffset value) { }
        public virtual void WriteValue(decimal value) { }
        public virtual void WriteValue(double value) { }
        public virtual void WriteValue(int value) { }
        public virtual void WriteValue(long value) { }
        public virtual void WriteValue(object value) { }
        public virtual void WriteValue(float value) { }
        public virtual void WriteValue(string value) { }
        public abstract void WriteWhitespace(string ws);
        public virtual System.Threading.Tasks.Task WriteWhitespaceAsync(string ws) { return default(System.Threading.Tasks.Task); }
    }
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    public sealed partial class XmlWriterSettings
    {
        public XmlWriterSettings() { }
        public bool Async { get { return default(bool); } set { } }
        public bool CheckCharacters { get { return default(bool); } set { } }
        public bool CloseOutput { get { return default(bool); } set { } }
        public System.Xml.ConformanceLevel ConformanceLevel { get { return default(System.Xml.ConformanceLevel); } set { } }
        public bool DoNotEscapeUriAttributes { get { return default(bool); } set { } }
        public System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } set { } }
        public bool Indent { get { return default(bool); } set { } }
        public string IndentChars { get { return default(string); } set { } }
        public System.Xml.NamespaceHandling NamespaceHandling { get { return default(System.Xml.NamespaceHandling); } set { } }
        public string NewLineChars { get { return default(string); } set { } }
        public System.Xml.NewLineHandling NewLineHandling { get { return default(System.Xml.NewLineHandling); } set { } }
        public bool NewLineOnAttributes { get { return default(bool); } set { } }
        public bool OmitXmlDeclaration { get { return default(bool); } set { } }
        public System.Xml.XmlOutputMethod OutputMethod { get { return default(System.Xml.XmlOutputMethod); } }
        public bool WriteEndDocumentOnClose { get { return default(bool); } set { } }
        public System.Xml.XmlWriterSettings Clone() { return default(System.Xml.XmlWriterSettings); }
        public void Reset() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
    public partial class XmlXapResolver : System.Xml.XmlResolver
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public XmlXapResolver() { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override object GetEntity(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(object); }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterApplicationResourceStreamResolver(System.Xml.IApplicationResourceStreamResolver appStreamResolver) { }
    }
}
namespace System.Xml.Resolvers
{
    [System.FlagsAttribute]
    public enum XmlKnownDtds
    {
        All = 65535,
        None = 0,
        Rss091 = 2,
        Xhtml10 = 1,
    }
    public partial class XmlPreloadedResolver : System.Xml.XmlResolver
    {
        public XmlPreloadedResolver() { }
        public XmlPreloadedResolver(System.Xml.Resolvers.XmlKnownDtds preloadedDtds) { }
        public XmlPreloadedResolver(System.Xml.XmlResolver fallbackResolver) { }
        public XmlPreloadedResolver(System.Xml.XmlResolver fallbackResolver, System.Xml.Resolvers.XmlKnownDtds preloadedDtds) { }
        public XmlPreloadedResolver(System.Xml.XmlResolver fallbackResolver, System.Xml.Resolvers.XmlKnownDtds preloadedDtds, System.Collections.Generic.IEqualityComparer<System.Uri> uriComparer) { }
        public override System.Net.ICredentials Credentials { set { } }
        public System.Collections.Generic.IEnumerable<System.Uri> PreloadedUris { get { return default(System.Collections.Generic.IEnumerable<System.Uri>); } }
        public void Add(System.Uri uri, byte[] value) { }
        public void Add(System.Uri uri, byte[] value, int offset, int count) { }
        public void Add(System.Uri uri, System.IO.Stream value) { }
        public void Add(System.Uri uri, string value) { }
        public override object GetEntity(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(object); }
        public override System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string role, System.Type ofObjectToReturn) { return default(System.Threading.Tasks.Task<object>); }
        public void Remove(System.Uri uri) { }
        public override System.Uri ResolveUri(System.Uri baseUri, string relativeUri) { return default(System.Uri); }
        public override bool SupportsType(System.Uri absoluteUri, System.Type type) { return default(bool); }
    }
}
namespace System.Xml.Schema
{
    public partial interface IXmlSchemaInfo
    {
        bool IsDefault { get; }
        bool IsNil { get; }
        System.Xml.Schema.XmlSchemaSimpleType MemberType { get; }
        System.Xml.Schema.XmlSchemaAttribute SchemaAttribute { get; }
        System.Xml.Schema.XmlSchemaElement SchemaElement { get; }
        System.Xml.Schema.XmlSchemaType SchemaType { get; }
        System.Xml.Schema.XmlSchemaValidity Validity { get; }
    }
    public partial class ValidationEventArgs : System.EventArgs
    {
        internal ValidationEventArgs() { }
        public System.Xml.Schema.XmlSchemaException Exception { get { return default(System.Xml.Schema.XmlSchemaException); } }
        public string Message { get { return default(string); } }
        public System.Xml.Schema.XmlSeverityType Severity { get { return default(System.Xml.Schema.XmlSeverityType); } }
    }
    public delegate void ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e);
    public sealed partial class XmlAtomicValue : System.Xml.XPath.XPathItem, System.ICloneable
    {
        internal XmlAtomicValue() { }
        public override bool IsNode { get { return default(bool); } }
        public override object TypedValue { get { return default(object); } }
        public override string Value { get { return default(string); } }
        public override bool ValueAsBoolean { get { return default(bool); } }
        public override System.DateTime ValueAsDateTime { get { return default(System.DateTime); } }
        public override double ValueAsDouble { get { return default(double); } }
        public override int ValueAsInt { get { return default(int); } }
        public override long ValueAsLong { get { return default(long); } }
        public override System.Type ValueType { get { return default(System.Type); } }
        public override System.Xml.Schema.XmlSchemaType XmlType { get { return default(System.Xml.Schema.XmlSchemaType); } }
        public System.Xml.Schema.XmlAtomicValue Clone() { return default(System.Xml.Schema.XmlAtomicValue); }
        object System.ICloneable.Clone() { return default(object); }
        public override string ToString() { return default(string); }
        public override object ValueAs(System.Type type, System.Xml.IXmlNamespaceResolver nsResolver) { return default(object); }
    }
    [System.Xml.Serialization.XmlRootAttribute("schema", Namespace="http://www.w3.org/2001/XMLSchema")]
    public partial class XmlSchema : System.Xml.Schema.XmlSchemaObject
    {
        public const string InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        public const string Namespace = "http://www.w3.org/2001/XMLSchema";
        public XmlSchema() { }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaForm)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("attributeFormDefault")]
        public System.Xml.Schema.XmlSchemaForm AttributeFormDefault { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable AttributeGroups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaDerivationMethod)(256))]
        [System.Xml.Serialization.XmlAttributeAttribute("blockDefault")]
        public System.Xml.Schema.XmlSchemaDerivationMethod BlockDefault { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaForm)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("elementFormDefault")]
        public System.Xml.Schema.XmlSchemaForm ElementFormDefault { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Elements { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaDerivationMethod)(256))]
        [System.Xml.Serialization.XmlAttributeAttribute("finalDefault")]
        public System.Xml.Schema.XmlSchemaDerivationMethod FinalDefault { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Groups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string Id { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("import", typeof(System.Xml.Schema.XmlSchemaImport))]
        [System.Xml.Serialization.XmlElementAttribute("include", typeof(System.Xml.Schema.XmlSchemaInclude))]
        [System.Xml.Serialization.XmlElementAttribute("redefine", typeof(System.Xml.Schema.XmlSchemaRedefine))]
        public System.Xml.Schema.XmlSchemaObjectCollection Includes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool IsCompiled { get { return default(bool); } }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroup))]
        [System.Xml.Serialization.XmlElementAttribute("complexType", typeof(System.Xml.Schema.XmlSchemaComplexType))]
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroup))]
        [System.Xml.Serialization.XmlElementAttribute("notation", typeof(System.Xml.Schema.XmlSchemaNotation))]
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Notations { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable SchemaTypes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlAttributeAttribute("targetNamespace", DataType="anyURI")]
        public string TargetNamespace { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("version", DataType="token")]
        public string Version { get { return default(string); } set { } }
        [System.ObsoleteAttribute("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void Compile(System.Xml.Schema.ValidationEventHandler validationEventHandler) { }
        [System.ObsoleteAttribute("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
        public void Compile(System.Xml.Schema.ValidationEventHandler validationEventHandler, System.Xml.XmlResolver resolver) { }
        public static System.Xml.Schema.XmlSchema Read(System.IO.Stream stream, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(System.Xml.Schema.XmlSchema); }
        public static System.Xml.Schema.XmlSchema Read(System.IO.TextReader reader, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(System.Xml.Schema.XmlSchema); }
        public static System.Xml.Schema.XmlSchema Read(System.Xml.XmlReader reader, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(System.Xml.Schema.XmlSchema); }
        public void Write(System.IO.Stream stream) { }
        public void Write(System.IO.Stream stream, System.Xml.XmlNamespaceManager namespaceManager) { }
        public void Write(System.IO.TextWriter writer) { }
        public void Write(System.IO.TextWriter writer, System.Xml.XmlNamespaceManager namespaceManager) { }
        public void Write(System.Xml.XmlWriter writer) { }
        public void Write(System.Xml.XmlWriter writer, System.Xml.XmlNamespaceManager namespaceManager) { }
    }
    public partial class XmlSchemaAll : System.Xml.Schema.XmlSchemaGroupBase
    {
        public XmlSchemaAll() { }
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    }
    public partial class XmlSchemaAnnotated : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaAnnotated() { }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        public System.Xml.Schema.XmlSchemaAnnotation Annotation { get { return default(System.Xml.Schema.XmlSchemaAnnotation); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string Id { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
    }
    public partial class XmlSchemaAnnotation : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaAnnotation() { }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string Id { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("appinfo", typeof(System.Xml.Schema.XmlSchemaAppInfo))]
        [System.Xml.Serialization.XmlElementAttribute("documentation", typeof(System.Xml.Schema.XmlSchemaDocumentation))]
        public System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
    }
    public partial class XmlSchemaAny : System.Xml.Schema.XmlSchemaParticle
    {
        public XmlSchemaAny() { }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaContentProcessing)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("processContents")]
        public System.Xml.Schema.XmlSchemaContentProcessing ProcessContents { get { return default(System.Xml.Schema.XmlSchemaContentProcessing); } set { } }
    }
    public partial class XmlSchemaAnyAttribute : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAnyAttribute() { }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaContentProcessing)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("processContents")]
        public System.Xml.Schema.XmlSchemaContentProcessing ProcessContents { get { return default(System.Xml.Schema.XmlSchemaContentProcessing); } set { } }
    }
    public partial class XmlSchemaAppInfo : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaAppInfo() { }
        [System.Xml.Serialization.XmlAnyElementAttribute]
        [System.Xml.Serialization.XmlTextAttribute]
        public System.Xml.XmlNode[] Markup { get { return default(System.Xml.XmlNode[]); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("source", DataType="anyURI")]
        public string Source { get { return default(string); } set { } }
    }
    public partial class XmlSchemaAttribute : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAttribute() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaSimpleType AttributeSchemaType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } }
        [System.ObsoleteAttribute("This property has been deprecated. Please use AttributeSchemaType property that returns a strongly typed attribute type. http://go.microsoft.com/fwlink/?linkid=14202")]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public object AttributeType { get { return default(object); } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("default")]
        public string DefaultValue { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("fixed")]
        public string FixedValue { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaForm)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("form")]
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType")]
        public System.Xml.Schema.XmlSchemaSimpleType SchemaType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public System.Xml.XmlQualifiedName SchemaTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaUse)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("use")]
        public System.Xml.Schema.XmlSchemaUse Use { get { return default(System.Xml.Schema.XmlSchemaUse); } set { } }
    }
    public partial class XmlSchemaAttributeGroup : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAttributeGroup() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaAttributeGroup RedefinedAttributeGroup { get { return default(System.Xml.Schema.XmlSchemaAttributeGroup); } }
    }
    public partial class XmlSchemaAttributeGroupRef : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAttributeGroupRef() { }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public partial class XmlSchemaChoice : System.Xml.Schema.XmlSchemaGroupBase
    {
        public XmlSchemaChoice() { }
        [System.Xml.Serialization.XmlElementAttribute("any", typeof(System.Xml.Schema.XmlSchemaAny))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    }
    [System.ObsoleteAttribute("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
    public sealed partial class XmlSchemaCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public XmlSchemaCollection() { }
        public XmlSchemaCollection(System.Xml.XmlNameTable nametable) { }
        public int Count { get { return default(int); } }
        public System.Xml.Schema.XmlSchema this[string ns] { get { return default(System.Xml.Schema.XmlSchema); } }
        public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        int System.Collections.ICollection.Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public System.Xml.Schema.XmlSchema Add(string ns, string uri) { return default(System.Xml.Schema.XmlSchema); }
        public System.Xml.Schema.XmlSchema Add(string ns, System.Xml.XmlReader reader) { return default(System.Xml.Schema.XmlSchema); }
        public System.Xml.Schema.XmlSchema Add(string ns, System.Xml.XmlReader reader, System.Xml.XmlResolver resolver) { return default(System.Xml.Schema.XmlSchema); }
        public System.Xml.Schema.XmlSchema Add(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
        public System.Xml.Schema.XmlSchema Add(System.Xml.Schema.XmlSchema schema, System.Xml.XmlResolver resolver) { return default(System.Xml.Schema.XmlSchema); }
        public void Add(System.Xml.Schema.XmlSchemaCollection schema) { }
        public bool Contains(string ns) { return default(bool); }
        public bool Contains(System.Xml.Schema.XmlSchema schema) { return default(bool); }
        public void CopyTo(System.Xml.Schema.XmlSchema[] array, int index) { }
        public System.Xml.Schema.XmlSchemaCollectionEnumerator GetEnumerator() { return default(System.Xml.Schema.XmlSchemaCollectionEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class XmlSchemaCollectionEnumerator : System.Collections.IEnumerator
    {
        internal XmlSchemaCollectionEnumerator() { }
        public System.Xml.Schema.XmlSchema Current { get { return default(System.Xml.Schema.XmlSchema); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        bool System.Collections.IEnumerator.MoveNext() { return default(bool); }
        void System.Collections.IEnumerator.Reset() { }
    }
    public sealed partial class XmlSchemaCompilationSettings
    {
        public XmlSchemaCompilationSettings() { }
        public bool EnableUpaCheck { get { return default(bool); } set { } }
    }
    public partial class XmlSchemaComplexContent : System.Xml.Schema.XmlSchemaContentModel
    {
        public XmlSchemaComplexContent() { }
        [System.Xml.Serialization.XmlElementAttribute("extension", typeof(System.Xml.Schema.XmlSchemaComplexContentExtension))]
        [System.Xml.Serialization.XmlElementAttribute("restriction", typeof(System.Xml.Schema.XmlSchemaComplexContentRestriction))]
        public override System.Xml.Schema.XmlSchemaContent Content { get { return default(System.Xml.Schema.XmlSchemaContent); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("mixed")]
        public bool IsMixed { get { return default(bool); } set { } }
    }
    public partial class XmlSchemaComplexContentExtension : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaComplexContentExtension() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaParticle Particle { get { return default(System.Xml.Schema.XmlSchemaParticle); } set { } }
    }
    public partial class XmlSchemaComplexContentRestriction : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaComplexContentRestriction() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaParticle Particle { get { return default(System.Xml.Schema.XmlSchemaParticle); } set { } }
    }
    public partial class XmlSchemaComplexType : System.Xml.Schema.XmlSchemaType
    {
        public XmlSchemaComplexType() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable AttributeUses { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaAnyAttribute AttributeWildcard { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaDerivationMethod)(256))]
        [System.Xml.Serialization.XmlAttributeAttribute("block")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Block { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod BlockResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
        [System.Xml.Serialization.XmlElementAttribute("complexContent", typeof(System.Xml.Schema.XmlSchemaComplexContent))]
        [System.Xml.Serialization.XmlElementAttribute("simpleContent", typeof(System.Xml.Schema.XmlSchemaSimpleContent))]
        public System.Xml.Schema.XmlSchemaContentModel ContentModel { get { return default(System.Xml.Schema.XmlSchemaContentModel); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaContentType ContentType { get { return default(System.Xml.Schema.XmlSchemaContentType); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaParticle ContentTypeParticle { get { return default(System.Xml.Schema.XmlSchemaParticle); } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("abstract")]
        public bool IsAbstract { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("mixed")]
        public override bool IsMixed { get { return default(bool); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaParticle Particle { get { return default(System.Xml.Schema.XmlSchemaParticle); } set { } }
    }
    public abstract partial class XmlSchemaContent : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaContent() { }
    }
    public abstract partial class XmlSchemaContentModel : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaContentModel() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public abstract System.Xml.Schema.XmlSchemaContent Content { get; set; }
    }
    public enum XmlSchemaContentProcessing
    {
        [System.Xml.Serialization.XmlEnumAttribute("lax")]
        Lax = 2,
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 0,
        [System.Xml.Serialization.XmlEnumAttribute("skip")]
        Skip = 1,
        [System.Xml.Serialization.XmlEnumAttribute("strict")]
        Strict = 3,
    }
    public enum XmlSchemaContentType
    {
        ElementOnly = 2,
        Empty = 1,
        Mixed = 3,
        TextOnly = 0,
    }
    public abstract partial class XmlSchemaDatatype
    {
        protected XmlSchemaDatatype() { }
        public abstract System.Xml.XmlTokenizedType TokenizedType { get; }
        public virtual System.Xml.Schema.XmlTypeCode TypeCode { get { return default(System.Xml.Schema.XmlTypeCode); } }
        public abstract System.Type ValueType { get; }
        public virtual System.Xml.Schema.XmlSchemaDatatypeVariety Variety { get { return default(System.Xml.Schema.XmlSchemaDatatypeVariety); } }
        public virtual object ChangeType(object value, System.Type targetType) { return default(object); }
        public virtual object ChangeType(object value, System.Type targetType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
        public virtual bool IsDerivedFrom(System.Xml.Schema.XmlSchemaDatatype datatype) { return default(bool); }
        public abstract object ParseValue(string s, System.Xml.XmlNameTable nameTable, System.Xml.IXmlNamespaceResolver nsmgr);
    }
    public enum XmlSchemaDatatypeVariety
    {
        Atomic = 0,
        List = 1,
        Union = 2,
    }
    [System.FlagsAttribute]
    public enum XmlSchemaDerivationMethod
    {
        [System.Xml.Serialization.XmlEnumAttribute("#all")]
        All = 255,
        [System.Xml.Serialization.XmlEnumAttribute("")]
        Empty = 0,
        [System.Xml.Serialization.XmlEnumAttribute("extension")]
        Extension = 2,
        [System.Xml.Serialization.XmlEnumAttribute("list")]
        List = 8,
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 256,
        [System.Xml.Serialization.XmlEnumAttribute("restriction")]
        Restriction = 4,
        [System.Xml.Serialization.XmlEnumAttribute("substitution")]
        Substitution = 1,
        [System.Xml.Serialization.XmlEnumAttribute("union")]
        Union = 16,
    }
    public partial class XmlSchemaDocumentation : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaDocumentation() { }
        [System.Xml.Serialization.XmlAttributeAttribute("xml:lang")]
        public string Language { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAnyElementAttribute]
        [System.Xml.Serialization.XmlTextAttribute]
        public System.Xml.XmlNode[] Markup { get { return default(System.Xml.XmlNode[]); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("source", DataType="anyURI")]
        public string Source { get { return default(string); } set { } }
    }
    public partial class XmlSchemaElement : System.Xml.Schema.XmlSchemaParticle
    {
        public XmlSchemaElement() { }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaDerivationMethod)(256))]
        [System.Xml.Serialization.XmlAttributeAttribute("block")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Block { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod BlockResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
        [System.Xml.Serialization.XmlElementAttribute("key", typeof(System.Xml.Schema.XmlSchemaKey))]
        [System.Xml.Serialization.XmlElementAttribute("keyref", typeof(System.Xml.Schema.XmlSchemaKeyref))]
        [System.Xml.Serialization.XmlElementAttribute("unique", typeof(System.Xml.Schema.XmlSchemaUnique))]
        public System.Xml.Schema.XmlSchemaObjectCollection Constraints { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("default")]
        public string DefaultValue { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaType ElementSchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } }
        [System.ObsoleteAttribute("This property has been deprecated. Please use ElementSchemaType property that returns a strongly typed element type. http://go.microsoft.com/fwlink/?linkid=14202")]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public object ElementType { get { return default(object); } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaDerivationMethod)(256))]
        [System.Xml.Serialization.XmlAttributeAttribute("final")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Final { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod FinalResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("fixed")]
        public string FixedValue { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaForm)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("form")]
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("abstract")]
        public bool IsAbstract { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("nillable")]
        public bool IsNillable { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("complexType", typeof(System.Xml.Schema.XmlSchemaComplexType))]
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaType SchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public System.Xml.XmlQualifiedName SchemaTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("substitutionGroup")]
        public System.Xml.XmlQualifiedName SubstitutionGroup { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public partial class XmlSchemaEnumerationFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaEnumerationFacet() { }
    }
    public partial class XmlSchemaException : System.SystemException
    {
        public XmlSchemaException() { }
        protected XmlSchemaException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlSchemaException(string message) { }
        public XmlSchemaException(string message, System.Exception innerException) { }
        public XmlSchemaException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public override string Message { get { return default(string); } }
        public System.Xml.Schema.XmlSchemaObject SourceSchemaObject { get { return default(System.Xml.Schema.XmlSchemaObject); } }
        public string SourceUri { get { return default(string); } }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class XmlSchemaExternal : System.Xml.Schema.XmlSchemaObject
    {
        protected XmlSchemaExternal() { }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string Id { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchema Schema { get { return default(System.Xml.Schema.XmlSchema); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("schemaLocation", DataType="anyURI")]
        public string SchemaLocation { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
    }
    public abstract partial class XmlSchemaFacet : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaFacet() { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("fixed")]
        public virtual bool IsFixed { get { return default(bool); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("value")]
        public string Value { get { return default(string); } set { } }
    }
    public enum XmlSchemaForm
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 0,
        [System.Xml.Serialization.XmlEnumAttribute("qualified")]
        Qualified = 1,
        [System.Xml.Serialization.XmlEnumAttribute("unqualified")]
        Unqualified = 2,
    }
    public partial class XmlSchemaFractionDigitsFacet : System.Xml.Schema.XmlSchemaNumericFacet
    {
        public XmlSchemaFractionDigitsFacet() { }
    }
    public partial class XmlSchemaGroup : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaGroup() { }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaGroupBase Particle { get { return default(System.Xml.Schema.XmlSchemaGroupBase); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
    }
    public abstract partial class XmlSchemaGroupBase : System.Xml.Schema.XmlSchemaParticle
    {
        protected XmlSchemaGroupBase() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public abstract System.Xml.Schema.XmlSchemaObjectCollection Items { get; }
    }
    public partial class XmlSchemaGroupRef : System.Xml.Schema.XmlSchemaParticle
    {
        public XmlSchemaGroupRef() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaGroupBase Particle { get { return default(System.Xml.Schema.XmlSchemaGroupBase); } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public partial class XmlSchemaIdentityConstraint : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaIdentityConstraint() { }
        [System.Xml.Serialization.XmlElementAttribute("field", typeof(System.Xml.Schema.XmlSchemaXPath))]
        public System.Xml.Schema.XmlSchemaObjectCollection Fields { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
        [System.Xml.Serialization.XmlElementAttribute("selector", typeof(System.Xml.Schema.XmlSchemaXPath))]
        public System.Xml.Schema.XmlSchemaXPath Selector { get { return default(System.Xml.Schema.XmlSchemaXPath); } set { } }
    }
    public partial class XmlSchemaImport : System.Xml.Schema.XmlSchemaExternal
    {
        public XmlSchemaImport() { }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        public System.Xml.Schema.XmlSchemaAnnotation Annotation { get { return default(System.Xml.Schema.XmlSchemaAnnotation); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace", DataType="anyURI")]
        public string Namespace { get { return default(string); } set { } }
    }
    public partial class XmlSchemaInclude : System.Xml.Schema.XmlSchemaExternal
    {
        public XmlSchemaInclude() { }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        public System.Xml.Schema.XmlSchemaAnnotation Annotation { get { return default(System.Xml.Schema.XmlSchemaAnnotation); } set { } }
    }
    public sealed partial class XmlSchemaInference
    {
        public XmlSchemaInference() { }
        public System.Xml.Schema.XmlSchemaInference.InferenceOption Occurrence { get { return default(System.Xml.Schema.XmlSchemaInference.InferenceOption); } set { } }
        public System.Xml.Schema.XmlSchemaInference.InferenceOption TypeInference { get { return default(System.Xml.Schema.XmlSchemaInference.InferenceOption); } set { } }
        public System.Xml.Schema.XmlSchemaSet InferSchema(System.Xml.XmlReader instanceDocument) { return default(System.Xml.Schema.XmlSchemaSet); }
        public System.Xml.Schema.XmlSchemaSet InferSchema(System.Xml.XmlReader instanceDocument, System.Xml.Schema.XmlSchemaSet schemas) { return default(System.Xml.Schema.XmlSchemaSet); }
        public enum InferenceOption
        {
            Relaxed = 1,
            Restricted = 0,
        }
    }
    public partial class XmlSchemaInferenceException : System.Xml.Schema.XmlSchemaException
    {
        public XmlSchemaInferenceException() { }
        protected XmlSchemaInferenceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlSchemaInferenceException(string message) { }
        public XmlSchemaInferenceException(string message, System.Exception innerException) { }
        public XmlSchemaInferenceException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class XmlSchemaInfo : System.Xml.Schema.IXmlSchemaInfo
    {
        public XmlSchemaInfo() { }
        public System.Xml.Schema.XmlSchemaContentType ContentType { get { return default(System.Xml.Schema.XmlSchemaContentType); } set { } }
        public bool IsDefault { get { return default(bool); } set { } }
        public bool IsNil { get { return default(bool); } set { } }
        public System.Xml.Schema.XmlSchemaSimpleType MemberType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
        public System.Xml.Schema.XmlSchemaAttribute SchemaAttribute { get { return default(System.Xml.Schema.XmlSchemaAttribute); } set { } }
        public System.Xml.Schema.XmlSchemaElement SchemaElement { get { return default(System.Xml.Schema.XmlSchemaElement); } set { } }
        public System.Xml.Schema.XmlSchemaType SchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } set { } }
        public System.Xml.Schema.XmlSchemaValidity Validity { get { return default(System.Xml.Schema.XmlSchemaValidity); } set { } }
    }
    public partial class XmlSchemaKey : System.Xml.Schema.XmlSchemaIdentityConstraint
    {
        public XmlSchemaKey() { }
    }
    public partial class XmlSchemaKeyref : System.Xml.Schema.XmlSchemaIdentityConstraint
    {
        public XmlSchemaKeyref() { }
        [System.Xml.Serialization.XmlAttributeAttribute("refer")]
        public System.Xml.XmlQualifiedName Refer { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public partial class XmlSchemaLengthFacet : System.Xml.Schema.XmlSchemaNumericFacet
    {
        public XmlSchemaLengthFacet() { }
    }
    public partial class XmlSchemaMaxExclusiveFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaMaxExclusiveFacet() { }
    }
    public partial class XmlSchemaMaxInclusiveFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaMaxInclusiveFacet() { }
    }
    public partial class XmlSchemaMaxLengthFacet : System.Xml.Schema.XmlSchemaNumericFacet
    {
        public XmlSchemaMaxLengthFacet() { }
    }
    public partial class XmlSchemaMinExclusiveFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaMinExclusiveFacet() { }
    }
    public partial class XmlSchemaMinInclusiveFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaMinInclusiveFacet() { }
    }
    public partial class XmlSchemaMinLengthFacet : System.Xml.Schema.XmlSchemaNumericFacet
    {
        public XmlSchemaMinLengthFacet() { }
    }
    public partial class XmlSchemaNotation : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaNotation() { }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("public")]
        public string Public { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("system")]
        public string System { get { return default(string); } set { } }
    }
    public abstract partial class XmlSchemaNumericFacet : System.Xml.Schema.XmlSchemaFacet
    {
        protected XmlSchemaNumericFacet() { }
    }
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    public abstract partial class XmlSchemaObject
    {
        protected XmlSchemaObject() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int LineNumber { get { return default(int); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int LinePosition { get { return default(int); } set { } }
        [System.Xml.Serialization.XmlNamespaceDeclarationsAttribute]
        public System.Xml.Serialization.XmlSerializerNamespaces Namespaces { get { return default(System.Xml.Serialization.XmlSerializerNamespaces); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObject Parent { get { return default(System.Xml.Schema.XmlSchemaObject); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string SourceUri { get { return default(string); } set { } }
    }
    public partial class XmlSchemaObjectCollection : System.Collections.CollectionBase
    {
        public XmlSchemaObjectCollection() { }
        public XmlSchemaObjectCollection(System.Xml.Schema.XmlSchemaObject parent) { }
        public virtual System.Xml.Schema.XmlSchemaObject this[int index] { get { return default(System.Xml.Schema.XmlSchemaObject); } set { } }
        public int Add(System.Xml.Schema.XmlSchemaObject item) { return default(int); }
        public bool Contains(System.Xml.Schema.XmlSchemaObject item) { return default(bool); }
        public void CopyTo(System.Xml.Schema.XmlSchemaObject[] array, int index) { }
        public new System.Xml.Schema.XmlSchemaObjectEnumerator GetEnumerator() { return default(System.Xml.Schema.XmlSchemaObjectEnumerator); }
        public int IndexOf(System.Xml.Schema.XmlSchemaObject item) { return default(int); }
        public void Insert(int index, System.Xml.Schema.XmlSchemaObject item) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object item) { }
        protected override void OnRemove(int index, object item) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(System.Xml.Schema.XmlSchemaObject item) { }
    }
    public partial class XmlSchemaObjectEnumerator : System.Collections.IEnumerator
    {
        internal XmlSchemaObjectEnumerator() { }
        public System.Xml.Schema.XmlSchemaObject Current { get { return default(System.Xml.Schema.XmlSchemaObject); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
        bool System.Collections.IEnumerator.MoveNext() { return default(bool); }
        void System.Collections.IEnumerator.Reset() { }
    }
    public partial class XmlSchemaObjectTable
    {
        internal XmlSchemaObjectTable() { }
        public int Count { get { return default(int); } }
        public System.Xml.Schema.XmlSchemaObject this[System.Xml.XmlQualifiedName name] { get { return default(System.Xml.Schema.XmlSchemaObject); } }
        public System.Collections.ICollection Names { get { return default(System.Collections.ICollection); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public bool Contains(System.Xml.XmlQualifiedName name) { return default(bool); }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
    }
    public abstract partial class XmlSchemaParticle : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaParticle() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public decimal MaxOccurs { get { return default(decimal); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("maxOccurs")]
        public string MaxOccursString { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public decimal MinOccurs { get { return default(decimal); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("minOccurs")]
        public string MinOccursString { get { return default(string); } set { } }
    }
    public partial class XmlSchemaPatternFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaPatternFacet() { }
    }
    public partial class XmlSchemaRedefine : System.Xml.Schema.XmlSchemaExternal
    {
        public XmlSchemaRedefine() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable AttributeGroups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Groups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroup))]
        [System.Xml.Serialization.XmlElementAttribute("complexType", typeof(System.Xml.Schema.XmlSchemaComplexType))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroup))]
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable SchemaTypes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    }
    public partial class XmlSchemaSequence : System.Xml.Schema.XmlSchemaGroupBase
    {
        public XmlSchemaSequence() { }
        [System.Xml.Serialization.XmlElementAttribute("any", typeof(System.Xml.Schema.XmlSchemaAny))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    }
    public partial class XmlSchemaSet
    {
        public XmlSchemaSet() { }
        public XmlSchemaSet(System.Xml.XmlNameTable nameTable) { }
        public System.Xml.Schema.XmlSchemaCompilationSettings CompilationSettings { get { return default(System.Xml.Schema.XmlSchemaCompilationSettings); } set { } }
        public int Count { get { return default(int); } }
        public System.Xml.Schema.XmlSchemaObjectTable GlobalAttributes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        public System.Xml.Schema.XmlSchemaObjectTable GlobalElements { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        public System.Xml.Schema.XmlSchemaObjectTable GlobalTypes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
        public bool IsCompiled { get { return default(bool); } }
        public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public System.Xml.Schema.XmlSchema Add(string targetNamespace, string schemaUri) { return default(System.Xml.Schema.XmlSchema); }
        public System.Xml.Schema.XmlSchema Add(string targetNamespace, System.Xml.XmlReader schemaDocument) { return default(System.Xml.Schema.XmlSchema); }
        public System.Xml.Schema.XmlSchema Add(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
        public void Add(System.Xml.Schema.XmlSchemaSet schemas) { }
        public void Compile() { }
        public bool Contains(string targetNamespace) { return default(bool); }
        public bool Contains(System.Xml.Schema.XmlSchema schema) { return default(bool); }
        public void CopyTo(System.Xml.Schema.XmlSchema[] schemas, int index) { }
        public System.Xml.Schema.XmlSchema Remove(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
        public bool RemoveRecursive(System.Xml.Schema.XmlSchema schemaToRemove) { return default(bool); }
        public System.Xml.Schema.XmlSchema Reprocess(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
        public System.Collections.ICollection Schemas() { return default(System.Collections.ICollection); }
        public System.Collections.ICollection Schemas(string targetNamespace) { return default(System.Collections.ICollection); }
    }
    public partial class XmlSchemaSimpleContent : System.Xml.Schema.XmlSchemaContentModel
    {
        public XmlSchemaSimpleContent() { }
        [System.Xml.Serialization.XmlElementAttribute("extension", typeof(System.Xml.Schema.XmlSchemaSimpleContentExtension))]
        [System.Xml.Serialization.XmlElementAttribute("restriction", typeof(System.Xml.Schema.XmlSchemaSimpleContentRestriction))]
        public override System.Xml.Schema.XmlSchemaContent Content { get { return default(System.Xml.Schema.XmlSchemaContent); } set { } }
    }
    public partial class XmlSchemaSimpleContentExtension : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaSimpleContentExtension() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public partial class XmlSchemaSimpleContentRestriction : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaSimpleContentRestriction() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaSimpleType BaseType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("enumeration", typeof(System.Xml.Schema.XmlSchemaEnumerationFacet))]
        [System.Xml.Serialization.XmlElementAttribute("fractionDigits", typeof(System.Xml.Schema.XmlSchemaFractionDigitsFacet))]
        [System.Xml.Serialization.XmlElementAttribute("length", typeof(System.Xml.Schema.XmlSchemaLengthFacet))]
        [System.Xml.Serialization.XmlElementAttribute("maxExclusive", typeof(System.Xml.Schema.XmlSchemaMaxExclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("maxInclusive", typeof(System.Xml.Schema.XmlSchemaMaxInclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("maxLength", typeof(System.Xml.Schema.XmlSchemaMaxLengthFacet))]
        [System.Xml.Serialization.XmlElementAttribute("minExclusive", typeof(System.Xml.Schema.XmlSchemaMinExclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("minInclusive", typeof(System.Xml.Schema.XmlSchemaMinInclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("minLength", typeof(System.Xml.Schema.XmlSchemaMinLengthFacet))]
        [System.Xml.Serialization.XmlElementAttribute("pattern", typeof(System.Xml.Schema.XmlSchemaPatternFacet))]
        [System.Xml.Serialization.XmlElementAttribute("totalDigits", typeof(System.Xml.Schema.XmlSchemaTotalDigitsFacet))]
        [System.Xml.Serialization.XmlElementAttribute("whiteSpace", typeof(System.Xml.Schema.XmlSchemaWhiteSpaceFacet))]
        public System.Xml.Schema.XmlSchemaObjectCollection Facets { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    }
    public partial class XmlSchemaSimpleType : System.Xml.Schema.XmlSchemaType
    {
        public XmlSchemaSimpleType() { }
        [System.Xml.Serialization.XmlElementAttribute("list", typeof(System.Xml.Schema.XmlSchemaSimpleTypeList))]
        [System.Xml.Serialization.XmlElementAttribute("restriction", typeof(System.Xml.Schema.XmlSchemaSimpleTypeRestriction))]
        [System.Xml.Serialization.XmlElementAttribute("union", typeof(System.Xml.Schema.XmlSchemaSimpleTypeUnion))]
        public System.Xml.Schema.XmlSchemaSimpleTypeContent Content { get { return default(System.Xml.Schema.XmlSchemaSimpleTypeContent); } set { } }
    }
    public abstract partial class XmlSchemaSimpleTypeContent : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaSimpleTypeContent() { }
    }
    public partial class XmlSchemaSimpleTypeList : System.Xml.Schema.XmlSchemaSimpleTypeContent
    {
        public XmlSchemaSimpleTypeList() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaSimpleType BaseItemType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaSimpleType ItemType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("itemType")]
        public System.Xml.XmlQualifiedName ItemTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public partial class XmlSchemaSimpleTypeRestriction : System.Xml.Schema.XmlSchemaSimpleTypeContent
    {
        public XmlSchemaSimpleTypeRestriction() { }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaSimpleType BaseType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("enumeration", typeof(System.Xml.Schema.XmlSchemaEnumerationFacet))]
        [System.Xml.Serialization.XmlElementAttribute("fractionDigits", typeof(System.Xml.Schema.XmlSchemaFractionDigitsFacet))]
        [System.Xml.Serialization.XmlElementAttribute("length", typeof(System.Xml.Schema.XmlSchemaLengthFacet))]
        [System.Xml.Serialization.XmlElementAttribute("maxExclusive", typeof(System.Xml.Schema.XmlSchemaMaxExclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("maxInclusive", typeof(System.Xml.Schema.XmlSchemaMaxInclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("maxLength", typeof(System.Xml.Schema.XmlSchemaMaxLengthFacet))]
        [System.Xml.Serialization.XmlElementAttribute("minExclusive", typeof(System.Xml.Schema.XmlSchemaMinExclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("minInclusive", typeof(System.Xml.Schema.XmlSchemaMinInclusiveFacet))]
        [System.Xml.Serialization.XmlElementAttribute("minLength", typeof(System.Xml.Schema.XmlSchemaMinLengthFacet))]
        [System.Xml.Serialization.XmlElementAttribute("pattern", typeof(System.Xml.Schema.XmlSchemaPatternFacet))]
        [System.Xml.Serialization.XmlElementAttribute("totalDigits", typeof(System.Xml.Schema.XmlSchemaTotalDigitsFacet))]
        [System.Xml.Serialization.XmlElementAttribute("whiteSpace", typeof(System.Xml.Schema.XmlSchemaWhiteSpaceFacet))]
        public System.Xml.Schema.XmlSchemaObjectCollection Facets { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    }
    public partial class XmlSchemaSimpleTypeUnion : System.Xml.Schema.XmlSchemaSimpleTypeContent
    {
        public XmlSchemaSimpleTypeUnion() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaSimpleType[] BaseMemberTypes { get { return default(System.Xml.Schema.XmlSchemaSimpleType[]); } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaObjectCollection BaseTypes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("memberTypes")]
        public System.Xml.XmlQualifiedName[] MemberTypes { get { return default(System.Xml.XmlQualifiedName[]); } set { } }
    }
    public partial class XmlSchemaTotalDigitsFacet : System.Xml.Schema.XmlSchemaNumericFacet
    {
        public XmlSchemaTotalDigitsFacet() { }
    }
    public partial class XmlSchemaType : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaType() { }
        [System.ObsoleteAttribute("This property has been deprecated. Please use BaseXmlSchemaType property that returns a strongly typed base schema type. http://go.microsoft.com/fwlink/?linkid=14202")]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public object BaseSchemaType { get { return default(object); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaType BaseXmlSchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDatatype Datatype { get { return default(System.Xml.Schema.XmlSchemaDatatype); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod DerivedBy { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
        [System.ComponentModel.DefaultValueAttribute((System.Xml.Schema.XmlSchemaDerivationMethod)(256))]
        [System.Xml.Serialization.XmlAttributeAttribute("final")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Final { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod FinalResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public virtual bool IsMixed { get { return default(bool); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlTypeCode TypeCode { get { return default(System.Xml.Schema.XmlTypeCode); } }
        public static System.Xml.Schema.XmlSchemaComplexType GetBuiltInComplexType(System.Xml.Schema.XmlTypeCode typeCode) { return default(System.Xml.Schema.XmlSchemaComplexType); }
        public static System.Xml.Schema.XmlSchemaComplexType GetBuiltInComplexType(System.Xml.XmlQualifiedName qualifiedName) { return default(System.Xml.Schema.XmlSchemaComplexType); }
        public static System.Xml.Schema.XmlSchemaSimpleType GetBuiltInSimpleType(System.Xml.Schema.XmlTypeCode typeCode) { return default(System.Xml.Schema.XmlSchemaSimpleType); }
        public static System.Xml.Schema.XmlSchemaSimpleType GetBuiltInSimpleType(System.Xml.XmlQualifiedName qualifiedName) { return default(System.Xml.Schema.XmlSchemaSimpleType); }
        public static bool IsDerivedFrom(System.Xml.Schema.XmlSchemaType derivedType, System.Xml.Schema.XmlSchemaType baseType, System.Xml.Schema.XmlSchemaDerivationMethod except) { return default(bool); }
    }
    public partial class XmlSchemaUnique : System.Xml.Schema.XmlSchemaIdentityConstraint
    {
        public XmlSchemaUnique() { }
    }
    public enum XmlSchemaUse
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 0,
        [System.Xml.Serialization.XmlEnumAttribute("optional")]
        Optional = 1,
        [System.Xml.Serialization.XmlEnumAttribute("prohibited")]
        Prohibited = 2,
        [System.Xml.Serialization.XmlEnumAttribute("required")]
        Required = 3,
    }
    public partial class XmlSchemaValidationException : System.Xml.Schema.XmlSchemaException
    {
        public XmlSchemaValidationException() { }
        protected XmlSchemaValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlSchemaValidationException(string message) { }
        public XmlSchemaValidationException(string message, System.Exception innerException) { }
        public XmlSchemaValidationException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
        public object SourceObject { get { return default(object); } }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected internal void SetSourceObject(object sourceObject) { }
    }
    [System.FlagsAttribute]
    public enum XmlSchemaValidationFlags
    {
        AllowXmlAttributes = 16,
        None = 0,
        ProcessIdentityConstraints = 8,
        ProcessInlineSchema = 1,
        ProcessSchemaLocation = 2,
        ReportValidationWarnings = 4,
    }
    public sealed partial class XmlSchemaValidator
    {
        public XmlSchemaValidator(System.Xml.XmlNameTable nameTable, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.IXmlNamespaceResolver namespaceResolver, System.Xml.Schema.XmlSchemaValidationFlags validationFlags) { }
        public System.Xml.IXmlLineInfo LineInfoProvider { get { return default(System.Xml.IXmlLineInfo); } set { } }
        public System.Uri SourceUri { get { return default(System.Uri); } set { } }
        public object ValidationEventSender { get { return default(object); } set { } }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public void AddSchema(System.Xml.Schema.XmlSchema schema) { }
        public void EndValidation() { }
        public System.Xml.Schema.XmlSchemaAttribute[] GetExpectedAttributes() { return default(System.Xml.Schema.XmlSchemaAttribute[]); }
        public System.Xml.Schema.XmlSchemaParticle[] GetExpectedParticles() { return default(System.Xml.Schema.XmlSchemaParticle[]); }
        public void GetUnspecifiedDefaultAttributes(System.Collections.ArrayList defaultAttributes) { }
        public void Initialize() { }
        public void Initialize(System.Xml.Schema.XmlSchemaObject partialValidationType) { }
        public void SkipToEndElement(System.Xml.Schema.XmlSchemaInfo schemaInfo) { }
        public object ValidateAttribute(string localName, string namespaceUri, string attributeValue, System.Xml.Schema.XmlSchemaInfo schemaInfo) { return default(object); }
        public object ValidateAttribute(string localName, string namespaceUri, System.Xml.Schema.XmlValueGetter attributeValue, System.Xml.Schema.XmlSchemaInfo schemaInfo) { return default(object); }
        public void ValidateElement(string localName, string namespaceUri, System.Xml.Schema.XmlSchemaInfo schemaInfo) { }
        public void ValidateElement(string localName, string namespaceUri, System.Xml.Schema.XmlSchemaInfo schemaInfo, string xsiType, string xsiNil, string xsiSchemaLocation, string xsiNoNamespaceSchemaLocation) { }
        public object ValidateEndElement(System.Xml.Schema.XmlSchemaInfo schemaInfo) { return default(object); }
        public object ValidateEndElement(System.Xml.Schema.XmlSchemaInfo schemaInfo, object typedValue) { return default(object); }
        public void ValidateEndOfAttributes(System.Xml.Schema.XmlSchemaInfo schemaInfo) { }
        public void ValidateText(string elementValue) { }
        public void ValidateText(System.Xml.Schema.XmlValueGetter elementValue) { }
        public void ValidateWhitespace(string elementValue) { }
        public void ValidateWhitespace(System.Xml.Schema.XmlValueGetter elementValue) { }
    }
    public enum XmlSchemaValidity
    {
        Invalid = 2,
        NotKnown = 0,
        Valid = 1,
    }
    public partial class XmlSchemaWhiteSpaceFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaWhiteSpaceFacet() { }
    }
    public partial class XmlSchemaXPath : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaXPath() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("xpath")]
        public string XPath { get { return default(string); } set { } }
    }
    public enum XmlSeverityType
    {
        Error = 0,
        Warning = 1,
    }
    public enum XmlTypeCode
    {
        AnyAtomicType = 10,
        AnyUri = 28,
        Attribute = 5,
        Base64Binary = 27,
        Boolean = 13,
        Byte = 46,
        Comment = 8,
        Date = 20,
        DateTime = 18,
        DayTimeDuration = 54,
        Decimal = 14,
        Document = 3,
        Double = 16,
        Duration = 17,
        Element = 4,
        Entity = 39,
        Float = 15,
        GDay = 24,
        GMonth = 25,
        GMonthDay = 23,
        GYear = 22,
        GYearMonth = 21,
        HexBinary = 26,
        Id = 37,
        Idref = 38,
        Int = 44,
        Integer = 40,
        Item = 1,
        Language = 33,
        Long = 43,
        Name = 35,
        Namespace = 6,
        NCName = 36,
        NegativeInteger = 42,
        NmToken = 34,
        Node = 2,
        None = 0,
        NonNegativeInteger = 47,
        NonPositiveInteger = 41,
        NormalizedString = 31,
        Notation = 30,
        PositiveInteger = 52,
        ProcessingInstruction = 7,
        QName = 29,
        Short = 45,
        String = 12,
        Text = 9,
        Time = 19,
        Token = 32,
        UnsignedByte = 51,
        UnsignedInt = 49,
        UnsignedLong = 48,
        UnsignedShort = 50,
        UntypedAtomic = 11,
        YearMonthDuration = 53,
    }
    public delegate object XmlValueGetter();
}
namespace System.Xml.Serialization
{
    [System.FlagsAttribute]
    public enum CodeGenerationOptions
    {
        [System.Xml.Serialization.XmlEnumAttribute("enableDataBinding")]
        EnableDataBinding = 16,
        [System.Xml.Serialization.XmlEnumAttribute("newAsync")]
        GenerateNewAsync = 2,
        [System.Xml.Serialization.XmlEnumAttribute("oldAsync")]
        GenerateOldAsync = 4,
        [System.Xml.Serialization.XmlEnumAttribute("order")]
        GenerateOrder = 8,
        [System.Xml.Serialization.XmlEnumAttribute("properties")]
        GenerateProperties = 1,
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 0,
    }
    public partial class CodeIdentifier
    {
        [System.ObsoleteAttribute("This class should never get constructed as it contains only static methods.")]
        public CodeIdentifier() { }
        public static string MakeCamel(string identifier) { return default(string); }
        public static string MakePascal(string identifier) { return default(string); }
        public static string MakeValid(string identifier) { return default(string); }
    }
    public partial class CodeIdentifiers
    {
        public CodeIdentifiers() { }
        public CodeIdentifiers(bool caseSensitive) { }
        public bool UseCamelCasing { get { return default(bool); } set { } }
        public void Add(string identifier, object value) { }
        public void AddReserved(string identifier) { }
        public string AddUnique(string identifier, object value) { return default(string); }
        public void Clear() { }
        public bool IsInUse(string identifier) { return default(bool); }
        public string MakeRightCase(string identifier) { return default(string); }
        public string MakeUnique(string identifier) { return default(string); }
        public void Remove(string identifier) { }
        public void RemoveReserved(string identifier) { }
        public object ToArray(System.Type type) { return default(object); }
    }
    public partial class ImportContext
    {
        public ImportContext(System.Xml.Serialization.CodeIdentifiers identifiers, bool shareTypes) { }
        public bool ShareTypes { get { return default(bool); } }
        public System.Xml.Serialization.CodeIdentifiers TypeIdentifiers { get { return default(System.Xml.Serialization.CodeIdentifiers); } }
        public System.Collections.Specialized.StringCollection Warnings { get { return default(System.Collections.Specialized.StringCollection); } }
    }
    public partial interface IXmlSerializable
    {
        System.Xml.Schema.XmlSchema GetSchema();
        void ReadXml(System.Xml.XmlReader reader);
        void WriteXml(System.Xml.XmlWriter writer);
    }
    public partial interface IXmlTextParser
    {
        bool Normalized { get; set; }
        System.Xml.WhitespaceHandling WhitespaceHandling { get; set; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class SoapAttributeAttribute : System.Attribute
    {
        public SoapAttributeAttribute() { }
        public SoapAttributeAttribute(string attributeName) { }
        public string AttributeName { get { return default(string); } set { } }
        public string DataType { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    public partial class SoapAttributeOverrides
    {
        public SoapAttributeOverrides() { }
        public System.Xml.Serialization.SoapAttributes this[System.Type type] { get { return default(System.Xml.Serialization.SoapAttributes); } }
        public System.Xml.Serialization.SoapAttributes this[System.Type type, string member] { get { return default(System.Xml.Serialization.SoapAttributes); } }
        public void Add(System.Type type, string member, System.Xml.Serialization.SoapAttributes attributes) { }
        public void Add(System.Type type, System.Xml.Serialization.SoapAttributes attributes) { }
    }
    public partial class SoapAttributes
    {
        public SoapAttributes() { }
        public SoapAttributes(System.Reflection.ICustomAttributeProvider provider) { }
        public System.Xml.Serialization.SoapAttributeAttribute SoapAttribute { get { return default(System.Xml.Serialization.SoapAttributeAttribute); } set { } }
        public object SoapDefaultValue { get { return default(object); } set { } }
        public System.Xml.Serialization.SoapElementAttribute SoapElement { get { return default(System.Xml.Serialization.SoapElementAttribute); } set { } }
        public System.Xml.Serialization.SoapEnumAttribute SoapEnum { get { return default(System.Xml.Serialization.SoapEnumAttribute); } set { } }
        public bool SoapIgnore { get { return default(bool); } set { } }
        public System.Xml.Serialization.SoapTypeAttribute SoapType { get { return default(System.Xml.Serialization.SoapTypeAttribute); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class SoapElementAttribute : System.Attribute
    {
        public SoapElementAttribute() { }
        public SoapElementAttribute(string elementName) { }
        public string DataType { get { return default(string); } set { } }
        public string ElementName { get { return default(string); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    public partial class SoapEnumAttribute : System.Attribute
    {
        public SoapEnumAttribute() { }
        public SoapEnumAttribute(string name) { }
        public string Name { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class SoapIgnoreAttribute : System.Attribute
    {
        public SoapIgnoreAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1100), AllowMultiple=true)]
    public partial class SoapIncludeAttribute : System.Attribute
    {
        public SoapIncludeAttribute(System.Type type) { }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    public partial class SoapReflectionImporter
    {
        public SoapReflectionImporter() { }
        public SoapReflectionImporter(string defaultNamespace) { }
        public SoapReflectionImporter(System.Xml.Serialization.SoapAttributeOverrides attributeOverrides) { }
        public SoapReflectionImporter(System.Xml.Serialization.SoapAttributeOverrides attributeOverrides, string defaultNamespace) { }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate, System.Xml.Serialization.XmlMappingAccess access) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, string defaultNamespace) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public void IncludeType(System.Type type) { }
        public void IncludeTypes(System.Reflection.ICustomAttributeProvider provider) { }
    }
    public partial class SoapSchemaMember
    {
        public SoapSchemaMember() { }
        public string MemberName { get { return default(string); } set { } }
        public System.Xml.XmlQualifiedName MemberType { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1052))]
    public partial class SoapTypeAttribute : System.Attribute
    {
        public SoapTypeAttribute() { }
        public SoapTypeAttribute(string typeName) { }
        public SoapTypeAttribute(string typeName, string ns) { }
        public bool IncludeInSchema { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public string TypeName { get { return default(string); } set { } }
    }
    public partial class UnreferencedObjectEventArgs : System.EventArgs
    {
        public UnreferencedObjectEventArgs(object o, string id) { }
        public string UnreferencedId { get { return default(string); } }
        public object UnreferencedObject { get { return default(object); } }
    }
    public delegate void UnreferencedObjectEventHandler(object sender, System.Xml.Serialization.UnreferencedObjectEventArgs e);
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlAnyAttributeAttribute : System.Attribute
    {
        public XmlAnyAttributeAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple=true)]
    public partial class XmlAnyElementAttribute : System.Attribute
    {
        public XmlAnyElementAttribute() { }
        public XmlAnyElementAttribute(string name) { }
        public XmlAnyElementAttribute(string name, string ns) { }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
    }
    public partial class XmlAnyElementAttributes : System.Collections.CollectionBase
    {
        public XmlAnyElementAttributes() { }
        public System.Xml.Serialization.XmlAnyElementAttribute this[int index] { get { return default(System.Xml.Serialization.XmlAnyElementAttribute); } set { } }
        public int Add(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(int); }
        public bool Contains(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(bool); }
        public void CopyTo(System.Xml.Serialization.XmlAnyElementAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(int); }
        public void Insert(int index, System.Xml.Serialization.XmlAnyElementAttribute attribute) { }
        public void Remove(System.Xml.Serialization.XmlAnyElementAttribute attribute) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlArrayAttribute : System.Attribute
    {
        public XmlArrayAttribute() { }
        public XmlArrayAttribute(string elementName) { }
        public string ElementName { get { return default(string); } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple=true)]
    public partial class XmlArrayItemAttribute : System.Attribute
    {
        public XmlArrayItemAttribute() { }
        public XmlArrayItemAttribute(string elementName) { }
        public XmlArrayItemAttribute(string elementName, System.Type type) { }
        public XmlArrayItemAttribute(System.Type type) { }
        public string DataType { get { return default(string); } set { } }
        public string ElementName { get { return default(string); } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public int NestingLevel { get { return default(int); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    public partial class XmlArrayItemAttributes : System.Collections.CollectionBase
    {
        public XmlArrayItemAttributes() { }
        public System.Xml.Serialization.XmlArrayItemAttribute this[int index] { get { return default(System.Xml.Serialization.XmlArrayItemAttribute); } set { } }
        public int Add(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(int); }
        public bool Contains(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(bool); }
        public void CopyTo(System.Xml.Serialization.XmlArrayItemAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(int); }
        public void Insert(int index, System.Xml.Serialization.XmlArrayItemAttribute attribute) { }
        public void Remove(System.Xml.Serialization.XmlArrayItemAttribute attribute) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlAttributeAttribute : System.Attribute
    {
        public XmlAttributeAttribute() { }
        public XmlAttributeAttribute(string attributeName) { }
        public XmlAttributeAttribute(string attributeName, System.Type type) { }
        public XmlAttributeAttribute(System.Type type) { }
        public string AttributeName { get { return default(string); } set { } }
        public string DataType { get { return default(string); } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    public partial class XmlAttributeEventArgs : System.EventArgs
    {
        internal XmlAttributeEventArgs() { }
        public System.Xml.XmlAttribute Attr { get { return default(System.Xml.XmlAttribute); } }
        public string ExpectedAttributes { get { return default(string); } }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public object ObjectBeingDeserialized { get { return default(object); } }
    }
    public delegate void XmlAttributeEventHandler(object sender, System.Xml.Serialization.XmlAttributeEventArgs e);
    public partial class XmlAttributeOverrides
    {
        public XmlAttributeOverrides() { }
        public System.Xml.Serialization.XmlAttributes this[System.Type type] { get { return default(System.Xml.Serialization.XmlAttributes); } }
        public System.Xml.Serialization.XmlAttributes this[System.Type type, string member] { get { return default(System.Xml.Serialization.XmlAttributes); } }
        public void Add(System.Type type, string member, System.Xml.Serialization.XmlAttributes attributes) { }
        public void Add(System.Type type, System.Xml.Serialization.XmlAttributes attributes) { }
    }
    public partial class XmlAttributes
    {
        public XmlAttributes() { }
        public XmlAttributes(System.Reflection.ICustomAttributeProvider provider) { }
        public System.Xml.Serialization.XmlAnyAttributeAttribute XmlAnyAttribute { get { return default(System.Xml.Serialization.XmlAnyAttributeAttribute); } set { } }
        public System.Xml.Serialization.XmlAnyElementAttributes XmlAnyElements { get { return default(System.Xml.Serialization.XmlAnyElementAttributes); } }
        public System.Xml.Serialization.XmlArrayAttribute XmlArray { get { return default(System.Xml.Serialization.XmlArrayAttribute); } set { } }
        public System.Xml.Serialization.XmlArrayItemAttributes XmlArrayItems { get { return default(System.Xml.Serialization.XmlArrayItemAttributes); } }
        public System.Xml.Serialization.XmlAttributeAttribute XmlAttribute { get { return default(System.Xml.Serialization.XmlAttributeAttribute); } set { } }
        public System.Xml.Serialization.XmlChoiceIdentifierAttribute XmlChoiceIdentifier { get { return default(System.Xml.Serialization.XmlChoiceIdentifierAttribute); } }
        public object XmlDefaultValue { get { return default(object); } set { } }
        public System.Xml.Serialization.XmlElementAttributes XmlElements { get { return default(System.Xml.Serialization.XmlElementAttributes); } }
        public System.Xml.Serialization.XmlEnumAttribute XmlEnum { get { return default(System.Xml.Serialization.XmlEnumAttribute); } set { } }
        public bool XmlIgnore { get { return default(bool); } set { } }
        public bool Xmlns { get { return default(bool); } set { } }
        public System.Xml.Serialization.XmlRootAttribute XmlRoot { get { return default(System.Xml.Serialization.XmlRootAttribute); } set { } }
        public System.Xml.Serialization.XmlTextAttribute XmlText { get { return default(System.Xml.Serialization.XmlTextAttribute); } set { } }
        public System.Xml.Serialization.XmlTypeAttribute XmlType { get { return default(System.Xml.Serialization.XmlTypeAttribute); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlChoiceIdentifierAttribute : System.Attribute
    {
        public XmlChoiceIdentifierAttribute() { }
        public XmlChoiceIdentifierAttribute(string name) { }
        public string MemberName { get { return default(string); } set { } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct XmlDeserializationEvents
    {
        public System.Xml.Serialization.XmlAttributeEventHandler OnUnknownAttribute { get { return default(System.Xml.Serialization.XmlAttributeEventHandler); } set { } }
        public System.Xml.Serialization.XmlElementEventHandler OnUnknownElement { get { return default(System.Xml.Serialization.XmlElementEventHandler); } set { } }
        public System.Xml.Serialization.XmlNodeEventHandler OnUnknownNode { get { return default(System.Xml.Serialization.XmlNodeEventHandler); } set { } }
        public System.Xml.Serialization.UnreferencedObjectEventHandler OnUnreferencedObject { get { return default(System.Xml.Serialization.UnreferencedObjectEventHandler); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple=true)]
    public partial class XmlElementAttribute : System.Attribute
    {
        public XmlElementAttribute() { }
        public XmlElementAttribute(string elementName) { }
        public XmlElementAttribute(string elementName, System.Type type) { }
        public XmlElementAttribute(System.Type type) { }
        public string DataType { get { return default(string); } set { } }
        public string ElementName { get { return default(string); } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    public partial class XmlElementAttributes : System.Collections.CollectionBase
    {
        public XmlElementAttributes() { }
        public System.Xml.Serialization.XmlElementAttribute this[int index] { get { return default(System.Xml.Serialization.XmlElementAttribute); } set { } }
        public int Add(System.Xml.Serialization.XmlElementAttribute attribute) { return default(int); }
        public bool Contains(System.Xml.Serialization.XmlElementAttribute attribute) { return default(bool); }
        public void CopyTo(System.Xml.Serialization.XmlElementAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlElementAttribute attribute) { return default(int); }
        public void Insert(int index, System.Xml.Serialization.XmlElementAttribute attribute) { }
        public void Remove(System.Xml.Serialization.XmlElementAttribute attribute) { }
    }
    public partial class XmlElementEventArgs : System.EventArgs
    {
        internal XmlElementEventArgs() { }
        public System.Xml.XmlElement Element { get { return default(System.Xml.XmlElement); } }
        public string ExpectedElements { get { return default(string); } }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public object ObjectBeingDeserialized { get { return default(object); } }
    }
    public delegate void XmlElementEventHandler(object sender, System.Xml.Serialization.XmlElementEventArgs e);
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    public partial class XmlEnumAttribute : System.Attribute
    {
        public XmlEnumAttribute() { }
        public XmlEnumAttribute(string name) { }
        public string Name { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlIgnoreAttribute : System.Attribute
    {
        public XmlIgnoreAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1100), AllowMultiple=true)]
    public partial class XmlIncludeAttribute : System.Attribute
    {
        public XmlIncludeAttribute(System.Type type) { }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    public abstract partial class XmlMapping
    {
        internal XmlMapping() { }
        public string ElementName { get { return default(string); } }
        public string Namespace { get { return default(string); } }
        public string XsdElementName { get { return default(string); } }
        public void SetKey(string key) { }
    }
    [System.FlagsAttribute]
    public enum XmlMappingAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
    }
    public partial class XmlMemberMapping
    {
        internal XmlMemberMapping() { }
        public bool Any { get { return default(bool); } }
        public bool CheckSpecified { get { return default(bool); } }
        public string ElementName { get { return default(string); } }
        public string MemberName { get { return default(string); } }
        public string Namespace { get { return default(string); } }
        public string TypeFullName { get { return default(string); } }
        public string TypeName { get { return default(string); } }
        public string TypeNamespace { get { return default(string); } }
        public string XsdElementName { get { return default(string); } }
    }
    public partial class XmlMembersMapping : System.Xml.Serialization.XmlMapping
    {
        internal XmlMembersMapping() { }
        public int Count { get { return default(int); } }
        public System.Xml.Serialization.XmlMemberMapping this[int index] { get { return default(System.Xml.Serialization.XmlMemberMapping); } }
        public string TypeName { get { return default(string); } }
        public string TypeNamespace { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlNamespaceDeclarationsAttribute : System.Attribute
    {
        public XmlNamespaceDeclarationsAttribute() { }
    }
    public partial class XmlNodeEventArgs : System.EventArgs
    {
        internal XmlNodeEventArgs() { }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public string LocalName { get { return default(string); } }
        public string Name { get { return default(string); } }
        public string NamespaceURI { get { return default(string); } }
        public System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public object ObjectBeingDeserialized { get { return default(object); } }
        public string Text { get { return default(string); } }
    }
    public delegate void XmlNodeEventHandler(object sender, System.Xml.Serialization.XmlNodeEventArgs e);
    public partial class XmlReflectionImporter
    {
        public XmlReflectionImporter() { }
        public XmlReflectionImporter(string defaultNamespace) { }
        public XmlReflectionImporter(System.Xml.Serialization.XmlAttributeOverrides attributeOverrides) { }
        public XmlReflectionImporter(System.Xml.Serialization.XmlAttributeOverrides attributeOverrides, string defaultNamespace) { }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel, System.Xml.Serialization.XmlMappingAccess access) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, string defaultNamespace) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public void IncludeType(System.Type type) { }
        public void IncludeTypes(System.Reflection.ICustomAttributeProvider provider) { }
    }
    public partial class XmlReflectionMember
    {
        public XmlReflectionMember() { }
        public bool IsReturnValue { get { return default(bool); } set { } }
        public string MemberName { get { return default(string); } set { } }
        public System.Type MemberType { get { return default(System.Type); } set { } }
        public bool OverrideIsNullable { get { return default(bool); } set { } }
        public System.Xml.Serialization.SoapAttributes SoapAttributes { get { return default(System.Xml.Serialization.SoapAttributes); } set { } }
        public System.Xml.Serialization.XmlAttributes XmlAttributes { get { return default(System.Xml.Serialization.XmlAttributes); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(9244))]
    public partial class XmlRootAttribute : System.Attribute
    {
        public XmlRootAttribute() { }
        public XmlRootAttribute(string elementName) { }
        public string DataType { get { return default(string); } set { } }
        public string ElementName { get { return default(string); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    public partial class XmlSchemaEnumerator : System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema>, System.Collections.IEnumerator, System.IDisposable
    {
        public XmlSchemaEnumerator(System.Xml.Serialization.XmlSchemas list) { }
        public System.Xml.Schema.XmlSchema Current { get { return default(System.Xml.Schema.XmlSchema); } }
        object System.Collections.IEnumerator.Current { get { return default(object); } }
        public void Dispose() { }
        public bool MoveNext() { return default(bool); }
        void System.Collections.IEnumerator.Reset() { }
    }
    public partial class XmlSchemaExporter
    {
        public XmlSchemaExporter(System.Xml.Serialization.XmlSchemas schemas) { }
        public string ExportAnyType(string ns) { return default(string); }
        public string ExportAnyType(System.Xml.Serialization.XmlMembersMapping members) { return default(string); }
        public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { }
        public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping, bool exportEnclosingType) { }
        public System.Xml.XmlQualifiedName ExportTypeMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { return default(System.Xml.XmlQualifiedName); }
        public void ExportTypeMapping(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
    }
    public partial class XmlSchemaImporter
    {
        public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas) { }
        public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeIdentifiers typeIdentifiers) { }
        public System.Xml.Serialization.XmlMembersMapping ImportAnyType(System.Xml.XmlQualifiedName typeName, string elementName) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type baseType) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string ns, System.Xml.Serialization.SoapSchemaMember[] members) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName name) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName[] names) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName[] names, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlMembersMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName, System.Type baseType) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlTypeMapping); }
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Xml.XmlQualifiedName name) { return default(System.Xml.Serialization.XmlTypeMapping); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1036))]
    public sealed partial class XmlSchemaProviderAttribute : System.Attribute
    {
        public XmlSchemaProviderAttribute(string methodName) { }
        public bool IsAny { get { return default(bool); } set { } }
        public string MethodName { get { return default(string); } }
    }
    public partial class XmlSchemas : System.Collections.CollectionBase, System.Collections.Generic.IEnumerable<System.Xml.Schema.XmlSchema>, System.Collections.IEnumerable
    {
        public XmlSchemas() { }
        public bool IsCompiled { get { return default(bool); } }
        public System.Xml.Schema.XmlSchema this[int index] { get { return default(System.Xml.Schema.XmlSchema); } set { } }
        public System.Xml.Schema.XmlSchema this[string ns] { get { return default(System.Xml.Schema.XmlSchema); } }
        public int Add(System.Xml.Schema.XmlSchema schema) { return default(int); }
        public int Add(System.Xml.Schema.XmlSchema schema, System.Uri baseUri) { return default(int); }
        public void Add(System.Xml.Serialization.XmlSchemas schemas) { }
        public void AddReference(System.Xml.Schema.XmlSchema schema) { }
        public void Compile(System.Xml.Schema.ValidationEventHandler handler, bool fullCompile) { }
        public bool Contains(string targetNamespace) { return default(bool); }
        public bool Contains(System.Xml.Schema.XmlSchema schema) { return default(bool); }
        public void CopyTo(System.Xml.Schema.XmlSchema[] array, int index) { }
        public object Find(System.Xml.XmlQualifiedName name, System.Type type) { return default(object); }
        public System.Collections.IList GetSchemas(string ns) { return default(System.Collections.IList); }
        public int IndexOf(System.Xml.Schema.XmlSchema schema) { return default(int); }
        public void Insert(int index, System.Xml.Schema.XmlSchema schema) { }
        public static bool IsDataSet(System.Xml.Schema.XmlSchema schema) { return default(bool); }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(System.Xml.Schema.XmlSchema schema) { }
        System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema> System.Collections.Generic.IEnumerable<System.Xml.Schema.XmlSchema>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema>); }
    }
    public delegate void XmlSerializationCollectionFixupCallback(object collection, object collectionItems);
    public delegate void XmlSerializationFixupCallback(object fixup);
    public abstract partial class XmlSerializationGeneratedCode
    {
        protected XmlSerializationGeneratedCode() { }
    }
    public delegate object XmlSerializationReadCallback();
    public abstract partial class XmlSerializationReader : System.Xml.Serialization.XmlSerializationGeneratedCode
    {
        protected XmlSerializationReader() { }
        protected bool DecodeName { get { return default(bool); } set { } }
        protected System.Xml.XmlDocument Document { get { return default(System.Xml.XmlDocument); } }
        protected bool IsReturnValue { get { return default(bool); } set { } }
        protected System.Xml.XmlReader Reader { get { return default(System.Xml.XmlReader); } }
        protected int ReaderCount { get { return default(int); } }
        protected void AddFixup(System.Xml.Serialization.XmlSerializationReader.CollectionFixup fixup) { }
        protected void AddFixup(System.Xml.Serialization.XmlSerializationReader.Fixup fixup) { }
        protected void AddReadCallback(string name, string ns, System.Type type, System.Xml.Serialization.XmlSerializationReadCallback read) { }
        protected void AddTarget(string id, object o) { }
        protected void CheckReaderCount(ref int whileIterations, ref int readerCount) { }
        protected string CollapseWhitespace(string value) { return default(string); }
        protected System.Exception CreateAbstractTypeException(string name, string ns) { return default(System.Exception); }
        protected System.Exception CreateBadDerivationException(string xsdDerived, string nsDerived, string xsdBase, string nsBase, string clrDerived, string clrBase) { return default(System.Exception); }
        protected System.Exception CreateCtorHasSecurityException(string typeName) { return default(System.Exception); }
        protected System.Exception CreateInaccessibleConstructorException(string typeName) { return default(System.Exception); }
        protected System.Exception CreateInvalidCastException(System.Type type, object value) { return default(System.Exception); }
        protected System.Exception CreateInvalidCastException(System.Type type, object value, string id) { return default(System.Exception); }
        protected System.Exception CreateMissingIXmlSerializableType(string name, string ns, string clrType) { return default(System.Exception); }
        protected System.Exception CreateReadOnlyCollectionException(string name) { return default(System.Exception); }
        protected System.Exception CreateUnknownConstantException(string value, System.Type enumType) { return default(System.Exception); }
        protected System.Exception CreateUnknownNodeException() { return default(System.Exception); }
        protected System.Exception CreateUnknownTypeException(System.Xml.XmlQualifiedName type) { return default(System.Exception); }
        protected System.Array EnsureArrayIndex(System.Array a, int index, System.Type elementType) { return default(System.Array); }
        protected void FixupArrayRefs(object fixup) { }
        protected int GetArrayLength(string name, string ns) { return default(int); }
        protected bool GetNullAttr() { return default(bool); }
        protected object GetTarget(string id) { return default(object); }
        protected System.Xml.XmlQualifiedName GetXsiType() { return default(System.Xml.XmlQualifiedName); }
        protected abstract void InitCallbacks();
        protected abstract void InitIDs();
        protected bool IsXmlnsAttribute(string name) { return default(bool); }
        protected void ParseWsdlArrayType(System.Xml.XmlAttribute attr) { }
        protected System.Xml.XmlQualifiedName ReadElementQualifiedName() { return default(System.Xml.XmlQualifiedName); }
        protected void ReadEndElement() { }
        protected bool ReadNull() { return default(bool); }
        protected System.Xml.XmlQualifiedName ReadNullableQualifiedName() { return default(System.Xml.XmlQualifiedName); }
        protected string ReadNullableString() { return default(string); }
        protected bool ReadReference(out string fixupReference) { fixupReference = default(string); return default(bool); }
        protected object ReadReferencedElement() { return default(object); }
        protected object ReadReferencedElement(string name, string ns) { return default(object); }
        protected void ReadReferencedElements() { }
        protected object ReadReferencingElement(string name, string ns, bool elementCanBeType, out string fixupReference) { fixupReference = default(string); return default(object); }
        protected object ReadReferencingElement(string name, string ns, out string fixupReference) { fixupReference = default(string); return default(object); }
        protected object ReadReferencingElement(out string fixupReference) { fixupReference = default(string); return default(object); }
        protected System.Xml.Serialization.IXmlSerializable ReadSerializable(System.Xml.Serialization.IXmlSerializable serializable) { return default(System.Xml.Serialization.IXmlSerializable); }
        protected string ReadString(string value) { return default(string); }
        protected string ReadString(string value, bool trim) { return default(string); }
        protected object ReadTypedNull(System.Xml.XmlQualifiedName type) { return default(object); }
        protected object ReadTypedPrimitive(System.Xml.XmlQualifiedName type) { return default(object); }
        protected System.Xml.XmlDocument ReadXmlDocument(bool wrapped) { return default(System.Xml.XmlDocument); }
        protected System.Xml.XmlNode ReadXmlNode(bool wrapped) { return default(System.Xml.XmlNode); }
        protected void Referenced(object o) { }
        protected static System.Reflection.Assembly ResolveDynamicAssembly(string assemblyFullName) { return default(System.Reflection.Assembly); }
        protected System.Array ShrinkArray(System.Array a, int length, System.Type elementType, bool isNullable) { return default(System.Array); }
        protected byte[] ToByteArrayBase64(bool isNull) { return default(byte[]); }
        protected static byte[] ToByteArrayBase64(string value) { return default(byte[]); }
        protected byte[] ToByteArrayHex(bool isNull) { return default(byte[]); }
        protected static byte[] ToByteArrayHex(string value) { return default(byte[]); }
        protected static char ToChar(string value) { return default(char); }
        protected static System.DateTime ToDate(string value) { return default(System.DateTime); }
        protected static System.DateTime ToDateTime(string value) { return default(System.DateTime); }
        protected static long ToEnum(string value, System.Collections.Hashtable h, string typeName) { return default(long); }
        protected static System.DateTime ToTime(string value) { return default(System.DateTime); }
        protected static string ToXmlName(string value) { return default(string); }
        protected static string ToXmlNCName(string value) { return default(string); }
        protected static string ToXmlNmToken(string value) { return default(string); }
        protected static string ToXmlNmTokens(string value) { return default(string); }
        protected System.Xml.XmlQualifiedName ToXmlQualifiedName(string value) { return default(System.Xml.XmlQualifiedName); }
        protected void UnknownAttribute(object o, System.Xml.XmlAttribute attr) { }
        protected void UnknownAttribute(object o, System.Xml.XmlAttribute attr, string qnames) { }
        protected void UnknownElement(object o, System.Xml.XmlElement elem) { }
        protected void UnknownElement(object o, System.Xml.XmlElement elem, string qnames) { }
        protected void UnknownNode(object o) { }
        protected void UnknownNode(object o, string qnames) { }
        protected void UnreferencedObject(string id, object o) { }
        protected partial class CollectionFixup
        {
            public CollectionFixup(object collection, System.Xml.Serialization.XmlSerializationCollectionFixupCallback callback, object collectionItems) { }
            public System.Xml.Serialization.XmlSerializationCollectionFixupCallback Callback { get { return default(System.Xml.Serialization.XmlSerializationCollectionFixupCallback); } }
            public object Collection { get { return default(object); } }
            public object CollectionItems { get { return default(object); } }
        }
        protected partial class Fixup
        {
            public Fixup(object o, System.Xml.Serialization.XmlSerializationFixupCallback callback, int count) { }
            public Fixup(object o, System.Xml.Serialization.XmlSerializationFixupCallback callback, string[] ids) { }
            public System.Xml.Serialization.XmlSerializationFixupCallback Callback { get { return default(System.Xml.Serialization.XmlSerializationFixupCallback); } }
            public string[] Ids { get { return default(string[]); } }
            public object Source { get { return default(object); } set { } }
        }
    }
    public delegate void XmlSerializationWriteCallback(object o);
    public abstract partial class XmlSerializationWriter : System.Xml.Serialization.XmlSerializationGeneratedCode
    {
        protected XmlSerializationWriter() { }
        protected bool EscapeName { get { return default(bool); } set { } }
        protected System.Collections.ArrayList Namespaces { get { return default(System.Collections.ArrayList); } set { } }
        protected System.Xml.XmlWriter Writer { get { return default(System.Xml.XmlWriter); } set { } }
        protected void AddWriteCallback(System.Type type, string typeName, string typeNs, System.Xml.Serialization.XmlSerializationWriteCallback callback) { }
        protected System.Exception CreateChoiceIdentifierValueException(string value, string identifier, string name, string ns) { return default(System.Exception); }
        protected System.Exception CreateInvalidAnyTypeException(object o) { return default(System.Exception); }
        protected System.Exception CreateInvalidAnyTypeException(System.Type type) { return default(System.Exception); }
        protected System.Exception CreateInvalidChoiceIdentifierValueException(string type, string identifier) { return default(System.Exception); }
        protected System.Exception CreateInvalidEnumValueException(object value, string typeName) { return default(System.Exception); }
        protected System.Exception CreateMismatchChoiceException(string value, string elementName, string enumValue) { return default(System.Exception); }
        protected System.Exception CreateUnknownAnyElementException(string name, string ns) { return default(System.Exception); }
        protected System.Exception CreateUnknownTypeException(object o) { return default(System.Exception); }
        protected System.Exception CreateUnknownTypeException(System.Type type) { return default(System.Exception); }
        protected static byte[] FromByteArrayBase64(byte[] value) { return default(byte[]); }
        protected static string FromByteArrayHex(byte[] value) { return default(string); }
        protected static string FromChar(char value) { return default(string); }
        protected static string FromDate(System.DateTime value) { return default(string); }
        protected static string FromDateTime(System.DateTime value) { return default(string); }
        protected static string FromEnum(long value, string[] values, long[] ids) { return default(string); }
        protected static string FromEnum(long value, string[] values, long[] ids, string typeName) { return default(string); }
        protected static string FromTime(System.DateTime value) { return default(string); }
        protected static string FromXmlName(string name) { return default(string); }
        protected static string FromXmlNCName(string ncName) { return default(string); }
        protected static string FromXmlNmToken(string nmToken) { return default(string); }
        protected static string FromXmlNmTokens(string nmTokens) { return default(string); }
        protected string FromXmlQualifiedName(System.Xml.XmlQualifiedName xmlQualifiedName) { return default(string); }
        protected string FromXmlQualifiedName(System.Xml.XmlQualifiedName xmlQualifiedName, bool ignoreEmpty) { return default(string); }
        protected abstract void InitCallbacks();
        protected static System.Reflection.Assembly ResolveDynamicAssembly(string assemblyFullName) { return default(System.Reflection.Assembly); }
        protected void TopLevelElement() { }
        protected void WriteAttribute(string localName, byte[] value) { }
        protected void WriteAttribute(string localName, string value) { }
        protected void WriteAttribute(string localName, string ns, byte[] value) { }
        protected void WriteAttribute(string localName, string ns, string value) { }
        protected void WriteAttribute(string prefix, string localName, string ns, string value) { }
        protected void WriteElementEncoded(System.Xml.XmlNode node, string name, string ns, bool isNullable, bool any) { }
        protected void WriteElementLiteral(System.Xml.XmlNode node, string name, string ns, bool isNullable, bool any) { }
        protected void WriteElementQualifiedName(string localName, string ns, System.Xml.XmlQualifiedName value) { }
        protected void WriteElementQualifiedName(string localName, string ns, System.Xml.XmlQualifiedName value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementQualifiedName(string localName, System.Xml.XmlQualifiedName value) { }
        protected void WriteElementQualifiedName(string localName, System.Xml.XmlQualifiedName value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementString(string localName, string value) { }
        protected void WriteElementString(string localName, string ns, string value) { }
        protected void WriteElementString(string localName, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementString(string localName, string value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementStringRaw(string localName, byte[] value) { }
        protected void WriteElementStringRaw(string localName, byte[] value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementStringRaw(string localName, string value) { }
        protected void WriteElementStringRaw(string localName, string ns, byte[] value) { }
        protected void WriteElementStringRaw(string localName, string ns, byte[] value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementStringRaw(string localName, string ns, string value) { }
        protected void WriteElementStringRaw(string localName, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteElementStringRaw(string localName, string value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteEmptyTag(string name) { }
        protected void WriteEmptyTag(string name, string ns) { }
        protected void WriteEndElement() { }
        protected void WriteEndElement(object o) { }
        protected void WriteId(object o) { }
        protected void WriteNamespaceDeclarations(System.Xml.Serialization.XmlSerializerNamespaces xmlns) { }
        protected void WriteNullableQualifiedNameEncoded(string name, string ns, System.Xml.XmlQualifiedName value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteNullableQualifiedNameLiteral(string name, string ns, System.Xml.XmlQualifiedName value) { }
        protected void WriteNullableStringEncoded(string name, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteNullableStringEncodedRaw(string name, string ns, byte[] value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteNullableStringEncodedRaw(string name, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
        protected void WriteNullableStringLiteral(string name, string ns, string value) { }
        protected void WriteNullableStringLiteralRaw(string name, string ns, byte[] value) { }
        protected void WriteNullableStringLiteralRaw(string name, string ns, string value) { }
        protected void WriteNullTagEncoded(string name) { }
        protected void WriteNullTagEncoded(string name, string ns) { }
        protected void WriteNullTagLiteral(string name) { }
        protected void WriteNullTagLiteral(string name, string ns) { }
        protected void WritePotentiallyReferencingElement(string n, string ns, object o) { }
        protected void WritePotentiallyReferencingElement(string n, string ns, object o, System.Type ambientType) { }
        protected void WritePotentiallyReferencingElement(string n, string ns, object o, System.Type ambientType, bool suppressReference) { }
        protected void WritePotentiallyReferencingElement(string n, string ns, object o, System.Type ambientType, bool suppressReference, bool isNullable) { }
        protected void WriteReferencedElements() { }
        protected void WriteReferencingElement(string n, string ns, object o) { }
        protected void WriteReferencingElement(string n, string ns, object o, bool isNullable) { }
        protected void WriteRpcResult(string name, string ns) { }
        protected void WriteSerializable(System.Xml.Serialization.IXmlSerializable serializable, string name, string ns, bool isNullable) { }
        protected void WriteSerializable(System.Xml.Serialization.IXmlSerializable serializable, string name, string ns, bool isNullable, bool wrapped) { }
        protected void WriteStartDocument() { }
        protected void WriteStartElement(string name) { }
        protected void WriteStartElement(string name, string ns) { }
        protected void WriteStartElement(string name, string ns, bool writePrefixed) { }
        protected void WriteStartElement(string name, string ns, object o) { }
        protected void WriteStartElement(string name, string ns, object o, bool writePrefixed) { }
        protected void WriteStartElement(string name, string ns, object o, bool writePrefixed, System.Xml.Serialization.XmlSerializerNamespaces xmlns) { }
        protected void WriteTypedPrimitive(string name, string ns, object o, bool xsiType) { }
        protected void WriteValue(byte[] value) { }
        protected void WriteValue(string value) { }
        protected void WriteXmlAttribute(System.Xml.XmlNode node) { }
        protected void WriteXmlAttribute(System.Xml.XmlNode node, object container) { }
        protected void WriteXsiType(string name, string ns) { }
    }
    public partial class XmlSerializer
    {
        protected XmlSerializer() { }
        public XmlSerializer(System.Type type) { }
        public XmlSerializer(System.Type type, string defaultNamespace) { }
        public XmlSerializer(System.Type type, System.Type[] extraTypes) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace, string location, System.Security.Policy.Evidence evidence) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { }
        public XmlSerializer(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
        public event System.Xml.Serialization.XmlAttributeEventHandler UnknownAttribute { add { } remove { } }
        public event System.Xml.Serialization.XmlElementEventHandler UnknownElement { add { } remove { } }
        public event System.Xml.Serialization.XmlNodeEventHandler UnknownNode { add { } remove { } }
        public event System.Xml.Serialization.UnreferencedObjectEventHandler UnreferencedObject { add { } remove { } }
        public virtual bool CanDeserialize(System.Xml.XmlReader xmlReader) { return default(bool); }
        protected virtual System.Xml.Serialization.XmlSerializationReader CreateReader() { return default(System.Xml.Serialization.XmlSerializationReader); }
        protected virtual System.Xml.Serialization.XmlSerializationWriter CreateWriter() { return default(System.Xml.Serialization.XmlSerializationWriter); }
        public object Deserialize(System.IO.Stream stream) { return default(object); }
        public object Deserialize(System.IO.TextReader textReader) { return default(object); }
        protected virtual object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) { return default(object); }
        public object Deserialize(System.Xml.XmlReader xmlReader) { return default(object); }
        public object Deserialize(System.Xml.XmlReader xmlReader, string encodingStyle) { return default(object); }
        public object Deserialize(System.Xml.XmlReader xmlReader, string encodingStyle, System.Xml.Serialization.XmlDeserializationEvents events) { return default(object); }
        public object Deserialize(System.Xml.XmlReader xmlReader, System.Xml.Serialization.XmlDeserializationEvents events) { return default(object); }
        public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[] mappings) { return default(System.Xml.Serialization.XmlSerializer[]); }
        public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[] mappings, System.Security.Policy.Evidence evidence) { return default(System.Xml.Serialization.XmlSerializer[]); }
        public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[] mappings, System.Type type) { return default(System.Xml.Serialization.XmlSerializer[]); }
        public static System.Xml.Serialization.XmlSerializer[] FromTypes(System.Type[] types) { return default(System.Xml.Serialization.XmlSerializer[]); }
        public static string GetXmlSerializerAssemblyName(System.Type type) { return default(string); }
        public static string GetXmlSerializerAssemblyName(System.Type type, string defaultNamespace) { return default(string); }
        public void Serialize(System.IO.Stream stream, object o) { }
        public void Serialize(System.IO.Stream stream, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public void Serialize(System.IO.TextWriter textWriter, object o) { }
        public void Serialize(System.IO.TextWriter textWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        protected virtual void Serialize(object o, System.Xml.Serialization.XmlSerializationWriter writer) { }
        public void Serialize(System.Xml.XmlWriter xmlWriter, object o) { }
        public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces, string encodingStyle) { }
        public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces, string encodingStyle, string id) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1052))]
    public sealed partial class XmlSerializerAssemblyAttribute : System.Attribute
    {
        public XmlSerializerAssemblyAttribute() { }
        public XmlSerializerAssemblyAttribute(string assemblyName) { }
        public XmlSerializerAssemblyAttribute(string assemblyName, string codeBase) { }
        public string AssemblyName { get { return default(string); } set { } }
        public string CodeBase { get { return default(string); } set { } }
    }
    public partial class XmlSerializerFactory
    {
        public XmlSerializerFactory() { }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, string defaultNamespace) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Type[] extraTypes) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace, string location, System.Security.Policy.Evidence evidence) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { return default(System.Xml.Serialization.XmlSerializer); }
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { return default(System.Xml.Serialization.XmlSerializer); }
    }
    public abstract partial class XmlSerializerImplementation
    {
        protected XmlSerializerImplementation() { }
        public virtual System.Xml.Serialization.XmlSerializationReader Reader { get { return default(System.Xml.Serialization.XmlSerializationReader); } }
        public virtual System.Collections.Hashtable ReadMethods { get { return default(System.Collections.Hashtable); } }
        public virtual System.Collections.Hashtable TypedSerializers { get { return default(System.Collections.Hashtable); } }
        public virtual System.Collections.Hashtable WriteMethods { get { return default(System.Collections.Hashtable); } }
        public virtual System.Xml.Serialization.XmlSerializationWriter Writer { get { return default(System.Xml.Serialization.XmlSerializationWriter); } }
        public virtual bool CanSerialize(System.Type type) { return default(bool); }
        public virtual System.Xml.Serialization.XmlSerializer GetSerializer(System.Type type) { return default(System.Xml.Serialization.XmlSerializer); }
    }
    public partial class XmlSerializerNamespaces
    {
        public XmlSerializerNamespaces() { }
        public XmlSerializerNamespaces(System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public XmlSerializerNamespaces(System.Xml.XmlQualifiedName[] namespaces) { }
        public int Count { get { return default(int); } }
        public void Add(string prefix, string ns) { }
        public System.Xml.XmlQualifiedName[] ToArray() { return default(System.Xml.XmlQualifiedName[]); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1))]
    public sealed partial class XmlSerializerVersionAttribute : System.Attribute
    {
        public XmlSerializerVersionAttribute() { }
        public XmlSerializerVersionAttribute(System.Type type) { }
        public string Namespace { get { return default(string); } set { } }
        public string ParentAssemblyId { get { return default(string); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
        public string Version { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlTextAttribute : System.Attribute
    {
        public XmlTextAttribute() { }
        public XmlTextAttribute(System.Type type) { }
        public string DataType { get { return default(string); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1052))]
    public partial class XmlTypeAttribute : System.Attribute
    {
        public XmlTypeAttribute() { }
        public XmlTypeAttribute(string typeName) { }
        public bool AnonymousType { get { return default(bool); } set { } }
        public bool IncludeInSchema { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public string TypeName { get { return default(string); } set { } }
    }
    public partial class XmlTypeMapping : System.Xml.Serialization.XmlMapping
    {
        internal XmlTypeMapping() { }
        public string TypeFullName { get { return default(string); } }
        public string TypeName { get { return default(string); } }
        public string XsdTypeName { get { return default(string); } }
        public string XsdTypeNamespace { get { return default(string); } }
    }
}
namespace System.Xml.XmlConfiguration
{
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class XmlReaderSection
    {
        public XmlReaderSection() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class XsltConfigSection
    {
        public XsltConfigSection() { }
    }
}
namespace System.Xml.XPath
{
    public partial interface IXPathNavigable
    {
        System.Xml.XPath.XPathNavigator CreateNavigator();
    }
    public enum XmlCaseOrder
    {
        LowerFirst = 2,
        None = 0,
        UpperFirst = 1,
    }
    public enum XmlDataType
    {
        Number = 2,
        Text = 1,
    }
    public enum XmlSortOrder
    {
        Ascending = 1,
        Descending = 2,
    }
    public partial class XPathDocument : System.Xml.XPath.IXPathNavigable
    {
        public XPathDocument(System.IO.Stream stream) { }
        public XPathDocument(System.IO.TextReader textReader) { }
        public XPathDocument(string uri) { }
        public XPathDocument(string uri, System.Xml.XmlSpace space) { }
        public XPathDocument(System.Xml.XmlReader reader) { }
        public XPathDocument(System.Xml.XmlReader reader, System.Xml.XmlSpace space) { }
        public System.Xml.XPath.XPathNavigator CreateNavigator() { return default(System.Xml.XPath.XPathNavigator); }
    }
    public partial class XPathException : System.SystemException
    {
        public XPathException() { }
        protected XPathException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XPathException(string message) { }
        public XPathException(string message, System.Exception innerException) { }
        public override string Message { get { return default(string); } }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class XPathExpression
    {
        internal XPathExpression() { }
        public abstract string Expression { get; }
        public abstract System.Xml.XPath.XPathResultType ReturnType { get; }
        public abstract void AddSort(object expr, System.Collections.IComparer comparer);
        public abstract void AddSort(object expr, System.Xml.XPath.XmlSortOrder order, System.Xml.XPath.XmlCaseOrder caseOrder, string lang, System.Xml.XPath.XmlDataType dataType);
        public abstract System.Xml.XPath.XPathExpression Clone();
        public static System.Xml.XPath.XPathExpression Compile(string xpath) { return default(System.Xml.XPath.XPathExpression); }
        public static System.Xml.XPath.XPathExpression Compile(string xpath, System.Xml.IXmlNamespaceResolver nsResolver) { return default(System.Xml.XPath.XPathExpression); }
        public abstract void SetContext(System.Xml.IXmlNamespaceResolver nsResolver);
        public abstract void SetContext(System.Xml.XmlNamespaceManager nsManager);
    }
    public abstract partial class XPathItem
    {
        protected XPathItem() { }
        public abstract bool IsNode { get; }
        public abstract object TypedValue { get; }
        public abstract string Value { get; }
        public abstract bool ValueAsBoolean { get; }
        public abstract System.DateTime ValueAsDateTime { get; }
        public abstract double ValueAsDouble { get; }
        public abstract int ValueAsInt { get; }
        public abstract long ValueAsLong { get; }
        public abstract System.Type ValueType { get; }
        public abstract System.Xml.Schema.XmlSchemaType XmlType { get; }
        public virtual object ValueAs(System.Type returnType) { return default(object); }
        public abstract object ValueAs(System.Type returnType, System.Xml.IXmlNamespaceResolver nsResolver);
    }
    public enum XPathNamespaceScope
    {
        All = 0,
        ExcludeXml = 1,
        Local = 2,
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    public abstract partial class XPathNavigator : System.Xml.XPath.XPathItem, System.ICloneable, System.Xml.IXmlNamespaceResolver, System.Xml.XPath.IXPathNavigable
    {
        protected XPathNavigator() { }
        public abstract string BaseURI { get; }
        public virtual bool CanEdit { get { return default(bool); } }
        public virtual bool HasAttributes { get { return default(bool); } }
        public virtual bool HasChildren { get { return default(bool); } }
        public virtual string InnerXml { get { return default(string); } set { } }
        public abstract bool IsEmptyElement { get; }
        public sealed override bool IsNode { get { return default(bool); } }
        public abstract string LocalName { get; }
        public abstract string Name { get; }
        public abstract string NamespaceURI { get; }
        public abstract System.Xml.XmlNameTable NameTable { get; }
        public static System.Collections.IEqualityComparer NavigatorComparer { get { return default(System.Collections.IEqualityComparer); } }
        public abstract System.Xml.XPath.XPathNodeType NodeType { get; }
        public virtual string OuterXml { get { return default(string); } set { } }
        public abstract string Prefix { get; }
        public virtual System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { return default(System.Xml.Schema.IXmlSchemaInfo); } }
        public override object TypedValue { get { return default(object); } }
        public virtual object UnderlyingObject { get { return default(object); } }
        public override bool ValueAsBoolean { get { return default(bool); } }
        public override System.DateTime ValueAsDateTime { get { return default(System.DateTime); } }
        public override double ValueAsDouble { get { return default(double); } }
        public override int ValueAsInt { get { return default(int); } }
        public override long ValueAsLong { get { return default(long); } }
        public override System.Type ValueType { get { return default(System.Type); } }
        public virtual string XmlLang { get { return default(string); } }
        public override System.Xml.Schema.XmlSchemaType XmlType { get { return default(System.Xml.Schema.XmlSchemaType); } }
        public virtual System.Xml.XmlWriter AppendChild() { return default(System.Xml.XmlWriter); }
        public virtual void AppendChild(string newChild) { }
        public virtual void AppendChild(System.Xml.XmlReader newChild) { }
        public virtual void AppendChild(System.Xml.XPath.XPathNavigator newChild) { }
        public virtual void AppendChildElement(string prefix, string localName, string namespaceURI, string value) { }
        public virtual bool CheckValidity(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(bool); }
        public abstract System.Xml.XPath.XPathNavigator Clone();
        public virtual System.Xml.XmlNodeOrder ComparePosition(System.Xml.XPath.XPathNavigator nav) { return default(System.Xml.XmlNodeOrder); }
        public virtual System.Xml.XPath.XPathExpression Compile(string xpath) { return default(System.Xml.XPath.XPathExpression); }
        public virtual void CreateAttribute(string prefix, string localName, string namespaceURI, string value) { }
        public virtual System.Xml.XmlWriter CreateAttributes() { return default(System.Xml.XmlWriter); }
        public virtual System.Xml.XPath.XPathNavigator CreateNavigator() { return default(System.Xml.XPath.XPathNavigator); }
        public virtual void DeleteRange(System.Xml.XPath.XPathNavigator lastSiblingToDelete) { }
        public virtual void DeleteSelf() { }
        public virtual object Evaluate(string xpath) { return default(object); }
        public virtual object Evaluate(string xpath, System.Xml.IXmlNamespaceResolver resolver) { return default(object); }
        public virtual object Evaluate(System.Xml.XPath.XPathExpression expr) { return default(object); }
        public virtual object Evaluate(System.Xml.XPath.XPathExpression expr, System.Xml.XPath.XPathNodeIterator context) { return default(object); }
        public virtual string GetAttribute(string localName, string namespaceURI) { return default(string); }
        public virtual string GetNamespace(string name) { return default(string); }
        public virtual System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { return default(System.Collections.Generic.IDictionary<string, string>); }
        public virtual System.Xml.XmlWriter InsertAfter() { return default(System.Xml.XmlWriter); }
        public virtual void InsertAfter(string newSibling) { }
        public virtual void InsertAfter(System.Xml.XmlReader newSibling) { }
        public virtual void InsertAfter(System.Xml.XPath.XPathNavigator newSibling) { }
        public virtual System.Xml.XmlWriter InsertBefore() { return default(System.Xml.XmlWriter); }
        public virtual void InsertBefore(string newSibling) { }
        public virtual void InsertBefore(System.Xml.XmlReader newSibling) { }
        public virtual void InsertBefore(System.Xml.XPath.XPathNavigator newSibling) { }
        public virtual void InsertElementAfter(string prefix, string localName, string namespaceURI, string value) { }
        public virtual void InsertElementBefore(string prefix, string localName, string namespaceURI, string value) { }
        public virtual bool IsDescendant(System.Xml.XPath.XPathNavigator nav) { return default(bool); }
        public abstract bool IsSamePosition(System.Xml.XPath.XPathNavigator other);
        public virtual string LookupNamespace(string prefix) { return default(string); }
        public virtual string LookupPrefix(string namespaceURI) { return default(string); }
        public virtual bool Matches(string xpath) { return default(bool); }
        public virtual bool Matches(System.Xml.XPath.XPathExpression expr) { return default(bool); }
        public abstract bool MoveTo(System.Xml.XPath.XPathNavigator other);
        public virtual bool MoveToAttribute(string localName, string namespaceURI) { return default(bool); }
        public virtual bool MoveToChild(string localName, string namespaceURI) { return default(bool); }
        public virtual bool MoveToChild(System.Xml.XPath.XPathNodeType type) { return default(bool); }
        public virtual bool MoveToFirst() { return default(bool); }
        public abstract bool MoveToFirstAttribute();
        public abstract bool MoveToFirstChild();
        public bool MoveToFirstNamespace() { return default(bool); }
        public abstract bool MoveToFirstNamespace(System.Xml.XPath.XPathNamespaceScope namespaceScope);
        public virtual bool MoveToFollowing(string localName, string namespaceURI) { return default(bool); }
        public virtual bool MoveToFollowing(string localName, string namespaceURI, System.Xml.XPath.XPathNavigator end) { return default(bool); }
        public virtual bool MoveToFollowing(System.Xml.XPath.XPathNodeType type) { return default(bool); }
        public virtual bool MoveToFollowing(System.Xml.XPath.XPathNodeType type, System.Xml.XPath.XPathNavigator end) { return default(bool); }
        public abstract bool MoveToId(string id);
        public virtual bool MoveToNamespace(string name) { return default(bool); }
        public abstract bool MoveToNext();
        public virtual bool MoveToNext(string localName, string namespaceURI) { return default(bool); }
        public virtual bool MoveToNext(System.Xml.XPath.XPathNodeType type) { return default(bool); }
        public abstract bool MoveToNextAttribute();
        public bool MoveToNextNamespace() { return default(bool); }
        public abstract bool MoveToNextNamespace(System.Xml.XPath.XPathNamespaceScope namespaceScope);
        public abstract bool MoveToParent();
        public abstract bool MoveToPrevious();
        public virtual void MoveToRoot() { }
        public virtual System.Xml.XmlWriter PrependChild() { return default(System.Xml.XmlWriter); }
        public virtual void PrependChild(string newChild) { }
        public virtual void PrependChild(System.Xml.XmlReader newChild) { }
        public virtual void PrependChild(System.Xml.XPath.XPathNavigator newChild) { }
        public virtual void PrependChildElement(string prefix, string localName, string namespaceURI, string value) { }
        public virtual System.Xml.XmlReader ReadSubtree() { return default(System.Xml.XmlReader); }
        public virtual System.Xml.XmlWriter ReplaceRange(System.Xml.XPath.XPathNavigator lastSiblingToReplace) { return default(System.Xml.XmlWriter); }
        public virtual void ReplaceSelf(string newNode) { }
        public virtual void ReplaceSelf(System.Xml.XmlReader newNode) { }
        public virtual void ReplaceSelf(System.Xml.XPath.XPathNavigator newNode) { }
        public virtual System.Xml.XPath.XPathNodeIterator Select(string xpath) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator Select(string xpath, System.Xml.IXmlNamespaceResolver resolver) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator Select(System.Xml.XPath.XPathExpression expr) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator SelectAncestors(string name, string namespaceURI, bool matchSelf) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator SelectAncestors(System.Xml.XPath.XPathNodeType type, bool matchSelf) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator SelectChildren(string name, string namespaceURI) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator SelectChildren(System.Xml.XPath.XPathNodeType type) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator SelectDescendants(string name, string namespaceURI, bool matchSelf) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNodeIterator SelectDescendants(System.Xml.XPath.XPathNodeType type, bool matchSelf) { return default(System.Xml.XPath.XPathNodeIterator); }
        public virtual System.Xml.XPath.XPathNavigator SelectSingleNode(string xpath) { return default(System.Xml.XPath.XPathNavigator); }
        public virtual System.Xml.XPath.XPathNavigator SelectSingleNode(string xpath, System.Xml.IXmlNamespaceResolver resolver) { return default(System.Xml.XPath.XPathNavigator); }
        public virtual System.Xml.XPath.XPathNavigator SelectSingleNode(System.Xml.XPath.XPathExpression expression) { return default(System.Xml.XPath.XPathNavigator); }
        public virtual void SetTypedValue(object typedValue) { }
        public virtual void SetValue(string value) { }
        object System.ICloneable.Clone() { return default(object); }
        public override string ToString() { return default(string); }
        public override object ValueAs(System.Type returnType, System.Xml.IXmlNamespaceResolver nsResolver) { return default(object); }
        public virtual void WriteSubtree(System.Xml.XmlWriter writer) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Position={CurrentPosition}, Current={debuggerDisplayProxy}")]
    public abstract partial class XPathNodeIterator : System.Collections.IEnumerable, System.ICloneable
    {
        protected XPathNodeIterator() { }
        public virtual int Count { get { return default(int); } }
        public abstract System.Xml.XPath.XPathNavigator Current { get; }
        public abstract int CurrentPosition { get; }
        public abstract System.Xml.XPath.XPathNodeIterator Clone();
        public virtual System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public abstract bool MoveNext();
        object System.ICloneable.Clone() { return default(object); }
    }
    public enum XPathNodeType
    {
        All = 9,
        Attribute = 2,
        Comment = 8,
        Element = 1,
        Namespace = 3,
        ProcessingInstruction = 7,
        Root = 0,
        SignificantWhitespace = 5,
        Text = 4,
        Whitespace = 6,
    }
    public enum XPathResultType
    {
        Any = 5,
        Boolean = 2,
        Error = 6,
        Navigator = 1,
        NodeSet = 3,
        Number = 0,
        String = 1,
    }
}
namespace System.Xml.Xsl
{
    public partial interface IXsltContextFunction
    {
        System.Xml.XPath.XPathResultType[] ArgTypes { get; }
        int Maxargs { get; }
        int Minargs { get; }
        System.Xml.XPath.XPathResultType ReturnType { get; }
        object Invoke(System.Xml.Xsl.XsltContext xsltContext, object[] args, System.Xml.XPath.XPathNavigator docContext);
    }
    public partial interface IXsltContextVariable
    {
        bool IsLocal { get; }
        bool IsParam { get; }
        System.Xml.XPath.XPathResultType VariableType { get; }
        object Evaluate(System.Xml.Xsl.XsltContext xsltContext);
    }
    public sealed partial class XslCompiledTransform
    {
        public XslCompiledTransform() { }
        public XslCompiledTransform(bool enableDebug) { }
        public System.Xml.XmlWriterSettings OutputSettings { get { return default(System.Xml.XmlWriterSettings); } }
        public void Load(string stylesheetUri) { }
        public void Load(string stylesheetUri, System.Xml.Xsl.XsltSettings settings, System.Xml.XmlResolver stylesheetResolver) { }
        public void Load(System.Xml.XmlReader stylesheet) { }
        public void Load(System.Xml.XmlReader stylesheet, System.Xml.Xsl.XsltSettings settings, System.Xml.XmlResolver stylesheetResolver) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet, System.Xml.Xsl.XsltSettings settings, System.Xml.XmlResolver stylesheetResolver) { }
        public void Transform(string inputUri, string resultsFile) { }
        public void Transform(string inputUri, System.Xml.XmlWriter results) { }
        public void Transform(string inputUri, System.Xml.Xsl.XsltArgumentList arguments, System.IO.Stream results) { }
        public void Transform(string inputUri, System.Xml.Xsl.XsltArgumentList arguments, System.IO.TextWriter results) { }
        public void Transform(string inputUri, System.Xml.Xsl.XsltArgumentList arguments, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList arguments, System.IO.Stream results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList arguments, System.IO.TextWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList arguments, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList arguments, System.Xml.XmlWriter results, System.Xml.XmlResolver documentResolver) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList arguments, System.IO.Stream results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList arguments, System.IO.TextWriter results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList arguments, System.Xml.XmlWriter results) { }
    }
    public partial class XsltArgumentList
    {
        public XsltArgumentList() { }
        public event System.Xml.Xsl.XsltMessageEncounteredEventHandler XsltMessageEncountered { add { } remove { } }
        public void AddExtensionObject(string namespaceUri, object extension) { }
        public void AddParam(string name, string namespaceUri, object parameter) { }
        public void Clear() { }
        public object GetExtensionObject(string namespaceUri) { return default(object); }
        public object GetParam(string name, string namespaceUri) { return default(object); }
        public object RemoveExtensionObject(string namespaceUri) { return default(object); }
        public object RemoveParam(string name, string namespaceUri) { return default(object); }
    }
    public partial class XsltCompileException : System.Xml.Xsl.XsltException
    {
        public XsltCompileException() { }
        public XsltCompileException(System.Exception inner, string sourceUri, int lineNumber, int linePosition) { }
        protected XsltCompileException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XsltCompileException(string message) { }
        public XsltCompileException(string message, System.Exception innerException) { }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class XsltContext : System.Xml.XmlNamespaceManager
    {
        protected XsltContext() : base (default(System.Xml.XmlNameTable)) { }
        protected XsltContext(System.Xml.NameTable table) : base (default(System.Xml.XmlNameTable)) { }
        public abstract bool Whitespace { get; }
        public abstract int CompareDocument(string baseUri, string nextbaseUri);
        public abstract bool PreserveWhitespace(System.Xml.XPath.XPathNavigator node);
        public abstract System.Xml.Xsl.IXsltContextFunction ResolveFunction(string prefix, string name, System.Xml.XPath.XPathResultType[] ArgTypes);
        public abstract System.Xml.Xsl.IXsltContextVariable ResolveVariable(string prefix, string name);
    }
    public partial class XsltException : System.SystemException
    {
        public XsltException() { }
        protected XsltException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XsltException(string message) { }
        public XsltException(string message, System.Exception innerException) { }
        public virtual int LineNumber { get { return default(int); } }
        public virtual int LinePosition { get { return default(int); } }
        public override string Message { get { return default(string); } }
        public virtual string SourceUri { get { return default(string); } }
        [System.Security.Permissions.SecurityPermissionAttribute(System.Security.Permissions.SecurityAction.LinkDemand, SerializationFormatter=true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class XsltMessageEncounteredEventArgs : System.EventArgs
    {
        protected XsltMessageEncounteredEventArgs() { }
        public abstract string Message { get; }
    }
    public delegate void XsltMessageEncounteredEventHandler(object sender, System.Xml.Xsl.XsltMessageEncounteredEventArgs e);
    [System.ObsoleteAttribute("This class has been deprecated. Please use System.Xml.Xsl.XslCompiledTransform instead. http://go.microsoft.com/fwlink/?linkid=14202")]
    public sealed partial class XslTransform
    {
        public XslTransform() { }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public void Load(string url) { }
        public void Load(string url, System.Xml.XmlResolver resolver) { }
        public void Load(System.Xml.XmlReader stylesheet) { }
        public void Load(System.Xml.XmlReader stylesheet, System.Xml.XmlResolver resolver) { }
        public void Load(System.Xml.XmlReader stylesheet, System.Xml.XmlResolver resolver, System.Security.Policy.Evidence evidence) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet, System.Xml.XmlResolver resolver) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet, System.Xml.XmlResolver resolver, System.Security.Policy.Evidence evidence) { }
        public void Load(System.Xml.XPath.XPathNavigator stylesheet) { }
        public void Load(System.Xml.XPath.XPathNavigator stylesheet, System.Xml.XmlResolver resolver) { }
        public void Load(System.Xml.XPath.XPathNavigator stylesheet, System.Xml.XmlResolver resolver, System.Security.Policy.Evidence evidence) { }
        public void Transform(string inputfile, string outputfile) { }
        public void Transform(string inputfile, string outputfile, System.Xml.XmlResolver resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args) { return default(System.Xml.XmlReader); }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.IO.Stream output) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.IO.Stream output, System.Xml.XmlResolver resolver) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.IO.TextWriter output) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.IO.TextWriter output, System.Xml.XmlResolver resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.Xml.XmlResolver resolver) { return default(System.Xml.XmlReader); }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.Xml.XmlWriter output) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList args, System.Xml.XmlWriter output, System.Xml.XmlResolver resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args) { return default(System.Xml.XmlReader); }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.IO.Stream output) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.IO.Stream output, System.Xml.XmlResolver resolver) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.IO.TextWriter output) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.IO.TextWriter output, System.Xml.XmlResolver resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.Xml.XmlResolver resolver) { return default(System.Xml.XmlReader); }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.Xml.XmlWriter output) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList args, System.Xml.XmlWriter output, System.Xml.XmlResolver resolver) { }
    }
    public sealed partial class XsltSettings
    {
        public XsltSettings() { }
        public XsltSettings(bool enableDocumentFunction, bool enableScript) { }
        public static System.Xml.Xsl.XsltSettings Default { get { return default(System.Xml.Xsl.XsltSettings); } }
        public bool EnableDocumentFunction { get { return default(bool); } set { } }
        public bool EnableScript { get { return default(bool); } set { } }
        public static System.Xml.Xsl.XsltSettings TrustedXslt { get { return default(System.Xml.Xsl.XsltSettings); } }
    }
}
namespace System.Xml.Xsl.Runtime
{
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct StringConcat
    {
        public string Delimiter { get { return default(string); } set { } }
        public void Clear() { }
        public void Concat(string value) { }
        public string GetResult() { return default(string); }
    }
}
