// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.ServiceModel.Syndication
{
    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter : SyndicationItemFormatter, IXmlSerializable
    {
        private readonly Rss20FeedFormatter _feedSerializer;
        private bool _preserveAttributeExtensions = true;
        private bool _preserveElementExtensions = true;
        private bool _serializeExtensionsAsAtom;

        public Rss20ItemFormatter() : this(typeof(SyndicationItem))
        {
        }

        public Rss20ItemFormatter(Type itemTypeToCreate) : base()
        {
            if (itemTypeToCreate is null)
            {
                throw new ArgumentNullException(nameof(itemTypeToCreate));
            }

            if (!typeof(SyndicationItem).IsAssignableFrom(itemTypeToCreate))
            {
                throw new ArgumentException(SR.Format(SR.InvalidObjectTypePassed, nameof(itemTypeToCreate), nameof(SyndicationItem)), nameof(itemTypeToCreate));
            }

            _feedSerializer = new Rss20FeedFormatter
            {
                SerializeExtensionsAsAtom = _serializeExtensionsAsAtom = true
            };
            ItemType = itemTypeToCreate;
        }

        public Rss20ItemFormatter(SyndicationItem itemToWrite) : this(itemToWrite, true)
        {
        }

        public Rss20ItemFormatter(SyndicationItem itemToWrite, bool serializeExtensionsAsAtom) : base(itemToWrite)
        {
            _feedSerializer = new Rss20FeedFormatter
            {
                SerializeExtensionsAsAtom = _serializeExtensionsAsAtom = serializeExtensionsAsAtom
            };
            ItemType = itemToWrite.GetType();
        }

        public bool PreserveAttributeExtensions
        {
            get => _preserveAttributeExtensions;
            set
            {
                _preserveAttributeExtensions = value;
                _feedSerializer.PreserveAttributeExtensions = value;
            }
        }

        public bool PreserveElementExtensions
        {
            get => _preserveElementExtensions;
            set
            {
                _preserveElementExtensions = value;
                _feedSerializer.PreserveElementExtensions = value;
            }
        }

        public bool SerializeExtensionsAsAtom
        {
            get => _serializeExtensionsAsAtom;
            set
            {
                _serializeExtensionsAsAtom = value;
                _feedSerializer.SerializeExtensionsAsAtom = value;
            }
        }

        public override string Version => SyndicationVersions.Rss20;

        protected Type ItemType { get; }

        public override bool CanRead(XmlReader reader)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return reader.IsStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
        }

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            ReadItem(reader);
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteItem(writer);
        }

        public override void ReadFrom(XmlReader reader)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(SR.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI));
            }

            ReadItem(reader);
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
            WriteItem(writer);
            writer.WriteEndElement();
        }

        protected override SyndicationItem CreateItemInstance() => CreateItemInstance(ItemType);

        private void ReadItem(XmlReader reader)
        {
            SetItem(CreateItemInstance());
            _feedSerializer.ReadItemFrom(XmlDictionaryReader.CreateDictionaryReader(reader), Item);
        }

        private void WriteItem(XmlWriter writer)
        {
            if (Item == null)
            {
                throw new InvalidOperationException(SR.ItemFormatterDoesNotHaveItem);
            }

            XmlDictionaryWriter w = XmlDictionaryWriter.CreateDictionaryWriter(writer);
            _feedSerializer.WriteItemContents(w, Item);
        }
    }

    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter<TSyndicationItem> : Rss20ItemFormatter, IXmlSerializable where TSyndicationItem : SyndicationItem, new()
    {
        public Rss20ItemFormatter() : base(typeof(TSyndicationItem))
        {
        }

        public Rss20ItemFormatter(TSyndicationItem itemToWrite) : base(itemToWrite)
        {
        }

        public Rss20ItemFormatter(TSyndicationItem itemToWrite, bool serializeExtensionsAsAtom) : base(itemToWrite, serializeExtensionsAsAtom)
        {
        }

        protected override SyndicationItem CreateItemInstance() => new TSyndicationItem();
    }
}
