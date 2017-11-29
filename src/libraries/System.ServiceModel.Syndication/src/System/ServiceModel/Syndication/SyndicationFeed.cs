// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationFeed : IExtensibleSyndicationObject
    {
        private Collection<SyndicationPerson> _authors;
        private Uri _baseUri;
        private Collection<SyndicationCategory> _categories;
        private Collection<SyndicationPerson> _contributors;
        private TextSyndicationContent _copyright;
        private TextSyndicationContent _description;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private string _generator;
        private string _id;
        private Uri _imageUrl;
        private IEnumerable<SyndicationItem> _items;
        private string _language;
        private DateTimeOffset _lastUpdatedTime;
        private Collection<SyndicationLink> _links;
        private TextSyndicationContent _title;

        public SyndicationFeed()
            : this((IEnumerable<SyndicationItem>)null)
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
                _title = new TextSyndicationContent(title);
            }
            if (description != null)
            {
                _description = new TextSyndicationContent(description);
            }
            if (feedAlternateLink != null)
            {
                this.Links.Add(SyndicationLink.CreateAlternateLink(feedAlternateLink));
            }
            _id = id;
            _lastUpdatedTime = lastUpdatedTime;
            _items = items;
        }

        protected SyndicationFeed(SyndicationFeed source, bool cloneItems)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            _authors = FeedUtils.ClonePersons(source._authors);
            _categories = FeedUtils.CloneCategories(source._categories);
            _contributors = FeedUtils.ClonePersons(source._contributors);
            _copyright = FeedUtils.CloneTextContent(source._copyright);
            _description = FeedUtils.CloneTextContent(source._description);
            _extensions = source._extensions.Clone();
            _generator = source._generator;
            _id = source._id;
            _imageUrl = source._imageUrl;
            _language = source._language;
            _lastUpdatedTime = source._lastUpdatedTime;
            _links = FeedUtils.CloneLinks(source._links);
            _title = FeedUtils.CloneTextContent(source._title);
            _baseUri = source._baseUri;
            IList<SyndicationItem> srcList = source._items as IList<SyndicationItem>;
            if (srcList != null)
            {
                Collection<SyndicationItem> tmp = new NullNotAllowedCollection<SyndicationItem>();
                for (int i = 0; i < srcList.Count; ++i)
                {
                    tmp.Add((cloneItems) ? srcList[i].Clone() : srcList[i]);
                }
                _items = tmp;
            }
            else
            {
                if (cloneItems)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.UnbufferedItemsCannotBeCloned)));
                }
                _items = source._items;
            }
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

        public TextSyndicationContent Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return _extensions.ElementExtensions; }
        }

        public string Generator
        {
            get { return _generator; }
            set { _generator = value; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Uri ImageUrl
        {
            get { return _imageUrl; }
            set { _imageUrl = value; }
        }

        public IEnumerable<SyndicationItem> Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new NullNotAllowedCollection<SyndicationItem>();
                }
                return _items;
            }
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }
                _items = value;
            }
        }

        public string Language
        {
            get { return _language; }
            set { _language = value; }
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

        public TextSyndicationContent Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public static SyndicationFeed Load(XmlReader reader)
        {
            return Load<SyndicationFeed>(reader);
        }

        public static TSyndicationFeed Load<TSyndicationFeed>(XmlReader reader)
            where TSyndicationFeed : SyndicationFeed, new()
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
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI)));
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
