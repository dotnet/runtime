// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using DiagnosticUtility = System.ServiceModel.DiagnosticUtility;
    using System.Runtime.CompilerServices;

    [XmlRoot(ElementName = Rss20Constants.RssTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20FeedFormatter : SyndicationFeedFormatter, IXmlSerializable
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

        public Rss20FeedFormatter()
            : this(typeof(SyndicationFeed))
        {
        }

        public Rss20FeedFormatter(Type feedTypeToCreate)
            : base()
        {
            if (feedTypeToCreate == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feedTypeToCreate");
            }
            if (!typeof(SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("feedTypeToCreate",
                    SR.Format(SR.InvalidObjectTypePassed, "feedTypeToCreate", "SyndicationFeed"));
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

        internal override Func<string, string, string, DateTimeOffset> GetDefaultDateTimeParser()
        {
            return DateTimeHelper.DefaultRss20DateTimeParser;
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            return reader.IsStartElement(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            TraceFeedReadBegin();
            ReadFeed(reader);
            TraceFeedReadEnd();
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            TraceFeedWriteBegin();
            WriteFeed(writer);
            TraceFeedWriteEnd();
        }

        public override void ReadFrom(XmlReader reader)
        {
            TraceFeedReadBegin();
            if (!CanRead(reader))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI)));
            }
            ReadFeed(reader);
            TraceFeedReadEnd();
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            TraceFeedWriteBegin();
            writer.WriteStartElement(Rss20Constants.RssTag, Rss20Constants.Rss20Namespace);
            WriteFeed(writer);
            writer.WriteEndElement();
            TraceFeedWriteEnd();
        }

        protected internal override void SetFeed(SyndicationFeed feed)
        {
            base.SetFeed(feed);
            _atomSerializer.SetFeed(this.Feed);
        }

        internal static void TraceExtensionsIgnoredOnWrite(string message)
        {
        }

        internal void ReadItemFrom(XmlReader reader, SyndicationItem result)
        {
            ReadItemFrom(reader, result, null);
        }

        internal void WriteItemContents(XmlWriter writer, SyndicationItem item)
        {
            WriteItemContents(writer, item, null);
        }

        protected override SyndicationFeed CreateFeedInstance()
        {
            return SyndicationFeedFormatter.CreateFeedInstance(_feedType);
        }

        protected virtual SyndicationItem ReadItem(XmlReader reader, SyndicationFeed feed)
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
            TraceItemReadBegin();
            ReadItemFrom(reader, item, feed.BaseUri);
            TraceItemReadEnd();
            return item;
        }

        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "The out parameter is needed to enable implementations that read in items from the stream on demand")]
        protected virtual IEnumerable<SyndicationItem> ReadItems(XmlReader reader, SyndicationFeed feed, out bool areAllItemsRead)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            NullNotAllowedCollection<SyndicationItem> items = new NullNotAllowedCollection<SyndicationItem>();
            while (reader.IsStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace))
            {
                items.Add(ReadItem(reader, feed));
            }
            areAllItemsRead = true;
            return items;
        }

        protected virtual void WriteItem(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            TraceItemWriteBegin();
            writer.WriteStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
            WriteItemContents(writer, item, feedBaseUri);
            writer.WriteEndElement();
            TraceItemWriteEnd();
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
        
        private string AsString(DateTimeOffset dateTime)
        {
            if (dateTime.Offset == Atom10FeedFormatter.ZeroOffset)
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

        private SyndicationLink ReadAlternateLink(XmlReader reader, Uri baseUri)
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
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, reader.Value);
                    }
                    else if (!FeedUtils.IsXmlns(reader.LocalName, reader.NamespaceURI))
                    {
                        if (this.PreserveAttributeExtensions)
                        {
                            link.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                        }
                        else
                        {
                            TraceSyndicationElementIgnoredOnRead(reader);
                        }
                    }
                }
            }

            string uri = reader.ReadElementString();
            link.Uri = UriParser(uri, UriKind.RelativeOrAbsolute, Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace);
            return link;
        }

        private SyndicationCategory ReadCategory(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationCategory result = CreateCategory(feed);
            ReadCategory(reader, result);
            return result;
        }

        private SyndicationCategory ReadCategory(XmlReader reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            ReadCategory(reader, result);
            return result;
        }

        private void ReadCategory(XmlReader reader, SyndicationCategory category)
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
                    string val = reader.Value;
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
                        else
                        {
                            TraceSyndicationElementIgnoredOnRead(reader);
                        }
                    }
                }
            }
            reader.ReadStartElement(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace);
            if (!isEmpty)
            {
                category.Name = reader.ReadString();
                reader.ReadEndElement();
            }
        }

        private void ReadFeed(XmlReader reader)
        {
            SetFeed(CreateFeedInstance());
            ReadXml(reader, this.Feed);
        }

        private void ReadItemFrom(XmlReader reader, SyndicationItem result, Uri feedBaseUri)
        {
            try
            {
                result.BaseUri = feedBaseUri;
                reader.MoveToContent();
                bool isEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
                        {
                            result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, reader.Value);
                            continue;
                        }
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, result, this.Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                            }
                            else
                            {
                                TraceSyndicationElementIgnoredOnRead(reader);
                            }
                        }
                    }
                }
                reader.ReadStartElement();
                if (!isEmpty)
                {
                    string fallbackAlternateLink = null;
                    string fallbackAlternateLinkLocalName = null;
                    string fallbackAlternateLinkNamespace = null;
                    XmlDictionaryWriter extWriter = null;
                    bool readAlternateLink = false;
                    try
                    {
                        XmlBuffer buffer = null;
                        while (reader.IsStartElement())
                        {
                            if (reader.IsStartElement(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace))
                            {
                                result.Title = new TextSyndicationContent(reader.ReadElementString());
                            }
                            else if (reader.IsStartElement(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace))
                            {
                                result.Links.Add(ReadAlternateLink(reader, result.BaseUri));
                                readAlternateLink = true;
                            }
                            else if (reader.IsStartElement(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace))
                            {
                                result.Summary = new TextSyndicationContent(reader.ReadElementString());
                            }
                            else if (reader.IsStartElement(Rss20Constants.AuthorTag, Rss20Constants.Rss20Namespace))
                            {
                                result.Authors.Add(ReadPerson(reader, result));
                            }
                            else if (reader.IsStartElement(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace))
                            {
                                result.Categories.Add(ReadCategory(reader, result));
                            }
                            else if (reader.IsStartElement(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace))
                            {
                                result.Links.Add(ReadMediaEnclosure(reader, result.BaseUri));
                            }
                            else if (reader.IsStartElement(Rss20Constants.GuidTag, Rss20Constants.Rss20Namespace))
                            {
                                bool isPermalink = true;
                                string permalinkString = reader.GetAttribute(Rss20Constants.IsPermaLinkTag, Rss20Constants.Rss20Namespace);
                                if ((permalinkString != null) && (permalinkString.ToUpperInvariant() == "FALSE"))
                                {
                                    isPermalink = false;
                                }

                                result.Id = reader.ReadElementString();
                                if (isPermalink)
                                {
                                    fallbackAlternateLink = result.Id;
                                    fallbackAlternateLinkLocalName = Rss20Constants.GuidTag;
                                    fallbackAlternateLinkNamespace = Rss20Constants.Rss20Namespace;
                                }
                            }
                            else if (reader.IsStartElement(Rss20Constants.PubDateTag, Rss20Constants.Rss20Namespace))
                            {
                                bool canReadContent = !reader.IsEmptyElement;
                                reader.ReadStartElement();
                                if (canReadContent)
                                {
                                    string str = reader.ReadString();
                                    if (!string.IsNullOrEmpty(str))
                                    {
                                        try
                                        {
                                            result.PublishDate = DateFromString(str, reader);
                                        }
                                        catch (XmlException e)
                                        {
                                            result.PublishDateException = e;
                                        }
                                    }
                                    reader.ReadEndElement();
                                }
                            }
                            else if (reader.IsStartElement(Rss20Constants.SourceTag, Rss20Constants.Rss20Namespace))
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
                                        string val = reader.Value;
                                        if (name == Rss20Constants.UrlTag && ns == Rss20Constants.Rss20Namespace)
                                        {
                                            feed.Links.Add(SyndicationLink.CreateSelfLink(UriParser(val, UriKind.RelativeOrAbsolute, Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace)));
                                        }
                                        else if (!FeedUtils.IsXmlns(name, ns))
                                        {
                                            if (_preserveAttributeExtensions)
                                            {
                                                feed.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                                            }
                                            else
                                            {
                                                TraceSyndicationElementIgnoredOnRead(reader);
                                            }
                                        }
                                    }
                                }

                                string feedTitle = reader.ReadElementString();
                                feed.Title = new TextSyndicationContent(feedTitle);
                                result.SourceFeed = feed;
                            }
                            else
                            {
                                bool parsedExtension = _serializeExtensionsAsAtom && _atomSerializer.TryParseItemElementFrom(reader, result);
                                if (!parsedExtension)
                                {
                                    parsedExtension = TryParseElement(reader, result, this.Version);
                                }
                                if (!parsedExtension)
                                {
                                    if (_preserveElementExtensions)
                                    {
                                        CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                                    }
                                    else
                                    {
                                        TraceSyndicationElementIgnoredOnRead(reader);
                                        reader.Skip();
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
                    reader.ReadEndElement(); // item
                    if (!readAlternateLink && fallbackAlternateLink != null)
                    {
                        result.Links.Add(SyndicationLink.CreateAlternateLink(UriParser(fallbackAlternateLink, UriKind.RelativeOrAbsolute, fallbackAlternateLinkLocalName, fallbackAlternateLinkNamespace)));
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
            catch (FormatException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingItem), e));
            }
            catch (ArgumentException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingItem), e));
            }
        }

        private SyndicationLink ReadMediaEnclosure(XmlReader reader, Uri baseUri)
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
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, reader.Value);
                        continue;
                    }
                    if (FeedUtils.IsXmlns(name, ns))
                    {
                        continue;
                    }
                    string val = reader.Value;
                    if (name == Rss20Constants.UrlTag && ns == Rss20Constants.Rss20Namespace)
                    {
                        link.Uri = UriParser(val, UriKind.RelativeOrAbsolute, Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace);
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
                        else
                        {
                            TraceSyndicationElementIgnoredOnRead(reader);
                        }
                    }
                }
            }
            reader.ReadStartElement(Rss20Constants.EnclosureTag, Rss20Constants.Rss20Namespace);
            if (!isEmptyElement)
            {
                reader.ReadEndElement();
            }
            return link;
        }

        private SyndicationPerson ReadPerson(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            ReadPerson(reader, result);
            return result;
        }

        private SyndicationPerson ReadPerson(XmlReader reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            ReadPerson(reader, result);
            return result;
        }

        private void ReadPerson(XmlReader reader, SyndicationPerson person)
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
                    string val = reader.Value;
                    if (!TryParseAttribute(name, ns, val, person, this.Version))
                    {
                        if (_preserveAttributeExtensions)
                        {
                            person.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                        }
                        else
                        {
                            TraceSyndicationElementIgnoredOnRead(reader);
                        }
                    }
                }
            }
            reader.ReadStartElement();
            if (!isEmpty)
            {
                string email = reader.ReadString();
                reader.ReadEndElement();
                person.Email = email;
            }
        }

        private void ReadXml(XmlReader reader, SyndicationFeed result)
        {
            try
            {
                string baseUri = null;
                string baseUriLocalName = null;
                string baseUriNamespace = null;
                reader.MoveToContent();
                string elementLocalName = reader.LocalName;
                string elementNamespace = reader.NamespaceURI;
                string version = reader.GetAttribute(Rss20Constants.VersionTag, Rss20Constants.Rss20Namespace);
                if (version != Rss20Constants.Version)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(FeedUtils.AddLineInfo(reader, (SR.Format(SR.UnsupportedRssVersion, version)))));
                }
                if (reader.AttributeCount > 1)
                {
                    string tmp = reader.GetAttribute("base", Atom10FeedFormatter.XmlNs);
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        baseUri = tmp;
                        baseUriLocalName = elementLocalName;
                        baseUriNamespace = elementNamespace;
                    }
                }
                reader.ReadStartElement();
                reader.MoveToContent();
                elementLocalName = reader.LocalName;
                elementNamespace = reader.NamespaceURI;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (name == "base" && ns == Atom10FeedFormatter.XmlNs)
                        {
                            baseUri = reader.Value;
                            baseUriLocalName = elementLocalName;
                            baseUriNamespace = elementNamespace;
                            continue;
                        }
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, result, this.Version))
                        {
                            if (_preserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                            }
                            else
                            {
                                TraceSyndicationElementIgnoredOnRead(reader);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(baseUri))
                {
                    result.BaseUri = UriParser(baseUri, UriKind.RelativeOrAbsolute, baseUriLocalName, baseUriNamespace);
                }

                bool areAllItemsRead = true;
                reader.ReadStartElement(Rss20Constants.ChannelTag, Rss20Constants.Rss20Namespace);

                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                NullNotAllowedCollection<SyndicationItem> feedItems = null;

                try
                {
                    while (reader.IsStartElement())
                    {
                        if (reader.IsStartElement(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Title = new TextSyndicationContent(reader.ReadElementString());
                        }
                        else if (reader.IsStartElement(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Links.Add(ReadAlternateLink(reader, result.BaseUri));
                        }
                        else if (reader.IsStartElement(Rss20Constants.DescriptionTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Description = new TextSyndicationContent(reader.ReadElementString());
                        }
                        else if (reader.IsStartElement(Rss20Constants.LanguageTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Language = reader.ReadElementString();
                        }
                        else if (reader.IsStartElement(Rss20Constants.CopyrightTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Copyright = new TextSyndicationContent(reader.ReadElementString());
                        }
                        else if (reader.IsStartElement(Rss20Constants.ManagingEditorTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Authors.Add(ReadPerson(reader, result));
                        }
                        else if (reader.IsStartElement(Rss20Constants.LastBuildDateTag, Rss20Constants.Rss20Namespace))
                        {
                            bool canReadContent = !reader.IsEmptyElement;
                            reader.ReadStartElement();
                            if (canReadContent)
                            {
                                string str = reader.ReadString();
                                if (!string.IsNullOrEmpty(str))
                                {
                                    try
                                    {
                                        result.LastUpdatedTime = DateFromString(str, reader);
                                    }
                                    catch (XmlException e)
                                    {
                                        result.LastUpdatedTimeException = e;
                                    }
                                }
                                reader.ReadEndElement();
                            }
                        }
                        else if (reader.IsStartElement(Rss20Constants.CategoryTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Categories.Add(ReadCategory(reader, result));
                        }
                        else if (reader.IsStartElement(Rss20Constants.GeneratorTag, Rss20Constants.Rss20Namespace))
                        {
                            result.Generator = reader.ReadElementString();
                        }
                        else if (reader.IsStartElement(Rss20Constants.ImageTag, Rss20Constants.Rss20Namespace))
                        {
                            reader.ReadStartElement();
                            while (reader.IsStartElement())
                            {
                                if (reader.IsStartElement(Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace))
                                {
                                    result.ImageUrl = UriParser(reader.ReadElementString(), UriKind.RelativeOrAbsolute, Rss20Constants.UrlTag, Rss20Constants.Rss20Namespace);
                                }
                                else
                                {
                                    // ignore other content
                                    TraceSyndicationElementIgnoredOnRead(reader);
                                    reader.Skip();
                                }
                            }
                            reader.ReadEndElement(); // image
                        }
                        else if (reader.IsStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace))
                        {
                            feedItems = feedItems ?? new NullNotAllowedCollection<SyndicationItem>();
                            IEnumerable<SyndicationItem> items = ReadItems(reader, result, out areAllItemsRead);
                            foreach(SyndicationItem item in items)
                            {
                                feedItems.Add(item);
                            }
                            
                            // if the derived class is reading the items lazily, then stop reading from the stream
                            if (!areAllItemsRead)
                            {
                                break;
                            }
                        }
                        else
                        {
                            bool parsedExtension = _serializeExtensionsAsAtom && _atomSerializer.TryParseFeedElementFrom(reader, result);
                            if (!parsedExtension)
                            {
                                parsedExtension = TryParseElement(reader, result, this.Version);
                            }
                            if (!parsedExtension)
                            {
                                if (_preserveElementExtensions)
                                {
                                    CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                                }
                                else
                                {
                                    TraceSyndicationElementIgnoredOnRead(reader);
                                    reader.Skip();
                                }
                            }
                        }
                    }

                    if (feedItems != null)
                    {
                        result.Items = feedItems;
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
                if (areAllItemsRead)
                {
                    reader.ReadEndElement(); // channel   
                    reader.ReadEndElement(); // rss
                }
            }
            catch (FormatException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingFeed), e));
            }
            catch (ArgumentException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingFeed), e));
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.FeedFormatterDoesNotHaveFeed)));
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
#pragma warning disable 56506 // tvish: this.Feed.Authors is never null
            if ((this.Feed.Authors.Count == 1) && (this.Feed.Authors[0].Email != null))
#pragma warning restore 56506
            {
                WritePerson(writer, Rss20Constants.ManagingEditorTag, this.Feed.Authors[0]);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteFeedAuthorsTo(writer, this.Feed.Authors);
                }
                else
                {
                    TraceExtensionsIgnoredOnWrite(SR.FeedAuthorsIgnoredOnWrite);
                }
            }

            if (this.Feed.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                writer.WriteStartElement(Rss20Constants.LastBuildDateTag);
                writer.WriteString(AsString(this.Feed.LastUpdatedTime));
                writer.WriteEndElement();
            }

#pragma warning disable 56506 // tvish: this.Feed.Categories is never null
            for (int i = 0; i < this.Feed.Categories.Count; ++i)
#pragma warning restore 56506
            {
                WriteCategory(writer, this.Feed.Categories[i]);
            }

            if (!string.IsNullOrEmpty(this.Feed.Generator))
            {
                writer.WriteElementString(Rss20Constants.GeneratorTag, this.Feed.Generator);
            }

#pragma warning disable 56506 // tvish: this.Feed.Contributors is never null
            if (this.Feed.Contributors.Count > 0)
#pragma warning restore 56506
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteFeedContributorsTo(writer, this.Feed.Contributors);
                }
                else
                {
                    TraceExtensionsIgnoredOnWrite(SR.FeedContributorsIgnoredOnWrite);
                }
            }

            if (this.Feed.ImageUrl != null)
            {
                writer.WriteStartElement(Rss20Constants.ImageTag);
                writer.WriteElementString(Rss20Constants.UrlTag, FeedUtils.GetUriString(this.Feed.ImageUrl));
                writer.WriteElementString(Rss20Constants.TitleTag, Rss20Constants.Rss20Namespace, title);
                string imgAlternateLink = (alternateLink != null) ? FeedUtils.GetUriString(alternateLink.Uri) : string.Empty;
                writer.WriteElementString(Rss20Constants.LinkTag, Rss20Constants.Rss20Namespace, imgAlternateLink);
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
            else
            {
                if (this.Feed.Id != null)
                {
                    TraceExtensionsIgnoredOnWrite(SR.FeedIdIgnoredOnWrite);
                }
                if (this.Feed.Links.Count > 1)
                {
                    TraceExtensionsIgnoredOnWrite(SR.FeedLinksIgnoredOnWrite);
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

#pragma warning disable 56506 // tvish, item.Authors is never null
            if (item.Authors.Count == 1 && !string.IsNullOrEmpty(item.Authors[0].Email))
#pragma warning restore 56506
            {
                WritePerson(writer, Rss20Constants.AuthorTag, item.Authors[0]);
            }
            else
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteItemAuthorsTo(writer, item.Authors);
                }
                else
                {
                    TraceExtensionsIgnoredOnWrite(SR.ItemAuthorsIgnoredOnWrite);
                }
            }

#pragma warning disable 56506 // tvish, item.Categories is never null
            for (int i = 0; i < item.Categories.Count; ++i)
#pragma warning restore 56506
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
            bool isLinkIgnored = false;
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
                else
                {
                    isLinkIgnored = true;
                }
            }
            if (isLinkIgnored)
            {
                TraceExtensionsIgnoredOnWrite(SR.ItemLinksIgnoredOnWrite);
            }

            if (item.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteItemLastUpdatedTimeTo(writer, item.LastUpdatedTime);
                }
                else
                {
                    TraceExtensionsIgnoredOnWrite(SR.ItemLastUpdatedTimeIgnoredOnWrite);
                }
            }

            if (_serializeExtensionsAsAtom)
            {
                _atomSerializer.WriteContentTo(writer, Atom10Constants.RightsTag, item.Copyright);
            }
            else
            {
                TraceExtensionsIgnoredOnWrite(SR.ItemCopyrightIgnoredOnWrite);
            }

            if (!serializedContentAsDescription)
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteContentTo(writer, Atom10Constants.ContentTag, item.Content);
                }
                else
                {
                    TraceExtensionsIgnoredOnWrite(SR.ItemContentIgnoredOnWrite);
                }
            }

#pragma warning disable 56506 // tvish, item.COntributors is never null
            if (item.Contributors.Count > 0)
#pragma warning restore 56506
            {
                if (_serializeExtensionsAsAtom)
                {
                    _atomSerializer.WriteItemContributorsTo(writer, item.Contributors);
                }
                else
                {
                    TraceExtensionsIgnoredOnWrite(SR.ItemContributorsIgnoredOnWrite);
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
}
