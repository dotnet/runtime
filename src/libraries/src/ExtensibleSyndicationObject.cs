//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using System.Xml;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    struct ExtensibleSyndicationObject : IExtensibleSyndicationObject
    {
        Dictionary<XmlQualifiedName, string> attributeExtensions;
        SyndicationElementExtensionCollection elementExtensions;

        ExtensibleSyndicationObject(ExtensibleSyndicationObject source)
        {
            if (source.attributeExtensions != null)
            {
                this.attributeExtensions = new Dictionary<XmlQualifiedName, string>();
                foreach (XmlQualifiedName key in source.attributeExtensions.Keys)
                {
                    this.attributeExtensions.Add(key, source.attributeExtensions[key]);
                }
            }
            else
            {
                this.attributeExtensions = null;
            }
            if (source.elementExtensions != null)
            {
                this.elementExtensions = new SyndicationElementExtensionCollection(source.elementExtensions);
            }
            else
            {
                this.elementExtensions = null;
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions 
        {
            get
            {
                if (this.attributeExtensions == null)
                {
                    this.attributeExtensions = new Dictionary<XmlQualifiedName, string>();
                }
                return this.attributeExtensions;
            }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get
            {
                if (this.elementExtensions == null)
                {
                    this.elementExtensions = new SyndicationElementExtensionCollection();
                }
                return this.elementExtensions;
            }
        }

        static XmlBuffer CreateXmlBuffer(XmlDictionaryReader unparsedExtensionsReader, int maxExtensionSize)
        {
            XmlBuffer buffer = new XmlBuffer(maxExtensionSize);
            using (XmlDictionaryWriter writer = buffer.OpenSection(unparsedExtensionsReader.Quotas))
            {
                writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                while (unparsedExtensionsReader.IsStartElement())
                {
                    writer.WriteNode(unparsedExtensionsReader, false);
                }
                writer.WriteEndElement();
            }
            buffer.CloseSection();
            buffer.Close();
            return buffer;
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            if (readerOverUnparsedExtensions == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("readerOverUnparsedExtensions");
            }
            if (maxExtensionSize < 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxExtensionSize"));
            }
            XmlDictionaryReader r = XmlDictionaryReader.CreateDictionaryReader(readerOverUnparsedExtensions);
            this.elementExtensions = new SyndicationElementExtensionCollection(CreateXmlBuffer(r, maxExtensionSize));
        }


        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            this.elementExtensions = new SyndicationElementExtensionCollection(buffer);
        }

        internal void WriteAttributeExtensions(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.attributeExtensions != null)
            {
                foreach (XmlQualifiedName qname in this.attributeExtensions.Keys)
                {
                    string value = this.attributeExtensions[qname];
                    writer.WriteAttributeString(qname.Name, qname.Namespace, value);
                }
            }
        }

        internal void WriteElementExtensions(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.elementExtensions != null)
            {
                this.elementExtensions.WriteTo(writer);
            }
        }

        public ExtensibleSyndicationObject Clone()
        {
            return new ExtensibleSyndicationObject(this);
        }
    }
}
