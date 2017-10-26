// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.ServiceModel.Channels;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = Atom10Constants.FeedTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10FeedFormatter : SyndicationFeedFormatter, IXmlSerializable
    {
        internal static readonly TimeSpan zeroOffset = new TimeSpan(0, 0, 0);
        internal const string XmlNs = "http://www.w3.org/XML/1998/namespace";
        internal const string XmlNsNs = "http://www.w3.org/2000/xmlns/";
        private static readonly XmlQualifiedName s_atom10Href = new XmlQualifiedName(Atom10Constants.HrefTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Label = new XmlQualifiedName(Atom10Constants.LabelTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Length = new XmlQualifiedName(Atom10Constants.LengthTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Relative = new XmlQualifiedName(Atom10Constants.RelativeTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Scheme = new XmlQualifiedName(Atom10Constants.SchemeTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Term = new XmlQualifiedName(Atom10Constants.TermTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Title = new XmlQualifiedName(Atom10Constants.TitleTag, string.Empty);
        private static readonly XmlQualifiedName s_atom10Type = new XmlQualifiedName(Atom10Constants.TypeTag, string.Empty);
        private static readonly UriGenerator s_idGenerator = new UriGenerator();
        private const string Rfc3339LocalDateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz";
        private const string Rfc3339UTCDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        private Type _feedType;
        private int _maxExtensionSize;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;
        public Atom10FeedFormatter() : this(typeof(SyndicationFeed)) { }
        public Atom10FeedFormatter(Type feedTypeToCreate) : base()
        {
            if (feedTypeToCreate == null)
            {
                throw new ArgumentNullException(nameof(feedTypeToCreate));
            }
            
            if (!typeof(SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw new ArgumentException(SR.Format(SR.InvalidObjectTypePassed, nameof(feedTypeToCreate), nameof(SyndicationFeed)));
            }
            
            _maxExtensionSize = int.MaxValue;
            _preserveAttributeExtensions = _preserveElementExtensions = true;
            _feedType = feedTypeToCreate;
            DateTimeParser = DateTimeHelper.CreateAtom10DateTimeParser();
        }

        public Atom10FeedFormatter(SyndicationFeed feedToWrite) : base(feedToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _maxExtensionSize = int.MaxValue;
            _preserveAttributeExtensions = _preserveElementExtensions = true;
            _feedType = feedToWrite.GetType();
            DateTimeParser = DateTimeHelper.CreateAtom10DateTimeParser();
        }

        public bool PreserveAttributeExtensions
        {
            get { return _preserveAttributeExtensions; }
            set { _preserveAttributeExtensions = value; }
        }

        public bool PreserveElementExtensions
        {
            get { return _preserveElementExtensions; }
            set { _preserveElementExtensions = value; }
        }

        public override string Version
        {
            get { return SyndicationVersions.Atom10; }
        }

        protected Type FeedType
        {
            get
            {
                return _feedType;
            }
        }

        public override bool CanRead(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return reader.IsStartElement(Atom10Constants.FeedTag, Atom10Constants.Atom10Namespace);
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            ReadFromAsync(reader, CancellationToken.None).GetAwaiter().GetResult();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);
            WriteFeedAsync(writer).GetAwaiter().GetResult();
        }

        public override void ReadFrom(XmlReader reader)
        {
            ReadFromAsync(reader, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override void WriteTo(XmlWriter writer)
        {
            WriteToAsync(writer, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task ReadFromAsync(XmlReader reader, CancellationToken ct)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(SR.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
            }

            SetFeed(CreateFeedInstance());
            await ReadFeedFromAsync(XmlReaderWrapper.CreateFromReader(reader), Feed, false).ConfigureAwait(false);
        }

        public override async Task WriteToAsync(XmlWriter writer, CancellationToken ct)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(Atom10Constants.FeedTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            await WriteFeedAsync(writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        internal static async Task<SyndicationCategory> ReadCategoryAsync(XmlReader reader, SyndicationCategory category, string version, bool preserveAttributeExtensions, bool preserveElementExtensions, int _maxExtensionSize)
        {
            await MoveToStartElementAsync(reader).ConfigureAwait(false);
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string value = await reader.GetValueAsync().ConfigureAwait(false);
                    bool notHandled = false;

                    if (reader.NamespaceURI == string.Empty)
                    {
                        switch (reader.LocalName)
                        {
                            case Atom10Constants.TermTag:
                                category.Name = value;
                                break;

                            case Atom10Constants.SchemeTag:
                                category.Scheme = value;
                                break;

                            case Atom10Constants.LabelTag:
                                category.Label = value;
                                break;

                            default:
                                notHandled = true;
                                break;
                        }
                    }
                    else
                    {
                        notHandled = true;
                    }

                    if (notHandled)
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns))
                        {
                            continue;
                        }

                        if (!TryParseAttribute(name, ns, value, category, version))
                        {
                            if (preserveAttributeExtensions)
                            {
                                category.AttributeExtensions.Add(new XmlQualifiedName(name, ns), value);
                            }
                        }
                    }
                }
            }

            if (!isEmpty)
            {
                await reader.ReadStartElementAsync().ConfigureAwait(false);
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        if (TryParseElement(reader, category, version))
                        {
                            continue;
                        }
                        else if (!preserveElementExtensions)
                        {
                            await reader.SkipAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            var tuple = await CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                            buffer = tuple.Item1;
                            extWriter = tuple.Item2;
                        }
                    }

                    LoadElementExtensions(buffer, extWriter, category);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        ((IDisposable) extWriter).Dispose();
                    }
                }

                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadStartElementAsync().ConfigureAwait(false);
            }

            return category;
        }

        internal Task<TextSyndicationContent> ReadTextContentFromAsync(XmlReader reader, string context, bool preserveAttributeExtensions)
        {
            string type = reader.GetAttribute(Atom10Constants.TypeTag);
            return ReadTextContentFromHelperAsync(reader, type, context, preserveAttributeExtensions);
        }

        internal static async Task WriteCategoryAsync(XmlWriter writer, SyndicationCategory category, string version)
        {
            await writer.WriteStartElementAsync(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            await WriteAttributeExtensionsAsync(writer, category, version).ConfigureAwait(false);
            string categoryName = category.Name ?? string.Empty;
            if (!category.AttributeExtensions.ContainsKey(s_atom10Term))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.TermTag, categoryName).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(category.Label) && !category.AttributeExtensions.ContainsKey(s_atom10Label))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.LabelTag, category.Label).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(category.Scheme) && !category.AttributeExtensions.ContainsKey(s_atom10Scheme))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.SchemeTag, category.Scheme).ConfigureAwait(false);
            }

            await WriteElementExtensionsAsync(writer, category, version).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        internal async Task ReadItemFromAsync(XmlReader reader, SyndicationItem result)
        {
            await ReadItemFromAsync(reader, result, null).ConfigureAwait(false);
        }

        internal async Task<bool> TryParseFeedElementFromAsync(XmlReader reader, SyndicationFeed result)
        {
            if (await reader.MoveToContentAsync().ConfigureAwait(false) != XmlNodeType.Element)
            {
                return false;
            }

            string name = reader.LocalName;
            string ns = reader.NamespaceURI;

            if (ns == Atom10Constants.Atom10Namespace)
            {
                switch (name)
                {
                    case Atom10Constants.AuthorTag:
                        result.Authors.Add(await ReadPersonFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.CategoryTag:
                        result.Categories.Add(await ReadCategoryFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.ContributorTag:
                        result.Contributors.Add(await ReadPersonFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.GeneratorTag:
                        result.Generator = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Atom10Constants.GeneratorTag, ns);
                        break;
                    case Atom10Constants.IdTag:
                        result.Id = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Atom10Constants.IdTag, ns);
                        break;
                    case Atom10Constants.LinkTag:
                        result.Links.Add(await ReadLinkFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.LogoTag:
                        result.ImageUrl = UriParser(await reader.ReadElementStringAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute, Atom10Constants.LogoTag, ns);
                        break;
                    case Atom10Constants.RightsTag:
                        result.Copyright = await ReadTextContentFromAsync(reader, "//atom:feed/atom:rights[@type]").ConfigureAwait(false);
                        break;
                    case Atom10Constants.SubtitleTag:
                        result.Description = await ReadTextContentFromAsync(reader, "//atom:feed/atom:subtitle[@type]").ConfigureAwait(false);
                        break;
                    case Atom10Constants.TitleTag:
                        result.Title = await ReadTextContentFromAsync(reader, "//atom:feed/atom:title[@type]").ConfigureAwait(false);
                        break;
                    case Atom10Constants.UpdatedTag:
                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                        result.LastUpdatedTime = DateTimeParser(await reader.ReadStringAsync().ConfigureAwait(false), Atom10Constants.UpdatedTag, ns);
                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                        break;
                    case Atom10Constants.IconTag:
                        result.IconImage = UriParser(await reader.ReadElementStringAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute, Atom10Constants.IconTag, ns);
                        break;
                    default:
                        return false;
                }

                return true;
            }

            return false;
        }

        internal async Task<bool> TryParseItemElementFromAsync(XmlReader reader, SyndicationItem result)
        {
            if (await reader.MoveToContentAsync().ConfigureAwait(false) != XmlNodeType.Element)
            {
                return false;
            }

            string name = reader.LocalName;
            string ns = reader.NamespaceURI;

            if (ns == Atom10Constants.Atom10Namespace)
            {
                switch (name)
                {
                    case Atom10Constants.AuthorTag:
                        result.Authors.Add(await ReadPersonFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.CategoryTag:
                        result.Categories.Add(await ReadCategoryFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.ContentTag:
                        result.Content = await ReadContentFromAsync(reader, result).ConfigureAwait(false);
                        break;
                    case Atom10Constants.ContributorTag:
                        result.Contributors.Add(await ReadPersonFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.IdTag:
                        result.Id = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Atom10Constants.IdTag, ns);
                        break;
                    case Atom10Constants.LinkTag:
                        result.Links.Add(await ReadLinkFromAsync(reader, result).ConfigureAwait(false));
                        break;
                    case Atom10Constants.PublishedTag:
                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                        result.PublishDate = DateTimeParser(await reader.ReadStringAsync().ConfigureAwait(false), Atom10Constants.UpdatedTag, ns);
                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                        break;
                    case Atom10Constants.RightsTag:
                        result.Copyright = await ReadTextContentFromAsync(reader, "//atom:feed/atom:entry/atom:rights[@type]").ConfigureAwait(false);
                        break;
                    case Atom10Constants.SourceFeedTag:
                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                        result.SourceFeed = await ReadFeedFromAsync(reader, new SyndicationFeed(), true).ConfigureAwait(false); //  isSourceFeed 
                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                        break;
                    case Atom10Constants.SummaryTag:
                        result.Summary = await ReadTextContentFromAsync(reader, "//atom:feed/atom:entry/atom:summary[@type]").ConfigureAwait(false);
                        break;
                    case Atom10Constants.TitleTag:
                        result.Title = await ReadTextContentFromAsync(reader, "//atom:feed/atom:entry/atom:title[@type]").ConfigureAwait(false);
                        break;
                    case Atom10Constants.UpdatedTag:
                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                        result.LastUpdatedTime = DateTimeParser(await reader.ReadStringAsync().ConfigureAwait(false), Atom10Constants.UpdatedTag, ns);
                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                        break;
                    default:
                        return false;
                }

                return true;
            }

            return false;
        }

        internal Task WriteContentToAsync(XmlWriter writer, string elementName, SyndicationContent content)
        {
            if (content != null)
            {
                return content.WriteToAsync(writer, elementName, Atom10Constants.Atom10Namespace);
            }
            return Task.CompletedTask;
        }

        internal async Task WriteElementAsync(XmlWriter writer, string elementName, string value)
        {
            if (value != null)
            {
                await writer.WriteElementStringAsync(elementName, Atom10Constants.Atom10Namespace, value).ConfigureAwait(false);
            }
        }

        internal async Task WriteFeedAuthorsToAsync(XmlWriter writer, Collection<SyndicationPerson> authors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < authors.Count; ++i)
            {
                SyndicationPerson p = authors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.AuthorTag).ConfigureAwait(false);
            }
        }

        internal async Task WriteFeedContributorsToAsync(XmlWriter writer, Collection<SyndicationPerson> contributors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < contributors.Count; ++i)
            {
                SyndicationPerson p = contributors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.ContributorTag).ConfigureAwait(false);
            }
        }

        internal Task WriteFeedLastUpdatedTimeToAsync(XmlWriter writer, DateTimeOffset lastUpdatedTime, bool isRequired)
        {
            if (lastUpdatedTime == DateTimeOffset.MinValue && isRequired)
            {
                lastUpdatedTime = DateTimeOffset.UtcNow;
            }

            if (lastUpdatedTime != DateTimeOffset.MinValue)
            {
                return WriteElementAsync(writer, Atom10Constants.UpdatedTag, AsString(lastUpdatedTime));
            }

            return Task.CompletedTask;
        }

        internal async Task WriteItemAuthorsToAsync(XmlWriter writer, Collection<SyndicationPerson> authors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < authors.Count; ++i)
            {
                SyndicationPerson p = authors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.AuthorTag).ConfigureAwait(false);
            }
        }

        internal Task WriteItemContentsAsync(XmlWriter dictWriter, SyndicationItem item)
        {
            dictWriter = XmlWriterWrapper.CreateFromWriter(dictWriter);
            return WriteItemContentsAsync(dictWriter, item, null);
        }

        internal async Task WriteItemContributorsToAsync(XmlWriter writer, Collection<SyndicationPerson> contributors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);

            for (int i = 0; i < contributors.Count; ++i)
            {
                SyndicationPerson p = contributors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.ContributorTag).ConfigureAwait(false);
            }
        }

        internal Task WriteItemLastUpdatedTimeToAsync(XmlWriter writer, DateTimeOffset lastUpdatedTime)
        {
            if (lastUpdatedTime == DateTimeOffset.MinValue)
            {
                lastUpdatedTime = DateTimeOffset.UtcNow;
            }

            return writer.WriteElementStringAsync(Atom10Constants.UpdatedTag, Atom10Constants.Atom10Namespace, AsString(lastUpdatedTime));
        }

        internal async Task WriteLinkAsync(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            await writer.WriteStartElementAsync(Atom10Constants.LinkTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", XmlNs, FeedUtils.GetUriString(baseUriToWrite)).ConfigureAwait(false);
            }

            await link.WriteAttributeExtensionsAsync(writer, SyndicationVersions.Atom10).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(link.RelationshipType) && !link.AttributeExtensions.ContainsKey(s_atom10Relative))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.RelativeTag, link.RelationshipType).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(link.MediaType) && !link.AttributeExtensions.ContainsKey(s_atom10Type))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.TypeTag, link.MediaType).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(link.Title) && !link.AttributeExtensions.ContainsKey(s_atom10Title))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.TitleTag, link.Title).ConfigureAwait(false);
            }

            if (link.Length != 0 && !link.AttributeExtensions.ContainsKey(s_atom10Length))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.LengthTag, Convert.ToString(link.Length, CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            if (!link.AttributeExtensions.ContainsKey(s_atom10Href))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.HrefTag, FeedUtils.GetUriString(link.Uri)).ConfigureAwait(false);
            }

            await link.WriteElementExtensionsAsync(writer, SyndicationVersions.Atom10).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        protected override SyndicationFeed CreateFeedInstance()
        {
            return SyndicationFeedFormatter.CreateFeedInstance(_feedType);
        }

        protected virtual SyndicationItem ReadItem(XmlReader reader, SyndicationFeed feed)
        {
            return ReadItemAsync(reader, feed).GetAwaiter().GetResult();
        }

        protected virtual IEnumerable<SyndicationItem> ReadItems(XmlReader reader, SyndicationFeed feed, out bool areAllItemsRead)
        {
            IEnumerable<SyndicationItem> result = ReadItemsAsync(reader, feed).GetAwaiter().GetResult();
            areAllItemsRead = true;
            return result;
        }

        protected virtual async Task<SyndicationItem> ReadItemAsync(XmlReader reader, SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            SyndicationItem item = CreateItem(feed);
            reader = XmlReaderWrapper.CreateFromReader(reader);
            await ReadItemFromAsync(reader, item, feed.BaseUri).ConfigureAwait(false);
            return item;
        }

        //not referenced anymore
        protected virtual async Task<IEnumerable<SyndicationItem>> ReadItemsAsync(XmlReader reader, SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            NullNotAllowedCollection<SyndicationItem> items = new NullNotAllowedCollection<SyndicationItem>();
            reader = XmlReaderWrapper.CreateFromReader(reader);

            while (await reader.IsStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false))
            {
                items.Add(await ReadItemAsync(reader, feed).ConfigureAwait(false));
            }

            return items;
        }

        protected virtual void WriteItem(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            WriteItemAsync(writer, item, feedBaseUri).GetAwaiter().GetResult();
        }

        protected virtual void WriteItems(XmlWriter writer, IEnumerable<SyndicationItem> items, Uri feedBaseUri)
        {
            WriteItemsAsync(writer, items, feedBaseUri).GetAwaiter().GetResult();
        }

        protected virtual async Task WriteItemAsync(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            await writer.WriteStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            await WriteItemContentsAsync(writer, item, feedBaseUri).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        protected virtual async Task WriteItemsAsync(XmlWriter writer, IEnumerable<SyndicationItem> items, Uri feedBaseUri)
        {
            if (items == null)
            {
                return;
            }

            foreach (SyndicationItem item in items)
            {
                await WriteItemAsync(writer, item, feedBaseUri).ConfigureAwait(false);
            }
        }

        private async Task<TextSyndicationContent> ReadTextContentFromHelperAsync(XmlReader reader, string type, string context, bool preserveAttributeExtensions)
        {
            if (string.IsNullOrEmpty(type))
            {
                type = Atom10Constants.PlaintextType;
            }

            TextSyndicationContentKind kind = new TextSyndicationContentKind();
            switch (type)
            {
                case Atom10Constants.PlaintextType:
                    kind = TextSyndicationContentKind.Plaintext;
                    break;
                case Atom10Constants.HtmlType:
                    kind = TextSyndicationContentKind.Html;
                    break;
                case Atom10Constants.XHtmlType:
                    kind = TextSyndicationContentKind.XHtml;
                    break;

                    throw new XmlException(SR.Format(SR.Atom10SpecRequiresTextConstruct, context, type));
            }

            Dictionary<XmlQualifiedName, string> attrs = null;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == Atom10Constants.TypeTag && reader.NamespaceURI == string.Empty)
                    {
                        continue;
                    }
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;
                    if (FeedUtils.IsXmlns(name, ns))
                    {
                        continue;
                    }
                    if (preserveAttributeExtensions)
                    {
                        string value = await reader.GetValueAsync().ConfigureAwait(false);
                        if (attrs == null)
                        {
                            attrs = new Dictionary<XmlQualifiedName, string>();
                        }
                        attrs.Add(new XmlQualifiedName(name, ns), value);
                    }
                }
            }
            reader.MoveToElement();
            string localName = reader.LocalName;
            string nameSpace = reader.NamespaceURI;
            string val = (kind == TextSyndicationContentKind.XHtml) ? await reader.ReadInnerXmlAsync().ConfigureAwait(false) : StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), localName, nameSpace); // cant custom parse because its static
            TextSyndicationContent result = new TextSyndicationContent(val, kind);
            if (attrs != null)
            {
                foreach (XmlQualifiedName attr in attrs.Keys)
                {
                    if (!FeedUtils.IsXmlns(attr.Name, attr.Namespace))
                    {
                        result.AttributeExtensions.Add(attr, attrs[attr]);
                    }
                }
            }
            return result;
        }

        private string AsString(DateTimeOffset dateTime)
        {
            if (dateTime.Offset == zeroOffset)
            {
                return dateTime.ToUniversalTime().ToString(Rfc3339UTCDateTimeFormat, CultureInfo.InvariantCulture);
            }
            else
            {
                return dateTime.ToString(Rfc3339LocalDateTimeFormat, CultureInfo.InvariantCulture);
            }
        }

        private Task<SyndicationCategory> ReadCategoryAsync(XmlReader reader, SyndicationCategory category)
        {
            return ReadCategoryAsync(reader, category, Version, PreserveAttributeExtensions, PreserveElementExtensions, _maxExtensionSize);
        }

        private Task<SyndicationCategory> ReadCategoryFromAsync(XmlReader reader, SyndicationFeed feed)

        {
            SyndicationCategory result = CreateCategory(feed);
            return ReadCategoryAsync(reader, result);
        }

        private async Task<SyndicationCategory> ReadCategoryFromAsync(XmlReader reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            await ReadCategoryAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private async Task<SyndicationContent> ReadContentFromAsync(XmlReader reader, SyndicationItem item)
        {
            await MoveToStartElementAsync(reader).ConfigureAwait(false);
            string type = reader.GetAttribute(Atom10Constants.TypeTag, string.Empty);

            SyndicationContent result;
            if (TryParseContent(reader, item, type, Version, out result))
            {
                return result;
            }

            if (string.IsNullOrEmpty(type))
            {
                type = Atom10Constants.PlaintextType;
            }
            string src = reader.GetAttribute(Atom10Constants.SourceTag, string.Empty);

            if (string.IsNullOrEmpty(src) && type != Atom10Constants.PlaintextType && type != Atom10Constants.HtmlType && type != Atom10Constants.XHtmlType)
            {
                return new XmlSyndicationContent(reader);
            }

            if (!string.IsNullOrEmpty(src))
            {
                result = new UrlSyndicationContent(new Uri(src, UriKind.RelativeOrAbsolute), type);
                bool isEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalName == Atom10Constants.TypeTag && reader.NamespaceURI == string.Empty)
                        {
                            continue;
                        }
                        else if (reader.LocalName == Atom10Constants.SourceTag && reader.NamespaceURI == string.Empty)
                        {
                            continue;
                        }
                        else if (!FeedUtils.IsXmlns(reader.LocalName, reader.NamespaceURI))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync().ConfigureAwait(false));
                            }
                            else
                            {
                                //result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync());
                            }
                        }
                    }
                }

                await reader.ReadStartElementAsync().ConfigureAwait(false);
                if (!isEmpty)
                {
                    await reader.ReadEndElementAsync().ConfigureAwait(false);
                }

                return result;
            }
            else
            {
                return await ReadTextContentFromHelperAsync(reader, type, "//atom:feed/atom:entry/atom:content[@type]", _preserveAttributeExtensions).ConfigureAwait(false);
            }
        }

        private async Task<SyndicationFeed> ReadFeedFromAsync(XmlReader reader, SyndicationFeed result, bool isSourceFeed)
        {
            await reader.MoveToContentAsync().ConfigureAwait(false);
            //fix to accept non contiguous items
            NullNotAllowedCollection<SyndicationItem> feedItems = new NullNotAllowedCollection<SyndicationItem>();

            bool elementIsEmpty = false;
            if (!isSourceFeed)
            {
                await MoveToStartElementAsync(reader).ConfigureAwait(false);
                elementIsEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalName == "lang" && reader.NamespaceURI == XmlNs)
                        {
                            result.Language = await reader.GetValueAsync().ConfigureAwait(false);
                        }
                        else if (reader.LocalName == "base" && reader.NamespaceURI == XmlNs)
                        {
                            result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                        }
                        else
                        {
                            string ns = reader.NamespaceURI;
                            string name = reader.LocalName;
                            if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                            {
                                continue;
                            }

                            string val = await reader.GetValueAsync().ConfigureAwait(false);

                            if (!TryParseAttribute(name, ns, val, result, Version))
                            {
                                if (_preserveAttributeExtensions)
                                {
                                    result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), val);
                                }
                            }
                        }
                    }
                }
                await reader.ReadStartElementAsync().ConfigureAwait(false);
            }

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;

            if (!elementIsEmpty)
            {
                try
                {
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        if (await TryParseFeedElementFromAsync(reader, result).ConfigureAwait(false))
                        {
                            // nothing, we parsed something, great
                        }
                        else if (await reader.IsStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false) && !isSourceFeed)
                        {
                            while (await reader.IsStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false))
                            {
                                feedItems.Add(await ReadItemAsync(reader, result).ConfigureAwait(false));
                            }
                        }
                        else
                        {
                            if (!TryParseElement(reader, result, Version))
                            {
                                if (_preserveElementExtensions)
                                {
                                    var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                                    buffer = tuple.Item1;
                                    extWriter = tuple.Item2;
                                }
                                else
                                {
                                    await reader.SkipAsync().ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    //Add all read items to the feed
                    result.Items = feedItems;
                    LoadElementExtensions(buffer, extWriter, result);
                }
                catch (FormatException e)
                {
                    throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingFeed), e);
                }
                catch (ArgumentException e)
                {
                    throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingFeed), e);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        ((IDisposable) extWriter).Dispose();
                    }
                }
            }
            if (!isSourceFeed)
            {
                await reader.ReadEndElementAsync().ConfigureAwait(false); // feed
            }

            return result;
        }

        private async Task ReadItemFromAsync(XmlReader reader, SyndicationItem result, Uri feedBaseUri)
        {
            try
            {
                result.BaseUri = feedBaseUri;
                await MoveToStartElementAsync(reader).ConfigureAwait(false);
                bool isEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (name == "base" && ns == XmlNs)
                        {
                            result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                            continue;
                        }

                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }

                        string val = await reader.GetValueAsync().ConfigureAwait(false);
                        if (!TryParseAttribute(name, ns, val, result, Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                        }
                    }
                }
                await reader.ReadStartElementAsync().ConfigureAwait(false);

                if (!isEmpty)
                {
                    XmlBuffer buffer = null;
                    XmlDictionaryWriter extWriter = null;
                    try
                    {
                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                        {
                            if (await TryParseItemElementFromAsync(reader, result).ConfigureAwait(false))
                            {
                                // nothing, we parsed something, great
                            }
                            else
                            {
                                if (!TryParseElement(reader, result, Version))
                                {
                                    if (_preserveElementExtensions)
                                    {
                                        var tuple = await CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                                        buffer = tuple.Item1;
                                        extWriter = tuple.Item2;
                                    }
                                    else
                                    {
                                        await reader.SkipAsync().ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                        LoadElementExtensions(buffer, extWriter, result);
                    }
                    finally
                    {
                        if (extWriter != null)
                        {
                            ((IDisposable) extWriter).Dispose();
                        }
                    }
                    await reader.ReadEndElementAsync().ConfigureAwait(false); // item
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingItem), e);
            }
            catch (ArgumentException e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingItem), e);
            }
        }

        private async Task ReadLinkAsync(XmlReader reader, SyndicationLink link, Uri baseUri)
        {
            bool isEmpty = reader.IsEmptyElement;
            string mediaType = null;
            string relationship = null;
            string title = null;
            string lengthStr = null;
            string val = null;
            string ns = null;
            link.BaseUri = baseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    bool notHandled = false;
                    if (reader.LocalName == "base" && reader.NamespaceURI == XmlNs)
                    {
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                    }
                    else if (reader.NamespaceURI == string.Empty)
                    {
                        switch (reader.LocalName)
                        {
                            case Atom10Constants.TypeTag:
                                mediaType = await reader.GetValueAsync().ConfigureAwait(false);
                                break;
                            case Atom10Constants.RelativeTag:
                                relationship = await reader.GetValueAsync().ConfigureAwait(false);
                                break;
                            case Atom10Constants.TitleTag:
                                title = await reader.GetValueAsync().ConfigureAwait(false);
                                break;
                            case Atom10Constants.LengthTag:
                                lengthStr = await reader.GetValueAsync().ConfigureAwait(false);
                                break;
                            case Atom10Constants.HrefTag:
                                val = await reader.GetValueAsync().ConfigureAwait(false);
                                ns = reader.NamespaceURI;
                                break;
                            default:
                                notHandled = true;
                                break;
                        }
                    }
                    else
                    {
                        notHandled = true;
                    }

                    if (notHandled && !FeedUtils.IsXmlns(reader.LocalName, reader.NamespaceURI))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            link.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync().ConfigureAwait(false));
                        }
                    }
                }
            }

            long length = 0;
            if (!string.IsNullOrEmpty(lengthStr))
            {
                length = Convert.ToInt64(lengthStr, CultureInfo.InvariantCulture.NumberFormat);
            }

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            if (!isEmpty)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        if (TryParseElement(reader, link, Version))
                        {
                            continue;
                        }
                        else if (!_preserveElementExtensions)
                        {
                            await reader.SkipAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                            buffer = tuple.Item1;
                            extWriter = tuple.Item2;
                        }
                    }

                    LoadElementExtensions(buffer, extWriter, link);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        ((IDisposable) extWriter).Dispose();
                    }
                }

                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }

            link.Length = length;
            link.MediaType = mediaType;
            link.RelationshipType = relationship;
            link.Title = title;
            link.Uri = (val != null) ? UriParser(val, UriKind.RelativeOrAbsolute, Atom10Constants.LinkTag, ns) : null;
        }

        private async Task<SyndicationLink> ReadLinkFromAsync(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationLink result = CreateLink(feed);
            await ReadLinkAsync(reader, result, feed.BaseUri).ConfigureAwait(false);
            return result;
        }

        private async Task<SyndicationLink> ReadLinkFromAsync(XmlReader reader, SyndicationItem item)
        {
            SyndicationLink result = CreateLink(item);
            await ReadLinkAsync(reader, result, item.BaseUri).ConfigureAwait(false);
            return result;
        }

        private async Task<SyndicationPerson> ReadPersonFromAsync(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            await ReadPersonFromAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private async Task<SyndicationPerson> ReadPersonFromAsync(XmlReader reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            await ReadPersonFromAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private async Task ReadPersonFromAsync(XmlReader reader, SyndicationPerson result)
        {
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;
                    if (FeedUtils.IsXmlns(name, ns))
                    {
                        continue;
                    }
                    string val = await reader.GetValueAsync().ConfigureAwait(false);
                    if (!TryParseAttribute(name, ns, val, result, Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync().ConfigureAwait(false));
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            if (!isEmpty)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        string name = reader.LocalName;
                        string ns = reader.NamespaceURI;
                        bool notHandled = false;

                        switch (name)
                        {
                            case Atom10Constants.NameTag:
                                result.Name = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Atom10Constants.NameTag, ns);
                                break;
                            case Atom10Constants.UriTag:
                                result.Uri = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Atom10Constants.UriTag, ns);
                                break;
                            case Atom10Constants.EmailTag:
                                result.Email = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Atom10Constants.EmailTag, ns);
                                break;
                            default:
                                notHandled = true;
                                break;
                        }

                        if (notHandled && !TryParseElement(reader, result, Version))
                        {
                            if (_preserveElementExtensions)
                            {
                                var tuple = await SyndicationFeedFormatter.CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize).ConfigureAwait(false);
                                buffer = tuple.Item1;
                                extWriter = tuple.Item2;
                            }
                            else
                            {
                                await reader.SkipAsync().ConfigureAwait(false);
                            }
                        }
                    }

                    LoadElementExtensions(buffer, extWriter, result);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        ((IDisposable) extWriter).Dispose();
                    }
                }

                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }
        }

        private Task<TextSyndicationContent> ReadTextContentFromAsync(XmlReader reader, string context)
        {
            return ReadTextContentFromAsync(reader, context, PreserveAttributeExtensions);
        }

        private async Task WriteCategoriesToAsync(XmlWriter writer, Collection<SyndicationCategory> categories)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < categories.Count; ++i)
            {
                await WriteCategoryAsync(writer, categories[i], Version).ConfigureAwait(false);
            }
        }

        private Task WriteFeedAsync(XmlWriter writer)
        {
            if (Feed == null)
            {
                throw new InvalidOperationException(SR.FeedFormatterDoesNotHaveFeed);
            }
            return WriteFeedToAsync(writer, Feed, false); //  isSourceFeed 
        }

        private async Task WriteFeedToAsync(XmlWriter writer, SyndicationFeed feed, bool isSourceFeed)
        {
            if (!isSourceFeed)
            {
                if (!string.IsNullOrEmpty(feed.Language))
                {
                    await writer.WriteAttributeStringAsync("xml", "lang", XmlNs, feed.Language).ConfigureAwait(false);
                }
                if (feed.BaseUri != null)
                {
                    await writer.WriteAttributeStringAsync("xml", "base", XmlNs, FeedUtils.GetUriString(feed.BaseUri)).ConfigureAwait(false);
                }
                await WriteAttributeExtensionsAsync(writer, feed, Version).ConfigureAwait(false);
            }
            bool isElementRequired = !isSourceFeed;
            TextSyndicationContent title = feed.Title;
            if (isElementRequired)
            {
                title = title ?? new TextSyndicationContent(string.Empty);
            }
            await WriteContentToAsync(writer, Atom10Constants.TitleTag, title).ConfigureAwait(false);
            await WriteContentToAsync(writer, Atom10Constants.SubtitleTag, feed.Description).ConfigureAwait(false);
            string id = feed.Id;
            if (isElementRequired)
            {
                id = id ?? s_idGenerator.Next();
            }
            await WriteElementAsync(writer, Atom10Constants.IdTag, id).ConfigureAwait(false);
            await WriteContentToAsync(writer, Atom10Constants.RightsTag, feed.Copyright).ConfigureAwait(false);
            await WriteFeedLastUpdatedTimeToAsync(writer, feed.LastUpdatedTime, isElementRequired).ConfigureAwait(false);
            await WriteCategoriesToAsync(writer, feed.Categories).ConfigureAwait(false);
            if (feed.ImageUrl != null)
            {
                await WriteElementAsync(writer, Atom10Constants.LogoTag, feed.ImageUrl.ToString()).ConfigureAwait(false);
            }
            await WriteFeedAuthorsToAsync(writer, feed.Authors).ConfigureAwait(false);
            await WriteFeedContributorsToAsync(writer, feed.Contributors).ConfigureAwait(false);
            await WriteElementAsync(writer, Atom10Constants.GeneratorTag, feed.Generator).ConfigureAwait(false);

            if (feed.IconImage != null)
            {
                await WriteElementAsync(writer, Atom10Constants.IconTag, feed.IconImage.AbsoluteUri).ConfigureAwait(false);
            }

            for (int i = 0; i < feed.Links.Count; ++i)
            {
                await WriteLinkAsync(writer, feed.Links[i], feed.BaseUri).ConfigureAwait(false);
            }

            await WriteElementExtensionsAsync(writer, feed, Version).ConfigureAwait(false);

            if (!isSourceFeed)
            {
                await WriteItemsAsync(writer, feed.Items, feed.BaseUri).ConfigureAwait(false);
            }
        }

        private async Task WriteItemContentsAsync(XmlWriter dictWriter, SyndicationItem item, Uri feedBaseUri)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(feedBaseUri, item.BaseUri);
            if (baseUriToWrite != null)
            {
                await dictWriter.WriteAttributeStringAsync("xml", "base", XmlNs, FeedUtils.GetUriString(baseUriToWrite)).ConfigureAwait(false);
            }
            await WriteAttributeExtensionsAsync(dictWriter, item, Version).ConfigureAwait(false);

            string id = item.Id ?? s_idGenerator.Next();
            await WriteElementAsync(dictWriter, Atom10Constants.IdTag, id).ConfigureAwait(false);

            TextSyndicationContent title = item.Title ?? new TextSyndicationContent(string.Empty);
            await WriteContentToAsync(dictWriter, Atom10Constants.TitleTag, title).ConfigureAwait(false);
            await WriteContentToAsync(dictWriter, Atom10Constants.SummaryTag, item.Summary).ConfigureAwait(false);
            if (item.PublishDate != DateTimeOffset.MinValue)
            {
                await dictWriter.WriteElementStringAsync(Atom10Constants.PublishedTag, Atom10Constants.Atom10Namespace, AsString(item.PublishDate)).ConfigureAwait(false);
            }
            await WriteItemLastUpdatedTimeToAsync(dictWriter, item.LastUpdatedTime).ConfigureAwait(false);
            await WriteItemAuthorsToAsync(dictWriter, item.Authors).ConfigureAwait(false);
            await WriteItemContributorsToAsync(dictWriter, item.Contributors).ConfigureAwait(false);
            for (int i = 0; i < item.Links.Count; ++i)
            {
                await WriteLinkAsync(dictWriter, item.Links[i], item.BaseUri).ConfigureAwait(false);
            }
            await WriteCategoriesToAsync(dictWriter, item.Categories).ConfigureAwait(false);
            await WriteContentToAsync(dictWriter, Atom10Constants.ContentTag, item.Content).ConfigureAwait(false);
            await WriteContentToAsync(dictWriter, Atom10Constants.RightsTag, item.Copyright).ConfigureAwait(false);
            if (item.SourceFeed != null)
            {
                await dictWriter.WriteStartElementAsync(Atom10Constants.SourceFeedTag, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
                await WriteFeedToAsync(dictWriter, item.SourceFeed, true).ConfigureAwait(false); //  isSourceFeed 
                await dictWriter.WriteEndElementAsync().ConfigureAwait(false);
            }
            await WriteElementExtensionsAsync(dictWriter, item, Version).ConfigureAwait(false);
        }

        private async Task WritePersonToAsync(XmlWriter writer, SyndicationPerson p, string elementName)
        {
            await writer.WriteStartElementAsync(elementName, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            await WriteAttributeExtensionsAsync(writer, p, Version).ConfigureAwait(false);
            await WriteElementAsync(writer, Atom10Constants.NameTag, p.Name).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(p.Uri))
            {
                await writer.WriteElementStringAsync(Atom10Constants.UriTag, Atom10Constants.Atom10Namespace, p.Uri).ConfigureAwait(false);
            }
            if (!string.IsNullOrEmpty(p.Email))
            {
                await writer.WriteElementStringAsync(Atom10Constants.EmailTag, Atom10Constants.Atom10Namespace, p.Email).ConfigureAwait(false);
            }
            await WriteElementExtensionsAsync(writer, p, Version).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    [XmlRoot(ElementName = Atom10Constants.FeedTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10FeedFormatter<TSyndicationFeed> : Atom10FeedFormatter
        where TSyndicationFeed : SyndicationFeed, new()
    {
        // constructors
        public Atom10FeedFormatter()
            : base(typeof(TSyndicationFeed))
        {
        }
        public Atom10FeedFormatter(TSyndicationFeed feedToWrite)
            : base(feedToWrite)
        {
        }

        protected override SyndicationFeed CreateFeedInstance()
        {
            return new TSyndicationFeed();
        }
    }
}
