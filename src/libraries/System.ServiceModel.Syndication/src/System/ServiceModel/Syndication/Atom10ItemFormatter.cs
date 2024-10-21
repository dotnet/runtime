// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.ServiceModel.Syndication
{
    [XmlRoot(ElementName = Atom10Constants.EntryTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10ItemFormatter : SyndicationItemFormatter, IXmlSerializable
    {
        private readonly Atom10FeedFormatter _feedSerializer = new Atom10FeedFormatter();
        private bool _preserveAttributeExtensions = true;
        private bool _preserveElementExtensions = true;

        public Atom10ItemFormatter() : this(typeof(SyndicationItem))
        {
        }

        public Atom10ItemFormatter(Type itemTypeToCreate) : base()
        {
            if (itemTypeToCreate is null)
            {
                throw new ArgumentNullException(nameof(itemTypeToCreate));
            }

            if (!typeof(SyndicationItem).IsAssignableFrom(itemTypeToCreate))
            {
                throw new ArgumentException(SR.Format(SR.InvalidObjectTypePassed, nameof(itemTypeToCreate), nameof(SyndicationItem)), nameof(itemTypeToCreate));
            }

            ItemType = itemTypeToCreate;
        }

        public Atom10ItemFormatter(SyndicationItem itemToWrite) : base(itemToWrite)
        {
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

        public override string Version => SyndicationVersions.Atom10;

        protected Type ItemType { get; }

        public override bool CanRead(XmlReader reader)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return reader.IsStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
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

            writer.WriteStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
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

    [XmlRoot(ElementName = Atom10Constants.EntryTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10ItemFormatter<TSyndicationItem> : Atom10ItemFormatter where TSyndicationItem : SyndicationItem, new()
    {
        public Atom10ItemFormatter() : base(typeof(TSyndicationItem))
        {
        }

        public Atom10ItemFormatter(TSyndicationItem itemToWrite) : base(itemToWrite)
        {
        }

        protected override SyndicationItem CreateItemInstance() => new TSyndicationItem();
    }
}
