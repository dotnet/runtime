// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    public static partial class Atom10Constants
    {
        public const string AlternateTag = "alternate";
        public const string Atom10Namespace = "http://www.w3.org/2005/Atom";
        public const string Atom10Prefix = "a10";
        public const string AtomMediaType = "application/atom+xml";
        public const string AuthorTag = "author";
        public const string CategoryTag = "category";
        public const string ContentTag = "content";
        public const string ContributorTag = "contributor";
        public const string EmailTag = "email";
        public const string EntryTag = "entry";
        public const string FeedTag = "feed";
        public const string GeneratorTag = "generator";
        public const string HrefTag = "href";
        public const string HtmlMediaType = "text/html";
        public const string HtmlType = "html";
        public const string IconTag = "icon";
        public const string IdTag = "id";
        public const string LabelTag = "label";
        public const string LengthTag = "length";
        public const string LinkTag = "link";
        public const string LogoTag = "logo";
        public const string NameTag = "name";
        public const string PlaintextType = "text";
        public const string PublishedTag = "published";
        public const string RelativeTag = "rel";
        public const string RightsTag = "rights";
        public const string SchemeTag = "scheme";
        public const string SelfTag = "self";
        public const string SourceFeedTag = "source";
        public const string SourceTag = "src";
        public const string SpecificationLink = "http://atompub.org/2005/08/17/draft-ietf-atompub-format-11.html";
        public const string SubtitleTag = "subtitle";
        public const string SummaryTag = "summary";
        public const string TermTag = "term";
        public const string TitleTag = "title";
        public const string TypeTag = "type";
        public const string UpdatedTag = "updated";
        public const string UriTag = "uri";
        public const string XHtmlMediaType = "application/xhtml+xml";
        public const string XHtmlType = "xhtml";
        public const string XmlMediaType = "text/xml";
    }
    public partial class Atom10FeedFormatter : System.ServiceModel.Syndication.SyndicationFeedFormatter
    {
        public System.Func<string, string, string, System.DateTimeOffset> dateParser;
        public System.Func<string, string, string, string> stringParser;
        public System.Func<string, System.UriKind, string, string, System.Uri> uriParser;
        public Atom10FeedFormatter() { }
        public Atom10FeedFormatter(System.ServiceModel.Syndication.SyndicationFeed feedToWrite) { }
        public Atom10FeedFormatter(System.Type feedTypeToCreate) { }
        protected System.Type FeedType { get { throw null; } }
        public bool PreserveAttributeExtensions { get { throw null; } set { } }
        public bool PreserveElementExtensions { get { throw null; } set { } }
        public override string Version { get { throw null; } }
        public override bool CanRead(System.Xml.XmlReader reader) { throw null; }
        protected override System.ServiceModel.Syndication.SyndicationFeed CreateFeedInstance() { throw null; }
        public override System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader, System.Threading.CancellationToken ct) { throw null; }
        protected virtual System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationItem> ReadItemAsync(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected virtual System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem>> ReadItemsAsync(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteItemAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationItem item, System.Uri feedBaseUri) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteItemsAsync(System.Xml.XmlWriter writer, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem> items, System.Uri feedBaseUri) { throw null; }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken ct) { throw null; }
    }
    public partial class Atom10FeedFormatter<TSyndicationFeed> : System.ServiceModel.Syndication.Atom10FeedFormatter where TSyndicationFeed : System.ServiceModel.Syndication.SyndicationFeed, new()
    {
        public Atom10FeedFormatter() { }
        public Atom10FeedFormatter(TSyndicationFeed feedToWrite) { }
        protected override System.ServiceModel.Syndication.SyndicationFeed CreateFeedInstance() { throw null; }
    }
    public partial class Atom10ItemFormatter : System.ServiceModel.Syndication.SyndicationItemFormatter
    {
        public Atom10ItemFormatter() { }
        public Atom10ItemFormatter(System.ServiceModel.Syndication.SyndicationItem itemToWrite) { }
        public Atom10ItemFormatter(System.Type itemTypeToCreate) { }
        protected System.Type ItemType { get { throw null; } }
        public bool PreserveAttributeExtensions { get { throw null; } set { } }
        public bool PreserveElementExtensions { get { throw null; } set { } }
        public override string Version { get { throw null; } }
        public override bool CanRead(System.Xml.XmlReader reader) { throw null; }
        protected override System.ServiceModel.Syndication.SyndicationItem CreateItemInstance() { throw null; }
        public override System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader) { throw null; }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer) { throw null; }
    }
    public partial class Atom10ItemFormatter<TSyndicationItem> : System.ServiceModel.Syndication.Atom10ItemFormatter where TSyndicationItem : System.ServiceModel.Syndication.SyndicationItem, new()
    {
        public Atom10ItemFormatter() { }
        public Atom10ItemFormatter(TSyndicationItem itemToWrite) { }
        protected override System.ServiceModel.Syndication.SyndicationItem CreateItemInstance() { throw null; }
    }
    public partial class AtomPub10CategoriesDocumentFormatter : System.ServiceModel.Syndication.CategoriesDocumentFormatter
    {
        public AtomPub10CategoriesDocumentFormatter() { }
        public AtomPub10CategoriesDocumentFormatter(System.ServiceModel.Syndication.CategoriesDocument documentToWrite) { }
        public AtomPub10CategoriesDocumentFormatter(System.Type inlineDocumentType, System.Type referencedDocumentType) { }
        public override string Version { get { throw null; } }
        public override System.Threading.Tasks.Task<bool> CanReadAsync(System.Xml.XmlReader reader) { throw null; }
        protected override System.ServiceModel.Syndication.InlineCategoriesDocument CreateInlineCategoriesDocument() { throw null; }
        protected override System.ServiceModel.Syndication.ReferencedCategoriesDocument CreateReferencedCategoriesDocument() { throw null; }
        public override System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader) { throw null; }
        public override System.Threading.Tasks.Task WriteTo(System.Xml.XmlWriter writer) { throw null; }
    }
    public partial class AtomPub10ServiceDocumentFormatter : System.ServiceModel.Syndication.ServiceDocumentFormatter
    {
        public AtomPub10ServiceDocumentFormatter() { }
        public AtomPub10ServiceDocumentFormatter(System.ServiceModel.Syndication.ServiceDocument documentToWrite) { }
        public AtomPub10ServiceDocumentFormatter(System.Type documentTypeToCreate) { }
        public override string Version { get { throw null; } }
        public override System.Threading.Tasks.Task<bool> CanReadAsync(System.Xml.XmlReader reader) { throw null; }
        protected override System.ServiceModel.Syndication.ServiceDocument CreateDocumentInstance() { throw null; }
        public override System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader) { throw null; }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer) { throw null; }
    }
    public partial class AtomPub10ServiceDocumentFormatter<TServiceDocument> : System.ServiceModel.Syndication.AtomPub10ServiceDocumentFormatter where TServiceDocument : System.ServiceModel.Syndication.ServiceDocument, new()
    {
        public AtomPub10ServiceDocumentFormatter() { }
        public AtomPub10ServiceDocumentFormatter(TServiceDocument documentToWrite) { }
        protected override System.ServiceModel.Syndication.ServiceDocument CreateDocumentInstance() { throw null; }
    }
    public abstract partial class CategoriesDocument
    {
        internal CategoriesDocument() { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public string Language { get { throw null; } set { } }
        public static System.ServiceModel.Syndication.InlineCategoriesDocument Create(System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationCategory> categories) { throw null; }
        public static System.ServiceModel.Syndication.InlineCategoriesDocument Create(System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationCategory> categories, bool isFixed, string scheme) { throw null; }
        public static System.ServiceModel.Syndication.ReferencedCategoriesDocument Create(System.Uri linkToCategoriesDocument) { throw null; }
        public System.ServiceModel.Syndication.CategoriesDocumentFormatter GetFormatter() { throw null; }
        public static System.Threading.Tasks.Task<System.ServiceModel.Syndication.CategoriesDocument> LoadAsync(System.Xml.XmlReader reader) { throw null; }
        public void Save(System.Xml.XmlWriter writer) { }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    public abstract partial class CategoriesDocumentFormatter
    {
        protected CategoriesDocumentFormatter() { }
        protected CategoriesDocumentFormatter(System.ServiceModel.Syndication.CategoriesDocument documentToWrite) { }
        public System.ServiceModel.Syndication.CategoriesDocument Document { get { throw null; } }
        public abstract string Version { get; }
        public abstract System.Threading.Tasks.Task<bool> CanReadAsync(System.Xml.XmlReader reader);
        protected virtual System.ServiceModel.Syndication.InlineCategoriesDocument CreateInlineCategoriesDocument() { throw null; }
        protected virtual System.ServiceModel.Syndication.ReferencedCategoriesDocument CreateReferencedCategoriesDocument() { throw null; }
        public abstract System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader);
        protected virtual void SetDocument(System.ServiceModel.Syndication.CategoriesDocument document) { }
        public abstract System.Threading.Tasks.Task WriteTo(System.Xml.XmlWriter writer);
    }
    public partial class InlineCategoriesDocument : System.ServiceModel.Syndication.CategoriesDocument
    {
        public InlineCategoriesDocument() { }
        public InlineCategoriesDocument(System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationCategory> categories) { }
        public InlineCategoriesDocument(System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationCategory> categories, bool isFixed, string scheme) { }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationCategory> Categories { get { throw null; } }
        public bool IsFixed { get { throw null; } set { } }
        public string Scheme { get { throw null; } set { } }
        protected internal virtual System.ServiceModel.Syndication.SyndicationCategory CreateCategory() { throw null; }
    }
    public partial class ReferencedCategoriesDocument : System.ServiceModel.Syndication.CategoriesDocument
    {
        public ReferencedCategoriesDocument() { }
        public ReferencedCategoriesDocument(System.Uri link) { }
        public System.Uri Link { get { throw null; } set { } }
    }
    public partial class ResourceCollectionInfo
    {
        public ResourceCollectionInfo() { }
        public ResourceCollectionInfo(System.ServiceModel.Syndication.TextSyndicationContent title, System.Uri link) { }
        public ResourceCollectionInfo(System.ServiceModel.Syndication.TextSyndicationContent title, System.Uri link, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.CategoriesDocument> categories, bool allowsNewEntries) { }
        public ResourceCollectionInfo(System.ServiceModel.Syndication.TextSyndicationContent title, System.Uri link, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.CategoriesDocument> categories, System.Collections.Generic.IEnumerable<string> accepts) { }
        public ResourceCollectionInfo(string title, System.Uri link) { }
        public System.Collections.ObjectModel.Collection<string> Accepts { get { throw null; } }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.CategoriesDocument> Categories { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public System.Uri Link { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TextSyndicationContent Title { get { throw null; } set { } }
        protected internal virtual System.ServiceModel.Syndication.InlineCategoriesDocument CreateInlineCategoriesDocument() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.ReferencedCategoriesDocument CreateReferencedCategoriesDocument() { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    public partial class Rss20FeedFormatter : System.ServiceModel.Syndication.SyndicationFeedFormatter
    {
        public Rss20FeedFormatter() { }
        public Rss20FeedFormatter(System.ServiceModel.Syndication.SyndicationFeed feedToWrite) { }
        public Rss20FeedFormatter(System.ServiceModel.Syndication.SyndicationFeed feedToWrite, bool serializeExtensionsAsAtom) { }
        public Rss20FeedFormatter(System.Type feedTypeToCreate) { }
        public System.Func<string, string, string, System.DateTimeOffset> DateParser { get { throw null; } set { } }
        protected System.Type FeedType { get { throw null; } }
        public bool PreserveAttributeExtensions { get { throw null; } set { } }
        public bool PreserveElementExtensions { get { throw null; } set { } }
        public bool SerializeExtensionsAsAtom { get { throw null; } set { } }
        public System.Func<string, string, string, string> StringParser { get { throw null; } set { } }
        public System.Func<string, string, string, System.Uri> UriParser { get { throw null; } set { } }
        public override string Version { get { throw null; } }
        public override bool CanRead(System.Xml.XmlReader reader) { throw null; }
        protected override System.ServiceModel.Syndication.SyndicationFeed CreateFeedInstance() { throw null; }
        public static System.DateTimeOffset DefaultDateParser(string dateTimeString, string localName, string ns) { throw null; }
        public override System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader, System.Threading.CancellationToken ct) { throw null; }
        protected virtual System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationItem> ReadItemAsync(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected internal override void SetFeed(System.ServiceModel.Syndication.SyndicationFeed feed) { }
        protected virtual System.Threading.Tasks.Task WriteItemAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationItem item, System.Uri feedBaseUri) { throw null; }
        protected virtual System.Threading.Tasks.Task WriteItemsAsync(System.Xml.XmlWriter writer, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem> items, System.Uri feedBaseUri) { throw null; }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken ct) { throw null; }
    }
    public partial class Rss20FeedFormatter<TSyndicationFeed> : System.ServiceModel.Syndication.Rss20FeedFormatter where TSyndicationFeed : System.ServiceModel.Syndication.SyndicationFeed, new()
    {
        public Rss20FeedFormatter() { }
        public Rss20FeedFormatter(TSyndicationFeed feedToWrite) { }
        public Rss20FeedFormatter(TSyndicationFeed feedToWrite, bool serializeExtensionsAsAtom) { }
        protected override System.ServiceModel.Syndication.SyndicationFeed CreateFeedInstance() { throw null; }
    }
    public partial class Rss20ItemFormatter : System.ServiceModel.Syndication.SyndicationItemFormatter
    {
        public Rss20ItemFormatter() { }
        public Rss20ItemFormatter(System.ServiceModel.Syndication.SyndicationItem itemToWrite) { }
        public Rss20ItemFormatter(System.ServiceModel.Syndication.SyndicationItem itemToWrite, bool serializeExtensionsAsAtom) { }
        public Rss20ItemFormatter(System.Type itemTypeToCreate) { }
        protected System.Type ItemType { get { throw null; } }
        public bool PreserveAttributeExtensions { get { throw null; } set { } }
        public bool PreserveElementExtensions { get { throw null; } set { } }
        public bool SerializeExtensionsAsAtom { get { throw null; } set { } }
        public override string Version { get { throw null; } }
        public override bool CanRead(System.Xml.XmlReader reader) { throw null; }
        protected override System.ServiceModel.Syndication.SyndicationItem CreateItemInstance() { throw null; }
        public override System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader) { throw null; }
        public override System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer) { throw null; }
    }
    public partial class Rss20ItemFormatter<TSyndicationItem> : System.ServiceModel.Syndication.Rss20ItemFormatter where TSyndicationItem : System.ServiceModel.Syndication.SyndicationItem, new()
    {
        public Rss20ItemFormatter() { }
        public Rss20ItemFormatter(TSyndicationItem itemToWrite) { }
        public Rss20ItemFormatter(TSyndicationItem itemToWrite, bool serializeExtensionsAsAtom) { }
        protected override System.ServiceModel.Syndication.SyndicationItem CreateItemInstance() { throw null; }
    }
    public partial class ServiceDocument
    {
        public ServiceDocument() { }
        public ServiceDocument(System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.Workspace> workspaces) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public string Language { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.Workspace> Workspaces { get { throw null; } }
        protected internal virtual System.ServiceModel.Syndication.Workspace CreateWorkspace() { throw null; }
        public System.ServiceModel.Syndication.ServiceDocumentFormatter GetFormatter() { throw null; }
        public static System.Threading.Tasks.Task<TServiceDocument> LoadAsync<TServiceDocument>(System.Xml.XmlReader reader) where TServiceDocument : System.ServiceModel.Syndication.ServiceDocument, new() { throw null; }
        public System.Threading.Tasks.Task Save(System.Xml.XmlWriter writer) { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    [System.Runtime.Serialization.DataContractAttribute]
    public abstract partial class ServiceDocumentFormatter
    {
        protected ServiceDocumentFormatter() { }
        protected ServiceDocumentFormatter(System.ServiceModel.Syndication.ServiceDocument documentToWrite) { }
        public System.ServiceModel.Syndication.ServiceDocument Document { get { throw null; } }
        public abstract string Version { get; }
        public abstract System.Threading.Tasks.Task<bool> CanReadAsync(System.Xml.XmlReader reader);
        protected static System.ServiceModel.Syndication.SyndicationCategory CreateCategory(System.ServiceModel.Syndication.InlineCategoriesDocument inlineCategories) { throw null; }
        protected static System.ServiceModel.Syndication.ResourceCollectionInfo CreateCollection(System.ServiceModel.Syndication.Workspace workspace) { throw null; }
        protected virtual System.ServiceModel.Syndication.ServiceDocument CreateDocumentInstance() { throw null; }
        protected static System.ServiceModel.Syndication.InlineCategoriesDocument CreateInlineCategories(System.ServiceModel.Syndication.ResourceCollectionInfo collection) { throw null; }
        protected static System.ServiceModel.Syndication.ReferencedCategoriesDocument CreateReferencedCategories(System.ServiceModel.Syndication.ResourceCollectionInfo collection) { throw null; }
        protected static System.ServiceModel.Syndication.Workspace CreateWorkspace(System.ServiceModel.Syndication.ServiceDocument document) { throw null; }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.CategoriesDocument categories, int maxExtensionSize) { }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.ResourceCollectionInfo collection, int maxExtensionSize) { }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.ServiceDocument document, int maxExtensionSize) { }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.Workspace workspace, int maxExtensionSize) { }
        public abstract System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader);
        protected virtual void SetDocument(System.ServiceModel.Syndication.ServiceDocument document) { }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.CategoriesDocument categories, string version) { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.ResourceCollectionInfo collection, string version) { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.ServiceDocument document, string version) { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.Workspace workspace, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.CategoriesDocument categories, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.ResourceCollectionInfo collection, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.ServiceDocument document, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.Workspace workspace, string version) { throw null; }
        protected static void WriteAttributeExtensions(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.ServiceDocument document, string version) { }
        protected static void WriteAttributeExtensions(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.Workspace workspace, string version) { }
        protected static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.CategoriesDocument categories, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.ResourceCollectionInfo collection, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.CategoriesDocument categories, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.ResourceCollectionInfo collection, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.ServiceDocument document, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.Workspace workspace, string version) { throw null; }
        public abstract System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer);
    }
    public partial class SyndicationCategory
    {
        public SyndicationCategory() { }
        protected SyndicationCategory(System.ServiceModel.Syndication.SyndicationCategory source) { }
        public SyndicationCategory(string name) { }
        public SyndicationCategory(string name, string scheme, string label) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public string Label { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string Scheme { get { throw null; } set { } }
        public virtual System.ServiceModel.Syndication.SyndicationCategory Clone() { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    public abstract partial class SyndicationContent
    {
        protected SyndicationContent() { }
        protected SyndicationContent(System.ServiceModel.Syndication.SyndicationContent source) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public abstract string Type { get; }
        public abstract System.ServiceModel.Syndication.SyndicationContent Clone();
        public static System.ServiceModel.Syndication.TextSyndicationContent CreateHtmlContent(string content) { throw null; }
        public static System.ServiceModel.Syndication.TextSyndicationContent CreatePlaintextContent(string content) { throw null; }
        public static System.ServiceModel.Syndication.UrlSyndicationContent CreateUrlContent(System.Uri url, string mediaType) { throw null; }
        public static System.ServiceModel.Syndication.TextSyndicationContent CreateXhtmlContent(string content) { throw null; }
        public static System.ServiceModel.Syndication.XmlSyndicationContent CreateXmlContent(object dataContractObject) { throw null; }
        public static System.ServiceModel.Syndication.XmlSyndicationContent CreateXmlContent(object dataContractObject, System.Runtime.Serialization.XmlObjectSerializer dataContractSerializer) { throw null; }
        public static System.ServiceModel.Syndication.XmlSyndicationContent CreateXmlContent(object xmlSerializerObject, System.Xml.Serialization.XmlSerializer serializer) { throw null; }
        public static System.ServiceModel.Syndication.XmlSyndicationContent CreateXmlContent(System.Xml.XmlReader XmlReaderWrapper) { throw null; }
        protected abstract void WriteContentsTo(System.Xml.XmlWriter writer);
        public System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, string outerElementName, string outerElementNamespace) { throw null; }
    }
    public partial class SyndicationElementExtension
    {
        public SyndicationElementExtension(object dataContractExtension) { }
        public SyndicationElementExtension(object dataContractExtension, System.Runtime.Serialization.XmlObjectSerializer dataContractSerializer) { }
        public SyndicationElementExtension(object xmlSerializerExtension, System.Xml.Serialization.XmlSerializer serializer) { }
        public SyndicationElementExtension(string outerName, string outerNamespace, object dataContractExtension) { }
        public SyndicationElementExtension(string outerName, string outerNamespace, object dataContractExtension, System.Runtime.Serialization.XmlObjectSerializer dataContractSerializer) { }
        public SyndicationElementExtension(System.Xml.XmlReader reader) { }
        public string OuterName { get { throw null; } }
        public string OuterNamespace { get { throw null; } }
        public System.Threading.Tasks.Task<TExtension> GetObject<TExtension>() { throw null; }
        public System.Threading.Tasks.Task<TExtension> GetObject<TExtension>(System.Runtime.Serialization.XmlObjectSerializer serializer) { throw null; }
        public System.Threading.Tasks.Task<TExtension> GetObject<TExtension>(System.Xml.Serialization.XmlSerializer serializer) { throw null; }
        public System.Threading.Tasks.Task<System.Xml.XmlReader> GetReaderAsync() { throw null; }
        public System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer) { throw null; }
    }
    public sealed partial class SyndicationElementExtensionCollection : System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationElementExtension>
    {
        internal SyndicationElementExtensionCollection() { }
        public void Add(object extension) { }
        public void Add(object dataContractExtension, System.Runtime.Serialization.DataContractSerializer serializer) { }
        public void Add(object xmlSerializerExtension, System.Xml.Serialization.XmlSerializer serializer) { }
        public void Add(string outerName, string outerNamespace, object dataContractExtension) { }
        public void Add(string outerName, string outerNamespace, object dataContractExtension, System.Runtime.Serialization.XmlObjectSerializer dataContractSerializer) { }
        public void Add(System.Xml.XmlReader reader) { }
        protected override void ClearItems() { }
        public System.Threading.Tasks.Task<System.Xml.XmlReader> GetReaderAtElementExtensions() { throw null; }
        protected override void InsertItem(int index, System.ServiceModel.Syndication.SyndicationElementExtension item) { }
        public System.Threading.Tasks.Task<System.Collections.ObjectModel.Collection<TExtension>> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace) { throw null; }
        public System.Threading.Tasks.Task<System.Collections.ObjectModel.Collection<TExtension>> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace, System.Runtime.Serialization.XmlObjectSerializer serializer) { throw null; }
        public System.Threading.Tasks.Task<System.Collections.ObjectModel.Collection<TExtension>> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace, System.Xml.Serialization.XmlSerializer serializer) { throw null; }
        protected override void RemoveItem(int index) { }
        protected override void SetItem(int index, System.ServiceModel.Syndication.SyndicationElementExtension item) { }
    }
    public partial class SyndicationFeed
    {
        public SyndicationFeed() { }
        public SyndicationFeed(System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem> items) { }
        protected SyndicationFeed(System.ServiceModel.Syndication.SyndicationFeed source, bool cloneItems) { }
        public SyndicationFeed(string title, string description, System.Uri feedAlternateLink) { }
        public SyndicationFeed(string title, string description, System.Uri feedAlternateLink, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem> items) { }
        public SyndicationFeed(string title, string description, System.Uri feedAlternateLink, string id, System.DateTimeOffset lastUpdatedTime) { }
        public SyndicationFeed(string title, string description, System.Uri feedAlternateLink, string id, System.DateTimeOffset lastUpdatedTime, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem> items) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationPerson> Authors { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationCategory> Categories { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationPerson> Contributors { get { throw null; } }
        public System.ServiceModel.Syndication.TextSyndicationContent Copyright { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TextSyndicationContent Description { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationLink Documentation { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public string Generator { get { throw null; } set { } }
        public System.Uri IconImage { get { throw null; } set { } }
        public string Id { get { throw null; } set { } }
        public System.Uri ImageLink { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TextSyndicationContent ImageTitle { get { throw null; } set { } }
        public System.Uri ImageUrl { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.SyndicationItem> Items { get { throw null; } set { } }
        public string Language { get { throw null; } set { } }
        public System.DateTimeOffset LastUpdatedTime { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationLink> Links { get { throw null; } }
        public System.Collections.ObjectModel.Collection<string> SkipDays { get { throw null; } }
        public System.Collections.ObjectModel.Collection<int> SkipHours { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationTextInput TextInput { get { throw null; } set { } }
        public int TimeToLive { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TextSyndicationContent Title { get { throw null; } set { } }
        public virtual System.ServiceModel.Syndication.SyndicationFeed Clone(bool cloneItems) { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationCategory CreateCategory() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationItem CreateItem() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationLink CreateLink() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationPerson CreatePerson() { throw null; }
        public System.ServiceModel.Syndication.Atom10FeedFormatter GetAtom10Formatter() { throw null; }
        public System.ServiceModel.Syndication.Rss20FeedFormatter GetRss20Formatter() { throw null; }
        public System.ServiceModel.Syndication.Rss20FeedFormatter GetRss20Formatter(bool serializeExtensionsAsAtom) { throw null; }
        public static System.ServiceModel.Syndication.SyndicationFeed Load(System.Xml.XmlReader reader) { throw null; }
        public static System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationFeed> LoadAsync(System.Xml.XmlReader reader, System.ServiceModel.Syndication.Atom10FeedFormatter formatter, System.Threading.CancellationToken ct) { throw null; }
        public static System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationFeed> LoadAsync(System.Xml.XmlReader reader, System.ServiceModel.Syndication.Rss20FeedFormatter Rssformatter, System.ServiceModel.Syndication.Atom10FeedFormatter Atomformatter, System.Threading.CancellationToken ct) { throw null; }
        public static System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationFeed> LoadAsync(System.Xml.XmlReader reader, System.ServiceModel.Syndication.Rss20FeedFormatter formatter, System.Threading.CancellationToken ct) { throw null; }
        public static System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationFeed> LoadAsync(System.Xml.XmlReader reader, System.Threading.CancellationToken ct) { throw null; }
        public static System.Threading.Tasks.Task<TSyndicationFeed> LoadAsync<TSyndicationFeed>(System.Xml.XmlReader reader, System.Threading.CancellationToken ct) where TSyndicationFeed : System.ServiceModel.Syndication.SyndicationFeed, new() { throw null; }
        public static TSyndicationFeed Load<TSyndicationFeed>(System.Xml.XmlReader reader) where TSyndicationFeed : System.ServiceModel.Syndication.SyndicationFeed, new() { throw null; }
        public System.Threading.Tasks.Task SaveAsAtom10Async(System.Xml.XmlWriter writer, System.Threading.CancellationToken ct) { throw null; }
        public System.Threading.Tasks.Task SaveAsRss20Async(System.Xml.XmlWriter writer, System.Threading.CancellationToken ct) { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    [System.Runtime.Serialization.DataContractAttribute]
    public abstract partial class SyndicationFeedFormatter
    {
        protected SyndicationFeedFormatter() { }
        protected SyndicationFeedFormatter(System.ServiceModel.Syndication.SyndicationFeed feedToWrite) { }
        public System.ServiceModel.Syndication.SyndicationFeed Feed { get { throw null; } }
        public abstract string Version { get; }
        public abstract bool CanRead(System.Xml.XmlReader reader);
        protected internal static System.ServiceModel.Syndication.SyndicationCategory CreateCategory(System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected internal static System.ServiceModel.Syndication.SyndicationCategory CreateCategory(System.ServiceModel.Syndication.SyndicationItem item) { throw null; }
        protected abstract System.ServiceModel.Syndication.SyndicationFeed CreateFeedInstance();
        protected internal static System.ServiceModel.Syndication.SyndicationItem CreateItem(System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected internal static System.ServiceModel.Syndication.SyndicationLink CreateLink(System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected internal static System.ServiceModel.Syndication.SyndicationLink CreateLink(System.ServiceModel.Syndication.SyndicationItem item) { throw null; }
        protected internal static System.ServiceModel.Syndication.SyndicationPerson CreatePerson(System.ServiceModel.Syndication.SyndicationFeed feed) { throw null; }
        protected internal static System.ServiceModel.Syndication.SyndicationPerson CreatePerson(System.ServiceModel.Syndication.SyndicationItem item) { throw null; }
        protected internal static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationCategory category, int maxExtensionSize) { }
        protected internal static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationFeed feed, int maxExtensionSize) { }
        protected internal static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationItem item, int maxExtensionSize) { }
        protected internal static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationLink link, int maxExtensionSize) { }
        protected internal static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationPerson person, int maxExtensionSize) { }
        public abstract System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader, System.Threading.CancellationToken ct);
        protected internal virtual void SetFeed(System.ServiceModel.Syndication.SyndicationFeed feed) { }
        public override string ToString() { throw null; }
        protected internal static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected internal static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationFeed feed, string version) { throw null; }
        protected internal static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected internal static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected internal static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        protected internal static bool TryParseContent(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationItem item, string contentType, string version, out System.ServiceModel.Syndication.SyndicationContent content) { content = default(System.ServiceModel.Syndication.SyndicationContent); throw null; }
        protected internal static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected internal static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationFeed feed, string version) { throw null; }
        protected internal static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected internal static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected internal static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteAttributeExtensions(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationFeed feed, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationFeed feed, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected internal static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        public abstract System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer, System.Threading.CancellationToken ct);
    }
    public partial class SyndicationItem
    {
        public SyndicationItem() { }
        protected SyndicationItem(System.ServiceModel.Syndication.SyndicationItem source) { }
        public SyndicationItem(string title, System.ServiceModel.Syndication.SyndicationContent content, System.Uri itemAlternateLink, string id, System.DateTimeOffset lastUpdatedTime) { }
        public SyndicationItem(string title, string content, System.Uri itemAlternateLink) { }
        public SyndicationItem(string title, string content, System.Uri itemAlternateLink, string id, System.DateTimeOffset lastUpdatedTime) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationPerson> Authors { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationCategory> Categories { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationContent Content { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationPerson> Contributors { get { throw null; } }
        public System.ServiceModel.Syndication.TextSyndicationContent Copyright { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public string Id { get { throw null; } set { } }
        public System.DateTimeOffset LastUpdatedTime { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.SyndicationLink> Links { get { throw null; } }
        public System.DateTimeOffset PublishDate { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationFeed SourceFeed { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TextSyndicationContent Summary { get { throw null; } set { } }
        public System.ServiceModel.Syndication.TextSyndicationContent Title { get { throw null; } set { } }
        public void AddPermalink(System.Uri permalink) { }
        public virtual System.ServiceModel.Syndication.SyndicationItem Clone() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationCategory CreateCategory() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationLink CreateLink() { throw null; }
        protected internal virtual System.ServiceModel.Syndication.SyndicationPerson CreatePerson() { throw null; }
        public System.ServiceModel.Syndication.Atom10ItemFormatter GetAtom10Formatter() { throw null; }
        public System.ServiceModel.Syndication.Rss20ItemFormatter GetRss20Formatter() { throw null; }
        public System.ServiceModel.Syndication.Rss20ItemFormatter GetRss20Formatter(bool serializeExtensionsAsAtom) { throw null; }
        public static System.Threading.Tasks.Task<System.ServiceModel.Syndication.SyndicationItem> LoadAsync(System.Xml.XmlReader reader) { throw null; }
        public static System.Threading.Tasks.Task<TSyndicationItem> LoadAsync<TSyndicationItem>(System.Xml.XmlReader reader) where TSyndicationItem : System.ServiceModel.Syndication.SyndicationItem, new() { throw null; }
        public System.Threading.Tasks.Task SaveAsAtom10(System.Xml.XmlWriter writer) { throw null; }
        public System.Threading.Tasks.Task SaveAsRss20(System.Xml.XmlWriter writer) { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseContent(System.Xml.XmlReader reader, string contentType, string version, out System.ServiceModel.Syndication.SyndicationContent content) { content = default(System.ServiceModel.Syndication.SyndicationContent); throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    [System.Runtime.Serialization.DataContractAttribute]
    public abstract partial class SyndicationItemFormatter
    {
        protected SyndicationItemFormatter() { }
        protected SyndicationItemFormatter(System.ServiceModel.Syndication.SyndicationItem itemToWrite) { }
        public System.ServiceModel.Syndication.SyndicationItem Item { get { throw null; } }
        public abstract string Version { get; }
        public abstract bool CanRead(System.Xml.XmlReader reader);
        protected static System.ServiceModel.Syndication.SyndicationCategory CreateCategory(System.ServiceModel.Syndication.SyndicationItem item) { throw null; }
        protected abstract System.ServiceModel.Syndication.SyndicationItem CreateItemInstance();
        protected static System.ServiceModel.Syndication.SyndicationLink CreateLink(System.ServiceModel.Syndication.SyndicationItem item) { throw null; }
        protected static System.ServiceModel.Syndication.SyndicationPerson CreatePerson(System.ServiceModel.Syndication.SyndicationItem item) { throw null; }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationCategory category, int maxExtensionSize) { }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationItem item, int maxExtensionSize) { }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationLink link, int maxExtensionSize) { }
        protected static void LoadElementExtensions(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationPerson person, int maxExtensionSize) { }
        public abstract System.Threading.Tasks.Task ReadFromAsync(System.Xml.XmlReader reader);
        protected internal virtual void SetItem(System.ServiceModel.Syndication.SyndicationItem item) { }
        public override string ToString() { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected static bool TryParseAttribute(string name, string ns, string value, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        protected static bool TryParseContent(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationItem item, string contentType, string version, out System.ServiceModel.Syndication.SyndicationContent content) { content = default(System.ServiceModel.Syndication.SyndicationContent); throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected static bool TryParseElement(System.Xml.XmlReader reader, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        protected System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationCategory category, string version) { throw null; }
        protected static System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationItem item, string version) { throw null; }
        protected System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationLink link, string version) { throw null; }
        protected System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, System.ServiceModel.Syndication.SyndicationPerson person, string version) { throw null; }
        public abstract System.Threading.Tasks.Task WriteToAsync(System.Xml.XmlWriter writer);
    }
    public partial class SyndicationLink
    {
        public SyndicationLink() { }
        protected SyndicationLink(System.ServiceModel.Syndication.SyndicationLink source) { }
        public SyndicationLink(System.Uri uri) { }
        public SyndicationLink(System.Uri uri, string relationshipType, string title, string mediaType, long length) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public long Length { get { throw null; } set { } }
        public string MediaType { get { throw null; } set { } }
        public string RelationshipType { get { throw null; } set { } }
        public string Title { get { throw null; } set { } }
        public System.Uri Uri { get { throw null; } set { } }
        public virtual System.ServiceModel.Syndication.SyndicationLink Clone() { throw null; }
        public static System.ServiceModel.Syndication.SyndicationLink CreateAlternateLink(System.Uri uri) { throw null; }
        public static System.ServiceModel.Syndication.SyndicationLink CreateAlternateLink(System.Uri uri, string mediaType) { throw null; }
        public static System.ServiceModel.Syndication.SyndicationLink CreateMediaEnclosureLink(System.Uri uri, string mediaType, long length) { throw null; }
        public static System.ServiceModel.Syndication.SyndicationLink CreateSelfLink(System.Uri uri) { throw null; }
        public static System.ServiceModel.Syndication.SyndicationLink CreateSelfLink(System.Uri uri, string mediaType) { throw null; }
        public System.Uri GetAbsoluteUri() { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    public partial class SyndicationPerson
    {
        public SyndicationPerson() { }
        protected SyndicationPerson(System.ServiceModel.Syndication.SyndicationPerson source) { }
        public SyndicationPerson(string email) { }
        public SyndicationPerson(string email, string name, string uri) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public string Email { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string Uri { get { throw null; } set { } }
        public virtual System.ServiceModel.Syndication.SyndicationPerson Clone() { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    public partial class SyndicationTextInput
    {
        public string Description;
        public System.ServiceModel.Syndication.SyndicationLink link;
        public string name;
        public string title;
        public SyndicationTextInput() { }
    }
    public static partial class SyndicationVersions
    {
        public const string Atom10 = "Atom10";
        public const string Rss20 = "Rss20";
    }
    public partial class TextSyndicationContent : System.ServiceModel.Syndication.SyndicationContent
    {
        protected TextSyndicationContent(System.ServiceModel.Syndication.TextSyndicationContent source) { }
        public TextSyndicationContent(string text) { }
        public TextSyndicationContent(string text, System.ServiceModel.Syndication.TextSyndicationContentKind textKind) { }
        public string Text { get { throw null; } }
        public override string Type { get { throw null; } }
        public override System.ServiceModel.Syndication.SyndicationContent Clone() { throw null; }
        protected override void WriteContentsTo(System.Xml.XmlWriter writer) { }
    }
    public enum TextSyndicationContentKind
    {
        Html = 1,
        Plaintext = 0,
        XHtml = 2,
    }
    public partial class UrlSyndicationContent : System.ServiceModel.Syndication.SyndicationContent
    {
        protected UrlSyndicationContent(System.ServiceModel.Syndication.UrlSyndicationContent source) { }
        public UrlSyndicationContent(System.Uri url, string mediaType) { }
        public override string Type { get { throw null; } }
        public System.Uri Url { get { throw null; } }
        public override System.ServiceModel.Syndication.SyndicationContent Clone() { throw null; }
        protected override void WriteContentsTo(System.Xml.XmlWriter writer) { }
    }
    public partial class Workspace
    {
        public Workspace() { }
        public Workspace(System.ServiceModel.Syndication.TextSyndicationContent title, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.ResourceCollectionInfo> collections) { }
        public Workspace(string title, System.Collections.Generic.IEnumerable<System.ServiceModel.Syndication.ResourceCollectionInfo> collections) { }
        public System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, string> AttributeExtensions { get { throw null; } }
        public System.Uri BaseUri { get { throw null; } set { } }
        public System.Collections.ObjectModel.Collection<System.ServiceModel.Syndication.ResourceCollectionInfo> Collections { get { throw null; } }
        public System.ServiceModel.Syndication.SyndicationElementExtensionCollection ElementExtensions { get { throw null; } }
        public System.ServiceModel.Syndication.TextSyndicationContent Title { get { throw null; } set { } }
        protected internal virtual System.ServiceModel.Syndication.ResourceCollectionInfo CreateResourceCollection() { throw null; }
        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version) { throw null; }
        protected internal virtual bool TryParseElement(System.Xml.XmlReader reader, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteAttributeExtensions(System.Xml.XmlWriter writer, string version) { throw null; }
        protected internal virtual System.Threading.Tasks.Task WriteElementExtensionsAsync(System.Xml.XmlWriter writer, string version) { throw null; }
    }
    public partial class XmlSyndicationContent : System.ServiceModel.Syndication.SyndicationContent
    {
        protected XmlSyndicationContent(System.ServiceModel.Syndication.XmlSyndicationContent source) { }
        public XmlSyndicationContent(string type, object dataContractExtension, System.Runtime.Serialization.XmlObjectSerializer dataContractSerializer) { }
        public XmlSyndicationContent(string type, object xmlSerializerExtension, System.Xml.Serialization.XmlSerializer serializer) { }
        public XmlSyndicationContent(string type, System.ServiceModel.Syndication.SyndicationElementExtension extension) { }
        public XmlSyndicationContent(System.Xml.XmlReader reader) { }
        public System.ServiceModel.Syndication.SyndicationElementExtension Extension { get { throw null; } }
        public override string Type { get { throw null; } }
        public override System.ServiceModel.Syndication.SyndicationContent Clone() { throw null; }
        public System.Threading.Tasks.Task<System.Xml.XmlDictionaryReader> GetReaderAtContent() { throw null; }
        public System.Threading.Tasks.Task<TContent> ReadContent<TContent>() { throw null; }
        public System.Threading.Tasks.Task<TContent> ReadContent<TContent>(System.Runtime.Serialization.XmlObjectSerializer dataContractSerializer) { throw null; }
        public System.Threading.Tasks.Task<TContent> ReadContent<TContent>(System.Xml.Serialization.XmlSerializer serializer) { throw null; }
        protected override void WriteContentsTo(System.Xml.XmlWriter writer) { }
    }
}
