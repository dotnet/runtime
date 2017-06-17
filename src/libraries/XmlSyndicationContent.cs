// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using System.Runtime;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Runtime.CompilerServices;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class XmlSyndicationContent : SyndicationContent
    {
        XmlBuffer contentBuffer;
        SyndicationElementExtension extension;
        string type;

        // Saves the element in the reader to the buffer (attributes preserved)
        // Type is populated from type attribute on reader
        // Reader must be positioned at an element
        public XmlSyndicationContent(XmlReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("reader");
            }
            SyndicationFeedFormatter.MoveToStartElement(reader);
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    string name = reader.LocalName;
                    string ns = reader.NamespaceURI;
                    string value = reader.Value;
                    if (name == Atom10Constants.TypeTag && ns == string.Empty)
                    {
                        this.type = value;
                    }
                    else if (!FeedUtils.IsXmlns(name, ns))
                    {
                        base.AttributeExtensions.Add(new XmlQualifiedName(name, ns), value);
                    }
                }
                reader.MoveToElement();
            }
            this.type = string.IsNullOrEmpty(this.type) ? Atom10Constants.XmlMediaType : this.type;
            this.contentBuffer = new XmlBuffer(int.MaxValue);
            using (XmlDictionaryWriter writer = this.contentBuffer.OpenSection(XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteNode(reader, false);
            }
            contentBuffer.CloseSection();
            contentBuffer.Close();
        }

        public XmlSyndicationContent(string type, object dataContractExtension, XmlObjectSerializer dataContractSerializer)
        {
            this.type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            this.extension = new SyndicationElementExtension(dataContractExtension, dataContractSerializer);
        }

        public XmlSyndicationContent(string type, object xmlSerializerExtension, XmlSerializer serializer)
        {
            this.type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            this.extension = new SyndicationElementExtension(xmlSerializerExtension, serializer);
        }

        public XmlSyndicationContent(string type, SyndicationElementExtension extension)
        {
            if (extension == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("extension");
            }
            this.type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            this.extension = extension;
        }

        protected XmlSyndicationContent(XmlSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            this.contentBuffer = source.contentBuffer;
            this.extension = source.extension;
            this.type = source.type;
        }

        public SyndicationElementExtension Extension
        {
            get
            {
                return this.extension;
            }
        }

        public override string Type
        {
            get { return this.type; }
        }

        public override SyndicationContent Clone()
        {
            return new XmlSyndicationContent(this);
        }

        public XmlDictionaryReader GetReaderAtContent()
        {
            EnsureContentBuffer();
            return this.contentBuffer.GetReader(0);
        }

        public TContent ReadContent<TContent>()
        {
            return ReadContent<TContent>((DataContractSerializer) null);
        }

        public TContent ReadContent<TContent>(XmlObjectSerializer dataContractSerializer)
        {
            if (dataContractSerializer == null)
            {
                dataContractSerializer = new DataContractSerializer(typeof(TContent));
            }
            if (this.extension != null)
            {
                return this.extension.GetObject<TContent>(dataContractSerializer);
            }
            else
            {
                //Fx.Assert(this.contentBuffer != null, "contentBuffer cannot be null");
                using (XmlDictionaryReader reader = this.contentBuffer.GetReader(0))
                {
                    // skip past the content element
                    reader.ReadStartElement();
                    return (TContent) dataContractSerializer.ReadObject(reader, false);
                }
            }
        }

        public TContent ReadContent<TContent>(XmlSerializer serializer)
        {
            if (serializer == null)
            {
                serializer = new XmlSerializer(typeof(TContent));
            }
            if (this.extension != null)
            {
                return this.extension.GetObject<TContent>(serializer);
            }
            else
            {
                //Fx.Assert(this.contentBuffer != null, "contentBuffer cannot be null");
                using (XmlDictionaryReader reader = this.contentBuffer.GetReader(0))
                {
                    // skip past the content element
                    reader.ReadStartElement();
                    return (TContent) serializer.Deserialize(reader);
                }
            }
        }

        // does not write start element or type attribute, writes other attributes and rest of content
        protected override void WriteContentsTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.extension != null)
            {
                this.extension.WriteTo(writer);
            }
            else if (this.contentBuffer != null)
            {
                using (XmlDictionaryReader reader = this.contentBuffer.GetReader(0))
                {
                    reader.MoveToStartElement();
                    if (!reader.IsEmptyElement)
                    {
                        reader.ReadStartElement();
                        while (reader.Depth >= 1 && reader.ReadState == ReadState.Interactive)
                        {
                            writer.WriteNode(reader, false);
                        }
                    }
                }
            }
        }

        void EnsureContentBuffer()
        {
            if (this.contentBuffer == null)
            {
                XmlBuffer tmp = new XmlBuffer(int.MaxValue);
                using (XmlDictionaryWriter writer = tmp.OpenSection(XmlDictionaryReaderQuotas.Max))
                {
                    this.WriteTo(writer, Atom10Constants.ContentTag, Atom10Constants.Atom10Namespace);
                }
                tmp.CloseSection();
                tmp.Close();
                this.contentBuffer = tmp;
            }
        }
    }
}
