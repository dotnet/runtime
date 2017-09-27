// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;


    [XmlRoot(ElementName = Rss20Constants.RssTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20FeedFormatter : SyndicationFeedFormatter
    {
        private static readonly XmlQualifiedName s_rss20Domain = new XmlQualifiedName(Rss20Constants.DomainTag, string.Empty);
        private static readonly XmlQualifiedName s_rss20Length = new XmlQualifiedName(Rss20Constants.LengthTag, string.Empty);
        private static readonly XmlQualifiedName s_rss20Type = new XmlQualifiedName(Rss20Constants.TypeTag, string.Empty);
        private static readonly XmlQualifiedName s_rss20Url = new XmlQualifiedName(Rss20Constants.UrlTag, string.Empty);
        private static List<string> acceptedDays = new List<string> { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
        private const string Rfc822OutputLocalDateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss zzz";
        private const string Rfc822OutputUtcDateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss Z";

        private Atom10FeedFormatter _atomSerializer;
        private Type _feedType;
        private int _maxExtensionSize;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;
        private bool _serializeExtensionsAsAtom;

        //Custom Parsers
        //   value, localname , ns , result
        public Func<string, string, string, string> StringParser { get; set; } = DefaultStringParser;
        public Func<string, string, string, DateTimeOffset> DateParser { get; set; } = DefaultDateParser;
        public Func<string, string, string, Uri> UriParser { get; set; } = DefaultUriParser;

        static private string DefaultStringParser(string value, string localName, string ns)
        {
            return value;
        }

        static private Uri DefaultUriParser(string value, string localName, string ns)
        {
            return new Uri(value, UriKind.RelativeOrAbsolute);
        }

        private async Task<bool> OnReadImage(XmlReaderWrapper reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync();
            string localName = string.Empty;

            while (await reader.IsStartElementAsync())
            {
                if (await reader.IsStartElementAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace))
                {
                    result.ImageUrl = UriParser(await reader.ReadElementStringAsync(), Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace);
                }
                else if(await reader.IsStartElementAsync(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace))
                {
                    result.ImageLink = UriParser(await reader.ReadElementStringAsync(), Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace);
                }
                else if (await reader.IsStartElementAsync(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace))
                {
                    result.ImageTitle = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync(), Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace));
                }
            }
            await reader.ReadEndElementAsync(); // image
            return true;
        }

        public Rss20FeedFormatter()
            : this(typeof(SyndicationFeed))
        {
        }

        public Rss20FeedFormatter(Type feedTypeToCreate)
            : base()
        {
            if (feedTypeToCreate == null)
            {
                throw new ArgumentNullException(nameof(feedTypeToCreate));
            }
            if (!typeof(SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw new ArgumentException(string.Format(SR.InvalidObjectTypePassed, nameof(feedTypeToCreate), nameof(SyndicationFeed)));
            }
            _serializeExtensionsAsAtom = true;
            _maxExtensionSize = int.MaxValue;
            _preserveElementExtensions = true;
            _preserveAttributeExtensions = true;
            _atomSerializer = new Atom10FeedFormatter(feedTypeToCreate);
            _feedType = feedTypeToCreate;
        }

        public Rss20FeedFormatter(SyndicationFeed feedToWrite)
            : this(feedToWrite, true)
        {
        }

        public Rss20FeedFormatter(SyndicationFeed feedToWrite, bool serializeExtensionsAsAtom)
            : base(feedToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _serializeExtensionsAsAtom = serializeExtensionsAsAtom;
            _maxExtensionSize = int.MaxValue;
            _preserveElementExtensions = true;
            _preserveAttributeExtensions = true;
            _atomSerializer = new Atom10FeedFormatter(this.Feed);
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

        public bool SerializeExtensionsAsAtom
        {
            get { return _serializeExtensionsAsAtom; }
            set { _serializeExtensionsAsAtom = value; }
        }

        public override string Version
        {
            get { return SyndicationVersions.Rss20; }
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

            return reader.IsStartElement(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
        }

        public override Task ReadFromAsync(XmlReader reader, CancellationToken ct)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(string.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
            }

            SetFeed(CreateFeedInstance());
            return ReadXmlAsync(XmlReaderWrapper.CreateFromReader(reader), this.Feed);
        }

        private Task WriteXmlAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            return WriteFeedAsync(writer);
        }

        public override async Task WriteToAsync(XmlWriter writer, CancellationToken ct)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
            await WriteFeedAsync(writer);
            await writer.WriteEndElementAsync();
        }

        protected internal override void SetFeed(SyndicationFeed feed)
        {
            base.SetFeed(feed);
            _atomSerializer.SetFeed(this.Feed);
        }

        private async Task ReadItemFromAsync(XmlReaderWrapper reader, SyndicationItem result, Uri feedBaseUri)
        {
            result.BaseUri = feedBaseUri;
            await reader.MoveToContentAsync();
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;
                    if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
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
                            result.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync();

            if (!isEmpty)
            {
                string fallbackAlternateLink = null;
                XmlDictionaryWriter extWriter = null;
                bool readAlternateLink = false;
                try
                {
                    XmlBuffer buffer = null;
                    while (await reader.IsStartElementAsync())
                    {
                        bool notHandled = false;
                        if (await reader.MoveToContentAsync() == XmlNodeType.Element && reader.NamespaceURI == Rss20Constants.Rss20Namespace)
                        {
                            switch (reader.LocalName)
                            {
                                case Rss20Constants.TitleTag:
                                    result.Title = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync(), Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace));
                                    break;

                                case Rss20Constants.LinkTag:
                                    result.Links.Add(await ReadAlternateLinkAsync(reader, result.BaseUri));
                                    readAlternateLink = true;
                                    break;

                                case Rss20Constants.DescriptionTag:
                                    result.Summary = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync(), Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace));
                                    break;

                                case Rss20Constants.AuthorTag:
                                    result.Authors.Add(await ReadPersonAsync(reader, result));
                                    break;

                                case Rss20Constants.CategoryTag:
                                    result.Categories.Add(await ReadCategoryAsync(reader, result));
                                    break;

                                case Rss20Constants.EnclosureTag:
                                    result.Links.Add(await ReadMediaEnclosureAsync(reader, result.BaseUri));
                                    break;

                                case Rss20Constants.GuidTag:
                                    {
                                        bool isPermalink = true;
                                        string permalinkString = reader.GetAttribute(Rss20Constants.IsPermaLinkTag, Rss20Constants.Rss20Namespace);
                                        if (permalinkString != null && permalinkString.Equals("false",StringComparison.OrdinalIgnoreCase))
                                        {
                                            isPermalink = false;
                                        }
                                        string localName = reader.LocalName;
                                        string namespaceUri = reader.NamespaceURI;
                                        result.Id = StringParser(await reader.ReadElementStringAsync(), localName, namespaceUri);
                                        if (isPermalink)
                                        {
                                            fallbackAlternateLink = result.Id;
                                        }

                                        break;
                                    }

                                case Rss20Constants.PubDateTag:
                                    {
                                        bool canReadContent = !reader.IsEmptyElement;
                                        await reader.ReadStartElementAsync();
                                        if (canReadContent)
                                        {
                                            string str = await reader.ReadStringAsync();
                                            if (!string.IsNullOrEmpty(str))
                                            {
                                                result.PublishDate = DateParser(str, reader.LocalName, reader.NamespaceURI);
                                            }

                                            await reader.ReadEndElementAsync();
                                        }

                                        break;
                                    }

                                case Rss20Constants.SourceTag:
                                    {
                                        SyndicationFeed feed = new SyndicationFeed();
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
                                                if (name == Rss20Constants.UrlTag && ns == Rss20Constants.Rss20Namespace)
                                                {
                                                    feed.Links.Add(SyndicationLink.CreateSelfLink(UriParser(val, Rss20Constants.UrlTag,ns)));
                                                }
                                                else if (!FeedUtils.IsXmlns(name, ns))
                                                {
                                                    if (_preserveAttributeExtensions)
                                                    {
                                                        feed.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                                                    }
                                                }
                                            }
                                        }
                                        string localName = reader.LocalName;
                                        string namespaceUri = reader.NamespaceURI;
                                        string feedTitle = StringParser(await reader.ReadElementStringAsync(), localName, namespaceUri);
                                        feed.Title = new TextSyndicationContent(feedTitle);
                                        result.SourceFeed = feed;

                                        break;
                                    }

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
                            bool parsedExtension = _serializeExtensionsAsAtom && _atomSerializer.TryParseItemElementFromAsync(reader, result).Result;

                            if (!parsedExtension)
                            {
                                parsedExtension = TryParseElement(reader, result, this.Version);
                            }

                            if (!parsedExtension)
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

                if (!readAlternateLink && fallbackAlternateLink != null)
                {
                    result.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(fallbackAlternateLink, UriKind.RelativeOrAbsolute)));
                    readAlternateLink = true;
                }

                // if there's no content and no alternate link set the summary as the item content
                if (result.Content == null && !readAlternateLink)
                {
                    result.Content = result.Summary;
                    result.Summary = null;
                }
            }
        }

        internal Task ReadItemFromAsync(XmlReaderWrapper reader, SyndicationItem result)
        {
            return ReadItemFromAsync(reader, result, null);
        }

        internal Task WriteItemContentsAsync(XmlWriter writer, SyndicationItem item)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);
            return WriteItemContentsAsync(writer, item, null);
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
            await ReadItemFromAsync(readerWrapper, item, feed.BaseUri); // delegate => ItemParser(reader,item,feed.BaseUri);//
            return item;
        }

        protected virtual async Task WriteItemAsync(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
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

        private static string NormalizeTimeZone(string rfc822TimeZone, out bool isUtc)
        {
            isUtc = false;
            // return a string in "-08:00" format
            if (rfc822TimeZone[0] == '+' || rfc822TimeZone[0] == '-')
            {
                // the time zone is supposed to be 4 digits but some feeds omit the initial 0
                StringBuilder result = new StringBuilder(rfc822TimeZone);
                if (result.Length == 4)
                {
                    // the timezone is +/-HMM. Convert to +/-HHMM
                    result.Insert(1, '0');
                }
                result.Insert(3, ':');
                return result.ToString();
            }
            switch (rfc822TimeZone)
            {
                case "UT":
                case "Z":
                    isUtc = true;
                    return "-00:00";
                case "GMT":
                    return "-00:00";
                case "A":
                    return "-01:00";
                case "B":
                    return "-02:00";
                case "C":
                    return "-03:00";
                case "D":
                case "EDT":
                    return "-04:00";
                case "E":
                case "EST":
                case "CDT":
                    return "-05:00";
                case "F":
                case "CST":
                case "MDT":
                    return "-06:00";
                case "G":
                case "MST":
                case "PDT":
                    return "-07:00";
                case "H":
                case "PST":
                    return "-08:00";
                case "I":
                    return "-09:00";
                case "K":
                    return "-10:00";
                case "L":
                    return "-11:00";
                case "M":
                    return "-12:00";
                case "N":
                    return "+01:00";
                case "O":
                    return "+02:00";
                case "P":
                    return "+03:00";
                case "Q":
                    return "+04:00";
                case "R":
                    return "+05:00";
                case "S":
                    return "+06:00";
                case "T":
                    return "+07:00";
                case "U":
                    return "+08:00";
                case "V":
                    return "+09:00";
                case "W":
                    return "+10:00";
                case "X":
                    return "+11:00";
                case "Y":
                    return "+12:00";
                default:
                    return "";
            }
        }
        
        private async Task ReadSkipHoursAsync(XmlReaderWrapper reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync();

            while (await reader.IsStartElementAsync())
            {

                if(reader.LocalName == Rss20Constants.HourTag)
                {
                    string val = StringParser(await reader.ReadElementStringAsync(), Rss20Constants.HourTag, Rss20Constants.Rss20Namespace);
                    int hour = int.Parse(val);
                    bool parsed = false;
                    parsed = int.TryParse(val,NumberStyles.Integer,NumberFormatInfo.InvariantInfo,out hour);

                    if(parsed == false)
                    {
                        throw new ArgumentException("The number on skip hours must be an integer betwen 0 and 23.");
                    }

                    if (hour < 0 || hour > 23)
                    {
                        throw new ArgumentException("The hour can't be lower than 0 or greater than 23.");
                    }

                    result.SkipHours.Add(hour);
                }
                else
                {
                    await reader.SkipAsync();
                }

            }

            await reader.ReadEndElementAsync();
        }


        private bool checkDay(string day)
        {
            if (acceptedDays.Contains(day.ToLower()))
            {
                return true;
            }

            return false;
        }

        private async Task ReadSkipDaysAsync(XmlReaderWrapper reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync();

            while (await reader.IsStartElementAsync())
            {
                if (reader.LocalName == Rss20Constants.DayTag)
                {
                    string day = StringParser(await reader.ReadElementStringAsync(), Rss20Constants.DayTag, Rss20Constants.Rss20Namespace);

                    //Check if the day is actually an accepted day.
                    if (checkDay(day))
                    {
                        result.SkipDays.Add(day);
                    }
                }
                else
                {
                    await reader.SkipAsync();
                }
            }

            await reader.ReadEndElementAsync();
        }

        internal static void RemoveExtraWhiteSpaceAtStart(StringBuilder stringBuilder)
        {
            int i = 0;
            while (i < stringBuilder.Length)
            {
                if (!char.IsWhiteSpace(stringBuilder[i]))
                {
                    break;
                }
                ++i;
            }
            if (i > 0)
            {
                stringBuilder.Remove(0, i);
            }
        }
       
        private static void ReplaceMultipleWhiteSpaceWithSingleWhiteSpace(StringBuilder builder)
        {
            int index = 0;
            int whiteSpaceStart = -1;
            while (index < builder.Length)
            {
                if (char.IsWhiteSpace(builder[index]))
                {
                    if (whiteSpaceStart < 0)
                    {
                        whiteSpaceStart = index;
                        // normalize all white spaces to be ' ' so that the date time parsing works
                        builder[index] = ' ';
                    }
                }
                else if (whiteSpaceStart >= 0)
                {
                    if (index > whiteSpaceStart + 1)
                    {
                        // there are at least 2 spaces... replace by 1
                        builder.Remove(whiteSpaceStart, index - whiteSpaceStart - 1);
                        index = whiteSpaceStart + 1;
                    }
                    whiteSpaceStart = -1;
                }
                ++index;
            }
            // we have already trimmed the start and end so there cannot be a trail of white spaces in the end
            Debug.Assert(builder.Length == 0 || builder[builder.Length - 1] != ' ', "The string builder doesnt end in a white space");
        }

        private string AsString(DateTimeOffset dateTime)
        {
            if (dateTime.Offset == Atom10FeedFormatter.zeroOffset)
            {
                return dateTime.ToUniversalTime().ToString(Rfc822OutputUtcDateTimeFormat, CultureInfo.InvariantCulture);
            }
            else
            {
                StringBuilder sb = new StringBuilder(dateTime.ToString(Rfc822OutputLocalDateTimeFormat, CultureInfo.InvariantCulture));
                // the zzz in Rfc822OutputLocalDateTimeFormat makes the timezone e.g. "-08:00" but we require e.g. "-0800" without the ':'
                sb.Remove(sb.Length - 3, 1);
                return sb.ToString();
            }
        }
        
        private async Task<SyndicationLink> ReadAlternateLinkAsync(XmlReaderWrapper reader, Uri baseUri)
        {
            SyndicationLink link = new SyndicationLink();
            link.BaseUri = baseUri;
            link.RelationshipType = Atom10Constants.AlternateTag;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == Atom10FeedFormatter.XmlNs)
                    {
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, await reader.GetValueAsync());
                    }
                    else if (!FeedUtils.IsXmlns(reader.LocalName, reader.NamespaceURI))
                    {
                        if (this.PreserveAttributeExtensions)
                        {
                            link.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync());
                        }
                    }
                }
            }
            string localName = reader.LocalName;
            string namespaceUri = reader.NamespaceURI;
            link.Uri = UriParser(await reader.ReadElementStringAsync(), localName, namespaceUri);//new Uri(uri, UriKind.RelativeOrAbsolute);
            return link;
        }
        
        private async Task<SyndicationCategory> ReadCategoryAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            SyndicationCategory result = CreateCategory(feed);
            await ReadCategoryAsync(reader, result);
            return result;
        }
        
        private async Task ReadCategoryAsync(XmlReaderWrapper reader, SyndicationCategory category)
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
                    if (name == Rss20Constants.DomainTag && ns == Rss20Constants.Rss20Namespace)
                    {
                        category.Scheme = val;
                    }
                    else if (!TryParseAttribute(name, ns, val, category, this.Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            category.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace);

            if (!isEmpty)
            {
                category.Name = StringParser(await reader.ReadStringAsync(), reader.LocalName, Rss20Constants.Rss20Namespace);
                await reader.ReadEndElementAsync();
            }
        }
        
        private async Task<SyndicationCategory> ReadCategoryAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            await ReadCategoryAsync(reader, result);
            return result;
        }


        private async Task<SyndicationLink> ReadMediaEnclosureAsync(XmlReaderWrapper reader, Uri baseUri)
        {
            SyndicationLink link = new SyndicationLink();
            link.BaseUri = baseUri;
            link.RelationshipType = Rss20Constants.EnclosureTag;
            bool isEmptyElement = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;
                    if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
                    {
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, await reader.GetValueAsync());
                        continue;
                    }
                    if (FeedUtils.IsXmlns(name, ns))
                    {
                        continue;
                    }
                    string val = await reader.GetValueAsync();
                    if (name == Rss20Constants.UrlTag && ns == Rss20Constants.Rss20Namespace)
                    {
                        link.Uri = new Uri(val, UriKind.RelativeOrAbsolute);
                    }
                    else if (name == Rss20Constants.TypeTag && ns == Rss20Constants.Rss20Namespace)
                    {
                        link.MediaType = val;
                    }
                    else if (name == Rss20Constants.LengthTag && ns == Rss20Constants.Rss20Namespace)
                    {
                        link.Length = !string.IsNullOrEmpty(val) ? Convert.ToInt64(val, CultureInfo.InvariantCulture.NumberFormat) : 0;
                    }
                    else if (!FeedUtils.IsXmlns(name, ns))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            link.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace);

            if (!isEmptyElement)
            {
                await reader.ReadEndElementAsync();
            }

            return link;
        }

        private async Task<SyndicationPerson> ReadPersonAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            await ReadPersonAsync(reader, result);
            return result;
        }

        private async Task ReadPersonAsync(XmlReaderWrapper reader, SyndicationPerson person)
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
                    if (!TryParseAttribute(name, ns, val, person, this.Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            person.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync();
            if (!isEmpty)
            {
                string email = StringParser(await reader.ReadStringAsync(),reader.LocalName,reader.NamespaceURI);
                await reader.ReadEndElementAsync();
                person.Email = email;
            }
        }

        private async Task<SyndicationPerson> ReadPersonAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            await ReadPersonAsync(reader, result);
            return result;
        }

        private bool checkTextInput(SyndicationTextInput textInput)
        {
            //All textInput items are required, we check if all items were instantiated.
            return (textInput.Description != null && textInput.title != null && textInput.name != null && textInput.link != null);
        }

        private async Task readTextInputTag(XmlReaderWrapper reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync();

            SyndicationTextInput textInput = new SyndicationTextInput();
            string val = String.Empty;

            while (await reader.IsStartElementAsync())
            {
                string name = reader.LocalName;
                string namespaceUri = reader.NamespaceURI;
                val = StringParser(await reader.ReadElementStringAsync(), name, Rss20Constants.Rss20Namespace);

                switch (name)
                {
                    case Rss20Constants.DescriptionTag:
                        textInput.Description = val;
                        break;

                    case Rss20Constants.TitleTag:
                        textInput.title = val;
                        break;

                    case Rss20Constants.LinkTag:
                        textInput.link = new SyndicationLink(UriParser(val, name, namespaceUri));
                        break;

                    case Rss20Constants.NameTag:
                        textInput.name = val;
                        break;

                    default:
                        //ignore!
                        break;
                }
            }

            if(checkTextInput(textInput) == true)
            {
                result.TextInput = textInput;
            }

            await reader.ReadEndElementAsync();
        }

        private async Task ReadXmlAsync(XmlReaderWrapper reader, SyndicationFeed result)
        {
            string baseUri = null;
            await reader.MoveToContentAsync();

            string version = reader.GetAttribute(Rss20Constants.VersionTag, Rss20Constants.Rss20Namespace);
            if (version != Rss20Constants.Version)
            {
                throw new NotSupportedException(FeedUtils.AddLineInfo(reader, (string.Format(SR.UnsupportedRssVersion, version))));
            }

            if (reader.AttributeCount > 1)
            {
                string tmp = reader.GetAttribute("base", Atom10FeedFormatter.XmlNs);
                if (!string.IsNullOrEmpty(tmp))
                {
                    baseUri = tmp;
                }
            }

            await reader.ReadStartElementAsync();
            await reader.MoveToContentAsync();

            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;

                    if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
                    {
                        baseUri = await reader.GetValueAsync();
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
                            result.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(baseUri))
            {
                result.BaseUri = new Uri(baseUri, UriKind.RelativeOrAbsolute);
            }

            bool areAllItemsRead = true;
            await reader.ReadStartElementAsync(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace);

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;
            NullNotAllowedCollection<SyndicationItem> feedItems = new NullNotAllowedCollection<SyndicationItem>();

            try
            {
                while (await reader.IsStartElementAsync())
                {
                    bool notHandled = false;
                    if (await reader.MoveToContentAsync() == XmlNodeType.Element && reader.NamespaceURI == Rss20Constants.Rss20Namespace)
                    {
                        switch (reader.LocalName)
                        {
                            case Rss20Constants.TitleTag:
                                result.Title = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync(), Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace));
                                break;

                            case Rss20Constants.LinkTag:
                                result.Links.Add(await ReadAlternateLinkAsync(reader, result.BaseUri));
                                break;

                            case Rss20Constants.DescriptionTag:
                                result.Description = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync(), Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace));
                                break;

                            case Rss20Constants.LanguageTag:

                                result.Language = StringParser(await reader.ReadElementStringAsync(), Rss20Constants.LanguageTag, Rss20Constants.Rss20Namespace);
                                break;

                            case Rss20Constants.CopyrightTag:
                                result.Copyright = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync(), Rss20Constants.CopyrightTag, Rss20Constants.Rss20Namespace));
                                break;

                            case Rss20Constants.ManagingEditorTag:
                                result.Authors.Add(await ReadPersonAsync(reader, result));
                                break;

                            case Rss20Constants.LastBuildDateTag:
                                {
                                    bool canReadContent = !reader.IsEmptyElement;
                                    await reader.ReadStartElementAsync();
                                    if (canReadContent)
                                    {
                                        string str = await reader.ReadStringAsync();

                                        if (!string.IsNullOrEmpty(str))
                                        {
                                            result.LastUpdatedTime = DateParser(str, Rss20Constants.LastBuildDateTag, reader.NamespaceURI);
                                        }

                                        await reader.ReadEndElementAsync();
                                    }

                                    break;
                                }

                            case Rss20Constants.CategoryTag:
                                result.Categories.Add(await ReadCategoryAsync(reader, result));
                                break;

                            case Rss20Constants.GeneratorTag:
                                result.Generator = StringParser(await reader.ReadElementStringAsync(), Rss20Constants.GeneratorTag, Rss20Constants.Rss20Namespace);
                                break;

                            case Rss20Constants.ImageTag:
                                {
                                    await OnReadImage(reader, result);
                                    break;
                                }

                            case Rss20Constants.ItemTag:
                                {
                                    NullNotAllowedCollection<SyndicationItem> items = new NullNotAllowedCollection<SyndicationItem>();
                                    while (await reader.IsStartElementAsync(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace))
                                    {
                                        feedItems.Add(await ReadItemAsync(reader, result));
                                    }


                                    areAllItemsRead = true;
                                    break;
                                }

                            //Optional tags
                            case Rss20Constants.DocumentationTag:
                                result.Documentation = await ReadAlternateLinkAsync(reader, result.BaseUri);
                                break;

                            case Rss20Constants.TimeToLiveTag:
                                string value = StringParser(await reader.ReadElementStringAsync(), Rss20Constants.TimeToLiveTag, Rss20Constants.Rss20Namespace);
                                int timeToLive = int.Parse(value);
                                result.TimeToLive = timeToLive;
                                break;

                            case Rss20Constants.TextInputTag:
                                await readTextInputTag(reader, result);
                                break;

                            case Rss20Constants.SkipHoursTag:
                                await ReadSkipHoursAsync(reader, result);
                                break;

                            case Rss20Constants.SkipDaysTag:
                                await ReadSkipDaysAsync(reader, result);
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
                        bool parsedExtension = _serializeExtensionsAsAtom && await _atomSerializer.TryParseFeedElementFromAsync(reader, result);

                        if (!parsedExtension)
                        {
                            parsedExtension = TryParseElement(reader, result, this.Version);
                        }

                        if (!parsedExtension)
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

                    if (!areAllItemsRead)
                    {
                        break;
                    }
                }

                //asign all read items to feed items.
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
            if (areAllItemsRead)
            {
                await reader.ReadEndElementAsync(); // channel   
                await reader.ReadEndElementAsync(); // rss
            }
        }

        private async Task WriteAlternateLinkAsync(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            await writer.WriteStartElementAsync(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            await link.WriteAttributeExtensionsAsync(writer, SyndicationVersions.Rss20);
            await writer.WriteStringAsync(FeedUtils.GetUriString(link.Uri));
            await writer.WriteEndElementAsync();
        }
        
        private async Task WriteCategoryAsync(XmlWriter writer, SyndicationCategory category)
        {
            if (category == null)
            {
                return;
            }
            await writer.WriteStartElementAsync(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace);
            await WriteAttributeExtensionsAsync(writer, category, this.Version);
            if (!string.IsNullOrEmpty(category.Scheme) && !category.AttributeExtensions.ContainsKey(s_rss20Domain))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.DomainTag, Rss20Constants.Rss20Namespace, category.Scheme);
            }
            await writer.WriteStringAsync(category.Name);
            await writer.WriteEndElementAsync();
        }
        
        private async Task WriteFeedAsync(XmlWriter writer)
        {
            if (this.Feed == null)
            {
                throw new InvalidOperationException(SR.FeedFormatterDoesNotHaveFeed);
            }
            if (_serializeExtensionsAsAtom)
            {
                await writer.InternalWriteAttributeStringAsync("xmlns", Atom10Constants.Atom10Prefix, null, Atom10Constants.Atom10Namespace);
            }
            await writer.WriteAttributeStringAsync(Rss20Constants.VersionTag, Rss20Constants.Version);
            await writer.WriteStartElementAsync(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace);
            if (this.Feed.BaseUri != null)
            {
                await writer.InternalWriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(this.Feed.BaseUri));
            }
            await WriteAttributeExtensionsAsync(writer, this.Feed, this.Version);
            string title = this.Feed.Title != null ? this.Feed.Title.Text : string.Empty;
            await writer.WriteElementStringAsync(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, title);

            SyndicationLink alternateLink = null;
            for (int i = 0; i < this.Feed.Links.Count; ++i)
            {
                if (this.Feed.Links[i].RelationshipType == Atom10Constants.AlternateTag)
                {
                    alternateLink = this.Feed.Links[i];
                    await WriteAlternateLinkAsync(writer, alternateLink, this.Feed.BaseUri);
                    break;
                }
            }

            string description = this.Feed.Description != null ? this.Feed.Description.Text : string.Empty;
            await writer.WriteElementStringAsync(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace, description);

            if (this.Feed.Language != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.LanguageTag, this.Feed.Language);
            }

            if (this.Feed.Copyright != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.CopyrightTag, Rss20Constants.Rss20Namespace, this.Feed.Copyright.Text);
            }

            // if there's a single author with an email address, then serialize as the managingEditor
            // else serialize the authors as Atom extensions
            if ((this.Feed.Authors.Count == 1) && (this.Feed.Authors[0].Email != null))
            {
                await WritePersonAsync(writer, Rss20Constants.ManagingEditorTag, this.Feed.Authors[0]);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteFeedAuthorsToAsync(writer, this.Feed.Authors);
                }
            }

            if (this.Feed.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                await writer.WriteStartElementAsync(Rss20Constants.LastBuildDateTag);
                await writer.WriteStringAsync(AsString(this.Feed.LastUpdatedTime));
                await writer.WriteEndElementAsync();
            }

            for (int i = 0; i < this.Feed.Categories.Count; ++i)
            {
                await WriteCategoryAsync(writer, this.Feed.Categories[i]);
            }

            if (!string.IsNullOrEmpty(this.Feed.Generator))
            {
                await writer.WriteElementStringAsync(Rss20Constants.GeneratorTag, this.Feed.Generator);
            }

            if (this.Feed.Contributors.Count > 0)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteFeedContributorsToAsync(writer, this.Feed.Contributors);
                }
            }

            if (this.Feed.ImageUrl != null)
            {
                await writer.WriteStartElementAsync(Rss20Constants.ImageTag);
                await writer.WriteElementStringAsync(Rss20Constants.UrlTag, FeedUtils.GetUriString(this.Feed.ImageUrl));

                string imageTitle = Feed.ImageTitle == null ? title : Feed.ImageTitle.Text;
                await writer.WriteElementStringAsync(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, imageTitle);

                string imgAlternateLink = alternateLink != null ? FeedUtils.GetUriString(alternateLink.Uri) : string.Empty;

                string imageLink = Feed.ImageLink == null ? imgAlternateLink : FeedUtils.GetUriString(Feed.ImageLink);
                await writer.WriteElementStringAsync(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace, imageLink);
                await writer.WriteEndElementAsync(); // image
            }

            //Optional spec items
            //time to live
            if(this.Feed.TimeToLive != 0)
            {
                await writer.WriteElementStringAsync(Rss20Constants.TimeToLiveTag, this.Feed.TimeToLive.ToString());
            }

            //skiphours
            if(this.Feed.SkipHours.Count > 0)
            {
                await writer.WriteStartElementAsync(Rss20Constants.SkipHoursTag);

                foreach(int hour in this.Feed.SkipHours)
                {
                    writer.WriteElementString(Rss20Constants.HourTag,hour.ToString());
                }

                await writer.WriteEndElementAsync();
            }

            //skipDays
            if(this.Feed.SkipDays.Count > 0)
            {
                await writer.WriteStartElementAsync(Rss20Constants.SkipDaysTag);

                foreach(string day in this.Feed.SkipDays)
                {
                    await writer.WriteElementStringAsync(Rss20Constants.DayTag,day);
                }

                await writer.WriteEndElementAsync();
            }

            //textinput
            if(this.Feed.TextInput != null)
            {
                await writer.WriteStartElementAsync(Rss20Constants.TextInputTag);

                await writer.WriteElementStringAsync(Rss20Constants.DescriptionTag, this.Feed.TextInput.Description);
                await writer.WriteElementStringAsync(Rss20Constants.TitleTag, this.Feed.TextInput.title);
                await writer.WriteElementStringAsync(Rss20Constants.LinkTag, this.Feed.TextInput.link.GetAbsoluteUri().ToString());
                await writer.WriteElementStringAsync(Rss20Constants.NameTag, this.Feed.TextInput.name);

                await writer.WriteEndElementAsync();
            }
            
            if (_serializeExtensionsAsAtom)
            {
                await _atomSerializer.WriteElementAsync(writer, Atom10Constants.IdTag, this.Feed.Id);

                // dont write out the 1st alternate link since that would have been written out anyway
                bool isFirstAlternateLink = true;
                for (int i = 0; i < this.Feed.Links.Count; ++i)
                {
                    if (this.Feed.Links[i].RelationshipType == Atom10Constants.AlternateTag && isFirstAlternateLink)
                    {
                        isFirstAlternateLink = false;
                        continue;
                    }
                    await _atomSerializer.WriteLinkAsync(writer, this.Feed.Links[i], this.Feed.BaseUri);
                }
            }

            await WriteElementExtensionsAsync(writer, this.Feed, this.Version);
            await WriteItemsAsync(writer, this.Feed.Items, this.Feed.BaseUri);
            await writer.WriteEndElementAsync(); // channel
        }
        
        private async Task WriteItemContentsAsync(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(feedBaseUri, item.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.InternalWriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            await WriteAttributeExtensionsAsync(writer, item, this.Version);
            string guid = item.Id ?? string.Empty;
            bool isPermalink = false;
            SyndicationLink firstAlternateLink = null;
            for (int i = 0; i < item.Links.Count; ++i)
            {
                if (item.Links[i].RelationshipType == Atom10Constants.AlternateTag)
                {
                    if (firstAlternateLink == null)
                    {
                        firstAlternateLink = item.Links[i];
                    }
                    if (guid == FeedUtils.GetUriString(item.Links[i].Uri))
                    {
                        isPermalink = true;
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(guid))
            {
                await writer.WriteStartElementAsync(Rss20Constants.GuidTag);
                if (isPermalink)
                {
                    await writer.WriteAttributeStringAsync(Rss20Constants.IsPermaLinkTag, "true");
                }
                else
                {
                    await writer.WriteAttributeStringAsync(Rss20Constants.IsPermaLinkTag, "false");
                }
                await writer.WriteStringAsync(guid);
                await writer.WriteEndElementAsync();
            }
            if (firstAlternateLink != null)
            {
                await WriteAlternateLinkAsync(writer, firstAlternateLink, (item.BaseUri != null ? item.BaseUri : feedBaseUri));
            }

            if (item.Authors.Count == 1 && !string.IsNullOrEmpty(item.Authors[0].Email))
            {
                await WritePersonAsync(writer, Rss20Constants.AuthorTag, item.Authors[0]);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteItemAuthorsToAsync(writer, item.Authors);
                }
            }

            for (int i = 0; i < item.Categories.Count; ++i)
            {
                await WriteCategoryAsync(writer, item.Categories[i]);
            }

            bool serializedTitle = false;
            if (item.Title != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.TitleTag, item.Title.Text);
                serializedTitle = true;
            }

            bool serializedContentAsDescription = false;
            TextSyndicationContent summary = item.Summary;
            if (summary == null)
            {
                summary = (item.Content as TextSyndicationContent);
                serializedContentAsDescription = (summary != null);
            }
            // the spec requires the wire to have a title or a description
            if (!serializedTitle && summary == null)
            {
                summary = new TextSyndicationContent(string.Empty);
            }
            if (summary != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace, summary.Text);
            }

            if (item.SourceFeed != null)
            {
                await writer.WriteStartElementAsync(Rss20Constants.SourceTag, Rss20Constants.Rss20Namespace);
                await WriteAttributeExtensionsAsync(writer, item.SourceFeed, this.Version);
                SyndicationLink selfLink = null;
                for (int i = 0; i < item.SourceFeed.Links.Count; ++i)
                {
                    if (item.SourceFeed.Links[i].RelationshipType == Atom10Constants.SelfTag)
                    {
                        selfLink = item.SourceFeed.Links[i];
                        break;
                    }
                }
                if (selfLink != null && !item.SourceFeed.AttributeExtensions.ContainsKey(s_rss20Url))
                {
                    await writer.WriteAttributeStringAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace, FeedUtils.GetUriString(selfLink.Uri));
                }
                string title = (item.SourceFeed.Title != null) ? item.SourceFeed.Title.Text : string.Empty;
                await writer.WriteStringAsync(title);
                await writer.WriteEndElementAsync();
            }

            if (item.PublishDate > DateTimeOffset.MinValue)
            {
                await writer.WriteElementStringAsync(Rss20Constants.PubDateTag, Rss20Constants.Rss20Namespace, AsString(item.PublishDate));
            }

            // serialize the enclosures
            SyndicationLink firstEnclosureLink = null;
            bool passedFirstAlternateLink = false;

            for (int i = 0; i < item.Links.Count; ++i)
            {
                if (item.Links[i].RelationshipType == Rss20Constants.EnclosureTag)
                {
                    if (firstEnclosureLink == null)
                    {
                        firstEnclosureLink = item.Links[i];
                        await WriteMediaEnclosureAsync(writer, item.Links[i], item.BaseUri);
                        continue;
                    }
                }
                else if (item.Links[i].RelationshipType == Atom10Constants.AlternateTag)
                {
                    if (!passedFirstAlternateLink)
                    {
                        passedFirstAlternateLink = true;
                        continue;
                    }
                }
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteLinkAsync(writer, item.Links[i], item.BaseUri);
                }
            }


            if (item.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteItemLastUpdatedTimeToAsync(writer, item.LastUpdatedTime);
                }
            }

            if (_serializeExtensionsAsAtom)
            {
                await _atomSerializer.WriteContentToAsync(writer, Atom10Constants.RightsTag, item.Copyright);
            }

            if (!serializedContentAsDescription)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteContentToAsync(writer, Atom10Constants.ContentTag, item.Content);
                }
            }

            if (item.Contributors.Count > 0)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteItemContributorsToAsync(writer, item.Contributors);
                }
            }

            await WriteElementExtensionsAsync(writer, item, this.Version);
        }
        
        private async Task WriteMediaEnclosureAsync(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            await writer.WriteStartElementAsync(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.InternalWriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            await link.WriteAttributeExtensionsAsync(writer, SyndicationVersions.Rss20);
            if (!link.AttributeExtensions.ContainsKey(s_rss20Url))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace, FeedUtils.GetUriString(link.Uri));
            }
            if (link.MediaType != null && !link.AttributeExtensions.ContainsKey(s_rss20Type))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.TypeTag, Rss20Constants.Rss20Namespace, link.MediaType);
            }
            if (link.Length != 0 && !link.AttributeExtensions.ContainsKey(s_rss20Length))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.LengthTag, Rss20Constants.Rss20Namespace, Convert.ToString(link.Length, CultureInfo.InvariantCulture));
            }
            await writer.WriteEndElementAsync();
        }
        
        private async Task WritePersonAsync(XmlWriter writer, string elementTag, SyndicationPerson person)
        {
            await writer.WriteStartElementAsync(elementTag, Rss20Constants.Rss20Namespace);
            await WriteAttributeExtensionsAsync(writer, person, this.Version);
            await writer.WriteStringAsync(person.Email);
            await writer.WriteEndElementAsync();
        }

        private static bool OriginalDateParser(string dateTimeString, out DateTimeOffset dto)
        {
            StringBuilder dateTimeStringBuilder = new StringBuilder(dateTimeString.Trim());
            if (dateTimeStringBuilder.Length < 18)
            {
                return false;
            }
            
            int timeZoneStartIndex;
            for (timeZoneStartIndex = dateTimeStringBuilder.Length-1; dateTimeStringBuilder[timeZoneStartIndex] != ' '; timeZoneStartIndex--);
            timeZoneStartIndex++;

            string timeZoneSuffix = dateTimeStringBuilder.ToString().Substring(timeZoneStartIndex);
            dateTimeStringBuilder.Remove(timeZoneStartIndex, dateTimeStringBuilder.Length - timeZoneStartIndex);
            bool isUtc;
            dateTimeStringBuilder.Append(NormalizeTimeZone(timeZoneSuffix, out isUtc));
            string wellFormattedString = dateTimeStringBuilder.ToString();

            DateTimeOffset theTime;
            string[] parseFormat = 
            {
                "ddd, dd MMMM yyyy HH:mm:ss zzz",
                "dd MMMM yyyy HH:mm:ss zzz",
                "ddd, dd MMM yyyy HH:mm:ss zzz",
                "dd MMM yyyy HH:mm:ss zzz",
                
                "ddd, dd MMMM yyyy HH:mm zzz",
                "dd MMMM yyyy HH:mm zzz",
                "ddd, dd MMM yyyy HH:mm zzz",
                "dd MMM yyyy HH:mm zzz"
            };

            if (DateTimeOffset.TryParseExact(wellFormattedString, parseFormat,
                CultureInfo.InvariantCulture.DateTimeFormat,
                (isUtc ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None), out theTime))
            {
                dto = theTime;
                return true;
            }

            return false;
        }

        // Custom parsers
        public static DateTimeOffset DefaultDateParser(string dateTimeString, string localName, string ns)
        {
            bool parsed = false;
            DateTimeOffset dto;
            parsed = DateTimeOffset.TryParse(dateTimeString, out dto);
            if (parsed)
                return dto;


            //original parser here
            parsed = OriginalDateParser(dateTimeString,out dto);
            if (parsed)
                return dto;

            //Impossible to parse - using a default date;
            return new DateTimeOffset();
        }
    }

    [XmlRoot(ElementName = Rss20Constants.RssTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20FeedFormatter<TSyndicationFeed> : Rss20FeedFormatter
        where TSyndicationFeed : SyndicationFeed, new()
    {
        // constructors
        public Rss20FeedFormatter()
            : base(typeof(TSyndicationFeed))
        {
        }
        public Rss20FeedFormatter(TSyndicationFeed feedToWrite)
            : base(feedToWrite)
        {
        }
        public Rss20FeedFormatter(TSyndicationFeed feedToWrite, bool serializeExtensionsAsAtom)
            : base(feedToWrite, serializeExtensionsAsAtom)
        {
        }

        protected override SyndicationFeed CreateFeedInstance()
        {
            return new TSyndicationFeed();
        }
    }


    internal class ItemParseOptions
    {
        public bool readItemsAtLeastOnce;
        public bool areAllItemsRead;

        public ItemParseOptions(bool readItemsAtLeastOnce, bool areAllItemsRead)
        {
            this.readItemsAtLeastOnce = readItemsAtLeastOnce;
            this.areAllItemsRead = areAllItemsRead;
        }
    }
}
