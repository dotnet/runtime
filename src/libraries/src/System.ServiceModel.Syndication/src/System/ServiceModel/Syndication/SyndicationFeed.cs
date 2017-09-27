// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System.ServiceModel;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;


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
        private TextSyndicationContent _imageTitle;
        private Uri _imageLink;
        
        private IEnumerable<SyndicationItem> _items;
        private string _language;
        private DateTimeOffset _lastUpdatedTime;
        private Collection<SyndicationLink> _links;
        private TextSyndicationContent _title;

        // optional RSS tags
        private SyndicationLink _documentation;
        private int _timeToLive;
        private Collection<int> _skipHours;
        private Collection<string> _skipDays;
        private SyndicationTextInput _textInput;
        private Uri _iconImage;

        public Uri IconImage
        {
            get
            {
                return _iconImage;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _iconImage = value;
            }
        } 

        public SyndicationTextInput TextInput
        {
            get
            {
                return _textInput;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _textInput = value;
            }
        }

        public SyndicationLink Documentation
        {
            get
            {
                return _documentation;
            }
            set
            {
                _documentation = value;
            }
        }

        public int TimeToLive
        {
            get {
                return _timeToLive;
            }
            set
            {
                _timeToLive = value;
            }
        }

        public Collection<int> SkipHours
        {
            get
            {
                if (_skipHours == null)
                    _skipHours = new Collection<int>();
                return _skipHours;
            }
        }

        public Collection<string> SkipDays
        {
            get
            {
                if (_skipDays == null)
                    _skipDays = new Collection<string>();
                return _skipDays;
            }
        } 

        //======================================

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
                throw new ArgumentNullException(nameof(source));
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
                    throw new InvalidOperationException(SR.UnbufferedItemsCannotBeCloned);
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

        public TextSyndicationContent ImageTitle {
            get { return _imageTitle; }
            set { _imageTitle = value; }
        }

        public Uri ImageLink {
            get { return _imageLink; }
            set { _imageLink = value; }
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
                    throw new ArgumentNullException(nameof(value));
                }
                _items = value;
            }
        }

        public string Language
        {
            get { return _language; }
            set { _language = value; }
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

        public TextSyndicationContent Title
        {
            get { return _title; }
            set { _title = value; }
        }

        //// Custom Parsing
        public static async Task<SyndicationFeed> LoadAsync(XmlReader reader, Rss20FeedFormatter formatter, CancellationToken ct)
        {
            return await LoadAsync(reader, formatter, new Atom10FeedFormatter(), ct);
        }

        public static async Task<SyndicationFeed> LoadAsync(XmlReader reader, Atom10FeedFormatter formatter, CancellationToken ct)
        {
            return await LoadAsync(reader, new Rss20FeedFormatter(), formatter, ct);
        }

        public static async Task<SyndicationFeed> LoadAsync(XmlReader reader, Rss20FeedFormatter Rssformatter, Atom10FeedFormatter Atomformatter, CancellationToken ct)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            XmlReaderWrapper wrappedReader = XmlReaderWrapper.CreateFromReader(reader);

            Atom10FeedFormatter atomSerializer = Atomformatter;
            if (atomSerializer.CanRead(wrappedReader))
            {
                await atomSerializer.ReadFromAsync(wrappedReader, new CancellationToken());
                return atomSerializer.Feed;
            }
            Rss20FeedFormatter rssSerializer = Rssformatter;
            if (rssSerializer.CanRead(wrappedReader))
            {
                await rssSerializer.ReadFromAsync(wrappedReader, new CancellationToken());
                return rssSerializer.Feed;
            }
            throw new XmlException(string.Format(SR.UnknownFeedXml, wrappedReader.LocalName, wrappedReader.NamespaceURI));
        }

        //=================================

        public static SyndicationFeed Load(XmlReader reader)
        {
            return Load<SyndicationFeed>(reader);
        }

        public static TSyndicationFeed Load<TSyndicationFeed>(XmlReader reader)
            where TSyndicationFeed : SyndicationFeed, new()
        {
            CancellationToken ct = new CancellationToken();
            return LoadAsync<TSyndicationFeed>(reader,ct).GetAwaiter().GetResult();
        }

        public static async Task<SyndicationFeed> LoadAsync(XmlReader reader, CancellationToken ct)
        {
            return await LoadAsync<SyndicationFeed>(reader, ct);
        }

        public static async Task<TSyndicationFeed> LoadAsync<TSyndicationFeed>(XmlReader reader, CancellationToken ct)
            where TSyndicationFeed : SyndicationFeed, new()
        {
            Atom10FeedFormatter<TSyndicationFeed> atomSerializer = new Atom10FeedFormatter<TSyndicationFeed>();
            if (atomSerializer.CanRead(reader))
            {
                await atomSerializer.ReadFromAsync(reader, ct);
                return atomSerializer.Feed as TSyndicationFeed;
            }

            Rss20FeedFormatter<TSyndicationFeed> rssSerializer = new Rss20FeedFormatter<TSyndicationFeed>();
            if (rssSerializer.CanRead(reader))
            {
                await rssSerializer.ReadFromAsync(reader, ct);
                return rssSerializer.Feed as TSyndicationFeed;
            }

            throw new XmlException(string.Format(SR.UnknownFeedXml, reader.LocalName, reader.NamespaceURI));
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

        public Task SaveAsAtom10Async(XmlWriter writer, CancellationToken ct)
        {
            return GetAtom10Formatter().WriteToAsync(writer, ct);
        }

        public Task SaveAsRss20Async(XmlWriter writer, CancellationToken ct)
        {
            return GetRss20Formatter().WriteToAsync(writer, ct);
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
