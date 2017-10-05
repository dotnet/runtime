// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

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
                throw new ArgumentNullException(nameof(reader));
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
                throw new ArgumentNullException(nameof(extension));
            }
            _type = string.IsNullOrEmpty(type) ? Atom10Constants.XmlMediaType : type;
            _extension = extension;
        }

        protected XmlSyndicationContent(XmlSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
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

        public async Task<XmlDictionaryReader> GetReaderAtContent()
        {
            await EnsureContentBufferAsync();
            return _contentBuffer.GetReader(0);
        }

        public Task<TContent> ReadContent<TContent>()
        {
            return ReadContent<TContent>((DataContractSerializer)null);
        }

        public async Task<TContent> ReadContent<TContent>(XmlObjectSerializer dataContractSerializer)
        {
            if (dataContractSerializer == null)
            {
                dataContractSerializer = new DataContractSerializer(typeof(TContent));
            }
            if (_extension != null)
            {
                return await _extension.GetObject<TContent>(dataContractSerializer);
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

        public Task<TContent> ReadContent<TContent>(XmlSerializer serializer)
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
                    return Task.FromResult((TContent)serializer.Deserialize(reader));
                    //return (TContent)serializer.Deserialize(reader);
                }
            }
        }

        // does not write start element or type attribute, writes other attributes and rest of content
        protected override void WriteContentsTo(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (_extension != null)
            {
                _extension.WriteToAsync(writer).GetAwaiter().GetResult();
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

        private async Task EnsureContentBufferAsync()
        {
            if (_contentBuffer == null)
            {
                XmlBuffer tmp = new XmlBuffer(int.MaxValue);
                using (XmlDictionaryWriter writer = tmp.OpenSection(XmlDictionaryReaderQuotas.Max))
                {
                    await this.WriteToAsync(writer, Atom10Constants.ContentTag, Atom10Constants.Atom10Namespace);
                }
                tmp.CloseSection();
                tmp.Close();
                _contentBuffer = tmp;
            }
        }
    }
}
