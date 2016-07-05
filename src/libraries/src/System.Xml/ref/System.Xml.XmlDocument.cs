// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Xml
{
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
        protected internal XmlCDataSection(string data, System.Xml.XmlDocument doc) : base(default(string), default(System.Xml.XmlDocument)) { }
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
        protected internal XmlComment(string comment, System.Xml.XmlDocument doc) : base(default(string), default(System.Xml.XmlDocument)) { }
        public override string LocalName { get { return default(string); } }
        public override string Name { get { return default(string); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override System.Xml.XmlNode CloneNode(bool deep) { return default(System.Xml.XmlNode); }
        public override void WriteContentTo(System.Xml.XmlWriter w) { }
        public override void WriteTo(System.Xml.XmlWriter w) { }
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
        public virtual System.Xml.XmlDocumentFragment CreateDocumentFragment() { return default(System.Xml.XmlDocumentFragment); }
        public System.Xml.XmlElement CreateElement(string name) { return default(System.Xml.XmlElement); }
        public System.Xml.XmlElement CreateElement(string qualifiedName, string namespaceURI) { return default(System.Xml.XmlElement); }
        public virtual System.Xml.XmlElement CreateElement(string prefix, string localName, string namespaceURI) { return default(System.Xml.XmlElement); }
        public virtual System.Xml.XmlNode CreateNode(string nodeTypeString, string name, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode CreateNode(System.Xml.XmlNodeType type, string name, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlNode CreateNode(System.Xml.XmlNodeType type, string prefix, string name, string namespaceURI) { return default(System.Xml.XmlNode); }
        public virtual System.Xml.XmlProcessingInstruction CreateProcessingInstruction(string target, string data) { return default(System.Xml.XmlProcessingInstruction); }
        public virtual System.Xml.XmlSignificantWhitespace CreateSignificantWhitespace(string text) { return default(System.Xml.XmlSignificantWhitespace); }
        public virtual System.Xml.XmlText CreateTextNode(string text) { return default(System.Xml.XmlText); }
        public virtual System.Xml.XmlWhitespace CreateWhitespace(string text) { return default(System.Xml.XmlWhitespace); }
        public virtual System.Xml.XmlDeclaration CreateXmlDeclaration(string version, string encoding, string standalone) { return default(System.Xml.XmlDeclaration); }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string name) { return default(System.Xml.XmlNodeList); }
        public virtual System.Xml.XmlNodeList GetElementsByTagName(string localName, string namespaceURI) { return default(System.Xml.XmlNodeList); }
        public virtual System.Xml.XmlNode ImportNode(System.Xml.XmlNode node, bool deep) { return default(System.Xml.XmlNode); }
        public virtual void Load(System.IO.Stream inStream) { }
        public virtual void Load(System.IO.TextReader txtReader) { }
        public virtual void Load(System.Xml.XmlReader reader) { }
        public virtual void LoadXml(string xml) { }
        public virtual System.Xml.XmlNode ReadNode(System.Xml.XmlReader reader) { return default(System.Xml.XmlNode); }
        public virtual void Save(System.IO.Stream outStream) { }
        public virtual void Save(System.IO.TextWriter writer) { }
        public virtual void Save(System.Xml.XmlWriter w) { }
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
    public abstract partial class XmlNode : System.Collections.IEnumerable
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
        public virtual string Value { get { return default(string); } set { } }
        public virtual System.Xml.XmlNode AppendChild(System.Xml.XmlNode newChild) { return default(System.Xml.XmlNode); }
        public abstract System.Xml.XmlNode CloneNode(bool deep);
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
        public virtual bool Supports(string feature, string version) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
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
    public partial class XmlSignificantWhitespace : System.Xml.XmlCharacterData
    {
        protected internal XmlSignificantWhitespace(string strData, System.Xml.XmlDocument doc) : base(default(string), default(System.Xml.XmlDocument)) { }
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
    public partial class XmlText : System.Xml.XmlCharacterData
    {
        protected internal XmlText(string strData, System.Xml.XmlDocument doc) : base(default(string), default(System.Xml.XmlDocument)) { }
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
    public partial class XmlWhitespace : System.Xml.XmlCharacterData
    {
        protected internal XmlWhitespace(string strData, System.Xml.XmlDocument doc) : base(default(string), default(System.Xml.XmlDocument)) { }
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
}
