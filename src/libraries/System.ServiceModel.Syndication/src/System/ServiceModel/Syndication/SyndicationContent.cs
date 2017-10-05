// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    public abstract class SyndicationContent
    {
        private Dictionary<XmlQualifiedName, string> _attributeExtensions;

        protected SyndicationContent()
        {
        }

        protected SyndicationContent(SyndicationContent source)
        {
            CopyAttributeExtensions(source);
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

        public abstract string Type
        {
            get;
        }

        public static TextSyndicationContent CreateHtmlContent(string content)
        {
            return new TextSyndicationContent(content, TextSyndicationContentKind.Html);
        }

        public static TextSyndicationContent CreatePlaintextContent(string content)
        {
            return new TextSyndicationContent(content);
        }

        public static UrlSyndicationContent CreateUrlContent(Uri url, string mediaType)
        {
            return new UrlSyndicationContent(url, mediaType);
        }

        public static TextSyndicationContent CreateXhtmlContent(string content)
        {
            return new TextSyndicationContent(content, TextSyndicationContentKind.XHtml);
        }

        public static XmlSyndicationContent CreateXmlContent(object dataContractObject)
        {
            return new XmlSyndicationContent(Atom10Constants.XmlMediaType, dataContractObject, (DataContractSerializer)null);
        }

        public static XmlSyndicationContent CreateXmlContent(object dataContractObject, XmlObjectSerializer dataContractSerializer)
        {
            return new XmlSyndicationContent(Atom10Constants.XmlMediaType, dataContractObject, dataContractSerializer);
        }

        public static XmlSyndicationContent CreateXmlContent(XmlReader XmlReaderWrapper)
        {
            return new XmlSyndicationContent(XmlReaderWrapper);
        }

        public static XmlSyndicationContent CreateXmlContent(object xmlSerializerObject, XmlSerializer serializer)
        {
            return new XmlSyndicationContent(Atom10Constants.XmlMediaType, xmlSerializerObject, serializer);
        }

        public abstract SyndicationContent Clone();

        public async Task WriteToAsync(XmlWriter writer, string outerElementName, string outerElementNamespace)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (string.IsNullOrEmpty(outerElementName))
            {
                throw new ArgumentException(SR.OuterElementNameNotSpecified);
            }

            writer = XmlWriterWrapper.CreateFromWriter(writer);

            await writer.WriteStartElementAsync(outerElementName, outerElementNamespace);
            await writer.WriteAttributeStringAsync(Atom10Constants.TypeTag, string.Empty, this.Type);
            if (_attributeExtensions != null)
            {
                foreach (XmlQualifiedName key in _attributeExtensions.Keys)
                {
                    if (key.Name == Atom10Constants.TypeTag && key.Namespace == string.Empty)
                    {
                        continue;
                    }
                    string attrValue;
                    if (_attributeExtensions.TryGetValue(key, out attrValue))
                    {
                        await writer.WriteAttributeStringAsync(key.Name, key.Namespace, attrValue);
                    }
                }
            }
            WriteContentsTo(writer);
            await writer.WriteEndElementAsync();
        }

        internal void CopyAttributeExtensions(SyndicationContent source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source._attributeExtensions != null)
            {
                foreach (XmlQualifiedName key in source._attributeExtensions.Keys)
                {
                    this.AttributeExtensions.Add(key, source._attributeExtensions[key]);
                }
            }
        }

        protected abstract void WriteContentsTo(XmlWriter writer);
    }
}
