// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*
 * Some diagnostic lines have been commented
 * 
 * */

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = Atom10Constants.EntryTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10ItemFormatter : SyndicationItemFormatter
    {
        private Atom10FeedFormatter _feedSerializer;
        private Type _itemType;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;

        public Atom10ItemFormatter()
            : this(typeof(SyndicationItem))
        {
        }

        public Atom10ItemFormatter(Type itemTypeToCreate)
            : base()
        {
            if (itemTypeToCreate == null)
            {
                throw new ArgumentNullException(nameof(itemTypeToCreate));
            }
            if (!typeof(SyndicationItem).IsAssignableFrom(itemTypeToCreate))
            {
                throw new ArgumentException(string.Format(SR.InvalidObjectTypePassed, nameof(itemTypeToCreate), nameof(SyndicationItem)));
            }
            _feedSerializer = new Atom10FeedFormatter();
            _feedSerializer.PreserveAttributeExtensions = _preserveAttributeExtensions = true;
            _feedSerializer.PreserveElementExtensions = _preserveElementExtensions = true;
            _itemType = itemTypeToCreate;
        }

        public Atom10ItemFormatter(SyndicationItem itemToWrite)
            : base(itemToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _feedSerializer = new Atom10FeedFormatter();
            _feedSerializer.PreserveAttributeExtensions = _preserveAttributeExtensions = true;
            _feedSerializer.PreserveElementExtensions = _preserveElementExtensions = true;
            _itemType = itemToWrite.GetType();
        }

        public bool PreserveAttributeExtensions
        {
            get { return _preserveAttributeExtensions; }
            set
            {
                _preserveAttributeExtensions = value;
                _feedSerializer.PreserveAttributeExtensions = value;
            }
        }

        public bool PreserveElementExtensions
        {
            get { return _preserveElementExtensions; }
            set
            {
                _preserveElementExtensions = value;
                _feedSerializer.PreserveElementExtensions = value;
            }
        }

        public override string Version
        {
            get { return SyndicationVersions.Atom10; }
        }

        protected Type ItemType
        {
            get
            {
                return _itemType;
            }
        }

        public override bool CanRead(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            return reader.IsStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
        }

        public override Task ReadFromAsync(XmlReader reader)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(string.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI));
            }

            return ReadItemAsync(reader);
        }

        public override async Task WriteToAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
            await WriteItemAsync(writer);
            await writer.WriteEndElementAsync();
        }

        protected override SyndicationItem CreateItemInstance()
        {
            return SyndicationItemFormatter.CreateItemInstance(_itemType);
        }
        
        private Task ReadItemAsync(XmlReader reader)
        {
            SetItem(CreateItemInstance());
            return _feedSerializer.ReadItemFromAsync(XmlReaderWrapper.CreateFromReader(XmlDictionaryReader.CreateDictionaryReader(reader)), this.Item);
        }

        private Task WriteItemAsync(XmlWriter writer)
        {
            if (this.Item == null)
            {
                throw new InvalidOperationException(SR.ItemFormatterDoesNotHaveItem);
            }
            XmlDictionaryWriter w = XmlDictionaryWriter.CreateDictionaryWriter(writer);
            return _feedSerializer.WriteItemContentsAsync(w, this.Item);
        }
    }

    [XmlRoot(ElementName = Atom10Constants.EntryTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10ItemFormatter<TSyndicationItem> : Atom10ItemFormatter
        where TSyndicationItem : SyndicationItem, new()
    {
        // constructors
        public Atom10ItemFormatter()
            : base(typeof(TSyndicationItem))
        {
        }
        public Atom10ItemFormatter(TSyndicationItem itemToWrite)
            : base(itemToWrite)
        {
        }

        protected override SyndicationItem CreateItemInstance()
        {
            return new TSyndicationItem();
        }
    }
}
