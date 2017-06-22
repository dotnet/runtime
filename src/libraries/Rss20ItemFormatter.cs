// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml.Schema;
    using System.Runtime.CompilerServices;
    using Microsoft.ServiceModel.Syndication.Resources;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter : SyndicationItemFormatter, IXmlSerializable
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

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            ReadItem(reader);
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            WriteItem(writer);
        }

        public override void ReadFrom(XmlReader reader)
        {
            if (!CanRead(reader))
            {
                throw new XmlException(String.Format(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI));
            }
            ReadItem(reader);
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            writer.WriteStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
            WriteItem(writer);
            writer.WriteEndElement();
        }

        protected override SyndicationItem CreateItemInstance()
        {
            return SyndicationItemFormatter.CreateItemInstance(_itemType);
        }

        private void ReadItem(XmlReader reader)
        {
            SetItem(CreateItemInstance());
            _feedSerializer.ReadItemFrom(XmlDictionaryReader.CreateDictionaryReader(reader), this.Item);
        }

        private void WriteItem(XmlWriter writer)
        {
            if (this.Item == null)
            {
                throw new InvalidOperationException(SR.ItemFormatterDoesNotHaveItem);
            }
            XmlDictionaryWriter w = XmlDictionaryWriter.CreateDictionaryWriter(writer);
            _feedSerializer.WriteItemContents(w, this.Item);
        }
    }

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter<TSyndicationItem> : Rss20ItemFormatter, IXmlSerializable
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
