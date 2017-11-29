// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Xml;
    using DiagnosticUtility = System.ServiceModel.DiagnosticUtility;

    [DataContract]
    public abstract class SyndicationFeedFormatter
    {
        private SyndicationFeed _feed;

        protected SyndicationFeedFormatter()
        {
            _feed = null;
            DateTimeParser = GetDefaultDateTimeParser();
        }

        protected SyndicationFeedFormatter(SyndicationFeed feedToWrite)
        {
            if (feedToWrite == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feedToWrite");
            }
            _feed = feedToWrite;
            DateTimeParser = GetDefaultDateTimeParser();
        }

        public SyndicationFeed Feed
        {
            get
            {
                return _feed;
            }
        }

        public Func<string, UriKind, string, string, Uri> UriParser { get; set; } = DefaultUriParser;

        // Different DateTimeParsers are needed for Atom and Rss so can't set inline
        public Func<string, string, string, DateTimeOffset> DateTimeParser { get; set; }

        internal virtual Func<string, string, string, DateTimeOffset> GetDefaultDateTimeParser()
        {
            return NotImplementedDateTimeParser;
        }

        private DateTimeOffset NotImplementedDateTimeParser(string dtoString, string localName, string ns)
        {
            throw new NotImplementedException();
        }

        public abstract string Version
        { get; }

        public abstract bool CanRead(XmlReader reader);

        public abstract void ReadFrom(XmlReader reader);

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}, SyndicationVersion={1}", this.GetType(), this.Version);
        }

        public abstract void WriteTo(XmlWriter writer);

        internal static protected SyndicationCategory CreateCategory(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            return GetNonNullValue<SyndicationCategory>(feed.CreateCategory(), SR.FeedCreatedNullCategory);
        }

        internal static protected SyndicationCategory CreateCategory(SyndicationItem item)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            return GetNonNullValue<SyndicationCategory>(item.CreateCategory(), SR.ItemCreatedNullCategory);
        }

        internal static protected SyndicationItem CreateItem(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            return GetNonNullValue<SyndicationItem>(feed.CreateItem(), SR.FeedCreatedNullItem);
        }

        internal static protected SyndicationLink CreateLink(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            return GetNonNullValue<SyndicationLink>(feed.CreateLink(), SR.FeedCreatedNullPerson);
        }

        internal static protected SyndicationLink CreateLink(SyndicationItem item)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            return GetNonNullValue<SyndicationLink>(item.CreateLink(), SR.ItemCreatedNullPerson);
        }

        internal static protected SyndicationPerson CreatePerson(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            return GetNonNullValue<SyndicationPerson>(feed.CreatePerson(), SR.FeedCreatedNullPerson);
        }

        internal static protected SyndicationPerson CreatePerson(SyndicationItem item)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            return GetNonNullValue<SyndicationPerson>(item.CreatePerson(), SR.ItemCreatedNullPerson);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationFeed feed, int maxExtensionSize)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            feed.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationItem item, int maxExtensionSize)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            item.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationCategory category, int maxExtensionSize)
        {
            if (category == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("category");
            }
            category.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationLink link, int maxExtensionSize)
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            link.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationPerson person, int maxExtensionSize)
        {
            if (person == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("person");
            }
            person.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return feed.TryParseAttribute(name, ns, value, version);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return item.TryParseAttribute(name, ns, value, version);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("category");
            }
            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return category.TryParseAttribute(name, ns, value, version);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return link.TryParseAttribute(name, ns, value, version);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("person");
            }
            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return person.TryParseAttribute(name, ns, value, version);
        }

        internal static protected bool TryParseContent(XmlReader reader, SyndicationItem item, string contentType, string version, out SyndicationContent content)
        {
            return item.TryParseContent(reader, contentType, version, out content);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            return feed.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            return item.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("category");
            }
            return category.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            return link.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("person");
            }
            return person.TryParseElement(reader, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            feed.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            item.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("category");
            }
            category.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            link.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("person");
            }
            person.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            feed.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            item.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("category");
            }
            category.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            link.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("person");
            }
            person.WriteElementExtensions(writer, version);
        }

        internal protected virtual void SetFeed(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            _feed = feed;
        }

        internal DateTimeOffset DateFromString(string dateTimeString, XmlReader reader)
        {
            try
            {
                return DateTimeParser(dateTimeString, reader.LocalName, reader.NamespaceURI);
            }
            catch (FormatException e)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new XmlException(FeedUtils.AddLineInfo(reader, SR.ErrorParsingDateTime), e));
            }
        }

        private static Uri DefaultUriParser(string value, UriKind kind, string localName, string ns)
        {
            return new Uri(value, kind);
        }

        internal static void CloseBuffer(XmlBuffer buffer, XmlDictionaryWriter extWriter)
        {
            if (buffer == null)
            {
                return;
            }
            extWriter.WriteEndElement();
            buffer.CloseSection();
            buffer.Close();
        }

        internal static void CreateBufferIfRequiredAndWriteNode(ref XmlBuffer buffer, ref XmlDictionaryWriter extWriter, XmlReader reader, int maxExtensionSize)
        {
            if (buffer == null)
            {
                buffer = new XmlBuffer(maxExtensionSize);
                extWriter = buffer.OpenSection(XmlDictionaryReaderQuotas.Max);
                extWriter.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
            }
            extWriter.WriteNode(reader, false);
        }

        internal static SyndicationFeed CreateFeedInstance(Type feedType)
        {
            if (feedType.Equals(typeof(SyndicationFeed)))
            {
                return new SyndicationFeed();
            }
            else
            {
                return (SyndicationFeed)Activator.CreateInstance(feedType);
            }
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feed");
            }
            CloseBuffer(buffer, writer);
            feed.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationItem item)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            CloseBuffer(buffer, writer);
            item.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationCategory category)
        {
            if (category == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("category");
            }
            CloseBuffer(buffer, writer);
            category.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationLink link)
        {
            if (link == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("link");
            }
            CloseBuffer(buffer, writer);
            link.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationPerson person)
        {
            if (person == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("person");
            }
            CloseBuffer(buffer, writer);
            person.LoadElementExtensions(buffer);
        }

        internal static void MoveToStartElement(XmlReader reader)
        {
            Debug.Assert(reader != null, "reader != null");
            if (!reader.IsStartElement())
            {
                XmlExceptionHelper.ThrowStartElementExpected(XmlDictionaryReader.CreateDictionaryReader(reader));
            }
        }

        internal static void TraceFeedReadBegin()
        {
        }

        internal static void TraceFeedReadEnd()
        {
        }

        internal static void TraceFeedWriteBegin()
        {
        }

        internal static void TraceFeedWriteEnd()
        {
        }

        internal static void TraceItemReadBegin()
        {
        }

        internal static void TraceItemReadEnd()
        {
        }

        internal static void TraceItemWriteBegin()
        {
        }

        internal static void TraceItemWriteEnd()
        {
        }

        internal static void TraceSyndicationElementIgnoredOnRead(XmlReader reader)
        {
        }

        protected abstract SyndicationFeed CreateFeedInstance();

        private static T GetNonNullValue<T>(T value, string errorMsg)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(errorMsg)));
            }
            return value;
        }

        private static class XmlExceptionHelper
        {
            private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1)
            {
                string s = SR.Format(res, arg1);
                IXmlLineInfo lineInfo = reader as IXmlLineInfo;
                if (lineInfo != null && lineInfo.HasLineInfo())
                {
                    s += " " + SR.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
                }

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(s));
            }

            private static string GetName(string prefix, string localName)
            {
                if (prefix.Length == 0)
                    return localName;
                else
                    return string.Concat(prefix, ":", localName);
            }

            private static string GetWhatWasFound(XmlDictionaryReader reader)
            {
                if (reader.EOF)
                    return SR.Format(SR.XmlFoundEndOfFile);
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        return SR.Format(SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.EndElement:
                        return SR.Format(SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return SR.Format(SR.XmlFoundText, reader.Value);
                    case XmlNodeType.Comment:
                        return SR.Format(SR.XmlFoundComment, reader.Value);
                    case XmlNodeType.CDATA:
                        return SR.Format(SR.XmlFoundCData, reader.Value);
                }
                return SR.Format(SR.XmlFoundNodeType, reader.NodeType);
            }

            static public void ThrowStartElementExpected(XmlDictionaryReader reader)
            {
                ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
            }
        }
    }
}
