// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    // sealed because the ctor results in a call to the virtual InsertItem method
    public sealed class SyndicationElementExtensionCollection : Collection<SyndicationElementExtension>
    {
        private XmlBuffer _buffer;
        private bool _initialized;

        internal SyndicationElementExtensionCollection()
            : this((XmlBuffer)null)
        {
        }

        internal SyndicationElementExtensionCollection(XmlBuffer buffer)
            : base()
        {
            _buffer = buffer;
            if (_buffer != null)
            {
                PopulateElements();
            }
            _initialized = true;
        }

        internal SyndicationElementExtensionCollection(SyndicationElementExtensionCollection source)
            : base()
        {
            _buffer = source._buffer;
            for (int i = 0; i < source.Items.Count; ++i)
            {
                base.Add(source.Items[i]);
            }
            _initialized = true;
        }

        public void Add(object extension)
        {
            if (extension is SyndicationElementExtension)
            {
                base.Add((SyndicationElementExtension)extension);
            }
            else
            {
                this.Add(extension, (DataContractSerializer)null);
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
                throw new ArgumentNullException(nameof(dataContractExtension));
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
                throw new ArgumentNullException(nameof(xmlSerializerExtension));
            }
            if (serializer == null)
            {
                serializer = new XmlSerializer(xmlSerializerExtension.GetType());
            }
            base.Add(new SyndicationElementExtension(xmlSerializerExtension, serializer));
        }

        public void Add(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            base.Add(new SyndicationElementExtension(reader));
        }

        public async Task<XmlReader> GetReaderAtElementExtensions()
        {
            XmlBuffer extensionsBuffer = await GetOrCreateBufferOverExtensions();
            XmlReader reader = extensionsBuffer.GetReader(0);
            reader.ReadStartElement();
            return reader;
        }

        public Task<Collection<TExtension>> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace)
        {
            return ReadElementExtensions<TExtension>(extensionName, extensionNamespace, new DataContractSerializer(typeof(TExtension)));
        }

        public Task<Collection<TExtension>> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace, XmlObjectSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            return ReadExtensions<TExtension>(extensionName, extensionNamespace, serializer, null);
        }

        public Task<Collection<TExtension>> ReadElementExtensions<TExtension>(string extensionName, string extensionNamespace, XmlSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            return ReadExtensions<TExtension>(extensionName, extensionNamespace, null, serializer);
        }

        internal async Task WriteToAsync(XmlWriter writer)
        {
            if (_buffer != null)
            {
                using (XmlDictionaryReader reader = _buffer.GetReader(0))
                {
                    reader.ReadStartElement();
                    while (reader.IsStartElement())
                    {
                        await writer.WriteNodeAsync(reader, false);
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Items.Count; ++i)
                {
                    await this.Items[i].WriteToAsync(writer);
                }
            }
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            // clear the cached buffer if the operation is happening outside the constructor
            if (_initialized)
            {
                _buffer = null;
            }
        }

        protected override void InsertItem(int index, SyndicationElementExtension item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            base.InsertItem(index, item);
            // clear the cached buffer if the operation is happening outside the constructor
            if (_initialized)
            {
                _buffer = null;
            }
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            // clear the cached buffer if the operation is happening outside the constructor
            if (_initialized)
            {
                _buffer = null;
            }
        }

        protected override void SetItem(int index, SyndicationElementExtension item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            base.SetItem(index, item);
            // clear the cached buffer if the operation is happening outside the constructor
            if (_initialized)
            {
                _buffer = null;
            }
        }

        private async Task<XmlBuffer> GetOrCreateBufferOverExtensions()
        {
            if (_buffer != null)
            {
                return _buffer;
            }
            XmlBuffer newBuffer = new XmlBuffer(int.MaxValue);
            using (XmlWriter writer = newBuffer.OpenSection(XmlDictionaryReaderQuotas.Max))
            {
                writer.WriteStartElement(Rss20Constants.ExtensionWrapperTag);
                for (int i = 0; i < this.Count; ++i)
                {
                    await this[i].WriteToAsync(writer);
                }
                writer.WriteEndElement();
            }
            newBuffer.CloseSection();
            newBuffer.Close();
            _buffer = newBuffer;
            return newBuffer;
        }

        private void PopulateElements()
        {
            using (XmlDictionaryReader reader = _buffer.GetReader(0))
            {
                reader.ReadStartElement();
                int index = 0;
                while (reader.IsStartElement())
                {
                    base.Add(new SyndicationElementExtension(_buffer, index, reader.LocalName, reader.NamespaceURI));
                    reader.Skip();
                    ++index;
                }
            }
        }

        private async Task<Collection<TExtension>> ReadExtensions<TExtension>(string extensionName, string extensionNamespace, XmlObjectSerializer dcSerializer, XmlSerializer xmlSerializer)
        {
            if (string.IsNullOrEmpty(extensionName))
            {
                throw new ArgumentNullException(SR.ExtensionNameNotSpecified);
            }
            Debug.Assert((dcSerializer == null) != (xmlSerializer == null), "exactly one serializer should be supplied");
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
                    results.Add(await this[i].GetObject<TExtension>(dcSerializer));
                }
                else
                {
                    results.Add(await this[i].GetObject<TExtension>(xmlSerializer));
                }
            }
            return results;
        }
    }
}
