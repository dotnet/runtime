// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.ServiceModel.Syndication
{
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
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
            get
            {
                if (_links == null)
                {
                    _links = new NullNotAllowedCollection<SyndicationLink>();
                }
                return _links;
            }
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

        public static SyndicationItem Load(XmlReader reader)
        {
            return Load<SyndicationItem>(reader);
        }

        public static TSyndicationItem Load<TSyndicationItem>(XmlReader reader)
            where TSyndicationItem : SyndicationItem, new()
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
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI)));
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


