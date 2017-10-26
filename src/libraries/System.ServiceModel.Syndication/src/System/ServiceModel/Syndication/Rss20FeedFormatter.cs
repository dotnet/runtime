// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;


    [XmlRoot(ElementName = Rss20Constants.RssTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20FeedFormatter : SyndicationFeedFormatter, IXmlSerializable
    {
        private static readonly XmlQualifiedName s_rss20Domain = new XmlQualifiedName(Rss20Constants.DomainTag, string.Empty);
        private static readonly XmlQualifiedName s_rss20Length = new XmlQualifiedName(Rss20Constants.LengthTag, string.Empty);
        private static readonly XmlQualifiedName s_rss20Type = new XmlQualifiedName(Rss20Constants.TypeTag, string.Empty);
        private static readonly XmlQualifiedName s_rss20Url = new XmlQualifiedName(Rss20Constants.UrlTag, string.Empty);
        private static List<string> s_acceptedDays = new List<string> { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
        private const string Rfc822OutputLocalDateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss zzz";
        private const string Rfc822OutputUtcDateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss Z";

        private Atom10FeedFormatter _atomSerializer;
        private Type _feedType;
        private int _maxExtensionSize;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;
        private bool _serializeExtensionsAsAtom;

        private async Task<bool> OnReadImage(XmlReader reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync().ConfigureAwait(false);
            string localName = string.Empty;

            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                if (await reader.IsStartElementAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false))
                {
                    result.ImageUrl = UriParser(await reader.ReadElementStringAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute, Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace);
                }
                else if (await reader.IsStartElementAsync(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false))
                {
                    result.ImageLink = UriParser(await reader.ReadElementStringAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute, Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace);
                }
                else if (await reader.IsStartElementAsync(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false))
                {
                    result.ImageTitle = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace));
                }
            }
            await reader.ReadEndElementAsync().ConfigureAwait(false); // image
            return true;
        }

        public Rss20FeedFormatter() : this(typeof (SyndicationFeed))
        {
        }

        public Rss20FeedFormatter(Type feedTypeToCreate) : base()
        {
            if (feedTypeToCreate == null)
            {
                throw new ArgumentNullException(nameof(feedTypeToCreate));
            }
            if (!typeof (SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw new ArgumentException(SR.Format(SR.InvalidObjectTypePassed, nameof(feedTypeToCreate), nameof(SyndicationFeed)));
            }
            _serializeExtensionsAsAtom = true;
            _maxExtensionSize = int.MaxValue;
            _preserveElementExtensions = true;
            _preserveAttributeExtensions = true;
            _atomSerializer = new Atom10FeedFormatter(feedTypeToCreate);
            _feedType = feedTypeToCreate;
            DateTimeParser = DateTimeHelper.CreateRss20DateTimeParser();
        }

        public Rss20FeedFormatter(SyndicationFeed feedToWrite) : this(feedToWrite, true)
        {
        }

        public Rss20FeedFormatter(SyndicationFeed feedToWrite, bool serializeExtensionsAsAtom) : base(feedToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _serializeExtensionsAsAtom = serializeExtensionsAsAtom;
            _maxExtensionSize = int.MaxValue;
            _preserveElementExtensions = true;
            _preserveAttributeExtensions = true;
            _atomSerializer = new Atom10FeedFormatter(Feed);
            _feedType = feedToWrite.GetType();
            DateTimeParser = DateTimeHelper.CreateRss20DateTimeParser();
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
            get { return _feedType; }
        }

        public override bool CanRead(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return reader.IsStartElement(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
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

            ReadFeedAsync(reader).GetAwaiter().GetResult();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteFeedAsync(XmlWriterWrapper.CreateFromWriter(writer)).GetAwaiter().GetResult();
        }


        public override Task ReadFromAsync(XmlReader reader, CancellationToken ct)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(SR.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
            }

            SetFeed(CreateFeedInstance());
            return ReadXmlAsync(XmlReaderWrapper.CreateFromReader(reader), Feed);
        }

        private Task WriteXmlAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            return WriteFeedAsync(writer);
        }

        public override void ReadFrom(XmlReader reader)
        {
            ReadFromAsync(reader, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override void WriteTo(XmlWriter writer)
        {
            WriteToAsync(writer, CancellationToken.None).GetAwaiter().GetResult();
        }

        public override async Task WriteToAsync(XmlWriter writer, CancellationToken ct)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
            await WriteFeedAsync(writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        protected internal override void SetFeed(SyndicationFeed feed)
        {
            base.SetFeed(feed);
            _atomSerializer.SetFeed(Feed);
        }

        private async Task ReadItemFromAsync(XmlReader reader, SyndicationItem result, Uri feedBaseUri)
        {
            result.BaseUri = feedBaseUri;
            await reader.MoveToContentAsync().ConfigureAwait(false);
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;
                    if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
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
                            result.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync().ConfigureAwait(false);

            if (!isEmpty)
            {
                string fallbackAlternateLink = null;
                XmlDictionaryWriter extWriter = null;
                bool readAlternateLink = false;
                try
                {
                    XmlBuffer buffer = null;
                    while (await reader.IsStartElementAsync().ConfigureAwait(false))
                    {
                        bool notHandled = false;
                        if (await reader.MoveToContentAsync().ConfigureAwait(false) == XmlNodeType.Element && reader.NamespaceURI == Rss20Constants.Rss20Namespace)
                        {
                            switch (reader.LocalName)
                            {
                                case Rss20Constants.TitleTag:
                                    result.Title = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace));
                                    break;

                                case Rss20Constants.LinkTag:
                                    result.Links.Add(await ReadAlternateLinkAsync(reader, result.BaseUri).ConfigureAwait(false));
                                    readAlternateLink = true;
                                    break;

                                case Rss20Constants.DescriptionTag:
                                    result.Summary = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace));
                                    break;

                                case Rss20Constants.AuthorTag:
                                    result.Authors.Add(await ReadPersonAsync(reader, result).ConfigureAwait(false));
                                    break;

                                case Rss20Constants.CategoryTag:
                                    result.Categories.Add(await ReadCategoryAsync(reader, result).ConfigureAwait(false));
                                    break;

                                case Rss20Constants.EnclosureTag:
                                    result.Links.Add(await ReadMediaEnclosureAsync(reader, result.BaseUri).ConfigureAwait(false));
                                    break;

                                case Rss20Constants.GuidTag:
                                {
                                    bool isPermalink = true;
                                    string permalinkString = reader.GetAttribute(Rss20Constants.IsPermaLinkTag, Rss20Constants.Rss20Namespace);
                                    if (permalinkString != null && permalinkString.Equals("false", StringComparison.OrdinalIgnoreCase))
                                    {
                                        isPermalink = false;
                                    }
                                    string localName = reader.LocalName;
                                    string namespaceUri = reader.NamespaceURI;
                                    result.Id = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), localName, namespaceUri);
                                    if (isPermalink)
                                    {
                                        fallbackAlternateLink = result.Id;
                                    }

                                    break;
                                }

                                case Rss20Constants.PubDateTag:
                                {
                                    bool canReadContent = !reader.IsEmptyElement;
                                    await reader.ReadStartElementAsync().ConfigureAwait(false);
                                    if (canReadContent)
                                    {
                                        string str = await reader.ReadStringAsync().ConfigureAwait(false);
                                        if (!string.IsNullOrEmpty(str))
                                        {
                                            result.PublishDate = DateTimeParser(str, reader.LocalName, reader.NamespaceURI);
                                        }

                                        await reader.ReadEndElementAsync().ConfigureAwait(false);
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
                                            string val = await reader.GetValueAsync().ConfigureAwait(false);
                                            if (name == Rss20Constants.UrlTag && ns == Rss20Constants.Rss20Namespace)
                                            {
                                                feed.Links.Add(SyndicationLink.CreateSelfLink(UriParser(val, UriKind.RelativeOrAbsolute, Rss20Constants.UrlTag, ns)));
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
                                    string feedTitle = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), localName, namespaceUri);
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
                            bool parsedExtension = _serializeExtensionsAsAtom && _atomSerializer.TryParseItemElementFromAsync(reader, result).GetAwaiter().GetResult();

                            if (!parsedExtension)
                            {
                                parsedExtension = TryParseElement(reader, result, Version);
                            }

                            if (!parsedExtension)
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

        internal Task ReadItemFromAsync(XmlReader reader, SyndicationItem result)
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

        protected virtual SyndicationItem ReadItem(XmlReader reader, SyndicationFeed feed)
        {
            return ReadItemAsync(reader, feed).GetAwaiter().GetResult();
        }

        protected virtual IEnumerable<SyndicationItem> ReadItems(XmlReader reader, SyndicationFeed feed, out bool areAllItemsRead)
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
            while (reader.IsStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace))
            {
                items.Add(ReadItem(reader, feed));
            }
            areAllItemsRead = true;
            return items;
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

            await writer.WriteStartElementAsync(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
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

        private Task ReadFeedAsync(XmlReader reader)
        {
            SetFeed(CreateFeedInstance());
            return ReadXmlAsync(XmlReaderWrapper.CreateFromReader(reader), Feed);
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

        private async Task ReadSkipHoursAsync(XmlReader reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync().ConfigureAwait(false);

            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                if (reader.LocalName == Rss20Constants.HourTag)
                {
                    string val = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.HourTag, Rss20Constants.Rss20Namespace);
                    int hour = int.Parse(val);
                    bool parsed = false;
                    parsed = int.TryParse(val, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out hour);

                    if (parsed == false)
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
                    await reader.SkipAsync().ConfigureAwait(false);
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);
        }


        private bool checkDay(string day)
        {
            if (s_acceptedDays.Contains(day.ToLower()))
            {
                return true;
            }

            return false;
        }

        private async Task ReadSkipDaysAsync(XmlReader reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync().ConfigureAwait(false);

            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                if (reader.LocalName == Rss20Constants.DayTag)
                {
                    string day = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.DayTag, Rss20Constants.Rss20Namespace);

                    //Check if the day is actually an accepted day.
                    if (checkDay(day))
                    {
                        result.SkipDays.Add(day);
                    }
                }
                else
                {
                    await reader.SkipAsync().ConfigureAwait(false);
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);
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

        private async Task<SyndicationLink> ReadAlternateLinkAsync(XmlReader reader, Uri baseUri)
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
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                    }
                    else if (!FeedUtils.IsXmlns(reader.LocalName, reader.NamespaceURI))
                    {
                        if (PreserveAttributeExtensions)
                        {
                            link.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), await reader.GetValueAsync().ConfigureAwait(false));
                        }
                    }
                }
            }
            string localName = reader.LocalName;
            string namespaceUri = reader.NamespaceURI;
            link.Uri = UriParser(await reader.ReadElementStringAsync().ConfigureAwait(false), UriKind.RelativeOrAbsolute, localName, namespaceUri);
            return link;
        }

        private async Task<SyndicationCategory> ReadCategoryAsync(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationCategory result = CreateCategory(feed);
            await ReadCategoryAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private async Task ReadCategoryAsync(XmlReader reader, SyndicationCategory category)
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
                    if (name == Rss20Constants.DomainTag && ns == Rss20Constants.Rss20Namespace)
                    {
                        category.Scheme = val;
                    }
                    else if (!TryParseAttribute(name, ns, val, category, Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            category.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);

            if (!isEmpty)
            {
                category.Name = StringParser(await reader.ReadStringAsync().ConfigureAwait(false), reader.LocalName, Rss20Constants.Rss20Namespace);
                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }
        }

        private async Task<SyndicationCategory> ReadCategoryAsync(XmlReader reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            await ReadCategoryAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private async Task<SyndicationLink> ReadMediaEnclosureAsync(XmlReader reader, Uri baseUri)
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
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, await reader.GetValueAsync().ConfigureAwait(false));
                        continue;
                    }
                    if (FeedUtils.IsXmlns(name, ns))
                    {
                        continue;
                    }
                    string val = await reader.GetValueAsync().ConfigureAwait(false);
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

            await reader.ReadStartElementAsync(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);

            if (!isEmptyElement)
            {
                await reader.ReadEndElementAsync().ConfigureAwait(false);
            }

            return link;
        }

        private async Task<SyndicationPerson> ReadPersonAsync(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            await ReadPersonAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private async Task ReadPersonAsync(XmlReader reader, SyndicationPerson person)
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
                    if (!TryParseAttribute(name, ns, val, person, Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            person.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                    }
                }
            }

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            if (!isEmpty)
            {
                string email = StringParser(await reader.ReadStringAsync().ConfigureAwait(false), reader.LocalName, reader.NamespaceURI);
                await reader.ReadEndElementAsync().ConfigureAwait(false);
                person.Email = email;
            }
        }

        private async Task<SyndicationPerson> ReadPersonAsync(XmlReader reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            await ReadPersonAsync(reader, result).ConfigureAwait(false);
            return result;
        }

        private bool checkTextInput(SyndicationTextInput textInput)
        {
            //All textInput items are required, we check if all items were instantiated.
            return (textInput.Description != null && textInput.title != null && textInput.name != null && textInput.link != null);
        }

        private async Task ReadTextInputTag(XmlReader reader, SyndicationFeed result)
        {
            await reader.ReadStartElementAsync().ConfigureAwait(false);

            SyndicationTextInput textInput = new SyndicationTextInput();
            string val = String.Empty;

            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                string name = reader.LocalName;
                string namespaceUri = reader.NamespaceURI;
                val = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), name, Rss20Constants.Rss20Namespace);

                switch (name)
                {
                    case Rss20Constants.DescriptionTag:
                        textInput.Description = val;
                        break;

                    case Rss20Constants.TitleTag:
                        textInput.title = val;
                        break;

                    case Rss20Constants.LinkTag:
                        textInput.link = new SyndicationLink(UriParser(val, UriKind.RelativeOrAbsolute, name, namespaceUri));
                        break;

                    case Rss20Constants.NameTag:
                        textInput.name = val;
                        break;

                    default:
                        //ignore!
                        break;
                }
            }

            if (checkTextInput(textInput) == true)
            {
                result.TextInput = textInput;
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);
        }

        private async Task ReadXmlAsync(XmlReader reader, SyndicationFeed result)
        {
            string baseUri = null;
            await reader.MoveToContentAsync().ConfigureAwait(false);

            string version = reader.GetAttribute(Rss20Constants.VersionTag, Rss20Constants.Rss20Namespace);
            if (version != Rss20Constants.Version)
            {
                throw new NotSupportedException(FeedUtils.AddLineInfo(reader, (SR.Format(SR.UnsupportedRssVersion, version))));
            }

            if (reader.AttributeCount > 1)
            {
                string tmp = reader.GetAttribute("base", Atom10FeedFormatter.XmlNs);
                if (!string.IsNullOrEmpty(tmp))
                {
                    baseUri = tmp;
                }
            }

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            await reader.MoveToContentAsync().ConfigureAwait(false);

            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string ns = reader.NamespaceURI;
                    string name = reader.LocalName;

                    if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
                    {
                        baseUri = await reader.GetValueAsync().ConfigureAwait(false);
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
            await reader.ReadStartElementAsync(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;
            NullNotAllowedCollection<SyndicationItem> feedItems = new NullNotAllowedCollection<SyndicationItem>();

            try
            {
                while (await reader.IsStartElementAsync().ConfigureAwait(false))
                {
                    bool notHandled = false;
                    if (await reader.MoveToContentAsync().ConfigureAwait(false) == XmlNodeType.Element && reader.NamespaceURI == Rss20Constants.Rss20Namespace)
                    {
                        switch (reader.LocalName)
                        {
                            case Rss20Constants.TitleTag:
                                result.Title = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace));
                                break;

                            case Rss20Constants.LinkTag:
                                result.Links.Add(await ReadAlternateLinkAsync(reader, result.BaseUri).ConfigureAwait(false));
                                break;

                            case Rss20Constants.DescriptionTag:
                                result.Description = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace));
                                break;

                            case Rss20Constants.LanguageTag:

                                result.Language = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.LanguageTag, Rss20Constants.Rss20Namespace);
                                break;

                            case Rss20Constants.CopyrightTag:
                                result.Copyright = new TextSyndicationContent(StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.CopyrightTag, Rss20Constants.Rss20Namespace));
                                break;

                            case Rss20Constants.ManagingEditorTag:
                                result.Authors.Add(await ReadPersonAsync(reader, result).ConfigureAwait(false));
                                break;

                            case Rss20Constants.LastBuildDateTag:
                            {
                                bool canReadContent = !reader.IsEmptyElement;
                                await reader.ReadStartElementAsync().ConfigureAwait(false);
                                if (canReadContent)
                                {
                                    string str = await reader.ReadStringAsync().ConfigureAwait(false);

                                    if (!string.IsNullOrEmpty(str))
                                    {
                                        result.LastUpdatedTime = DateTimeParser(str, Rss20Constants.LastBuildDateTag, reader.NamespaceURI);
                                    }

                                    await reader.ReadEndElementAsync().ConfigureAwait(false);
                                }

                                break;
                            }

                            case Rss20Constants.CategoryTag:
                                result.Categories.Add(await ReadCategoryAsync(reader, result).ConfigureAwait(false));
                                break;

                            case Rss20Constants.GeneratorTag:
                                result.Generator = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.GeneratorTag, Rss20Constants.Rss20Namespace);
                                break;

                            case Rss20Constants.ImageTag:
                            {
                                await OnReadImage(reader, result).ConfigureAwait(false);
                                break;
                            }

                            case Rss20Constants.ItemTag:
                            {
                                NullNotAllowedCollection<SyndicationItem> items = new NullNotAllowedCollection<SyndicationItem>();
                                while (await reader.IsStartElementAsync(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false))
                                {
                                    feedItems.Add(await ReadItemAsync(reader, result).ConfigureAwait(false));
                                }


                                areAllItemsRead = true;
                                break;
                            }

                            //Optional tags
                            case Rss20Constants.DocumentationTag:
                                result.Documentation = await ReadAlternateLinkAsync(reader, result.BaseUri).ConfigureAwait(false);
                                break;

                            case Rss20Constants.TimeToLiveTag:
                                string value = StringParser(await reader.ReadElementStringAsync().ConfigureAwait(false), Rss20Constants.TimeToLiveTag, Rss20Constants.Rss20Namespace);
                                int timeToLive = int.Parse(value);
                                result.TimeToLive = timeToLive;
                                break;

                            case Rss20Constants.TextInputTag:
                                await ReadTextInputTag(reader, result).ConfigureAwait(false);
                                break;

                            case Rss20Constants.SkipHoursTag:
                                await ReadSkipHoursAsync(reader, result).ConfigureAwait(false);
                                break;

                            case Rss20Constants.SkipDaysTag:
                                await ReadSkipDaysAsync(reader, result).ConfigureAwait(false);
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
                        bool parsedExtension = _serializeExtensionsAsAtom && await _atomSerializer.TryParseFeedElementFromAsync(reader, result).ConfigureAwait(false);

                        if (!parsedExtension)
                        {
                            parsedExtension = TryParseElement(reader, result, Version);
                        }

                        if (!parsedExtension)
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
                    ((IDisposable) extWriter).Dispose();
                }
            }
            if (areAllItemsRead)
            {
                await reader.ReadEndElementAsync().ConfigureAwait(false); // channel   
                await reader.ReadEndElementAsync().ConfigureAwait(false); // rss
            }
        }

        private async Task WriteAlternateLinkAsync(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            await writer.WriteStartElementAsync(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite)).ConfigureAwait(false);
            }
            await link.WriteAttributeExtensionsAsync(writer, SyndicationVersions.Rss20).ConfigureAwait(false);
            await writer.WriteStringAsync(FeedUtils.GetUriString(link.Uri)).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        private async Task WriteCategoryAsync(XmlWriter writer, SyndicationCategory category)
        {
            if (category == null)
            {
                return;
            }
            await writer.WriteStartElementAsync(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
            await WriteAttributeExtensionsAsync(writer, category, Version).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(category.Scheme) && !category.AttributeExtensions.ContainsKey(s_rss20Domain))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.DomainTag, Rss20Constants.Rss20Namespace, category.Scheme).ConfigureAwait(false);
            }
            await writer.WriteStringAsync(category.Name).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        private async Task WriteFeedAsync(XmlWriter writer)
        {
            if (Feed == null)
            {
                throw new InvalidOperationException(SR.FeedFormatterDoesNotHaveFeed);
            }
            if (_serializeExtensionsAsAtom)
            {
                await writer.WriteAttributeStringAsync("xmlns", Atom10Constants.Atom10Prefix, null, Atom10Constants.Atom10Namespace).ConfigureAwait(false);
            }
            await writer.WriteAttributeStringAsync(Rss20Constants.VersionTag, Rss20Constants.Version).ConfigureAwait(false);
            await writer.WriteStartElementAsync(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
            if (Feed.BaseUri != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(Feed.BaseUri)).ConfigureAwait(false);
            }
            await WriteAttributeExtensionsAsync(writer, Feed, Version).ConfigureAwait(false);
            string title = Feed.Title != null ? Feed.Title.Text : string.Empty;
            await writer.WriteElementStringAsync(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, title).ConfigureAwait(false);

            SyndicationLink alternateLink = null;
            for (int i = 0; i < Feed.Links.Count; ++i)
            {
                if (Feed.Links[i].RelationshipType == Atom10Constants.AlternateTag)
                {
                    alternateLink = Feed.Links[i];
                    await WriteAlternateLinkAsync(writer, alternateLink, Feed.BaseUri).ConfigureAwait(false);
                    break;
                }
            }

            string description = Feed.Description != null ? Feed.Description.Text : string.Empty;
            await writer.WriteElementStringAsync(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace, description).ConfigureAwait(false);

            if (Feed.Language != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.LanguageTag, Feed.Language).ConfigureAwait(false);
            }

            if (Feed.Copyright != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.CopyrightTag, Rss20Constants.Rss20Namespace, Feed.Copyright.Text).ConfigureAwait(false);
            }

            // if there's a single author with an email address, then serialize as the managingEditor
            // else serialize the authors as Atom extensions
            if ((Feed.Authors.Count == 1) && (Feed.Authors[0].Email != null))
            {
                await WritePersonAsync(writer, Rss20Constants.ManagingEditorTag, Feed.Authors[0]).ConfigureAwait(false);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteFeedAuthorsToAsync(writer, Feed.Authors).ConfigureAwait(false);
                }
            }

            if (Feed.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                await writer.WriteStartElementAsync(Rss20Constants.LastBuildDateTag).ConfigureAwait(false);
                await writer.WriteStringAsync(AsString(Feed.LastUpdatedTime)).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            for (int i = 0; i < Feed.Categories.Count; ++i)
            {
                await WriteCategoryAsync(writer, Feed.Categories[i]).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(Feed.Generator))
            {
                await writer.WriteElementStringAsync(Rss20Constants.GeneratorTag, Feed.Generator).ConfigureAwait(false);
            }

            if (Feed.Contributors.Count > 0)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteFeedContributorsToAsync(writer, Feed.Contributors).ConfigureAwait(false);
                }
            }

            if (Feed.ImageUrl != null)
            {
                await writer.WriteStartElementAsync(Rss20Constants.ImageTag).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Rss20Constants.UrlTag, FeedUtils.GetUriString(Feed.ImageUrl)).ConfigureAwait(false);

                string imageTitle = Feed.ImageTitle == null ? title : Feed.ImageTitle.Text;
                await writer.WriteElementStringAsync(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, imageTitle).ConfigureAwait(false);

                string imgAlternateLink = alternateLink != null ? FeedUtils.GetUriString(alternateLink.Uri) : string.Empty;

                string imageLink = Feed.ImageLink == null ? imgAlternateLink : FeedUtils.GetUriString(Feed.ImageLink);
                await writer.WriteElementStringAsync(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace, imageLink).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false); // image
            }

            //Optional spec items
            //time to live
            if (Feed.TimeToLive != 0)
            {
                await writer.WriteElementStringAsync(Rss20Constants.TimeToLiveTag, Feed.TimeToLive.ToString()).ConfigureAwait(false);
            }

            //skiphours
            if (Feed.SkipHours.Count > 0)
            {
                await writer.WriteStartElementAsync(Rss20Constants.SkipHoursTag).ConfigureAwait(false);

                foreach (int hour in Feed.SkipHours)
                {
                    writer.WriteElementString(Rss20Constants.HourTag, hour.ToString());
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            //skipDays
            if (Feed.SkipDays.Count > 0)
            {
                await writer.WriteStartElementAsync(Rss20Constants.SkipDaysTag).ConfigureAwait(false);

                foreach (string day in Feed.SkipDays)
                {
                    await writer.WriteElementStringAsync(Rss20Constants.DayTag, day).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            //textinput
            if (Feed.TextInput != null)
            {
                await writer.WriteStartElementAsync(Rss20Constants.TextInputTag).ConfigureAwait(false);

                await writer.WriteElementStringAsync(Rss20Constants.DescriptionTag, Feed.TextInput.Description).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Rss20Constants.TitleTag, Feed.TextInput.title).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Rss20Constants.LinkTag, Feed.TextInput.link.GetAbsoluteUri().ToString()).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Rss20Constants.NameTag, Feed.TextInput.name).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (_serializeExtensionsAsAtom)
            {
                await _atomSerializer.WriteElementAsync(writer, Atom10Constants.IdTag, Feed.Id).ConfigureAwait(false);

                // dont write out the 1st alternate link since that would have been written out anyway
                bool isFirstAlternateLink = true;
                for (int i = 0; i < Feed.Links.Count; ++i)
                {
                    if (Feed.Links[i].RelationshipType == Atom10Constants.AlternateTag && isFirstAlternateLink)
                    {
                        isFirstAlternateLink = false;
                        continue;
                    }
                    await _atomSerializer.WriteLinkAsync(writer, Feed.Links[i], Feed.BaseUri).ConfigureAwait(false);
                }
            }

            await WriteElementExtensionsAsync(writer, Feed, Version).ConfigureAwait(false);
            await WriteItemsAsync(writer, Feed.Items, Feed.BaseUri).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false); // channel
        }

        private async Task WriteItemContentsAsync(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(feedBaseUri, item.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite)).ConfigureAwait(false);
            }
            await WriteAttributeExtensionsAsync(writer, item, Version).ConfigureAwait(false);
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
                await writer.WriteStartElementAsync(Rss20Constants.GuidTag).ConfigureAwait(false);
                if (isPermalink)
                {
                    await writer.WriteAttributeStringAsync(Rss20Constants.IsPermaLinkTag, "true").ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteAttributeStringAsync(Rss20Constants.IsPermaLinkTag, "false").ConfigureAwait(false);
                }
                await writer.WriteStringAsync(guid).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (firstAlternateLink != null)
            {
                await WriteAlternateLinkAsync(writer, firstAlternateLink, (item.BaseUri != null ? item.BaseUri : feedBaseUri)).ConfigureAwait(false);
            }

            if (item.Authors.Count == 1 && !string.IsNullOrEmpty(item.Authors[0].Email))
            {
                await WritePersonAsync(writer, Rss20Constants.AuthorTag, item.Authors[0]).ConfigureAwait(false);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteItemAuthorsToAsync(writer, item.Authors).ConfigureAwait(false);
                }
            }

            for (int i = 0; i < item.Categories.Count; ++i)
            {
                await WriteCategoryAsync(writer, item.Categories[i]).ConfigureAwait(false);
            }

            bool serializedTitle = false;
            if (item.Title != null)
            {
                await writer.WriteElementStringAsync(Rss20Constants.TitleTag, item.Title.Text).ConfigureAwait(false);
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
                await writer.WriteElementStringAsync(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace, summary.Text).ConfigureAwait(false);
            }

            if (item.SourceFeed != null)
            {
                await writer.WriteStartElementAsync(Rss20Constants.SourceTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
                await WriteAttributeExtensionsAsync(writer, item.SourceFeed, Version).ConfigureAwait(false);
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
                    await writer.WriteAttributeStringAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace, FeedUtils.GetUriString(selfLink.Uri)).ConfigureAwait(false);
                }
                string title = (item.SourceFeed.Title != null) ? item.SourceFeed.Title.Text : string.Empty;
                await writer.WriteStringAsync(title).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item.PublishDate > DateTimeOffset.MinValue)
            {
                await writer.WriteElementStringAsync(Rss20Constants.PubDateTag, Rss20Constants.Rss20Namespace, AsString(item.PublishDate)).ConfigureAwait(false);
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
                        await WriteMediaEnclosureAsync(writer, item.Links[i], item.BaseUri).ConfigureAwait(false);
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
                    await _atomSerializer.WriteLinkAsync(writer, item.Links[i], item.BaseUri).ConfigureAwait(false);
                }
            }

            if (item.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteItemLastUpdatedTimeToAsync(writer, item.LastUpdatedTime).ConfigureAwait(false);
                }
            }

            if (_serializeExtensionsAsAtom)
            {
                await _atomSerializer.WriteContentToAsync(writer, Atom10Constants.RightsTag, item.Copyright).ConfigureAwait(false);
            }

            if (!serializedContentAsDescription)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteContentToAsync(writer, Atom10Constants.ContentTag, item.Content).ConfigureAwait(false);
                }
            }

            if (item.Contributors.Count > 0)
            {
                if (_serializeExtensionsAsAtom)
                {
                    await _atomSerializer.WriteItemContributorsToAsync(writer, item.Contributors).ConfigureAwait(false);
                }
            }

            await WriteElementExtensionsAsync(writer, item, Version).ConfigureAwait(false);
        }

        private async Task WriteMediaEnclosureAsync(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            await writer.WriteStartElementAsync(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                await writer.WriteAttributeStringAsync("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite)).ConfigureAwait(false);
            }

            await link.WriteAttributeExtensionsAsync(writer, SyndicationVersions.Rss20).ConfigureAwait(false);
            if (!link.AttributeExtensions.ContainsKey(s_rss20Url))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace, FeedUtils.GetUriString(link.Uri)).ConfigureAwait(false);
            }

            if (link.MediaType != null && !link.AttributeExtensions.ContainsKey(s_rss20Type))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.TypeTag, Rss20Constants.Rss20Namespace, link.MediaType).ConfigureAwait(false);
            }

            if (link.Length != 0 && !link.AttributeExtensions.ContainsKey(s_rss20Length))
            {
                await writer.WriteAttributeStringAsync(Rss20Constants.LengthTag, Rss20Constants.Rss20Namespace, Convert.ToString(link.Length, CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        private async Task WritePersonAsync(XmlWriter writer, string elementTag, SyndicationPerson person)
        {
            await writer.WriteStartElementAsync(elementTag, Rss20Constants.Rss20Namespace).ConfigureAwait(false);
            await WriteAttributeExtensionsAsync(writer, person, Version).ConfigureAwait(false);
            await writer.WriteStringAsync(person.Email).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
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
