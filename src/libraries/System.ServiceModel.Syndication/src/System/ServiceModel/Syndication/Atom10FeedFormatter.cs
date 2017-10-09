// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.ServiceModel.Channels;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = Atom10Constants.FeedTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10FeedFormatter : SyndicationFeedFormatter
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

        //Custom parsing
        //   value, localname , ns , result
        public Func<string, string, string, string> stringParser = DefaultStringParser;
        public Func<string, string, string, DateTimeOffset> dateParser = DefaultDateFromString;
        public Func<string, UriKind, string, string, Uri> uriParser = DefaultUriParser;

        private static string DefaultStringParser(string value, string localName, string ns)
        {
            return value;
        }

        private static Uri DefaultUriParser(string value, UriKind kind, string localName, string ns)
        {
            return new Uri(value, kind);
        }

        public Atom10FeedFormatter()
            : this(typeof(SyndicationFeed))
        {
        }

        public Atom10FeedFormatter(Type feedTypeToCreate)
            : base()
        {
            if (feedTypeToCreate == null)
            {
                throw new ArgumentException(nameof(feedTypeToCreate));
            }
            if (!typeof(SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw new ArgumentException(string.Format(SR.InvalidObjectTypePassed, nameof(feedTypeToCreate), nameof(SyndicationFeed)));
            }
            _maxExtensionSize = int.MaxValue;
            _preserveAttributeExtensions = _preserveElementExtensions = true;
            _feedType = feedTypeToCreate;
        }

        public Atom10FeedFormatter(SyndicationFeed feedToWrite)
            : base(feedToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _maxExtensionSize = int.MaxValue;
            _preserveAttributeExtensions = _preserveElementExtensions = true;
            _feedType = feedToWrite.GetType();
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

        public override async Task ReadFromAsync(XmlReader reader, CancellationToken ct)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(string.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
            }

            SetFeed(CreateFeedInstance());
            await ReadFeedFromAsync(XmlReaderWrapper.CreateFromReader(reader), this.Feed, false);
        }

        public override async Task WriteToAsync(XmlWriter writer, CancellationToken ct)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(Atom10Constants.FeedTag, Atom10Constants.Atom10Namespace);
            await WriteFeedAsync(writer);
            await writer.WriteEndElementAsync();
        }

        internal static async Task<SyndicationCategory> ReadCategoryAsync(XmlReaderWrapper reader, SyndicationCategory category, string version, bool preserveAttributeExtensions, bool preserveElementExtensions, int _maxExtensionSize)
        {
            await MoveToStartElementAsync(reader);
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string value = await reader.GetValueAsync();
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
                await reader.ReadStartElementAsync();
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync())
                    {
                        if (TryParseElement(reader, category, version))
                        {
                            continue;
                        }
                        else if (!preserveElementExtensions)
                        {
                            await reader.SkipAsync();
                        }
                        else
                        {
                            var tuple = await CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize);
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
                        ((IDisposable)extWriter).Dispose();
                    }
                }

                await reader.ReadEndElementAsync();
            }
            else
            {
                await reader.ReadStartElementAsync();
            }

            return category;
        }

        internal Task<TextSyndicationContent> ReadTextContentFromAsync(XmlReaderWrapper reader, string context, bool preserveAttributeExtensions)
        {
            string type = reader.GetAttribute(Atom10Constants.TypeTag);
            return ReadTextContentFromHelperAsync(reader, type, context, preserveAttributeExtensions);
        }

        internal static async Task WriteCategoryAsync(XmlWriter writer, SyndicationCategory category, string version)
        {
            await writer.WriteStartElementAsync(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace);
            await WriteAttributeExtensionsAsync(writer, category, version);
            string categoryName = category.Name ?? string.Empty;
            if (!category.AttributeExtensions.ContainsKey(s_atom10Term))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.TermTag, categoryName);
            }

            if (!string.IsNullOrEmpty(category.Label) && !category.AttributeExtensions.ContainsKey(s_atom10Label))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.LabelTag, category.Label);
            }

            if (!string.IsNullOrEmpty(category.Scheme) && !category.AttributeExtensions.ContainsKey(s_atom10Scheme))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.SchemeTag, category.Scheme);
            }

            await WriteElementExtensionsAsync(writer, category, version);
            await writer.WriteEndElementAsync();
        }

        internal async Task ReadItemFromAsync(XmlReaderWrapper reader, SyndicationItem result)
        {
            await ReadItemFromAsync(reader, result, null);
        }

        internal async Task<bool> TryParseFeedElementFromAsync(XmlReaderWrapper reader, SyndicationFeed result)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
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
                        result.Authors.Add(await ReadPersonFromAsync(reader, result));
                        break;
                    case Atom10Constants.CategoryTag:
                        result.Categories.Add(await ReadCategoryFromAsync(reader, result));
                        break;
                    case Atom10Constants.ContributorTag:
                        result.Contributors.Add(await ReadPersonFromAsync(reader, result));
                        break;
                    case Atom10Constants.GeneratorTag:
                        result.Generator = stringParser(await reader.ReadElementStringAsync(), Atom10Constants.GeneratorTag, ns);
                        break;
                    case Atom10Constants.IdTag:
                        result.Id = stringParser(await reader.ReadElementStringAsync(), Atom10Constants.IdTag, ns);
                        break;
                    case Atom10Constants.LinkTag:
                        result.Links.Add(await ReadLinkFromAsync(reader, result));
                        break;
                    case Atom10Constants.LogoTag:
                        result.ImageUrl = uriParser(await reader.ReadElementStringAsync(), UriKind.RelativeOrAbsolute, Atom10Constants.LogoTag, ns);  //new Uri(await reader.ReadElementStringAsync(), UriKind.RelativeOrAbsolute);
                        break;
                    case Atom10Constants.RightsTag:
                        result.Copyright = await ReadTextContentFromAsync(reader, "//atom:feed/atom:rights[@type]");
                        break;
                    case Atom10Constants.SubtitleTag:
                        result.Description = await ReadTextContentFromAsync(reader, "//atom:feed/atom:subtitle[@type]");
                        break;
                    case Atom10Constants.TitleTag:
                        result.Title = await ReadTextContentFromAsync(reader, "//atom:feed/atom:title[@type]");
                        break;
                    case Atom10Constants.UpdatedTag:
                        await reader.ReadStartElementAsync();
                        result.LastUpdatedTime = dateParser(await reader.ReadStringAsync(), Atom10Constants.UpdatedTag, ns);
                        await reader.ReadEndElementAsync();
                        break;
                    case Atom10Constants.IconTag:
                        result.IconImage = uriParser(await reader.ReadElementStringAsync(), UriKind.RelativeOrAbsolute, Atom10Constants.IconTag, ns);
                        break;
                    default:
                        return false;
                }

                return true;
            }

            return false;
        }

        internal async Task<bool> TryParseItemElementFromAsync(XmlReaderWrapper reader, SyndicationItem result)
        {
            if (await reader.MoveToContentAsync() != XmlNodeType.Element)
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
                        result.Authors.Add(await ReadPersonFromAsync(reader, result));
                        break;
                    case Atom10Constants.CategoryTag:
                        result.Categories.Add(await ReadCategoryFromAsync(reader, result));
                        break;
                    case Atom10Constants.ContentTag:
                        result.Content = await ReadContentFromAsync(reader, result);
                        break;
                    case Atom10Constants.ContributorTag:
                        result.Contributors.Add(await ReadPersonFromAsync(reader, result));
                        break;
                    case Atom10Constants.IdTag:
                        result.Id = stringParser(await reader.ReadElementStringAsync(), Atom10Constants.IdTag, ns);
                        break;
                    case Atom10Constants.LinkTag:
                        result.Links.Add(await ReadLinkFromAsync(reader, result));
                        break;
                    case Atom10Constants.PublishedTag:
                        await reader.ReadStartElementAsync();
                        result.PublishDate = dateParser(await reader.ReadStringAsync(), Atom10Constants.UpdatedTag, ns);
                        await reader.ReadEndElementAsync();
                        break;
                    case Atom10Constants.RightsTag:
                        result.Copyright = await ReadTextContentFromAsync(reader, "//atom:feed/atom:entry/atom:rights[@type]");
                        break;
                    case Atom10Constants.SourceFeedTag:
                        await reader.ReadStartElementAsync();
                        result.SourceFeed = await ReadFeedFromAsync(reader, new SyndicationFeed(), true); //  isSourceFeed 
                        await reader.ReadEndElementAsync();
                        break;
                    case Atom10Constants.SummaryTag:
                        result.Summary = await ReadTextContentFromAsync(reader, "//atom:feed/atom:entry/atom:summary[@type]");
                        break;
                    case Atom10Constants.TitleTag:
                        result.Title = await ReadTextContentFromAsync(reader, "//atom:feed/atom:entry/atom:title[@type]");
                        break;
                    case Atom10Constants.UpdatedTag:
                        await reader.ReadStartElementAsync();
                        result.LastUpdatedTime = dateParser(await reader.ReadStringAsync(), Atom10Constants.UpdatedTag, ns);
                        await reader.ReadEndElementAsync();
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
                await writer.WriteElementStringAsync(elementName, Atom10Constants.Atom10Namespace, value);
            }
        }

        internal async Task WriteFeedAuthorsToAsync(XmlWriter writer, Collection<SyndicationPerson> authors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < authors.Count; ++i)
            {
                SyndicationPerson p = authors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.AuthorTag);
            }
        }

        internal async Task WriteFeedContributorsToAsync(XmlWriter writer, Collection<SyndicationPerson> contributors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < contributors.Count; ++i)
            {
                SyndicationPerson p = contributors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.ContributorTag);
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
                await WritePersonToAsync(writer, p, Atom10Constants.AuthorTag);
            }
        }

        internal Task WriteItemContentsAsync(XmlWriter dictWriter, SyndicationItem item)
        {
            return WriteItemContentsAsync(dictWriter, item, null);
        }

        internal async Task WriteItemContributorsToAsync(XmlWriter writer, Collection<SyndicationPerson> contributors)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);

            for (int i = 0; i < contributors.Count; ++i)
            {
                SyndicationPerson p = contributors[i];
                await WritePersonToAsync(writer, p, Atom10Constants.ContributorTag);
            }
        }

        internal Task WriteItemLastUpdatedTimeToAsync(XmlWriter writer, DateTimeOffset lastUpdatedTime)
        {
            if (lastUpdatedTime == DateTimeOffset.MinValue)
            {
                lastUpdatedTime = DateTimeOffset.UtcNow;
            }

            return writer.WriteElementStringAsync(Atom10Constants.UpdatedTag,
                Atom10Constants.Atom10Namespace,
                AsString(lastUpdatedTime));
        }

        internal async Task WriteLinkAsync(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            await writer.WriteStartElementAsync(Atom10Constants.LinkTag, Atom10Constants.Atom10Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }

            await link.WriteAttributeExtensionsAsync(writer, SyndicationVersions.Atom10);
            if (!string.IsNullOrEmpty(link.RelationshipType) && !link.AttributeExtensions.ContainsKey(s_atom10Relative))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.RelativeTag, link.RelationshipType);
            }

            if (!string.IsNullOrEmpty(link.MediaType) && !link.AttributeExtensions.ContainsKey(s_atom10Type))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.TypeTag, link.MediaType);
            }

            if (!string.IsNullOrEmpty(link.Title) && !link.AttributeExtensions.ContainsKey(s_atom10Title))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.TitleTag, link.Title);
            }

            if (link.Length != 0 && !link.AttributeExtensions.ContainsKey(s_atom10Length))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.LengthTag, Convert.ToString(link.Length, CultureInfo.InvariantCulture));
            }

            if (!link.AttributeExtensions.ContainsKey(s_atom10Href))
            {
                await writer.WriteAttributeStringAsync(Atom10Constants.HrefTag, FeedUtils.GetUriString(link.Uri));
            }

            await link.WriteElementExtensionsAsync(writer, SyndicationVersions.Atom10);
            await writer.WriteEndElementAsync();
        }

        protected override SyndicationFeed CreateFeedInstance()
        {
            return SyndicationFeedFormatter.CreateFeedInstance(_feedType);
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
            XmlReaderWrapper readerWrapper = XmlReaderWrapper.CreateFromReader(reader);
            await ReadItemFromAsync(readerWrapper, item, feed.BaseUri);
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
            XmlReaderWrapper readerWrapper = XmlReaderWrapper.CreateFromReader(reader);

            while (await readerWrapper.IsStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace))
            {
                items.Add(await ReadItemAsync(reader, feed));
            }

            return items;
        }

        protected virtual async Task WriteItemAsync(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            await writer.WriteStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
            await WriteItemContentsAsync(writer, item, feedBaseUri);
            await writer.WriteEndElementAsync();
        }

        protected virtual async Task WriteItemsAsync(XmlWriter writer, IEnumerable<SyndicationItem> items, Uri feedBaseUri)
        {
            if (items == null)
            {
                return;
            }

            foreach (SyndicationItem item in items)
            {
                await this.WriteItemAsync(writer, item, feedBaseUri);
            }
        }

        private async Task<TextSyndicationContent> ReadTextContentFromHelperAsync(XmlReaderWrapper reader, string type, string context, bool preserveAttributeExtensions)
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

                    throw new XmlException(string.Format(SR.Atom10SpecRequiresTextConstruct, context, type));
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
                        string value = await reader.GetValueAsync();
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
            string val = (kind == TextSyndicationContentKind.XHtml) ? await reader.ReadInnerXmlAsync() : stringParser(await reader.ReadElementStringAsync(), localName, nameSpace); // cant custom parse because its static
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

        private static DateTimeOffset DefaultDateFromString(string dateTimeString, string localName, string ns)
        {
            dateTimeString = dateTimeString.Trim();
            if (dateTimeString.Length < 20)
            {
                throw new XmlException(SR.ErrorParsingDateTime);
            }
            if (dateTimeString[19] == '.')
            {
                // remove any fractional seconds, we choose to ignore them
                int i = 20;
                while (dateTimeString.Length > i && char.IsDigit(dateTimeString[i]))
                {
                    ++i;
                }
                dateTimeString = dateTimeString.Substring(0, 19) + dateTimeString.Substring(i);
            }
            DateTimeOffset localTime;
            if (DateTimeOffset.TryParseExact(dateTimeString, Rfc3339LocalDateTimeFormat,
                CultureInfo.InvariantCulture.DateTimeFormat,
                DateTimeStyles.None, out localTime))
            {
                return localTime;
            }
            DateTimeOffset utcTime;
            if (DateTimeOffset.TryParseExact(dateTimeString, Rfc3339UTCDateTimeFormat,
                CultureInfo.InvariantCulture.DateTimeFormat,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utcTime))
            {
                return utcTime;
            }

            //throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDateTime));

            //if imposible to parse, return default date
            return new DateTimeOffset();
        }

        private Task<SyndicationCategory> ReadCategoryAsync(XmlReaderWrapper reader, SyndicationCategory category)
        {
            return ReadCategoryAsync(reader, category, this.Version, this.PreserveAttributeExtensions, this.PreserveElementExtensions, _maxExtensionSize);
        }

        private Task<SyndicationCategory> ReadCategoryFromAsync(XmlReaderWrapper reader, SyndicationFeed feed)

        {
            SyndicationCategory result = CreateCategory(feed);
            return ReadCategoryAsync(reader, result);
        }

        private async Task<SyndicationCategory> ReadCategoryFromAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            await ReadCategoryAsync(reader, result);
            return result;
        }

        private async Task<SyndicationContent> ReadContentFromAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            await MoveToStartElementAsync(reader);
            string type = reader.GetAttribute(Atom10Constants.TypeTag, string.Empty);

            SyndicationContent result;
            if (TryParseContent(reader, item, type, this.Version, out result))
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
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync());
                            }
                            else
                            {
                                //result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync());
                            }
                        }
                    }
                }

                await reader.ReadStartElementAsync();
                if (!isEmpty)
                {
                    await reader.ReadEndElementAsync();
                }

                return result;
            }
            else
            {
                return await ReadTextContentFromHelperAsync(reader, type, "//atom:feed/atom:entry/atom:content[@type]", _preserveAttributeExtensions);
            }
        }

        private async Task<SyndicationFeed> ReadFeedFromAsync(XmlReaderWrapper reader, SyndicationFeed result, bool isSourceFeed)
        {
            await reader.MoveToContentAsync();
            //fix to accept non contiguous items
            NullNotAllowedCollection<SyndicationItem> feedItems = new NullNotAllowedCollection<SyndicationItem>();

            bool elementIsEmpty = false;
            if (!isSourceFeed)
            {
                await MoveToStartElementAsync(reader);
                elementIsEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalName == "lang" && reader.NamespaceURI == XmlNs)
                        {
                            result.Language = await reader.GetValueAsync();
                        }
                        else if (reader.LocalName == "base" && reader.NamespaceURI == XmlNs)
                        {
                            result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, await reader.GetValueAsync());
                        }
                        else
                        {
                            string ns = reader.NamespaceURI;
                            string name = reader.LocalName;
                            if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                            {
                                continue;
                            }

                            string val = await reader.GetValueAsync();

                            if (!TryParseAttribute(name, ns, val, result, this.Version))
                            {
                                if (_preserveAttributeExtensions)
                                {
                                    result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), val);
                                }
                            }
                        }
                    }
                }
                await reader.ReadStartElementAsync();
            }

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;

            if (!elementIsEmpty)
            {
                try
                {
                    while (await reader.IsStartElementAsync())
                    {
                        if (await TryParseFeedElementFromAsync(reader, result))
                        {
                            // nothing, we parsed something, great
                        }
                        else if (await reader.IsStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace) && !isSourceFeed)
                        {
                            while (await reader.IsStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace))
                            {
                                feedItems.Add(await ReadItemAsync(reader, result));
                            }
                        }
                        else
                        {
                            if (!TryParseElement(reader, result, this.Version))
                            {
                                if (_preserveElementExtensions)
                                {
                                    if (buffer == null)
                                    {
                                        buffer = new XmlBuffer(_maxExtensionSize);
                                        extWriter = buffer.OpenSection(XmlDictionaryReaderQuotas.Max);
                                        extWriter.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                                    }

                                    await XmlReaderWrapper.WriteNodeAsync(extWriter, reader, false);
                                }
                                else
                                {
                                    await reader.SkipAsync();
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
                        ((IDisposable)extWriter).Dispose();
                    }
                }
            }
            if (!isSourceFeed)
            {
                await reader.ReadEndElementAsync(); // feed
            }

            return result;
        }

        private async Task ReadItemFromAsync(XmlReaderWrapper reader, SyndicationItem result, Uri feedBaseUri)
        {
            try
            {
                result.BaseUri = feedBaseUri;
                await MoveToStartElementAsync(reader);
                bool isEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (name == "base" && ns == XmlNs)
                        {
                            result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, await reader.GetValueAsync());
                            continue;
                        }

                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }

                        string val = await reader.GetValueAsync();
                        if (!TryParseAttribute(name, ns, val, result, this.Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                        }
                    }
                }
                await reader.ReadStartElementAsync();

                if (!isEmpty)
                {
                    XmlBuffer buffer = null;
                    XmlDictionaryWriter extWriter = null;
                    try
                    {
                        while (await reader.IsStartElementAsync())
                        {
                            if (await TryParseItemElementFromAsync(reader, result))
                            {
                                // nothing, we parsed something, great
                            }
                            else
                            {
                                if (!TryParseElement(reader, result, this.Version))
                                {
                                    if (_preserveElementExtensions)
                                    {
                                        var tuple = await CreateBufferIfRequiredAndWriteNodeAsync(buffer, extWriter, reader, _maxExtensionSize);
                                        buffer = tuple.Item1;
                                        extWriter = tuple.Item2;
                                    }
                                    else
                                    {
                                        await reader.SkipAsync();
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
                            ((IDisposable)extWriter).Dispose();
                        }
                    }
                    await reader.ReadEndElementAsync(); // item
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

        private async Task ReadLinkAsync(XmlReaderWrapper reader, SyndicationLink link, Uri baseUri)
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
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, await reader.GetValueAsync());
                    }
                    else if (reader.NamespaceURI == string.Empty)
                    {
                        switch (reader.LocalName)
                        {
                            case Atom10Constants.TypeTag:
                                mediaType = await reader.GetValueAsync();
                                break;
                            case Atom10Constants.RelativeTag:
                                relationship = await reader.GetValueAsync();
                                break;
                            case Atom10Constants.TitleTag:
                                title = await reader.GetValueAsync();
                                break;
                            case Atom10Constants.LengthTag:
                                lengthStr = await reader.GetValueAsync();
                                break;
                            case Atom10Constants.HrefTag:
                                val = await reader.GetValueAsync();
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
                            link.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync());
                        }
                    }
                }
            }

            long length = 0;
            if (!string.IsNullOrEmpty(lengthStr))
            {
                length = Convert.ToInt64(lengthStr, CultureInfo.InvariantCulture.NumberFormat);
            }

            await reader.ReadStartElementAsync();
            if (!isEmpty)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync())
                    {
                        if (TryParseElement(reader, link, this.Version))
                        {
                            continue;
                        }
                        else if (!_preserveElementExtensions)
                        {
                            await reader.SkipAsync();
                        }
                        else
                        {
                            if (buffer == null)
                            {
                                buffer = new XmlBuffer(_maxExtensionSize);
                                extWriter = buffer.OpenSection(XmlDictionaryReaderQuotas.Max);
                                extWriter.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                            }

                            await XmlReaderWrapper.WriteNodeAsync(extWriter, reader, false);
                        }
                    }

                    LoadElementExtensions(buffer, extWriter, link);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        ((IDisposable)extWriter).Dispose();
                    }
                }

                await reader.ReadEndElementAsync();
            }

            link.Length = length;
            link.MediaType = mediaType;
            link.RelationshipType = relationship;
            link.Title = title;
            link.Uri = (val != null) ? uriParser(val, UriKind.RelativeOrAbsolute, Atom10Constants.LinkTag, ns) /*new Uri(val, UriKind.RelativeOrAbsolute)*/ : null;
        }

        private async Task<SyndicationLink> ReadLinkFromAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            SyndicationLink result = CreateLink(feed);
            await ReadLinkAsync(reader, result, feed.BaseUri);
            return result;
        }

        private async Task<SyndicationLink> ReadLinkFromAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationLink result = CreateLink(item);
            await ReadLinkAsync(reader, result, item.BaseUri);
            return result;
        }

        private async Task<SyndicationPerson> ReadPersonFromAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            await ReadPersonFromAsync(reader, result);
            return result;
        }

        private async Task<SyndicationPerson> ReadPersonFromAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            await ReadPersonFromAsync(reader, result);
            return result;
        }

        private async Task ReadPersonFromAsync(XmlReaderWrapper reader, SyndicationPerson result)
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
                    string val = await reader.GetValueAsync();
                    if (!TryParseAttribute(name, ns, val, result, this.Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync());
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync();
            if (!isEmpty)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (await reader.IsStartElementAsync())
                    {
                        string name = reader.LocalName;
                        string ns = reader.NamespaceURI;
                        bool notHandled = false;

                        switch (name)
                        {
                            case Atom10Constants.NameTag:
                                result.Name = stringParser(await reader.ReadElementStringAsync(), Atom10Constants.NameTag, ns);
                                break;
                            case Atom10Constants.UriTag:
                                result.Uri = stringParser(await reader.ReadElementStringAsync(), Atom10Constants.UriTag, ns);
                                break;
                            case Atom10Constants.EmailTag:
                                result.Email = stringParser(await reader.ReadElementStringAsync(), Atom10Constants.EmailTag, ns);
                                break;
                            default:
                                notHandled = true;
                                break;
                        }

                        if (notHandled && !TryParseElement(reader, result, this.Version))
                        {
                            if (_preserveElementExtensions)
                            {
                                if (buffer == null)
                                {
                                    buffer = new XmlBuffer(_maxExtensionSize);
                                    extWriter = buffer.OpenSection(XmlDictionaryReaderQuotas.Max);
                                    extWriter.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                                }

                                await XmlReaderWrapper.WriteNodeAsync(extWriter, reader, false);
                            }
                            else
                            {
                                await reader.SkipAsync();
                            }
                        }
                    }

                    LoadElementExtensions(buffer, extWriter, result);
                }
                finally
                {
                    if (extWriter != null)
                    {
                        ((IDisposable)extWriter).Dispose();
                    }
                }

                await reader.ReadEndElementAsync();
            }
        }

        private Task<TextSyndicationContent> ReadTextContentFromAsync(XmlReaderWrapper reader, string context)
        {
            return ReadTextContentFromAsync(reader, context, this.PreserveAttributeExtensions);
        }

        private async Task WriteCategoriesToAsync(XmlWriter writer, Collection<SyndicationCategory> categories)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            for (int i = 0; i < categories.Count; ++i)
            {
                await WriteCategoryAsync(writer, categories[i], this.Version);
            }
        }

        private Task WriteFeedAsync(XmlWriter writer)
        {
            if (this.Feed == null)
            {
                throw new InvalidOperationException(SR.FeedFormatterDoesNotHaveFeed);
            }
            return WriteFeedToAsync(writer, this.Feed, false); //  isSourceFeed 
        }

        private async Task WriteFeedToAsync(XmlWriter writer, SyndicationFeed feed, bool isSourceFeed)
        {
            if (!isSourceFeed)
            {
                if (!string.IsNullOrEmpty(feed.Language))
                {
                    await writer.WriteAttributeStringAsync("xml", "lang", XmlNs, feed.Language);
                }
                if (feed.BaseUri != null)
                {
                    await writer.InternalWriteAttributeStringAsync("xml", "base", XmlNs, FeedUtils.GetUriString(feed.BaseUri));
                }
                await WriteAttributeExtensionsAsync(writer, feed, this.Version);
            }
            bool isElementRequired = !isSourceFeed;
            TextSyndicationContent title = feed.Title;
            if (isElementRequired)
            {
                title = title ?? new TextSyndicationContent(string.Empty);
            }
            await WriteContentToAsync(writer, Atom10Constants.TitleTag, title);
            await WriteContentToAsync(writer, Atom10Constants.SubtitleTag, feed.Description);
            string id = feed.Id;
            if (isElementRequired)
            {
                id = id ?? s_idGenerator.Next();
            }
            await WriteElementAsync(writer, Atom10Constants.IdTag, id);
            await WriteContentToAsync(writer, Atom10Constants.RightsTag, feed.Copyright);
            await WriteFeedLastUpdatedTimeToAsync(writer, feed.LastUpdatedTime, isElementRequired);
            await WriteCategoriesToAsync(writer, feed.Categories);
            if (feed.ImageUrl != null)
            {
                await WriteElementAsync(writer, Atom10Constants.LogoTag, feed.ImageUrl.ToString());
            }
            await WriteFeedAuthorsToAsync(writer, feed.Authors);
            await WriteFeedContributorsToAsync(writer, feed.Contributors);
            await WriteElementAsync(writer, Atom10Constants.GeneratorTag, feed.Generator);

            if (feed.IconImage != null)
            {
                await WriteElementAsync(writer, Atom10Constants.IconTag, feed.IconImage.AbsoluteUri);
            }

            for (int i = 0; i < feed.Links.Count; ++i)
            {
                await WriteLinkAsync(writer, feed.Links[i], feed.BaseUri);
            }

            await WriteElementExtensionsAsync(writer, feed, this.Version);

            if (!isSourceFeed)
            {
                await WriteItemsAsync(writer, feed.Items, feed.BaseUri);
            }
        }

        private async Task WriteItemContentsAsync(XmlWriter dictWriter, SyndicationItem item, Uri feedBaseUri)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(feedBaseUri, item.BaseUri);
            if (baseUriToWrite != null)
            {
                await dictWriter.InternalWriteAttributeStringAsync("xml", "base", XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            await WriteAttributeExtensionsAsync(dictWriter, item, this.Version);

            string id = item.Id ?? s_idGenerator.Next();
            await WriteElementAsync(dictWriter, Atom10Constants.IdTag, id);

            TextSyndicationContent title = item.Title ?? new TextSyndicationContent(string.Empty);
            await WriteContentToAsync(dictWriter, Atom10Constants.TitleTag, title);
            await WriteContentToAsync(dictWriter, Atom10Constants.SummaryTag, item.Summary);
            if (item.PublishDate != DateTimeOffset.MinValue)
            {
                await dictWriter.WriteElementStringAsync(Atom10Constants.PublishedTag,
                    Atom10Constants.Atom10Namespace,
                    AsString(item.PublishDate));
            }
            await WriteItemLastUpdatedTimeToAsync(dictWriter, item.LastUpdatedTime);
            await WriteItemAuthorsToAsync(dictWriter, item.Authors);
            await WriteItemContributorsToAsync(dictWriter, item.Contributors);
            for (int i = 0; i < item.Links.Count; ++i)
            {
                await WriteLinkAsync(dictWriter, item.Links[i], item.BaseUri);
            }
            await WriteCategoriesToAsync(dictWriter, item.Categories);
            await WriteContentToAsync(dictWriter, Atom10Constants.ContentTag, item.Content);
            await WriteContentToAsync(dictWriter, Atom10Constants.RightsTag, item.Copyright);
            if (item.SourceFeed != null)
            {
                await dictWriter.WriteStartElementAsync(Atom10Constants.SourceFeedTag, Atom10Constants.Atom10Namespace);
                await WriteFeedToAsync(dictWriter, item.SourceFeed, true); //  isSourceFeed 
                await dictWriter.WriteEndElementAsync();
            }
            await WriteElementExtensionsAsync(dictWriter, item, this.Version);
        }

        private async Task WritePersonToAsync(XmlWriter writer, SyndicationPerson p, string elementName)
        {
            await writer.WriteStartElementAsync(elementName, Atom10Constants.Atom10Namespace);
            await WriteAttributeExtensionsAsync(writer, p, this.Version);
            await WriteElementAsync(writer, Atom10Constants.NameTag, p.Name);
            if (!string.IsNullOrEmpty(p.Uri))
            {
                await writer.WriteElementStringAsync(Atom10Constants.UriTag, Atom10Constants.Atom10Namespace, p.Uri);
            }
            if (!string.IsNullOrEmpty(p.Email))
            {
                await writer.WriteElementStringAsync(Atom10Constants.EmailTag, Atom10Constants.Atom10Namespace, p.Email);
            }
            await WriteElementExtensionsAsync(writer, p, this.Version);
            await writer.WriteEndElementAsync();
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
