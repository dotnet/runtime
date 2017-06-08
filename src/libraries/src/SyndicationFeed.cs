//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text;
    using System.Xml;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Xml.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;


    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class SyndicationFeed : IExtensibleSyndicationObject
    {
        Collection<SyndicationPerson> authors;
        Uri baseUri;
        Collection<SyndicationCategory> categories;
        Collection<SyndicationPerson> contributors;
        TextSyndicationContent copyright;
        TextSyndicationContent description;
        ExtensibleSyndicationObject extensions = new ExtensibleSyndicationObject();
        string generator;
        string id;
        Uri imageUrl;
        IEnumerable<SyndicationItem> items;
        string language;
        DateTimeOffset lastUpdatedTime;
        Collection<SyndicationLink> links;
        TextSyndicationContent title;

        public SyndicationFeed()
            : this((IEnumerable<SyndicationItem>) null)
        {
        }

        public SyndicationFeed(IEnumerable<SyndicationItem> items)
            : this(null, null, null, items)
        {
        }

        public SyndicationFeed(string title, string description, Uri feedAlternateLink)
            : this(title, description, feedAlternateLink, null)
        {
        }

        public SyndicationFeed(string title, string description, Uri feedAlternateLink, IEnumerable<SyndicationItem> items)
            : this(title, description, feedAlternateLink, null, DateTimeOffset.MinValue, items)
        {
        }

        public SyndicationFeed(string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime)
            : this(title, description, feedAlternateLink, id, lastUpdatedTime, null)
        {
        }

        public SyndicationFeed(string title, string description, Uri feedAlternateLink, string id, DateTimeOffset lastUpdatedTime, IEnumerable<SyndicationItem> items)
        {
            if (title != null)
            {
                this.title = new TextSyndicationContent(title);
            }
            if (description != null)
            {
                this.description = new TextSyndicationContent(description);
            }
            if (feedAlternateLink != null)
            {
                this.Links.Add(SyndicationLink.CreateAlternateLink(feedAlternateLink));
            }
            this.id = id;
            this.lastUpdatedTime = lastUpdatedTime;
            this.items = items;
        }

        protected SyndicationFeed(SyndicationFeed source, bool cloneItems)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            this.authors = FeedUtils.ClonePersons(source.authors);
            this.categories = FeedUtils.CloneCategories(source.categories);
            this.contributors = FeedUtils.ClonePersons(source.contributors);
            this.copyright = FeedUtils.CloneTextContent(source.copyright);
            this.description = FeedUtils.CloneTextContent(source.description);
            this.extensions = source.extensions.Clone();
            this.generator = source.generator;
            this.id = source.id;
            this.imageUrl = source.imageUrl;
            this.language = source.language;
            this.lastUpdatedTime = source.lastUpdatedTime;
            this.links = FeedUtils.CloneLinks(source.links);
            this.title = FeedUtils.CloneTextContent(source.title);
            this.baseUri = source.baseUri;
            IList<SyndicationItem> srcList = source.items as IList<SyndicationItem>;
            if (srcList != null)
            {
                Collection<SyndicationItem> tmp = new NullNotAllowedCollection<SyndicationItem>();
                for (int i = 0; i < srcList.Count; ++i)
                {
                    tmp.Add((cloneItems) ? srcList[i].Clone() : srcList[i]);
                }
                this.items = tmp;
            }
            else
            {
                if (cloneItems)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(SR.UnbufferedItemsCannotBeCloned)));
                }
                this.items = source.items;
            }
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
                return authors;
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
                return categories;
            }
        }

        public Collection<SyndicationPerson> Contributors
        {
            get
            {
                if (this.contributors == null)
                {
                    this.contributors = new NullNotAllowedCollection<SyndicationPerson>();
                }
                return contributors;
            }
        }

        public TextSyndicationContent Copyright
        {
            get { return this.copyright; }
            set { this.copyright = value; }
        }

        public TextSyndicationContent Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return this.extensions.ElementExtensions; }
        }

        public string Generator
        {
            get { return generator; }
            set { generator = value; }
        }

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        public Uri ImageUrl
        {
            get { return imageUrl; }
            set { imageUrl = value; }
        }

        public IEnumerable<SyndicationItem> Items
        {
            get
            {
                if (this.items == null)
                {
                    this.items = new NullNotAllowedCollection<SyndicationItem>();
                }
                return this.items;
            }
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }
                this.items = value;
            }
        }

        public string Language
        {
            get { return language; }
            set { language = value; }
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

        public TextSyndicationContent Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        public static SyndicationFeed Load(XmlReader reader)
        {
            return Load<SyndicationFeed>(reader);
        }

        public static TSyndicationFeed Load<TSyndicationFeed>(XmlReader reader)
            where TSyndicationFeed : SyndicationFeed, new ()
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            Atom10FeedFormatter<TSyndicationFeed> atomSerializer = new Atom10FeedFormatter<TSyndicationFeed>();
            if (atomSerializer.CanRead(reader))
            {
                atomSerializer.ReadFrom(reader);
                return atomSerializer.Feed as TSyndicationFeed;
            }
            Rss20FeedFormatter<TSyndicationFeed> rssSerializer = new Rss20FeedFormatter<TSyndicationFeed>();
            if (rssSerializer.CanRead(reader))
            {
                rssSerializer.ReadFrom(reader);
                return rssSerializer.Feed as TSyndicationFeed;
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.GetString(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI)));
        }

        public virtual SyndicationFeed Clone(bool cloneItems)
        {
            return new SyndicationFeed(this, cloneItems);
        }

        public Atom10FeedFormatter GetAtom10Formatter()
        {
            return new Atom10FeedFormatter(this);
        }

        public Rss20FeedFormatter GetRss20Formatter()
        {
            return GetRss20Formatter(true);
        }

        public Rss20FeedFormatter GetRss20Formatter(bool serializeExtensionsAsAtom)
        {
            return new Rss20FeedFormatter(this, serializeExtensionsAsAtom);
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

        protected internal virtual SyndicationItem CreateItem()
        {
            return new SyndicationItem();
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
