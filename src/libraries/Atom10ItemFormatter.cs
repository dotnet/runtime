//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

/*
 * Some diagnostic lines have been commented
 * 
 * */

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
    using System.ServiceModel.Channels;
    using Microsoft.ServiceModel.Syndication.Resources;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    [XmlRoot(ElementName = Atom10Constants.EntryTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10ItemFormatter : SyndicationItemFormatter, IXmlSerializable
    {
        Atom10FeedFormatter feedSerializer;
        Type itemType;
        bool preserveAttributeExtensions;
        bool preserveElementExtensions;

        public Atom10ItemFormatter()
            : this(typeof(SyndicationItem))
        {
        }

        public Atom10ItemFormatter(Type itemTypeToCreate)
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
            this.feedSerializer = new Atom10FeedFormatter();
            this.feedSerializer.PreserveAttributeExtensions = this.preserveAttributeExtensions = true;
            this.feedSerializer.PreserveElementExtensions = this.preserveElementExtensions = true;
            this.itemType = itemTypeToCreate;
        }

        public Atom10ItemFormatter(SyndicationItem itemToWrite)
            : base(itemToWrite)
        {
            // No need to check that the parameter passed is valid - it is checked by the c'tor of the base class
            this.feedSerializer = new Atom10FeedFormatter();
            this.feedSerializer.PreserveAttributeExtensions = this.preserveAttributeExtensions = true;
            this.feedSerializer.PreserveElementExtensions = this.preserveElementExtensions = true;
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

        public override string Version
        {
            get { return SyndicationVersions.Atom10; }
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
            return reader.IsStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
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
            writer.WriteStartElement(Atom10Constants.EntryTag, Atom10Constants.Atom10Namespace);
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
    [XmlRoot(ElementName = Atom10Constants.EntryTag, Namespace = Atom10Constants.Atom10Namespace)]
    public class Atom10ItemFormatter<TSyndicationItem> : Atom10ItemFormatter
        where TSyndicationItem : SyndicationItem, new ()
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
