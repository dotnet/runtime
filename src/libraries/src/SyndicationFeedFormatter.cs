// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// DEBUG!!! #if disabled statements 

namespace Microsoft.ServiceModel.Syndication
{
    using Microsoft.ServiceModel.Syndication.Resources;
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Xml;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
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
                throw new ArgumentNullException("feedToWrite");
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

        public abstract string Version
        { get; }

        public abstract bool CanRead(XmlReaderWrapper reader);

        public abstract void ReadFrom(XmlReaderWrapper reader);

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0}, SyndicationVersion={1}", this.GetType(), this.Version);
        }

        public abstract void WriteTo(XmlWriter writer);

        internal static protected SyndicationCategory CreateCategory(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            return GetNonNullValue<SyndicationCategory>(feed.CreateCategory(), SR.FeedCreatedNullCategory);
        }

        internal static protected SyndicationCategory CreateCategory(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return GetNonNullValue<SyndicationCategory>(item.CreateCategory(), SR.ItemCreatedNullCategory);
        }

        internal static protected SyndicationItem CreateItem(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            return GetNonNullValue<SyndicationItem>(feed.CreateItem(), SR.FeedCreatedNullItem);
        }

        internal static protected SyndicationLink CreateLink(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            return GetNonNullValue<SyndicationLink>(feed.CreateLink(), SR.FeedCreatedNullPerson);
        }

        internal static protected SyndicationLink CreateLink(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return GetNonNullValue<SyndicationLink>(item.CreateLink(), SR.ItemCreatedNullPerson);
        }

        internal static protected SyndicationPerson CreatePerson(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            return GetNonNullValue<SyndicationPerson>(feed.CreatePerson(), SR.FeedCreatedNullPerson);
        }

        internal static protected SyndicationPerson CreatePerson(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return GetNonNullValue<SyndicationPerson>(item.CreatePerson(), SR.ItemCreatedNullPerson);
        }

        internal static protected void LoadElementExtensions(XmlReaderWrapper reader, SyndicationFeed feed, int maxExtensionSize)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            feed.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReaderWrapper reader, SyndicationItem item, int maxExtensionSize)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            item.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReaderWrapper reader, SyndicationCategory category, int maxExtensionSize)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            category.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReaderWrapper reader, SyndicationLink link, int maxExtensionSize)
        {
            if (link == null)
            {
                throw new ArgumentNullException("link");
            }
            link.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected void LoadElementExtensions(XmlReaderWrapper reader, SyndicationPerson person, int maxExtensionSize)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            person.LoadElementExtensions(reader, maxExtensionSize);
        }

        internal static protected bool TryParseAttribute(string name, string ns, string value, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
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
                throw new ArgumentNullException("item");
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
                throw new ArgumentNullException("category");
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
                throw new ArgumentNullException("link");
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
                throw new ArgumentNullException("person");
            }
            if (FeedUtils.IsXmlns(name, ns))
            {
                return true;
            }
            return person.TryParseAttribute(name, ns, value, version);
        }

        internal static protected bool TryParseContent(XmlReaderWrapper reader, SyndicationItem item, string contentType, string version, out SyndicationContent content)
        {
            return item.TryParseContent(reader, contentType, version, out content);
        }

        internal static protected bool TryParseElement(XmlReaderWrapper reader, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            return feed.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReaderWrapper reader, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            return item.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReaderWrapper reader, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            return category.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReaderWrapper reader, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw new ArgumentNullException("link");
            }
            return link.TryParseElement(reader, version);
        }

        internal static protected bool TryParseElement(XmlReaderWrapper reader, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            return person.TryParseElement(reader, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            feed.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            item.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            category.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw new ArgumentNullException("link");
            }
            link.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteAttributeExtensions(XmlWriter writer, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            person.WriteAttributeExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationFeed feed, string version)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            feed.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationItem item, string version)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            item.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationCategory category, string version)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            category.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationLink link, string version)
        {
            if (link == null)
            {
                throw new ArgumentNullException("link");
            }
            link.WriteElementExtensions(writer, version);
        }

        internal static protected void WriteElementExtensions(XmlWriter writer, SyndicationPerson person, string version)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            person.WriteElementExtensions(writer, version);
        }

        internal protected virtual void SetFeed(SyndicationFeed feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException("feed");
            }
            _feed = feed;
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

        internal static void CreateBufferIfRequiredAndWriteNode(ref XmlBuffer buffer, ref XmlDictionaryWriter extWriter, XmlReaderWrapper reader, int maxExtensionSize)
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
                throw new ArgumentNullException("feed");
            }
            CloseBuffer(buffer, writer);
            feed.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            CloseBuffer(buffer, writer);
            item.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationCategory category)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }
            CloseBuffer(buffer, writer);
            category.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException("link");
            }
            CloseBuffer(buffer, writer);
            link.LoadElementExtensions(buffer);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationPerson person)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }
            CloseBuffer(buffer, writer);
            person.LoadElementExtensions(buffer);
        }

        internal static void MoveToStartElement(XmlReaderWrapper reader)
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
                string s = String.Format(res, arg1);
                IXmlLineInfo lineInfo = reader as IXmlLineInfo;
                if (lineInfo != null && lineInfo.HasLineInfo())
                {
                    s += " " + String.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
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
                        return String.Format(SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.EndElement:
                        return String.Format(SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return String.Format(SR.XmlFoundText, reader.Value);
                    case XmlNodeType.Comment:
                        return String.Format(SR.XmlFoundComment, reader.Value);
                    case XmlNodeType.CDATA:
                        return String.Format(SR.XmlFoundCData, reader.Value);
                }
                return String.Format(SR.XmlFoundNodeType, reader.NodeType);
            }

            static public void ThrowStartElementExpected(XmlDictionaryReader reader)
            {
                ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
            }
        }
    }
}
