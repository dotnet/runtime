// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.ServiceModel.Channels;
using System.Diagnostics;

namespace System.ServiceModel.Syndication
{
    [XmlRoot(ElementName = Atom10Constants.FeedTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10FeedFormatter : SyndicationFeedFormatter, IXmlSerializable
    {
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
        private readonly int _maxExtensionSize;

        public Atom10FeedFormatter() : this(typeof(SyndicationFeed))
        {
        }

        public Atom10FeedFormatter(Type feedTypeToCreate!!) : base()
        {
            if (!typeof(SyndicationFeed).IsAssignableFrom(feedTypeToCreate))
            {
                throw new ArgumentException(SR.Format(SR.InvalidObjectTypePassed, nameof(feedTypeToCreate), nameof(SyndicationFeed)), nameof(feedTypeToCreate));
            }

            _maxExtensionSize = int.MaxValue;
            FeedType = feedTypeToCreate;
        }

        public Atom10FeedFormatter(SyndicationFeed feedToWrite) : base(feedToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _maxExtensionSize = int.MaxValue;
            FeedType = feedToWrite.GetType();
        }

        internal override TryParseDateTimeCallback GetDefaultDateTimeParser()
        {
            return DateTimeHelper.DefaultAtom10DateTimeParser;
        }

        public bool PreserveAttributeExtensions { get; set; } = true;

        public bool PreserveElementExtensions { get; set; } = true;

        public override string Version => SyndicationVersions.Atom10;

        protected Type FeedType { get; }

        public override bool CanRead(XmlReader reader!!)
        {
            return reader.IsStartElement(Atom10Constants.FeedTag, Atom10Constants.Atom10Namespace);
        }

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader!!)
        {
            ReadFeed(reader);
        }

        void IXmlSerializable.WriteXml(XmlWriter writer!!)
        {
            WriteFeed(writer);
        }

        public override void ReadFrom(XmlReader reader)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(SR.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
            }

            ReadFeed(reader);
        }

        public override void WriteTo(XmlWriter writer!!)
        {
            writer.WriteStartElement(Atom10Constants.FeedTag, Atom10Constants.Atom10Namespace);
            WriteFeed(writer);
            writer.WriteEndElement();
        }

        internal static void ReadCategory(XmlReader reader, SyndicationCategory category, string version, bool preserveAttributeExtensions, bool preserveElementExtensions, int maxExtensionSize)
        {
            MoveToStartElement(reader);
            bool isEmpty = reader.IsEmptyElement;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == Atom10Constants.TermTag && reader.NamespaceURI == string.Empty)
                    {
                        category.Name = reader.Value;
                    }
                    else if (reader.LocalName == Atom10Constants.SchemeTag && reader.NamespaceURI == string.Empty)
                    {
                        category.Scheme = reader.Value;
                    }
                    else if (reader.LocalName == Atom10Constants.LabelTag && reader.NamespaceURI == string.Empty)
                    {
                        category.Label = reader.Value;
                    }
                    else
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (FeedUtils.IsXmlns(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, category, version))

                        {
                            if (preserveAttributeExtensions)
                            {
                                category.AttributeExtensions.Add(new XmlQualifiedName(name, ns), val);
                            }
                        }
                    }
                }
            }

            if (!isEmpty)
            {
                reader.ReadStartElement();
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (reader.IsStartElement())
                    {
                        if (TryParseElement(reader, category, version))
                        {
                            continue;
                        }
                        else if (!preserveElementExtensions)
                        {
                            reader.Skip();
                        }
                        else
                        {
                            CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, maxExtensionSize);
                        }
                    }
                    LoadElementExtensions(buffer, extWriter, category);
                }
                finally
                {
                    extWriter?.Dispose();
                }
                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        internal static TextSyndicationContent ReadTextContentFrom(XmlReader reader, string context, bool preserveAttributeExtensions)
        {
            string type = reader.GetAttribute(Atom10Constants.TypeTag);
            return ReadTextContentFromHelper(reader, type, context, preserveAttributeExtensions);
        }

        internal static void WriteCategory(XmlWriter writer, SyndicationCategory category, string version)
        {
            writer.WriteStartElement(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace);
            WriteAttributeExtensions(writer, category, version);
            string categoryName = category.Name ?? string.Empty;
            if (!category.AttributeExtensions.ContainsKey(s_atom10Term))
            {
                writer.WriteAttributeString(Atom10Constants.TermTag, categoryName);
            }
            if (!string.IsNullOrEmpty(category.Label) && !category.AttributeExtensions.ContainsKey(s_atom10Label))
            {
                writer.WriteAttributeString(Atom10Constants.LabelTag, category.Label);
            }
            if (!string.IsNullOrEmpty(category.Scheme) && !category.AttributeExtensions.ContainsKey(s_atom10Scheme))
            {
                writer.WriteAttributeString(Atom10Constants.SchemeTag, category.Scheme);
            }
            WriteElementExtensions(writer, category, version);
            writer.WriteEndElement();
        }

        internal void ReadItemFrom(XmlReader reader, SyndicationItem result)
        {
            ReadItemFrom(reader, result, null);
        }

        internal bool TryParseFeedElementFrom(XmlReader reader, SyndicationFeed result)
        {
            if (reader.IsStartElement(Atom10Constants.AuthorTag, Atom10Constants.Atom10Namespace))
            {
                result.Authors.Add(ReadPersonFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace))
            {
                result.Categories.Add(ReadCategoryFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.ContributorTag, Atom10Constants.Atom10Namespace))
            {
                result.Contributors.Add(ReadPersonFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.GeneratorTag, Atom10Constants.Atom10Namespace))
            {
                result.Generator = reader.ReadElementString();
            }
            else if (reader.IsStartElement(Atom10Constants.IdTag, Atom10Constants.Atom10Namespace))
            {
                result.Id = reader.ReadElementString();
            }
            else if (reader.IsStartElement(Atom10Constants.LinkTag, Atom10Constants.Atom10Namespace))
            {
                result.Links.Add(ReadLinkFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.LogoTag, Atom10Constants.Atom10Namespace))
            {
                result.ImageUrl = UriFromString(reader.ReadElementString(), UriKind.RelativeOrAbsolute, Atom10Constants.LogoTag, Atom10Constants.Atom10Namespace, reader);
            }
            else if (reader.IsStartElement(Atom10Constants.RightsTag, Atom10Constants.Atom10Namespace))
            {
                result.Copyright = ReadTextContentFrom(reader, "//atom:feed/atom:rights[@type]");
            }
            else if (reader.IsStartElement(Atom10Constants.SubtitleTag, Atom10Constants.Atom10Namespace))
            {
                result.Description = ReadTextContentFrom(reader, "//atom:feed/atom:subtitle[@type]");
            }
            else if (reader.IsStartElement(Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace))
            {
                result.Title = ReadTextContentFrom(reader, "//atom:feed/atom:title[@type]");
            }
            else if (reader.IsStartElement(Atom10Constants.UpdatedTag, Atom10Constants.Atom10Namespace))
            {
                reader.ReadStartElement();
                string dtoString = reader.ReadString();
                try
                {
                    result.LastUpdatedTime = DateFromString(dtoString, reader);
                }
                catch (XmlException e)
                {
                    result.LastUpdatedTimeException = e;
                }

                reader.ReadEndElement();
            }
            else
            {
                return false;
            }
            return true;
        }

        internal bool TryParseItemElementFrom(XmlReader reader, SyndicationItem result)
        {
            if (reader.IsStartElement(Atom10Constants.AuthorTag, Atom10Constants.Atom10Namespace))
            {
                result.Authors.Add(ReadPersonFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.CategoryTag, Atom10Constants.Atom10Namespace))
            {
                result.Categories.Add(ReadCategoryFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.ContentTag, Atom10Constants.Atom10Namespace))
            {
                result.Content = ReadContentFrom(reader, result);
            }
            else if (reader.IsStartElement(Atom10Constants.ContributorTag, Atom10Constants.Atom10Namespace))
            {
                result.Contributors.Add(ReadPersonFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.IdTag, Atom10Constants.Atom10Namespace))
            {
                result.Id = reader.ReadElementString();
            }
            else if (reader.IsStartElement(Atom10Constants.LinkTag, Atom10Constants.Atom10Namespace))
            {
                result.Links.Add(ReadLinkFrom(reader, result));
            }
            else if (reader.IsStartElement(Atom10Constants.PublishedTag, Atom10Constants.Atom10Namespace))
            {
                reader.ReadStartElement();
                string dtoString = reader.ReadString();
                try
                {
                    result.PublishDate = DateFromString(dtoString, reader);
                }
                catch (XmlException e)
                {
                    result.PublishDateException = e;
                }

                reader.ReadEndElement();
            }
            else if (reader.IsStartElement(Atom10Constants.RightsTag, Atom10Constants.Atom10Namespace))
            {
                result.Copyright = ReadTextContentFrom(reader, "//atom:feed/atom:entry/atom:rights[@type]");
            }
            else if (reader.IsStartElement(Atom10Constants.SourceFeedTag, Atom10Constants.Atom10Namespace))
            {
                reader.ReadStartElement();
                result.SourceFeed = ReadFeedFrom(reader, new SyndicationFeed(), true); //  isSourceFeed
                reader.ReadEndElement();
            }
            else if (reader.IsStartElement(Atom10Constants.SummaryTag, Atom10Constants.Atom10Namespace))
            {
                result.Summary = ReadTextContentFrom(reader, "//atom:feed/atom:entry/atom:summary[@type]");
            }
            else if (reader.IsStartElement(Atom10Constants.TitleTag, Atom10Constants.Atom10Namespace))
            {
                result.Title = ReadTextContentFrom(reader, "//atom:feed/atom:entry/atom:title[@type]");
            }
            else if (reader.IsStartElement(Atom10Constants.UpdatedTag, Atom10Constants.Atom10Namespace))
            {
                reader.ReadStartElement();
                string dtoString = reader.ReadString();
                try
                {
                    result.LastUpdatedTime = DateFromString(dtoString, reader);
                }
                catch (XmlException e)
                {
                    result.LastUpdatedTimeException = e;
                }

                reader.ReadEndElement();
            }
            else
            {
                return false;
            }
            return true;
        }

        internal static void WriteContentTo(XmlWriter writer, string elementName, SyndicationContent content)
        {
            if (content != null)
            {
                content.WriteTo(writer, elementName, Atom10Constants.Atom10Namespace);
            }
        }

        internal static void WriteElement(XmlWriter writer, string elementName, string value)
        {
            if (value != null)
            {
                writer.WriteElementString(elementName, Atom10Constants.Atom10Namespace, value);
            }
        }

        internal void WriteFeedAuthorsTo(XmlWriter writer, Collection<SyndicationPerson> authors)
        {
            for (int i = 0; i < authors.Count; ++i)
            {
                SyndicationPerson p = authors[i];
                WritePersonTo(writer, p, Atom10Constants.AuthorTag);
            }
        }

        internal void WriteFeedContributorsTo(XmlWriter writer, Collection<SyndicationPerson> contributors)
        {
            for (int i = 0; i < contributors.Count; ++i)
            {
                SyndicationPerson p = contributors[i];
                WritePersonTo(writer, p, Atom10Constants.ContributorTag);
            }
        }

        internal static void WriteFeedLastUpdatedTimeTo(XmlWriter writer, DateTimeOffset lastUpdatedTime, bool isRequired)
        {
            if (lastUpdatedTime == DateTimeOffset.MinValue && isRequired)
            {
                lastUpdatedTime = DateTimeOffset.UtcNow;
            }
            if (lastUpdatedTime != DateTimeOffset.MinValue)
            {
                WriteElement(writer, Atom10Constants.UpdatedTag, AsString(lastUpdatedTime));
            }
        }

        internal void WriteItemAuthorsTo(XmlWriter writer, Collection<SyndicationPerson> authors)
        {
            for (int i = 0; i < authors.Count; ++i)
            {
                SyndicationPerson p = authors[i];
                WritePersonTo(writer, p, Atom10Constants.AuthorTag);
            }
        }

        internal void WriteItemContents(XmlWriter dictWriter, SyndicationItem item)
        {
            WriteItemContents(dictWriter, item, null);
        }

        internal void WriteItemContributorsTo(XmlWriter writer, Collection<SyndicationPerson> contributors)
        {
            for (int i = 0; i < contributors.Count; ++i)
            {
                SyndicationPerson p = contributors[i];
                WritePersonTo(writer, p, Atom10Constants.ContributorTag);
            }
        }

        internal static void WriteItemLastUpdatedTimeTo(XmlWriter writer, DateTimeOffset lastUpdatedTime)
        {
            if (lastUpdatedTime == DateTimeOffset.MinValue)
            {
                lastUpdatedTime = DateTimeOffset.UtcNow;
            }
            writer.WriteElementString(Atom10Constants.UpdatedTag,
                Atom10Constants.Atom10Namespace,
                AsString(lastUpdatedTime));
        }

        internal static void WriteLink(XmlWriter writer, SyndicationLink link, Uri baseUri)
        {
            writer.WriteStartElement(Atom10Constants.LinkTag, Atom10Constants.Atom10Namespace);
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(baseUri, link.BaseUri);
            if (baseUriToWrite != null)
            {
                writer.WriteAttributeString("xml", "base", XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            link.WriteAttributeExtensions(writer, SyndicationVersions.Atom10);
            if (!string.IsNullOrEmpty(link.RelationshipType) && !link.AttributeExtensions.ContainsKey(s_atom10Relative))
            {
                writer.WriteAttributeString(Atom10Constants.RelativeTag, link.RelationshipType);
            }
            if (!string.IsNullOrEmpty(link.MediaType) && !link.AttributeExtensions.ContainsKey(s_atom10Type))
            {
                writer.WriteAttributeString(Atom10Constants.TypeTag, link.MediaType);
            }
            if (!string.IsNullOrEmpty(link.Title) && !link.AttributeExtensions.ContainsKey(s_atom10Title))
            {
                writer.WriteAttributeString(Atom10Constants.TitleTag, link.Title);
            }
            if (link.Length != 0 && !link.AttributeExtensions.ContainsKey(s_atom10Length))
            {
                writer.WriteAttributeString(Atom10Constants.LengthTag, link.Length.ToString(CultureInfo.InvariantCulture));
            }
            if (!link.AttributeExtensions.ContainsKey(s_atom10Href))
            {
                writer.WriteAttributeString(Atom10Constants.HrefTag, FeedUtils.GetUriString(link.Uri));
            }
            link.WriteElementExtensions(writer, SyndicationVersions.Atom10);
            writer.WriteEndElement();
        }

        protected override SyndicationFeed CreateFeedInstance() => CreateFeedInstance(FeedType);

        protected virtual SyndicationItem ReadItem(XmlReader reader!!, SyndicationFeed feed!!)
        {
            SyndicationItem item = CreateItem(feed);
            ReadItemFrom(reader, item, feed.BaseUri);
            return item;
        }

        protected virtual IEnumerable<SyndicationItem> ReadItems(XmlReader reader!!, SyndicationFeed feed!!, out bool areAllItemsRead)
        {
            NullNotAllowedCollection<SyndicationItem> items = new NullNotAllowedCollection<SyndicationItem>();
            while (reader.IsStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace))
            {
                items.Add(ReadItem(reader, feed));
            }
            areAllItemsRead = true;
            return items;
        }

        protected virtual void WriteItem(XmlWriter writer, SyndicationItem item, Uri feedBaseUri)
        {
            writer.WriteStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
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
                WriteItem(writer, item, feedBaseUri);
            }
        }

        private static TextSyndicationContent ReadTextContentFromHelper(XmlReader reader, string type, string context, bool preserveAttributeExtensions)
        {
            if (string.IsNullOrEmpty(type))
            {
                type = Atom10Constants.PlaintextType;
            }

            TextSyndicationContentKind kind = type switch
            {
                Atom10Constants.PlaintextType => TextSyndicationContentKind.Plaintext,
                Atom10Constants.HtmlType => TextSyndicationContentKind.Html,
                Atom10Constants.XHtmlType => TextSyndicationContentKind.XHtml,
                _ => throw new XmlException(FeedUtils.AddLineInfo(reader, SR.Format(SR.Atom10SpecRequiresTextConstruct, context, type))),
            };

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
                        string value = reader.Value;
                        if (attrs == null)
                        {
                            attrs = new Dictionary<XmlQualifiedName, string>();
                        }
                        attrs.Add(new XmlQualifiedName(name, ns), value);
                    }
                }
            }
            reader.MoveToElement();
            string val = (kind == TextSyndicationContentKind.XHtml) ? reader.ReadInnerXml() : reader.ReadElementString();
            TextSyndicationContent result = new TextSyndicationContent(val, kind);
            if (attrs != null)
            {
                foreach (XmlQualifiedName attr in attrs.Keys)
                {
                    Debug.Assert(!FeedUtils.IsXmlns(attr.Name, attr.Namespace), "XML namespace attributes should not be added to the list.");
                    result.AttributeExtensions.Add(attr, attrs[attr]);
                }
            }
            return result;
        }

        private static string AsString(DateTimeOffset dateTime)
        {
            if (dateTime.Offset == TimeSpan.Zero)
            {
                return dateTime.ToUniversalTime().ToString(Rfc3339UTCDateTimeFormat, CultureInfo.InvariantCulture);
            }
            else
            {
                return dateTime.ToString(Rfc3339LocalDateTimeFormat, CultureInfo.InvariantCulture);
            }
        }

        private void ReadCategory(XmlReader reader, SyndicationCategory category)
        {
            ReadCategory(reader, category, Version, PreserveAttributeExtensions, PreserveElementExtensions, _maxExtensionSize);
        }

        private SyndicationCategory ReadCategoryFrom(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationCategory result = CreateCategory(feed);
            ReadCategory(reader, result);
            return result;
        }

        private SyndicationCategory ReadCategoryFrom(XmlReader reader, SyndicationItem item)
        {
            SyndicationCategory result = CreateCategory(item);
            ReadCategory(reader, result);
            return result;
        }

        private SyndicationContent ReadContentFrom(XmlReader reader, SyndicationItem item)
        {
            MoveToStartElement(reader);
            string type = reader.GetAttribute(Atom10Constants.TypeTag, string.Empty);

            if (TryParseContent(reader, item, type, Version, out SyndicationContent result))
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
                result = new UrlSyndicationContent(UriFromString(src, UriKind.RelativeOrAbsolute, Atom10Constants.ContentTag, Atom10Constants.Atom10Namespace, reader), type);
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
                            if (PreserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                        }
                    }
                }

                reader.ReadStartElement();
                if (!isEmpty)
                {
                    reader.ReadEndElement();
                }
                return result;
            }
            else
            {
                return ReadTextContentFromHelper(reader, type, "//atom:feed/atom:entry/atom:content[@type]", PreserveAttributeExtensions);
            }
        }

        private void ReadFeed(XmlReader reader)
        {
            SetFeed(CreateFeedInstance());
            ReadFeedFrom(reader, Feed, false);
        }

        private SyndicationFeed ReadFeedFrom(XmlReader reader, SyndicationFeed result, bool isSourceFeed)
        {
            reader.MoveToContent();
            try
            {
                bool elementIsEmpty = false;
                if (!isSourceFeed)
                {
                    MoveToStartElement(reader);
                    elementIsEmpty = reader.IsEmptyElement;
                    if (reader.HasAttributes)
                    {
                        while (reader.MoveToNextAttribute())
                        {
                            if (reader.LocalName == "lang" && reader.NamespaceURI == XmlNs)
                            {
                                result.Language = reader.Value;
                            }
                            else if (reader.LocalName == "base" && reader.NamespaceURI == XmlNs)
                            {
                                result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, reader.Value);
                            }
                            else
                            {
                                string ns = reader.NamespaceURI;
                                string name = reader.LocalName;
                                if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                                {
                                    continue;
                                }
                                string val = reader.Value;
                                if (!TryParseAttribute(name, ns, val, result, Version))
                                {
                                    if (PreserveAttributeExtensions)
                                    {
                                        result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                                    }
                                }
                            }
                        }
                    }
                    reader.ReadStartElement();
                }

                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                bool areAllItemsRead = true;
                NullNotAllowedCollection<SyndicationItem> feedItems = null;

                if (!elementIsEmpty)
                {
                    try
                    {
                        while (reader.IsStartElement())
                        {
                            if (TryParseFeedElementFrom(reader, result))
                            {
                                // nothing, we parsed something, great
                            }
                            else if (reader.IsStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace) && !isSourceFeed)
                            {
                                feedItems = feedItems ?? new NullNotAllowedCollection<SyndicationItem>();
                                IEnumerable<SyndicationItem> items = ReadItems(reader, result, out areAllItemsRead);
                                foreach (SyndicationItem item in items)
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
                                if (!TryParseElement(reader, result, Version))
                                {
                                    if (PreserveElementExtensions)
                                    {
                                        CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                                    }
                                    else
                                    {
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
                }
                if (!isSourceFeed && areAllItemsRead)
                {
                    reader.ReadEndElement(); // feed
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingFeed), e);
            }
            catch (ArgumentException e)
            {
                throw new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingFeed), e);
            }

            return result;
        }

        private void ReadItemFrom(XmlReader reader, SyndicationItem result, Uri feedBaseUri)
        {
            try
            {
                result.BaseUri = feedBaseUri;
                MoveToStartElement(reader);
                bool isEmpty = reader.IsEmptyElement;
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        string ns = reader.NamespaceURI;
                        string name = reader.LocalName;
                        if (name == "base" && ns == XmlNs)
                        {
                            result.BaseUri = FeedUtils.CombineXmlBase(result.BaseUri, reader.Value);
                            continue;
                        }
                        if (FeedUtils.IsXmlns(name, ns) || FeedUtils.IsXmlSchemaType(name, ns))
                        {
                            continue;
                        }
                        string val = reader.Value;
                        if (!TryParseAttribute(name, ns, val, result, Version))
                        {
                            if (PreserveAttributeExtensions)
                            {
                                result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                            }
                        }
                    }
                }
                reader.ReadStartElement();
                if (!isEmpty)
                {
                    XmlBuffer buffer = null;
                    XmlDictionaryWriter extWriter = null;
                    try
                    {
                        while (reader.IsStartElement())
                        {
                            if (TryParseItemElementFrom(reader, result))
                            {
                                // nothing, we parsed something, great
                            }
                            else
                            {
                                if (!TryParseElement(reader, result, Version))
                                {
                                    if (PreserveElementExtensions)
                                    {
                                        CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                                    }
                                    else
                                    {
                                        reader.Skip();
                                    }
                                }
                            }
                        }
                        LoadElementExtensions(buffer, extWriter, result);
                    }
                    finally
                    {
                        extWriter?.Dispose();
                    }
                    reader.ReadEndElement(); // item
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

        private void ReadLink(XmlReader reader, SyndicationLink link, Uri baseUri)
        {
            bool isEmpty = reader.IsEmptyElement;
            string mediaType = null;
            string relationship = null;
            string title = null;
            string lengthStr = null;
            string val = null;
            link.BaseUri = baseUri;
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalName == "base" && reader.NamespaceURI == XmlNs)
                    {
                        link.BaseUri = FeedUtils.CombineXmlBase(link.BaseUri, reader.Value);
                    }
                    else if (reader.LocalName == Atom10Constants.TypeTag && reader.NamespaceURI == string.Empty)
                    {
                        mediaType = reader.Value;
                    }
                    else if (reader.LocalName == Atom10Constants.RelativeTag && reader.NamespaceURI == string.Empty)
                    {
                        relationship = reader.Value;
                    }
                    else if (reader.LocalName == Atom10Constants.TitleTag && reader.NamespaceURI == string.Empty)
                    {
                        title = reader.Value;
                    }
                    else if (reader.LocalName == Atom10Constants.LengthTag && reader.NamespaceURI == string.Empty)
                    {
                        lengthStr = reader.Value;
                    }
                    else if (reader.LocalName == Atom10Constants.HrefTag && reader.NamespaceURI == string.Empty)
                    {
                        val = reader.Value;
                    }
                    else if (!FeedUtils.IsXmlns(reader.LocalName, reader.NamespaceURI))
                    {
                        if (PreserveAttributeExtensions)
                        {
                            link.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                        }
                    }
                }
            }

            long length = 0;
            if (!string.IsNullOrEmpty(lengthStr))
            {
                length = Convert.ToInt64(lengthStr, CultureInfo.InvariantCulture.NumberFormat);
            }
            reader.ReadStartElement();
            if (!isEmpty)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (reader.IsStartElement())
                    {
                        if (TryParseElement(reader, link, Version))
                        {
                            continue;
                        }
                        else if (!PreserveElementExtensions)
                        {
                            reader.Skip();
                        }
                        else
                        {
                            CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                        }
                    }
                    LoadElementExtensions(buffer, extWriter, link);
                }
                finally
                {
                    extWriter?.Dispose();
                }
                reader.ReadEndElement();
            }
            link.Length = length;
            link.MediaType = mediaType;
            link.RelationshipType = relationship;
            link.Title = title;
            link.Uri = (val != null) ? UriFromString(val, UriKind.RelativeOrAbsolute, Atom10Constants.LinkTag, Atom10Constants.Atom10Namespace, reader) : null;
        }

        private SyndicationLink ReadLinkFrom(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationLink result = CreateLink(feed);
            ReadLink(reader, result, feed.BaseUri);
            return result;
        }

        private SyndicationLink ReadLinkFrom(XmlReader reader, SyndicationItem item)
        {
            SyndicationLink result = CreateLink(item);
            ReadLink(reader, result, item.BaseUri);
            return result;
        }

        private SyndicationPerson ReadPersonFrom(XmlReader reader, SyndicationFeed feed)
        {
            SyndicationPerson result = CreatePerson(feed);
            ReadPersonFrom(reader, result);
            return result;
        }

        private SyndicationPerson ReadPersonFrom(XmlReader reader, SyndicationItem item)
        {
            SyndicationPerson result = CreatePerson(item);
            ReadPersonFrom(reader, result);
            return result;
        }

        private void ReadPersonFrom(XmlReader reader, SyndicationPerson result)
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
                    if (!TryParseAttribute(name, ns, val, result, Version))
                    {
                        if (PreserveAttributeExtensions)
                        {
                            result.AttributeExtensions.Add(new XmlQualifiedName(reader.LocalName, reader.NamespaceURI), reader.Value);
                        }
                    }
                }
            }
            reader.ReadStartElement();
            if (!isEmpty)
            {
                XmlBuffer buffer = null;
                XmlDictionaryWriter extWriter = null;
                try
                {
                    while (reader.IsStartElement())
                    {
                        if (reader.IsStartElement(Atom10Constants.NameTag, Atom10Constants.Atom10Namespace))
                        {
                            result.Name = reader.ReadElementString();
                        }
                        else if (reader.IsStartElement(Atom10Constants.UriTag, Atom10Constants.Atom10Namespace))
                        {
                            result.Uri = reader.ReadElementString();
                        }
                        else if (reader.IsStartElement(Atom10Constants.EmailTag, Atom10Constants.Atom10Namespace))
                        {
                            result.Email = reader.ReadElementString();
                        }
                        else
                        {
                            if (!TryParseElement(reader, result, Version))
                            {
                                if (PreserveElementExtensions)
                                {
                                    CreateBufferIfRequiredAndWriteNode(ref buffer, ref extWriter, reader, _maxExtensionSize);
                                }
                                else
                                {
                                    reader.Skip();
                                }
                            }
                        }
                    }
                    LoadElementExtensions(buffer, extWriter, result);
                }
                finally
                {
                    extWriter?.Dispose();
                }
                reader.ReadEndElement();
            }
        }

        private TextSyndicationContent ReadTextContentFrom(XmlReader reader, string context)
        {
            return ReadTextContentFrom(reader, context, PreserveAttributeExtensions);
        }

        private void WriteCategoriesTo(XmlWriter writer, Collection<SyndicationCategory> categories)
        {
            for (int i = 0; i < categories.Count; ++i)
            {
                WriteCategory(writer, categories[i], Version);
            }
        }

        private void WriteFeed(XmlWriter writer)
        {
            if (Feed == null)
            {
                throw new InvalidOperationException(SR.FeedFormatterDoesNotHaveFeed);
            }

            WriteFeedTo(writer, Feed, isSourceFeed: false);
        }

        private void WriteFeedTo(XmlWriter writer, SyndicationFeed feed, bool isSourceFeed)
        {
            if (!isSourceFeed)
            {
                if (!string.IsNullOrEmpty(feed.Language))
                {
                    writer.WriteAttributeString("xml", "lang", XmlNs, feed.Language);
                }
                if (feed.BaseUri != null)
                {
                    writer.WriteAttributeString("xml", "base", XmlNs, FeedUtils.GetUriString(feed.BaseUri));
                }
                WriteAttributeExtensions(writer, feed, Version);
            }
            bool isElementRequired = !isSourceFeed;
            TextSyndicationContent title = feed.Title;
            if (isElementRequired)
            {
                title = title ?? new TextSyndicationContent(string.Empty);
            }
            WriteContentTo(writer, Atom10Constants.TitleTag, title);
            WriteContentTo(writer, Atom10Constants.SubtitleTag, feed.Description);
            string id = feed.Id;
            if (isElementRequired)
            {
                id = id ?? s_idGenerator.Next();
            }
            WriteElement(writer, Atom10Constants.IdTag, id);
            WriteContentTo(writer, Atom10Constants.RightsTag, feed.Copyright);
            WriteFeedLastUpdatedTimeTo(writer, feed.LastUpdatedTime, isElementRequired);
            WriteCategoriesTo(writer, feed.Categories);
            if (feed.ImageUrl != null)
            {
                WriteElement(writer, Atom10Constants.LogoTag, feed.ImageUrl.ToString());
            }
            WriteFeedAuthorsTo(writer, feed.Authors);
            WriteFeedContributorsTo(writer, feed.Contributors);
            WriteElement(writer, Atom10Constants.GeneratorTag, feed.Generator);

            for (int i = 0; i < feed.Links.Count; ++i)
            {
                WriteLink(writer, feed.Links[i], feed.BaseUri);
            }

            WriteElementExtensions(writer, feed, Version);

            if (!isSourceFeed)
            {
                WriteItems(writer, feed.Items, feed.BaseUri);
            }
        }

        private void WriteItemContents(XmlWriter dictWriter, SyndicationItem item, Uri feedBaseUri)
        {
            Uri baseUriToWrite = FeedUtils.GetBaseUriToWrite(feedBaseUri, item.BaseUri);
            if (baseUriToWrite != null)
            {
                dictWriter.WriteAttributeString("xml", "base", XmlNs, FeedUtils.GetUriString(baseUriToWrite));
            }
            WriteAttributeExtensions(dictWriter, item, Version);

            string id = item.Id ?? s_idGenerator.Next();
            WriteElement(dictWriter, Atom10Constants.IdTag, id);

            TextSyndicationContent title = item.Title ?? new TextSyndicationContent(string.Empty);
            WriteContentTo(dictWriter, Atom10Constants.TitleTag, title);
            WriteContentTo(dictWriter, Atom10Constants.SummaryTag, item.Summary);
            if (item.PublishDate != DateTimeOffset.MinValue)
            {
                dictWriter.WriteElementString(Atom10Constants.PublishedTag,
                    Atom10Constants.Atom10Namespace,
                    AsString(item.PublishDate));
            }
            WriteItemLastUpdatedTimeTo(dictWriter, item.LastUpdatedTime);
            WriteItemAuthorsTo(dictWriter, item.Authors);
            WriteItemContributorsTo(dictWriter, item.Contributors);
            for (int i = 0; i < item.Links.Count; ++i)
            {
                WriteLink(dictWriter, item.Links[i], item.BaseUri);
            }
            WriteCategoriesTo(dictWriter, item.Categories);
            WriteContentTo(dictWriter, Atom10Constants.ContentTag, item.Content);
            WriteContentTo(dictWriter, Atom10Constants.RightsTag, item.Copyright);
            if (item.SourceFeed != null)
            {
                dictWriter.WriteStartElement(Atom10Constants.SourceFeedTag, Atom10Constants.Atom10Namespace);
                WriteFeedTo(dictWriter, item.SourceFeed, isSourceFeed: true);
                dictWriter.WriteEndElement();
            }
            WriteElementExtensions(dictWriter, item, Version);
        }

        private void WritePersonTo(XmlWriter writer, SyndicationPerson p, string elementName)
        {
            writer.WriteStartElement(elementName, Atom10Constants.Atom10Namespace);
            WriteAttributeExtensions(writer, p, Version);
            WriteElement(writer, Atom10Constants.NameTag, p.Name);
            if (!string.IsNullOrEmpty(p.Uri))
            {
                writer.WriteElementString(Atom10Constants.UriTag, Atom10Constants.Atom10Namespace, p.Uri);
            }
            if (!string.IsNullOrEmpty(p.Email))
            {
                writer.WriteElementString(Atom10Constants.EmailTag, Atom10Constants.Atom10Namespace, p.Email);
            }
            WriteElementExtensions(writer, p, Version);
            writer.WriteEndElement();
        }
    }

    [XmlRoot(ElementName = Atom10Constants.FeedTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10FeedFormatter<TSyndicationFeed> : Atom10FeedFormatter where TSyndicationFeed : SyndicationFeed, new()
    {
        public Atom10FeedFormatter() : base(typeof(TSyndicationFeed))
        {
        }

        public Atom10FeedFormatter(TSyndicationFeed feedToWrite) : base(feedToWrite)
        {
        }

        protected override SyndicationFeed CreateFeedInstance() => new TSyndicationFeed();
    }
}
