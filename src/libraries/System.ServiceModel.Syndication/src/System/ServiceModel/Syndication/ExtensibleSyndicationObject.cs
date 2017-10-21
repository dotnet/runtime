// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Xml;
    using System.Threading.Tasks;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    internal struct ExtensibleSyndicationObject : IExtensibleSyndicationObject
    {
        private Dictionary<XmlQualifiedName, string> _attributeExtensions;
        private SyndicationElementExtensionCollection _elementExtensions;

        private ExtensibleSyndicationObject(ExtensibleSyndicationObject source)
        {
            if (source._attributeExtensions != null)
            {
                _attributeExtensions = new Dictionary<XmlQualifiedName, string>();
                foreach (XmlQualifiedName key in source._attributeExtensions.Keys)
                {
                    _attributeExtensions.Add(key, source._attributeExtensions[key]);
                }
            }
            else
            {
                _attributeExtensions = null;
            }
            if (source._elementExtensions != null)
            {
                _elementExtensions = new SyndicationElementExtensionCollection(source._elementExtensions);
            }
            else
            {
                _elementExtensions = null;
            }
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get
            {
                if (_attributeExtensions == null)
                {
                    _attributeExtensions = new Dictionary<XmlQualifiedName, string>();
                }
                return _attributeExtensions;
            }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get
            {
                if (_elementExtensions == null)
                {
                    _elementExtensions = new SyndicationElementExtensionCollection();
                }
                return _elementExtensions;
            }
        }

        private static XmlBuffer CreateXmlBuffer(XmlDictionaryReader unparsedExtensionsReader, int maxExtensionSize)
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
                throw new ArgumentNullException(nameof(readerOverUnparsedExtensions));
            }

            if (maxExtensionSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExtensionSize));
            }
            XmlDictionaryReader r = XmlDictionaryReader.CreateDictionaryReader(readerOverUnparsedExtensions);
            _elementExtensions = new SyndicationElementExtensionCollection(CreateXmlBuffer(r, maxExtensionSize));
        }


        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            _elementExtensions = new SyndicationElementExtensionCollection(buffer);
        }

        internal void WriteAttributeExtensions(XmlWriter writer)
        {
            WriteAttributeExtensionsAsync(writer).GetAwaiter().GetResult();
        }

        internal void WriteElementExtensions(XmlWriter writer)
        {
            WriteElementExtensionsAsync(writer).GetAwaiter().GetResult();
        }

        internal async Task WriteAttributeExtensionsAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            if (_attributeExtensions != null)
            {
                foreach (XmlQualifiedName qname in _attributeExtensions.Keys)
                {
                    string value = _attributeExtensions[qname];
                    await writer.WriteAttributeStringAsync(qname.Name, qname.Namespace, value);
                }
            }
        }

        internal async Task WriteElementExtensionsAsync(XmlWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_elementExtensions != null)
            {
                await _elementExtensions.WriteToAsync(writer);
            }
        }

        public ExtensibleSyndicationObject Clone()
        {
            return new ExtensibleSyndicationObject(this);
        }
    }
}
