// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public sealed class CipherReference : EncryptedReference
    {
        private byte[]? _cipherValue;

        public CipherReference() : base()
        {
            ReferenceType = "CipherReference";
        }

        public CipherReference(string uri) : base(uri)
        {
            ReferenceType = "CipherReference";
        }

        public CipherReference(string uri, TransformChain transformChain) : base(uri, transformChain)
        {
            ReferenceType = "CipherReference";
        }

        // This method is used to cache results from resolved cipher references.
        internal byte[]? CipherValue
        {
            get
            {
                if (!CacheValid)
                    return null;
                return _cipherValue;
            }
            set
            {
                _cipherValue = value;
            }
        }

        public override XmlElement GetXml()
        {
            if (CacheValid) return _cachedXml;

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal new XmlElement GetXml(XmlDocument document)
        {
            if (ReferenceType == null)
                throw new CryptographicException(SR.Cryptography_Xml_ReferenceTypeRequired);

            // Create the Reference
            XmlElement referenceElement = document.CreateElement(ReferenceType, EncryptedXml.XmlEncNamespaceUrl);
            if (!string.IsNullOrEmpty(Uri))
                referenceElement.SetAttribute("URI", Uri);

            // Add the transforms to the CipherReference
            if (TransformChain.Count > 0)
                referenceElement.AppendChild(TransformChain.GetXml(document, EncryptedXml.XmlEncNamespaceUrl));

            return referenceElement;
        }

        [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
        public override void LoadXml(XmlElement value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ReferenceType = value.LocalName;
            string? uri = Utils.GetAttribute(value, "URI", EncryptedXml.XmlEncNamespaceUrl);
            Uri = uri ?? throw new CryptographicException(SR.Cryptography_Xml_UriRequired);

            // Transforms
            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            XmlNode? transformsNode = value.SelectSingleNode("enc:Transforms", nsm);
            if (transformsNode != null)
                TransformChain.LoadXml((transformsNode as XmlElement)!);

            // cache the Xml
            _cachedXml = value;
        }
    }
}
