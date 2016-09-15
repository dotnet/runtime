// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Web
{
    public sealed partial class HttpUtility
    {
        public HttpUtility() { }
        public static string HtmlAttributeEncode(string s) { return default(string); }
        public static void HtmlAttributeEncode(string s, System.IO.TextWriter output) { }
        public static string HtmlDecode(string s) { return default(string); }
        public static void HtmlDecode(string s, System.IO.TextWriter output) { }
        public static string HtmlEncode(object value) { return default(string); }
        public static string HtmlEncode(string s) { return default(string); }
        public static void HtmlEncode(string s, System.IO.TextWriter output) { }
        public static string JavaScriptStringEncode(string value) { return default(string); }
        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes) { return default(string); }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query) { return default(System.Collections.Specialized.NameValueCollection); }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query, System.Text.Encoding encoding) { return default(System.Collections.Specialized.NameValueCollection); }
        public static string UrlDecode(byte[] bytes, int offset, int count, System.Text.Encoding e) { return default(string); }
        public static string UrlDecode(byte[] bytes, System.Text.Encoding e) { return default(string); }
        public static string UrlDecode(string str) { return default(string); }
        public static string UrlDecode(string s, System.Text.Encoding e) { return default(string); }
        public static byte[] UrlDecodeToBytes(byte[] bytes) { return default(byte[]); }
        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count) { return default(byte[]); }
        public static byte[] UrlDecodeToBytes(string str) { return default(byte[]); }
        public static byte[] UrlDecodeToBytes(string str, System.Text.Encoding e) { return default(byte[]); }
        public static string UrlEncode(byte[] bytes) { return default(string); }
        public static string UrlEncode(byte[] bytes, int offset, int count) { return default(string); }
        public static string UrlEncode(string str) { return default(string); }
        public static string UrlEncode(string s, System.Text.Encoding Enc) { return default(string); }
        public static byte[] UrlEncodeToBytes(byte[] bytes) { return default(byte[]); }
        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count) { return default(byte[]); }
        public static byte[] UrlEncodeToBytes(string str) { return default(byte[]); }
        public static byte[] UrlEncodeToBytes(string str, System.Text.Encoding e) { return default(byte[]); }
        public static string UrlEncodeUnicode(string str) { return default(string); }
        public static byte[] UrlEncodeUnicodeToBytes(string str) { return default(byte[]); }
        public static string UrlPathEncode(string s) { return default(string); }
    }
}
namespace System.Web.Services
{
    public enum TransactionOption
    {
        Disabled = 0,
        NotSupported = 1,
        Required = 2,
        RequiresNew = 3,
        Supported = 4,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=true)]
    public sealed partial class WebMethodAttribute : System.Attribute
    {
        public WebMethodAttribute() { }
        public WebMethodAttribute(bool enableSession) { }
        public WebMethodAttribute(bool enableSession, System.Web.Services.TransactionOption transactionOption) { }
        public WebMethodAttribute(bool enableSession, System.Web.Services.TransactionOption transactionOption, int cacheDuration) { }
        public WebMethodAttribute(bool enableSession, System.Web.Services.TransactionOption transactionOption, int cacheDuration, bool bufferResponse) { }
        public bool BufferResponse { get { return default(bool); } set { } }
        public int CacheDuration { get { return default(int); } set { } }
        public string Description { get { return default(string); } set { } }
        public bool EnableSession { get { return default(bool); } set { } }
        public string MessageName { get { return default(string); } set { } }
        public System.Web.Services.TransactionOption TransactionOption { get { return default(System.Web.Services.TransactionOption); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1028), Inherited=true)]
    public sealed partial class WebServiceAttribute : System.Attribute
    {
        public const string DefaultNamespace = "http://tempuri.org/";
        public WebServiceAttribute() { }
        public string Description { get { return default(string); } set { } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1028), AllowMultiple=true, Inherited=true)]
    public sealed partial class WebServiceBindingAttribute : System.Attribute
    {
        public WebServiceBindingAttribute() { }
        public WebServiceBindingAttribute(string name) { }
        public WebServiceBindingAttribute(string name, string ns) { }
        public WebServiceBindingAttribute(string name, string ns, string location) { }
        public System.Web.Services.WsiProfiles ConformsTo { get { return default(System.Web.Services.WsiProfiles); } set { } }
        public bool EmitConformanceClaims { get { return default(bool); } set { } }
        public string Location { get { return default(string); } set { } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    [System.FlagsAttribute]
    public enum WsiProfiles
    {
        BasicProfile1_1 = 1,
        None = 0,
    }
}
namespace System.Web.Services.Configuration
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true)]
    public sealed partial class XmlFormatExtensionAttribute : System.Attribute
    {
        public XmlFormatExtensionAttribute() { }
        public XmlFormatExtensionAttribute(string elementName, string ns, System.Type extensionPoint1) { }
        public XmlFormatExtensionAttribute(string elementName, string ns, System.Type extensionPoint1, System.Type extensionPoint2) { }
        public XmlFormatExtensionAttribute(string elementName, string ns, System.Type extensionPoint1, System.Type extensionPoint2, System.Type extensionPoint3) { }
        public XmlFormatExtensionAttribute(string elementName, string ns, System.Type extensionPoint1, System.Type extensionPoint2, System.Type extensionPoint3, System.Type extensionPoint4) { }
        public XmlFormatExtensionAttribute(string elementName, string ns, System.Type[] extensionPoints) { }
        public string ElementName { get { return default(string); } set { } }
        public System.Type[] ExtensionPoints { get { return default(System.Type[]); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true)]
    public sealed partial class XmlFormatExtensionPointAttribute : System.Attribute
    {
        public XmlFormatExtensionPointAttribute(string memberName) { }
        public bool AllowElements { get { return default(bool); } set { } }
        public string MemberName { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=true, Inherited=true)]
    public sealed partial class XmlFormatExtensionPrefixAttribute : System.Attribute
    {
        public XmlFormatExtensionPrefixAttribute() { }
        public XmlFormatExtensionPrefixAttribute(string prefix, string ns) { }
        public string Namespace { get { return default(string); } set { } }
        public string Prefix { get { return default(string); } set { } }
    }
}
namespace System.Web.Services.Description
{
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Binding : System.Web.Services.Description.NamedItem
    {
        public Binding() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("operation")]
        public System.Web.Services.Description.OperationBindingCollection Operations { get { return default(System.Web.Services.Description.OperationBindingCollection); } }
        public System.Web.Services.Description.ServiceDescription ServiceDescription { get { return default(System.Web.Services.Description.ServiceDescription); } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public System.Xml.XmlQualifiedName Type { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public sealed partial class BindingCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal BindingCollection() { }
        public System.Web.Services.Description.Binding this[int index] { get { return default(System.Web.Services.Description.Binding); } set { } }
        public System.Web.Services.Description.Binding this[string name] { get { return default(System.Web.Services.Description.Binding); } }
        public int Add(System.Web.Services.Description.Binding binding) { return default(int); }
        public bool Contains(System.Web.Services.Description.Binding binding) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.Binding[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.Binding binding) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.Binding binding) { }
        public void Remove(System.Web.Services.Description.Binding binding) { }
        protected override void SetParent(object value, object parent) { }
    }
    public abstract partial class DocumentableItem
    {
        protected DocumentableItem() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string Documentation { get { return default(string); } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Xml.Serialization.XmlAnyElementAttribute(Name="documentation", Namespace="http://schemas.xmlsoap.org/wsdl/")]
        public System.Xml.XmlElement DocumentationElement { get { return default(System.Xml.XmlElement); } set { } }
        [System.Xml.Serialization.XmlAnyAttributeAttribute]
        public System.Xml.XmlAttribute[] ExtensibleAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public abstract System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get; }
        [System.Xml.Serialization.XmlNamespaceDeclarationsAttribute]
        public System.Xml.Serialization.XmlSerializerNamespaces Namespaces { get { return default(System.Xml.Serialization.XmlSerializerNamespaces); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class FaultBinding : System.Web.Services.Description.MessageBinding
    {
        public FaultBinding() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    public sealed partial class FaultBindingCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal FaultBindingCollection() { }
        public System.Web.Services.Description.FaultBinding this[int index] { get { return default(System.Web.Services.Description.FaultBinding); } set { } }
        public System.Web.Services.Description.FaultBinding this[string name] { get { return default(System.Web.Services.Description.FaultBinding); } }
        public int Add(System.Web.Services.Description.FaultBinding bindingOperationFault) { return default(int); }
        public bool Contains(System.Web.Services.Description.FaultBinding bindingOperationFault) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.FaultBinding[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.FaultBinding bindingOperationFault) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.FaultBinding bindingOperationFault) { }
        public void Remove(System.Web.Services.Description.FaultBinding bindingOperationFault) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("address", "http://schemas.xmlsoap.org/wsdl/http/", typeof(System.Web.Services.Description.Port))]
    public sealed partial class HttpAddressBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public HttpAddressBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("location")]
        public string Location { get { return default(string); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("binding", "http://schemas.xmlsoap.org/wsdl/http/", typeof(System.Web.Services.Description.Binding))]
    [System.Web.Services.Configuration.XmlFormatExtensionPrefixAttribute("http", "http://schemas.xmlsoap.org/wsdl/http/")]
    public sealed partial class HttpBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public const string Namespace = "http://schemas.xmlsoap.org/wsdl/http/";
        public HttpBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("verb")]
        public string Verb { get { return default(string); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("operation", "http://schemas.xmlsoap.org/wsdl/http/", typeof(System.Web.Services.Description.OperationBinding))]
    public sealed partial class HttpOperationBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public HttpOperationBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("location")]
        public string Location { get { return default(string); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("urlEncoded", "http://schemas.xmlsoap.org/wsdl/http/", typeof(System.Web.Services.Description.InputBinding))]
    public sealed partial class HttpUrlEncodedBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public HttpUrlEncodedBinding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("urlReplacement", "http://schemas.xmlsoap.org/wsdl/http/", typeof(System.Web.Services.Description.InputBinding))]
    public sealed partial class HttpUrlReplacementBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public HttpUrlReplacementBinding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Import : System.Web.Services.Description.DocumentableItem
    {
        public Import() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("location")]
        public string Location { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        public System.Web.Services.Description.ServiceDescription ServiceDescription { get { return default(System.Web.Services.Description.ServiceDescription); } }
    }
    public sealed partial class ImportCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal ImportCollection() { }
        public System.Web.Services.Description.Import this[int index] { get { return default(System.Web.Services.Description.Import); } set { } }
        public int Add(System.Web.Services.Description.Import import) { return default(int); }
        public bool Contains(System.Web.Services.Description.Import import) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.Import[] array, int index) { }
        public int IndexOf(System.Web.Services.Description.Import import) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.Import import) { }
        public void Remove(System.Web.Services.Description.Import import) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class InputBinding : System.Web.Services.Description.MessageBinding
    {
        public InputBinding() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Message : System.Web.Services.Description.NamedItem
    {
        public Message() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("part")]
        public System.Web.Services.Description.MessagePartCollection Parts { get { return default(System.Web.Services.Description.MessagePartCollection); } }
        public System.Web.Services.Description.ServiceDescription ServiceDescription { get { return default(System.Web.Services.Description.ServiceDescription); } }
        public System.Web.Services.Description.MessagePart FindPartByName(string partName) { return default(System.Web.Services.Description.MessagePart); }
        public System.Web.Services.Description.MessagePart[] FindPartsByName(string[] partNames) { return default(System.Web.Services.Description.MessagePart[]); }
    }
    public abstract partial class MessageBinding : System.Web.Services.Description.NamedItem
    {
        protected MessageBinding() { }
        public System.Web.Services.Description.OperationBinding OperationBinding { get { return default(System.Web.Services.Description.OperationBinding); } }
    }
    public sealed partial class MessageCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal MessageCollection() { }
        public System.Web.Services.Description.Message this[int index] { get { return default(System.Web.Services.Description.Message); } set { } }
        public System.Web.Services.Description.Message this[string name] { get { return default(System.Web.Services.Description.Message); } }
        public int Add(System.Web.Services.Description.Message message) { return default(int); }
        public bool Contains(System.Web.Services.Description.Message message) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.Message[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.Message message) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.Message message) { }
        public void Remove(System.Web.Services.Description.Message message) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class MessagePart : System.Web.Services.Description.NamedItem
    {
        public MessagePart() { }
        [System.Xml.Serialization.XmlAttributeAttribute("element")]
        public System.Xml.XmlQualifiedName Element { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        public System.Web.Services.Description.Message Message { get { return default(System.Web.Services.Description.Message); } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public System.Xml.XmlQualifiedName Type { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public sealed partial class MessagePartCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal MessagePartCollection() { }
        public System.Web.Services.Description.MessagePart this[int index] { get { return default(System.Web.Services.Description.MessagePart); } set { } }
        public System.Web.Services.Description.MessagePart this[string name] { get { return default(System.Web.Services.Description.MessagePart); } }
        public int Add(System.Web.Services.Description.MessagePart messagePart) { return default(int); }
        public bool Contains(System.Web.Services.Description.MessagePart messagePart) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.MessagePart[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.MessagePart messagePart) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.MessagePart messagePart) { }
        public void Remove(System.Web.Services.Description.MessagePart messagePart) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("content", "http://schemas.xmlsoap.org/wsdl/mime/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding))]
    [System.Web.Services.Configuration.XmlFormatExtensionPrefixAttribute("mime", "http://schemas.xmlsoap.org/wsdl/mime/")]
    public sealed partial class MimeContentBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public const string Namespace = "http://schemas.xmlsoap.org/wsdl/mime/";
        public MimeContentBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("part")]
        public string Part { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public string Type { get { return default(string); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("multipartRelated", "http://schemas.xmlsoap.org/wsdl/mime/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding))]
    public sealed partial class MimeMultipartRelatedBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public MimeMultipartRelatedBinding() { }
        [System.Xml.Serialization.XmlElementAttribute("part")]
        public System.Web.Services.Description.MimePartCollection Parts { get { return default(System.Web.Services.Description.MimePartCollection); } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class MimePart : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public MimePart() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    public sealed partial class MimePartCollection : System.Collections.CollectionBase
    {
        public MimePartCollection() { }
        public System.Web.Services.Description.MimePart this[int index] { get { return default(System.Web.Services.Description.MimePart); } set { } }
        public int Add(System.Web.Services.Description.MimePart mimePart) { return default(int); }
        public bool Contains(System.Web.Services.Description.MimePart mimePart) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.MimePart[] array, int index) { }
        public int IndexOf(System.Web.Services.Description.MimePart mimePart) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.MimePart mimePart) { }
        public void Remove(System.Web.Services.Description.MimePart mimePart) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("text", "http://microsoft.com/wsdl/mime/textMatching/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding), typeof(System.Web.Services.Description.MimePart))]
    [System.Web.Services.Configuration.XmlFormatExtensionPrefixAttribute("tm", "http://microsoft.com/wsdl/mime/textMatching/")]
    public sealed partial class MimeTextBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public const string Namespace = "http://microsoft.com/wsdl/mime/textMatching/";
        public MimeTextBinding() { }
        [System.Xml.Serialization.XmlElementAttribute("match", typeof(System.Web.Services.Description.MimeTextMatch))]
        public System.Web.Services.Description.MimeTextMatchCollection Matches { get { return default(System.Web.Services.Description.MimeTextMatchCollection); } }
    }
    public sealed partial class MimeTextMatch
    {
        public MimeTextMatch() { }
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.Xml.Serialization.XmlAttributeAttribute("capture")]
        public int Capture { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute(1)]
        [System.Xml.Serialization.XmlAttributeAttribute("group")]
        public int Group { get { return default(int); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("ignoreCase")]
        public bool IgnoreCase { get { return default(bool); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("match")]
        public System.Web.Services.Description.MimeTextMatchCollection Matches { get { return default(System.Web.Services.Description.MimeTextMatchCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("pattern")]
        public string Pattern { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public int Repeats { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute("1")]
        [System.Xml.Serialization.XmlAttributeAttribute("repeats")]
        public string RepeatsString { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public string Type { get { return default(string); } set { } }
    }
    public sealed partial class MimeTextMatchCollection : System.Collections.CollectionBase
    {
        public MimeTextMatchCollection() { }
        public System.Web.Services.Description.MimeTextMatch this[int index] { get { return default(System.Web.Services.Description.MimeTextMatch); } set { } }
        public int Add(System.Web.Services.Description.MimeTextMatch match) { return default(int); }
        public bool Contains(System.Web.Services.Description.MimeTextMatch match) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.MimeTextMatch[] array, int index) { }
        public int IndexOf(System.Web.Services.Description.MimeTextMatch match) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.MimeTextMatch match) { }
        public void Remove(System.Web.Services.Description.MimeTextMatch match) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("mimeXml", "http://schemas.xmlsoap.org/wsdl/mime/", typeof(System.Web.Services.Description.MimePart), typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding))]
    public sealed partial class MimeXmlBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public MimeXmlBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("part")]
        public string Part { get { return default(string); } set { } }
    }
    public abstract partial class NamedItem : System.Web.Services.Description.DocumentableItem
    {
        protected NamedItem() { }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Operation : System.Web.Services.Description.NamedItem
    {
        public Operation() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("fault")]
        public System.Web.Services.Description.OperationFaultCollection Faults { get { return default(System.Web.Services.Description.OperationFaultCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("input", typeof(System.Web.Services.Description.OperationInput))]
        [System.Xml.Serialization.XmlElementAttribute("output", typeof(System.Web.Services.Description.OperationOutput))]
        public System.Web.Services.Description.OperationMessageCollection Messages { get { return default(System.Web.Services.Description.OperationMessageCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string[] ParameterOrder { get { return default(string[]); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("parameterOrder")]
        public string ParameterOrderString { get { return default(string); } set { } }
        public System.Web.Services.Description.PortType PortType { get { return default(System.Web.Services.Description.PortType); } }
        public bool IsBoundBy(System.Web.Services.Description.OperationBinding operationBinding) { return default(bool); }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class OperationBinding : System.Web.Services.Description.NamedItem
    {
        public OperationBinding() { }
        public System.Web.Services.Description.Binding Binding { get { return default(System.Web.Services.Description.Binding); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("fault")]
        public System.Web.Services.Description.FaultBindingCollection Faults { get { return default(System.Web.Services.Description.FaultBindingCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("input")]
        public System.Web.Services.Description.InputBinding Input { get { return default(System.Web.Services.Description.InputBinding); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("output")]
        public System.Web.Services.Description.OutputBinding Output { get { return default(System.Web.Services.Description.OutputBinding); } set { } }
    }
    public sealed partial class OperationBindingCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal OperationBindingCollection() { }
        public System.Web.Services.Description.OperationBinding this[int index] { get { return default(System.Web.Services.Description.OperationBinding); } set { } }
        public int Add(System.Web.Services.Description.OperationBinding bindingOperation) { return default(int); }
        public bool Contains(System.Web.Services.Description.OperationBinding bindingOperation) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.OperationBinding[] array, int index) { }
        public int IndexOf(System.Web.Services.Description.OperationBinding bindingOperation) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.OperationBinding bindingOperation) { }
        public void Remove(System.Web.Services.Description.OperationBinding bindingOperation) { }
        protected override void SetParent(object value, object parent) { }
    }
    public sealed partial class OperationCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal OperationCollection() { }
        public System.Web.Services.Description.Operation this[int index] { get { return default(System.Web.Services.Description.Operation); } set { } }
        public int Add(System.Web.Services.Description.Operation operation) { return default(int); }
        public bool Contains(System.Web.Services.Description.Operation operation) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.Operation[] array, int index) { }
        public int IndexOf(System.Web.Services.Description.Operation operation) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.Operation operation) { }
        public void Remove(System.Web.Services.Description.Operation operation) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class OperationFault : System.Web.Services.Description.OperationMessage
    {
        public OperationFault() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    public sealed partial class OperationFaultCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal OperationFaultCollection() { }
        public System.Web.Services.Description.OperationFault this[int index] { get { return default(System.Web.Services.Description.OperationFault); } set { } }
        public System.Web.Services.Description.OperationFault this[string name] { get { return default(System.Web.Services.Description.OperationFault); } }
        public int Add(System.Web.Services.Description.OperationFault operationFaultMessage) { return default(int); }
        public bool Contains(System.Web.Services.Description.OperationFault operationFaultMessage) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.OperationFault[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.OperationFault operationFaultMessage) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.OperationFault operationFaultMessage) { }
        public void Remove(System.Web.Services.Description.OperationFault operationFaultMessage) { }
        protected override void SetParent(object value, object parent) { }
    }
    public enum OperationFlow
    {
        None = 0,
        Notification = 2,
        OneWay = 1,
        RequestResponse = 3,
        SolicitResponse = 4,
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class OperationInput : System.Web.Services.Description.OperationMessage
    {
        public OperationInput() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    public abstract partial class OperationMessage : System.Web.Services.Description.NamedItem
    {
        protected OperationMessage() { }
        [System.Xml.Serialization.XmlAttributeAttribute("message")]
        public System.Xml.XmlQualifiedName Message { get { return default(System.Xml.XmlQualifiedName); } set { } }
        public System.Web.Services.Description.Operation Operation { get { return default(System.Web.Services.Description.Operation); } }
    }
    public sealed partial class OperationMessageCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal OperationMessageCollection() { }
        public System.Web.Services.Description.OperationFlow Flow { get { return default(System.Web.Services.Description.OperationFlow); } }
        public System.Web.Services.Description.OperationInput Input { get { return default(System.Web.Services.Description.OperationInput); } }
        public System.Web.Services.Description.OperationMessage this[int index] { get { return default(System.Web.Services.Description.OperationMessage); } set { } }
        public System.Web.Services.Description.OperationOutput Output { get { return default(System.Web.Services.Description.OperationOutput); } }
        public int Add(System.Web.Services.Description.OperationMessage operationMessage) { return default(int); }
        public bool Contains(System.Web.Services.Description.OperationMessage operationMessage) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.OperationMessage[] array, int index) { }
        public int IndexOf(System.Web.Services.Description.OperationMessage operationMessage) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.OperationMessage operationMessage) { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.Web.Services.Description.OperationMessage operationMessage) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class OperationOutput : System.Web.Services.Description.OperationMessage
    {
        public OperationOutput() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class OutputBinding : System.Web.Services.Description.MessageBinding
    {
        public OutputBinding() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Port : System.Web.Services.Description.NamedItem
    {
        public Port() { }
        [System.Xml.Serialization.XmlAttributeAttribute("binding")]
        public System.Xml.XmlQualifiedName Binding { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        public System.Web.Services.Description.Service Service { get { return default(System.Web.Services.Description.Service); } }
    }
    public sealed partial class PortCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal PortCollection() { }
        public System.Web.Services.Description.Port this[int index] { get { return default(System.Web.Services.Description.Port); } set { } }
        public System.Web.Services.Description.Port this[string name] { get { return default(System.Web.Services.Description.Port); } }
        public int Add(System.Web.Services.Description.Port port) { return default(int); }
        public bool Contains(System.Web.Services.Description.Port port) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.Port[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.Port port) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.Port port) { }
        public void Remove(System.Web.Services.Description.Port port) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class PortType : System.Web.Services.Description.NamedItem
    {
        public PortType() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("operation")]
        public System.Web.Services.Description.OperationCollection Operations { get { return default(System.Web.Services.Description.OperationCollection); } }
        public System.Web.Services.Description.ServiceDescription ServiceDescription { get { return default(System.Web.Services.Description.ServiceDescription); } }
    }
    public sealed partial class PortTypeCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal PortTypeCollection() { }
        public System.Web.Services.Description.PortType this[int index] { get { return default(System.Web.Services.Description.PortType); } set { } }
        public System.Web.Services.Description.PortType this[string name] { get { return default(System.Web.Services.Description.PortType); } }
        public int Add(System.Web.Services.Description.PortType portType) { return default(int); }
        public bool Contains(System.Web.Services.Description.PortType portType) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.PortType[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.PortType portType) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.PortType portType) { }
        public void Remove(System.Web.Services.Description.PortType portType) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Service : System.Web.Services.Description.NamedItem
    {
        public Service() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("port")]
        public System.Web.Services.Description.PortCollection Ports { get { return default(System.Web.Services.Description.PortCollection); } }
        public System.Web.Services.Description.ServiceDescription ServiceDescription { get { return default(System.Web.Services.Description.ServiceDescription); } }
    }
    public sealed partial class ServiceCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        internal ServiceCollection() { }
        public System.Web.Services.Description.Service this[int index] { get { return default(System.Web.Services.Description.Service); } set { } }
        public System.Web.Services.Description.Service this[string name] { get { return default(System.Web.Services.Description.Service); } }
        public int Add(System.Web.Services.Description.Service service) { return default(int); }
        public bool Contains(System.Web.Services.Description.Service service) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.Service[] array, int index) { }
        protected override string GetKey(object value) { return default(string); }
        public int IndexOf(System.Web.Services.Description.Service service) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.Service service) { }
        public void Remove(System.Web.Services.Description.Service service) { }
        protected override void SetParent(object value, object parent) { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    [System.Xml.Serialization.XmlRootAttribute("definitions", Namespace="http://schemas.xmlsoap.org/wsdl/")]
    public sealed partial class ServiceDescription : System.Web.Services.Description.NamedItem
    {
        public const string Namespace = "http://schemas.xmlsoap.org/wsdl/";
        public ServiceDescription() { }
        [System.Xml.Serialization.XmlElementAttribute("binding")]
        public System.Web.Services.Description.BindingCollection Bindings { get { return default(System.Web.Services.Description.BindingCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("import")]
        public System.Web.Services.Description.ImportCollection Imports { get { return default(System.Web.Services.Description.ImportCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("message")]
        public System.Web.Services.Description.MessageCollection Messages { get { return default(System.Web.Services.Description.MessageCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("portType")]
        public System.Web.Services.Description.PortTypeCollection PortTypes { get { return default(System.Web.Services.Description.PortTypeCollection); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string RetrievalUrl { get { return default(string); } set { } }
        public static System.Xml.Schema.XmlSchema Schema { get { return default(System.Xml.Schema.XmlSchema); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public static System.Xml.Serialization.XmlSerializer Serializer { get { return default(System.Xml.Serialization.XmlSerializer); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Web.Services.Description.ServiceDescriptionCollection ServiceDescriptions { get { return default(System.Web.Services.Description.ServiceDescriptionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("service")]
        public System.Web.Services.Description.ServiceCollection Services { get { return default(System.Web.Services.Description.ServiceCollection); } }
        [System.Xml.Serialization.XmlAttributeAttribute("targetNamespace")]
        public string TargetNamespace { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("types")]
        public System.Web.Services.Description.Types Types { get { return default(System.Web.Services.Description.Types); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Collections.Specialized.StringCollection ValidationWarnings { get { return default(System.Collections.Specialized.StringCollection); } }
        public static bool CanRead(System.Xml.XmlReader reader) { return default(bool); }
        public static System.Web.Services.Description.ServiceDescription Read(System.IO.Stream stream) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(System.IO.Stream stream, bool validate) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(System.IO.TextReader textReader) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(System.IO.TextReader reader, bool validate) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(string fileName) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(string fileName, bool validate) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(System.Xml.XmlReader reader) { return default(System.Web.Services.Description.ServiceDescription); }
        public static System.Web.Services.Description.ServiceDescription Read(System.Xml.XmlReader reader, bool validate) { return default(System.Web.Services.Description.ServiceDescription); }
        public void Write(System.IO.Stream stream) { }
        public void Write(System.IO.TextWriter writer) { }
        public void Write(string fileName) { }
        public void Write(System.Xml.XmlWriter writer) { }
    }
    public abstract partial class ServiceDescriptionBaseCollection : System.Collections.CollectionBase
    {
        internal ServiceDescriptionBaseCollection() { }
        protected virtual System.Collections.IDictionary Table { get { return default(System.Collections.IDictionary); } }
        protected virtual string GetKey(object value) { return default(string); }
        protected override void OnClear() { }
        protected override void OnInsertComplete(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        protected virtual void SetParent(object value, object parent) { }
    }
    public sealed partial class ServiceDescriptionCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        public ServiceDescriptionCollection() { }
        public System.Web.Services.Description.ServiceDescription this[int index] { get { return default(System.Web.Services.Description.ServiceDescription); } set { } }
        public System.Web.Services.Description.ServiceDescription this[string ns] { get { return default(System.Web.Services.Description.ServiceDescription); } }
        public int Add(System.Web.Services.Description.ServiceDescription serviceDescription) { return default(int); }
        public bool Contains(System.Web.Services.Description.ServiceDescription serviceDescription) { return default(bool); }
        public void CopyTo(System.Web.Services.Description.ServiceDescription[] array, int index) { }
        public System.Web.Services.Description.Binding GetBinding(System.Xml.XmlQualifiedName name) { return default(System.Web.Services.Description.Binding); }
        protected override string GetKey(object value) { return default(string); }
        public System.Web.Services.Description.Message GetMessage(System.Xml.XmlQualifiedName name) { return default(System.Web.Services.Description.Message); }
        public System.Web.Services.Description.PortType GetPortType(System.Xml.XmlQualifiedName name) { return default(System.Web.Services.Description.PortType); }
        public System.Web.Services.Description.Service GetService(System.Xml.XmlQualifiedName name) { return default(System.Web.Services.Description.Service); }
        public int IndexOf(System.Web.Services.Description.ServiceDescription serviceDescription) { return default(int); }
        public void Insert(int index, System.Web.Services.Description.ServiceDescription serviceDescription) { }
        protected override void OnInsertComplete(int index, object item) { }
        public void Remove(System.Web.Services.Description.ServiceDescription serviceDescription) { }
        protected override void SetParent(object value, object parent) { }
    }
    public abstract partial class ServiceDescriptionFormatExtension
    {
        protected ServiceDescriptionFormatExtension() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool Handled { get { return default(bool); } set { } }
        public object Parent { get { return default(object); } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("required", Namespace="http://schemas.xmlsoap.org/wsdl/")]
        public bool Required { get { return default(bool); } set { } }
    }
    public sealed partial class ServiceDescriptionFormatExtensionCollection : System.Web.Services.Description.ServiceDescriptionBaseCollection
    {
        public ServiceDescriptionFormatExtensionCollection(object parent) { }
        public object this[int index] { get { return default(object); } set { } }
        public int Add(object extension) { return default(int); }
        public bool Contains(object extension) { return default(bool); }
        public void CopyTo(object[] array, int index) { }
        public System.Xml.XmlElement Find(string name, string ns) { return default(System.Xml.XmlElement); }
        public object Find(System.Type type) { return default(object); }
        public System.Xml.XmlElement[] FindAll(string name, string ns) { return default(System.Xml.XmlElement[]); }
        public object[] FindAll(System.Type type) { return default(object[]); }
        public int IndexOf(object extension) { return default(int); }
        public void Insert(int index, object extension) { }
        public bool IsHandled(object item) { return default(bool); }
        public bool IsRequired(object item) { return default(bool); }
        protected override void OnValidate(object value) { }
        public void Remove(object extension) { }
        protected override void SetParent(object value, object parent) { }
    }
    public partial class ServiceDescriptionImporter
    {
        public ServiceDescriptionImporter() { }
        public string ProtocolName { get { return default(string); } set { } }
        public System.Xml.Serialization.XmlSchemas Schemas { get { return default(System.Xml.Serialization.XmlSchemas); } }
        public System.Web.Services.Description.ServiceDescriptionCollection ServiceDescriptions { get { return default(System.Web.Services.Description.ServiceDescriptionCollection); } }
        public System.Web.Services.Description.ServiceDescriptionImportStyle Style { get { return default(System.Web.Services.Description.ServiceDescriptionImportStyle); } set { } }
        public void AddServiceDescription(System.Web.Services.Description.ServiceDescription serviceDescription, string appSettingUrlKey, string appSettingBaseUrl) { }
    }
    public enum ServiceDescriptionImportStyle
    {
        [System.Xml.Serialization.XmlEnumAttribute("client")]
        Client = 0,
        [System.Xml.Serialization.XmlEnumAttribute("server")]
        Server = 1,
        [System.Xml.Serialization.XmlEnumAttribute("serverInterface")]
        ServerInterface = 2,
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("address", "http://schemas.xmlsoap.org/wsdl/soap12/", typeof(System.Web.Services.Description.Port))]
    public sealed partial class Soap12AddressBinding : System.Web.Services.Description.SoapAddressBinding
    {
        public Soap12AddressBinding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("binding", "http://schemas.xmlsoap.org/wsdl/soap12/", typeof(System.Web.Services.Description.Binding))]
    [System.Web.Services.Configuration.XmlFormatExtensionPrefixAttribute("soap12", "http://schemas.xmlsoap.org/wsdl/soap12/")]
    public sealed partial class Soap12Binding : System.Web.Services.Description.SoapBinding
    {
        public new const string HttpTransport = "http://schemas.xmlsoap.org/soap/http";
        public new const string Namespace = "http://schemas.xmlsoap.org/wsdl/soap12/";
        public Soap12Binding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("body", "http://schemas.xmlsoap.org/wsdl/soap12/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding), typeof(System.Web.Services.Description.MimePart))]
    public sealed partial class Soap12BodyBinding : System.Web.Services.Description.SoapBodyBinding
    {
        public Soap12BodyBinding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("fault", "http://schemas.xmlsoap.org/wsdl/soap12/", typeof(System.Web.Services.Description.FaultBinding))]
    public sealed partial class Soap12FaultBinding : System.Web.Services.Description.SoapFaultBinding
    {
        public Soap12FaultBinding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("header", "http://schemas.xmlsoap.org/wsdl/soap12/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding))]
    public sealed partial class Soap12HeaderBinding : System.Web.Services.Description.SoapHeaderBinding
    {
        public Soap12HeaderBinding() { }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("operation", "http://schemas.xmlsoap.org/wsdl/soap12/", typeof(System.Web.Services.Description.OperationBinding))]
    public sealed partial class Soap12OperationBinding : System.Web.Services.Description.SoapOperationBinding
    {
        public Soap12OperationBinding() { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.Xml.Serialization.XmlAttributeAttribute("soapActionRequired")]
        public bool SoapActionRequired { get { return default(bool); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("address", "http://schemas.xmlsoap.org/wsdl/soap/", typeof(System.Web.Services.Description.Port))]
    public partial class SoapAddressBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public SoapAddressBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("location")]
        public string Location { get { return default(string); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("binding", "http://schemas.xmlsoap.org/wsdl/soap/", typeof(System.Web.Services.Description.Binding))]
    [System.Web.Services.Configuration.XmlFormatExtensionPrefixAttribute("soap", "http://schemas.xmlsoap.org/wsdl/soap/")]
    [System.Web.Services.Configuration.XmlFormatExtensionPrefixAttribute("soapenc", "http://schemas.xmlsoap.org/soap/encoding/")]
    public partial class SoapBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public const string HttpTransport = "http://schemas.xmlsoap.org/soap/http";
        public const string Namespace = "http://schemas.xmlsoap.org/wsdl/soap/";
        public SoapBinding() { }
        public static System.Xml.Schema.XmlSchema Schema { get { return default(System.Xml.Schema.XmlSchema); } }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Description.SoapBindingStyle)(1))]
        [System.Xml.Serialization.XmlAttributeAttribute("style")]
        public System.Web.Services.Description.SoapBindingStyle Style { get { return default(System.Web.Services.Description.SoapBindingStyle); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("transport")]
        public string Transport { get { return default(string); } set { } }
    }
    public enum SoapBindingStyle
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        Default = 0,
        [System.Xml.Serialization.XmlEnumAttribute("document")]
        Document = 1,
        [System.Xml.Serialization.XmlEnumAttribute("rpc")]
        Rpc = 2,
    }
    public enum SoapBindingUse
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        Default = 0,
        [System.Xml.Serialization.XmlEnumAttribute("encoded")]
        Encoded = 1,
        [System.Xml.Serialization.XmlEnumAttribute("literal")]
        Literal = 2,
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("body", "http://schemas.xmlsoap.org/wsdl/soap/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding), typeof(System.Web.Services.Description.MimePart))]
    public partial class SoapBodyBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public SoapBodyBinding() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("encodingStyle")]
        public string Encoding { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public string[] Parts { get { return default(string[]); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("parts")]
        public string PartsString { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Description.SoapBindingUse)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("use")]
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("fault", "http://schemas.xmlsoap.org/wsdl/soap/", typeof(System.Web.Services.Description.FaultBinding))]
    public partial class SoapFaultBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public SoapFaultBinding() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("encodingStyle")]
        public string Encoding { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Description.SoapBindingUse)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("use")]
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("header", "http://schemas.xmlsoap.org/wsdl/soap/", typeof(System.Web.Services.Description.InputBinding), typeof(System.Web.Services.Description.OutputBinding))]
    public partial class SoapHeaderBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public SoapHeaderBinding() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("encodingStyle")]
        public string Encoding { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlElementAttribute("headerfault")]
        public System.Web.Services.Description.SoapHeaderFaultBinding Fault { get { return default(System.Web.Services.Description.SoapHeaderFaultBinding); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool MapToProperty { get { return default(bool); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("message")]
        public System.Xml.XmlQualifiedName Message { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("part")]
        public string Part { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Description.SoapBindingUse)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("use")]
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    public partial class SoapHeaderFaultBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public SoapHeaderFaultBinding() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("encodingStyle")]
        public string Encoding { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("message")]
        public System.Xml.XmlQualifiedName Message { get { return default(System.Xml.XmlQualifiedName); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.XmlAttributeAttribute("namespace")]
        public string Namespace { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("part")]
        public string Part { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Description.SoapBindingUse)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("use")]
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionAttribute("operation", "http://schemas.xmlsoap.org/wsdl/soap/", typeof(System.Web.Services.Description.OperationBinding))]
    public partial class SoapOperationBinding : System.Web.Services.Description.ServiceDescriptionFormatExtension
    {
        public SoapOperationBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("soapAction")]
        public string SoapAction { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Description.SoapBindingStyle)(0))]
        [System.Xml.Serialization.XmlAttributeAttribute("style")]
        public System.Web.Services.Description.SoapBindingStyle Style { get { return default(System.Web.Services.Description.SoapBindingStyle); } set { } }
    }
    [System.Web.Services.Configuration.XmlFormatExtensionPointAttribute("Extensions")]
    public sealed partial class Types : System.Web.Services.Description.DocumentableItem
    {
        public Types() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection Extensions { get { return default(System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection); } }
        [System.Xml.Serialization.XmlElementAttribute("schema", typeof(System.Xml.Schema.XmlSchema), Namespace="http://www.w3.org/2001/XMLSchema")]
        public System.Xml.Serialization.XmlSchemas Schemas { get { return default(System.Xml.Serialization.XmlSchemas); } }
    }
    public sealed partial class WebReference
    {
        public WebReference() { }
        public string AppSettingBaseUrl { get { return default(string); } }
        public string AppSettingUrlKey { get { return default(string); } }
        public System.Web.Services.Discovery.DiscoveryClientDocumentCollection Documents { get { return default(System.Web.Services.Discovery.DiscoveryClientDocumentCollection); } }
        public string ProtocolName { get { return default(string); } set { } }
        public System.Collections.Specialized.StringCollection ValidationWarnings { get { return default(System.Collections.Specialized.StringCollection); } }
    }
}
namespace System.Web.Services.Discovery
{
    [System.Xml.Serialization.XmlRootAttribute("contractRef", Namespace="http://schemas.xmlsoap.org/disco/scl/", IsNullable=true)]
    public partial class ContractReference : System.Web.Services.Discovery.DiscoveryReference
    {
        public const string Namespace = "http://schemas.xmlsoap.org/disco/scl/";
        public ContractReference() { }
        public ContractReference(string href) { }
        public ContractReference(string href, string docRef) { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Web.Services.Description.ServiceDescription Contract { get { return default(System.Web.Services.Description.ServiceDescription); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override string DefaultFilename { get { return default(string); } }
        [System.Xml.Serialization.XmlAttributeAttribute("docRef")]
        public string DocRef { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public string Ref { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override string Url { get { return default(string); } set { } }
        public override object ReadDocument(System.IO.Stream stream) { return default(object); }
        protected internal override void Resolve(string contentType, System.IO.Stream stream) { }
        public override void WriteDocument(object document, System.IO.Stream stream) { }
    }
    public sealed partial class ContractSearchPattern : System.Web.Services.Discovery.DiscoverySearchPattern
    {
        public ContractSearchPattern() { }
        public override string Pattern { get { return default(string); } }
        public override System.Web.Services.Discovery.DiscoveryReference GetDiscoveryReference(string filename) { return default(System.Web.Services.Discovery.DiscoveryReference); }
    }
    public sealed partial class DiscoveryClientDocumentCollection : System.Collections.DictionaryBase
    {
        public DiscoveryClientDocumentCollection() { }
        public object this[string url] { get { return default(object); } set { } }
        public System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(string url, object value) { }
        public bool Contains(string url) { return default(bool); }
        public void Remove(string url) { }
    }
    public partial class DiscoveryClientProtocol : System.Web.Services.Protocols.HttpWebClientProtocol
    {
        public DiscoveryClientProtocol() { }
        public System.Collections.IList AdditionalInformation { get { return default(System.Collections.IList); } }
        public System.Web.Services.Discovery.DiscoveryClientDocumentCollection Documents { get { return default(System.Web.Services.Discovery.DiscoveryClientDocumentCollection); } }
        public System.Web.Services.Discovery.DiscoveryExceptionDictionary Errors { get { return default(System.Web.Services.Discovery.DiscoveryExceptionDictionary); } }
        public System.Web.Services.Discovery.DiscoveryClientReferenceCollection References { get { return default(System.Web.Services.Discovery.DiscoveryClientReferenceCollection); } }
        public System.Web.Services.Discovery.DiscoveryDocument Discover(string url) { return default(System.Web.Services.Discovery.DiscoveryDocument); }
        public System.Web.Services.Discovery.DiscoveryDocument DiscoverAny(string url) { return default(System.Web.Services.Discovery.DiscoveryDocument); }
        public System.IO.Stream Download(ref string url) { return default(System.IO.Stream); }
        public System.IO.Stream Download(ref string url, ref string contentType) { return default(System.IO.Stream); }
        [System.ObsoleteAttribute("This method will be removed from a future version. The method call is no longer required for resource discovery", false)]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void LoadExternals() { }
        public System.Web.Services.Discovery.DiscoveryClientResultCollection ReadAll(string topLevelFilename) { return default(System.Web.Services.Discovery.DiscoveryClientResultCollection); }
        public void ResolveAll() { }
        public void ResolveOneLevel() { }
        public System.Web.Services.Discovery.DiscoveryClientResultCollection WriteAll(string directory, string topLevelFilename) { return default(System.Web.Services.Discovery.DiscoveryClientResultCollection); }
        public sealed partial class DiscoveryClientResultsFile
        {
            public DiscoveryClientResultsFile() { }
            public System.Web.Services.Discovery.DiscoveryClientResultCollection Results { get { return default(System.Web.Services.Discovery.DiscoveryClientResultCollection); } }
        }
    }
    public sealed partial class DiscoveryClientReferenceCollection : System.Collections.DictionaryBase
    {
        public DiscoveryClientReferenceCollection() { }
        public System.Web.Services.Discovery.DiscoveryReference this[string url] { get { return default(System.Web.Services.Discovery.DiscoveryReference); } set { } }
        public System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(string url, System.Web.Services.Discovery.DiscoveryReference value) { }
        public void Add(System.Web.Services.Discovery.DiscoveryReference value) { }
        public bool Contains(string url) { return default(bool); }
        public void Remove(string url) { }
    }
    public sealed partial class DiscoveryClientResult
    {
        public DiscoveryClientResult() { }
        public DiscoveryClientResult(System.Type referenceType, string url, string filename) { }
        [System.Xml.Serialization.XmlAttributeAttribute("filename")]
        public string Filename { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("referenceType")]
        public string ReferenceTypeName { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("url")]
        public string Url { get { return default(string); } set { } }
    }
    public sealed partial class DiscoveryClientResultCollection : System.Collections.CollectionBase
    {
        public DiscoveryClientResultCollection() { }
        public System.Web.Services.Discovery.DiscoveryClientResult this[int i] { get { return default(System.Web.Services.Discovery.DiscoveryClientResult); } set { } }
        public int Add(System.Web.Services.Discovery.DiscoveryClientResult value) { return default(int); }
        public bool Contains(System.Web.Services.Discovery.DiscoveryClientResult value) { return default(bool); }
        public void Remove(System.Web.Services.Discovery.DiscoveryClientResult value) { }
    }
    [System.Xml.Serialization.XmlRootAttribute("discovery", Namespace="http://schemas.xmlsoap.org/disco/")]
    public sealed partial class DiscoveryDocument
    {
        public const string Namespace = "http://schemas.xmlsoap.org/disco/";
        public DiscoveryDocument() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Collections.IList References { get { return default(System.Collections.IList); } }
        public static bool CanRead(System.Xml.XmlReader xmlReader) { return default(bool); }
        public static System.Web.Services.Discovery.DiscoveryDocument Read(System.IO.Stream stream) { return default(System.Web.Services.Discovery.DiscoveryDocument); }
        public static System.Web.Services.Discovery.DiscoveryDocument Read(System.IO.TextReader textReader) { return default(System.Web.Services.Discovery.DiscoveryDocument); }
        public static System.Web.Services.Discovery.DiscoveryDocument Read(System.Xml.XmlReader xmlReader) { return default(System.Web.Services.Discovery.DiscoveryDocument); }
        public void Write(System.IO.Stream stream) { }
        public void Write(System.IO.TextWriter textWriter) { }
        public void Write(System.Xml.XmlWriter xmlWriter) { }
    }
    public partial class DiscoveryDocumentLinksPattern : System.Web.Services.Discovery.DiscoverySearchPattern
    {
        public DiscoveryDocumentLinksPattern() { }
        public override string Pattern { get { return default(string); } }
        public override System.Web.Services.Discovery.DiscoveryReference GetDiscoveryReference(string filename) { return default(System.Web.Services.Discovery.DiscoveryReference); }
    }
    [System.Xml.Serialization.XmlRootAttribute("discoveryRef", Namespace="http://schemas.xmlsoap.org/disco/", IsNullable=true)]
    public sealed partial class DiscoveryDocumentReference : System.Web.Services.Discovery.DiscoveryReference
    {
        public DiscoveryDocumentReference() { }
        public DiscoveryDocumentReference(string href) { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override string DefaultFilename { get { return default(string); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Web.Services.Discovery.DiscoveryDocument Document { get { return default(System.Web.Services.Discovery.DiscoveryDocument); } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public string Ref { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override string Url { get { return default(string); } set { } }
        public override object ReadDocument(System.IO.Stream stream) { return default(object); }
        protected internal override void Resolve(string contentType, System.IO.Stream stream) { }
        public void ResolveAll() { }
        public override void WriteDocument(object document, System.IO.Stream stream) { }
    }
    public sealed partial class DiscoveryDocumentSearchPattern : System.Web.Services.Discovery.DiscoverySearchPattern
    {
        public DiscoveryDocumentSearchPattern() { }
        public override string Pattern { get { return default(string); } }
        public override System.Web.Services.Discovery.DiscoveryReference GetDiscoveryReference(string filename) { return default(System.Web.Services.Discovery.DiscoveryReference); }
    }
    public sealed partial class DiscoveryExceptionDictionary : System.Collections.DictionaryBase
    {
        public DiscoveryExceptionDictionary() { }
        public System.Exception this[string url] { get { return default(System.Exception); } set { } }
        public System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(string url, System.Exception value) { }
        public bool Contains(string url) { return default(bool); }
        public void Remove(string url) { }
    }
    public abstract partial class DiscoveryReference
    {
        protected DiscoveryReference() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Web.Services.Discovery.DiscoveryClientProtocol ClientProtocol { get { return default(System.Web.Services.Discovery.DiscoveryClientProtocol); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public virtual string DefaultFilename { get { return default(string); } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public abstract string Url { get; set; }
        public static string FilenameFromUrl(string url) { return default(string); }
        public abstract object ReadDocument(System.IO.Stream stream);
        public void Resolve() { }
        protected internal abstract void Resolve(string contentType, System.IO.Stream stream);
        public abstract void WriteDocument(object document, System.IO.Stream stream);
    }
    public sealed partial class DiscoveryReferenceCollection : System.Collections.CollectionBase
    {
        public DiscoveryReferenceCollection() { }
        public System.Web.Services.Discovery.DiscoveryReference this[int i] { get { return default(System.Web.Services.Discovery.DiscoveryReference); } set { } }
        public int Add(System.Web.Services.Discovery.DiscoveryReference value) { return default(int); }
        public bool Contains(System.Web.Services.Discovery.DiscoveryReference value) { return default(bool); }
        public void Remove(System.Web.Services.Discovery.DiscoveryReference value) { }
    }
    public abstract partial class DiscoverySearchPattern
    {
        protected DiscoverySearchPattern() { }
        public abstract string Pattern { get; }
        public abstract System.Web.Services.Discovery.DiscoveryReference GetDiscoveryReference(string filename);
    }
    [System.Xml.Serialization.XmlRootAttribute("dynamicDiscovery", Namespace="urn:schemas-dynamicdiscovery:disco.2000-03-17", IsNullable=true)]
    public sealed partial class DynamicDiscoveryDocument
    {
        public const string Namespace = "urn:schemas-dynamicdiscovery:disco.2000-03-17";
        public DynamicDiscoveryDocument() { }
        [System.Xml.Serialization.XmlElementAttribute("exclude", typeof(System.Web.Services.Discovery.ExcludePathInfo))]
        public System.Web.Services.Discovery.ExcludePathInfo[] ExcludePaths { get { return default(System.Web.Services.Discovery.ExcludePathInfo[]); } set { } }
        public static System.Web.Services.Discovery.DynamicDiscoveryDocument Load(System.IO.Stream stream) { return default(System.Web.Services.Discovery.DynamicDiscoveryDocument); }
        public void Write(System.IO.Stream stream) { }
    }
    public sealed partial class ExcludePathInfo
    {
        public ExcludePathInfo() { }
        public ExcludePathInfo(string path) { }
        [System.Xml.Serialization.XmlAttributeAttribute("path")]
        public string Path { get { return default(string); } set { } }
    }
    [System.Xml.Serialization.XmlRootAttribute("schemaRef", Namespace="http://schemas.xmlsoap.org/disco/schema/", IsNullable=true)]
    public sealed partial class SchemaReference : System.Web.Services.Discovery.DiscoveryReference
    {
        public const string Namespace = "http://schemas.xmlsoap.org/disco/schema/";
        public SchemaReference() { }
        public SchemaReference(string href) { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override string DefaultFilename { get { return default(string); } }
        [System.Xml.Serialization.XmlAttributeAttribute("ref")]
        public string Ref { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.Schema.XmlSchema Schema { get { return default(System.Xml.Schema.XmlSchema); } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Xml.Serialization.XmlAttributeAttribute("targetNamespace")]
        public string TargetNamespace { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public override string Url { get { return default(string); } set { } }
        public override object ReadDocument(System.IO.Stream stream) { return default(object); }
        protected internal override void Resolve(string contentType, System.IO.Stream stream) { }
        public override void WriteDocument(object document, System.IO.Stream stream) { }
    }
    [System.Xml.Serialization.XmlRootAttribute("soap", Namespace="http://schemas.xmlsoap.org/disco/soap/", IsNullable=true)]
    public sealed partial class SoapBinding
    {
        public const string Namespace = "http://schemas.xmlsoap.org/disco/soap/";
        public SoapBinding() { }
        [System.Xml.Serialization.XmlAttributeAttribute("address")]
        public string Address { get { return default(string); } set { } }
        [System.Xml.Serialization.XmlAttributeAttribute("binding")]
        public System.Xml.XmlQualifiedName Binding { get { return default(System.Xml.XmlQualifiedName); } set { } }
    }
    public sealed partial class XmlSchemaSearchPattern : System.Web.Services.Discovery.DiscoverySearchPattern
    {
        public XmlSchemaSearchPattern() { }
        public override string Pattern { get { return default(string); } }
        public override System.Web.Services.Discovery.DiscoveryReference GetDiscoveryReference(string filename) { return default(System.Web.Services.Discovery.DiscoveryReference); }
    }
}
namespace System.Web.Services.Protocols
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=true)]
    public sealed partial class HttpMethodAttribute : System.Attribute
    {
        public HttpMethodAttribute() { }
        public HttpMethodAttribute(System.Type returnFormatter, System.Type parameterFormatter) { }
        public System.Type ParameterFormatter { get { return default(System.Type); } set { } }
        public System.Type ReturnFormatter { get { return default(System.Type); } set { } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class HttpWebClientProtocol : System.Web.Services.Protocols.WebClientProtocol
    {
        protected HttpWebClientProtocol() { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool AllowAutoRedirect { get { return default(bool); } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { return default(System.Security.Cryptography.X509Certificates.X509CertificateCollection); } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.Net.CookieContainer CookieContainer { get { return default(System.Net.CookieContainer); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool EnableDecompression { get { return default(bool); } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public System.Net.IWebProxy Proxy { get { return default(System.Net.IWebProxy); } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public bool UnsafeAuthenticatedConnectionSharing { get { return default(bool); } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public string UserAgent { get { return default(string); } set { } }
        protected void CancelAsync(object userState) { }
        public static bool GenerateXmlMappings(System.Type type, System.Collections.ArrayList mapping) { return default(bool); }
        public static System.Collections.Hashtable GenerateXmlMappings(System.Type[] types, System.Collections.ArrayList mapping) { return default(System.Collections.Hashtable); }
        protected override System.Net.WebRequest GetWebRequest(System.Uri uri) { return default(System.Net.WebRequest); }
        protected override System.Net.WebResponse GetWebResponse(System.Net.WebRequest request) { return default(System.Net.WebResponse); }
        protected override System.Net.WebResponse GetWebResponse(System.Net.WebRequest request, System.IAsyncResult result) { return default(System.Net.WebResponse); }
    }
    public partial class InvokeCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal InvokeCompletedEventArgs() { }
        public object[] Results { get { return default(object[]); } }
    }
    public delegate void InvokeCompletedEventHandler(object sender, System.Web.Services.Protocols.InvokeCompletedEventArgs e);
    public sealed partial class LogicalMethodInfo
    {
        public LogicalMethodInfo(System.Reflection.MethodInfo method_info) { }
        public System.Reflection.ParameterInfo AsyncCallbackParameter { get { return default(System.Reflection.ParameterInfo); } }
        public System.Reflection.ParameterInfo AsyncResultParameter { get { return default(System.Reflection.ParameterInfo); } }
        public System.Reflection.ParameterInfo AsyncStateParameter { get { return default(System.Reflection.ParameterInfo); } }
        public System.Reflection.MethodInfo BeginMethodInfo { get { return default(System.Reflection.MethodInfo); } }
        public System.Reflection.ICustomAttributeProvider CustomAttributeProvider { get { return default(System.Reflection.ICustomAttributeProvider); } }
        public System.Type DeclaringType { get { return default(System.Type); } }
        public System.Reflection.MethodInfo EndMethodInfo { get { return default(System.Reflection.MethodInfo); } }
        public System.Reflection.ParameterInfo[] InParameters { get { return default(System.Reflection.ParameterInfo[]); } }
        public bool IsAsync { get { return default(bool); } }
        public bool IsVoid { get { return default(bool); } }
        public System.Reflection.MethodInfo MethodInfo { get { return default(System.Reflection.MethodInfo); } }
        public string Name { get { return default(string); } }
        public System.Reflection.ParameterInfo[] OutParameters { get { return default(System.Reflection.ParameterInfo[]); } }
        public System.Reflection.ParameterInfo[] Parameters { get { return default(System.Reflection.ParameterInfo[]); } }
        public System.Type ReturnType { get { return default(System.Type); } }
        public System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributeProvider { get { return default(System.Reflection.ICustomAttributeProvider); } }
        public System.IAsyncResult BeginInvoke(object target, object[] values, System.AsyncCallback callback, object asyncState) { return default(System.IAsyncResult); }
        public static System.Web.Services.Protocols.LogicalMethodInfo[] Create(System.Reflection.MethodInfo[] method_infos) { return default(System.Web.Services.Protocols.LogicalMethodInfo[]); }
        public static System.Web.Services.Protocols.LogicalMethodInfo[] Create(System.Reflection.MethodInfo[] method_infos, System.Web.Services.Protocols.LogicalMethodTypes types) { return default(System.Web.Services.Protocols.LogicalMethodInfo[]); }
        public object[] EndInvoke(object target, System.IAsyncResult asyncResult) { return default(object[]); }
        public object GetCustomAttribute(System.Type type) { return default(object); }
        public object[] GetCustomAttributes(System.Type type) { return default(object[]); }
        public object[] Invoke(object target, object[] values) { return default(object[]); }
        public static bool IsBeginMethod(System.Reflection.MethodInfo method_info) { return default(bool); }
        public static bool IsEndMethod(System.Reflection.MethodInfo method_info) { return default(bool); }
        public override string ToString() { return default(string); }
    }
    public enum LogicalMethodTypes
    {
        Async = 2,
        Sync = 1,
    }
    public abstract partial class MimeFormatter
    {
        protected MimeFormatter() { }
        public static System.Web.Services.Protocols.MimeFormatter CreateInstance(System.Type type, object initializer) { return default(System.Web.Services.Protocols.MimeFormatter); }
        public static object GetInitializer(System.Type type, System.Web.Services.Protocols.LogicalMethodInfo methodInfo) { return default(object); }
        public abstract object GetInitializer(System.Web.Services.Protocols.LogicalMethodInfo methodInfo);
        public static object[] GetInitializers(System.Type type, System.Web.Services.Protocols.LogicalMethodInfo[] methodInfos) { return default(object[]); }
        public virtual object[] GetInitializers(System.Web.Services.Protocols.LogicalMethodInfo[] methodInfos) { return default(object[]); }
        public abstract void Initialize(object initializer);
    }
    public abstract partial class MimeParameterWriter : System.Web.Services.Protocols.MimeFormatter
    {
        protected MimeParameterWriter() { }
        public virtual System.Text.Encoding RequestEncoding { get { return default(System.Text.Encoding); } set { } }
        public virtual bool UsesWriteRequest { get { return default(bool); } }
        public virtual string GetRequestUrl(string url, object[] parameters) { return default(string); }
        public virtual void InitializeRequest(System.Net.WebRequest request, object[] values) { }
        public virtual void WriteRequest(System.IO.Stream requestStream, object[] values) { }
    }
    public abstract partial class MimeReturnReader : System.Web.Services.Protocols.MimeFormatter
    {
        protected MimeReturnReader() { }
        public abstract object Read(System.Net.WebResponse response, System.IO.Stream responseStream);
    }
    public partial class NopReturnReader : System.Web.Services.Protocols.MimeReturnReader
    {
        public NopReturnReader() { }
        public override object GetInitializer(System.Web.Services.Protocols.LogicalMethodInfo methodInfo) { return default(object); }
        public override void Initialize(object initializer) { }
        public override object Read(System.Net.WebResponse response, System.IO.Stream responseStream) { return default(object); }
    }
    public partial class ServerType
    {
        public ServerType(System.Type type) { }
    }
    public sealed partial class Soap12FaultCodes
    {
        internal Soap12FaultCodes() { }
        public static readonly System.Xml.XmlQualifiedName DataEncodingUnknownFaultCode;
        public static readonly System.Xml.XmlQualifiedName EncodingMissingIdFaultCode;
        public static readonly System.Xml.XmlQualifiedName EncodingUntypedValueFaultCode;
        public static readonly System.Xml.XmlQualifiedName MustUnderstandFaultCode;
        public static readonly System.Xml.XmlQualifiedName ReceiverFaultCode;
        public static readonly System.Xml.XmlQualifiedName RpcBadArgumentsFaultCode;
        public static readonly System.Xml.XmlQualifiedName RpcProcedureNotPresentFaultCode;
        public static readonly System.Xml.XmlQualifiedName SenderFaultCode;
        public static readonly System.Xml.XmlQualifiedName VersionMismatchFaultCode;
    }
    public sealed partial class SoapClientMessage : System.Web.Services.Protocols.SoapMessage
    {
        internal SoapClientMessage() { }
        public override string Action { get { return default(string); } }
        public System.Web.Services.Protocols.SoapHttpClientProtocol Client { get { return default(System.Web.Services.Protocols.SoapHttpClientProtocol); } }
        public override System.Web.Services.Protocols.LogicalMethodInfo MethodInfo { get { return default(System.Web.Services.Protocols.LogicalMethodInfo); } }
        public override bool OneWay { get { return default(bool); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public override System.Web.Services.Protocols.SoapProtocolVersion SoapVersion { get { return default(System.Web.Services.Protocols.SoapProtocolVersion); } }
        public override string Url { get { return default(string); } }
        protected override void EnsureInStage() { }
        protected override void EnsureOutStage() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=true)]
    public sealed partial class SoapDocumentMethodAttribute : System.Attribute
    {
        public SoapDocumentMethodAttribute() { }
        public SoapDocumentMethodAttribute(string action) { }
        public string Action { get { return default(string); } set { } }
        public string Binding { get { return default(string); } set { } }
        public bool OneWay { get { return default(bool); } set { } }
        public System.Web.Services.Protocols.SoapParameterStyle ParameterStyle { get { return default(System.Web.Services.Protocols.SoapParameterStyle); } set { } }
        public string RequestElementName { get { return default(string); } set { } }
        public string RequestNamespace { get { return default(string); } set { } }
        public string ResponseElementName { get { return default(string); } set { } }
        public string ResponseNamespace { get { return default(string); } set { } }
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true)]
    public sealed partial class SoapDocumentServiceAttribute : System.Attribute
    {
        public SoapDocumentServiceAttribute() { }
        public SoapDocumentServiceAttribute(System.Web.Services.Description.SoapBindingUse use) { }
        public SoapDocumentServiceAttribute(System.Web.Services.Description.SoapBindingUse use, System.Web.Services.Protocols.SoapParameterStyle paramStyle) { }
        public System.Web.Services.Protocols.SoapParameterStyle ParameterStyle { get { return default(System.Web.Services.Protocols.SoapParameterStyle); } set { } }
        public System.Web.Services.Protocols.SoapServiceRoutingStyle RoutingStyle { get { return default(System.Web.Services.Protocols.SoapServiceRoutingStyle); } set { } }
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    public partial class SoapException : System.SystemException
    {
        public static readonly System.Xml.XmlQualifiedName ClientFaultCode;
        public static readonly System.Xml.XmlQualifiedName DetailElementName;
        public static readonly System.Xml.XmlQualifiedName MustUnderstandFaultCode;
        public static readonly System.Xml.XmlQualifiedName ServerFaultCode;
        public static readonly System.Xml.XmlQualifiedName VersionMismatchFaultCode;
        public SoapException() { }
        protected SoapException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, System.Exception innerException) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, string actor) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, string actor, System.Exception innerException) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, string actor, string role, string lang, System.Xml.XmlNode detail, System.Web.Services.Protocols.SoapFaultSubCode subcode, System.Exception innerException) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, string actor, string role, System.Xml.XmlNode detail, System.Web.Services.Protocols.SoapFaultSubCode subcode, System.Exception innerException) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, string actor, System.Xml.XmlNode detail) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, string actor, System.Xml.XmlNode detail, System.Exception innerException) { }
        public SoapException(string message, System.Xml.XmlQualifiedName code, System.Web.Services.Protocols.SoapFaultSubCode subcode) { }
        public string Actor { get { return default(string); } }
        public System.Xml.XmlQualifiedName Code { get { return default(System.Xml.XmlQualifiedName); } }
        public System.Xml.XmlNode Detail { get { return default(System.Xml.XmlNode); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string Lang { get { return default(string); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string Node { get { return default(string); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string Role { get { return default(string); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Web.Services.Protocols.SoapFaultSubCode SubCode { get { return default(System.Web.Services.Protocols.SoapFaultSubCode); } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public static bool IsClientFaultCode(System.Xml.XmlQualifiedName code) { return default(bool); }
        public static bool IsMustUnderstandFaultCode(System.Xml.XmlQualifiedName code) { return default(bool); }
        public static bool IsServerFaultCode(System.Xml.XmlQualifiedName code) { return default(bool); }
        public static bool IsVersionMismatchFaultCode(System.Xml.XmlQualifiedName code) { return default(bool); }
    }
    public abstract partial class SoapExtension
    {
        protected SoapExtension() { }
        public virtual System.IO.Stream ChainStream(System.IO.Stream stream) { return default(System.IO.Stream); }
        public abstract object GetInitializer(System.Type serviceType);
        public abstract object GetInitializer(System.Web.Services.Protocols.LogicalMethodInfo methodInfo, System.Web.Services.Protocols.SoapExtensionAttribute attribute);
        public abstract void Initialize(object initializer);
        public abstract void ProcessMessage(System.Web.Services.Protocols.SoapMessage message);
    }
    public abstract partial class SoapExtensionAttribute : System.Attribute
    {
        protected SoapExtensionAttribute() { }
        public abstract System.Type ExtensionType { get; }
        public abstract int Priority { get; set; }
    }
    public partial class SoapFaultSubCode
    {
        public SoapFaultSubCode(System.Xml.XmlQualifiedName code) { }
        public SoapFaultSubCode(System.Xml.XmlQualifiedName code, System.Web.Services.Protocols.SoapFaultSubCode subcode) { }
        public System.Xml.XmlQualifiedName Code { get { return default(System.Xml.XmlQualifiedName); } }
        public System.Web.Services.Protocols.SoapFaultSubCode SubCode { get { return default(System.Web.Services.Protocols.SoapFaultSubCode); } }
    }
    [System.Xml.Serialization.SoapTypeAttribute(IncludeInSchema=false)]
    [System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema=false)]
    public abstract partial class SoapHeader
    {
        protected SoapHeader() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Xml.Serialization.SoapAttributeAttribute("actor", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
        [System.Xml.Serialization.XmlAttributeAttribute("actor", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
        public string Actor { get { return default(string); } set { } }
        [System.Xml.Serialization.SoapIgnoreAttribute]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool DidUnderstand { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute("0")]
        [System.Xml.Serialization.SoapAttributeAttribute("mustUnderstand", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
        [System.Xml.Serialization.XmlAttributeAttribute("mustUnderstand", Namespace="http://schemas.xmlsoap.org/soap/envelope/")]
        public string EncodedMustUnderstand { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute("0")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Xml.Serialization.SoapAttributeAttribute("mustUnderstand", Namespace="http://www.w3.org/2003/05/soap-envelope")]
        [System.Xml.Serialization.XmlAttributeAttribute("mustUnderstand", Namespace="http://www.w3.org/2003/05/soap-envelope")]
        public string EncodedMustUnderstand12 { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute("0")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Xml.Serialization.SoapAttributeAttribute("relay", Namespace="http://www.w3.org/2003/05/soap-envelope")]
        [System.Xml.Serialization.XmlAttributeAttribute("relay", Namespace="http://www.w3.org/2003/05/soap-envelope")]
        public string EncodedRelay { get { return default(string); } set { } }
        [System.Xml.Serialization.SoapIgnoreAttribute]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool MustUnderstand { get { return default(bool); } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Xml.Serialization.SoapIgnoreAttribute]
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public bool Relay { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Xml.Serialization.SoapAttributeAttribute("role", Namespace="http://www.w3.org/2003/05/soap-envelope")]
        [System.Xml.Serialization.XmlAttributeAttribute("role", Namespace="http://www.w3.org/2003/05/soap-envelope")]
        public string Role { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), AllowMultiple=true, Inherited=true)]
    public sealed partial class SoapHeaderAttribute : System.Attribute
    {
        public SoapHeaderAttribute(string memberName) { }
        public System.Web.Services.Protocols.SoapHeaderDirection Direction { get { return default(System.Web.Services.Protocols.SoapHeaderDirection); } set { } }
        public string MemberName { get { return default(string); } set { } }
        [System.ObsoleteAttribute("This property will be removed from a future version. The presence of a particular header in a SOAP message is no longer enforced", false)]
        public bool Required { get { return default(bool); } set { } }
    }
    public partial class SoapHeaderCollection : System.Collections.CollectionBase
    {
        public SoapHeaderCollection() { }
        public System.Web.Services.Protocols.SoapHeader this[int index] { get { return default(System.Web.Services.Protocols.SoapHeader); } set { } }
        public int Add(System.Web.Services.Protocols.SoapHeader header) { return default(int); }
        public bool Contains(System.Web.Services.Protocols.SoapHeader header) { return default(bool); }
        public void CopyTo(System.Web.Services.Protocols.SoapHeader[] array, int index) { }
        public int IndexOf(System.Web.Services.Protocols.SoapHeader header) { return default(int); }
        public void Insert(int index, System.Web.Services.Protocols.SoapHeader header) { }
        public void Remove(System.Web.Services.Protocols.SoapHeader header) { }
    }
    [System.FlagsAttribute]
    public enum SoapHeaderDirection
    {
        Fault = 4,
        In = 1,
        InOut = 3,
        Out = 2,
    }
    public sealed partial class SoapHeaderMapping
    {
        internal SoapHeaderMapping() { }
        public bool Custom { get { return default(bool); } }
        public System.Web.Services.Protocols.SoapHeaderDirection Direction { get { return default(System.Web.Services.Protocols.SoapHeaderDirection); } }
        public System.Type HeaderType { get { return default(System.Type); } }
        public System.Reflection.MemberInfo MemberInfo { get { return default(System.Reflection.MemberInfo); } }
        public bool Repeats { get { return default(bool); } }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class SoapHttpClientProtocol : System.Web.Services.Protocols.HttpWebClientProtocol
    {
        public SoapHttpClientProtocol() { }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Protocols.SoapProtocolVersion)(0))]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Web.Services.Protocols.SoapProtocolVersion SoapVersion { get { return default(System.Web.Services.Protocols.SoapProtocolVersion); } set { } }
        protected System.IAsyncResult BeginInvoke(string methodName, object[] parameters, System.AsyncCallback callback, object asyncState) { return default(System.IAsyncResult); }
        public void Discover() { }
        protected object[] EndInvoke(System.IAsyncResult asyncResult) { return default(object[]); }
        protected virtual System.Xml.XmlReader GetReaderForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize) { return default(System.Xml.XmlReader); }
        protected override System.Net.WebRequest GetWebRequest(System.Uri uri) { return default(System.Net.WebRequest); }
        protected virtual System.Xml.XmlWriter GetWriterForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize) { return default(System.Xml.XmlWriter); }
        protected object[] Invoke(string method_name, object[] parameters) { return default(object[]); }
        protected void InvokeAsync(string methodName, object[] parameters, System.Threading.SendOrPostCallback callback) { }
        protected void InvokeAsync(string methodName, object[] parameters, System.Threading.SendOrPostCallback callback, object userState) { }
    }
    public abstract partial class SoapMessage
    {
        internal SoapMessage() { }
        public abstract string Action { get; }
        public string ContentEncoding { get { return default(string); } set { } }
        public string ContentType { get { return default(string); } set { } }
        public System.Web.Services.Protocols.SoapException Exception { get { return default(System.Web.Services.Protocols.SoapException); } set { } }
        public System.Web.Services.Protocols.SoapHeaderCollection Headers { get { return default(System.Web.Services.Protocols.SoapHeaderCollection); } }
        public abstract System.Web.Services.Protocols.LogicalMethodInfo MethodInfo { get; }
        public abstract bool OneWay { get; }
        [System.ComponentModel.DefaultValueAttribute((System.Web.Services.Protocols.SoapProtocolVersion)(0))]
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual System.Web.Services.Protocols.SoapProtocolVersion SoapVersion { get { return default(System.Web.Services.Protocols.SoapProtocolVersion); } }
        public System.Web.Services.Protocols.SoapMessageStage Stage { get { return default(System.Web.Services.Protocols.SoapMessageStage); } }
        public System.IO.Stream Stream { get { return default(System.IO.Stream); } }
        public abstract string Url { get; }
        protected abstract void EnsureInStage();
        protected abstract void EnsureOutStage();
        protected void EnsureStage(System.Web.Services.Protocols.SoapMessageStage stage) { }
        public object GetInParameterValue(int index) { return default(object); }
        public object GetOutParameterValue(int index) { return default(object); }
        public object GetReturnValue() { return default(object); }
    }
    public enum SoapMessageStage
    {
        AfterDeserialize = 8,
        AfterSerialize = 2,
        BeforeDeserialize = 4,
        BeforeSerialize = 1,
    }
    public enum SoapParameterStyle
    {
        Bare = 1,
        Default = 0,
        Wrapped = 2,
    }
    public enum SoapProtocolVersion
    {
        Default = 0,
        Soap11 = 1,
        Soap12 = 2,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(64), Inherited=true)]
    public sealed partial class SoapRpcMethodAttribute : System.Attribute
    {
        public SoapRpcMethodAttribute() { }
        public SoapRpcMethodAttribute(string action) { }
        public string Action { get { return default(string); } set { } }
        public string Binding { get { return default(string); } set { } }
        public bool OneWay { get { return default(bool); } set { } }
        public string RequestElementName { get { return default(string); } set { } }
        public string RequestNamespace { get { return default(string); } set { } }
        public string ResponseElementName { get { return default(string); } set { } }
        public string ResponseNamespace { get { return default(string); } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=true)]
    public sealed partial class SoapRpcServiceAttribute : System.Attribute
    {
        public SoapRpcServiceAttribute() { }
        public System.Web.Services.Protocols.SoapServiceRoutingStyle RoutingStyle { get { return default(System.Web.Services.Protocols.SoapServiceRoutingStyle); } set { } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Web.Services.Description.SoapBindingUse Use { get { return default(System.Web.Services.Description.SoapBindingUse); } set { } }
    }
    public enum SoapServiceRoutingStyle
    {
        RequestElement = 1,
        SoapAction = 0,
    }
    public sealed partial class SoapUnknownHeader : System.Web.Services.Protocols.SoapHeader
    {
        public SoapUnknownHeader() { }
        [System.Xml.Serialization.XmlIgnoreAttribute]
        public System.Xml.XmlElement Element { get { return default(System.Xml.XmlElement); } set { } }
    }
    public abstract partial class UrlEncodedParameterWriter : System.Web.Services.Protocols.MimeParameterWriter
    {
        protected UrlEncodedParameterWriter() { }
        public override System.Text.Encoding RequestEncoding { get { return default(System.Text.Encoding); } set { } }
        protected void Encode(System.IO.TextWriter writer, object[] values) { }
        protected void Encode(System.IO.TextWriter writer, string name, object value) { }
        public override object GetInitializer(System.Web.Services.Protocols.LogicalMethodInfo methodInfo) { return default(object); }
        public override void Initialize(object initializer) { }
    }
    public partial class UrlParameterWriter : System.Web.Services.Protocols.UrlEncodedParameterWriter
    {
        public UrlParameterWriter() { }
        public override string GetRequestUrl(string url, object[] parameters) { return default(string); }
    }
    public partial class WebClientAsyncResult : System.IAsyncResult
    {
        internal WebClientAsyncResult() { }
        public object AsyncState { get { return default(object); } }
        public System.Threading.WaitHandle AsyncWaitHandle { get { return default(System.Threading.WaitHandle); } }
        public bool CompletedSynchronously { get { return default(bool); } }
        public bool IsCompleted { get { return default(bool); } }
        public void Abort() { }
    }
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public abstract partial class WebClientProtocol : System.ComponentModel.Component
    {
        protected WebClientProtocol() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string ConnectionGroupName { get { return default(string); } set { } }
        public System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool PreAuthenticate { get { return default(bool); } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.RecommendedAsConfigurableAttribute(true)]
        public System.Text.Encoding RequestEncoding { get { return default(System.Text.Encoding); } set { } }
        [System.ComponentModel.DefaultValueAttribute(100000)]
        [System.ComponentModel.RecommendedAsConfigurableAttribute(true)]
        public int Timeout { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RecommendedAsConfigurableAttribute(true)]
        public string Url { get { return default(string); } set { } }
        public bool UseDefaultCredentials { get { return default(bool); } set { } }
        public virtual void Abort() { }
        protected static void AddToCache(System.Type type, object value) { }
        protected static object GetFromCache(System.Type type) { return default(object); }
        protected virtual System.Net.WebRequest GetWebRequest(System.Uri uri) { return default(System.Net.WebRequest); }
        protected virtual System.Net.WebResponse GetWebResponse(System.Net.WebRequest request) { return default(System.Net.WebResponse); }
        protected virtual System.Net.WebResponse GetWebResponse(System.Net.WebRequest request, System.IAsyncResult result) { return default(System.Net.WebResponse); }
    }
    public partial class XmlReturnReader : System.Web.Services.Protocols.MimeReturnReader
    {
        public XmlReturnReader() { }
        public override object GetInitializer(System.Web.Services.Protocols.LogicalMethodInfo methodInfo) { return default(object); }
        public override object[] GetInitializers(System.Web.Services.Protocols.LogicalMethodInfo[] methodInfos) { return default(object[]); }
        public override void Initialize(object o) { }
        public override object Read(System.Net.WebResponse response, System.IO.Stream responseStream) { return default(object); }
    }
}
namespace System.Web.Util
{
    public partial class HttpEncoder
    {
        public HttpEncoder() { }
        public static System.Web.Util.HttpEncoder Current { get { return default(System.Web.Util.HttpEncoder); } set { } }
        public static System.Web.Util.HttpEncoder Default { get { return default(System.Web.Util.HttpEncoder); } }
        protected internal virtual void HeaderNameValueEncode(string headerName, string headerValue, out string encodedHeaderName, out string encodedHeaderValue) { encodedHeaderName = default(string); encodedHeaderValue = default(string); }
        protected internal virtual void HtmlAttributeEncode(string value, System.IO.TextWriter output) { }
        protected internal virtual void HtmlDecode(string value, System.IO.TextWriter output) { }
        protected internal virtual void HtmlEncode(string value, System.IO.TextWriter output) { }
        protected internal virtual byte[] UrlEncode(byte[] bytes, int offset, int count) { return default(byte[]); }
        protected internal virtual string UrlPathEncode(string value) { return default(string); }
    }
}
