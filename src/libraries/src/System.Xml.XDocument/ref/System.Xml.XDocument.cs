// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Xml.Linq
{
    public static partial class Extensions
    {
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XNode { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors<T>(this System.Collections.Generic.IEnumerable<T> source, System.Xml.Linq.XName name) where T : System.Xml.Linq.XNode { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source, System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source, System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodes<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodesAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants<T>(this System.Collections.Generic.IEnumerable<T> source, System.Xml.Linq.XName name) where T : System.Xml.Linq.XContainer { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source, System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements<T>(this System.Collections.Generic.IEnumerable<T> source, System.Xml.Linq.XName name) where T : System.Xml.Linq.XContainer { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static System.Collections.Generic.IEnumerable<T> InDocumentOrder<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XNode { return default(System.Collections.Generic.IEnumerable<T>); }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> Nodes<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public static void Remove(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> source) { }
        public static void Remove<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XNode { }
    }
    [System.FlagsAttribute]
    public enum LoadOptions
    {
        None = 0,
        PreserveWhitespace = 1,
        SetBaseUri = 2,
        SetLineInfo = 4,
    }
    [System.FlagsAttribute]
    public enum ReaderOptions
    {
        None = 0,
        OmitDuplicateNamespaces = 1,
    }
    [System.FlagsAttribute]
    public enum SaveOptions
    {
        DisableFormatting = 1,
        None = 0,
        OmitDuplicateNamespaces = 2,
    }
    public partial class XAttribute : System.Xml.Linq.XObject
    {
        public XAttribute(System.Xml.Linq.XAttribute other) { }
        public XAttribute(System.Xml.Linq.XName name, object value) { }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> EmptySequence { get { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute>); } }
        public bool IsNamespaceDeclaration { get { return default(bool); } }
        public System.Xml.Linq.XName Name { get { return default(System.Xml.Linq.XName); } }
        public System.Xml.Linq.XAttribute NextAttribute { get { return default(System.Xml.Linq.XAttribute); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public System.Xml.Linq.XAttribute PreviousAttribute { get { return default(System.Xml.Linq.XAttribute); } }
        public string Value { get { return default(string); } set { } }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator bool (System.Xml.Linq.XAttribute attribute) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTime(System.Xml.Linq.XAttribute attribute) { return default(System.DateTime); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTimeOffset(System.Xml.Linq.XAttribute attribute) { return default(System.DateTimeOffset); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator decimal (System.Xml.Linq.XAttribute attribute) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator double (System.Xml.Linq.XAttribute attribute) { return default(double); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Guid(System.Xml.Linq.XAttribute attribute) { return default(System.Guid); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator int (System.Xml.Linq.XAttribute attribute) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator long (System.Xml.Linq.XAttribute attribute) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<bool>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<bool>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.DateTime>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<System.DateTime>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.DateTimeOffset>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<System.DateTimeOffset>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<decimal>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<decimal>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<double>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<double>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.Guid>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<System.Guid>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<int>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<int>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<long>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<long>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<float>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<float>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.TimeSpan>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<System.TimeSpan>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<uint>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<uint>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<ulong>(System.Xml.Linq.XAttribute attribute) { return default(System.Nullable<ulong>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator float (System.Xml.Linq.XAttribute attribute) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator string (System.Xml.Linq.XAttribute attribute) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.TimeSpan(System.Xml.Linq.XAttribute attribute) { return default(System.TimeSpan); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Xml.Linq.XAttribute attribute) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Xml.Linq.XAttribute attribute) { return default(ulong); }
        public void Remove() { }
        public void SetValue(object value) { }
        public override string ToString() { return default(string); }
    }
    public partial class XCData : System.Xml.Linq.XText
    {
        public XCData(string value) : base(default(string)) { }
        public XCData(System.Xml.Linq.XCData other) : base(default(string)) { }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public partial class XComment : System.Xml.Linq.XNode
    {
        public XComment(string value) { }
        public XComment(System.Xml.Linq.XComment other) { }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string Value { get { return default(string); } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public abstract partial class XContainer : System.Xml.Linq.XNode
    {
        internal XContainer() { }
        public System.Xml.Linq.XNode FirstNode { get { return default(System.Xml.Linq.XNode); } }
        public System.Xml.Linq.XNode LastNode { get { return default(System.Xml.Linq.XNode); } }
        public void Add(object content) { }
        public void Add(params object[] content) { }
        public void AddFirst(object content) { }
        public void AddFirst(params object[] content) { }
        public System.Xml.XmlWriter CreateWriter() { return default(System.Xml.XmlWriter); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodes() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Xml.Linq.XElement Element(System.Xml.Linq.XName name) { return default(System.Xml.Linq.XElement); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> Nodes() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public void RemoveNodes() { }
        public void ReplaceNodes(object content) { }
        public void ReplaceNodes(params object[] content) { }
    }
    public partial class XDeclaration
    {
        public XDeclaration(string version, string encoding, string standalone) { }
        public XDeclaration(System.Xml.Linq.XDeclaration other) { }
        public string Encoding { get { return default(string); } set { } }
        public string Standalone { get { return default(string); } set { } }
        public string Version { get { return default(string); } set { } }
        public override string ToString() { return default(string); }
    }
    public partial class XDocument : System.Xml.Linq.XContainer
    {
        public XDocument() { }
        public XDocument(params object[] content) { }
        public XDocument(System.Xml.Linq.XDeclaration declaration, params object[] content) { }
        public XDocument(System.Xml.Linq.XDocument other) { }
        public System.Xml.Linq.XDeclaration Declaration { get { return default(System.Xml.Linq.XDeclaration); } set { } }
        public System.Xml.Linq.XDocumentType DocumentType { get { return default(System.Xml.Linq.XDocumentType); } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public System.Xml.Linq.XElement Root { get { return default(System.Xml.Linq.XElement); } }
        public static System.Xml.Linq.XDocument Load(System.IO.Stream stream) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(System.IO.Stream stream, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(System.IO.TextReader textReader) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(System.IO.TextReader textReader, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(string uri) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(string uri, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(System.Xml.XmlReader reader) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Load(System.Xml.XmlReader reader, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Parse(string text) { return default(System.Xml.Linq.XDocument); }
        public static System.Xml.Linq.XDocument Parse(string text, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XDocument); }
        public void Save(System.IO.Stream stream) { }
        public void Save(System.IO.Stream stream, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.IO.TextWriter textWriter) { }
        public void Save(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.Xml.XmlWriter writer) { }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public partial class XDocumentType : System.Xml.Linq.XNode
    {
        public XDocumentType(string name, string publicId, string systemId, string internalSubset) { }
        public XDocumentType(System.Xml.Linq.XDocumentType other) { }
        public string InternalSubset { get { return default(string); } set { } }
        public string Name { get { return default(string); } set { } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string PublicId { get { return default(string); } set { } }
        public string SystemId { get { return default(string); } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public partial class XElement : System.Xml.Linq.XContainer, System.Xml.Serialization.IXmlSerializable
    {
        public XElement(System.Xml.Linq.XElement other) { }
        public XElement(System.Xml.Linq.XName name) { }
        public XElement(System.Xml.Linq.XName name, object content) { }
        public XElement(System.Xml.Linq.XName name, params object[] content) { }
        public XElement(System.Xml.Linq.XStreamingElement other) { }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> EmptySequence { get { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); } }
        public System.Xml.Linq.XAttribute FirstAttribute { get { return default(System.Xml.Linq.XAttribute); } }
        public bool HasAttributes { get { return default(bool); } }
        public bool HasElements { get { return default(bool); } }
        public bool IsEmpty { get { return default(bool); } }
        public System.Xml.Linq.XAttribute LastAttribute { get { return default(System.Xml.Linq.XAttribute); } }
        public System.Xml.Linq.XName Name { get { return default(System.Xml.Linq.XName); } set { } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string Value { get { return default(string); } set { } }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Xml.Linq.XAttribute Attribute(System.Xml.Linq.XName name) { return default(System.Xml.Linq.XAttribute); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodesAndSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Xml.Linq.XNamespace GetDefaultNamespace() { return default(System.Xml.Linq.XNamespace); }
        public System.Xml.Linq.XNamespace GetNamespaceOfPrefix(string prefix) { return default(System.Xml.Linq.XNamespace); }
        public string GetPrefixOfNamespace(System.Xml.Linq.XNamespace ns) { return default(string); }
        public static System.Xml.Linq.XElement Load(System.IO.Stream stream) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(System.IO.Stream stream, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(System.IO.TextReader textReader) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(System.IO.TextReader textReader, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(string uri) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(string uri, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(System.Xml.XmlReader reader) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Load(System.Xml.XmlReader reader, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XElement); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator bool (System.Xml.Linq.XElement element) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTime(System.Xml.Linq.XElement element) { return default(System.DateTime); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTimeOffset(System.Xml.Linq.XElement element) { return default(System.DateTimeOffset); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator decimal (System.Xml.Linq.XElement element) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator double (System.Xml.Linq.XElement element) { return default(double); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Guid(System.Xml.Linq.XElement element) { return default(System.Guid); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator int (System.Xml.Linq.XElement element) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator long (System.Xml.Linq.XElement element) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<bool>(System.Xml.Linq.XElement element) { return default(System.Nullable<bool>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.DateTime>(System.Xml.Linq.XElement element) { return default(System.Nullable<System.DateTime>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.DateTimeOffset>(System.Xml.Linq.XElement element) { return default(System.Nullable<System.DateTimeOffset>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<decimal>(System.Xml.Linq.XElement element) { return default(System.Nullable<decimal>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<double>(System.Xml.Linq.XElement element) { return default(System.Nullable<double>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.Guid>(System.Xml.Linq.XElement element) { return default(System.Nullable<System.Guid>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<int>(System.Xml.Linq.XElement element) { return default(System.Nullable<int>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<long>(System.Xml.Linq.XElement element) { return default(System.Nullable<long>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<float>(System.Xml.Linq.XElement element) { return default(System.Nullable<float>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<System.TimeSpan>(System.Xml.Linq.XElement element) { return default(System.Nullable<System.TimeSpan>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<uint>(System.Xml.Linq.XElement element) { return default(System.Nullable<uint>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Nullable<ulong>(System.Xml.Linq.XElement element) { return default(System.Nullable<ulong>); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator float (System.Xml.Linq.XElement element) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator string (System.Xml.Linq.XElement element) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.TimeSpan(System.Xml.Linq.XElement element) { return default(System.TimeSpan); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Xml.Linq.XElement element) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Xml.Linq.XElement element) { return default(ulong); }
        public static System.Xml.Linq.XElement Parse(string text) { return default(System.Xml.Linq.XElement); }
        public static System.Xml.Linq.XElement Parse(string text, System.Xml.Linq.LoadOptions options) { return default(System.Xml.Linq.XElement); }
        public void RemoveAll() { }
        public void RemoveAttributes() { }
        public void ReplaceAll(object content) { }
        public void ReplaceAll(params object[] content) { }
        public void ReplaceAttributes(object content) { }
        public void ReplaceAttributes(params object[] content) { }
        public void Save(System.IO.Stream stream) { }
        public void Save(System.IO.Stream stream, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.IO.TextWriter textWriter) { }
        public void Save(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.Xml.XmlWriter writer) { }
        public void SetAttributeValue(System.Xml.Linq.XName name, object value) { }
        public void SetElementValue(System.Xml.Linq.XName name, object value) { }
        public void SetValue(object value) { }
        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema() { return default(System.Xml.Schema.XmlSchema); }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public sealed partial class XName : System.IEquatable<System.Xml.Linq.XName>
    {
        internal XName() { }
        public string LocalName { get { return default(string); } }
        public System.Xml.Linq.XNamespace Namespace { get { return default(System.Xml.Linq.XNamespace); } }
        public string NamespaceName { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public static System.Xml.Linq.XName Get(string expandedName) { return default(System.Xml.Linq.XName); }
        public static System.Xml.Linq.XName Get(string localName, string namespaceName) { return default(System.Xml.Linq.XName); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Xml.Linq.XName left, System.Xml.Linq.XName right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Xml.Linq.XName(string expandedName) { return default(System.Xml.Linq.XName); }
        public static bool operator !=(System.Xml.Linq.XName left, System.Xml.Linq.XName right) { return default(bool); }
        bool System.IEquatable<System.Xml.Linq.XName>.Equals(System.Xml.Linq.XName other) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public sealed partial class XNamespace
    {
        internal XNamespace() { }
        public string NamespaceName { get { return default(string); } }
        public static System.Xml.Linq.XNamespace None { get { return default(System.Xml.Linq.XNamespace); } }
        public static System.Xml.Linq.XNamespace Xml { get { return default(System.Xml.Linq.XNamespace); } }
        public static System.Xml.Linq.XNamespace Xmlns { get { return default(System.Xml.Linq.XNamespace); } }
        public override bool Equals(object obj) { return default(bool); }
        public static System.Xml.Linq.XNamespace Get(string namespaceName) { return default(System.Xml.Linq.XNamespace); }
        public override int GetHashCode() { return default(int); }
        public System.Xml.Linq.XName GetName(string localName) { return default(System.Xml.Linq.XName); }
        public static System.Xml.Linq.XName operator +(System.Xml.Linq.XNamespace ns, string localName) { return default(System.Xml.Linq.XName); }
        public static bool operator ==(System.Xml.Linq.XNamespace left, System.Xml.Linq.XNamespace right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Xml.Linq.XNamespace(string namespaceName) { return default(System.Xml.Linq.XNamespace); }
        public static bool operator !=(System.Xml.Linq.XNamespace left, System.Xml.Linq.XNamespace right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public abstract partial class XNode : System.Xml.Linq.XObject
    {
        internal XNode() { }
        public static System.Xml.Linq.XNodeDocumentOrderComparer DocumentOrderComparer { get { return default(System.Xml.Linq.XNodeDocumentOrderComparer); } }
        public static System.Xml.Linq.XNodeEqualityComparer EqualityComparer { get { return default(System.Xml.Linq.XNodeEqualityComparer); } }
        public System.Xml.Linq.XNode NextNode { get { return default(System.Xml.Linq.XNode); } }
        public System.Xml.Linq.XNode PreviousNode { get { return default(System.Xml.Linq.XNode); } }
        public void AddAfterSelf(object content) { }
        public void AddAfterSelf(params object[] content) { }
        public void AddBeforeSelf(object content) { }
        public void AddBeforeSelf(params object[] content) { }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public static int CompareDocumentOrder(System.Xml.Linq.XNode n1, System.Xml.Linq.XNode n2) { return default(int); }
        public System.Xml.XmlReader CreateReader() { return default(System.Xml.XmlReader); }
        public System.Xml.XmlReader CreateReader(System.Xml.Linq.ReaderOptions readerOptions) { return default(System.Xml.XmlReader); }
        public static bool DeepEquals(System.Xml.Linq.XNode n1, System.Xml.Linq.XNode n2) { return default(bool); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsAfterSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsAfterSelf(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsBeforeSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsBeforeSelf(System.Xml.Linq.XName name) { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement>); }
        public bool IsAfter(System.Xml.Linq.XNode node) { return default(bool); }
        public bool IsBefore(System.Xml.Linq.XNode node) { return default(bool); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> NodesAfterSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> NodesBeforeSelf() { return default(System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode>); }
        public static System.Xml.Linq.XNode ReadFrom(System.Xml.XmlReader reader) { return default(System.Xml.Linq.XNode); }
        public void Remove() { }
        public void ReplaceWith(object content) { }
        public void ReplaceWith(params object[] content) { }
        public override string ToString() { return default(string); }
        public string ToString(System.Xml.Linq.SaveOptions options) { return default(string); }
        public abstract void WriteTo(System.Xml.XmlWriter writer);
    }
    public sealed partial class XNodeDocumentOrderComparer : System.Collections.Generic.IComparer<System.Xml.Linq.XNode>, System.Collections.IComparer
    {
        public XNodeDocumentOrderComparer() { }
        public int Compare(System.Xml.Linq.XNode x, System.Xml.Linq.XNode y) { return default(int); }
        int System.Collections.IComparer.Compare(object x, object y) { return default(int); }
    }
    public sealed partial class XNodeEqualityComparer : System.Collections.Generic.IEqualityComparer<System.Xml.Linq.XNode>, System.Collections.IEqualityComparer
    {
        public XNodeEqualityComparer() { }
        public bool Equals(System.Xml.Linq.XNode x, System.Xml.Linq.XNode y) { return default(bool); }
        public int GetHashCode(System.Xml.Linq.XNode obj) { return default(int); }
        bool System.Collections.IEqualityComparer.Equals(object x, object y) { return default(bool); }
        int System.Collections.IEqualityComparer.GetHashCode(object obj) { return default(int); }
    }
    public abstract partial class XObject : System.Xml.IXmlLineInfo
    {
        internal XObject() { }
        public string BaseUri { get { return default(string); } }
        public System.Xml.Linq.XDocument Document { get { return default(System.Xml.Linq.XDocument); } }
        public abstract System.Xml.XmlNodeType NodeType { get; }
        public System.Xml.Linq.XElement Parent { get { return default(System.Xml.Linq.XElement); } }
        int System.Xml.IXmlLineInfo.LineNumber { get { return default(int); } }
        int System.Xml.IXmlLineInfo.LinePosition { get { return default(int); } }
        public event System.EventHandler<System.Xml.Linq.XObjectChangeEventArgs> Changed { add { } remove { } }
        public event System.EventHandler<System.Xml.Linq.XObjectChangeEventArgs> Changing { add { } remove { } }
        public void AddAnnotation(object annotation) { }
        public object Annotation(System.Type type) { return default(object); }
        public T Annotation<T>() where T : class { return default(T); }
        public System.Collections.Generic.IEnumerable<object> Annotations(System.Type type) { return default(System.Collections.Generic.IEnumerable<object>); }
        public System.Collections.Generic.IEnumerable<T> Annotations<T>() where T : class { return default(System.Collections.Generic.IEnumerable<T>); }
        public void RemoveAnnotations(System.Type type) { }
        public void RemoveAnnotations<T>() where T : class { }
        bool System.Xml.IXmlLineInfo.HasLineInfo() { return default(bool); }
    }
    public enum XObjectChange
    {
        Add = 0,
        Name = 2,
        Remove = 1,
        Value = 3,
    }
    public partial class XObjectChangeEventArgs : System.EventArgs
    {
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Add;
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Name;
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Remove;
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Value;
        public XObjectChangeEventArgs(System.Xml.Linq.XObjectChange objectChange) { }
        public System.Xml.Linq.XObjectChange ObjectChange { get { return default(System.Xml.Linq.XObjectChange); } }
    }
    public partial class XProcessingInstruction : System.Xml.Linq.XNode
    {
        public XProcessingInstruction(string target, string data) { }
        public XProcessingInstruction(System.Xml.Linq.XProcessingInstruction other) { }
        public string Data { get { return default(string); } set { } }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string Target { get { return default(string); } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public partial class XStreamingElement
    {
        public XStreamingElement(System.Xml.Linq.XName name) { }
        public XStreamingElement(System.Xml.Linq.XName name, object content) { }
        public XStreamingElement(System.Xml.Linq.XName name, params object[] content) { }
        public System.Xml.Linq.XName Name { get { return default(System.Xml.Linq.XName); } set { } }
        public void Add(object content) { }
        public void Add(params object[] content) { }
        public void Save(System.IO.Stream stream) { }
        public void Save(System.IO.Stream stream, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.IO.TextWriter textWriter) { }
        public void Save(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.Xml.XmlWriter writer) { }
        public override string ToString() { return default(string); }
        public string ToString(System.Xml.Linq.SaveOptions options) { return default(string); }
        public void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public partial class XText : System.Xml.Linq.XNode
    {
        public XText(string value) { }
        public XText(System.Xml.Linq.XText other) { }
        public override System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
        public string Value { get { return default(string); } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
    }
}
