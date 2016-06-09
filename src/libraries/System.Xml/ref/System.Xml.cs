// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


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
        Prohibit = 0,
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
    public static partial class XmlConvert
    {
        public static string DecodeName(string name) { return default(string); }
        public static string EncodeLocalName(string name) { return default(string); }
        public static string EncodeName(string name) { return default(string); }
        public static string EncodeNmToken(string name) { return default(string); }
        public static bool ToBoolean(string s) { return default(bool); }
        public static byte ToByte(string s) { return default(byte); }
        public static char ToChar(string s) { return default(char); }
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
    public partial class XmlException : System.Exception
    {
        public XmlException() { }
        public XmlException(string message) { }
        public XmlException(string message, System.Exception innerException) { }
        public XmlException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
        public int LineNumber { get { return default(int); } }
        public int LinePosition { get { return default(int); } }
        public override string Message { get { return default(string); } }
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
        public abstract System.Xml.ReadState ReadState { get; }
        public virtual System.Xml.XmlReaderSettings Settings { get { return default(System.Xml.XmlReaderSettings); } }
        public abstract string Value { get; }
        public virtual System.Type ValueType { get { return default(System.Type); } }
        public virtual string XmlLang { get { return default(string); } }
        public virtual System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public static System.Xml.XmlReader Create(System.IO.Stream input) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.Stream input, System.Xml.XmlReaderSettings settings, System.Xml.XmlParserContext inputContext) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(System.IO.TextReader input, System.Xml.XmlReaderSettings settings, System.Xml.XmlParserContext inputContext) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(string inputUri) { return default(System.Xml.XmlReader); }
        public static System.Xml.XmlReader Create(string inputUri, System.Xml.XmlReaderSettings settings) { return default(System.Xml.XmlReader); }
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
        public virtual System.Threading.Tasks.Task<System.Xml.XmlNodeType> MoveToContentAsync() { return default(System.Threading.Tasks.Task<System.Xml.XmlNodeType>); }
        public abstract bool MoveToElement();
        public abstract bool MoveToFirstAttribute();
        public abstract bool MoveToNextAttribute();
        public abstract bool Read();
        public virtual System.Threading.Tasks.Task<bool> ReadAsync() { return default(System.Threading.Tasks.Task<bool>); }
        public abstract bool ReadAttributeValue();
        public virtual object ReadContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
        public virtual System.Threading.Tasks.Task<object> ReadContentAsAsync(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(System.Threading.Tasks.Task<object>); }
        public virtual int ReadContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual int ReadContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual bool ReadContentAsBoolean() { return default(bool); }
        public virtual System.DateTimeOffset ReadContentAsDateTimeOffset() { return default(System.DateTimeOffset); }
        public virtual decimal ReadContentAsDecimal() { return default(decimal); }
        public virtual double ReadContentAsDouble() { return default(double); }
        public virtual float ReadContentAsFloat() { return default(float); }
        public virtual int ReadContentAsInt() { return default(int); }
        public virtual long ReadContentAsLong() { return default(long); }
        public virtual object ReadContentAsObject() { return default(object); }
        public virtual System.Threading.Tasks.Task<object> ReadContentAsObjectAsync() { return default(System.Threading.Tasks.Task<object>); }
        public virtual string ReadContentAsString() { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadContentAsStringAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual object ReadElementContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
        public virtual object ReadElementContentAs(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI) { return default(object); }
        public virtual System.Threading.Tasks.Task<object> ReadElementContentAsAsync(System.Type returnType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(System.Threading.Tasks.Task<object>); }
        public virtual int ReadElementContentAsBase64(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual int ReadElementContentAsBinHex(byte[] buffer, int index, int count) { return default(int); }
        public virtual System.Threading.Tasks.Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task<int>); }
        public virtual bool ReadElementContentAsBoolean() { return default(bool); }
        public virtual bool ReadElementContentAsBoolean(string localName, string namespaceURI) { return default(bool); }
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
        public virtual System.Threading.Tasks.Task<object> ReadElementContentAsObjectAsync() { return default(System.Threading.Tasks.Task<object>); }
        public virtual string ReadElementContentAsString() { return default(string); }
        public virtual string ReadElementContentAsString(string localName, string namespaceURI) { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadElementContentAsStringAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual void ReadEndElement() { }
        public virtual string ReadInnerXml() { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadInnerXmlAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual string ReadOuterXml() { return default(string); }
        public virtual System.Threading.Tasks.Task<string> ReadOuterXmlAsync() { return default(System.Threading.Tasks.Task<string>); }
        public virtual void ReadStartElement() { }
        public virtual void ReadStartElement(string name) { }
        public virtual void ReadStartElement(string localname, string ns) { }
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
        public System.Xml.XmlReaderSettings Clone() { return default(System.Xml.XmlReaderSettings); }
        public void Reset() { }
    }
    public enum XmlSpace
    {
        Default = 1,
        None = 0,
        Preserve = 2,
    }
    public abstract partial class XmlWriter : System.IDisposable
    {
        protected XmlWriter() { }
        public virtual System.Xml.XmlWriterSettings Settings { get { return default(System.Xml.XmlWriterSettings); } }
        public abstract System.Xml.WriteState WriteState { get; }
        public virtual string XmlLang { get { return default(string); } }
        public virtual System.Xml.XmlSpace XmlSpace { get { return default(System.Xml.XmlSpace); } }
        public static System.Xml.XmlWriter Create(System.IO.Stream output) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.IO.Stream output, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.IO.TextWriter output) { return default(System.Xml.XmlWriter); }
        public static System.Xml.XmlWriter Create(System.IO.TextWriter output, System.Xml.XmlWriterSettings settings) { return default(System.Xml.XmlWriter); }
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
        public virtual System.Threading.Tasks.Task WriteNodeAsync(System.Xml.XmlReader reader, bool defattr) { return default(System.Threading.Tasks.Task); }
        public abstract void WriteProcessingInstruction(string name, string text);
        public virtual System.Threading.Tasks.Task WriteProcessingInstructionAsync(string name, string text) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteQualifiedName(string localName, string ns) { }
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
    public sealed partial class XmlWriterSettings
    {
        public XmlWriterSettings() { }
        public bool Async { get { return default(bool); } set { } }
        public bool CheckCharacters { get { return default(bool); } set { } }
        public bool CloseOutput { get { return default(bool); } set { } }
        public System.Xml.ConformanceLevel ConformanceLevel { get { return default(System.Xml.ConformanceLevel); } set { } }
        public System.Text.Encoding Encoding { get { return default(System.Text.Encoding); } set { } }
        public bool Indent { get { return default(bool); } set { } }
        public string IndentChars { get { return default(string); } set { } }
        public System.Xml.NamespaceHandling NamespaceHandling { get { return default(System.Xml.NamespaceHandling); } set { } }
        public string NewLineChars { get { return default(string); } set { } }
        public System.Xml.NewLineHandling NewLineHandling { get { return default(System.Xml.NewLineHandling); } set { } }
        public bool NewLineOnAttributes { get { return default(bool); } set { } }
        public bool OmitXmlDeclaration { get { return default(bool); } set { } }
        public bool WriteEndDocumentOnClose { get { return default(bool); } set { } }
        public System.Xml.XmlWriterSettings Clone() { return default(System.Xml.XmlWriterSettings); }
        public void Reset() { }
    }
}
namespace System.Xml.Schema
{
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public partial class XmlSchema
    {
        internal XmlSchema() { }
    }
    public enum XmlSchemaForm
    {
        None = 0,
        Qualified = 1,
        Unqualified = 2,
    }
}
namespace System.Xml.Serialization
{
    public partial interface IXmlSerializable
    {
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        System.Xml.Schema.XmlSchema GetSchema();
        void ReadXml(System.Xml.XmlReader reader);
        void WriteXml(System.Xml.XmlWriter writer);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1036))]
    public sealed partial class XmlSchemaProviderAttribute : System.Attribute
    {
        public XmlSchemaProviderAttribute(string methodName) { }
        public bool IsAny { get { return default(bool); } set { } }
        public string MethodName { get { return default(string); } }
    }
}
