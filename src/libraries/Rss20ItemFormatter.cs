// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using Microsoft.ServiceModel.Syndication.Resources;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;


    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter : SyndicationItemFormatter
    {
        private Rss20FeedFormatter _feedSerializer;
        private Type _itemType;
        private bool _preserveAttributeExtensions;
        private bool _preserveElementExtensions;
        private bool _serializeExtensionsAsAtom;

        public Rss20ItemFormatter()
            : this(typeof(SyndicationItem))
        {
        }

        public Rss20ItemFormatter(Type itemTypeToCreate)
            : base()
        {
            if (itemTypeToCreate == null)
            {
                throw new ArgumentNullException("itemTypeToCreate");
            }
            if (!typeof(SyndicationItem).IsAssignableFrom(itemTypeToCreate))
            {
                throw new ArgumentException(String.Format(SR.InvalidObjectTypePassed, "itemTypeToCreate", "SyndicationItem"));
            }
            _feedSerializer = new Rss20FeedFormatter();
            _feedSerializer.PreserveAttributeExtensions = _preserveAttributeExtensions = true;
            _feedSerializer.PreserveElementExtensions = _preserveElementExtensions = true;
            _feedSerializer.SerializeExtensionsAsAtom = _serializeExtensionsAsAtom = true;
            _itemType = itemTypeToCreate;
        }

        public Rss20ItemFormatter(SyndicationItem itemToWrite)
            : this(itemToWrite, true)
        {
        }

        public Rss20ItemFormatter(SyndicationItem itemToWrite, bool serializeExtensionsAsAtom)
            : base(itemToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            _feedSerializer = new Rss20FeedFormatter();
            _feedSerializer.PreserveAttributeExtensions = _preserveAttributeExtensions = true;
            _feedSerializer.PreserveElementExtensions = _preserveElementExtensions = true;
            _feedSerializer.SerializeExtensionsAsAtom = _serializeExtensionsAsAtom = serializeExtensionsAsAtom;
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

        public bool SerializeExtensionsAsAtom
        {
            get { return _serializeExtensionsAsAtom; }
            set
            {
                _serializeExtensionsAsAtom = value;
                _feedSerializer.SerializeExtensionsAsAtom = value;
            }
        }

        public override string Version
        {
            get { return SyndicationVersions.Rss20; }
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
                throw new ArgumentNullException("reader");
            }

            return reader.IsStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
        }

        
        async Task WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            await WriteItem(writer);
        }

        public override async Task ReadFromAsync(XmlReader reader)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(String.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI));
            }

            await ReadItemAsync(XmlReaderWrapper.CreateFromReader(reader));
        }

        public override async Task WriteToAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            XmlWriterWrapper writerWrapper = XmlWriterWrapper.CreateFromWriter(writer);

            await writerWrapper.WriteStartElementAsync(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
            await WriteItem(writer);
            await writerWrapper.WriteEndElementAsync();
        }

        protected override SyndicationItem CreateItemInstance()
        {
            return SyndicationItemFormatter.CreateItemInstance(_itemType);
        }
        
        private async Task ReadItemAsync(XmlReaderWrapper reader)
        {
            SetItem(CreateItemInstance());
            await _feedSerializer.ReadItemFromAsync(XmlReaderWrapper.CreateFromReader(XmlDictionaryReader.CreateDictionaryReader(reader)), this.Item);
        }

        private async Task WriteItem(XmlWriter writer)
        {
            if (this.Item == null)
            {
                throw new InvalidOperationException(SR.ItemFormatterDoesNotHaveItem);
            }
            XmlDictionaryWriter w = XmlDictionaryWriter.CreateDictionaryWriter(writer);
            await _feedSerializer.WriteItemContentsAsync(w, this.Item);
        }
    }

    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter<TSyndicationItem> : Rss20ItemFormatter
        where TSyndicationItem : SyndicationItem, new()
    {
        public Rss20ItemFormatter()
            : base(typeof(TSyndicationItem))
        {
        }
        public Rss20ItemFormatter(TSyndicationItem itemToWrite)
            : base(itemToWrite)
        {
        }
        public Rss20ItemFormatter(TSyndicationItem itemToWrite, bool serializeExtensionsAsAtom)
            : base(itemToWrite, serializeExtensionsAsAtom)
        {
        }

        protected override SyndicationItem CreateItemInstance()
        {
            return new TSyndicationItem();
        }
    }
}
