//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ServiceModel.Syndication
{
    using System.Collections.ObjectModel;
    using System.Runtime;
    using System.Runtime.Serialization;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Runtime.CompilerServices;

    // sealed because the ctor results in a call to the virtual InsertItem method
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed class SyndicationElementExtensionCollection : Collection<SyndicationElementExtension>
    {
        XmlBuffer buffer;
        bool initialized;

        internal SyndicationElementExtensionCollection()
            : this((XmlBuffer) null)
        {
        }

        internal SyndicationElementExtensionCollection(XmlBuffer buffer)
            : base()
        {
            this.buffer = buffer;
            if (this.buffer != null)
            {
                PopulateElements();
            }
            initialized = true;
        }

        internal SyndicationElementExtensionCollection(SyndicationElementExtensionCollection source)
            : base()
        {
            this.buffer = source.buffer;
            for (int i = 0; i < source.Items.Count; ++i)
            {
                base.Add(source.Items[i]);
            }
            initialized = true;
        }

        public void Add(object extension)
        {
            if (extension is SyndicationElementExtension)
            {
                base.Add((SyndicationElementExtension) extension);
            }
            else
            {
                this.Add(extension, (DataContractSerializer) null);
            }
        }

        public void Add(string outerName, string outerNamespace, object dataContractExtension)
        {
            this.Add(outerName, outerNamespace, dataContractExtension, null);
        }

        public void Add(object dataContractExtension, DataContractSerializer serializer)
        {
            this.Add(null, null, dataContractExtension, serializer);
        }

        public void Add(string outerName, string outerNamespace, object dataContractExtension, XmlObjectSerializer dataContractSerializer)
        {
            if (dataContractExtension == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("dataContractExtension");
            }
            if (dataContractSerializer == null)
            {
                dataContractSerializer = new DataContractSerializer(dataContractExtension.GetType());
            }
            base.Add(new SyndicationElementExtension(outerName, outerNamespace, dataContractExtension, dataContractSerializer));
        }

        public void Add(object xmlSerializerExtension, XmlSerializer serializer)
        {
            if (xmlSerializerExtension == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("xmlSerializerExtension");
            }
            if (serializer == null)
            {
                serializer = new XmlSerializer(xmlSerializerExtension.GetType());
            }
            base.Add(new SyndicationElementExtension(xmlSerializerExtension, serializer));
        }

        public void Add(XmlReader xmlReader)
        {
            if (xmlReader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("xmlReader");
            }
            base.Add(new SyndicationElementExtension(xmlReader));
        }

        public XmlReader GetReaderAtElementExtensions()
        {
            XmlBuffer extensionsBuffer = GetOrCreateBufferOverExtensions();
            XmlReader reader = extensionsBuffer.GetReader(0);
            reader.ReadStartElement();
            return reader;
        }

        public Collection<TExtension> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace)
        {
            return ReadElementExtensions<TExtension>(extensionName, extensionNamespace, new DataContractSerializer(typeof(TExtension)));
        }

        public Collection<TExtension> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace, XmlObjectSerializer serializer)
        {
            if (serializer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("serializer");
            }
            return ReadExtensions<TExtension>(extensionName, extensionNamespace, serializer, null);
        }

        public Collection<TExtension> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace, XmlSerializer serializer)
        {
            if (serializer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("serializer");
            }
            return ReadExtensions<TExtension>(extensionName, extensionNamespace, null, serializer);
        }

        internal void WriteTo(XmlWriter writer)
        {
            if (this.buffer != null)
            {
                using (XmlDictionaryReader reader = this.buffer.GetReader(0))
                {
                    reader.ReadStartElement();
                    while (reader.IsStartElement())
                    {
                        writer.WriteNode(reader, false);
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Items.Count; ++i)
                {
                    this.Items[i].WriteTo(writer);
                }
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            // clear the cached buffer if the operation is happening outside the constructor
            if (initialized)
            {
                this.buffer = null;
            }
        }

        protected override void InsertItem(int index, SyndicationElementExtension item)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            base.InsertItem(index, item);
            // clear the cached buffer if the operation is happening outside the constructor
            if (initialized)
            {
                this.buffer = null;
            }
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            // clear the cached buffer if the operation is happening outside the constructor
            if (initialized)
            {
                this.buffer = null;
            }
        }

        protected override void SetItem(int index, SyndicationElementExtension item)
        {
            if (item == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");
            }
            base.SetItem(index, item);
            // clear the cached buffer if the operation is happening outside the constructor
            if (initialized)
            {
                this.buffer = null;
            }
        }

        XmlBuffer GetOrCreateBufferOverExtensions()
        {
            if (this.buffer != null)
            {
                return this.buffer;
            }
            XmlBuffer newBuffer = new XmlBuffer(int.MaxValue);
            using (XmlWriter writer = newBuffer.OpenSection(XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                for (int i = 0; i < this.Count; ++i)
                {
                    this[i].WriteTo(writer);
                }
                writer.WriteEndElement();
            }
            newBuffer.CloseSection();
            newBuffer.Close();
            this.buffer = newBuffer;
            return newBuffer;
        }

        void PopulateElements()
        {
            using (XmlDictionaryReader reader = this.buffer.GetReader(0))
            {
                reader.ReadStartElement();
                int index = 0;
                while (reader.IsStartElement())
                {
                    base.Add(new SyndicationElementExtension(this.buffer, index, reader.LocalName, reader.NamespaceURI));
                    reader.Skip();
                    ++index;
                }
            }
        }

        Collection<TExtension> ReadExtensions<TExtension>(string extensionName, string extensionNamespace, XmlObjectSerializer dcSerializer, XmlSerializer xmlSerializer)
        {
            if (string.IsNullOrEmpty(extensionName))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(SR.GetString(SR.ExtensionNameNotSpecified));
            }
            //Fx.Assert((dcSerializer == null) != (xmlSerializer == null), "exactly one serializer should be supplied");
            // normalize the null and empty namespace
            if (extensionNamespace == null)
            {
                extensionNamespace = string.Empty;
            }
            Collection<TExtension> results = new Collection<TExtension>();
            for (int i = 0; i < this.Count; ++i)
            {
                if (extensionName != this[i].OuterName || extensionNamespace != this[i].OuterNamespace)
                {
                    continue;
                }
                if (dcSerializer != null)
                {
                    results.Add(this[i].GetObject<TExtension>(dcSerializer));
                }
                else
                {
                    results.Add(this[i].GetObject<TExtension>(xmlSerializer));
                }
            }
            return results;
        }
    }
}
