//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Runtime.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public abstract class SyndicationContent
    {
        Dictionary<XmlQualifiedName, string> attributeExtensions;

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
                if (this.attributeExtensions == null)
                {
                    this.attributeExtensions = new Dictionary<XmlQualifiedName, string>();
                }
                return this.attributeExtensions;
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
            return new XmlSyndicationContent(Atom10Constants.XmlMediaType, dataContractObject, (DataContractSerializer) null);
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
            if (writer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writer");
            }
            if (string.IsNullOrEmpty(outerElementName))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(SR.GetString(SR.OuterElementNameNotSpecified));
            }
            writer.WriteStartElement(outerElementName, outerElementNamespace);
            writer.WriteAttributeString(Atom10Constants.TypeTag, string.Empty, this.Type);
            if (this.attributeExtensions != null)
            {
                foreach (XmlQualifiedName key in this.attributeExtensions.Keys)
                {
                    if (key.Name == Atom10Constants.TypeTag && key.Namespace == string.Empty)
                    {
                        continue;
                    }
                    string attrValue;
                    if (this.attributeExtensions.TryGetValue(key, out attrValue))
                    {
                        writer.WriteAttributeString(key.Name, key.Namespace, attrValue);
                    }
                }
            }
            WriteContentsTo(writer);
            writer.WriteEndElement();
        }

        internal void CopyAttributeExtensions(SyndicationContent source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            if (source.attributeExtensions != null)
            {
                foreach (XmlQualifiedName key in source.attributeExtensions.Keys)
                {
                    this.AttributeExtensions.Add(key, source.attributeExtensions[key]);
                }
            }
        }

        protected abstract void WriteContentsTo(XmlWriter writer);
    }
}
