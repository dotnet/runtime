// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class XmlSyndicationContent : SyndicationContent
    {
        private XmlBuffer _contentBuffer;
        private SyndicationElementExtension _extension;
        private string _type;

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
                        _type = value;
                    }
                    else if (!FeedUtils.IsXmlns(name, ns))
                    {
                        base.AttributeExtensions.Add(new XmlQualifiedName(name, ns), value);
                    }
                }
                reader.MoveToElement();
            }
            _type = string.IsNullOrEmpty(_type) ? Atom10Constants.XmlMediaType : _type;
            _contentBuffer = new XmlBuffer(int.MaxValue);
            using (XmlDictionaryWriter writer = _contentBuffer.OpenSection(XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteNode(reader, false);
            }
            _contentBuffer.CloseSection();
            _contentBuffer.Close();
        }

        public XmlSyndicationContent(string type, object dataContractExtension, XmlObjectSerializer dataContractSerializer)
        {
            _type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            _extension = new SyndicationElementExtension(dataContractExtension, dataContractSerializer);
        }

        public XmlSyndicationContent(string type, object xmlSerializerExtension, XmlSerializer serializer)
        {
            _type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            _extension = new SyndicationElementExtension(xmlSerializerExtension, serializer);
        }

        public XmlSyndicationContent(string type, SyndicationElementExtension extension)
        {
            if (extension == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("extension");
            }
            _type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            _extension = extension;
        }

        protected XmlSyndicationContent(XmlSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            _contentBuffer = source._contentBuffer;
            _extension = source._extension;
            _type = source._type;
        }

        public SyndicationElementExtension Extension
        {
            get
            {
                return _extension;
            }
        }

        public override string Type
        {
            get { return _type; }
        }

        public override SyndicationContent Clone()
        {
            return new XmlSyndicationContent(this);
        }

        public XmlDictionaryReader GetReaderAtContent()
        {
            EnsureContentBuffer();
            return _contentBuffer.GetReader(0);
        }

        public TContent ReadContent<TContent>()
        {
            return ReadContent<TContent>((DataContractSerializer)null);
        }

        public TContent ReadContent<TContent>(XmlObjectSerializer dataContractSerializer)
        {
            if (dataContractSerializer == null)
            {
                dataContractSerializer = new DataContractSerializer(typeof(TContent));
            }
            if (_extension != null)
            {
                return _extension.GetObject<TContent>(dataContractSerializer);
            }
            else
            {
                Debug.Assert(_contentBuffer != null, "contentBuffer cannot be null");
                using (XmlDictionaryReader reader = _contentBuffer.GetReader(0))
                {
                    // skip past the content element
                    reader.ReadStartElement();
                    return (TContent)dataContractSerializer.ReadObject(reader, false);
                }
            }
        }

        public TContent ReadContent<TContent>(XmlSerializer serializer)
        {
            if (serializer == null)
            {
                serializer = new XmlSerializer(typeof(TContent));
            }
            if (_extension != null)
            {
                return _extension.GetObject<TContent>(serializer);
            }
            else
            {
                Debug.Assert(_contentBuffer != null, "contentBuffer cannot be null");
                using (XmlDictionaryReader reader = _contentBuffer.GetReader(0))
                {
                    // skip past the content element
                    reader.ReadStartElement();
                    return (TContent)serializer.Deserialize(reader);
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
            if (_extension != null)
            {
                _extension.WriteTo(writer);
            }
            else if (_contentBuffer != null)
            {
                using (XmlDictionaryReader reader = _contentBuffer.GetReader(0))
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

        private void EnsureContentBuffer()
        {
            if (_contentBuffer == null)
            {
                XmlBuffer tmp = new XmlBuffer(int.MaxValue);
                using (XmlDictionaryWriter writer = tmp.OpenSection(XmlDictionaryReaderQuotas.Max))
                {
                    this.WriteTo(writer, Atom10Constants.ContentTag, Atom10Constants.Atom10Namespace);
                }
                tmp.CloseSection();
                tmp.Close();
                _contentBuffer = tmp;
            }
        }
    }
}
