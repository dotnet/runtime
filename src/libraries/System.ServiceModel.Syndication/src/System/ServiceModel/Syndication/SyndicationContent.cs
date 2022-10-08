// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace System.ServiceModel.Syndication
{
    public abstract class SyndicationContent
    {
        private Dictionary<XmlQualifiedName, string> _attributeExtensions;

        protected SyndicationContent()
        {
        }

        protected SyndicationContent(SyndicationContent source) => CopyAttributeExtensions(source);

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get => _attributeExtensions ??= new Dictionary<XmlQualifiedName, string>();
        }

        public abstract string Type { get; }

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

        public static XmlSyndicationContent CreateXmlContent(XmlReader xmlReader)
        {
            return new XmlSyndicationContent(xmlReader);
        }

        public static XmlSyndicationContent CreateXmlContent(object xmlSerializerObject, XmlSerializer serializer)
        {
            return new XmlSyndicationContent(Atom10Constants.XmlMediaType, xmlSerializerObject, serializer);
        }

        public abstract SyndicationContent Clone();

        public void WriteTo(XmlWriter writer, string outerElementName, string outerElementNamespace)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (string.IsNullOrEmpty(outerElementName))
            {
                throw new ArgumentException(SR.OuterElementNameNotSpecified, nameof(outerElementName));
            }

            writer.WriteStartElement(outerElementName, outerElementNamespace);
            writer.WriteAttributeString(Atom10Constants.TypeTag, string.Empty, Type);
            if (_attributeExtensions != null)
            {
                foreach (XmlQualifiedName key in _attributeExtensions.Keys)
                {
                    if (key.Name == Atom10Constants.TypeTag && key.Namespace == string.Empty)
                    {
                        continue;
                    }

                    writer.WriteAttributeString(key.Name, key.Namespace, _attributeExtensions[key]);
                }
            }
            WriteContentsTo(writer);
            writer.WriteEndElement();
        }

        internal void CopyAttributeExtensions(SyndicationContent source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source._attributeExtensions != null)
            {
                foreach (XmlQualifiedName key in source._attributeExtensions.Keys)
                {
                    AttributeExtensions.Add(key, source._attributeExtensions[key]);
                }
            }
        }

        protected abstract void WriteContentsTo(XmlWriter writer);
    }
}
