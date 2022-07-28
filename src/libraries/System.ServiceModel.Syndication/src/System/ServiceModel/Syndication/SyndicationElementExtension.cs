// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;

namespace System.ServiceModel.Syndication
{
    public class SyndicationElementExtension
    {
        private XmlBuffer _buffer;
        private int _bufferElementIndex;
        // extensionData and extensionDataWriter are only present on the send side
        private readonly object _extensionData;
        private readonly ExtensionDataWriter _extensionDataWriter;
        private string _outerName;
        private string _outerNamespace;

        public SyndicationElementExtension(XmlReader xmlReader)
        {
            if (xmlReader is null)
            {
                throw new ArgumentNullException(nameof(xmlReader));
            }

            SyndicationFeedFormatter.MoveToStartElement(xmlReader);
            _outerName = xmlReader.LocalName;
            _outerNamespace = xmlReader.NamespaceURI;
            _buffer = new XmlBuffer(int.MaxValue);
            using (XmlDictionaryWriter writer = _buffer.OpenSection(XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                writer.WriteNode(xmlReader, false);
                writer.WriteEndElement();
            }
            _buffer.CloseSection();
            _buffer.Close();
            _bufferElementIndex = 0;
        }

        public SyndicationElementExtension(object dataContractExtension) : this(dataContractExtension, (XmlObjectSerializer)null)
        {
        }

        public SyndicationElementExtension(object dataContractExtension, XmlObjectSerializer dataContractSerializer)
            : this(null, null, dataContractExtension, dataContractSerializer)
        {
        }

        public SyndicationElementExtension(string outerName, string outerNamespace, object dataContractExtension)
            : this(outerName, outerNamespace, dataContractExtension, null)
        {
        }

        public SyndicationElementExtension(string outerName, string outerNamespace, object dataContractExtension, XmlObjectSerializer dataContractSerializer)
        {
            if (dataContractExtension is null)
            {
                throw new ArgumentNullException(nameof(dataContractExtension));
            }

            if (outerName == string.Empty)
            {
                throw new ArgumentException(SR.OuterNameOfElementExtensionEmpty, nameof(outerName));
            }

            dataContractSerializer ??= new DataContractSerializer(dataContractExtension.GetType());
            _outerName = outerName;
            _outerNamespace = outerNamespace;
            _extensionData = dataContractExtension;
            _extensionDataWriter = new ExtensionDataWriter(_extensionData, dataContractSerializer, _outerName, _outerNamespace);
        }

        public SyndicationElementExtension(object xmlSerializerExtension, XmlSerializer serializer)
        {
            if (xmlSerializerExtension is null)
            {
                throw new ArgumentNullException(nameof(xmlSerializerExtension));
            }

            serializer ??= new XmlSerializer(xmlSerializerExtension.GetType());
            _extensionData = xmlSerializerExtension;
            _extensionDataWriter = new ExtensionDataWriter(_extensionData, serializer);
        }

        internal SyndicationElementExtension(XmlBuffer buffer, int bufferElementIndex, string outerName, string outerNamespace)
        {
            _buffer = buffer;
            _bufferElementIndex = bufferElementIndex;
            _outerName = outerName;
            _outerNamespace = outerNamespace;
        }

        public string OuterName
        {
            get
            {
                if (_outerName == null)
                {
                    EnsureOuterNameAndNs();
                }

                return _outerName;
            }
        }

        public string OuterNamespace
        {
            get
            {
                if (_outerName == null)
                {
                    EnsureOuterNameAndNs();
                }

                return _outerNamespace;
            }
        }

        public TExtension GetObject<TExtension>() => GetObject<TExtension>(new DataContractSerializer(typeof(TExtension)));

        public TExtension GetObject<TExtension>(XmlObjectSerializer serializer)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (_extensionData != null && typeof(TExtension).IsAssignableFrom(_extensionData.GetType()))
            {
                return (TExtension)_extensionData;
            }
            using (XmlReader reader = GetReader())
            {
                return (TExtension)serializer.ReadObject(reader, false);
            }
        }

        public TExtension GetObject<TExtension>(XmlSerializer serializer)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if (_extensionData != null && typeof(TExtension).IsAssignableFrom(_extensionData.GetType()))
            {
                return (TExtension)_extensionData;
            }
            using (XmlReader reader = GetReader())
            {
                return (TExtension)serializer.Deserialize(reader);
            }
        }

        public XmlReader GetReader()
        {
            EnsureBuffer();
            XmlReader reader = _buffer.GetReader(0);
            int index = 0;
            reader.ReadStartElement(Rss20Constants.ExtensionWrapperTag);
            while (reader.IsStartElement())
            {
                if (index == _bufferElementIndex)
                {
                    break;
                }
                ++index;
                reader.Skip();
            }
            return reader;
        }

        public void WriteTo(XmlWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_extensionDataWriter != null)
            {
                _extensionDataWriter.WriteTo(writer);
            }
            else
            {
                using (XmlReader reader = GetReader())
                {
                    writer.WriteNode(reader, false);
                }
            }
        }

        private void EnsureBuffer()
        {
            if (_buffer == null)
            {
                _buffer = new XmlBuffer(int.MaxValue);
                using (XmlDictionaryWriter writer = _buffer.OpenSection(XmlDictionaryReaderQuotas.Max))
                {
                    writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                    WriteTo(writer);
                    writer.WriteEndElement();
                }
                _buffer.CloseSection();
                _buffer.Close();
                _bufferElementIndex = 0;
            }
        }

        private void EnsureOuterNameAndNs()
        {
            Debug.Assert(_extensionDataWriter != null, "outer name is null only for datacontract and xmlserializer cases");
            _extensionDataWriter.ComputeOuterNameAndNs(out _outerName, out _outerNamespace);
        }

        // this class holds the extension data and the associated serializer (either DataContractSerializer or XmlSerializer but not both)
        private sealed class ExtensionDataWriter
        {
            private readonly XmlObjectSerializer _dataContractSerializer;
            private readonly object _extensionData;
            private readonly string _outerName;
            private readonly string _outerNamespace;
            private readonly XmlSerializer _xmlSerializer;

            public ExtensionDataWriter(object extensionData, XmlObjectSerializer dataContractSerializer, string outerName, string outerNamespace)
            {
                Debug.Assert(extensionData != null && dataContractSerializer != null, "null check");
                _dataContractSerializer = dataContractSerializer;
                _extensionData = extensionData;
                _outerName = outerName;
                _outerNamespace = outerNamespace;
            }

            public ExtensionDataWriter(object extensionData, XmlSerializer serializer)
            {
                Debug.Assert(extensionData != null && serializer != null, "null check");
                _xmlSerializer = serializer;
                _extensionData = extensionData;
            }

            public void WriteTo(XmlWriter writer)
            {
                if (_xmlSerializer != null)
                {
                    Debug.Assert((_dataContractSerializer == null && _outerName == null && _outerNamespace == null), "Xml serializer cannot have outer name, ns");
                    _xmlSerializer.Serialize(writer, _extensionData);
                }
                else
                {
                    Debug.Assert(_xmlSerializer == null, "Xml serializer cannot be configured");
                    if (_outerName != null)
                    {
                        writer.WriteStartElement(_outerName, _outerNamespace);
                        _dataContractSerializer.WriteObjectContent(writer, _extensionData);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        _dataContractSerializer.WriteObject(writer, _extensionData);
                    }
                }
            }

            internal void ComputeOuterNameAndNs(out string name, out string ns)
            {
                Debug.Assert(_outerName == null, "All callers of this function should already check for a null outer name.");
                if (_dataContractSerializer != null)
                {
                    Debug.Assert(_xmlSerializer == null, "only one of xmlserializer or datacontract serializer can be present");
                    XsdDataContractExporter dcExporter = new XsdDataContractExporter();
                    XmlQualifiedName qName = dcExporter.GetRootElementName(_extensionData.GetType());
                    if (qName != null)
                    {
                        name = qName.Name;
                        ns = qName.Namespace;
                    }
                    else
                    {
                        // this can happen if an IXmlSerializable type is specified with IsAny=true
                        ReadOuterNameAndNs(out name, out ns);
                    }
                }
                else
                {
                    Debug.Assert(_dataContractSerializer == null, "only one of xmlserializer or datacontract serializer can be present");
                    XmlReflectionImporter importer = new XmlReflectionImporter();
                    XmlTypeMapping typeMapping = importer.ImportTypeMapping(_extensionData.GetType());
                    if (typeMapping != null && !string.IsNullOrEmpty(typeMapping.ElementName))
                    {
                        name = typeMapping.ElementName;
                        ns = typeMapping.Namespace;
                    }
                    else
                    {
                        // this can happen if an IXmlSerializable type is specified with IsAny=true
                        ReadOuterNameAndNs(out name, out ns);
                    }
                }
            }

            internal void ReadOuterNameAndNs(out string name, out string ns)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (XmlWriter writer = XmlWriter.Create(stream))
                    {
                        WriteTo(writer);
                    }
                    stream.Seek(0, SeekOrigin.Begin);
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        SyndicationFeedFormatter.MoveToStartElement(reader);
                        name = reader.LocalName;
                        ns = reader.NamespaceURI;
                    }
                }
            }
        }
    }
}
