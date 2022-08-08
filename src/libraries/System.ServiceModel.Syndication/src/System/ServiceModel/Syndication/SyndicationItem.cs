// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using System.Diagnostics.CodeAnalysis;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationItem : IExtensibleSyndicationObject
    {
        private Collection<SyndicationPerson> _authors;
        private Collection<SyndicationCategory> _categories;
        private Collection<SyndicationPerson> _contributors;
        private ExtensibleSyndicationObject _extensions;
        private DateTimeOffset _lastUpdatedTime;
        private Collection<SyndicationLink> _links;
        private DateTimeOffset _publishDate;

        public SyndicationItem() : this(null, null, null)
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
                Title = new TextSyndicationContent(title);
            }
            Content = content;
            if (itemAlternateLink != null)
            {
                Links.Add(SyndicationLink.CreateAlternateLink(itemAlternateLink));
            }
            Id = id;
            _lastUpdatedTime = lastUpdatedTime;
        }

        protected SyndicationItem(SyndicationItem source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _extensions = source._extensions.Clone();
            _authors = FeedUtils.ClonePersons(source._authors);
            _categories = FeedUtils.CloneCategories(source._categories);
            Content = source.Content?.Clone();
            _contributors = FeedUtils.ClonePersons(source._contributors);
            Copyright = FeedUtils.CloneTextContent(source.Copyright);
            Id = source.Id;
            _lastUpdatedTime = source._lastUpdatedTime;
            _links = FeedUtils.CloneLinks(source._links);
            _publishDate = source._publishDate;
            if (source.SourceFeed != null)
            {
                SourceFeed = source.SourceFeed.Clone(false);
                SourceFeed.Items = new Collection<SyndicationItem>();
            }
            Summary = FeedUtils.CloneTextContent(source.Summary);
            BaseUri = source.BaseUri;
            Title = FeedUtils.CloneTextContent(source.Title);
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions => _extensions.AttributeExtensions;

        public Collection<SyndicationPerson> Authors
        {
            get => _authors ??= new NullNotAllowedCollection<SyndicationPerson>();
        }

        public Uri BaseUri { get; set; }

        public Collection<SyndicationCategory> Categories
        {
            get => _categories ??= new NullNotAllowedCollection<SyndicationCategory>();
        }

        public SyndicationContent Content { get; set; }

        public Collection<SyndicationPerson> Contributors
        {
            get => _contributors ??= new NullNotAllowedCollection<SyndicationPerson>();
        }

        public TextSyndicationContent Copyright { get; set; }

        public SyndicationElementExtensionCollection ElementExtensions => _extensions.ElementExtensions;

        public string Id { get; set; }

        internal Exception LastUpdatedTimeException { get; set; }

        public DateTimeOffset LastUpdatedTime
        {
            get
            {
                if (LastUpdatedTimeException != null)
                {
                    throw LastUpdatedTimeException;
                }

                return _lastUpdatedTime;
            }
            set
            {
                LastUpdatedTimeException = null;
                _lastUpdatedTime = value;
            }
        }

        public Collection<SyndicationLink> Links
        {
            get => _links ??= new NullNotAllowedCollection<SyndicationLink>();
        }

        internal Exception PublishDateException { get; set; }

        public DateTimeOffset PublishDate
        {
            get
            {
                if (PublishDateException != null)
                {
                    throw PublishDateException;
                }

                return _publishDate;
            }
            set
            {
                PublishDateException = null;
                _publishDate = value;
            }
        }

        public SyndicationFeed SourceFeed { get; set; }

        public TextSyndicationContent Summary { get; set; }

        public TextSyndicationContent Title { get; set; }

        public static SyndicationItem Load(XmlReader reader) => Load<SyndicationItem>(reader);

        public static TSyndicationItem Load<TSyndicationItem>(XmlReader reader) where TSyndicationItem : SyndicationItem, new()
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
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

            throw new XmlException(SR.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI));
        }


        public void AddPermalink(Uri permalink)
        {
            if (permalink is null)
            {
                throw new ArgumentNullException(nameof(permalink));
            }

            Id = permalink.AbsoluteUri;
            Links.Add(SyndicationLink.CreateAlternateLink(permalink));
        }

        public virtual SyndicationItem Clone() => new SyndicationItem(this);

        public Atom10ItemFormatter GetAtom10Formatter() => new Atom10ItemFormatter(this);

        public Rss20ItemFormatter GetRss20Formatter() => GetRss20Formatter(true);

        public Rss20ItemFormatter GetRss20Formatter(bool serializeExtensionsAsAtom)
        {
            return new Rss20ItemFormatter(this, serializeExtensionsAsAtom);
        }

        public void SaveAsAtom10(XmlWriter writer)
        {
            GetAtom10Formatter().WriteTo(writer);
        }

        public void SaveAsRss20(XmlWriter writer)
        {
            GetRss20Formatter().WriteTo(writer);
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
            _extensions.WriteAttributeExtensions(writer);
        }

        protected internal virtual void WriteElementExtensions(XmlWriter writer, string version)
        {
            _extensions.WriteElementExtensions(writer);
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
