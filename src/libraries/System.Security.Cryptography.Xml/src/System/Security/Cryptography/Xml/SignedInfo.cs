// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
    public class SignedInfo : ICollection
    {
        private string? _id;
        private string? _canonicalizationMethod;
        private string? _signatureMethod;
        private string? _signatureLength;
        private readonly ArrayList _references;
        private XmlElement? _cachedXml;
        private SignedXml? _signedXml;
        private Transform? _canonicalizationMethodTransform;

        internal SignedXml? SignedXml
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

        public string? Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _cachedXml = null;
            }
        }

        [AllowNull]
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
                    _canonicalizationMethodTransform = CryptoHelpers.CreateFromName<Transform>(CanonicalizationMethod);
                    if (_canonicalizationMethodTransform == null)
                        throw new CryptographicException(SR.Format(SR.Cryptography_Xml_CreateTransformFailed, CanonicalizationMethod));
                    _canonicalizationMethodTransform.SignedXml = SignedXml;
                    _canonicalizationMethodTransform.Reference = null;
                }
                return _canonicalizationMethodTransform;
            }
        }

        public string? SignatureMethod
        {
            get { return _signatureMethod; }
            set
            {
                _signatureMethod = value;
                _cachedXml = null;
            }
        }

        public string? SignatureLength
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

        [MemberNotNullWhen(true, nameof(_cachedXml))]
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
                Reference reference = (Reference)_references[i]!;
                signedInfoElement.AppendChild(reference.GetXml(document));
            }

            return signedInfoElement;
        }

        public void LoadXml(XmlElement value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // SignedInfo
            XmlElement signedInfoElement = value;
            if (!signedInfoElement.LocalName.Equals("SignedInfo"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo");

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            int expectedChildNodes = 0;

            // Id attribute -- optional
            _id = Utils.GetAttribute(signedInfoElement, "Id", SignedXml.XmlDsigNamespaceUrl);
            if (!Utils.VerifyAttributes(signedInfoElement, "Id"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo");

            // CanonicalizationMethod -- must be present
            XmlNodeList? canonicalizationMethodNodes = signedInfoElement.SelectNodes("ds:CanonicalizationMethod", nsm);
            if (canonicalizationMethodNodes == null || canonicalizationMethodNodes.Count == 0 || canonicalizationMethodNodes.Count > 1)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/CanonicalizationMethod");
            XmlElement canonicalizationMethodElement = (canonicalizationMethodNodes.Item(0) as XmlElement)!;
            expectedChildNodes += canonicalizationMethodNodes.Count;
            _canonicalizationMethod = Utils.GetAttribute(canonicalizationMethodElement, "Algorithm", SignedXml.XmlDsigNamespaceUrl);
            if (_canonicalizationMethod == null || !Utils.VerifyAttributes(canonicalizationMethodElement, "Algorithm"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/CanonicalizationMethod");
            _canonicalizationMethodTransform = null;
            if (canonicalizationMethodElement.ChildNodes.Count > 0)
                CanonicalizationMethodObject.LoadInnerXml(canonicalizationMethodElement.ChildNodes);

            // SignatureMethod -- must be present
            XmlNodeList? signatureMethodNodes = signedInfoElement.SelectNodes("ds:SignatureMethod", nsm);
            if (signatureMethodNodes == null || signatureMethodNodes.Count == 0 || signatureMethodNodes.Count > 1)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/SignatureMethod");
            XmlElement signatureMethodElement = (signatureMethodNodes.Item(0) as XmlElement)!;
            expectedChildNodes += signatureMethodNodes.Count;
            _signatureMethod = Utils.GetAttribute(signatureMethodElement, "Algorithm", SignedXml.XmlDsigNamespaceUrl);
            if (_signatureMethod == null || !Utils.VerifyAttributes(signatureMethodElement, "Algorithm"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/SignatureMethod");

            // Now get the output length if we are using a MAC algorithm
            XmlElement? signatureLengthElement = signatureMethodElement.SelectSingleNode("ds:HMACOutputLength", nsm) as XmlElement;
            if (signatureLengthElement != null)
                _signatureLength = signatureLengthElement.InnerXml;

            // flush out any reference that was there
            _references.Clear();

            // Reference - 0 or more
            XmlNodeList? referenceNodes = signedInfoElement.SelectNodes("ds:Reference", nsm);
            if (referenceNodes != null)
            {
                if (referenceNodes.Count > Utils.MaxReferencesPerSignedInfo)
                {
                    throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/Reference");
                }
                foreach (XmlNode node in referenceNodes)
                {
                    XmlElement referenceElement = (node as XmlElement)!;
                    Reference reference = new Reference();
                    AddReference(reference);
                    reference.LoadXml(referenceElement);
                }
                expectedChildNodes += referenceNodes.Count;
                // Verify that there aren't any extra nodes that aren't allowed
                if (signedInfoElement.SelectNodes("*")!.Count != expectedChildNodes)
                {
                    throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo");
                }
            }

            // Save away the cached value
            _cachedXml = signedInfoElement;
        }

        public void AddReference(Reference reference)
        {
            if (reference is null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            reference.SignedXml = SignedXml;
            _references.Add(reference);
        }
    }
}
