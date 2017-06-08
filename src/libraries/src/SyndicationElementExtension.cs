//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System.IO;
    using System.Runtime;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Runtime.CompilerServices;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class SyndicationElementExtension
    {
        XmlBuffer buffer;
        int bufferElementIndex;
        // extensionData and extensionDataWriter are only present on the send side
        object extensionData;
        ExtensionDataWriter extensionDataWriter;
        string outerName;
        string outerNamespace;

        public SyndicationElementExtension(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("xmlReader");
            }
            SyndicationFeedFormatter.MoveToStartElement(xmlReader);
            this.outerName = xmlReader.LocalName;
            this.outerNamespace = xmlReader.NamespaceURI;
            this.buffer = new XmlBuffer(int.MaxValue);
            using (XmlDictionaryWriter writer = this.buffer.OpenSection(XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                writer.WriteNode(xmlReader, false);
                writer.WriteEndElement();
            }
            buffer.CloseSection();
            buffer.Close();
            this.bufferElementIndex = 0;
        }

        public SyndicationElementExtension(object dataContractExtension)
            : this(dataContractExtension, (XmlObjectSerializer) null)
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
            if (dataContractExtension == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("dataContractExtension");
            }
            if (outerName == string.Empty)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(SR.GetString(SR.OuterNameOfElementExtensionEmpty));
            }
            if (dataContractSerializer == null)
            {
                dataContractSerializer = new DataContractSerializer(dataContractExtension.GetType());
            }
            this.outerName = outerName;
            this.outerNamespace = outerNamespace;
            this.extensionData = dataContractExtension;
            this.extensionDataWriter = new ExtensionDataWriter(this.extensionData, dataContractSerializer, this.outerName, this.outerNamespace);
        }

        public SyndicationElementExtension(object xmlSerializerExtension, XmlSerializer serializer)
        {
            if (xmlSerializerExtension == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("xmlSerializerExtension");
            }
            if (serializer == null)
            {
                serializer = new XmlSerializer(xmlSerializerExtension.GetType());
            }
            this.extensionData = xmlSerializerExtension;
            this.extensionDataWriter = new ExtensionDataWriter(this.extensionData, serializer);
        }

        internal SyndicationElementExtension(XmlBuffer buffer, int bufferElementIndex, string outerName, string outerNamespace)
        {
            this.buffer = buffer;
            this.bufferElementIndex = bufferElementIndex;
            this.outerName = outerName;
            this.outerNamespace = outerNamespace;
        }

        public string OuterName
        {
            get
            {
                if (this.outerName == null)
                {
                    EnsureOuterNameAndNs();
                }
                return this.outerName;
            }
        }

        public string OuterNamespace
        {
            get
            {
                if (this.outerName == null)
                {
                    EnsureOuterNameAndNs();
                }
                return this.outerNamespace;
            }
        }

        public TExtension GetObject<TExtension>()
        {
            return GetObject<TExtension>(new DataContractSerializer(typeof(TExtension)));
        }

        public TExtension GetObject<TExtension>(XmlObjectSerializer serializer)
        {
            if (serializer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("serializer");
            }
            if (this.extensionData != null && typeof(TExtension).IsAssignableFrom(extensionData.GetType()))
            {
                return (TExtension) this.extensionData;
            }
            using (XmlReader reader = GetReader())
            {
                return (TExtension) serializer.ReadObject(reader, false);
            }
        }

        public TExtension GetObject<TExtension>(XmlSerializer serializer)
        {
            if (serializer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("serializer");
            }
            if (this.extensionData != null && typeof(TExtension).IsAssignableFrom(extensionData.GetType()))
            {
                return (TExtension) this.extensionData;
            }
            using (XmlReader reader = GetReader())
            {
                return (TExtension) serializer.Deserialize(reader);
            }
        }

        public XmlReader GetReader()
        {
            this.EnsureBuffer();
            XmlReader reader = this.buffer.GetReader(0);
            int index = 0;
            reader.ReadStartElement(Rss20Constants.ExtensionWrapperTag);
            while (reader.IsStartElement())
            {
                if (index == this.bufferElementIndex)
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
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (this.extensionDataWriter != null)
            {
                this.extensionDataWriter.WriteTo(writer);
            }
            else
            {
                using (XmlReader reader = GetReader())
                {
                    writer.WriteNode(reader, false);
                }
            }
        }

        void EnsureBuffer()
        {
            if (this.buffer == null)
            {
                this.buffer = new XmlBuffer(int.MaxValue);
                using (XmlDictionaryWriter writer = this.buffer.OpenSection(XmlDictionaryReaderQuotas.Max))
                {
                    writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                    this.WriteTo(writer);
                    writer.WriteEndElement();
                }
                buffer.CloseSection();
                buffer.Close();
                this.bufferElementIndex = 0;
            }
        }

        void EnsureOuterNameAndNs()
        {
            //Fx.Assert(this.extensionDataWriter != null, "outer name is null only for datacontract and xmlserializer cases");
            this.extensionDataWriter.ComputeOuterNameAndNs(out this.outerName, out this.outerNamespace);
        }

        // this class holds the extension data and the associated serializer (either DataContractSerializer or XmlSerializer but not both)
        class ExtensionDataWriter
        {
            readonly XmlObjectSerializer dataContractSerializer;
            readonly object extensionData;
            readonly string outerName;
            readonly string outerNamespace;
            readonly XmlSerializer xmlSerializer;

            public ExtensionDataWriter(object extensionData, XmlObjectSerializer dataContractSerializer, string outerName, string outerNamespace)
            {
                //Fx.Assert(extensionData != null && dataContractSerializer != null, "null check");
                this.dataContractSerializer = dataContractSerializer;
                this.extensionData = extensionData;
                this.outerName = outerName;
                this.outerNamespace = outerNamespace;
            }

            public ExtensionDataWriter(object extensionData, XmlSerializer serializer)
            {
                //Fx.Assert(extensionData != null && serializer != null, "null check");
                this.xmlSerializer = serializer;
                this.extensionData = extensionData;
            }

            public void WriteTo(XmlWriter writer)
            {
                if (this.xmlSerializer != null)
                {
                    //Fx.Assert((this.dataContractSerializer == null && this.outerName == null && this.outerNamespace == null), "Xml serializer cannot have outer name, ns");
                    this.xmlSerializer.Serialize(writer, this.extensionData);
                }
                else
                {
                    //Fx.Assert(this.xmlSerializer == null, "Xml serializer cannot be configured");
                    if (this.outerName != null)
                    {
                        writer.WriteStartElement(outerName, outerNamespace);
                        this.dataContractSerializer.WriteObjectContent(writer, this.extensionData);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        this.dataContractSerializer.WriteObject(writer, this.extensionData);
                    }
                }
            }

            internal void ComputeOuterNameAndNs(out string name, out string ns)
            {
                if (this.outerName != null)
                {
                    //Fx.Assert(this.xmlSerializer == null, "outer name is not null for data contract extension only");
                    name = this.outerName;
                    ns = this.outerNamespace;
                }
                else if (this.dataContractSerializer != null)
                {
                    //Fx.Assert(this.xmlSerializer == null, "only one of xmlserializer or datacontract serializer can be present");
                    XsdDataContractExporter dcExporter = new XsdDataContractExporter();
                    XmlQualifiedName qName = dcExporter.GetRootElementName(this.extensionData.GetType());
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
                    //Fx.Assert(this.dataContractSerializer == null, "only one of xmlserializer or datacontract serializer can be present");
                    XmlReflectionImporter importer = new XmlReflectionImporter();
                    XmlTypeMapping typeMapping = importer.ImportTypeMapping(this.extensionData.GetType());
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
                        this.WriteTo(writer);
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
