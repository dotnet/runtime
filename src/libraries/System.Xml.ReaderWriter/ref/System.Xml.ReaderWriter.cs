// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Xml
{
    public enum ConformanceLevel
    {
        Auto = 0,
        Fragment = 1,
        Document = 2,
    }
    public enum DtdProcessing
    {
        Prohibit = 0,
        Ignore = 1,
        Parse = 2,
    }
    public enum EntityHandling
    {
        ExpandEntities = 1,
        ExpandCharEntities = 2,
    }
    public enum Formatting
    {
        None = 0,
        Indented = 1,
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
    public partial interface IApplicationResourceStreamResolver
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
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
        string? LookupNamespace(string prefix);
        string? LookupPrefix(string namespaceName);
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
        public override string Add(char[] key, int start, int len) { throw null; }
        public override string Add(string key) { throw null; }
        public override string? Get(char[] key, int start, int len) { throw null; }
        public override string? Get(string value) { throw null; }
    }
    public enum NewLineHandling
    {
        Replace = 0,
        Entitize = 1,
        None = 2,
    }
    public enum ReadState
    {
        Initial = 0,
        Interactive = 1,
        Error = 2,
        EndOfFile = 3,
        Closed = 4,
    }
    public enum ValidationType
    {
        None = 0,
        [System.ObsoleteAttribute("ValidationType.Auto has been deprecated. Use DTD or Schema instead.")]
        Auto = 1,
        DTD = 2,
        [System.ObsoleteAttribute("XDR Validation through XmlValidatingReader has been deprecated and is not supported.")]
        XDR = 3,
        Schema = 4,
    }
    public enum WhitespaceHandling
    {
        All = 0,
        Significant = 1,
        None = 2,
    }
    public enum WriteState
    {
        Start = 0,
        Prolog = 1,
        Element = 2,
        Attribute = 3,
        Content = 4,
        Closed = 5,
        Error = 6,
    }
    public partial class XmlAttribute : System.Xml.XmlNode
    {
        protected internal XmlAttribute(string? prefix, string localName, string? namespaceURI, System.Xml.XmlDocument doc) { }
        public override string BaseURI { get { throw null; } }
        public override string InnerText { set { } }
        public override string InnerXml { set { } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string NamespaceURI { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlDocument OwnerDocument { get { throw null; } }
        public virtual System.Xml.XmlElement? OwnerElement { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override string Prefix { get { throw null; } set { } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { throw null; } }
        public virtual bool Specified { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override string Value { get { throw null; } set { } }
        public override System.Xml.XmlNode? AppendChild(System.Xml.XmlNode newChild) { throw null; }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override System.Xml.XmlNode? InsertAfter(System.Xml.XmlNode newChild, System.Xml.XmlNode? refChild) { throw null; }
        public override System.Xml.XmlNode? InsertBefore(System.Xml.XmlNode newChild, System.Xml.XmlNode? refChild) { throw null; }
        public override System.Xml.XmlNode? PrependChild(System.Xml.XmlNode newChild) { throw null; }
        public override System.Xml.XmlNode RemoveChild(System.Xml.XmlNode oldChild) { throw null; }
        public override System.Xml.XmlNode ReplaceChild(System.Xml.XmlNode newChild, System.Xml.XmlNode oldChild) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public sealed partial class XmlAttributeCollection : System.Xml.XmlNamedNodeMap, System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal XmlAttributeCollection() { }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Xml.XmlAttribute this[int i] { get { throw null; } }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Xml.XmlAttribute? this[string name] { get { throw null; } }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public System.Xml.XmlAttribute? this[string localName, string? namespaceURI] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public System.Xml.XmlAttribute Append(System.Xml.XmlAttribute node) { throw null; }
        public void CopyTo(System.Xml.XmlAttribute[] array, int index) { }
        public System.Xml.XmlAttribute InsertAfter(System.Xml.XmlAttribute newNode, System.Xml.XmlAttribute? refNode) { throw null; }
        public System.Xml.XmlAttribute InsertBefore(System.Xml.XmlAttribute newNode, System.Xml.XmlAttribute? refNode) { throw null; }
        public System.Xml.XmlAttribute Prepend(System.Xml.XmlAttribute node) { throw null; }
        public System.Xml.XmlAttribute? Remove(System.Xml.XmlAttribute? node) { throw null; }
        public void RemoveAll() { }
        public System.Xml.XmlAttribute? RemoveAt(int i) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("node")]
        public override System.Xml.XmlNode? SetNamedItem(System.Xml.XmlNode? node) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public partial class XmlCDataSection : System.Xml.XmlCharacterData
    {
        protected internal XmlCDataSection(string? data, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override System.Xml.XmlNode? PreviousText { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public abstract partial class XmlCharacterData : System.Xml.XmlLinkedNode
    {
        protected internal XmlCharacterData(string? data, System.Xml.XmlDocument doc) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string Data { get { throw null; } set { } }
        public override string InnerText { get { throw null; } set { } }
        public virtual int Length { get { throw null; } }
        public override string? Value { get { throw null; } set { } }
        public virtual void AppendData(string? strData) { }
        public virtual void DeleteData(int offset, int count) { }
        public virtual void InsertData(int offset, string? strData) { }
        public virtual void ReplaceData(int offset, int count, string? strData) { }
        public virtual string Substring(int offset, int count) { throw null; }
    }
    public partial class XmlComment : System.Xml.XmlCharacterData
    {
        protected internal XmlComment(string? comment, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlConvert
    {
        public XmlConvert() { }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("name")]
        public static string? DecodeName(string? name) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("name")]
        public static string? EncodeLocalName(string? name) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("name")]
        public static string? EncodeName(string? name) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("name")]
        public static string? EncodeNmToken(string? name) { throw null; }
        public static bool IsNCNameChar(char ch) { throw null; }
        public static bool IsPublicIdChar(char ch) { throw null; }
        public static bool IsStartNCNameChar(char ch) { throw null; }
        public static bool IsWhitespaceChar(char ch) { throw null; }
        public static bool IsXmlChar(char ch) { throw null; }
        public static bool IsXmlSurrogatePair(char lowChar, char highChar) { throw null; }
        public static bool ToBoolean(string s) { throw null; }
        public static byte ToByte(string s) { throw null; }
        public static char ToChar(string s) { throw null; }
        [System.ObsoleteAttribute("Use XmlConvert.ToDateTime() that accepts an XmlDateTimeSerializationMode instead.")]
        public static System.DateTime ToDateTime(string s) { throw null; }
        public static System.DateTime ToDateTime(string s, string format) { throw null; }
        public static System.DateTime ToDateTime(string s, string[] formats) { throw null; }
        public static System.DateTime ToDateTime(string s, System.Xml.XmlDateTimeSerializationMode dateTimeOption) { throw null; }
        public static System.DateTimeOffset ToDateTimeOffset(string s) { throw null; }
        public static System.DateTimeOffset ToDateTimeOffset(string s, string format) { throw null; }
        public static System.DateTimeOffset ToDateTimeOffset(string s, string[] formats) { throw null; }
        public static decimal ToDecimal(string s) { throw null; }
        public static double ToDouble(string s) { throw null; }
        public static System.Guid ToGuid(string s) { throw null; }
        public static short ToInt16(string s) { throw null; }
        public static int ToInt32(string s) { throw null; }
        public static long ToInt64(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string s) { throw null; }
        public static float ToSingle(string s) { throw null; }
        public static string ToString(bool value) { throw null; }
        public static string ToString(byte value) { throw null; }
        public static string ToString(char value) { throw null; }
        [System.ObsoleteAttribute("Use XmlConvert.ToString() that accepts an XmlDateTimeSerializationMode instead.")]
        public static string ToString(System.DateTime value) { throw null; }
        public static string ToString(System.DateTime value, string format) { throw null; }
        public static string ToString(System.DateTime value, System.Xml.XmlDateTimeSerializationMode dateTimeOption) { throw null; }
        public static string ToString(System.DateTimeOffset value) { throw null; }
        public static string ToString(System.DateTimeOffset value, string format) { throw null; }
        public static string ToString(decimal value) { throw null; }
        public static string ToString(double value) { throw null; }
        public static string ToString(System.Guid value) { throw null; }
        public static string ToString(short value) { throw null; }
        public static string ToString(int value) { throw null; }
        public static string ToString(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value) { throw null; }
        public static string ToString(float value) { throw null; }
        public static string ToString(System.TimeSpan value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value) { throw null; }
        public static System.TimeSpan ToTimeSpan(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string s) { throw null; }
        public static string VerifyName(string name) { throw null; }
        public static string VerifyNCName(string name) { throw null; }
        public static string VerifyNMTOKEN(string name) { throw null; }
        public static string VerifyPublicId(string publicId) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("token")]
        public static string? VerifyTOKEN(string? token) { throw null; }
        public static string VerifyWhitespace(string content) { throw null; }
        public static string VerifyXmlChars(string content) { throw null; }
    }
    public enum XmlDateTimeSerializationMode
    {
        Local = 0,
        Utc = 1,
        Unspecified = 2,
        RoundtripKind = 3,
    }
    public partial class XmlDeclaration : System.Xml.XmlLinkedNode
    {
        protected internal XmlDeclaration(string version, string? encoding, string? standalone, System.Xml.XmlDocument doc) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Encoding { get { throw null; } set { } }
        public override string InnerText { get { throw null; } set { } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Standalone { get { throw null; } set { } }
        public override string? Value { get { throw null; } set { } }
        public string Version { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlDocument : System.Xml.XmlNode
    {
        public XmlDocument() { }
        protected internal XmlDocument(System.Xml.XmlImplementation imp) { }
        public XmlDocument(System.Xml.XmlNameTable nt) { }
        public override string BaseURI { get { throw null; } }
        public System.Xml.XmlElement? DocumentElement { get { throw null; } }
        public virtual System.Xml.XmlDocumentType? DocumentType { get { throw null; } }
        public System.Xml.XmlImplementation Implementation { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override string InnerText { set { } }
        public override string InnerXml { get { throw null; } set { } }
        public override bool IsReadOnly { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public System.Xml.XmlNameTable NameTable { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlDocument? OwnerDocument { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public bool PreserveWhitespace { get { throw null; } set { } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { throw null; } }
        public System.Xml.Schema.XmlSchemaSet Schemas { get { throw null; } set { } }
        public virtual System.Xml.XmlResolver? XmlResolver { set { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeChanged { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeChanging { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeInserted { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeInserting { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeRemoved { add { } remove { } }
        public event System.Xml.XmlNodeChangedEventHandler NodeRemoving { add { } remove { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public System.Xml.XmlAttribute CreateAttribute(string name) { throw null; }
        public System.Xml.XmlAttribute CreateAttribute(string qualifiedName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlAttribute CreateAttribute(string? prefix, string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlCDataSection CreateCDataSection(string? data) { throw null; }
        public virtual System.Xml.XmlComment CreateComment(string? data) { throw null; }
        protected internal virtual System.Xml.XmlAttribute CreateDefaultAttribute(string? prefix, string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlDocumentFragment CreateDocumentFragment() { throw null; }
        public virtual System.Xml.XmlDocumentType CreateDocumentType(string name, string? publicId, string? systemId, string? internalSubset) { throw null; }
        public System.Xml.XmlElement CreateElement(string name) { throw null; }
        public System.Xml.XmlElement CreateElement(string qualifiedName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlElement CreateElement(string? prefix, string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlEntityReference CreateEntityReference(string name) { throw null; }
        public override System.Xml.XPath.XPathNavigator? CreateNavigator() { throw null; }
        protected internal virtual System.Xml.XPath.XPathNavigator? CreateNavigator(System.Xml.XmlNode node) { throw null; }
        public virtual System.Xml.XmlNode CreateNode(string nodeTypeString, string name, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlNode CreateNode(System.Xml.XmlNodeType type, string name, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlNode CreateNode(System.Xml.XmlNodeType type, string? prefix, string name, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlProcessingInstruction CreateProcessingInstruction(string target, string data) { throw null; }
        public virtual System.Xml.XmlSignificantWhitespace CreateSignificantWhitespace(string? text) { throw null; }
        public virtual System.Xml.XmlText CreateTextNode(string? text) { throw null; }
        public virtual System.Xml.XmlWhitespace CreateWhitespace(string? text) { throw null; }
        public virtual System.Xml.XmlDeclaration CreateXmlDeclaration(string version, string? encoding, string? standalone) { throw null; }
        public virtual System.Xml.XmlElement? GetElementById(string elementId) { throw null; }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string name) { throw null; }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string localName, string namespaceURI) { throw null; }
        public virtual System.Xml.XmlNode ImportNode(System.Xml.XmlNode node, bool deep) { throw null; }
        public virtual void Load(System.IO.Stream inStream) { }
        public virtual void Load(System.IO.TextReader txtReader) { }
        public virtual void Load(string filename) { }
        public virtual void Load(System.Xml.XmlReader reader) { }
        public virtual void LoadXml(string xml) { }
        public virtual System.Xml.XmlNode? ReadNode(System.Xml.XmlReader reader) { throw null; }
        public virtual void Save(System.IO.Stream outStream) { }
        public virtual void Save(System.IO.TextWriter writer) { }
        public virtual void Save(string filename) { }
        public virtual void Save(System.Xml.XmlWriter w) { }
        public void Validate(System.Xml.Schema.ValidationEventHandler? validationEventHandler) { }
        public void Validate(System.Xml.Schema.ValidationEventHandler? validationEventHandler, System.Xml.XmlNode nodeToValidate) { }
        public override void WriteContentTo(System.Xml.XmlWriter xw) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlDocumentFragment : System.Xml.XmlNode
    {
        protected internal XmlDocumentFragment(System.Xml.XmlDocument ownerDocument) { }
        public override string InnerXml { get { throw null; } set { } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlDocument OwnerDocument { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlDocumentType : System.Xml.XmlLinkedNode
    {
        protected internal XmlDocumentType(string name, string? publicId, string? systemId, string? internalSubset, System.Xml.XmlDocument doc) { }
        public System.Xml.XmlNamedNodeMap Entities { get { throw null; } }
        public string? InternalSubset { get { throw null; } }
        public override bool IsReadOnly { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public System.Xml.XmlNamedNodeMap Notations { get { throw null; } }
        public string? PublicId { get { throw null; } }
        public string? SystemId { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlElement : System.Xml.XmlLinkedNode
    {
        protected internal XmlElement(string prefix, string localName, string? namespaceURI, System.Xml.XmlDocument doc) { }
        public override System.Xml.XmlAttributeCollection Attributes { get { throw null; } }
        public virtual bool HasAttributes { get { throw null; } }
        public override string InnerText { get { throw null; } set { } }
        public override string InnerXml { get { throw null; } set { } }
        public bool IsEmpty { get { throw null; } set { } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string NamespaceURI { get { throw null; } }
        public override System.Xml.XmlNode? NextSibling { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlDocument OwnerDocument { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override string Prefix { get { throw null; } set { } }
        public override System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public virtual string GetAttribute(string name) { throw null; }
        public virtual string GetAttribute(string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlAttribute? GetAttributeNode(string name) { throw null; }
        public virtual System.Xml.XmlAttribute? GetAttributeNode(string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string name) { throw null; }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string localName, string namespaceURI) { throw null; }
        public virtual bool HasAttribute(string name) { throw null; }
        public virtual bool HasAttribute(string localName, string? namespaceURI) { throw null; }
        public override void RemoveAll() { }
        public virtual void RemoveAllAttributes() { }
        public virtual void RemoveAttribute(string name) { }
        public virtual void RemoveAttribute(string localName, string? namespaceURI) { }
        public virtual System.Xml.XmlNode? RemoveAttributeAt(int i) { throw null; }
        public virtual System.Xml.XmlAttribute? RemoveAttributeNode(string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlAttribute? RemoveAttributeNode(System.Xml.XmlAttribute oldAttr) { throw null; }
        public virtual void SetAttribute(string name, string? value) { }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        public virtual string? SetAttribute(string localName, string? namespaceURI, string? value) { throw null; }
        public virtual System.Xml.XmlAttribute SetAttributeNode(string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlAttribute? SetAttributeNode(System.Xml.XmlAttribute newAttr) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlEntity : System.Xml.XmlNode
    {
        internal XmlEntity() { }
        public override string BaseURI { get { throw null; } }
        public override string InnerText { get { throw null; } set { } }
        public override string InnerXml { get { throw null; } set { } }
        public override bool IsReadOnly { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string? NotationName { get { throw null; } }
        public override string OuterXml { get { throw null; } }
        public string? PublicId { get { throw null; } }
        public string? SystemId { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlEntityReference : System.Xml.XmlLinkedNode
    {
        protected internal XmlEntityReference(string name, System.Xml.XmlDocument doc) { }
        public override string BaseURI { get { throw null; } }
        public override bool IsReadOnly { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override string? Value { get { throw null; } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlException : System.SystemException
    {
        public XmlException() { }
        protected XmlException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlException(string? message) { }
        public XmlException(string? message, System.Exception? innerException) { }
        public XmlException(string? message, System.Exception? innerException, int lineNumber, int linePosition) { }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public override string Message { get { throw null; } }
        public string? SourceUri { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class XmlImplementation
    {
        public XmlImplementation() { }
        public XmlImplementation(System.Xml.XmlNameTable nt) { }
        public virtual System.Xml.XmlDocument CreateDocument() { throw null; }
        public bool HasFeature(string strFeature, string strVersion) { throw null; }
    }
    public abstract partial class XmlLinkedNode : System.Xml.XmlNode
    {
        internal XmlLinkedNode() { }
        public override System.Xml.XmlNode? NextSibling { get { throw null; } }
        public override System.Xml.XmlNode? PreviousSibling { get { throw null; } }
    }
    public partial class XmlNamedNodeMap : System.Collections.IEnumerable
    {
        internal XmlNamedNodeMap() { }
        public virtual int Count { get { throw null; } }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual System.Xml.XmlNode? GetNamedItem(string name) { throw null; }
        public virtual System.Xml.XmlNode? GetNamedItem(string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlNode? Item(int index) { throw null; }
        public virtual System.Xml.XmlNode? RemoveNamedItem(string name) { throw null; }
        public virtual System.Xml.XmlNode? RemoveNamedItem(string localName, string? namespaceURI) { throw null; }
        public virtual System.Xml.XmlNode? SetNamedItem(System.Xml.XmlNode? node) { throw null; }
    }
    public partial class XmlNamespaceManager : System.Collections.IEnumerable, System.Xml.IXmlNamespaceResolver
    {
        public XmlNamespaceManager(System.Xml.XmlNameTable nameTable) { }
        public virtual string DefaultNamespace { get { throw null; } }
        public virtual System.Xml.XmlNameTable? NameTable { get { throw null; } }
        public virtual void AddNamespace(string prefix, string uri) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { throw null; }
        public virtual bool HasNamespace(string prefix) { throw null; }
        public virtual string? LookupNamespace(string prefix) { throw null; }
        public virtual string? LookupPrefix(string uri) { throw null; }
        public virtual bool PopScope() { throw null; }
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
        public abstract string? Get(char[] array, int offset, int length);
        public abstract string? Get(string array);
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    public abstract partial class XmlNode : System.Collections.IEnumerable, System.ICloneable, System.Xml.XPath.IXPathNavigable
    {
        internal XmlNode() { }
        public virtual System.Xml.XmlAttributeCollection? Attributes { get { throw null; } }
        public virtual string BaseURI { get { throw null; } }
        public virtual System.Xml.XmlNodeList ChildNodes { get { throw null; } }
        public virtual System.Xml.XmlNode? FirstChild { get { throw null; } }
        public virtual bool HasChildNodes { get { throw null; } }
        public virtual string InnerText { get { throw null; } set { } }
        public virtual string InnerXml { get { throw null; } set { } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual System.Xml.XmlElement? this[string name] { get { throw null; } }
        public virtual System.Xml.XmlElement? this[string localname, string ns] { get { throw null; } }
        public virtual System.Xml.XmlNode? LastChild { get { throw null; } }
        public abstract string LocalName { get; }
        public abstract string Name { get; }
        public virtual string NamespaceURI { get { throw null; } }
        public virtual System.Xml.XmlNode? NextSibling { get { throw null; } }
        public abstract System.Xml.XmlNodeType NodeType { get; }
        public virtual string OuterXml { get { throw null; } }
        public virtual System.Xml.XmlDocument? OwnerDocument { get { throw null; } }
        public virtual System.Xml.XmlNode? ParentNode { get { throw null; } }
        public virtual string Prefix { get { throw null; } set { } }
        public virtual System.Xml.XmlNode? PreviousSibling { get { throw null; } }
        public virtual System.Xml.XmlNode? PreviousText { get { throw null; } }
        public virtual System.Xml.Schema.IXmlSchemaInfo SchemaInfo { get { throw null; } }
        public virtual string? Value { get { throw null; } set { } }
        public virtual System.Xml.XmlNode? AppendChild(System.Xml.XmlNode newChild) { throw null; }
        public virtual System.Xml.XmlNode Clone() { throw null; }
        public abstract System.Xml.XmlNode CloneNode(bool deep);
        public virtual System.Xml.XPath.XPathNavigator? CreateNavigator() { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public virtual string GetNamespaceOfPrefix(string prefix) { throw null; }
        public virtual string GetPrefixOfNamespace(string namespaceURI) { throw null; }
        public virtual System.Xml.XmlNode? InsertAfter(System.Xml.XmlNode newChild, System.Xml.XmlNode? refChild) { throw null; }
        public virtual System.Xml.XmlNode? InsertBefore(System.Xml.XmlNode newChild, System.Xml.XmlNode? refChild) { throw null; }
        public virtual void Normalize() { }
        public virtual System.Xml.XmlNode? PrependChild(System.Xml.XmlNode newChild) { throw null; }
        public virtual void RemoveAll() { }
        public virtual System.Xml.XmlNode RemoveChild(System.Xml.XmlNode oldChild) { throw null; }
        public virtual System.Xml.XmlNode ReplaceChild(System.Xml.XmlNode newChild, System.Xml.XmlNode oldChild) { throw null; }
        public System.Xml.XmlNodeList? SelectNodes(string xpath) { throw null; }
        public System.Xml.XmlNodeList? SelectNodes(string xpath, System.Xml.XmlNamespaceManager nsmgr) { throw null; }
        public System.Xml.XmlNode? SelectSingleNode(string xpath) { throw null; }
        public System.Xml.XmlNode? SelectSingleNode(string xpath, System.Xml.XmlNamespaceManager nsmgr) { throw null; }
        public virtual bool Supports(string feature, string version) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public abstract void WriteContentTo(System.Xml.XmlWriter w);
        public abstract void WriteTo(System.Xml.XmlWriter w);
    }
    public enum XmlNodeChangedAction
    {
        Insert = 0,
        Remove = 1,
        Change = 2,
    }
    public partial class XmlNodeChangedEventArgs : System.EventArgs
    {
        public XmlNodeChangedEventArgs(System.Xml.XmlNode? node, System.Xml.XmlNode? oldParent, System.Xml.XmlNode? newParent, string? oldValue, string? newValue, System.Xml.XmlNodeChangedAction action) { }
        public System.Xml.XmlNodeChangedAction Action { get { throw null; } }
        public System.Xml.XmlNode? NewParent { get { throw null; } }
        public string? NewValue { get { throw null; } }
        public System.Xml.XmlNode? Node { get { throw null; } }
        public System.Xml.XmlNode? OldParent { get { throw null; } }
        public string? OldValue { get { throw null; } }
    }
    public delegate void XmlNodeChangedEventHandler(object sender, System.Xml.XmlNodeChangedEventArgs e);
    public abstract partial class XmlNodeList : System.Collections.IEnumerable, System.IDisposable
    {
        protected XmlNodeList() { }
        public abstract int Count { get; }
        [System.Runtime.CompilerServices.IndexerName("ItemOf")]
        public virtual System.Xml.XmlNode? this[int i] { get { throw null; } }
        public abstract System.Collections.IEnumerator GetEnumerator();
        public abstract System.Xml.XmlNode? Item(int index);
        protected virtual void PrivateDisposeNodeList() { }
        void System.IDisposable.Dispose() { }
    }
    public enum XmlNodeOrder
    {
        Before = 0,
        After = 1,
        Same = 2,
        Unknown = 3,
    }
    public partial class XmlNodeReader : System.Xml.XmlReader, System.Xml.IXmlNamespaceResolver
    {
        public XmlNodeReader(System.Xml.XmlNode node) { }
        public override int AttributeCount { get { throw null; } }
        public override string BaseURI { get { throw null; } }
        public override bool CanReadBinaryContent { get { throw null; } }
        public override bool CanResolveEntity { get { throw null; } }
        public override int Depth { get { throw null; } }
        public override bool EOF { get { throw null; } }
        public override bool HasAttributes { get { throw null; } }
        public override bool HasValue { get { throw null; } }
        public override bool IsDefault { get { throw null; } }
        public override bool IsEmptyElement { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override string NamespaceURI { get { throw null; } }
        public override System.Xml.XmlNameTable NameTable { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override string Prefix { get { throw null; } }
        public override System.Xml.ReadState ReadState { get { throw null; } }
        public override System.Xml.Schema.IXmlSchemaInfo? SchemaInfo { get { throw null; } }
        public override string Value { get { throw null; } }
        public override string XmlLang { get { throw null; } }
        public override System.Xml.XmlSpace XmlSpace { get { throw null; } }
        public override void Close() { }
        public override string GetAttribute(int attributeIndex) { throw null; }
        public override string? GetAttribute(string name) { throw null; }
        public override string? GetAttribute(string name, string? namespaceURI) { throw null; }
        public override string? LookupNamespace(string prefix) { throw null; }
        public override void MoveToAttribute(int attributeIndex) { }
        public override bool MoveToAttribute(string name) { throw null; }
        public override bool MoveToAttribute(string name, string? namespaceURI) { throw null; }
        public override bool MoveToElement() { throw null; }
        public override bool MoveToFirstAttribute() { throw null; }
        public override bool MoveToNextAttribute() { throw null; }
        public override bool Read() { throw null; }
        public override bool ReadAttributeValue() { throw null; }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public override string ReadString() { throw null; }
        public override void ResolveEntity() { }
        public override void Skip() { }
        System.Collections.Generic.IDictionary<string, string> System.Xml.IXmlNamespaceResolver.GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { throw null; }
        string System.Xml.IXmlNamespaceResolver.LookupNamespace(string prefix) { throw null; }
        string System.Xml.IXmlNamespaceResolver.LookupPrefix(string namespaceName) { throw null; }
    }
    public enum XmlNodeType
    {
        None = 0,
        Element = 1,
        Attribute = 2,
        Text = 3,
        CDATA = 4,
        EntityReference = 5,
        Entity = 6,
        ProcessingInstruction = 7,
        Comment = 8,
        Document = 9,
        DocumentType = 10,
        DocumentFragment = 11,
        Notation = 12,
        Whitespace = 13,
        SignificantWhitespace = 14,
        EndElement = 15,
        EndEntity = 16,
        XmlDeclaration = 17,
    }
    public partial class XmlNotation : System.Xml.XmlNode
    {
        internal XmlNotation() { }
        public override string InnerXml { get { throw null; } set { } }
        public override bool IsReadOnly { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override string OuterXml { get { throw null; } }
        public string? PublicId { get { throw null; } }
        public string? SystemId { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public enum XmlOutputMethod
    {
        Xml = 0,
        Html = 1,
        Text = 2,
        AutoDetect = 3,
    }
    public partial class XmlParserContext
    {
        public XmlParserContext(System.Xml.XmlNameTable? nt, System.Xml.XmlNamespaceManager? nsMgr, string? docTypeName, string? pubId, string? sysId, string? internalSubset, string? baseURI, string? xmlLang, System.Xml.XmlSpace xmlSpace) { }
        public XmlParserContext(System.Xml.XmlNameTable? nt, System.Xml.XmlNamespaceManager? nsMgr, string? docTypeName, string? pubId, string? sysId, string? internalSubset, string? baseURI, string? xmlLang, System.Xml.XmlSpace xmlSpace, System.Text.Encoding? enc) { }
        public XmlParserContext(System.Xml.XmlNameTable? nt, System.Xml.XmlNamespaceManager? nsMgr, string? xmlLang, System.Xml.XmlSpace xmlSpace) { }
        public XmlParserContext(System.Xml.XmlNameTable? nt, System.Xml.XmlNamespaceManager? nsMgr, string? xmlLang, System.Xml.XmlSpace xmlSpace, System.Text.Encoding? enc) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string BaseURI { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DocTypeName { get { throw null; } set { } }
        public System.Text.Encoding? Encoding { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string InternalSubset { get { throw null; } set { } }
        public System.Xml.XmlNamespaceManager? NamespaceManager { get { throw null; } set { } }
        public System.Xml.XmlNameTable? NameTable { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string PublicId { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string SystemId { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string XmlLang { get { throw null; } set { } }
        public System.Xml.XmlSpace XmlSpace { get { throw null; } set { } }
    }
    public partial class XmlProcessingInstruction : System.Xml.XmlLinkedNode
    {
        protected internal XmlProcessingInstruction(string target, string data, System.Xml.XmlDocument doc) { }
        public string Data { get { throw null; } set { } }
        public override string InnerText { get { throw null; } set { } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string? Target { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override string Value { get { throw null; } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public partial class XmlQualifiedName
    {
        public static readonly System.Xml.XmlQualifiedName Empty;
        public XmlQualifiedName() { }
        public XmlQualifiedName(string? name) { }
        public XmlQualifiedName(string? name, string? ns) { }
        public bool IsEmpty { get { throw null; } }
        public string Name { get { throw null; } }
        public string Namespace { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Xml.XmlQualifiedName? a, System.Xml.XmlQualifiedName? b) { throw null; }
        public static bool operator !=(System.Xml.XmlQualifiedName? a, System.Xml.XmlQualifiedName? b) { throw null; }
        public override string ToString() { throw null; }
        public static string ToString(string name, string ns) { throw null; }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    [System.Diagnostics.DebuggerDisplayAttribute("{debuggerDisplayProxy}")]
    public abstract partial class XmlReader : System.IDisposable
    {
        protected XmlReader() { }
        public abstract int AttributeCount { get; }
        public abstract string BaseURI { get; }
        public virtual bool CanReadBinaryContent { get { throw null; } }
        public virtual bool CanReadValueChunk { get { throw null; } }
        public virtual bool CanResolveEntity { get { throw null; } }
        public abstract int Depth { get; }
        public abstract bool EOF { get; }
        public virtual bool HasAttributes { get { throw null; } }
        public virtual bool HasValue { get { throw null; } }
        public virtual bool IsDefault { get { throw null; } }
        public abstract bool IsEmptyElement { get; }
        public virtual string this[int i] { get { throw null; } }
        public virtual string? this[string name] { get { throw null; } }
        public virtual string? this[string name, string? namespaceURI] { get { throw null; } }
        public abstract string LocalName { get; }
        public virtual string Name { get { throw null; } }
        public abstract string NamespaceURI { get; }
        public abstract System.Xml.XmlNameTable NameTable { get; }
        public abstract System.Xml.XmlNodeType NodeType { get; }
        public abstract string Prefix { get; }
        public virtual char QuoteChar { get { throw null; } }
        public abstract System.Xml.ReadState ReadState { get; }
        public virtual System.Xml.Schema.IXmlSchemaInfo? SchemaInfo { get { throw null; } }
        public virtual System.Xml.XmlReaderSettings? Settings { get { throw null; } }
        public abstract string Value { get; }
        public virtual System.Type ValueType { get { throw null; } }
        public virtual string XmlLang { get { throw null; } }
        public virtual System.Xml.XmlSpace XmlSpace { get { throw null; } }
        public virtual void Close() { }
        public static System.Xml.XmlReader Create(System.IO.Stream input) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings? settings) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings? settings, string? baseUri) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings? settings, System.Xml.XmlParserContext? inputContext) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.TextReader input) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings? settings) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings? settings, string? baseUri) { throw null; }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings? settings, System.Xml.XmlParserContext? inputContext) { throw null; }
        public static System.Xml.XmlReader Create(string inputUri) { throw null; }
        public static System.Xml.XmlReader Create(string inputUri, System.Xml.XmlReaderSettings? settings) { throw null; }
        public static System.Xml.XmlReader Create(string inputUri, System.Xml.XmlReaderSettings? settings, System.Xml.XmlParserContext? inputContext) { throw null; }
        public static System.Xml.XmlReader Create(System.Xml.XmlReader reader, System.Xml.XmlReaderSettings? settings) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract string GetAttribute(int i);
        public abstract string? GetAttribute(string name);
        public abstract string? GetAttribute(string name, string? namespaceURI);
        public virtual System.Threading.Tasks.Task<string> GetValueAsync() { throw null; }
        public static bool IsName(string str) { throw null; }
        public static bool IsNameToken(string str) { throw null; }
        public virtual bool IsStartElement() { throw null; }
        public virtual bool IsStartElement(string name) { throw null; }
        public virtual bool IsStartElement(string localname, string ns) { throw null; }
        public abstract string? LookupNamespace(string prefix);
        public virtual void MoveToAttribute(int i) { }
        public abstract bool MoveToAttribute(string name);
        public abstract bool MoveToAttribute(string name, string? ns);
        public virtual System.Xml.XmlNodeType MoveToContent() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Xml.XmlNodeType> MoveToContentAsync() { throw null; }
        public abstract bool MoveToElement();
        public abstract bool MoveToFirstAttribute();
        public abstract bool MoveToNextAttribute();
        public abstract bool Read();
        public virtual System.Threading.Tasks.Task<bool> ReadAsync() { throw null; }
        public abstract bool ReadAttributeValue();
        public virtual object ReadContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver? namespaceResolver) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadContentAsAsync(System.Type returnType, System.Xml.IXmlNamespaceResolver? namespaceResolver) { throw null; }
        public virtual int ReadContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count) { throw null; }
        public virtual int ReadContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count) { throw null; }
        public virtual bool ReadContentAsBoolean() { throw null; }
        public virtual System.DateTime ReadContentAsDateTime() { throw null; }
        public virtual System.DateTimeOffset ReadContentAsDateTimeOffset() { throw null; }
        public virtual decimal ReadContentAsDecimal() { throw null; }
        public virtual double ReadContentAsDouble() { throw null; }
        public virtual float ReadContentAsFloat() { throw null; }
        public virtual int ReadContentAsInt() { throw null; }
        public virtual long ReadContentAsLong() { throw null; }
        public virtual object ReadContentAsObject() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadContentAsObjectAsync() { throw null; }
        public virtual string ReadContentAsString() { throw null; }
        public virtual System.Threading.Tasks.Task<string> ReadContentAsStringAsync() { throw null; }
        public virtual object ReadElementContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { throw null; }
        public virtual object ReadElementContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadElementContentAsAsync(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { throw null; }
        public virtual int ReadElementContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count) { throw null; }
        public virtual int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count) { throw null; }
        public virtual bool ReadElementContentAsBoolean() { throw null; }
        public virtual bool ReadElementContentAsBoolean(string localName, string namespaceURI) { throw null; }
        public virtual System.DateTime ReadElementContentAsDateTime() { throw null; }
        public virtual System.DateTime ReadElementContentAsDateTime(string localName, string namespaceURI) { throw null; }
        public virtual decimal ReadElementContentAsDecimal() { throw null; }
        public virtual decimal ReadElementContentAsDecimal(string localName, string namespaceURI) { throw null; }
        public virtual double ReadElementContentAsDouble() { throw null; }
        public virtual double ReadElementContentAsDouble(string localName, string namespaceURI) { throw null; }
        public virtual float ReadElementContentAsFloat() { throw null; }
        public virtual float ReadElementContentAsFloat(string localName, string namespaceURI) { throw null; }
        public virtual int ReadElementContentAsInt() { throw null; }
        public virtual int ReadElementContentAsInt(string localName, string namespaceURI) { throw null; }
        public virtual long ReadElementContentAsLong() { throw null; }
        public virtual long ReadElementContentAsLong(string localName, string namespaceURI) { throw null; }
        public virtual object ReadElementContentAsObject() { throw null; }
        public virtual object ReadElementContentAsObject(string localName, string namespaceURI) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<object> ReadElementContentAsObjectAsync() { throw null; }
        public virtual string ReadElementContentAsString() { throw null; }
        public virtual string ReadElementContentAsString(string localName, string namespaceURI) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> ReadElementContentAsStringAsync() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadElementString() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadElementString(string name) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadElementString(string localname, string ns) { throw null; }
        public virtual void ReadEndElement() { }
        public virtual string ReadInnerXml() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> ReadInnerXmlAsync() { throw null; }
        public virtual string ReadOuterXml() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> ReadOuterXmlAsync() { throw null; }
        public virtual void ReadStartElement() { }
        public virtual void ReadStartElement(string name) { }
        public virtual void ReadStartElement(string localname, string ns) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ReadString() { throw null; }
        public virtual System.Xml.XmlReader ReadSubtree() { throw null; }
        public virtual bool ReadToDescendant(string name) { throw null; }
        public virtual bool ReadToDescendant(string localName, string namespaceURI) { throw null; }
        public virtual bool ReadToFollowing(string name) { throw null; }
        public virtual bool ReadToFollowing(string localName, string namespaceURI) { throw null; }
        public virtual bool ReadToNextSibling(string name) { throw null; }
        public virtual bool ReadToNextSibling(string localName, string namespaceURI) { throw null; }
        public virtual int ReadValueChunk(char[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task<int> ReadValueChunkAsync(char[] buffer, int index, int count) { throw null; }
        public abstract void ResolveEntity();
        public virtual void Skip() { }
        public virtual System.Threading.Tasks.Task SkipAsync() { throw null; }
    }
    public sealed partial class XmlReaderSettings
    {
        public XmlReaderSettings() { }
        public bool Async { get { throw null; } set { } }
        public bool CheckCharacters { get { throw null; } set { } }
        public bool CloseInput { get { throw null; } set { } }
        public System.Xml.ConformanceLevel ConformanceLevel { get { throw null; } set { } }
        public System.Xml.DtdProcessing DtdProcessing { get { throw null; } set { } }
        public bool IgnoreComments { get { throw null; } set { } }
        public bool IgnoreProcessingInstructions { get { throw null; } set { } }
        public bool IgnoreWhitespace { get { throw null; } set { } }
        public int LineNumberOffset { get { throw null; } set { } }
        public int LinePositionOffset { get { throw null; } set { } }
        public long MaxCharactersFromEntities { get { throw null; } set { } }
        public long MaxCharactersInDocument { get { throw null; } set { } }
        public System.Xml.XmlNameTable? NameTable { get { throw null; } set { } }
        [System.ObsoleteAttribute("XmlReaderSettings.ProhibitDtd has been deprecated. Use DtdProcessing instead.")]
        public bool ProhibitDtd { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaSet Schemas { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaValidationFlags ValidationFlags { get { throw null; } set { } }
        public System.Xml.ValidationType ValidationType { get { throw null; } set { } }
        public System.Xml.XmlResolver? XmlResolver { set { } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public System.Xml.XmlReaderSettings Clone() { throw null; }
        public void Reset() { }
    }
    public abstract partial class XmlResolver
    {
        protected XmlResolver() { }
        public virtual System.Net.ICredentials Credentials { set { } }
        public abstract object? GetEntity(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn);
        public virtual System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public virtual System.Uri ResolveUri(System.Uri? baseUri, string? relativeUri) { throw null; }
        public virtual bool SupportsType(System.Uri absoluteUri, System.Type? type) { throw null; }
    }
    public partial class XmlSecureResolver : System.Xml.XmlResolver
    {
        public XmlSecureResolver(System.Xml.XmlResolver resolver, string? securityUrl) { }
        public override System.Net.ICredentials Credentials { set { } }
        public override object? GetEntity(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public override System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public override System.Uri ResolveUri(System.Uri? baseUri, string? relativeUri) { throw null; }
    }
    public partial class XmlSignificantWhitespace : System.Xml.XmlCharacterData
    {
        protected internal XmlSignificantWhitespace(string? strData, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override System.Xml.XmlNode? PreviousText { get { throw null; } }
        public override string? Value { get { throw null; } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public enum XmlSpace
    {
        None = 0,
        Default = 1,
        Preserve = 2,
    }
    public partial class XmlText : System.Xml.XmlCharacterData
    {
        protected internal XmlText(string? strData, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override System.Xml.XmlNode? PreviousText { get { throw null; } }
        public override string? Value { get { throw null; } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public virtual System.Xml.XmlText SplitText(int offset) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class XmlTextReader : System.Xml.XmlReader, System.Xml.IXmlLineInfo, System.Xml.IXmlNamespaceResolver
    {
        protected XmlTextReader() { }
        public XmlTextReader(System.IO.Stream input) { }
        public XmlTextReader(System.IO.Stream input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(System.IO.Stream xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext? context) { }
        public XmlTextReader(System.IO.TextReader input) { }
        public XmlTextReader(System.IO.TextReader input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string url) { }
        public XmlTextReader(string url, System.IO.Stream input) { }
        public XmlTextReader(string url, System.IO.Stream input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string url, System.IO.TextReader input) { }
        public XmlTextReader(string url, System.IO.TextReader input, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string url, System.Xml.XmlNameTable nt) { }
        public XmlTextReader(string xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext? context) { }
        protected XmlTextReader(System.Xml.XmlNameTable nt) { }
        public override int AttributeCount { get { throw null; } }
        public override string BaseURI { get { throw null; } }
        public override bool CanReadBinaryContent { get { throw null; } }
        public override bool CanReadValueChunk { get { throw null; } }
        public override bool CanResolveEntity { get { throw null; } }
        public override int Depth { get { throw null; } }
        public System.Xml.DtdProcessing DtdProcessing { get { throw null; } set { } }
        public System.Text.Encoding? Encoding { get { throw null; } }
        public System.Xml.EntityHandling EntityHandling { get { throw null; } set { } }
        public override bool EOF { get { throw null; } }
        public override bool HasValue { get { throw null; } }
        public override bool IsDefault { get { throw null; } }
        public override bool IsEmptyElement { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public bool Namespaces { get { throw null; } set { } }
        public override string NamespaceURI { get { throw null; } }
        public override System.Xml.XmlNameTable NameTable { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public bool Normalization { get { throw null; } set { } }
        public override string Prefix { get { throw null; } }
        [System.ObsoleteAttribute("XmlTextReader.ProhibitDtd has been deprecated. Use DtdProcessing instead.")]
        public bool ProhibitDtd { get { throw null; } set { } }
        public override char QuoteChar { get { throw null; } }
        public override System.Xml.ReadState ReadState { get { throw null; } }
        public override string Value { get { throw null; } }
        public System.Xml.WhitespaceHandling WhitespaceHandling { get { throw null; } set { } }
        public override string XmlLang { get { throw null; } }
        public System.Xml.XmlResolver? XmlResolver { set { } }
        public override System.Xml.XmlSpace XmlSpace { get { throw null; } }
        public override void Close() { }
        public override string GetAttribute(int i) { throw null; }
        public override string? GetAttribute(string name) { throw null; }
        public override string? GetAttribute(string localName, string? namespaceURI) { throw null; }
        public System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { throw null; }
        public System.IO.TextReader GetRemainder() { throw null; }
        public bool HasLineInfo() { throw null; }
        public override string? LookupNamespace(string prefix) { throw null; }
        public override void MoveToAttribute(int i) { }
        public override bool MoveToAttribute(string name) { throw null; }
        public override bool MoveToAttribute(string localName, string? namespaceURI) { throw null; }
        public override bool MoveToElement() { throw null; }
        public override bool MoveToFirstAttribute() { throw null; }
        public override bool MoveToNextAttribute() { throw null; }
        public override bool Read() { throw null; }
        public override bool ReadAttributeValue() { throw null; }
        public int ReadBase64(byte[] array, int offset, int len) { throw null; }
        public int ReadBinHex(byte[] array, int offset, int len) { throw null; }
        public int ReadChars(char[] buffer, int index, int count) { throw null; }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public override string ReadString() { throw null; }
        public void ResetState() { }
        public override void ResolveEntity() { }
        public override void Skip() { }
        System.Collections.Generic.IDictionary<string, string> System.Xml.IXmlNamespaceResolver.GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { throw null; }
        string? System.Xml.IXmlNamespaceResolver.LookupNamespace(string prefix) { throw null; }
        string? System.Xml.IXmlNamespaceResolver.LookupPrefix(string namespaceName) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public partial class XmlTextWriter : System.Xml.XmlWriter
    {
        public XmlTextWriter(System.IO.Stream w, System.Text.Encoding? encoding) { }
        public XmlTextWriter(System.IO.TextWriter w) { }
        public XmlTextWriter(string filename, System.Text.Encoding? encoding) { }
        public System.IO.Stream? BaseStream { get { throw null; } }
        public System.Xml.Formatting Formatting { get { throw null; } set { } }
        public int Indentation { get { throw null; } set { } }
        public char IndentChar { get { throw null; } set { } }
        public bool Namespaces { get { throw null; } set { } }
        public char QuoteChar { get { throw null; } set { } }
        public override System.Xml.WriteState WriteState { get { throw null; } }
        public override string? XmlLang { get { throw null; } }
        public override System.Xml.XmlSpace XmlSpace { get { throw null; } }
        public override void Close() { }
        public override void Flush() { }
        public override string? LookupPrefix(string ns) { throw null; }
        public override void WriteBase64(byte[] buffer, int index, int count) { }
        public override void WriteBinHex(byte[] buffer, int index, int count) { }
        public override void WriteCData(string? text) { }
        public override void WriteCharEntity(char ch) { }
        public override void WriteChars(char[] buffer, int index, int count) { }
        public override void WriteComment(string? text) { }
        public override void WriteDocType(string name, string? pubid, string? sysid, string? subset) { }
        public override void WriteEndAttribute() { }
        public override void WriteEndDocument() { }
        public override void WriteEndElement() { }
        public override void WriteEntityRef(string name) { }
        public override void WriteFullEndElement() { }
        public override void WriteName(string name) { }
        public override void WriteNmToken(string name) { }
        public override void WriteProcessingInstruction(string name, string? text) { }
        public override void WriteQualifiedName(string localName, string? ns) { }
        public override void WriteRaw(char[] buffer, int index, int count) { }
        public override void WriteRaw(string data) { }
        public override void WriteStartAttribute(string? prefix, string localName, string? ns) { }
        public override void WriteStartDocument() { }
        public override void WriteStartDocument(bool standalone) { }
        public override void WriteStartElement(string? prefix, string localName, string? ns) { }
        public override void WriteString(string? text) { }
        public override void WriteSurrogateCharEntity(char lowChar, char highChar) { }
        public override void WriteWhitespace(string? ws) { }
    }
    public enum XmlTokenizedType
    {
        CDATA = 0,
        ID = 1,
        IDREF = 2,
        IDREFS = 3,
        ENTITY = 4,
        ENTITIES = 5,
        NMTOKEN = 6,
        NMTOKENS = 7,
        NOTATION = 8,
        ENUMERATION = 9,
        QName = 10,
        NCName = 11,
        None = 12,
    }
    public partial class XmlUrlResolver : System.Xml.XmlResolver
    {
        public XmlUrlResolver() { }
        public System.Net.Cache.RequestCachePolicy CachePolicy { set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
        public override System.Net.ICredentials? Credentials { set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
        public System.Net.IWebProxy? Proxy { set { } }
        public override object? GetEntity(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public override System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public override System.Uri ResolveUri(System.Uri? baseUri, string? relativeUri) { throw null; }
    }
    [System.ObsoleteAttribute("XmlValidatingReader has been deprecated. Use XmlReader created by XmlReader.Create() method using appropriate XmlReaderSettings instead.")]
    public partial class XmlValidatingReader : System.Xml.XmlReader, System.Xml.IXmlLineInfo, System.Xml.IXmlNamespaceResolver
    {
        public XmlValidatingReader(System.IO.Stream xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext context) { }
        public XmlValidatingReader(string xmlFragment, System.Xml.XmlNodeType fragType, System.Xml.XmlParserContext context) { }
        public XmlValidatingReader(System.Xml.XmlReader reader) { }
        public override int AttributeCount { get { throw null; } }
        public override string BaseURI { get { throw null; } }
        public override bool CanReadBinaryContent { get { throw null; } }
        public override bool CanResolveEntity { get { throw null; } }
        public override int Depth { get { throw null; } }
        public System.Text.Encoding? Encoding { get { throw null; } }
        public System.Xml.EntityHandling EntityHandling { get { throw null; } set { } }
        public override bool EOF { get { throw null; } }
        public override bool HasValue { get { throw null; } }
        public override bool IsDefault { get { throw null; } }
        public override bool IsEmptyElement { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public bool Namespaces { get { throw null; } set { } }
        public override string NamespaceURI { get { throw null; } }
        public override System.Xml.XmlNameTable NameTable { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override string Prefix { get { throw null; } }
        public override char QuoteChar { get { throw null; } }
        public System.Xml.XmlReader Reader { get { throw null; } }
        public override System.Xml.ReadState ReadState { get { throw null; } }
        public System.Xml.Schema.XmlSchemaCollection Schemas { get { throw null; } }
        public object? SchemaType { get { throw null; } }
        public System.Xml.ValidationType ValidationType { get { throw null; } set { } }
        public override string Value { get { throw null; } }
        public override string XmlLang { get { throw null; } }
        public System.Xml.XmlResolver XmlResolver { set { } }
        public override System.Xml.XmlSpace XmlSpace { get { throw null; } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public override void Close() { }
        public override string GetAttribute(int i) { throw null; }
        public override string? GetAttribute(string name) { throw null; }
        public override string? GetAttribute(string localName, string? namespaceURI) { throw null; }
        public bool HasLineInfo() { throw null; }
        public override string? LookupNamespace(string prefix) { throw null; }
        public override void MoveToAttribute(int i) { }
        public override bool MoveToAttribute(string name) { throw null; }
        public override bool MoveToAttribute(string localName, string? namespaceURI) { throw null; }
        public override bool MoveToElement() { throw null; }
        public override bool MoveToFirstAttribute() { throw null; }
        public override bool MoveToNextAttribute() { throw null; }
        public override bool Read() { throw null; }
        public override bool ReadAttributeValue() { throw null; }
        public override int ReadContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public override int ReadContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count) { throw null; }
        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { throw null; }
        public override string ReadString() { throw null; }
        public object? ReadTypedValue() { throw null; }
        public override void ResolveEntity() { }
        System.Collections.Generic.IDictionary<string, string> System.Xml.IXmlNamespaceResolver.GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { throw null; }
        string System.Xml.IXmlNamespaceResolver.LookupNamespace(string prefix) { throw null; }
        string System.Xml.IXmlNamespaceResolver.LookupPrefix(string namespaceName) { throw null; }
    }
    public partial class XmlWhitespace : System.Xml.XmlCharacterData
    {
        protected internal XmlWhitespace(string? strData, System.Xml.XmlDocument doc) : base (default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { throw null; } }
        public override string Name { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override System.Xml.XmlNode? ParentNode { get { throw null; } }
        public override System.Xml.XmlNode? PreviousText { get { throw null; } }
        public override string? Value { get { throw null; } set { } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
    }
    public abstract partial class XmlWriter : System.IAsyncDisposable, System.IDisposable
    {
        protected XmlWriter() { }
        public virtual System.Xml.XmlWriterSettings? Settings { get { throw null; } }
        public abstract System.Xml.WriteState WriteState { get; }
        public virtual string? XmlLang { get { throw null; } }
        public virtual System.Xml.XmlSpace XmlSpace { get { throw null; } }
        public virtual void Close() { }
        public static System.Xml.XmlWriter Create(System.IO.Stream output) { throw null; }
        public static System.Xml.XmlWriter Create(System.IO.Stream output, System.Xml.XmlWriterSettings? settings) { throw null; }
        public static System.Xml.XmlWriter Create(System.IO.TextWriter output) { throw null; }
        public static System.Xml.XmlWriter Create(System.IO.TextWriter output, System.Xml.XmlWriterSettings? settings) { throw null; }
        public static System.Xml.XmlWriter Create(string outputFileName) { throw null; }
        public static System.Xml.XmlWriter Create(string outputFileName, System.Xml.XmlWriterSettings? settings) { throw null; }
        public static System.Xml.XmlWriter Create(System.Text.StringBuilder output) { throw null; }
        public static System.Xml.XmlWriter Create(System.Text.StringBuilder output, System.Xml.XmlWriterSettings? settings) { throw null; }
        public static System.Xml.XmlWriter Create(System.Xml.XmlWriter output) { throw null; }
        public static System.Xml.XmlWriter Create(System.Xml.XmlWriter output, System.Xml.XmlWriterSettings? settings) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void Flush();
        public virtual System.Threading.Tasks.Task FlushAsync() { throw null; }
        public abstract string? LookupPrefix(string ns);
        public virtual void WriteAttributes(System.Xml.XmlReader reader, bool defattr) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task WriteAttributesAsync(System.Xml.XmlReader reader, bool defattr) { throw null; }
        public void WriteAttributeString(string localName, string? value) { }
        public void WriteAttributeString(string localName, string? ns, string? value) { }
        public void WriteAttributeString(string? prefix, string localName, string? ns, string? value) { }
        public System.Threading.Tasks.Task WriteAttributeStringAsync(string? prefix, string localName, string? ns, string? value) { throw null; }
        public abstract void WriteBase64(byte[] buffer, int index, int count);
        public virtual System.Threading.Tasks.Task WriteBase64Async(byte[] buffer, int index, int count) { throw null; }
        public virtual void WriteBinHex(byte[] buffer, int index, int count) { }
        public virtual System.Threading.Tasks.Task WriteBinHexAsync(byte[] buffer, int index, int count) { throw null; }
        public abstract void WriteCData(string? text);
        public virtual System.Threading.Tasks.Task WriteCDataAsync(string? text) { throw null; }
        public abstract void WriteCharEntity(char ch);
        public virtual System.Threading.Tasks.Task WriteCharEntityAsync(char ch) { throw null; }
        public abstract void WriteChars(char[] buffer, int index, int count);
        public virtual System.Threading.Tasks.Task WriteCharsAsync(char[] buffer, int index, int count) { throw null; }
        public abstract void WriteComment(string? text);
        public virtual System.Threading.Tasks.Task WriteCommentAsync(string? text) { throw null; }
        public abstract void WriteDocType(string name, string? pubid, string? sysid, string? subset);
        public virtual System.Threading.Tasks.Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset) { throw null; }
        public void WriteElementString(string localName, string? value) { }
        public void WriteElementString(string localName, string? ns, string? value) { }
        public void WriteElementString(string? prefix, string localName, string? ns, string? value) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task WriteElementStringAsync(string? prefix, string localName, string? ns, string value) { throw null; }
        public abstract void WriteEndAttribute();
        protected internal virtual System.Threading.Tasks.Task WriteEndAttributeAsync() { throw null; }
        public abstract void WriteEndDocument();
        public virtual System.Threading.Tasks.Task WriteEndDocumentAsync() { throw null; }
        public abstract void WriteEndElement();
        public virtual System.Threading.Tasks.Task WriteEndElementAsync() { throw null; }
        public abstract void WriteEntityRef(string name);
        public virtual System.Threading.Tasks.Task WriteEntityRefAsync(string name) { throw null; }
        public abstract void WriteFullEndElement();
        public virtual System.Threading.Tasks.Task WriteFullEndElementAsync() { throw null; }
        public virtual void WriteName(string name) { }
        public virtual System.Threading.Tasks.Task WriteNameAsync(string name) { throw null; }
        public virtual void WriteNmToken(string name) { }
        public virtual System.Threading.Tasks.Task WriteNmTokenAsync(string name) { throw null; }
        public virtual void WriteNode(System.Xml.XmlReader reader, bool defattr) { }
        public virtual void WriteNode(System.Xml.XPath.XPathNavigator navigator, bool defattr) { }
        public virtual System.Threading.Tasks.Task WriteNodeAsync(System.Xml.XmlReader reader, bool defattr) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task WriteNodeAsync(System.Xml.XPath.XPathNavigator navigator, bool defattr) { throw null; }
        public abstract void WriteProcessingInstruction(string name, string? text);
        public virtual System.Threading.Tasks.Task WriteProcessingInstructionAsync(string name, string? text) { throw null; }
        public virtual void WriteQualifiedName(string localName, string? ns) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task WriteQualifiedNameAsync(string localName, string? ns) { throw null; }
        public abstract void WriteRaw(char[] buffer, int index, int count);
        public abstract void WriteRaw(string data);
        public virtual System.Threading.Tasks.Task WriteRawAsync(char[] buffer, int index, int count) { throw null; }
        public virtual System.Threading.Tasks.Task WriteRawAsync(string data) { throw null; }
        public void WriteStartAttribute(string localName) { }
        public void WriteStartAttribute(string localName, string? ns) { }
        public abstract void WriteStartAttribute(string? prefix, string localName, string? ns);
        protected internal virtual System.Threading.Tasks.Task WriteStartAttributeAsync(string? prefix, string localName, string? ns) { throw null; }
        public abstract void WriteStartDocument();
        public abstract void WriteStartDocument(bool standalone);
        public virtual System.Threading.Tasks.Task WriteStartDocumentAsync() { throw null; }
        public virtual System.Threading.Tasks.Task WriteStartDocumentAsync(bool standalone) { throw null; }
        public void WriteStartElement(string localName) { }
        public void WriteStartElement(string localName, string? ns) { }
        public abstract void WriteStartElement(string? prefix, string localName, string? ns);
        public virtual System.Threading.Tasks.Task WriteStartElementAsync(string? prefix, string localName, string? ns) { throw null; }
        public abstract void WriteString(string? text);
        public virtual System.Threading.Tasks.Task WriteStringAsync(string? text) { throw null; }
        public abstract void WriteSurrogateCharEntity(char lowChar, char highChar);
        public virtual System.Threading.Tasks.Task WriteSurrogateCharEntityAsync(char lowChar, char highChar) { throw null; }
        public virtual void WriteValue(bool value) { }
        public virtual void WriteValue(System.DateTime value) { }
        public virtual void WriteValue(System.DateTimeOffset value) { }
        public virtual void WriteValue(decimal value) { }
        public virtual void WriteValue(double value) { }
        public virtual void WriteValue(int value) { }
        public virtual void WriteValue(long value) { }
        public virtual void WriteValue(object value) { }
        public virtual void WriteValue(float value) { }
        public virtual void WriteValue(string? value) { }
        public abstract void WriteWhitespace(string? ws);
        public virtual System.Threading.Tasks.Task WriteWhitespaceAsync(string? ws) { throw null; }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
    }
    public sealed partial class XmlWriterSettings
    {
        public XmlWriterSettings() { }
        public bool Async { get { throw null; } set { } }
        public bool CheckCharacters { get { throw null; } set { } }
        public bool CloseOutput { get { throw null; } set { } }
        public System.Xml.ConformanceLevel ConformanceLevel { get { throw null; } set { } }
        public bool DoNotEscapeUriAttributes { get { throw null; } set { } }
        public System.Text.Encoding Encoding { get { throw null; } set { } }
        public bool Indent { get { throw null; } set { } }
        public string IndentChars { get { throw null; } set { } }
        public System.Xml.NamespaceHandling NamespaceHandling { get { throw null; } set { } }
        public string NewLineChars { get { throw null; } set { } }
        public System.Xml.NewLineHandling NewLineHandling { get { throw null; } set { } }
        public bool NewLineOnAttributes { get { throw null; } set { } }
        public bool OmitXmlDeclaration { get { throw null; } set { } }
        public System.Xml.XmlOutputMethod OutputMethod { get { throw null; } }
        public bool WriteEndDocumentOnClose { get { throw null; } set { } }
        public System.Xml.XmlWriterSettings Clone() { throw null; }
        public void Reset() { }
    }
}
namespace System.Xml.Resolvers
{
    [System.FlagsAttribute]
    public enum XmlKnownDtds
    {
        None = 0,
        Xhtml10 = 1,
        Rss091 = 2,
        All = 65535,
    }
    public partial class XmlPreloadedResolver : System.Xml.XmlResolver
    {
        public XmlPreloadedResolver() { }
        public XmlPreloadedResolver(System.Xml.Resolvers.XmlKnownDtds preloadedDtds) { }
        public XmlPreloadedResolver(System.Xml.XmlResolver? fallbackResolver) { }
        public XmlPreloadedResolver(System.Xml.XmlResolver? fallbackResolver, System.Xml.Resolvers.XmlKnownDtds preloadedDtds) { }
        public XmlPreloadedResolver(System.Xml.XmlResolver? fallbackResolver, System.Xml.Resolvers.XmlKnownDtds preloadedDtds, System.Collections.Generic.IEqualityComparer<System.Uri>? uriComparer) { }
        public override System.Net.ICredentials Credentials { set { } }
        public System.Collections.Generic.IEnumerable<System.Uri> PreloadedUris { get { throw null; } }
        public void Add(System.Uri uri, byte[] value) { }
        public void Add(System.Uri uri, byte[] value, int offset, int count) { }
        public void Add(System.Uri uri, System.IO.Stream value) { }
        public void Add(System.Uri uri, string value) { }
        public override object? GetEntity(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public override System.Threading.Tasks.Task<object> GetEntityAsync(System.Uri absoluteUri, string? role, System.Type? ofObjectToReturn) { throw null; }
        public void Remove(System.Uri uri) { }
        public override System.Uri ResolveUri(System.Uri? baseUri, string? relativeUri) { throw null; }
        public override bool SupportsType(System.Uri absoluteUri, System.Type? type) { throw null; }
    }
}
namespace System.Xml.Schema
{
    public partial interface IXmlSchemaInfo
    {
        bool IsDefault { get; }
        bool IsNil { get; }
        System.Xml.Schema.XmlSchemaSimpleType? MemberType { get; }
        System.Xml.Schema.XmlSchemaAttribute? SchemaAttribute { get; }
        System.Xml.Schema.XmlSchemaElement? SchemaElement { get; }
        System.Xml.Schema.XmlSchemaType? SchemaType { get; }
        System.Xml.Schema.XmlSchemaValidity Validity { get; }
    }
    public partial class ValidationEventArgs : System.EventArgs
    {
        internal ValidationEventArgs() { }
        public System.Xml.Schema.XmlSchemaException Exception { get { throw null; } }
        public string Message { get { throw null; } }
        public System.Xml.Schema.XmlSeverityType Severity { get { throw null; } }
    }
    public delegate void ValidationEventHandler(object? sender, System.Xml.Schema.ValidationEventArgs e);
    public sealed partial class XmlAtomicValue : System.Xml.XPath.XPathItem, System.ICloneable
    {
        internal XmlAtomicValue() { }
        public override bool IsNode { get { throw null; } }
        public override object TypedValue { get { throw null; } }
        public override string Value { get { throw null; } }
        public override bool ValueAsBoolean { get { throw null; } }
        public override System.DateTime ValueAsDateTime { get { throw null; } }
        public override double ValueAsDouble { get { throw null; } }
        public override int ValueAsInt { get { throw null; } }
        public override long ValueAsLong { get { throw null; } }
        public override System.Type ValueType { get { throw null; } }
        public override System.Xml.Schema.XmlSchemaType XmlType { get { throw null; } }
        public System.Xml.Schema.XmlAtomicValue Clone() { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public override object ValueAs(System.Type type, System.Xml.IXmlNamespaceResolver? nsResolver) { throw null; }
    }
    [System.Xml.Serialization.XmlRootAttribute("schema", Namespace="http://www.w3.org/2001/XMLSchema")]
    public partial class XmlSchema : System.Xml.Schema.XmlSchemaObject
    {
        public const string InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        public const string Namespace = "http://www.w3.org/2001/XMLSchema";
        public XmlSchema() { }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaForm.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("attributeFormDefault")]
        public System.Xml.Schema.XmlSchemaForm AttributeFormDefault { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable AttributeGroups { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Attributes { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaDerivationMethod.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("blockDefault")]
        public System.Xml.Schema.XmlSchemaDerivationMethod BlockDefault { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaForm.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("elementFormDefault")]
        public System.Xml.Schema.XmlSchemaForm ElementFormDefault { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Elements { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaDerivationMethod.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("finalDefault")]
        public System.Xml.Schema.XmlSchemaDerivationMethod FinalDefault { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Groups { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string? Id { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("import", typeof(System.Xml.Schema.XmlSchemaImport))]
        [System.Xml.Serialization.XmlElementAttribute("include", typeof(System.Xml.Schema.XmlSchemaInclude))]
        [System.Xml.Serialization.XmlElementAttribute("redefine", typeof(System.Xml.Schema.XmlSchemaRedefine))]
        public System.Xml.Schema.XmlSchemaObjectCollection Includes { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool IsCompiled { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroup))]
        [System.Xml.Serialization.XmlElementAttribute("complexType", typeof(System.Xml.Schema.XmlSchemaComplexType))]
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroup))]
        [System.Xml.Serialization.XmlElementAttribute("notation", typeof(System.Xml.Schema.XmlSchemaNotation))]
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaObjectCollection Items { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Notations { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable SchemaTypes { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("targetNamespace", DataType="anyURI")]
        public string? TargetNamespace { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[]? UnhandledAttributes { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("version", DataType="token")]
        public string? Version { get { throw null; } set { } }
        [System.ObsoleteAttribute("XmlSchema.Compile has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation.")]
        public void Compile(System.Xml.Schema.ValidationEventHandler? validationEventHandler) { }
        [System.ObsoleteAttribute("XmlSchema.Compile has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation.")]
        public void Compile(System.Xml.Schema.ValidationEventHandler? validationEventHandler, System.Xml.XmlResolver? resolver) { }
        public static System.Xml.Schema.XmlSchema? Read(System.IO.Stream stream, System.Xml.Schema.ValidationEventHandler? validationEventHandler) { throw null; }
        public static System.Xml.Schema.XmlSchema? Read(System.IO.TextReader reader, System.Xml.Schema.ValidationEventHandler? validationEventHandler) { throw null; }
        public static System.Xml.Schema.XmlSchema? Read(System.Xml.XmlReader reader, System.Xml.Schema.ValidationEventHandler? validationEventHandler) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Write(System.IO.Stream stream) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Write(System.IO.Stream stream, System.Xml.XmlNamespaceManager? namespaceManager) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Write(System.IO.TextWriter writer) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Write(System.IO.TextWriter writer, System.Xml.XmlNamespaceManager? namespaceManager) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Write(System.Xml.XmlWriter writer) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Write(System.Xml.XmlWriter writer, System.Xml.XmlNamespaceManager? namespaceManager) { }
    }
    public partial class XmlSchemaAll : System.Xml.Schema.XmlSchemaGroupBase
    {
        public XmlSchemaAll() { }
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { throw null; } }
    }
    public partial class XmlSchemaAnnotated : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaAnnotated() { }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        public System.Xml.Schema.XmlSchemaAnnotation? Annotation { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType = "ID")]
        public string? Id { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[]? UnhandledAttributes { get { throw null; } set { } }
    }
    public partial class XmlSchemaAnnotation : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaAnnotation() { }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string? Id { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("appinfo", typeof(System.Xml.Schema.XmlSchemaAppInfo))]
        [System.Xml.Serialization.XmlElementAttribute("documentation", typeof(System.Xml.Schema.XmlSchemaDocumentation))]
        public System.Xml.Schema.XmlSchemaObjectCollection Items { get { throw null; } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[]? UnhandledAttributes { get { throw null; } set { } }
    }
    public partial class XmlSchemaAny : System.Xml.Schema.XmlSchemaParticle
    {
        public XmlSchemaAny() { }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string? Namespace { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaContentProcessing.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("processContents")]
        public System.Xml.Schema.XmlSchemaContentProcessing ProcessContents { get { throw null; } set { } }
    }
    public partial class XmlSchemaAnyAttribute : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAnyAttribute() { }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string? Namespace { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaContentProcessing.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("processContents")]
        public System.Xml.Schema.XmlSchemaContentProcessing ProcessContents { get { throw null; } set { } }
    }
    public partial class XmlSchemaAppInfo : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaAppInfo() { }
        [System.Xml.Serialization.XmlAnyElementAttribute]
        [System.Xml.Serialization.XmlTextAttribute]
        public System.Xml.XmlNode?[]? Markup { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("source", DataType="anyURI")]
        public string? Source { get { throw null; } set { } }
    }
    public partial class XmlSchemaAttribute : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAttribute() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaSimpleType? AttributeSchemaType { get { throw null; } }
        [System.ObsoleteAttribute("XmlSchemaAttribute.AttributeType has been deprecated. Use the AttributeSchemaType property that returns a strongly typed attribute type instead.")]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public object? AttributeType { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("default")]
        public string? DefaultValue { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("fixed")]
        public string? FixedValue { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaForm.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("form")]
        public System.Xml.Schema.XmlSchemaForm Form { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public System.Xml.XmlQualifiedName RefName { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType")]
        public System.Xml.Schema.XmlSchemaSimpleType? SchemaType { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public System.Xml.XmlQualifiedName SchemaTypeName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaUse.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("use")]
        public System.Xml.Schema.XmlSchemaUse Use { get { throw null; } set { } }
    }
    public partial class XmlSchemaAttributeGroup : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAttributeGroup() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AnyAttribute { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaAttributeGroup? RedefinedAttributeGroup { get { throw null; } }
    }
    public partial class XmlSchemaAttributeGroupRef : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaAttributeGroupRef() { }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Xml.XmlQualifiedName RefName { get { throw null; } set { } }
    }
    public partial class XmlSchemaChoice : System.Xml.Schema.XmlSchemaGroupBase
    {
        public XmlSchemaChoice() { }
        [System.Xml.Serialization.XmlElementAttribute("any", typeof(System.Xml.Schema.XmlSchemaAny))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { throw null; } }
    }
    [System.ObsoleteAttribute("XmlSchemaCollection has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation.")]
    public sealed partial class XmlSchemaCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public XmlSchemaCollection() { }
        public XmlSchemaCollection(System.Xml.XmlNameTable nametable) { }
        public int Count { get { throw null; } }
        public System.Xml.Schema.XmlSchema? this[string? ns] { get { throw null; } }
        public System.Xml.XmlNameTable NameTable { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public System.Xml.Schema.XmlSchema? Add(string? ns, string uri) { throw null; }
        public System.Xml.Schema.XmlSchema? Add(string? ns, System.Xml.XmlReader reader) { throw null; }
        public System.Xml.Schema.XmlSchema? Add(string? ns, System.Xml.XmlReader reader, System.Xml.XmlResolver? resolver) { throw null; }
        public System.Xml.Schema.XmlSchema? Add(System.Xml.Schema.XmlSchema schema) { throw null; }
        public System.Xml.Schema.XmlSchema? Add(System.Xml.Schema.XmlSchema schema, System.Xml.XmlResolver? resolver) { throw null; }
        public void Add(System.Xml.Schema.XmlSchemaCollection schema) { }
        public bool Contains(string? ns) { throw null; }
        public bool Contains(System.Xml.Schema.XmlSchema schema) { throw null; }
        public void CopyTo(System.Xml.Schema.XmlSchema[] array, int index) { }
        public System.Xml.Schema.XmlSchemaCollectionEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class XmlSchemaCollectionEnumerator : System.Collections.IEnumerator
    {
        internal XmlSchemaCollectionEnumerator() { }
        public System.Xml.Schema.XmlSchema? Current { get { throw null; } }
        object? System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        bool System.Collections.IEnumerator.MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
    }
    public sealed partial class XmlSchemaCompilationSettings
    {
        public XmlSchemaCompilationSettings() { }
        public bool EnableUpaCheck { get { throw null; } set { } }
    }
    public partial class XmlSchemaComplexContent : System.Xml.Schema.XmlSchemaContentModel
    {
        public XmlSchemaComplexContent() { }
        [System.Xml.Serialization.XmlElementAttribute("extension", typeof(System.Xml.Schema.XmlSchemaComplexContentExtension))]
        [System.Xml.Serialization.XmlElementAttribute("restriction", typeof(System.Xml.Schema.XmlSchemaComplexContentRestriction))]
        public override System.Xml.Schema.XmlSchemaContent? Content { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("mixed")]
        public bool IsMixed { get { throw null; } set { } }
    }
    public partial class XmlSchemaComplexContentExtension : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaComplexContentExtension() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AnyAttribute { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaParticle? Particle { get { throw null; } set { } }
    }
    public partial class XmlSchemaComplexContentRestriction : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaComplexContentRestriction() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AnyAttribute { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaParticle? Particle { get { throw null; } set { } }
    }
    public partial class XmlSchemaComplexType : System.Xml.Schema.XmlSchemaType
    {
        public XmlSchemaComplexType() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AnyAttribute { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable AttributeUses { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AttributeWildcard { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaDerivationMethod.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("block")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Block { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod BlockResolved { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("complexContent", typeof(System.Xml.Schema.XmlSchemaComplexContent))]
        [System.Xml.Serialization.XmlElementAttribute("simpleContent", typeof(System.Xml.Schema.XmlSchemaSimpleContent))]
        public System.Xml.Schema.XmlSchemaContentModel? ContentModel { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaContentType ContentType { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaParticle ContentTypeParticle { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("abstract")]
        public bool IsAbstract { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("mixed")]
        public override bool IsMixed { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaParticle? Particle { get { throw null; } set { } }
    }
    public abstract partial class XmlSchemaContent : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaContent() { }
    }
    public abstract partial class XmlSchemaContentModel : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaContentModel() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public abstract System.Xml.Schema.XmlSchemaContent? Content { get; set; }
    }
    public enum XmlSchemaContentProcessing
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 0,
        [System.Xml.Serialization.XmlEnumAttribute("skip")]
        Skip = 1,
        [System.Xml.Serialization.XmlEnumAttribute("lax")]
        Lax = 2,
        [System.Xml.Serialization.XmlEnumAttribute("strict")]
        Strict = 3,
    }
    public enum XmlSchemaContentType
    {
        TextOnly = 0,
        Empty = 1,
        ElementOnly = 2,
        Mixed = 3,
    }
    public abstract partial class XmlSchemaDatatype
    {
        internal XmlSchemaDatatype() { }
        public abstract System.Xml.XmlTokenizedType TokenizedType { get; }
        public virtual System.Xml.Schema.XmlTypeCode TypeCode { get { throw null; } }
        public abstract System.Type ValueType { get; }
        public virtual System.Xml.Schema.XmlSchemaDatatypeVariety Variety { get { throw null; } }
        public virtual object ChangeType(object value, System.Type targetType) { throw null; }
        public virtual object ChangeType(object value, System.Type targetType, System.Xml.IXmlNamespaceResolver namespaceResolver) { throw null; }
        public virtual bool IsDerivedFrom(System.Xml.Schema.XmlSchemaDatatype datatype) { throw null; }
        public abstract object ParseValue(string s, System.Xml.XmlNameTable? nameTable, System.Xml.IXmlNamespaceResolver? nsmgr);
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
        [System.Xml.Serialization.XmlEnumAttribute("")]
        Empty = 0,
        [System.Xml.Serialization.XmlEnumAttribute("substitution")]
        Substitution = 1,
        [System.Xml.Serialization.XmlEnumAttribute("extension")]
        Extension = 2,
        [System.Xml.Serialization.XmlEnumAttribute("restriction")]
        Restriction = 4,
        [System.Xml.Serialization.XmlEnumAttribute("list")]
        List = 8,
        [System.Xml.Serialization.XmlEnumAttribute("union")]
        Union = 16,
        [System.Xml.Serialization.XmlEnumAttribute("#all")]
        All = 255,
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 256,
    }
    public partial class XmlSchemaDocumentation : System.Xml.Schema.XmlSchemaObject
    {
        public XmlSchemaDocumentation() { }
        [System.Xml.Serialization.XmlAttributeAttribute("xml:lang")]
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public string? Language { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAnyElementAttribute]
        [System.Xml.Serialization.XmlTextAttribute]
        public System.Xml.XmlNode?[]? Markup { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("source", DataType="anyURI")]
        public string? Source { get { throw null; } set { } }
    }
    public partial class XmlSchemaElement : System.Xml.Schema.XmlSchemaParticle
    {
        public XmlSchemaElement() { }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaDerivationMethod.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("block")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Block { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod BlockResolved { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("key", typeof(System.Xml.Schema.XmlSchemaKey))]
        [System.Xml.Serialization.XmlElementAttribute("keyref", typeof(System.Xml.Schema.XmlSchemaKeyref))]
        [System.Xml.Serialization.XmlElementAttribute("unique", typeof(System.Xml.Schema.XmlSchemaUnique))]
        public System.Xml.Schema.XmlSchemaObjectCollection Constraints { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("default")]
        public string? DefaultValue { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaType? ElementSchemaType { get { throw null; } }
        [System.ObsoleteAttribute("XmlSchemaElement.ElementType has been deprecated. Use the ElementSchemaType property that returns a strongly typed element type instead.")]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public object? ElementType { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaDerivationMethod.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("final")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Final { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod FinalResolved { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("fixed")]
        public string? FixedValue { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaForm.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("form")]
        public System.Xml.Schema.XmlSchemaForm Form { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("abstract")]
        public bool IsAbstract { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("nillable")]
        public bool IsNillable { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Xml.XmlQualifiedName RefName { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("complexType", typeof(System.Xml.Schema.XmlSchemaComplexType))]
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaType? SchemaType { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Xml.XmlQualifiedName SchemaTypeName { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("substitutionGroup")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Xml.XmlQualifiedName SubstitutionGroup { get { throw null; } set { } }
    }
    public partial class XmlSchemaEnumerationFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaEnumerationFacet() { }
    }
    public partial class XmlSchemaException : System.SystemException
    {
        public XmlSchemaException() { }
        protected XmlSchemaException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlSchemaException(string? message) { }
        public XmlSchemaException(string? message, System.Exception? innerException) { }
        public XmlSchemaException(string? message, System.Exception? innerException, int lineNumber, int linePosition) { }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public override string Message { get { throw null; } }
        public System.Xml.Schema.XmlSchemaObject? SourceSchemaObject { get { throw null; } }
        public string? SourceUri { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class XmlSchemaExternal : System.Xml.Schema.XmlSchemaObject
    {
        protected XmlSchemaExternal() { }
        [System.Xml.Serialization.XmlAttributeAttribute("id", DataType="ID")]
        public string? Id { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchema? Schema { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("schemaLocation", DataType="anyURI")]
        public string? SchemaLocation { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[]? UnhandledAttributes { get { throw null; } set { } }
    }
    public abstract partial class XmlSchemaFacet : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaFacet() { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("fixed")]
        public virtual bool IsFixed { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("value")]
        public string? Value { get { throw null; } set { } }
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
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("all", typeof(System.Xml.Schema.XmlSchemaAll))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public System.Xml.Schema.XmlSchemaGroupBase? Particle { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { throw null; } }
    }
    public abstract partial class XmlSchemaGroupBase : System.Xml.Schema.XmlSchemaParticle
    {
        internal XmlSchemaGroupBase() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public abstract System.Xml.Schema.XmlSchemaObjectCollection Items { get; }
    }
    public partial class XmlSchemaGroupRef : System.Xml.Schema.XmlSchemaParticle
    {
        public XmlSchemaGroupRef() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaGroupBase? Particle { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public System.Xml.XmlQualifiedName RefName { get { throw null; } set { } }
    }
    public partial class XmlSchemaIdentityConstraint : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaIdentityConstraint() { }
        [System.Xml.Serialization.XmlElementAttribute("field", typeof(System.Xml.Schema.XmlSchemaXPath))]
        public System.Xml.Schema.XmlSchemaObjectCollection Fields { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("selector", typeof(System.Xml.Schema.XmlSchemaXPath))]
        public System.Xml.Schema.XmlSchemaXPath? Selector { get { throw null; } set { } }
    }
    public partial class XmlSchemaImport : System.Xml.Schema.XmlSchemaExternal
    {
        public XmlSchemaImport() { }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        public System.Xml.Schema.XmlSchemaAnnotation? Annotation { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace", DataType="anyURI")]
        public string? Namespace { get { throw null; } set { } }
    }
    public partial class XmlSchemaInclude : System.Xml.Schema.XmlSchemaExternal
    {
        public XmlSchemaInclude() { }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        public System.Xml.Schema.XmlSchemaAnnotation? Annotation { get { throw null; } set { } }
    }
    public sealed partial class XmlSchemaInference
    {
        public XmlSchemaInference() { }
        public System.Xml.Schema.XmlSchemaInference.InferenceOption Occurrence { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaInference.InferenceOption TypeInference { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaSet InferSchema(System.Xml.XmlReader instanceDocument) { throw null; }
        public System.Xml.Schema.XmlSchemaSet InferSchema(System.Xml.XmlReader instanceDocument, System.Xml.Schema.XmlSchemaSet schemas) { throw null; }
        public enum InferenceOption
        {
            Restricted = 0,
            Relaxed = 1,
        }
    }
    public partial class XmlSchemaInferenceException : System.Xml.Schema.XmlSchemaException
    {
        public XmlSchemaInferenceException() { }
        protected XmlSchemaInferenceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XmlSchemaInferenceException(string message) { }
        public XmlSchemaInferenceException(string message, System.Exception? innerException) { }
        public XmlSchemaInferenceException(string message, System.Exception? innerException, int lineNumber, int linePosition) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class XmlSchemaInfo : System.Xml.Schema.IXmlSchemaInfo
    {
        public XmlSchemaInfo() { }
        public System.Xml.Schema.XmlSchemaContentType ContentType { get { throw null; } set { } }
        public bool IsDefault { get { throw null; } set { } }
        public bool IsNil { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaSimpleType? MemberType { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaAttribute? SchemaAttribute { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaElement? SchemaElement { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaType? SchemaType { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaValidity Validity { get { throw null; } set { } }
    }
    public partial class XmlSchemaKey : System.Xml.Schema.XmlSchemaIdentityConstraint
    {
        public XmlSchemaKey() { }
    }
    public partial class XmlSchemaKeyref : System.Xml.Schema.XmlSchemaIdentityConstraint
    {
        public XmlSchemaKeyref() { }
        [System.Xml.Serialization.XmlAttributeAttribute("refer")]
        public System.Xml.XmlQualifiedName Refer { get { throw null; } set { } }
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
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("public")]
        public string? Public { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("system")]
        public string? System { get { throw null; } set { } }
    }
    public abstract partial class XmlSchemaNumericFacet : System.Xml.Schema.XmlSchemaFacet
    {
        protected XmlSchemaNumericFacet() { }
    }
    public abstract partial class XmlSchemaObject
    {
        protected XmlSchemaObject() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int LineNumber { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int LinePosition { get { throw null; } set { } }
        [System.Xml.Serialization.XmlNamespaceDeclarationsAttribute]
        public System.Xml.Serialization.XmlSerializerNamespaces Namespaces { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObject? Parent { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string? SourceUri { get { throw null; } set { } }
    }
    public partial class XmlSchemaObjectCollection : System.Collections.CollectionBase
    {
        public XmlSchemaObjectCollection() { }
        public XmlSchemaObjectCollection(System.Xml.Schema.XmlSchemaObject? parent) { }
        public virtual System.Xml.Schema.XmlSchemaObject this[int index] { get { throw null; } set { } }
        public int Add(System.Xml.Schema.XmlSchemaObject item) { throw null; }
        public bool Contains(System.Xml.Schema.XmlSchemaObject item) { throw null; }
        public void CopyTo(System.Xml.Schema.XmlSchemaObject[] array, int index) { }
        public new System.Xml.Schema.XmlSchemaObjectEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.Xml.Schema.XmlSchemaObject item) { throw null; }
        public void Insert(int index, System.Xml.Schema.XmlSchemaObject item) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object? item) { }
        protected override void OnRemove(int index, object? item) { }
        protected override void OnSet(int index, object? oldValue, object? newValue) { }
        public void Remove(System.Xml.Schema.XmlSchemaObject item) { }
    }
    public partial class XmlSchemaObjectEnumerator : System.Collections.IEnumerator
    {
        internal XmlSchemaObjectEnumerator() { }
        public System.Xml.Schema.XmlSchemaObject Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        public void Reset() { }
        bool System.Collections.IEnumerator.MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
    }
    public partial class XmlSchemaObjectTable
    {
        internal XmlSchemaObjectTable() { }
        public int Count { get { throw null; } }
        public System.Xml.Schema.XmlSchemaObject? this[System.Xml.XmlQualifiedName name] { get { throw null; } }
        public System.Collections.ICollection Names { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public bool Contains(System.Xml.XmlQualifiedName name) { throw null; }
        public System.Collections.IDictionaryEnumerator GetEnumerator() { throw null; }
    }
    public abstract partial class XmlSchemaParticle : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaParticle() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public decimal MaxOccurs { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("maxOccurs")]
        public string? MaxOccursString { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public decimal MinOccurs { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("minOccurs")]
        public string? MinOccursString { get { throw null; } set { } }
    }
    public partial class XmlSchemaPatternFacet : System.Xml.Schema.XmlSchemaFacet
    {
        public XmlSchemaPatternFacet() { }
    }
    public partial class XmlSchemaRedefine : System.Xml.Schema.XmlSchemaExternal
    {
        public XmlSchemaRedefine() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable AttributeGroups { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable Groups { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("annotation", typeof(System.Xml.Schema.XmlSchemaAnnotation))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroup))]
        [System.Xml.Serialization.XmlElementAttribute("complexType", typeof(System.Xml.Schema.XmlSchemaComplexType))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroup))]
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaObjectCollection Items { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaObjectTable SchemaTypes { get { throw null; } }
    }
    public partial class XmlSchemaSequence : System.Xml.Schema.XmlSchemaGroupBase
    {
        public XmlSchemaSequence() { }
        [System.Xml.Serialization.XmlElementAttribute("any", typeof(System.Xml.Schema.XmlSchemaAny))]
        [System.Xml.Serialization.XmlElementAttribute("choice", typeof(System.Xml.Schema.XmlSchemaChoice))]
        [System.Xml.Serialization.XmlElementAttribute("element", typeof(System.Xml.Schema.XmlSchemaElement))]
        [System.Xml.Serialization.XmlElementAttribute("group", typeof(System.Xml.Schema.XmlSchemaGroupRef))]
        [System.Xml.Serialization.XmlElementAttribute("sequence", typeof(System.Xml.Schema.XmlSchemaSequence))]
        public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { throw null; } }
    }
    public partial class XmlSchemaSet
    {
        public XmlSchemaSet() { }
        public XmlSchemaSet(System.Xml.XmlNameTable nameTable) { }
        public System.Xml.Schema.XmlSchemaCompilationSettings CompilationSettings { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public System.Xml.Schema.XmlSchemaObjectTable GlobalAttributes { get { throw null; } }
        public System.Xml.Schema.XmlSchemaObjectTable GlobalElements { get { throw null; } }
        public System.Xml.Schema.XmlSchemaObjectTable GlobalTypes { get { throw null; } }
        public bool IsCompiled { get { throw null; } }
        public System.Xml.XmlNameTable NameTable { get { throw null; } }
        public System.Xml.XmlResolver? XmlResolver { set { } }
        public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
        public System.Xml.Schema.XmlSchema? Add(string? targetNamespace, string schemaUri) { throw null; }
        public System.Xml.Schema.XmlSchema? Add(string? targetNamespace, System.Xml.XmlReader schemaDocument) { throw null; }
        public System.Xml.Schema.XmlSchema? Add(System.Xml.Schema.XmlSchema schema) { throw null; }
        public void Add(System.Xml.Schema.XmlSchemaSet schemas) { }
        public void Compile() { }
        public bool Contains(string? targetNamespace) { throw null; }
        public bool Contains(System.Xml.Schema.XmlSchema schema) { throw null; }
        public void CopyTo(System.Xml.Schema.XmlSchema[] schemas, int index) { }
        public System.Xml.Schema.XmlSchema? Remove(System.Xml.Schema.XmlSchema schema) { throw null; }
        public bool RemoveRecursive(System.Xml.Schema.XmlSchema schemaToRemove) { throw null; }
        public System.Xml.Schema.XmlSchema Reprocess(System.Xml.Schema.XmlSchema schema) { throw null; }
        public System.Collections.ICollection Schemas() { throw null; }
        public System.Collections.ICollection Schemas(string? targetNamespace) { throw null; }
    }
    public partial class XmlSchemaSimpleContent : System.Xml.Schema.XmlSchemaContentModel
    {
        public XmlSchemaSimpleContent() { }
        [System.Xml.Serialization.XmlElementAttribute("extension", typeof(System.Xml.Schema.XmlSchemaSimpleContentExtension))]
        [System.Xml.Serialization.XmlElementAttribute("restriction", typeof(System.Xml.Schema.XmlSchemaSimpleContentRestriction))]
        public override System.Xml.Schema.XmlSchemaContent? Content { get { throw null; } set { } }
    }
    public partial class XmlSchemaSimpleContentExtension : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaSimpleContentExtension() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AnyAttribute { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { throw null; } set { } }
    }
    public partial class XmlSchemaSimpleContentRestriction : System.Xml.Schema.XmlSchemaContent
    {
        public XmlSchemaSimpleContentRestriction() { }
        [System.Xml.Serialization.XmlElementAttribute("anyAttribute")]
        public System.Xml.Schema.XmlSchemaAnyAttribute? AnyAttribute { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("attribute", typeof(System.Xml.Schema.XmlSchemaAttribute))]
        [System.Xml.Serialization.XmlElementAttribute("attributeGroup", typeof(System.Xml.Schema.XmlSchemaAttributeGroupRef))]
        public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaSimpleType? BaseType { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { throw null; } set { } }
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
        public System.Xml.Schema.XmlSchemaObjectCollection Facets { get { throw null; } }
    }
    public partial class XmlSchemaSimpleType : System.Xml.Schema.XmlSchemaType
    {
        public XmlSchemaSimpleType() { }
        [System.Xml.Serialization.XmlElementAttribute("list", typeof(System.Xml.Schema.XmlSchemaSimpleTypeList))]
        [System.Xml.Serialization.XmlElementAttribute("restriction", typeof(System.Xml.Schema.XmlSchemaSimpleTypeRestriction))]
        [System.Xml.Serialization.XmlElementAttribute("union", typeof(System.Xml.Schema.XmlSchemaSimpleTypeUnion))]
        public System.Xml.Schema.XmlSchemaSimpleTypeContent? Content { get { throw null; } set { } }
    }
    public abstract partial class XmlSchemaSimpleTypeContent : System.Xml.Schema.XmlSchemaAnnotated
    {
        protected XmlSchemaSimpleTypeContent() { }
    }
    public partial class XmlSchemaSimpleTypeList : System.Xml.Schema.XmlSchemaSimpleTypeContent
    {
        public XmlSchemaSimpleTypeList() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaSimpleType? BaseItemType { get { throw null; } set { } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaSimpleType? ItemType { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("itemType")]
        public System.Xml.XmlQualifiedName ItemTypeName { get { throw null; } set { } }
    }
    public partial class XmlSchemaSimpleTypeRestriction : System.Xml.Schema.XmlSchemaSimpleTypeContent
    {
        public XmlSchemaSimpleTypeRestriction() { }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaSimpleType? BaseType { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("base")]
        public System.Xml.XmlQualifiedName BaseTypeName { get { throw null; } set { } }
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
        public System.Xml.Schema.XmlSchemaObjectCollection Facets { get { throw null; } }
    }
    public partial class XmlSchemaSimpleTypeUnion : System.Xml.Schema.XmlSchemaSimpleTypeContent
    {
        public XmlSchemaSimpleTypeUnion() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaSimpleType[]? BaseMemberTypes { get { throw null; } }
        [System.Xml.Serialization.XmlElementAttribute("simpleType", typeof(System.Xml.Schema.XmlSchemaSimpleType))]
        public System.Xml.Schema.XmlSchemaObjectCollection BaseTypes { get { throw null; } }
        [System.Xml.Serialization.XmlAttributeAttribute("memberTypes")]
        public System.Xml.XmlQualifiedName[]? MemberTypes { get { throw null; } set { } }
    }
    public partial class XmlSchemaTotalDigitsFacet : System.Xml.Schema.XmlSchemaNumericFacet
    {
        public XmlSchemaTotalDigitsFacet() { }
    }
    public partial class XmlSchemaType : System.Xml.Schema.XmlSchemaAnnotated
    {
        public XmlSchemaType() { }
        [System.ObsoleteAttribute("XmlSchemaType.BaseSchemaType has been deprecated. Use the BaseXmlSchemaType property that returns a strongly typed base schema type instead.")]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public object? BaseSchemaType { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaType? BaseXmlSchemaType { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDatatype? Datatype { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod DerivedBy { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Xml.Schema.XmlSchemaDerivationMethod.None)]
        [System.Xml.Serialization.XmlAttributeAttribute("final")]
        public System.Xml.Schema.XmlSchemaDerivationMethod Final { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchemaDerivationMethod FinalResolved { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public virtual bool IsMixed { get { throw null; } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string? Name { get { throw null; } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlQualifiedName QualifiedName { get { throw null; } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlTypeCode TypeCode { get { throw null; } }
        public static System.Xml.Schema.XmlSchemaComplexType? GetBuiltInComplexType(System.Xml.Schema.XmlTypeCode typeCode) { throw null; }
        public static System.Xml.Schema.XmlSchemaComplexType? GetBuiltInComplexType(System.Xml.XmlQualifiedName qualifiedName) { throw null; }
        public static System.Xml.Schema.XmlSchemaSimpleType GetBuiltInSimpleType(System.Xml.Schema.XmlTypeCode typeCode) { throw null; }
        public static System.Xml.Schema.XmlSchemaSimpleType? GetBuiltInSimpleType(System.Xml.XmlQualifiedName qualifiedName) { throw null; }
        public static bool IsDerivedFrom([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Xml.Schema.XmlSchemaType? derivedType, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Xml.Schema.XmlSchemaType? baseType, System.Xml.Schema.XmlSchemaDerivationMethod except) { throw null; }
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
        public XmlSchemaValidationException(string? message) { }
        public XmlSchemaValidationException(string? message, System.Exception? innerException) { }
        public XmlSchemaValidationException(string? message, System.Exception? innerException, int lineNumber, int linePosition) { }
        public object? SourceObject { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected internal void SetSourceObject(object? sourceObject) { }
    }
    [System.FlagsAttribute]
    public enum XmlSchemaValidationFlags
    {
        None = 0,
        ProcessInlineSchema = 1,
        ProcessSchemaLocation = 2,
        ReportValidationWarnings = 4,
        ProcessIdentityConstraints = 8,
        AllowXmlAttributes = 16,
    }
    public sealed partial class XmlSchemaValidator
    {
        public XmlSchemaValidator(System.Xml.XmlNameTable nameTable, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.IXmlNamespaceResolver namespaceResolver, System.Xml.Schema.XmlSchemaValidationFlags validationFlags) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Xml.IXmlLineInfo LineInfoProvider { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public System.Uri? SourceUri { get { throw null; } set { } }
        public object ValidationEventSender { get { throw null; } set { } }
        public System.Xml.XmlResolver? XmlResolver { set { } }
        public event System.Xml.Schema.ValidationEventHandler? ValidationEventHandler { add { } remove { } }
        public void AddSchema(System.Xml.Schema.XmlSchema schema) { }
        public void EndValidation() { }
        public System.Xml.Schema.XmlSchemaAttribute[] GetExpectedAttributes() { throw null; }
        public System.Xml.Schema.XmlSchemaParticle[] GetExpectedParticles() { throw null; }
        public void GetUnspecifiedDefaultAttributes(System.Collections.ArrayList defaultAttributes) { }
        public void Initialize() { }
        public void Initialize(System.Xml.Schema.XmlSchemaObject partialValidationType) { }
        public void SkipToEndElement(System.Xml.Schema.XmlSchemaInfo? schemaInfo) { }
        public object? ValidateAttribute(string localName, string namespaceUri, string attributeValue, System.Xml.Schema.XmlSchemaInfo? schemaInfo) { throw null; }
        public object? ValidateAttribute(string localName, string namespaceUri, System.Xml.Schema.XmlValueGetter attributeValue, System.Xml.Schema.XmlSchemaInfo? schemaInfo) { throw null; }
        public void ValidateElement(string localName, string namespaceUri, System.Xml.Schema.XmlSchemaInfo? schemaInfo) { }
        public void ValidateElement(string localName, string namespaceUri, System.Xml.Schema.XmlSchemaInfo? schemaInfo, string? xsiType, string? xsiNil, string? xsiSchemaLocation, string? xsiNoNamespaceSchemaLocation) { }
        public object? ValidateEndElement(System.Xml.Schema.XmlSchemaInfo? schemaInfo) { throw null; }
        public object? ValidateEndElement(System.Xml.Schema.XmlSchemaInfo? schemaInfo, object typedValue) { throw null; }
        public void ValidateEndOfAttributes(System.Xml.Schema.XmlSchemaInfo? schemaInfo) { }
        public void ValidateText(string elementValue) { }
        public void ValidateText(System.Xml.Schema.XmlValueGetter elementValue) { }
        public void ValidateWhitespace(string elementValue) { }
        public void ValidateWhitespace(System.Xml.Schema.XmlValueGetter elementValue) { }
    }
    public enum XmlSchemaValidity
    {
        NotKnown = 0,
        Valid = 1,
        Invalid = 2,
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
        public string? XPath { get { throw null; } set { } }
    }
    public enum XmlSeverityType
    {
        Error = 0,
        Warning = 1,
    }
    public enum XmlTypeCode
    {
        None = 0,
        Item = 1,
        Node = 2,
        Document = 3,
        Element = 4,
        Attribute = 5,
        Namespace = 6,
        ProcessingInstruction = 7,
        Comment = 8,
        Text = 9,
        AnyAtomicType = 10,
        UntypedAtomic = 11,
        String = 12,
        Boolean = 13,
        Decimal = 14,
        Float = 15,
        Double = 16,
        Duration = 17,
        DateTime = 18,
        Time = 19,
        Date = 20,
        GYearMonth = 21,
        GYear = 22,
        GMonthDay = 23,
        GDay = 24,
        GMonth = 25,
        HexBinary = 26,
        Base64Binary = 27,
        AnyUri = 28,
        QName = 29,
        Notation = 30,
        NormalizedString = 31,
        Token = 32,
        Language = 33,
        NmToken = 34,
        Name = 35,
        NCName = 36,
        Id = 37,
        Idref = 38,
        Entity = 39,
        Integer = 40,
        NonPositiveInteger = 41,
        NegativeInteger = 42,
        Long = 43,
        Int = 44,
        Short = 45,
        Byte = 46,
        NonNegativeInteger = 47,
        UnsignedLong = 48,
        UnsignedInt = 49,
        UnsignedShort = 50,
        UnsignedByte = 51,
        PositiveInteger = 52,
        YearMonthDuration = 53,
        DayTimeDuration = 54,
    }
    public delegate object? XmlValueGetter();
}
namespace System.Xml.Serialization
{
    public partial interface IXmlSerializable
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        System.Xml.Schema.XmlSchema? GetSchema();
        void ReadXml(System.Xml.XmlReader reader);
        void WriteXml(System.Xml.XmlWriter writer);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple = false)]
    public partial class XmlAnyAttributeAttribute : System.Attribute
    {
        public XmlAnyAttributeAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple = true)]
    public partial class XmlAnyElementAttribute : System.Attribute
    {
        public XmlAnyElementAttribute() { }
        public XmlAnyElementAttribute(string? name) { }
        public XmlAnyElementAttribute(string? name, string? ns) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Name { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        public int Order { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue)]
    public partial class XmlAttributeAttribute : System.Attribute
    {
        public XmlAttributeAttribute() { }
        public XmlAttributeAttribute(string? attributeName) { }
        public XmlAttributeAttribute(string? attributeName, System.Type? type) { }
        public XmlAttributeAttribute(System.Type? type) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string AttributeName { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        public System.Type? Type { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple = true)]
    public partial class XmlElementAttribute : System.Attribute
    {
        public XmlElementAttribute() { }
        public XmlElementAttribute(string? elementName) { }
        public XmlElementAttribute(string? elementName, System.Type? type) { }
        public XmlElementAttribute(System.Type? type) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string ElementName { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { throw null; } set { } }
        public bool IsNullable { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        public int Order { get { throw null; } set { } }
        public System.Type? Type { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field)]
    public partial class XmlEnumAttribute : System.Attribute
    {
        public XmlEnumAttribute() { }
        public XmlEnumAttribute(string? name) { }
        public string? Name { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue)]
    public partial class XmlIgnoreAttribute : System.Attribute
    {
        public XmlIgnoreAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple = false)]
    public partial class XmlNamespaceDeclarationsAttribute : System.Attribute
    {
        public XmlNamespaceDeclarationsAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.ReturnValue | System.AttributeTargets.Struct)]
    public partial class XmlRootAttribute : System.Attribute
    {
        public XmlRootAttribute() { }
        public XmlRootAttribute(string elementName) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        public string ElementName { get { throw null; } set { } }
        public bool IsNullable { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Struct)]
    public sealed partial class XmlSchemaProviderAttribute : System.Attribute
    {
        public XmlSchemaProviderAttribute(string? methodName) { }
        public bool IsAny { get { throw null; } set { } }
        public string? MethodName { get { throw null; } }
    }
    public partial class XmlSerializerNamespaces
    {
        public XmlSerializerNamespaces() { }
        public XmlSerializerNamespaces(System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public XmlSerializerNamespaces(System.Xml.XmlQualifiedName[] namespaces) { }
        public int Count { get { throw null; } }
        public void Add(string prefix, string? ns) { }
        public System.Xml.XmlQualifiedName[] ToArray() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue)]
    public partial class XmlTextAttribute : System.Attribute
    {
        public XmlTextAttribute() { }
        public XmlTextAttribute(System.Type? type) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        public System.Type? Type { get { throw null; } set { } }
    }
}
namespace System.Xml.XPath
{
    public partial interface IXPathNavigable
    {
        System.Xml.XPath.XPathNavigator? CreateNavigator();
    }
    public enum XmlCaseOrder
    {
        None = 0,
        UpperFirst = 1,
        LowerFirst = 2,
    }
    public enum XmlDataType
    {
        Text = 1,
        Number = 2,
    }
    public enum XmlSortOrder
    {
        Ascending = 1,
        Descending = 2,
    }
    public abstract partial class XPathExpression
    {
        internal XPathExpression() { }
        public abstract string Expression { get; }
        public abstract System.Xml.XPath.XPathResultType ReturnType { get; }
        public abstract void AddSort(object expr, System.Collections.IComparer comparer);
        public abstract void AddSort(object expr, System.Xml.XPath.XmlSortOrder order, System.Xml.XPath.XmlCaseOrder caseOrder, string lang, System.Xml.XPath.XmlDataType dataType);
        public abstract System.Xml.XPath.XPathExpression Clone();
        public static System.Xml.XPath.XPathExpression Compile(string xpath) { throw null; }
        public static System.Xml.XPath.XPathExpression Compile(string xpath, System.Xml.IXmlNamespaceResolver? nsResolver) { throw null; }
        public abstract void SetContext(System.Xml.IXmlNamespaceResolver? nsResolver);
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
        public abstract System.Xml.Schema.XmlSchemaType? XmlType { get; }
        public virtual object ValueAs(System.Type returnType) { throw null; }
        public abstract object ValueAs(System.Type returnType, System.Xml.IXmlNamespaceResolver? nsResolver);
    }
    public enum XPathNamespaceScope
    {
        All = 0,
        ExcludeXml = 1,
        Local = 2,
    }
    public abstract partial class XPathNavigator : System.Xml.XPath.XPathItem, System.ICloneable, System.Xml.IXmlNamespaceResolver, System.Xml.XPath.IXPathNavigable
    {
        protected XPathNavigator() { }
        public abstract string BaseURI { get; }
        public virtual bool CanEdit { get { throw null; } }
        public virtual bool HasAttributes { get { throw null; } }
        public virtual bool HasChildren { get { throw null; } }
        public virtual string InnerXml { get { throw null; } set { } }
        public abstract bool IsEmptyElement { get; }
        public sealed override bool IsNode { get { throw null; } }
        public abstract string LocalName { get; }
        public abstract string Name { get; }
        public abstract string NamespaceURI { get; }
        public abstract System.Xml.XmlNameTable NameTable { get; }
        public static System.Collections.IEqualityComparer NavigatorComparer { get { throw null; } }
        public abstract System.Xml.XPath.XPathNodeType NodeType { get; }
        public virtual string OuterXml { get { throw null; } set { } }
        public abstract string Prefix { get; }
        public virtual System.Xml.Schema.IXmlSchemaInfo? SchemaInfo { get { throw null; } }
        public override object TypedValue { get { throw null; } }
        public virtual object? UnderlyingObject { get { throw null; } }
        public override bool ValueAsBoolean { get { throw null; } }
        public override System.DateTime ValueAsDateTime { get { throw null; } }
        public override double ValueAsDouble { get { throw null; } }
        public override int ValueAsInt { get { throw null; } }
        public override long ValueAsLong { get { throw null; } }
        public override System.Type ValueType { get { throw null; } }
        public virtual string XmlLang { get { throw null; } }
        public override System.Xml.Schema.XmlSchemaType? XmlType { get { throw null; } }
        public virtual System.Xml.XmlWriter AppendChild() { throw null; }
        public virtual void AppendChild(string newChild) { }
        public virtual void AppendChild(System.Xml.XmlReader newChild) { }
        public virtual void AppendChild(System.Xml.XPath.XPathNavigator newChild) { }
        public virtual void AppendChildElement(string prefix, string localName, string namespaceURI, string value) { }
        public virtual bool CheckValidity(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler) { throw null; }
        public abstract System.Xml.XPath.XPathNavigator Clone();
        public virtual System.Xml.XmlNodeOrder ComparePosition(System.Xml.XPath.XPathNavigator? nav) { throw null; }
        public virtual System.Xml.XPath.XPathExpression Compile(string xpath) { throw null; }
        public virtual void CreateAttribute(string prefix, string localName, string namespaceURI, string value) { }
        public virtual System.Xml.XmlWriter CreateAttributes() { throw null; }
        public virtual System.Xml.XPath.XPathNavigator CreateNavigator() { throw null; }
        public virtual void DeleteRange(System.Xml.XPath.XPathNavigator lastSiblingToDelete) { }
        public virtual void DeleteSelf() { }
        public virtual object Evaluate(string xpath) { throw null; }
        public virtual object Evaluate(string xpath, System.Xml.IXmlNamespaceResolver? resolver) { throw null; }
        public virtual object Evaluate(System.Xml.XPath.XPathExpression expr) { throw null; }
        public virtual object Evaluate(System.Xml.XPath.XPathExpression expr, System.Xml.XPath.XPathNodeIterator? context) { throw null; }
        public virtual string GetAttribute(string localName, string namespaceURI) { throw null; }
        public virtual string GetNamespace(string name) { throw null; }
        public virtual System.Collections.Generic.IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope) { throw null; }
        public virtual System.Xml.XmlWriter InsertAfter() { throw null; }
        public virtual void InsertAfter(string newSibling) { }
        public virtual void InsertAfter(System.Xml.XmlReader newSibling) { }
        public virtual void InsertAfter(System.Xml.XPath.XPathNavigator newSibling) { }
        public virtual System.Xml.XmlWriter InsertBefore() { throw null; }
        public virtual void InsertBefore(string newSibling) { }
        public virtual void InsertBefore(System.Xml.XmlReader newSibling) { }
        public virtual void InsertBefore(System.Xml.XPath.XPathNavigator newSibling) { }
        public virtual void InsertElementAfter(string prefix, string localName, string namespaceURI, string value) { }
        public virtual void InsertElementBefore(string prefix, string localName, string namespaceURI, string value) { }
        public virtual bool IsDescendant([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] System.Xml.XPath.XPathNavigator? nav) { throw null; }
        public abstract bool IsSamePosition(System.Xml.XPath.XPathNavigator other);
        public virtual string? LookupNamespace(string prefix) { throw null; }
        public virtual string? LookupPrefix(string namespaceURI) { throw null; }
        public virtual bool Matches(string xpath) { throw null; }
        public virtual bool Matches(System.Xml.XPath.XPathExpression expr) { throw null; }
        public abstract bool MoveTo(System.Xml.XPath.XPathNavigator other);
        public virtual bool MoveToAttribute(string localName, string namespaceURI) { throw null; }
        public virtual bool MoveToChild(string localName, string namespaceURI) { throw null; }
        public virtual bool MoveToChild(System.Xml.XPath.XPathNodeType type) { throw null; }
        public virtual bool MoveToFirst() { throw null; }
        public abstract bool MoveToFirstAttribute();
        public abstract bool MoveToFirstChild();
        public bool MoveToFirstNamespace() { throw null; }
        public abstract bool MoveToFirstNamespace(System.Xml.XPath.XPathNamespaceScope namespaceScope);
        public virtual bool MoveToFollowing(string localName, string namespaceURI) { throw null; }
        public virtual bool MoveToFollowing(string localName, string namespaceURI, System.Xml.XPath.XPathNavigator? end) { throw null; }
        public virtual bool MoveToFollowing(System.Xml.XPath.XPathNodeType type) { throw null; }
        public virtual bool MoveToFollowing(System.Xml.XPath.XPathNodeType type, System.Xml.XPath.XPathNavigator? end) { throw null; }
        public abstract bool MoveToId(string id);
        public virtual bool MoveToNamespace(string name) { throw null; }
        public abstract bool MoveToNext();
        public virtual bool MoveToNext(string localName, string namespaceURI) { throw null; }
        public virtual bool MoveToNext(System.Xml.XPath.XPathNodeType type) { throw null; }
        public abstract bool MoveToNextAttribute();
        public bool MoveToNextNamespace() { throw null; }
        public abstract bool MoveToNextNamespace(System.Xml.XPath.XPathNamespaceScope namespaceScope);
        public abstract bool MoveToParent();
        public abstract bool MoveToPrevious();
        public virtual void MoveToRoot() { }
        public virtual System.Xml.XmlWriter PrependChild() { throw null; }
        public virtual void PrependChild(string newChild) { }
        public virtual void PrependChild(System.Xml.XmlReader newChild) { }
        public virtual void PrependChild(System.Xml.XPath.XPathNavigator newChild) { }
        public virtual void PrependChildElement(string prefix, string localName, string namespaceURI, string value) { }
        public virtual System.Xml.XmlReader ReadSubtree() { throw null; }
        public virtual System.Xml.XmlWriter ReplaceRange(System.Xml.XPath.XPathNavigator lastSiblingToReplace) { throw null; }
        public virtual void ReplaceSelf(string newNode) { }
        public virtual void ReplaceSelf(System.Xml.XmlReader newNode) { }
        public virtual void ReplaceSelf(System.Xml.XPath.XPathNavigator newNode) { }
        public virtual System.Xml.XPath.XPathNodeIterator Select(string xpath) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator Select(string xpath, System.Xml.IXmlNamespaceResolver? resolver) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator Select(System.Xml.XPath.XPathExpression expr) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator SelectAncestors(string name, string namespaceURI, bool matchSelf) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator SelectAncestors(System.Xml.XPath.XPathNodeType type, bool matchSelf) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator SelectChildren(string name, string namespaceURI) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator SelectChildren(System.Xml.XPath.XPathNodeType type) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator SelectDescendants(string name, string namespaceURI, bool matchSelf) { throw null; }
        public virtual System.Xml.XPath.XPathNodeIterator SelectDescendants(System.Xml.XPath.XPathNodeType type, bool matchSelf) { throw null; }
        public virtual System.Xml.XPath.XPathNavigator? SelectSingleNode(string xpath) { throw null; }
        public virtual System.Xml.XPath.XPathNavigator? SelectSingleNode(string xpath, System.Xml.IXmlNamespaceResolver? resolver) { throw null; }
        public virtual System.Xml.XPath.XPathNavigator? SelectSingleNode(System.Xml.XPath.XPathExpression expression) { throw null; }
        public virtual void SetTypedValue(object typedValue) { }
        public virtual void SetValue(string value) { }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
        public override object ValueAs(System.Type returnType, System.Xml.IXmlNamespaceResolver? nsResolver) { throw null; }
        public virtual void WriteSubtree(System.Xml.XmlWriter writer) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Position={CurrentPosition}, Current={debuggerDisplayProxy}")]
    public abstract partial class XPathNodeIterator : System.Collections.IEnumerable, System.ICloneable
    {
        protected XPathNodeIterator() { }
        public virtual int Count { get { throw null; } }
        public abstract System.Xml.XPath.XPathNavigator? Current { get; }
        public abstract int CurrentPosition { get; }
        public abstract System.Xml.XPath.XPathNodeIterator Clone();
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public abstract bool MoveNext();
        object System.ICloneable.Clone() { throw null; }
    }
    public enum XPathNodeType
    {
        Root = 0,
        Element = 1,
        Attribute = 2,
        Namespace = 3,
        Text = 4,
        SignificantWhitespace = 5,
        Whitespace = 6,
        ProcessingInstruction = 7,
        Comment = 8,
        All = 9,
    }
    public enum XPathResultType
    {
        Number = 0,
        Navigator = 1,
        String = 1,
        Boolean = 2,
        NodeSet = 3,
        Any = 5,
        Error = 6,
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
        public System.Xml.XmlWriterSettings? OutputSettings { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("This method will call into constructors of the earlyBoundTypes array which cannot be statically analyzed.")]
        public void Load(System.Reflection.MethodInfo executeMethod, byte[] queryData, System.Type[]? earlyBoundTypes) { }
        public void Load(string stylesheetUri) { }
        public void Load(string stylesheetUri, System.Xml.Xsl.XsltSettings? settings, System.Xml.XmlResolver? stylesheetResolver) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("This method will get fields and types from the assembly of the passed in compiledStylesheet and call their constructors which cannot be statically analyzed")]
        public void Load(System.Type compiledStylesheet) { }
        public void Load(System.Xml.XmlReader stylesheet) { }
        public void Load(System.Xml.XmlReader stylesheet, System.Xml.Xsl.XsltSettings? settings, System.Xml.XmlResolver? stylesheetResolver) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet, System.Xml.Xsl.XsltSettings? settings, System.Xml.XmlResolver? stylesheetResolver) { }
        public void Transform(string inputUri, string resultsFile) { }
        public void Transform(string inputUri, System.Xml.XmlWriter results) { }
        public void Transform(string inputUri, System.Xml.Xsl.XsltArgumentList? arguments, System.IO.Stream results) { }
        public void Transform(string inputUri, System.Xml.Xsl.XsltArgumentList? arguments, System.IO.TextWriter results) { }
        public void Transform(string inputUri, System.Xml.Xsl.XsltArgumentList? arguments, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList? arguments, System.IO.Stream results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList? arguments, System.IO.TextWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList? arguments, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XmlReader input, System.Xml.Xsl.XsltArgumentList? arguments, System.Xml.XmlWriter results, System.Xml.XmlResolver? documentResolver) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? arguments, System.IO.Stream results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? arguments, System.IO.TextWriter results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? arguments, System.Xml.XmlWriter results) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? arguments, System.Xml.XmlWriter results, System.Xml.XmlResolver? documentResolver) { }
    }
    public partial class XsltArgumentList
    {
        public XsltArgumentList() { }
        public event System.Xml.Xsl.XsltMessageEncounteredEventHandler XsltMessageEncountered { add { } remove { } }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The stylesheet may have calls to methods of the extension object passed in which cannot be statically analyzed by the trimmer. Ensure all methods that may be called are preserved.")]
        public void AddExtensionObject(string namespaceUri, object extension) { }
        public void AddParam(string name, string namespaceUri, object parameter) { }
        public void Clear() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The stylesheet may have calls to methods of the extension object passed in which cannot be statically analyzed by the trimmer. Ensure all methods that may be called are preserved.")]
        public object? GetExtensionObject(string namespaceUri) { throw null; }
        public object? GetParam(string name, string namespaceUri) { throw null; }
        public object? RemoveExtensionObject(string namespaceUri) { throw null; }
        public object? RemoveParam(string name, string namespaceUri) { throw null; }
    }
    public partial class XsltCompileException : System.Xml.Xsl.XsltException
    {
        public XsltCompileException() { }
        public XsltCompileException(System.Exception inner, string sourceUri, int lineNumber, int linePosition) { }
        protected XsltCompileException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public XsltCompileException(string message) { }
        public XsltCompileException(string message, System.Exception innerException) { }
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
        public XsltException(string message, System.Exception? innerException) { }
        public virtual int LineNumber { get { throw null; } }
        public virtual int LinePosition { get { throw null; } }
        public override string Message { get { throw null; } }
        public virtual string? SourceUri { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public abstract partial class XsltMessageEncounteredEventArgs : System.EventArgs
    {
        protected XsltMessageEncounteredEventArgs() { }
        public abstract string Message { get; }
    }
    public delegate void XsltMessageEncounteredEventHandler(object sender, System.Xml.Xsl.XsltMessageEncounteredEventArgs e);
    public sealed partial class XslTransform
    {
        public XslTransform() { }
        public System.Xml.XmlResolver? XmlResolver { set { } }
        public void Load(string url) { }
        public void Load(string url, System.Xml.XmlResolver? resolver) { }
        public void Load(System.Xml.XmlReader stylesheet) { }
        public void Load(System.Xml.XmlReader stylesheet, System.Xml.XmlResolver? resolver) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet) { }
        public void Load(System.Xml.XPath.IXPathNavigable stylesheet, System.Xml.XmlResolver? resolver) { }
        public void Load(System.Xml.XPath.XPathNavigator stylesheet) { }
        public void Load(System.Xml.XPath.XPathNavigator stylesheet, System.Xml.XmlResolver? resolver) { }
        public void Transform(string inputfile, string outputfile) { }
        public void Transform(string inputfile, string outputfile, System.Xml.XmlResolver? resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args) { throw null; }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.IO.Stream output) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.IO.Stream output, System.Xml.XmlResolver? resolver) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.IO.TextWriter output) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.IO.TextWriter output, System.Xml.XmlResolver? resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.Xml.XmlResolver? resolver) { throw null; }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.Xml.XmlWriter output) { }
        public void Transform(System.Xml.XPath.IXPathNavigable input, System.Xml.Xsl.XsltArgumentList? args, System.Xml.XmlWriter output, System.Xml.XmlResolver? resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args) { throw null; }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.IO.Stream output) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.IO.Stream output, System.Xml.XmlResolver? resolver) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.IO.TextWriter output) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.IO.TextWriter output, System.Xml.XmlResolver? resolver) { }
        public System.Xml.XmlReader Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.Xml.XmlResolver? resolver) { throw null; }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.Xml.XmlWriter output) { }
        public void Transform(System.Xml.XPath.XPathNavigator input, System.Xml.Xsl.XsltArgumentList? args, System.Xml.XmlWriter output, System.Xml.XmlResolver? resolver) { }
    }
    public sealed partial class XsltSettings
    {
        public XsltSettings() { }
        public XsltSettings(bool enableDocumentFunction, bool enableScript) { }
        public static System.Xml.Xsl.XsltSettings Default { get { throw null; } }
        public bool EnableDocumentFunction { get { throw null; } set { } }
        public bool EnableScript { get { throw null; } set { } }
        public static System.Xml.Xsl.XsltSettings TrustedXslt { get { throw null; } }
    }
}
