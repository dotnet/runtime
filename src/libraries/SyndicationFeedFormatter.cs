//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// DEBUG!!! #if disabled statements 

namespace Microsoft.ServiceModel.Syndication
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime;
    using System.Runtime.Serialization;
    //using System.ServiceModel.Diagnostics;
    using System.Xml;
    //using DiagnosticUtility = System.ServiceModel.DiagnosticUtility;
    using System.Runtime.CompilerServices;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [DataContract]
    public abstract class SyndicationFeedFormatter
    {
        SyndicationFeed feed;

        protected SyndicationFeedFormatter()
        {
            this.feed = null;
        }

        protected SyndicationFeedFormatter(SyndicationFeed feedToWrite)
        {
            if (feedToWrite == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("feedToWrite");
            }
            this.feed = feedToWrite;
        }

        public SyndicationFeed Feed
        {
            get
            {
                return this.feed;
            }
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
            this.feed = feed;
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
                return (SyndicationFeed) Activator.CreateInstance(feedType);
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
            //Fx.Assert(reader != null, "reader != null");
            if (!reader.IsStartElement())
            {
                XmlExceptionHelper.ThrowStartElementExpected(XmlDictionaryReader.CreateDictionaryReader(reader));
            }
        }

        internal static void TraceFeedReadBegin()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationReadFeedBegin, SR.GetString(SR.TraceCodeSyndicationFeedReadBegin));
#endif
            }
        }

        internal static void TraceFeedReadEnd()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled
                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationReadFeedEnd, SR.GetString(SR.TraceCodeSyndicationFeedReadEnd));

#endif
            }
        }

        internal static void TraceFeedWriteBegin()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationWriteFeedBegin, SR.GetString(SR.TraceCodeSyndicationFeedWriteBegin));
#endif
            }
        }

        internal static void TraceFeedWriteEnd()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationWriteFeedEnd, SR.GetString(SR.TraceCodeSyndicationFeedWriteEnd));
#endif
            }
        }

        internal static void TraceItemReadBegin()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationReadItemBegin, SR.GetString(SR.TraceCodeSyndicationItemReadBegin));
#endif
            }
        }

        internal static void TraceItemReadEnd()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationReadItemEnd, SR.GetString(SR.TraceCodeSyndicationItemReadEnd));
#endif
            }
        }

        internal static void TraceItemWriteBegin()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationWriteItemBegin, SR.GetString(SR.TraceCodeSyndicationItemWriteBegin));
#endif
            }
        }

        internal static void TraceItemWriteEnd()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled
                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationWriteItemEnd, SR.GetString(SR.TraceCodeSyndicationItemWriteEnd));

#endif
            }
        }

        internal static void TraceSyndicationElementIgnoredOnRead(XmlReader reader)
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
#if disabled

                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.SyndicationProtocolElementIgnoredOnRead, SR.GetString(SR.TraceCodeSyndicationProtocolElementIgnoredOnRead, reader.NodeType, reader.LocalName, reader.NamespaceURI));
#endif
            }
        }

        protected abstract SyndicationFeed CreateFeedInstance();

        static T GetNonNullValue<T>(T value, string errorMsg)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(errorMsg)));
            }
            return value;
        }

        static class XmlExceptionHelper
        {
            static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1)
            {
                string s = SR.GetString(res, arg1);
                IXmlLineInfo lineInfo = reader as IXmlLineInfo;
                if (lineInfo != null && lineInfo.HasLineInfo())
                {
                    s += " " + SR.GetString(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
                }

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(s));
            }

            static string GetName(string prefix, string localName)
            {
                if (prefix.Length == 0)
                    return localName;
                else
                    return string.Concat(prefix, ":", localName);
            }

            static string GetWhatWasFound(XmlDictionaryReader reader)
            {
                if (reader.EOF)
                    return SR.GetString(SR.XmlFoundEndOfFile);
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        return SR.GetString(SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.EndElement:
                        return SR.GetString(SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        return SR.GetString(SR.XmlFoundText, reader.Value);
                    case XmlNodeType.Comment:
                        return SR.GetString(SR.XmlFoundComment, reader.Value);
                    case XmlNodeType.CDATA:
                        return SR.GetString(SR.XmlFoundCData, reader.Value);
                }
                return SR.GetString(SR.XmlFoundNodeType, reader.NodeType);
            }

            static public void ThrowStartElementExpected(XmlDictionaryReader reader)
            {
                ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
            }
        }
    }
}
