// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public sealed class CipherReference : EncryptedReference
    {
        private byte[] _cipherValue;

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
        internal byte[] CipherValue
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

        new internal XmlElement GetXml(XmlDocument document)
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

        public override void LoadXml(XmlElement value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ReferenceType = value.LocalName;
            Uri = Utils.GetAttribute(value, "URI", EncryptedXml.XmlEncNamespaceUrl);

            // Transforms
            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            XmlNode transformsNode = value.SelectSingleNode("enc:Transforms", nsm);
            if (transformsNode != null)
                TransformChain.LoadXml(transformsNode as XmlElement);

            // cache the Xml
            _cachedXml = value;
        }
    }
}
