// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DEBUG!!! #if disabled statements 

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    [DataContract]
    public abstract class SyndicationFeedFormatter
    {
        private SyndicationFeed _feed;

        protected SyndicationFeedFormatter()
        {
            _feed = null;
        }

        protected SyndicationFeedFormatter(SyndicationFeed feedToWrite)
        {
            if (feedToWrite == null)
            {
                throw new ArgumentNullException(nameof(feedToWrite));
            }
            _feed = feedToWrite;
        }

        public SyndicationFeed Feed
        {
            get
            {
                return _feed;
            }
        }

        public abstract string Version { get; }

        public abstract bool CanRead(XmlReader reader);

        public abstract Task ReadFromAsync(XmlReader reader, CancellationToken ct);

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}, SyndicationVersion={1}", this.GetType(), this.Version);
        }

        public abstract Task WriteToAsync(XmlWriter writer, CancellationToken ct);

        internal static protected SyndicationCategory CreateCategory(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            return GetNonNullValue<SyndicationCategory>(feed.CreateCategory(), SR.FeedCreatedNullCategory);
        }

        internal static protected SyndicationCategory CreateCategory(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return GetNonNullValue<SyndicationCategory>(item.CreateCategory(), SR.ItemCreatedNullCategory);
        }

        internal static protected SyndicationItem CreateItem(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            return GetNonNullValue<SyndicationItem>(feed.CreateItem(), SR.FeedCreatedNullItem);
        }

        internal static protected SyndicationLink CreateLink(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            return GetNonNullValue<SyndicationLink>(feed.CreateLink(), SR.FeedCreatedNullPerson);
        }

        internal static protected SyndicationLink CreateLink(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return GetNonNullValue<SyndicationLink>(item.CreateLink(), SR.ItemCreatedNullPerson);
        }

        internal static protected SyndicationPerson CreatePerson(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            return GetNonNullValue<SyndicationPerson>(feed.CreatePerson(), SR.FeedCreatedNullPerson);
        }

        internal static protected SyndicationPerson CreatePerson(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return GetNonNullValue<SyndicationPerson>(item.CreatePerson(), SR.ItemCreatedNullPerson);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationFeed feed, int maxExtensionSize)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            feed.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationItem item, int maxExtensionSize)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            item.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationCategory category, int maxExtensionSize)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }
            category.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationLink link, int maxExtensionSize)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }
            link.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReader reader, SyndicationPerson person, int maxExtensionSize)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }
            person.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
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
                throw new ArgumentNullException(nameof(item));
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
                throw new ArgumentNullException(nameof(category));
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
                throw new ArgumentNullException(nameof(link));
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
                throw new ArgumentNullException(nameof(person));
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
                throw new ArgumentNullException(nameof(feed));
            }

            return feed.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return item.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }
            return category.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }
            return link.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReader reader, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }
            return person.TryParseElement(reader, version);
        }

        internal static protected async Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            await feed.WriteAttributeExtensionsAsync(writer, version);
        }

        internal static protected Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return item.WriteAttributeExtensionsAsync(writer, version);
        }

        internal static protected Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }
            return category.WriteAttributeExtensionsAsync(writer, version);
        }

        internal static protected Task WriteAttributeExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }
            return link.WriteAttributeExtensionsAsync(writer, version);
        }

        internal static protected Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }
            return person.WriteAttributeExtensionsAsync(writer, version);
        }

        internal static protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException(nameof(feed));
            }
            return feed.WriteElementExtensionsAsync(writer, version);
        }

        internal static protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            return item.WriteElementExtensionsAsync(writer, version);
        }

        internal static protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }
            return category.WriteElementExtensionsAsync(writer, version);
        }

        internal static protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }
            return link.WriteElementExtensionsAsync(writer, version);
        }

        internal static protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            return person.WriteElementExtensionsAsync(writer, version);
        }

        internal protected virtual void SetFeed(SyndicationFeed feed) { 
            _feed = feed ?? throw new ArgumentNullException(nameof(feed));
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

        internal static async Task<Tuple<XmlBuffer,XmlDictionaryWriter>> CreateBufferIfRequiredAndWriteNodeAsync(XmlBuffer buffer, XmlDictionaryWriter extWriter, XmlReader reader, int maxExtensionSize)
        {
            if (buffer == null)
            {
                buffer = new XmlBuffer(maxExtensionSize);
                extWriter = buffer.OpenSection(XmlDictionaryReaderQuotas.Max);
                extWriter.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
            }

            XmlDictionaryReader dictionaryReader = reader as XmlDictionaryReader;
            if (dictionaryReader != null)
            {
                // Reimplementing WriteNode for XmlDictionaryWriter asynchronously depends on multiple internal methods
                // so isn't feasible to reimplement here. As the primary scenario will be usage with an XmlReader which
                // isn't an XmlDictionaryReader, deferring to the synchronous implementation is a reasonable fallback.
                extWriter.WriteNode(reader, false);
            }
            else
            {
                await extWriter.WriteNodeAsync(reader, false);
            }

            return Tuple.Create(buffer, extWriter);
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
                throw new ArgumentNullException(nameof(feed));
            }

            CloseBuffer(buffer, writer);
            feed.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationItem item)
        {

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            CloseBuffer(buffer, writer);
            item.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationCategory category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            CloseBuffer(buffer, writer);
            category.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            CloseBuffer(buffer, writer);
            link.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationPerson person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            CloseBuffer(buffer, writer);
            person.LoadElementExtensions(buffer);
        }

        internal static async Task MoveToStartElementAsync(XmlReaderWrapper reader)
        {
            if (!await reader.IsStartElementAsync())
            {
                XmlExceptionHelper.ThrowStartElementExpected(XmlDictionaryReader.CreateDictionaryReader(reader));
            }
        }

        internal static void MoveToStartElement(XmlReader reader)
        {
            if (!reader.IsStartElement())
            {
                XmlExceptionHelper.ThrowStartElementExpected(XmlDictionaryReader.CreateDictionaryReader(reader));
            }
        }


        protected abstract SyndicationFeed CreateFeedInstance();

        private static T GetNonNullValue<T>(T value, string errorMsg)
        {
            return value;
        }

        private static class XmlExceptionHelper
        {
            private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1)
            {
                string s = string.Format(res, arg1);
                IXmlLineInfo lineInfo = reader as IXmlLineInfo;
                if (lineInfo != null && lineInfo.HasLineInfo())
                {
                    s += " " + string.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
                }

                throw new XmlException(s);
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
                    return SR.XmlFoundEndOfFile;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        return string.Format(SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.EndElement:
                        return string.Format(SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return string.Format(SR.XmlFoundText, reader.Value);
                    case XmlNodeType.Comment:
                        return string.Format(SR.XmlFoundComment, reader.Value);
                    case XmlNodeType.CDATA:
                        return string.Format(SR.XmlFoundCData, reader.Value);
                }
                return string.Format(SR.XmlFoundNodeType, reader.NodeType);
            }

            static public void ThrowStartElementExpected(XmlDictionaryReader reader)
            {
                ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
            }
        }
    }
}
