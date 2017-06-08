//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
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

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter : SyndicationItemFormatter, IXmlSerializable
    {
        Rss20FeedFormatter feedSerializer;
        Type itemType;
        bool preserveAttributeExtensions;
        bool preserveElementExtensions;
        bool serializeExtensionsAsAtom;

        public Rss20ItemFormatter()
            : this(typeof(SyndicationItem))
        {
        }

        public Rss20ItemFormatter(Type itemTypeToCreate)
            : base()
        {
            if (itemTypeToCreate == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("itemTypeToCreate");
            }
            if (!typeof(SyndicationItem).IsAssignableFrom(itemTypeToCreate))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("itemTypeToCreate",
                    SR.GetString(SR.InvalidObjectTypePassed, "itemTypeToCreate", "SyndicationItem"));
            }
            this.feedSerializer = new Rss20FeedFormatter();
            this.feedSerializer.PreserveAttributeExtensions = this.preserveAttributeExtensions = true;
            this.feedSerializer.PreserveElementExtensions = this.preserveElementExtensions = true;
            this.feedSerializer.SerializeExtensionsAsAtom = this.serializeExtensionsAsAtom = true;
            this.itemType = itemTypeToCreate;
        }

        public Rss20ItemFormatter(SyndicationItem itemToWrite)
            : this(itemToWrite, true)
        {
        }

        public Rss20ItemFormatter(SyndicationItem itemToWrite, bool serializeExtensionsAsAtom)
            : base(itemToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            this.feedSerializer = new Rss20FeedFormatter();
            this.feedSerializer.PreserveAttributeExtensions = this.preserveAttributeExtensions = true;
            this.feedSerializer.PreserveElementExtensions = this.preserveElementExtensions = true;
            this.feedSerializer.SerializeExtensionsAsAtom = this.serializeExtensionsAsAtom = serializeExtensionsAsAtom;
            this.itemType = itemToWrite.GetType();
        }

        public bool PreserveAttributeExtensions
        {
            get { return this.preserveAttributeExtensions; }
            set
            {
                this.preserveAttributeExtensions = value;
                this.feedSerializer.PreserveAttributeExtensions = value;
            }
        }

        public bool PreserveElementExtensions
        {
            get { return this.preserveElementExtensions; }
            set
            {
                this.preserveElementExtensions = value;
                this.feedSerializer.PreserveElementExtensions = value;
            }
        }

        public bool SerializeExtensionsAsAtom
        {
            get { return this.serializeExtensionsAsAtom; }
            set
            {
                this.serializeExtensionsAsAtom = value;
                this.feedSerializer.SerializeExtensionsAsAtom = value;
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
                return this.itemType;
            }
        }

        public override bool CanRead(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            SyndicationFeedFormatter.TraceItemReadBegin();
            ReadItem(reader);
            SyndicationFeedFormatter.TraceItemReadEnd();
        }

        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "The IXmlSerializable implementation is only for exposing under WCF DataContractSerializer. The funcionality is exposed to derived class through the ReadFrom\\WriteTo methods")]
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            SyndicationFeedFormatter.TraceItemWriteBegin();
            WriteItem(writer);
            SyndicationFeedFormatter.TraceItemWriteEnd();
        }

        public override void ReadFrom(XmlReader reader)
        {
            SyndicationFeedFormatter.TraceItemReadBegin();
            if (!CanRead(reader))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.GetString(SR.UnknownItemXml, reader.LocalName, reader.NamespaceURI)));
            }
            ReadItem(reader);
            SyndicationFeedFormatter.TraceItemReadEnd();
        }

        public override void WriteTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            SyndicationFeedFormatter.TraceItemWriteBegin();
            writer.WriteStartElement(Rss20Constants.ItemTag, Rss20Constants.Rss20Namespace);
            WriteItem(writer);
            writer.WriteEndElement();
            SyndicationFeedFormatter.TraceItemWriteEnd();
        }

        protected override SyndicationItem CreateItemInstance()
        {
            return SyndicationItemFormatter.CreateItemInstance(this.itemType);
        }

        void ReadItem(XmlReader reader)
        {
            SetItem(CreateItemInstance());
            feedSerializer.ReadItemFrom(XmlDictionaryReader.CreateDictionaryReader(reader), this.Item);
        }

        void WriteItem(XmlWriter writer)
        {
            if (this.Item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.GetString(SR.ItemFormatterDoesNotHaveItem)));
            }
            XmlDictionaryWriter w = XmlDictionaryWriter.CreateDictionaryWriter(writer);
            feedSerializer.WriteItemContents(w, this.Item);
        }
    }

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [XmlRoot(ElementName = Rss20Constants.ItemTag, Namespace = Rss20Constants.Rss20Namespace)]
    public class Rss20ItemFormatter<TSyndicationItem> : Rss20ItemFormatter, IXmlSerializable
        where TSyndicationItem : SyndicationItem, new ()
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
