// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Xml;
using System.Globalization;

namespace System.Security.Cryptography.Xml
{
    public class SignedInfo : ICollection
    {
        private string _id;
        private string _canonicalizationMethod;
        private string _signatureMethod;
        private string _signatureLength;
        private ArrayList _references;
        private XmlElement _cachedXml = null;
        private SignedXml _signedXml = null;
        private Transform _canonicalizationMethodTransform = null;

        internal SignedXml SignedXml
        {
            get { return _signedXml; }
            set { _signedXml = value; }
        }

        public SignedInfo()
        {
            _references = new ArrayList();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsSynchronized
        {
            get { throw new NotSupportedException(); }
        }

        public object SyncRoot
        {
            get { throw new NotSupportedException(); }
        }

        //
        // public properties
        //

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _cachedXml = null;
            }
        }

        public string CanonicalizationMethod
        {
            get
            {
                // Default the canonicalization method to C14N
                if (_canonicalizationMethod == null)
                    return SignedXml.XmlDsigC14NTransformUrl;
                return _canonicalizationMethod;
            }
            set
            {
                _canonicalizationMethod = value;
                _cachedXml = null;
            }
        }

        public Transform CanonicalizationMethodObject
        {
            get
            {
                if (_canonicalizationMethodTransform == null)
                {
                    _canonicalizationMethodTransform = CryptoHelpers.CreateFromName(CanonicalizationMethod) as Transform;
                    if (_canonicalizationMethodTransform == null)
                        throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, SR.Cryptography_Xml_CreateTransformFailed, CanonicalizationMethod));
                    _canonicalizationMethodTransform.SignedXml = SignedXml;
                    _canonicalizationMethodTransform.Reference = null;
                }
                return _canonicalizationMethodTransform;
            }
        }

        public string SignatureMethod
        {
            get { return _signatureMethod; }
            set
            {
                _signatureMethod = value;
                _cachedXml = null;
            }
        }

        public string SignatureLength
        {
            get { return _signatureLength; }
            set
            {
                _signatureLength = value;
                _cachedXml = null;
            }
        }

        public ArrayList References
        {
            get { return _references; }
        }

        internal bool CacheValid
        {
            get
            {
                if (_cachedXml == null) return false;
                // now check all the references
                foreach (Reference reference in References)
                {
                    if (!reference.CacheValid) return false;
                }
                return true;
            }
        }

        //
        // public methods
        //

        public XmlElement GetXml()
        {
            if (CacheValid) return _cachedXml;

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            // Create the root element
            XmlElement signedInfoElement = document.CreateElement("SignedInfo", SignedXml.XmlDsigNamespaceUrl);
            if (!string.IsNullOrEmpty(_id))
                signedInfoElement.SetAttribute("Id", _id);

            // Add the canonicalization method, defaults to SignedXml.XmlDsigNamespaceUrl
            XmlElement canonicalizationMethodElement = CanonicalizationMethodObject.GetXml(document, "CanonicalizationMethod");
            signedInfoElement.AppendChild(canonicalizationMethodElement);

            // Add the signature method
            if (string.IsNullOrEmpty(_signatureMethod))
                throw new CryptographicException(SR.Cryptography_Xml_SignatureMethodRequired);

            XmlElement signatureMethodElement = document.CreateElement("SignatureMethod", SignedXml.XmlDsigNamespaceUrl);
            signatureMethodElement.SetAttribute("Algorithm", _signatureMethod);
            // Add HMACOutputLength tag if we have one
            if (_signatureLength != null)
            {
                XmlElement hmacLengthElement = document.CreateElement(null, "HMACOutputLength", SignedXml.XmlDsigNamespaceUrl);
                XmlText outputLength = document.CreateTextNode(_signatureLength);
                hmacLengthElement.AppendChild(outputLength);
                signatureMethodElement.AppendChild(hmacLengthElement);
            }

            signedInfoElement.AppendChild(signatureMethodElement);

            // Add the references
            if (_references.Count == 0)
                throw new CryptographicException(SR.Cryptography_Xml_ReferenceElementRequired);

            for (int i = 0; i < _references.Count; ++i)
            {
                Reference reference = (Reference)_references[i];
                signedInfoElement.AppendChild(reference.GetXml(document));
            }

            return signedInfoElement;
        }

        public void LoadXml(XmlElement value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // SignedInfo
            XmlElement signedInfoElement = value;
            if (!signedInfoElement.LocalName.Equals("SignedInfo"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo");

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            // Id attribute -- optional
            _id = Utils.GetAttribute(signedInfoElement, "Id", SignedXml.XmlDsigNamespaceUrl);

            // CanonicalizationMethod -- must be present
            XmlElement canonicalizationMethodElement = signedInfoElement.SelectSingleNode("ds:CanonicalizationMethod", nsm) as XmlElement;
            if (canonicalizationMethodElement == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/CanonicalizationMethod");
            _canonicalizationMethod = Utils.GetAttribute(canonicalizationMethodElement, "Algorithm", SignedXml.XmlDsigNamespaceUrl);
            _canonicalizationMethodTransform = null;
            if (canonicalizationMethodElement.ChildNodes.Count > 0)
                CanonicalizationMethodObject.LoadInnerXml(canonicalizationMethodElement.ChildNodes);

            // SignatureMethod -- must be present
            XmlElement signatureMethodElement = signedInfoElement.SelectSingleNode("ds:SignatureMethod", nsm) as XmlElement;
            if (signatureMethodElement == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/SignatureMethod");
            _signatureMethod = Utils.GetAttribute(signatureMethodElement, "Algorithm", SignedXml.XmlDsigNamespaceUrl);

            // Now get the output length if we are using a MAC algorithm
            XmlElement signatureLengthElement = signatureMethodElement.SelectSingleNode("ds:HMACOutputLength", nsm) as XmlElement;
            if (signatureLengthElement != null)
                _signatureLength = signatureLengthElement.InnerXml;

            // flush out any reference that was there
            _references.Clear();

            XmlNodeList referenceNodes = signedInfoElement.SelectNodes("ds:Reference", nsm);
            if (referenceNodes != null)
            {
                foreach (XmlNode node in referenceNodes)
                {
                    XmlElement referenceElement = node as XmlElement;
                    Reference reference = new Reference();
                    AddReference(reference);
                    reference.LoadXml(referenceElement);
                }
            }

            // Save away the cached value
            _cachedXml = signedInfoElement;
        }

        public void AddReference(Reference reference)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));

            reference.SignedXml = SignedXml;
            _references.Add(reference);
        }
    }
}
