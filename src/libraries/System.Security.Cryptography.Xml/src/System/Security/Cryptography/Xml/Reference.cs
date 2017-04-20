// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml;
using System.Globalization;
using System.Runtime.Versioning;

namespace System.Security.Cryptography.Xml
{
    public class Reference
    {
        internal const string DefaultDigestMethod = SignedXml.XmlDsigSHA256Url;

        private string _id;
        private string _uri;
        private string _type;
        private TransformChain _transformChain;
        private string _digestMethod;
        private byte[] _digestValue;
        private HashAlgorithm _hashAlgorithm;
        private object _refTarget;
        private ReferenceTargetType _refTargetType;
        private XmlElement _cachedXml;
        private SignedXml _signedXml = null;
        internal CanonicalXmlNodeList _namespaces = null;

        //
        // public constructors
        //

        public Reference()
        {
            _transformChain = new TransformChain();
            _refTarget = null;
            _refTargetType = ReferenceTargetType.UriReference;
            _cachedXml = null;
            _digestMethod = DefaultDigestMethod;
        }

        public Reference(Stream stream)
        {
            _transformChain = new TransformChain();
            _refTarget = stream;
            _refTargetType = ReferenceTargetType.Stream;
            _cachedXml = null;
            _digestMethod = DefaultDigestMethod;
        }

        public Reference(string uri)
        {
            _transformChain = new TransformChain();
            _refTarget = uri;
            _uri = uri;
            _refTargetType = ReferenceTargetType.UriReference;
            _cachedXml = null;
            _digestMethod = DefaultDigestMethod;
        }

        internal Reference(XmlElement element)
        {
            _transformChain = new TransformChain();
            _refTarget = element;
            _refTargetType = ReferenceTargetType.XmlElement;
            _cachedXml = null;
            _digestMethod = DefaultDigestMethod;
        }

        //
        // public properties
        //

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Uri
        {
            get { return _uri; }
            set
            {
                _uri = value;
                _cachedXml = null;
            }
        }

        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                _cachedXml = null;
            }
        }

        public string DigestMethod
        {
            get { return _digestMethod; }
            set
            {
                _digestMethod = value;
                _cachedXml = null;
            }
        }

        public byte[] DigestValue
        {
            get { return _digestValue; }
            set
            {
                _digestValue = value;
                _cachedXml = null;
            }
        }

        public TransformChain TransformChain
        {
            get
            {
                if (_transformChain == null)
                    _transformChain = new TransformChain();
                return _transformChain;
            }
            set
            {
                _transformChain = value;
                _cachedXml = null;
            }
        }

        internal bool CacheValid
        {
            get
            {
                return (_cachedXml != null);
            }
        }

        internal SignedXml SignedXml
        {
            get { return _signedXml; }
            set { _signedXml = value; }
        }

        internal ReferenceTargetType ReferenceTargetType
        {
            get
            {
                return _refTargetType;
            }
        }

        //
        // public methods
        //

        public XmlElement GetXml()
        {
            if (CacheValid) return (_cachedXml);

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            // Create the Reference
            XmlElement referenceElement = document.CreateElement("Reference", SignedXml.XmlDsigNamespaceUrl);

            if (!string.IsNullOrEmpty(_id))
                referenceElement.SetAttribute("Id", _id);

            if (_uri != null)
                referenceElement.SetAttribute("URI", _uri);

            if (!string.IsNullOrEmpty(_type))
                referenceElement.SetAttribute("Type", _type);

            // Add the transforms to the Reference
            if (TransformChain.Count != 0)
                referenceElement.AppendChild(TransformChain.GetXml(document, SignedXml.XmlDsigNamespaceUrl));

            // Add the DigestMethod
            if (string.IsNullOrEmpty(_digestMethod))
                throw new CryptographicException(SR.Cryptography_Xml_DigestMethodRequired);

            XmlElement digestMethodElement = document.CreateElement("DigestMethod", SignedXml.XmlDsigNamespaceUrl);
            digestMethodElement.SetAttribute("Algorithm", _digestMethod);
            referenceElement.AppendChild(digestMethodElement);

            if (DigestValue == null)
            {
                if (_hashAlgorithm.Hash == null)
                    throw new CryptographicException(SR.Cryptography_Xml_DigestValueRequired);
                DigestValue = _hashAlgorithm.Hash;
            }

            XmlElement digestValueElement = document.CreateElement("DigestValue", SignedXml.XmlDsigNamespaceUrl);
            digestValueElement.AppendChild(document.CreateTextNode(Convert.ToBase64String(_digestValue)));
            referenceElement.AppendChild(digestValueElement);

            return referenceElement;
        }

        public void LoadXml(XmlElement value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _id = Utils.GetAttribute(value, "Id", SignedXml.XmlDsigNamespaceUrl);
            _uri = Utils.GetAttribute(value, "URI", SignedXml.XmlDsigNamespaceUrl);
            _type = Utils.GetAttribute(value, "Type", SignedXml.XmlDsigNamespaceUrl);

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            // Transforms
            TransformChain = new TransformChain();
            XmlElement transformsElement = value.SelectSingleNode("ds:Transforms", nsm) as XmlElement;
            if (transformsElement != null)
            {
                XmlNodeList transformNodes = transformsElement.SelectNodes("ds:Transform", nsm);
                if (transformNodes != null)
                {
                    foreach (XmlNode transformNode in transformNodes)
                    {
                        XmlElement transformElement = transformNode as XmlElement;
                        string algorithm = Utils.GetAttribute(transformElement, "Algorithm", SignedXml.XmlDsigNamespaceUrl);
                        Transform transform = CryptoHelpers.CreateFromName(algorithm) as Transform;
                        if (transform == null)
                            throw new CryptographicException(SR.Cryptography_Xml_UnknownTransform);
                        AddTransform(transform);
                        // let the transform read the children of the transformElement for data
                        transform.LoadInnerXml(transformElement.ChildNodes);
                        // Hack! this is done to get around the lack of here() function support in XPath
                        if (transform is XmlDsigEnvelopedSignatureTransform)
                        {
                            // Walk back to the Signature tag. Find the nearest signature ancestor
                            // Signature-->SignedInfo-->Reference-->Transforms-->Transform
                            XmlNode signatureTag = transformElement.SelectSingleNode("ancestor::ds:Signature[1]", nsm);
                            XmlNodeList signatureList = transformElement.SelectNodes("//ds:Signature", nsm);
                            if (signatureList != null)
                            {
                                int position = 0;
                                foreach (XmlNode node in signatureList)
                                {
                                    position++;
                                    if (node == signatureTag)
                                    {
                                        ((XmlDsigEnvelopedSignatureTransform)transform).SignaturePosition = position;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // DigestMethod
            XmlElement digestMethodElement = value.SelectSingleNode("ds:DigestMethod", nsm) as XmlElement;
            if (digestMethodElement == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "Reference/DigestMethod");
            _digestMethod = Utils.GetAttribute(digestMethodElement, "Algorithm", SignedXml.XmlDsigNamespaceUrl);

            // DigestValue
            XmlElement digestValueElement = value.SelectSingleNode("ds:DigestValue", nsm) as XmlElement;
            if (digestValueElement == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "Reference/DigestValue");
            _digestValue = Convert.FromBase64String(Utils.DiscardWhiteSpaces(digestValueElement.InnerText));

            // cache the Xml
            _cachedXml = value;
        }

        public void AddTransform(Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            transform.Reference = this;
            TransformChain.Add(transform);
        }

        internal void UpdateHashValue(XmlDocument document, CanonicalXmlNodeList refList)
        {
            DigestValue = CalculateHashValue(document, refList);
        }

        // What we want to do is pump the input throug the TransformChain and then 
        // hash the output of the chain document is the document context for resolving relative references
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        internal byte[] CalculateHashValue(XmlDocument document, CanonicalXmlNodeList refList)
        {
            // refList is a list of elements that might be targets of references
            // Now's the time to create our hashing algorithm
            _hashAlgorithm = CryptoHelpers.CreateFromName(_digestMethod) as HashAlgorithm;
            if (_hashAlgorithm == null)
                throw new CryptographicException(SR.Cryptography_Xml_CreateHashAlgorithmFailed);

            // Let's go get the target.
            string baseUri = (document == null ? System.Environment.CurrentDirectory + "\\" : document.BaseURI);
            Stream hashInputStream = null;
            WebResponse response = null;
            Stream inputStream = null;
            XmlResolver resolver = null;
            byte[] hashval = null;

            try
            {
                switch (_refTargetType)
                {
                    case ReferenceTargetType.Stream:
                        // This is the easiest case. We already have a stream, so just pump it through the TransformChain
                        resolver = (SignedXml.ResolverSet ? SignedXml._xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
                        hashInputStream = TransformChain.TransformToOctetStream((Stream)_refTarget, resolver, baseUri);
                        break;
                    case ReferenceTargetType.UriReference:
                        // Second-easiest case -- dereference the URI & pump through the TransformChain
                        // handle the special cases where the URI is null (meaning whole doc)
                        // or the URI is just a fragment (meaning a reference to an embedded Object)
                        if (_uri == null)
                        {
                            // We need to create a DocumentNavigator out of the XmlElement
                            resolver = (SignedXml.ResolverSet ? SignedXml._xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
                            // In the case of a Uri-less reference, we will simply pass null to the transform chain.
                            // The first transform in the chain is expected to know how to retrieve the data to hash.
                            hashInputStream = TransformChain.TransformToOctetStream((Stream)null, resolver, baseUri);
                        }
                        else if (_uri.Length == 0)
                        {
                            // This is the self-referential case. First, check that we have a document context.
                            // The Enveloped Signature does not discard comments as per spec; those will be omitted during the transform chain process
                            if (document == null)
                                throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, SR.Cryptography_Xml_SelfReferenceRequiresContext, _uri));

                            // Normalize the containing document
                            resolver = (SignedXml.ResolverSet ? SignedXml._xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
                            XmlDocument docWithNoComments = Utils.DiscardComments(Utils.PreProcessDocumentInput(document, resolver, baseUri));
                            hashInputStream = TransformChain.TransformToOctetStream(docWithNoComments, resolver, baseUri);
                        }
                        else if (_uri[0] == '#')
                        {
                            // If we get here, then we are constructing a Reference to an embedded DataObject
                            // referenced by an Id = attribute. Go find the relevant object
                            bool discardComments = true;
                            string idref = Utils.GetIdFromLocalUri(_uri, out discardComments);
                            if (idref == "xpointer(/)")
                            {
                                // This is a self referencial case
                                if (document == null)
                                    throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, SR.Cryptography_Xml_SelfReferenceRequiresContext, _uri));

                                // We should not discard comments here!!!
                                resolver = (SignedXml.ResolverSet ? SignedXml._xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
                                hashInputStream = TransformChain.TransformToOctetStream(Utils.PreProcessDocumentInput(document, resolver, baseUri), resolver, baseUri);
                                break;
                            }

                            XmlElement elem = SignedXml.GetIdElement(document, idref);
                            if (elem != null)
                                _namespaces = Utils.GetPropagatedAttributes(elem.ParentNode as XmlElement);

                            if (elem == null)
                            {
                                // Go throw the referenced items passed in
                                if (refList != null)
                                {
                                    foreach (XmlNode node in refList)
                                    {
                                        XmlElement tempElem = node as XmlElement;
                                        if ((tempElem != null) && (Utils.HasAttribute(tempElem, "Id", SignedXml.XmlDsigNamespaceUrl))
                                            && (Utils.GetAttribute(tempElem, "Id", SignedXml.XmlDsigNamespaceUrl).Equals(idref)))
                                        {
                                            elem = tempElem;
                                            if (_signedXml._context != null)
                                                _namespaces = Utils.GetPropagatedAttributes(_signedXml._context);
                                            break;
                                        }
                                    }
                                }
                            }

                            if (elem == null)
                                throw new CryptographicException(SR.Cryptography_Xml_InvalidReference);

                            XmlDocument normDocument = Utils.PreProcessElementInput(elem, resolver, baseUri);
                            // Add the propagated attributes
                            Utils.AddNamespaces(normDocument.DocumentElement, _namespaces);

                            resolver = (SignedXml.ResolverSet ? SignedXml._xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
                            if (discardComments)
                            {
                                // We should discard comments before going into the transform chain
                                XmlDocument docWithNoComments = Utils.DiscardComments(normDocument);
                                hashInputStream = TransformChain.TransformToOctetStream(docWithNoComments, resolver, baseUri);
                            }
                            else
                            {
                                // This is an XPointer reference, do not discard comments!!!
                                hashInputStream = TransformChain.TransformToOctetStream(normDocument, resolver, baseUri);
                            }
                        }
                        else
                        {
                            throw new CryptographicException(SR.Cryptography_Xml_UriNotResolved, _uri);
                        }
                        break;
                    case ReferenceTargetType.XmlElement:
                        // We need to create a DocumentNavigator out of the XmlElement
                        resolver = (SignedXml.ResolverSet ? SignedXml._xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), baseUri));
                        hashInputStream = TransformChain.TransformToOctetStream(Utils.PreProcessElementInput((XmlElement)_refTarget, resolver, baseUri), resolver, baseUri);
                        break;
                    default:
                        throw new CryptographicException(SR.Cryptography_Xml_UriNotResolved, _uri);
                }

                // Compute the new hash value
                hashInputStream = SignedXmlDebugLog.LogReferenceData(this, hashInputStream);
                hashval = _hashAlgorithm.ComputeHash(hashInputStream);
            }
            finally
            {
                if (hashInputStream != null)
                    hashInputStream.Close();
                if (response != null)
                    response.Close();
                if (inputStream != null)
                    inputStream.Close();
            }

            return hashval;
        }
    }
}
