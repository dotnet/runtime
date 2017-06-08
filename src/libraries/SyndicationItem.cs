//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Xml;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class SyndicationItem : IExtensibleSyndicationObject
    {
        Collection<SyndicationPerson> authors;
        Uri baseUri;
        Collection<SyndicationCategory> categories;
        SyndicationContent content;
        Collection<SyndicationPerson> contributors;
        TextSyndicationContent copyright;
        ExtensibleSyndicationObject extensions = new ExtensibleSyndicationObject();
        string id;
        DateTimeOffset lastUpdatedTime;
        Collection<SyndicationLink> links;
        DateTimeOffset publishDate;
        SyndicationFeed sourceFeed;
        TextSyndicationContent summary;
        TextSyndicationContent title;

        public SyndicationItem()
            : this(null, null, null)
        {
        }

        public SyndicationItem(string title, string content, Uri itemAlternateLink)
            : this(title, content, itemAlternateLink, null, DateTimeOffset.MinValue)
        {
        }

        public SyndicationItem(string title, string content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
            : this(title, (content != null) ? new TextSyndicationContent(content) : null, itemAlternateLink, id, lastUpdatedTime)
        {
        }

        public SyndicationItem(string title, SyndicationContent content, Uri itemAlternateLink, string id, DateTimeOffset lastUpdatedTime)
        {
            if (title != null)
            {
                this.Title = new TextSyndicationContent(title);
            }
            this.content = content;
            if (itemAlternateLink != null)
            {
                this.Links.Add(SyndicationLink.CreateAlternateLink(itemAlternateLink));
            }
            this.id = id;
            this.lastUpdatedTime = lastUpdatedTime;
        }

        protected SyndicationItem(SyndicationItem source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            this.extensions = source.extensions.Clone();
            this.authors = FeedUtils.ClonePersons(source.authors);
            this.categories = FeedUtils.CloneCategories(source.categories);
            this.content = (source.content != null) ? source.content.Clone() : null;
            this.contributors = FeedUtils.ClonePersons(source.contributors);
            this.copyright = FeedUtils.CloneTextContent(source.copyright);
            this.id = source.id;
            this.lastUpdatedTime = source.lastUpdatedTime;
            this.links = FeedUtils.CloneLinks(source.links);
            this.publishDate = source.publishDate;
            if (source.SourceFeed != null)
            {
                this.sourceFeed = source.sourceFeed.Clone(false);
                this.sourceFeed.Items = new Collection<SyndicationItem>();
            }
            this.summary = FeedUtils.CloneTextContent(source.summary);
            this.baseUri = source.baseUri;
            this.title = FeedUtils.CloneTextContent(source.title);
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return this.extensions.AttributeExtensions; }
        }

        public Collection<SyndicationPerson> Authors
        {
            get
            {
                if (this.authors == null)
                {
                    this.authors = new NullNotAllowedCollection<SyndicationPerson>();
                }
                return this.authors;
            }
        }

        public Uri BaseUri
        {
            get { return this.baseUri; }
            set { this.baseUri = value; }
        }

        public Collection<SyndicationCategory> Categories
        {
            get
            {
                if (this.categories == null)
                {
                    this.categories = new NullNotAllowedCollection<SyndicationCategory>();
                }
                return this.categories;
            }
        }

        public SyndicationContent Content
        {
            get { return content; }
            set { content = value; }
        }

        public Collection<SyndicationPerson> Contributors
        {
            get
            {
                if (this.contributors == null)
                {
                    this.contributors = new NullNotAllowedCollection<SyndicationPerson>();
                }
                return this.contributors;
            }
        }

        public TextSyndicationContent Copyright
        {
            get { return this.copyright; }
            set { this.copyright = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return this.extensions.ElementExtensions; }
        }

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        public DateTimeOffset LastUpdatedTime
        {
            get { return lastUpdatedTime; }
            set { lastUpdatedTime = value; }
        }

        public Collection<SyndicationLink> Links
        {
            get
            {
                if (this.links == null)
                {
                    this.links = new NullNotAllowedCollection<SyndicationLink>();
                }
                return this.links;
            }
        }

        public DateTimeOffset PublishDate
        {
            get { return publishDate; }
            set { publishDate = value; }
        }

        public SyndicationFeed SourceFeed
        {
            get { return this.sourceFeed; }
            set { this.sourceFeed = value; }
        }

        public TextSyndicationContent Summary
        {
            get { return this.summary; }
            set { this.summary = value; }
        }

        public TextSyndicationContent Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        public static SyndicationItem Load(XmlReader reader)
        {
            return Load<SyndicationItem>(reader);
        }

        public static TSyndicationItem Load<TSyndicationItem>(XmlReader reader)
            where TSyndicationItem : SyndicationItem, new ()
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            Atom10ItemFormatter<TSyndicationItem> atomSerializer = new Atom10ItemFormatter<TSyndicationItem>();
            if (atomSerializer.CanRead(reader))
            {
                atomSerializer.ReadFrom(reader);
                return atomSerializer.Item as TSyndicationItem;
            }
            Rss20ItemFormatter<TSyndicationItem> rssSerializer = new Rss20ItemFormatter<TSyndicationItem>();
            if (rssSerializer.CanRead(reader))
            {
                rssSerializer.ReadFrom(reader);
                return rssSerializer.Item as TSyndicationItem;
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.GetString(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI)));
        }


        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "0#permalink", Justification = "permalink is a term defined in the RSS format")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Permalink", Justification = "permalink is a term defined in the RSS format")]
        public void AddPermalink(Uri permalink)
        {
            if (permalink == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("permalink");
            }
            this.Id = permalink.AbsoluteUri;
            this.Links.Add(SyndicationLink.CreateAlternateLink(permalink));
        }

        public virtual SyndicationItem Clone()
        {
            return new SyndicationItem(this);
        }

        public Atom10ItemFormatter GetAtom10Formatter()
        {
            return new Atom10ItemFormatter(this);
        }

        public Rss20ItemFormatter GetRss20Formatter()
        {
            return GetRss20Formatter(true);
        }

        public Rss20ItemFormatter GetRss20Formatter(bool serializeExtensionsAsAtom)
        {
            return new Rss20ItemFormatter(this, serializeExtensionsAsAtom);
        }

        public void SaveAsAtom10(XmlWriter writer)
        {
            this.GetAtom10Formatter().WriteTo(writer);
        }

        public void SaveAsRss20(XmlWriter writer)
        {
            this.GetRss20Formatter().WriteTo(writer);
        }

        protected internal virtual SyndicationCategory CreateCategory()
        {
            return new SyndicationCategory();
        }

        protected internal virtual SyndicationLink CreateLink()
        {
            return new SyndicationLink();
        }

        protected internal virtual SyndicationPerson CreatePerson()
        {
            return new SyndicationPerson();
        }

        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version)
        {
            return false;
        }

        protected internal virtual bool TryParseContent(XmlReader reader, string contentType, string version, out SyndicationContent content)
        {
            content = null;
            return false;
        }

        protected internal virtual bool TryParseElement(XmlReader reader, string version)
        {
            return false;
        }

        protected internal virtual void WriteAttributeExtensions(XmlWriter writer, string version)
        {
            this.extensions.WriteAttributeExtensions(writer);
        }

        protected internal virtual void WriteElementExtensions(XmlWriter writer, string version)
        {
            this.extensions.WriteElementExtensions(writer);
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            this.extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            this.extensions.LoadElementExtensions(buffer);
        }
    }
}


