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
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source, System.Xml.Linq.XName name) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XNode { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors<T>(this System.Collections.Generic.IEnumerable<T> source, System.Xml.Linq.XName name) where T : System.Xml.Linq.XNode { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source, System.Xml.Linq.XName name) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodesAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodes<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf(this System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> source, System.Xml.Linq.XName name) { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants<T>(this System.Collections.Generic.IEnumerable<T> source, System.Xml.Linq.XName name) where T : System.Xml.Linq.XContainer { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements<T>(this System.Collections.Generic.IEnumerable<T> source, System.Xml.Linq.XName name) where T : System.Xml.Linq.XContainer { throw null; }
        public static System.Collections.Generic.IEnumerable<T> InDocumentOrder<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XNode { throw null; }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> Nodes<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Xml.Linq.XContainer { throw null; }
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
        None = 0,
        DisableFormatting = 1,
        OmitDuplicateNamespaces = 2,
    }
    public partial class XAttribute : System.Xml.Linq.XObject
    {
        public XAttribute(System.Xml.Linq.XAttribute other) { }
        public XAttribute(System.Xml.Linq.XName name, object value) { }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> EmptySequence { get { throw null; } }
        public bool IsNamespaceDeclaration { get { throw null; } }
        public System.Xml.Linq.XName Name { get { throw null; } }
        public System.Xml.Linq.XAttribute NextAttribute { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public System.Xml.Linq.XAttribute PreviousAttribute { get { throw null; } }
        public string Value { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator bool (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTime (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTimeOffset (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator decimal (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator double (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Guid (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator int (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator long (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator bool? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTimeOffset? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTime? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator decimal? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator double? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Guid? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator int? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator long? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator float? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.TimeSpan? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong? (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator float (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator string (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.TimeSpan (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Xml.Linq.XAttribute attribute) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Xml.Linq.XAttribute attribute) { throw null; }
        public void Remove() { }
        public void SetValue(object value) { }
        public override string ToString() { throw null; }
    }
    public partial class XCData : System.Xml.Linq.XText
    {
        public XCData(string value) : base (default(string)) { }
        public XCData(System.Xml.Linq.XCData other) : base (default(string)) { }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class XComment : System.Xml.Linq.XNode
    {
        public XComment(string value) { }
        public XComment(System.Xml.Linq.XComment other) { }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string Value { get { throw null; } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public abstract partial class XContainer : System.Xml.Linq.XNode
    {
        internal XContainer() { }
        public System.Xml.Linq.XNode FirstNode { get { throw null; } }
        public System.Xml.Linq.XNode LastNode { get { throw null; } }
        public void Add(object content) { }
        public void Add(params object[] content) { }
        public void AddFirst(object content) { }
        public void AddFirst(params object[] content) { }
        public System.Xml.XmlWriter CreateWriter() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodes() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Descendants(System.Xml.Linq.XName name) { throw null; }
        public System.Xml.Linq.XElement Element(System.Xml.Linq.XName name) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Elements(System.Xml.Linq.XName name) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> Nodes() { throw null; }
        public void RemoveNodes() { }
        public void ReplaceNodes(object content) { }
        public void ReplaceNodes(params object[] content) { }
    }
    public partial class XDeclaration
    {
        public XDeclaration(string version, string encoding, string standalone) { }
        public XDeclaration(System.Xml.Linq.XDeclaration other) { }
        public string Encoding { get { throw null; } set { } }
        public string Standalone { get { throw null; } set { } }
        public string Version { get { throw null; } set { } }
        public override string ToString() { throw null; }
    }
    public partial class XDocument : System.Xml.Linq.XContainer
    {
        public XDocument() { }
        public XDocument(params object[] content) { }
        public XDocument(System.Xml.Linq.XDeclaration declaration, params object[] content) { }
        public XDocument(System.Xml.Linq.XDocument other) { }
        public System.Xml.Linq.XDeclaration Declaration { get { throw null; } set { } }
        public System.Xml.Linq.XDocumentType DocumentType { get { throw null; } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public System.Xml.Linq.XElement Root { get { throw null; } }
        public static System.Xml.Linq.XDocument Load(System.IO.Stream stream) { throw null; }
        public static System.Xml.Linq.XDocument Load(System.IO.Stream stream, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Xml.Linq.XDocument Load(System.IO.TextReader textReader) { throw null; }
        public static System.Xml.Linq.XDocument Load(System.IO.TextReader textReader, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Xml.Linq.XDocument Load(string uri) { throw null; }
        public static System.Xml.Linq.XDocument Load(string uri, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Xml.Linq.XDocument Load(System.Xml.XmlReader reader) { throw null; }
        public static System.Xml.Linq.XDocument Load(System.Xml.XmlReader reader, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XDocument> LoadAsync(System.IO.Stream stream, System.Xml.Linq.LoadOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XDocument> LoadAsync(System.IO.TextReader textReader, System.Xml.Linq.LoadOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XDocument> LoadAsync(System.Xml.XmlReader reader, System.Xml.Linq.LoadOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Xml.Linq.XDocument Parse(string text) { throw null; }
        public static System.Xml.Linq.XDocument Parse(string text, System.Xml.Linq.LoadOptions options) { throw null; }
        public void Save(System.IO.Stream stream) { }
        public void Save(System.IO.Stream stream, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.IO.TextWriter textWriter) { }
        public void Save(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options) { }
        public void Save(string fileName) { }
        public void Save(string fileName, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.Xml.XmlWriter writer) { }
        public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream, System.Xml.Linq.SaveOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task SaveAsync(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task SaveAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class XDocumentType : System.Xml.Linq.XNode
    {
        public XDocumentType(string name, string publicId, string systemId, string internalSubset) { }
        public XDocumentType(System.Xml.Linq.XDocumentType other) { }
        public string InternalSubset { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string PublicId { get { throw null; } set { } }
        public string SystemId { get { throw null; } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute(null, IsAny=true)]
    public partial class XElement : System.Xml.Linq.XContainer, System.Xml.Serialization.IXmlSerializable
    {
        public XElement(System.Xml.Linq.XElement other) { }
        public XElement(System.Xml.Linq.XName name) { }
        public XElement(System.Xml.Linq.XName name, object content) { }
        public XElement(System.Xml.Linq.XName name, params object[] content) { }
        public XElement(System.Xml.Linq.XStreamingElement other) { }
        public static System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> EmptySequence { get { throw null; } }
        public System.Xml.Linq.XAttribute FirstAttribute { get { throw null; } }
        public bool HasAttributes { get { throw null; } }
        public bool HasElements { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public System.Xml.Linq.XAttribute LastAttribute { get { throw null; } }
        public System.Xml.Linq.XName Name { get { throw null; } set { } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string Value { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> AncestorsAndSelf(System.Xml.Linq.XName name) { throw null; }
        public System.Xml.Linq.XAttribute Attribute(System.Xml.Linq.XName name) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XAttribute> Attributes(System.Xml.Linq.XName name) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> DescendantNodesAndSelf() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> DescendantsAndSelf(System.Xml.Linq.XName name) { throw null; }
        public System.Xml.Linq.XNamespace GetDefaultNamespace() { throw null; }
        public System.Xml.Linq.XNamespace GetNamespaceOfPrefix(string prefix) { throw null; }
        public string GetPrefixOfNamespace(System.Xml.Linq.XNamespace ns) { throw null; }
        public static System.Xml.Linq.XElement Load(System.IO.Stream stream) { throw null; }
        public static System.Xml.Linq.XElement Load(System.IO.Stream stream, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Xml.Linq.XElement Load(System.IO.TextReader textReader) { throw null; }
        public static System.Xml.Linq.XElement Load(System.IO.TextReader textReader, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Xml.Linq.XElement Load(string uri) { throw null; }
        public static System.Xml.Linq.XElement Load(string uri, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Xml.Linq.XElement Load(System.Xml.XmlReader reader) { throw null; }
        public static System.Xml.Linq.XElement Load(System.Xml.XmlReader reader, System.Xml.Linq.LoadOptions options) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XElement> LoadAsync(System.IO.Stream stream, System.Xml.Linq.LoadOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XElement> LoadAsync(System.IO.TextReader textReader, System.Xml.Linq.LoadOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XElement> LoadAsync(System.Xml.XmlReader reader, System.Xml.Linq.LoadOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator bool (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTime (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTimeOffset (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator decimal (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator double (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Guid (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator int (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator long (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator bool? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTimeOffset? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.DateTime? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator decimal? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator double? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Guid? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator int? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator long? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator float? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.TimeSpan? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong? (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator float (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator string (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.TimeSpan (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Xml.Linq.XElement element) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Xml.Linq.XElement element) { throw null; }
        public static System.Xml.Linq.XElement Parse(string text) { throw null; }
        public static System.Xml.Linq.XElement Parse(string text, System.Xml.Linq.LoadOptions options) { throw null; }
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
        public void Save(string fileName) { }
        public void Save(string fileName, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.Xml.XmlWriter writer) { }
        public System.Threading.Tasks.Task SaveAsync(System.IO.Stream stream, System.Xml.Linq.SaveOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task SaveAsync(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task SaveAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void SetAttributeValue(System.Xml.Linq.XName name, object value) { }
        public void SetElementValue(System.Xml.Linq.XName name, object value) { }
        public void SetValue(object value) { }
        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public sealed partial class XName : System.IEquatable<System.Xml.Linq.XName>, System.Runtime.Serialization.ISerializable
    {
        internal XName() { }
        public string LocalName { get { throw null; } }
        public System.Xml.Linq.XNamespace Namespace { get { throw null; } }
        public string NamespaceName { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static System.Xml.Linq.XName Get(string expandedName) { throw null; }
        public static System.Xml.Linq.XName Get(string localName, string namespaceName) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Xml.Linq.XName left, System.Xml.Linq.XName right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Xml.Linq.XName (string expandedName) { throw null; }
        public static bool operator !=(System.Xml.Linq.XName left, System.Xml.Linq.XName right) { throw null; }
        bool System.IEquatable<System.Xml.Linq.XName>.Equals(System.Xml.Linq.XName other) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public sealed partial class XNamespace
    {
        internal XNamespace() { }
        public string NamespaceName { get { throw null; } }
        public static System.Xml.Linq.XNamespace None { get { throw null; } }
        public static System.Xml.Linq.XNamespace Xml { get { throw null; } }
        public static System.Xml.Linq.XNamespace Xmlns { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public static System.Xml.Linq.XNamespace Get(string namespaceName) { throw null; }
        public override int GetHashCode() { throw null; }
        public System.Xml.Linq.XName GetName(string localName) { throw null; }
        public static System.Xml.Linq.XName operator +(System.Xml.Linq.XNamespace ns, string localName) { throw null; }
        public static bool operator ==(System.Xml.Linq.XNamespace left, System.Xml.Linq.XNamespace right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Xml.Linq.XNamespace (string namespaceName) { throw null; }
        public static bool operator !=(System.Xml.Linq.XNamespace left, System.Xml.Linq.XNamespace right) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class XNode : System.Xml.Linq.XObject
    {
        internal XNode() { }
        public static System.Xml.Linq.XNodeDocumentOrderComparer DocumentOrderComparer { get { throw null; } }
        public static System.Xml.Linq.XNodeEqualityComparer EqualityComparer { get { throw null; } }
        public System.Xml.Linq.XNode NextNode { get { throw null; } }
        public System.Xml.Linq.XNode PreviousNode { get { throw null; } }
        public void AddAfterSelf(object content) { }
        public void AddAfterSelf(params object[] content) { }
        public void AddBeforeSelf(object content) { }
        public void AddBeforeSelf(params object[] content) { }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> Ancestors(System.Xml.Linq.XName name) { throw null; }
        public static int CompareDocumentOrder(System.Xml.Linq.XNode n1, System.Xml.Linq.XNode n2) { throw null; }
        public System.Xml.XmlReader CreateReader() { throw null; }
        public System.Xml.XmlReader CreateReader(System.Xml.Linq.ReaderOptions readerOptions) { throw null; }
        public static bool DeepEquals(System.Xml.Linq.XNode n1, System.Xml.Linq.XNode n2) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsAfterSelf() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsAfterSelf(System.Xml.Linq.XName name) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsBeforeSelf() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> ElementsBeforeSelf(System.Xml.Linq.XName name) { throw null; }
        public bool IsAfter(System.Xml.Linq.XNode node) { throw null; }
        public bool IsBefore(System.Xml.Linq.XNode node) { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> NodesAfterSelf() { throw null; }
        public System.Collections.Generic.IEnumerable<System.Xml.Linq.XNode> NodesBeforeSelf() { throw null; }
        public static System.Xml.Linq.XNode ReadFrom(System.Xml.XmlReader reader) { throw null; }
        public static System.Threading.Tasks.Task<System.Xml.Linq.XNode> ReadFromAsync(System.Xml.XmlReader reader, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Remove() { }
        public void ReplaceWith(object content) { }
        public void ReplaceWith(params object[] content) { }
        public override string ToString() { throw null; }
        public string ToString(System.Xml.Linq.SaveOptions options) { throw null; }
        public abstract void WriteTo(System.Xml.XmlWriter writer);
        public abstract System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken);
    }
    public sealed partial class XNodeDocumentOrderComparer : System.Collections.Generic.IComparer<System.Xml.Linq.XNode>, System.Collections.IComparer
    {
        public XNodeDocumentOrderComparer() { }
        public int Compare(System.Xml.Linq.XNode x, System.Xml.Linq.XNode y) { throw null; }
        int System.Collections.IComparer.Compare(object x, object y) { throw null; }
    }
    public sealed partial class XNodeEqualityComparer : System.Collections.Generic.IEqualityComparer<System.Xml.Linq.XNode>, System.Collections.IEqualityComparer
    {
        public XNodeEqualityComparer() { }
        public bool Equals(System.Xml.Linq.XNode x, System.Xml.Linq.XNode y) { throw null; }
        public int GetHashCode(System.Xml.Linq.XNode obj) { throw null; }
        bool System.Collections.IEqualityComparer.Equals(object x, object y) { throw null; }
        int System.Collections.IEqualityComparer.GetHashCode(object obj) { throw null; }
    }
    public abstract partial class XObject : System.Xml.IXmlLineInfo
    {
        internal XObject() { }
        public string BaseUri { get { throw null; } }
        public System.Xml.Linq.XDocument Document { get { throw null; } }
        public abstract System.Xml.XmlNodeType NodeType { get; }
        public System.Xml.Linq.XElement Parent { get { throw null; } }
        int System.Xml.IXmlLineInfo.LineNumber { get { throw null; } }
        int System.Xml.IXmlLineInfo.LinePosition { get { throw null; } }
        public event System.EventHandler<System.Xml.Linq.XObjectChangeEventArgs> Changed { add { } remove { } }
        public event System.EventHandler<System.Xml.Linq.XObjectChangeEventArgs> Changing { add { } remove { } }
        public void AddAnnotation(object annotation) { }
        public object Annotation(System.Type type) { throw null; }
        public System.Collections.Generic.IEnumerable<object> Annotations(System.Type type) { throw null; }
        public System.Collections.Generic.IEnumerable<T> Annotations<T>() where T : class { throw null; }
        public T Annotation<T>() where T : class { throw null; }
        public void RemoveAnnotations(System.Type type) { }
        public void RemoveAnnotations<T>() where T : class { }
        bool System.Xml.IXmlLineInfo.HasLineInfo() { throw null; }
    }
    public enum XObjectChange
    {
        Add = 0,
        Remove = 1,
        Name = 2,
        Value = 3,
    }
    public partial class XObjectChangeEventArgs : System.EventArgs
    {
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Add;
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Name;
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Remove;
        public static readonly System.Xml.Linq.XObjectChangeEventArgs Value;
        public XObjectChangeEventArgs(System.Xml.Linq.XObjectChange objectChange) { }
        public System.Xml.Linq.XObjectChange ObjectChange { get { throw null; } }
    }
    public partial class XProcessingInstruction : System.Xml.Linq.XNode
    {
        public XProcessingInstruction(string target, string data) { }
        public XProcessingInstruction(System.Xml.Linq.XProcessingInstruction other) { }
        public string Data { get { throw null; } set { } }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string Target { get { throw null; } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class XStreamingElement
    {
        public XStreamingElement(System.Xml.Linq.XName name) { }
        public XStreamingElement(System.Xml.Linq.XName name, object content) { }
        public XStreamingElement(System.Xml.Linq.XName name, params object[] content) { }
        public System.Xml.Linq.XName Name { get { throw null; } set { } }
        public void Add(object content) { }
        public void Add(params object[] content) { }
        public void Save(System.IO.Stream stream) { }
        public void Save(System.IO.Stream stream, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.IO.TextWriter textWriter) { }
        public void Save(System.IO.TextWriter textWriter, System.Xml.Linq.SaveOptions options) { }
        public void Save(string fileName) { }
        public void Save(string fileName, System.Xml.Linq.SaveOptions options) { }
        public void Save(System.Xml.XmlWriter writer) { }
        public override string ToString() { throw null; }
        public string ToString(System.Xml.Linq.SaveOptions options) { throw null; }
        public void WriteTo(System.Xml.XmlWriter writer) { }
    }
    public partial class XText : System.Xml.Linq.XNode
    {
        public XText(string value) { }
        public XText(System.Xml.Linq.XText other) { }
        public override System.Xml.XmlNodeType NodeType { get { throw null; } }
        public string Value { get { throw null; } set { } }
        public override void WriteTo(System.Xml.XmlWriter writer) { }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace System.Xml.Schema
{
    public static partial class Extensions
    {
        public static System.Xml.Schema.IXmlSchemaInfo GetSchemaInfo(this System.Xml.Linq.XAttribute source) { throw null; }
        public static System.Xml.Schema.IXmlSchemaInfo GetSchemaInfo(this System.Xml.Linq.XElement source) { throw null; }
        public static void Validate(this System.Xml.Linq.XAttribute source, System.Xml.Schema.XmlSchemaObject partialValidationType, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler) { }
        public static void Validate(this System.Xml.Linq.XAttribute source, System.Xml.Schema.XmlSchemaObject partialValidationType, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler, bool addSchemaInfo) { }
        public static void Validate(this System.Xml.Linq.XDocument source, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler) { }
        public static void Validate(this System.Xml.Linq.XDocument source, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler, bool addSchemaInfo) { }
        public static void Validate(this System.Xml.Linq.XElement source, System.Xml.Schema.XmlSchemaObject partialValidationType, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler) { }
        public static void Validate(this System.Xml.Linq.XElement source, System.Xml.Schema.XmlSchemaObject partialValidationType, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.ValidationEventHandler validationEventHandler, bool addSchemaInfo) { }
    }
}
