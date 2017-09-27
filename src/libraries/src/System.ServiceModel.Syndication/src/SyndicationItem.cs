// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Xml;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationItem : IExtensibleSyndicationObject
    {
        private Collection<SyndicationPerson> _authors;
        private Uri _baseUri;
        private Collection<SyndicationCategory> _categories;
        private SyndicationContent _content;
        private Collection<SyndicationPerson> _contributors;
        private TextSyndicationContent _copyright;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private string _id;
        private DateTimeOffset _lastUpdatedTime;
        private Collection<SyndicationLink> _links;
        private DateTimeOffset _publishDate;
        private SyndicationFeed _sourceFeed;
        private TextSyndicationContent _summary;
        private TextSyndicationContent _title;

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

            _content = content;
            if (itemAlternateLink != null)
            {
                this.Links.Add(SyndicationLink.CreateAlternateLink(itemAlternateLink));
            }
            _id = id;
            _lastUpdatedTime = lastUpdatedTime;
        }

        protected SyndicationItem(SyndicationItem source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            _extensions = source._extensions.Clone();
            _authors = FeedUtils.ClonePersons(source._authors);
            _categories = FeedUtils.CloneCategories(source._categories);
            _content = (source._content != null) ? source._content.Clone() : null;
            _contributors = FeedUtils.ClonePersons(source._contributors);
            _copyright = FeedUtils.CloneTextContent(source._copyright);
            _id = source._id;
            _lastUpdatedTime = source._lastUpdatedTime;
            _links = FeedUtils.CloneLinks(source._links);
            _publishDate = source._publishDate;
            if (source.SourceFeed != null)
            {
                _sourceFeed = source._sourceFeed.Clone(false);
                _sourceFeed.Items = new Collection<SyndicationItem>();
            }
            _summary = FeedUtils.CloneTextContent(source._summary);
            _baseUri = source._baseUri;
            _title = FeedUtils.CloneTextContent(source._title);
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return _extensions.AttributeExtensions; }
        }

        public Collection<SyndicationPerson> Authors
        {
            get
            {
                if (_authors == null)
                {
                    _authors = new NullNotAllowedCollection<SyndicationPerson>();
                }
                return _authors;
            }
        }

        public Uri BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        public Collection<SyndicationCategory> Categories
        {
            get
            {
                if (_categories == null)
                {
                    _categories = new NullNotAllowedCollection<SyndicationCategory>();
                }
                return _categories;
            }
        }

        public SyndicationContent Content
        {
            get { return _content; }
            set { _content = value; }
        }

        public Collection<SyndicationPerson> Contributors
        {
            get
            {
                if (_contributors == null)
                {
                    _contributors = new NullNotAllowedCollection<SyndicationPerson>();
                }
                return _contributors;
            }
        }

        public TextSyndicationContent Copyright
        {
            get { return _copyright; }
            set { _copyright = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return _extensions.ElementExtensions; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public DateTimeOffset LastUpdatedTime
        {
            get { return _lastUpdatedTime; }
            set { _lastUpdatedTime = value; }
        }

        public Collection<SyndicationLink> Links
        {
            get
            {
                if (_links == null)
                {
                    _links = new NullNotAllowedCollection<SyndicationLink>();
                }
                return _links;
            }
        }

        public DateTimeOffset PublishDate
        {
            get { return _publishDate; }
            set { _publishDate = value; }
        }

        public SyndicationFeed SourceFeed
        {
            get { return _sourceFeed; }
            set { _sourceFeed = value; }
        }

        public TextSyndicationContent Summary
        {
            get { return _summary; }
            set { _summary = value; }
        }

        public TextSyndicationContent Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public static Task<SyndicationItem> LoadAsync(XmlReader reader)
        {
            return LoadAsync<SyndicationItem>(reader);
        }
        
        public static async Task<TSyndicationItem> LoadAsync<TSyndicationItem>(XmlReader reader)
            where TSyndicationItem : SyndicationItem, new()
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            Rss20ItemFormatter<TSyndicationItem> rssSerializer = new Rss20ItemFormatter<TSyndicationItem>();

            if (rssSerializer.CanRead(reader))
            {
                await rssSerializer.ReadFromAsync(reader);
                return rssSerializer.Item as TSyndicationItem;
            }

            throw new XmlException(string.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI));
        }


        public void AddPermalink(Uri permalink)
        {
            if (permalink == null)
            {
                throw new ArgumentNullException(nameof(permalink));
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

        public Task SaveAsAtom10(XmlWriter writer)
        {
            return GetAtom10Formatter().WriteToAsync(writer);
        }

        public Task SaveAsRss20(XmlWriter writer)
        {
            return GetRss20Formatter().WriteToAsync(writer);
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

        protected internal virtual Task WriteAttributeExtensionsAsync(XmlWriter writer, string version)
        {
            return _extensions.WriteAttributeExtensionsAsync(writer);
        }

        protected internal virtual Task WriteElementExtensionsAsync(XmlWriter writer, string version)
        {
            return _extensions.WriteElementExtensionsAsync(writer);
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            _extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            _extensions.LoadElementExtensions(buffer);
        }
    }
}


