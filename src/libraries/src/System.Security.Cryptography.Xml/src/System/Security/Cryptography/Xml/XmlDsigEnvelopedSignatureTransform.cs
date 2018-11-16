// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace System.Security.Cryptography.Xml
{
    public class XmlDsigEnvelopedSignatureTransform : Transform
    {
        private Type[] _inputTypes = { typeof(Stream), typeof(XmlNodeList), typeof(XmlDocument) };
        private Type[] _outputTypes = { typeof(XmlNodeList), typeof(XmlDocument) };
        private XmlNodeList _inputNodeList;
        private bool _includeComments = false;
        private XmlNamespaceManager _nsm = null;
        private XmlDocument _containingDocument = null;
        private int _signaturePosition = 0;

        internal int SignaturePosition
        {
            set { _signaturePosition = value; }
        }

        public XmlDsigEnvelopedSignatureTransform()
        {
            Algorithm = SignedXml.XmlDsigEnvelopedSignatureTransformUrl;
        }

        /// <internalonly/>
        public XmlDsigEnvelopedSignatureTransform(bool includeComments)
        {
            _includeComments = includeComments;
            Algorithm = SignedXml.XmlDsigEnvelopedSignatureTransformUrl;
        }

        public override Type[] InputTypes
        {
            get { return _inputTypes; }
        }

        public override Type[] OutputTypes
        {
            get { return _outputTypes; }
        }

        // An enveloped signature has no inner XML elements
        public override void LoadInnerXml(XmlNodeList nodeList)
        {
            if (nodeList != null && nodeList.Count > 0)
                throw new CryptographicException(SR.Cryptography_Xml_UnknownTransform);
        }

        // An enveloped signature has no inner XML elements
        protected override XmlNodeList GetInnerXml()
        {
            return null;
        }

        public override void LoadInput(object obj)
        {
            if (obj is Stream)
            {
                LoadStreamInput((Stream)obj);
                return;
            }
            if (obj is XmlNodeList)
            {
                LoadXmlNodeListInput((XmlNodeList)obj);
                return;
            }
            if (obj is XmlDocument)
            {
                LoadXmlDocumentInput((XmlDocument)obj);
                return;
            }
        }

        private void LoadStreamInput(Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            XmlResolver resolver = (ResolverSet ? _xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), BaseURI));
            XmlReader xmlReader = Utils.PreProcessStreamInput(stream, resolver, BaseURI);
            doc.Load(xmlReader);
            _containingDocument = doc;
            if (_containingDocument == null)
                throw new CryptographicException(SR.Cryptography_Xml_EnvelopedSignatureRequiresContext);
            _nsm = new XmlNamespaceManager(_containingDocument.NameTable);
            _nsm.AddNamespace("dsig", SignedXml.XmlDsigNamespaceUrl);
        }

        private void LoadXmlNodeListInput(XmlNodeList nodeList)
        {
            // Empty node list is not acceptable
            if (nodeList == null)
                throw new ArgumentNullException(nameof(nodeList));
            _containingDocument = Utils.GetOwnerDocument(nodeList);
            if (_containingDocument == null)
                throw new CryptographicException(SR.Cryptography_Xml_EnvelopedSignatureRequiresContext);

            _nsm = new XmlNamespaceManager(_containingDocument.NameTable);
            _nsm.AddNamespace("dsig", SignedXml.XmlDsigNamespaceUrl);
            _inputNodeList = nodeList;
        }

        private void LoadXmlDocumentInput(XmlDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            _containingDocument = doc;
            _nsm = new XmlNamespaceManager(_containingDocument.NameTable);
            _nsm.AddNamespace("dsig", SignedXml.XmlDsigNamespaceUrl);
        }

        public override object GetOutput()
        {
            if (_containingDocument == null)
                throw new CryptographicException(SR.Cryptography_Xml_EnvelopedSignatureRequiresContext);

            // If we have received an XmlNodeList as input
            if (_inputNodeList != null)
            {
                // If the position has not been set, then we don't want to remove any signature tags
                if (_signaturePosition == 0) return _inputNodeList;
                XmlNodeList signatureList = _containingDocument.SelectNodes("//dsig:Signature", _nsm);
                if (signatureList == null) return _inputNodeList;

                CanonicalXmlNodeList resultNodeList = new CanonicalXmlNodeList();
                foreach (XmlNode node in _inputNodeList)
                {
                    if (node == null) continue;
                    // keep namespaces
                    if (Utils.IsXmlNamespaceNode(node) || Utils.IsNamespaceNode(node))
                    {
                        resultNodeList.Add(node);
                    }
                    else
                    {
                        // SelectSingleNode throws an exception for xmldecl PI for example, so we will just ignore those exceptions
                        try
                        {
                            // Find the nearest signature ancestor tag 
                            XmlNode result = node.SelectSingleNode("ancestor-or-self::dsig:Signature[1]", _nsm);
                            int position = 0;
                            foreach (XmlNode node1 in signatureList)
                            {
                                position++;
                                if (node1 == result) break;
                            }
                            if (result == null || position != _signaturePosition)
                            {
                                resultNodeList.Add(node);
                            }
                        }
                        catch { }
                    }
                }
                return resultNodeList;
            }
            // Else we have received either a stream or a document as input
            else
            {
                XmlNodeList signatureList = _containingDocument.SelectNodes("//dsig:Signature", _nsm);
                if (signatureList == null) return _containingDocument;
                if (signatureList.Count < _signaturePosition || _signaturePosition <= 0) return _containingDocument;

                // Remove the signature node with all its children nodes
                signatureList[_signaturePosition - 1].ParentNode.RemoveChild(signatureList[_signaturePosition - 1]);
                return _containingDocument;
            }
        }

        public override object GetOutput(Type type)
        {
            if (type == typeof(XmlNodeList) || type.IsSubclassOf(typeof(XmlNodeList)))
            {
                if (_inputNodeList == null)
                {
                    _inputNodeList = Utils.AllDescendantNodes(_containingDocument, true);
                }
                return (XmlNodeList)GetOutput();
            }
            else if (type == typeof(XmlDocument) || type.IsSubclassOf(typeof(XmlDocument)))
            {
                if (_inputNodeList != null) throw new ArgumentException(SR.Cryptography_Xml_TransformIncorrectInputType, nameof(type));
                return (XmlDocument)GetOutput();
            }
            else
            {
                throw new ArgumentException(SR.Cryptography_Xml_TransformIncorrectInputType, nameof(type));
            }
        }
    }
}
