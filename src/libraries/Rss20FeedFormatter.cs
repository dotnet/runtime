// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace Microsoft.ServiceModel.Syndication
{
    using Microsoft.ServiceModel.Syndication.Resources;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
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
        private const string Rfc822OutputLocalDateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss zzz";
        private const string Rfc822OutputUtcDateTimeFormat = "ddd, dd MMM yyyy HH:mm:ss Z";

        private Atom10FeedFormatter _atomSerializer;
        private Type _feedType;
        private int _maxExtensionSize;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;
        private bool _serializeExtensionsAsAtom;

        //Custom Parsers
        public Func<string, XmlReaderWrapper, DateTimeOffset> DateParser { get; set; } // ParseDate
        public Func<XmlReaderWrapper, TextSyndicationContent> TitleParser { get; set; }
        public Func<XmlReaderWrapper, TextSyndicationContent> DescriptionParser { get; set; }
        public Func<XmlReaderWrapper, string> LanguageParser { get; set; }
        public Func<XmlReaderWrapper, TextSyndicationContent> CopyrightParser { get; set; }
        public Func<XmlReaderWrapper, string> GeneratorParser { get; set; }
        public Func<XmlReaderWrapper, Uri, SyndicationLink> OnReadLink { get; set; }
        public Func<XmlReaderWrapper, SyndicationFeed, SyndicationPerson> ManagingEditorParser { get; set; }
        public Func<XmlReaderWrapper, SyndicationFeed, bool> ImageParser { get; set; }
        public Func<XmlReaderWrapper,SyndicationFeed,SyndicationItem> ItemParser { get; set; }
        public Func<XmlReaderWrapper, SyndicationFeed, SyndicationCategory> FeedCategoryParser { get; set; }




        private bool OnReadImage(XmlReaderWrapper reader, SyndicationFeed result)
        {
            reader.ReadStartElement();
            while (reader.IsStartElement())
            {
                if (reader.IsStartElement(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace))
                {
                    result.ImageUrl = new Uri(reader.ReadElementString(), UriKind.RelativeOrAbsolute);
                }
                else if(reader.IsStartElement("link",Rss20Constants.Rss20Namespace))
                {
                    result.ImageLink = new Uri(reader.ReadElementString(), UriKind.RelativeOrAbsolute);
                }
                else if (reader.IsStartElement("title", Rss20Constants.Rss20Namespace))
                {
                    result.ImageTitle = new TextSyndicationContent(reader.ReadElementString());
                }
            }
            reader.ReadEndElement(); // image
            return true;
        }

        //private SyndicationItem OnReadItem(XmlReaderWrapper reader, SyndicationFeed result)
        //{
        //    //return ReadItem(reader,result);
        //}
        private TextSyndicationContent OnReadTitle(XmlReaderWrapper reader)
        {
            return new TextSyndicationContent(reader.ReadElementString());
        }

        private TextSyndicationContent DescriptionParserAction(XmlReaderWrapper reader)
        {
            return new TextSyndicationContent(reader.ReadElementString());
        }

        private String LanguageParserAction(XmlReaderWrapper reader)
        {
            return reader.ReadElementString();
        }

        private TextSyndicationContent CopyrightParserAction(XmlReaderWrapper reader)
        {
            return new TextSyndicationContent(reader.ReadElementString());
        }

        private string GeneratorParserAction(XmlReaderWrapper reader)
        {
            return reader.ReadElementString();
        }

        //private SyndicationPerson ManagingEditorParserAction(XmlReaderWrapper reader, SyndicationFeed feed)
        //{
        //    //person = new SyndicationPerson();
        //    SyndicationPerson result = CreatePerson(feed);
        //    ReadPerson(reader, result);
        //    return result;
        //}

        //private SyndicationLink LinkParserAction(XmlReaderWrapper reader, Uri baseUri)
        //{
        //    return ReadAlternateLink(reader, baseUri);
        //}


        private void LoadDefaultParsers()
        {
            DateParser = DateParserAction;
            TitleParser = OnReadTitle;
            DescriptionParser = DescriptionParserAction;
            LanguageParser = LanguageParserAction;
            CopyrightParser = CopyrightParserAction;
            GeneratorParser = GeneratorParserAction;
            //ManagingEditorParser = ManagingEditorParserAction;
            //OnReadLink = LinkParserAction;
            //FeedCategoryParser = ReadCategory;
            //ItemParser = OnReadItem;
            ImageParser = OnReadImage;
        }

        public Rss20FeedFormatter()
            : this(typeof(SyndicationFeed))
        {
            //Setting default parsers
            LoadDefaultParsers();
        }

        public Rss20FeedFormatter(Type feedTypeToCreate)
            : base()
        {
            if (feedTypeToCreate == null)
            {
                throw new ArgumentNullException("feedTypeToCreate");
            }
            if (!typeof(SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw new ArgumentException(String.Format(SR.InvalidObjectTypePassed, "feedTypeToCreate", "SyndicationFeed"));
            }
            _serializeExtensionsAsAtom = true;
            _maxExtensionSize = int.MaxValue;
            _preserveElementExtensions = true;
            _preserveAttributeExtensions = true;
            _atomSerializer = new Atom10FeedFormatter(feedTypeToCreate);
            _feedType = feedTypeToCreate;

            LoadDefaultParsers();
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
                throw new ArgumentNullException("reader");
            }

            return reader.IsStartElement(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
        }

        public override async Task ReadFromAsync(XmlReader reader)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(String.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
            }

            SetFeed(CreateFeedInstance());
            await ReadXmlAsync(XmlReaderWrapper.CreateFromReader(reader), this.Feed);
        }

        void WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            WriteFeed(writer);
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            writer.WriteStartElement(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
            WriteFeed(writer);
            writer.WriteEndElement();
        }

        protected internal override void SetFeed(SyndicationFeed feed)
        {
            base.SetFeed(feed);
            _atomSerializer.SetFeed(this.Feed);
        }

        async Task ReadItemFromAsync(XmlReaderWrapper reader, SyndicationItem result, Uri feedBaseUri)
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
                                    result.Title = new TextSyndicationContent(await reader.ReadElementStringAsync());
                                    break;

                                case Rss20Constants.LinkTag:
                                    result.Links.Add(await ReadAlternateLinkAsync(reader, result.BaseUri));
                                    readAlternateLink = true;
                                    break;

                                case Rss20Constants.DescriptionTag:
                                    result.Summary = new TextSyndicationContent(await reader.ReadElementStringAsync());
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
                                        if (permalinkString != null && permalinkString.ToUpperInvariant() == "FALSE")
                                        {
                                            isPermalink = false;
                                        }

                                        result.Id = await reader.ReadElementStringAsync();
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
                                                result.PublishDate = DateParser(str, reader);
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
                                                    feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(val, UriKind.RelativeOrAbsolute)));
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
                                        string feedTitle = await reader.ReadElementStringAsync();
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

        internal async Task ReadItemFromAsync(XmlReaderWrapper reader, SyndicationItem result)
        {
            await ReadItemFromAsync(reader, result, null);
        }

        internal void WriteItemContents(XmlWriter writer, SyndicationItem item)
        {
            WriteItemContents(writer, item, null);
        }

        protected override SyndicationFeed CreateFeedInstance()
        {
            return SyndicationFeedFormatter.CreateFeedInstance(_feedType);
        }

        protected virtual async Task<SyndicationItem> ReadItemAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            SyndicationItem item = CreateItem(feed);
            await ReadItemFromAsync(reader, item, feed.BaseUri); // delegate => ItemParser(reader,item,feed.BaseUri);//
            return item;
        }

        protected virtual void WriteItem(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            writer.WriteStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
            WriteItemContents(writer, item, feedBaseUri);
            writer.WriteEndElement();
        }

        protected virtual void WriteItems(XmlWriter writer, IEnumerable<SyndicationItem> items, Uri feedBaseUri)
        {
            if (items == null)
            {
                return;
            }
            foreach (SyndicationItem item in items)
            {
                this.WriteItem(writer, item, feedBaseUri);
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

        public static void RemoveExtraWhiteSpaceAtStart(StringBuilder stringBuilder)
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

        public static void ReplaceMultipleWhiteSpaceWithSingleWhiteSpace(StringBuilder builder)
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
            string uri = await reader.ReadElementStringAsync();
            link.Uri = new Uri(uri, UriKind.RelativeOrAbsolute);
            return link;
        }

        async Task<SyndicationCategory> ReadCategoryAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            SyndicationCategory result = CreateCategory(feed);
            await ReadCategoryAsync(reader, result);
            return result;
        }

        async Task ReadCategoryAsync(XmlReaderWrapper reader, SyndicationCategory category)
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
                category.Name = await reader.ReadStringAsync();
                await reader.ReadEndElementAsync();
            }
        }

        async Task<SyndicationCategory> ReadCategoryAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            await ReadCategoryAsync(reader, result);
            return result;
        }

        //private void ReadItemFrom(XmlReaderWrapper reader, SyndicationItem result, Uri feedBaseUri)
        //{
        //    try
        //    {
        //        result.BaseUri = feedBaseUri;
        //        reader.MoveToContent();
        //        bool isEmpty = reader.IsEmptyElement;
        //        if (reader.HasAttributes)
        //        {
        //            while (reader.MoveToNextAttribute())
        //            {
        //                string ns = reader.NamespaceURI;
        //                string name = reader.LocalName;
        //                if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
        //                {
        //                    result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, reader.Value);
        //                    continue;
        //                }
        //                if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
        //                {
        //                    continue;
        //                }
        //                string val = reader.Value;
        //                if (!TryParseAttribute(name, ns, val, result, this.Version))
        //                {
        //                    if (_preserveAttributeExtensions)
        //                    {
        //                        result.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
        //                    }
        //                }
        //            }
        //        }
        //        reader.ReadStartElement();
        //        if (!isEmpty)
        //        {
        //            string fallbackAlternateLink = null;
        //            XmlDictionaryWriter extWriter = null;
        //            bool readAlternateLink = false;
        //            try
        //            {
        //                XmlBuffer buffer = null;
        //                while (reader.IsStartElement())
        //                {
        //                    if (reader.IsStartElement(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        //result.Title = new TextSyndicationContent(reader.ReadElementString());
        //                        result.Title = TitleParser(reader);
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        //result.Links.Add(ReadAlternateLink(reader, result.BaseUri));
        //                        result.Links.Add(OnReadLink(reader, result.BaseUri));
        //                        readAlternateLink = true;
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        //result.Summary = new TextSyndicationContent(reader.ReadElementString());
        //                        result.Summary = DescriptionParser(reader);
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.AuthorTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        result.Authors.Add(ReadPerson(reader, result)); // missing custom parser
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        result.Categories.Add(ReadCategory(reader, result));
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        result.Links.Add(ReadMediaEnclosure(reader, result.BaseUri));
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.GuidTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        bool isPermalink = true;
        //                        string permalinkString = reader.GetAttribute(Rss20Constants.IsPermaLinkTag, Rss20Constants.Rss20Namespace);
        //                        if ((permalinkString != null) && (permalinkString.ToUpperInvariant() == "FALSE"))
        //                        {
        //                            isPermalink = false;
        //                        }
        //                        result.Id = reader.ReadElementString();
        //                        if (isPermalink)
        //                        {
        //                            fallbackAlternateLink = result.Id;
        //                        }
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.PubDateTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        bool canReadContent = !reader.IsEmptyElement;
        //                        reader.ReadStartElement();
        //                        if (canReadContent)
        //                        {
        //                            string str = reader.ReadString();
        //                            if (!string.IsNullOrEmpty(str))
        //                            {
        //                                result.PublishDate = DateParser(str, reader); // original ==> DateFromString(str, reader);
        //                            }
        //                            reader.ReadEndElement();
        //                        }
        //                    }
        //                    else if (reader.IsStartElement(Rss20Constants.SourceTag, Rss20Constants.Rss20Namespace))
        //                    {
        //                        SyndicationFeed feed = new SyndicationFeed();
        //                        if (reader.HasAttributes)
        //                        {
        //                            while (reader.MoveToNextAttribute())
        //                            {
        //                                string ns = reader.NamespaceURI;
        //                                string name = reader.LocalName;
        //                                if (FeedUtils.IsXmlns(name, ns))
        //                                {
        //                                    continue;
        //                                }
        //                                string val = reader.Value;
        //                                if (name == Rss20Constants.UrlTag && ns == Rss20Constants.Rss20Namespace)
        //                                {
        //                                    feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(val, UriKind.RelativeOrAbsolute)));
        //                                }
        //                                else if (!FeedUtils.IsXmlns(name, ns))
        //                                {
        //                                    if (_preserveAttributeExtensions)
        //                                    {
        //                                        feed.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
        //                                    }
        //                                }
        //                            }
        //                        }
        //                        string feedTitle = reader.ReadElementString();
        //                        feed.Title = new TextSyndicationContent(feedTitle);
        //                        result.SourceFeed = feed;
        //                    }
        //                    else
        //                    {
        //                        bool parsedExtension = _serializeExtensionsAsAtom && _atomSerializer.TryParseItemElementFrom(reader, result);
        //                        if (!parsedExtension)
        //                        {
        //                            parsedExtension = TryParseElement(reader, result, this.Version);
        //                        }
        //                        if (!parsedExtension)
        //                        {
        //                            if (_preserveElementExtensions)
        //                            {
        //                                CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
        //                            }
        //                            else
        //                            {
        //                                reader.Skip();
        //                            }
        //                        }
        //                    }
        //                }
        //                LoadElementExtensions(buffer, extWriter, result);
        //            }
        //            finally
        //            {
        //                if (extWriter != null)
        //                {
        //                    ((IDisposable)extWriter).Dispose();
        //                }
        //            }
        //            reader.ReadEndElement(); // item
        //            if (!readAlternateLink && fallbackAlternateLink != null)
        //            {
        //                result.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(fallbackAlternateLink, UriKind.RelativeOrAbsolute)));
        //                readAlternateLink = true;
        //            }

        //            // if there's no content and no alternate link set the summary as the item content
        //            if (result.Content == null && !readAlternateLink)
        //            {
        //                result.Content = result.Summary;
        //                result.Summary = null;
        //            }
        //        }
        //    }
        //    catch (FormatException e)
        //    {
        //        throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingItem), e);
        //    }
        //    catch (ArgumentException e)
        //    {
        //        new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingItem), e);
        //    }
        //}

        async Task<SyndicationLink> ReadMediaEnclosureAsync(XmlReaderWrapper reader, Uri baseUri)
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

        async Task<SyndicationPerson> ReadPersonAsync(XmlReaderWrapper reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            await ReadPersonAsync(reader, result);
            return result;
        }

        async Task ReadPersonAsync(XmlReaderWrapper reader, SyndicationPerson person)
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
                string email = await reader.ReadStringAsync();
                await reader.ReadEndElementAsync();
                person.Email = email;
            }
        }

        async Task<SyndicationPerson> ReadPersonAsync(XmlReaderWrapper reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            await ReadPersonAsync(reader, result);
            return result;
        }

        private async Task ReadXmlAsync(XmlReaderWrapper reader, SyndicationFeed result)
        {
            string baseUri = null;
            await reader.MoveToContentAsync();

            string version = reader.GetAttribute(Rss20Constants.VersionTag, Rss20Constants.Rss20Namespace);
            if (version != Rss20Constants.Version)
            {
                throw new NotSupportedException();
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
            bool readItemsAtLeastOnce = false;
            await reader.ReadStartElementAsync(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace);

            XmlBuffer buffer = null;
            XmlDictionaryWriter extWriter = null;

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
                                result.Title = new TextSyndicationContent(await reader.ReadElementStringAsync());
                                break;

                            case Rss20Constants.LinkTag:
                                result.Links.Add(await ReadAlternateLinkAsync(reader, result.BaseUri));
                                break;

                            case Rss20Constants.DescriptionTag:
                                result.Description = new TextSyndicationContent(await reader.ReadElementStringAsync());
                                break;

                            case Rss20Constants.LanguageTag:
                                result.Language = await reader.ReadElementStringAsync();
                                break;

                            case Rss20Constants.CopyrightTag:
                                result.Copyright = new TextSyndicationContent(await reader.ReadElementStringAsync());
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
                                            result.LastUpdatedTime = DateParser(str, reader); // DateFromString(str, reader);
                                        }

                                        await reader.ReadEndElementAsync();
                                    }

                                    break;
                                }

                            case Rss20Constants.CategoryTag:
                                result.Categories.Add(await ReadCategoryAsync(reader, result));
                                break;

                            case Rss20Constants.GeneratorTag:
                                result.Generator = await reader.ReadElementStringAsync();
                                break;

                            case Rss20Constants.ImageTag:
                                {
                                    await reader.ReadStartElementAsync();

                                    while (reader.IsStartElement())
                                    {
                                        if (await reader.IsStartElementAsync(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace))
                                        {
                                            result.ImageUrl = new Uri(await reader.ReadElementStringAsync(), UriKind.RelativeOrAbsolute);
                                        }
                                        else
                                        {
                                            await reader.SkipAsync();
                                        }
                                    }

                                    await reader.ReadEndElementAsync();
                                    break;
                                }

                            case Rss20Constants.ItemTag:
                                {
                                    if (readItemsAtLeastOnce)
                                    {
                                        throw new InvalidOperationException();
                                    }

                                    NullNotAllowedCollection<SyndicationItem> items = new NullNotAllowedCollection<SyndicationItem>();
                                    while (await reader.IsStartElementAsync(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace))
                                    {
                                        items.Add(await ReadItemAsync(reader, result));
                                    }

                                    areAllItemsRead = true;
                                    result.Items = items;
                                    readItemsAtLeastOnce = true;

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
                LoadElementExtensions(buffer, extWriter, result); //JERRY MAKE THIS ASYNC
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

        

        private void WriteAlternateLink(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            writer.WriteStartElement(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                writer.WriteAttributeString("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            link.WriteAttributeExtensions(writer, SyndicationVersions.Rss20);
            writer.WriteString(FeedUtils.GetUriString(link.Uri));
            writer.WriteEndElement();
        }

        private void WriteCategory(XmlWriter writer, SyndicationCategory category)
        {
            if (category == null)
            {
                return;
            }
            writer.WriteStartElement(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace);
            WriteAttributeExtensions(writer, category, this.Version);
            if (!string.IsNullOrEmpty(category.Scheme) && !category.AttributeExtensions.ContainsKey(s_rss20Domain))
            {
                writer.WriteAttributeString(Rss20Constants.DomainTag, Rss20Constants.Rss20Namespace, category.Scheme);
            }
            writer.WriteString(category.Name);
            writer.WriteEndElement();
        }

        private void WriteFeed(XmlWriter writer)
        {
            if (this.Feed == null)
            {
                throw new InvalidOperationException(SR.FeedFormatterDoesNotHaveFeed);
            }
            if (_serializeExtensionsAsAtom)
            {
                writer.WriteAttributeString("xmlns", Atom10Constants.Atom10Prefix, null, Atom10Constants.Atom10Namespace);
            }
            writer.WriteAttributeString(Rss20Constants.VersionTag, Rss20Constants.Version);
            writer.WriteStartElement(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace);
            if (this.Feed.BaseUri != null)
            {
                writer.WriteAttributeString("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(this.Feed.BaseUri));
            }
            WriteAttributeExtensions(writer, this.Feed, this.Version);
            string title = this.Feed.Title != null ? this.Feed.Title.Text : string.Empty;
            writer.WriteElementString(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, title);

            SyndicationLink alternateLink = null;
            for (int i = 0; i < this.Feed.Links.Count; ++i)
            {
                if (this.Feed.Links[i].RelationshipType == Atom10Constants.AlternateTag)
                {
                    alternateLink = this.Feed.Links[i];
                    WriteAlternateLink(writer, alternateLink, this.Feed.BaseUri);
                    break;
                }
            }

            string description = this.Feed.Description != null ? this.Feed.Description.Text : string.Empty;
            writer.WriteElementString(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace, description);

            if (this.Feed.Language != null)
            {
                writer.WriteElementString(Rss20Constants.LanguageTag, this.Feed.Language);
            }

            if (this.Feed.Copyright != null)
            {
                writer.WriteElementString(Rss20Constants.CopyrightTag, Rss20Constants.Rss20Namespace, this.Feed.Copyright.Text);
            }

            // if there's a single author with an email address, then serialize as the managingEditor
            // else serialize the authors as Atom extensions
            if ((this.Feed.Authors.Count == 1) && (this.Feed.Authors[0].Email != null))
            {
                WritePerson(writer, Rss20Constants.ManagingEditorTag, this.Feed.Authors[0]);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteFeedAuthorsTo(writer, this.Feed.Authors);
                }
            }

            if (this.Feed.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                writer.WriteStartElement(Rss20Constants.LastBuildDateTag);
                writer.WriteString(AsString(this.Feed.LastUpdatedTime));
                writer.WriteEndElement();
            }

            for (int i = 0; i < this.Feed.Categories.Count; ++i)
            {
                WriteCategory(writer, this.Feed.Categories[i]);
            }

            if (!string.IsNullOrEmpty(this.Feed.Generator))
            {
                writer.WriteElementString(Rss20Constants.GeneratorTag, this.Feed.Generator);
            }

            if (this.Feed.Contributors.Count > 0)
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteFeedContributorsTo(writer, this.Feed.Contributors);
                }
            }

            if (this.Feed.ImageUrl != null)
            {
                writer.WriteStartElement(Rss20Constants.ImageTag);
                writer.WriteElementString(Rss20Constants.UrlTag, FeedUtils.GetUriString(this.Feed.ImageUrl));

                string imageTitle = Feed.ImageTitle == null ? title : Feed.ImageTitle.Text;
                writer.WriteElementString(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, imageTitle);

                string imgAlternateLink = alternateLink != null ? FeedUtils.GetUriString(alternateLink.Uri) : string.Empty;

                string imageLink = Feed.ImageLink == null ? imgAlternateLink : FeedUtils.GetUriString(Feed.ImageLink);
                writer.WriteElementString(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace, imageLink);
                writer.WriteEndElement(); // image
            }

            if (_serializeExtensionsAsAtom)
            {
                _atomSerializer.WriteElement(writer, Atom10Constants.IdTag, this.Feed.Id);

                // dont write out the 1st alternate link since that would have been written out anyway
                bool isFirstAlternateLink = true;
                for (int i = 0; i < this.Feed.Links.Count; ++i)
                {
                    if (this.Feed.Links[i].RelationshipType == Atom10Constants.AlternateTag && isFirstAlternateLink)
                    {
                        isFirstAlternateLink = false;
                        continue;
                    }
                    _atomSerializer.WriteLink(writer, this.Feed.Links[i], this.Feed.BaseUri);
                }
            }

            WriteElementExtensions(writer, this.Feed, this.Version);
            WriteItems(writer, this.Feed.Items, this.Feed.BaseUri);
            writer.WriteEndElement(); // channel
        }

        private void WriteItemContents(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(feedBaseUri, item.BaseUri);
            if (baseUriToWrite != null)
            {
                writer.WriteAttributeString("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            WriteAttributeExtensions(writer, item, this.Version);
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
                writer.WriteStartElement(Rss20Constants.GuidTag);
                if (isPermalink)
                {
                    writer.WriteAttributeString(Rss20Constants.IsPermaLinkTag, "true");
                }
                else
                {
                    writer.WriteAttributeString(Rss20Constants.IsPermaLinkTag, "false");
                }
                writer.WriteString(guid);
                writer.WriteEndElement();
            }
            if (firstAlternateLink != null)
            {
                WriteAlternateLink(writer, firstAlternateLink, (item.BaseUri != null ? item.BaseUri : feedBaseUri));
            }

            if (item.Authors.Count == 1 && !string.IsNullOrEmpty(item.Authors[0].Email))
            {
                WritePerson(writer, Rss20Constants.AuthorTag, item.Authors[0]);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteItemAuthorsTo(writer, item.Authors);
                }
            }

            for (int i = 0; i < item.Categories.Count; ++i)
            {
                WriteCategory(writer, item.Categories[i]);
            }

            bool serializedTitle = false;
            if (item.Title != null)
            {
                writer.WriteElementString(Rss20Constants.TitleTag, item.Title.Text);
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
                writer.WriteElementString(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace, summary.Text);
            }

            if (item.SourceFeed != null)
            {
                writer.WriteStartElement(Rss20Constants.SourceTag, Rss20Constants.Rss20Namespace);
                WriteAttributeExtensions(writer, item.SourceFeed, this.Version);
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
                    writer.WriteAttributeString(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace, FeedUtils.GetUriString(selfLink.Uri));
                }
                string title = (item.SourceFeed.Title != null) ? item.SourceFeed.Title.Text : string.Empty;
                writer.WriteString(title);
                writer.WriteEndElement();
            }

            if (item.PublishDate > DateTimeOffset.MinValue)
            {
                writer.WriteElementString(Rss20Constants.PubDateTag, Rss20Constants.Rss20Namespace, AsString(item.PublishDate));
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
                        WriteMediaEnclosure(writer, item.Links[i], item.BaseUri);
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
                    _atomSerializer.WriteLink(writer, item.Links[i], item.BaseUri);
                }
            }


            if (item.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteItemLastUpdatedTimeTo(writer, item.LastUpdatedTime);
                }
            }

            if (_serializeExtensionsAsAtom)
            {
                _atomSerializer.WriteContentTo(writer, Atom10Constants.RightsTag, item.Copyright);
            }

            if (!serializedContentAsDescription)
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteContentTo(writer, Atom10Constants.ContentTag, item.Content);
                }
            }

            if (item.Contributors.Count > 0)
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteItemContributorsTo(writer, item.Contributors);
                }
            }

            WriteElementExtensions(writer, item, this.Version);
        }

        private void WriteMediaEnclosure(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            writer.WriteStartElement(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                writer.WriteAttributeString("xml", "base", Atom10FeedFormatter.XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            link.WriteAttributeExtensions(writer, SyndicationVersions.Rss20);
            if (!link.AttributeExtensions.ContainsKey(s_rss20Url))
            {
                writer.WriteAttributeString(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace, FeedUtils.GetUriString(link.Uri));
            }
            if (link.MediaType != null && !link.AttributeExtensions.ContainsKey(s_rss20Type))
            {
                writer.WriteAttributeString(Rss20Constants.TypeTag, Rss20Constants.Rss20Namespace, link.MediaType);
            }
            if (link.Length != 0 && !link.AttributeExtensions.ContainsKey(s_rss20Length))
            {
                writer.WriteAttributeString(Rss20Constants.LengthTag, Rss20Constants.Rss20Namespace, Convert.ToString(link.Length, CultureInfo.InvariantCulture));
            }
            writer.WriteEndElement();
        }

        private void WritePerson(XmlWriter writer, string elementTag, SyndicationPerson person)
        {
            writer.WriteStartElement(elementTag, Rss20Constants.Rss20Namespace);
            WriteAttributeExtensions(writer, person, this.Version);
            writer.WriteString(person.Email);
            writer.WriteEndElement();
        }

        // Custom parsers
        public DateTimeOffset DateParserAction(string dateTimeString, XmlReaderWrapper reader)
        {
            bool parsed = false;
            //try
            DateTimeOffset dto;
            parsed = DateTimeOffset.TryParse(dateTimeString, out dto);

            if (parsed)
                return dto;


            StringBuilder dateTimeStringBuilder = new StringBuilder(dateTimeString.Trim());
            if (dateTimeStringBuilder.Length < 18)
            {
                //If we thrown an exception here, the program will stop

                //throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                //    new XmlException(FeedUtils.AddLineInfo(reader,
                //    SR.ErrorParsingDateTime)));                    
            }
            if (dateTimeStringBuilder[3] == ',')
            {
                // There is a leading (e.g.) "Tue, ", strip it off
                dateTimeStringBuilder.Remove(0, 4);
                // There's supposed to be a space here but some implementations dont have one
                Rss20FeedFormatter.RemoveExtraWhiteSpaceAtStart(dateTimeStringBuilder);
            }
            Rss20FeedFormatter.ReplaceMultipleWhiteSpaceWithSingleWhiteSpace(dateTimeStringBuilder);
            if (char.IsDigit(dateTimeStringBuilder[1]))
            {
                // two-digit day, we are good
            }
            else
            {
                dateTimeStringBuilder.Insert(0, '0');
            }
            if (dateTimeStringBuilder.Length < 19)
            {
                //throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                //    new XmlException(FeedUtils.AddLineInfo(reader,
                //    SR.ErrorParsingDateTime)));

                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDateTime));
            }
            bool thereAreSeconds = (dateTimeStringBuilder[17] == ':');
            int timeZoneStartIndex;
            if (thereAreSeconds)
            {
                timeZoneStartIndex = 21;
            }
            else
            {
                timeZoneStartIndex = 18;
            }
            string timeZoneSuffix = dateTimeStringBuilder.ToString().Substring(timeZoneStartIndex);
            dateTimeStringBuilder.Remove(timeZoneStartIndex, dateTimeStringBuilder.Length - timeZoneStartIndex);
            bool isUtc;
            dateTimeStringBuilder.Append(NormalizeTimeZone(timeZoneSuffix, out isUtc));
            string wellFormattedString = dateTimeStringBuilder.ToString();

            DateTimeOffset theTime;
            string parseFormat;
            if (thereAreSeconds)
            {
                parseFormat = "dd MMM yyyy HH:mm:ss zzz";
            }
            else
            {
                parseFormat = "dd MMM yyyy HH:mm zzz";
            }
            if (DateTimeOffset.TryParseExact(wellFormattedString, parseFormat,
                CultureInfo.InvariantCulture.DateTimeFormat,
                (isUtc ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None), out theTime))
            {
                return theTime;
            }
            //throw new FormatException("There was an error with the format of the date");
            //throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
            //    new XmlException(FeedUtils.AddLineInfo(reader,
            //    SR.ErrorParsingDateTime)));


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
