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
    // XML Decryption Transform is used to specify the order of XML Digital Signature 
    // and XML Encryption when performed on the same document.

    public class XmlDecryptionTransform : Transform
    {
        private Type[] _inputTypes = { typeof(Stream), typeof(XmlDocument) };
        private Type[] _outputTypes = { typeof(XmlDocument) };
        private XmlNodeList _encryptedDataList = null;
        private ArrayList _arrayListUri = null; // this ArrayList object represents the Uri's to be excluded
        private EncryptedXml _exml = null; // defines the XML encryption processing rules
        private XmlDocument _containingDocument = null;
        private XmlNamespaceManager _nsm = null;
        private const string XmlDecryptionTransformNamespaceUrl = "http://www.w3.org/2002/07/decrypt#";

        public XmlDecryptionTransform()
        {
            Algorithm = SignedXml.XmlDecryptionTransformUrl;
        }

        private ArrayList ExceptUris
        {
            get
            {
                if (_arrayListUri == null)
                    _arrayListUri = new ArrayList();
                return _arrayListUri;
            }
        }

        protected virtual bool IsTargetElement(XmlElement inputElement, string idValue)
        {
            if (inputElement == null)
                return false;
            if (inputElement.GetAttribute("Id") == idValue || inputElement.GetAttribute("id") == idValue ||
                inputElement.GetAttribute("ID") == idValue)
                return true;

            return false;
        }

        public EncryptedXml EncryptedXml
        {
            get
            {
                if (_exml != null)
                    return _exml;

                Reference reference = Reference;
                SignedXml signedXml = (reference == null ? SignedXml : reference.SignedXml);
                if (signedXml == null || signedXml.EncryptedXml == null)
                    _exml = new EncryptedXml(_containingDocument); // default processing rules
                else
                    _exml = signedXml.EncryptedXml;

                return _exml;
            }
            set { _exml = value; }
        }

        public override Type[] InputTypes
        {
            get { return _inputTypes; }
        }

        public override Type[] OutputTypes
        {
            get { return _outputTypes; }
        }

        public void AddExceptUri(string uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            ExceptUris.Add(uri);
        }

        public override void LoadInnerXml(XmlNodeList nodeList)
        {
            if (nodeList == null)
                throw new CryptographicException(SR.Cryptography_Xml_UnknownTransform);
            ExceptUris.Clear();
            foreach (XmlNode node in nodeList)
            {
                XmlElement elem = node as XmlElement;
                if (elem != null && elem.LocalName == "Except" && elem.NamespaceURI == XmlDecryptionTransformNamespaceUrl)
                {
                    // the Uri is required
                    string uri = Utils.GetAttribute(elem, "URI", XmlDecryptionTransformNamespaceUrl);
                    if (uri == null || uri.Length == 0 || uri[0] != '#')
                        throw new CryptographicException(SR.Cryptography_Xml_UriRequired);
                    string idref = Utils.ExtractIdFromLocalUri(uri);
                    ExceptUris.Add(idref);
                }
            }
        }

        protected override XmlNodeList GetInnerXml()
        {
            if (ExceptUris.Count == 0)
                return null;
            XmlDocument document = new XmlDocument();
            XmlElement element = document.CreateElement("Transform", SignedXml.XmlDsigNamespaceUrl);
            if (!string.IsNullOrEmpty(Algorithm))
                element.SetAttribute("Algorithm", Algorithm);
            foreach (string uri in ExceptUris)
            {
                XmlElement exceptUriElement = document.CreateElement("Except", XmlDecryptionTransformNamespaceUrl);
                exceptUriElement.SetAttribute("URI", uri);
                element.AppendChild(exceptUriElement);
            }
            return element.ChildNodes;
        }

        public override void LoadInput(object obj)
        {
            if (obj is Stream)
            {
                LoadStreamInput((Stream)obj);
            }
            else if (obj is XmlDocument)
            {
                LoadXmlDocumentInput((XmlDocument)obj);
            }
        }

        private void LoadStreamInput(Stream stream)
        {
            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            XmlResolver resolver = (ResolverSet ? _xmlResolver : new XmlSecureResolver(new XmlUrlResolver(), BaseURI));
            XmlReader xmlReader = Utils.PreProcessStreamInput(stream, resolver, BaseURI);
            document.Load(xmlReader);
            _containingDocument = document;
            _nsm = new XmlNamespaceManager(_containingDocument.NameTable);
            _nsm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            // select all EncryptedData elements
            _encryptedDataList = document.SelectNodes("//enc:EncryptedData", _nsm);
        }

        private void LoadXmlDocumentInput(XmlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            _containingDocument = document;
            _nsm = new XmlNamespaceManager(document.NameTable);
            _nsm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            // select all EncryptedData elements
            _encryptedDataList = document.SelectNodes("//enc:EncryptedData", _nsm);
        }

        // Replace the encrytped XML element with the decrypted data for signature verification
        private void ReplaceEncryptedData(XmlElement encryptedDataElement, byte[] decrypted)
        {
            XmlNode parent = encryptedDataElement.ParentNode;
            if (parent.NodeType == XmlNodeType.Document)
            {
                // We're replacing the root element.  In order to correctly reflect the semantics of the
                // decryption transform, we need to replace the entire document with the decrypted data. 
                // However, EncryptedXml.ReplaceData will preserve other top-level elements such as the XML
                // entity declaration and top level comments.  So, in this case we must do the replacement
                // ourselves.
                parent.InnerXml = EncryptedXml.Encoding.GetString(decrypted);
            }
            else
            {
                // We're replacing a node in the middle of the document - EncryptedXml knows how to handle
                // this case in conformance with the transform's requirements, so we'll just defer to it.
                EncryptedXml.ReplaceData(encryptedDataElement, decrypted);
            }
        }

        private bool ProcessEncryptedDataItem(XmlElement encryptedDataElement)
        {
            // first see whether we want to ignore this one
            if (ExceptUris.Count > 0)
            {
                for (int index = 0; index < ExceptUris.Count; index++)
                {
                    if (IsTargetElement(encryptedDataElement, (string)ExceptUris[index]))
                        return false;
                }
            }
            EncryptedData ed = new EncryptedData();
            ed.LoadXml(encryptedDataElement);
            SymmetricAlgorithm symAlg = EncryptedXml.GetDecryptionKey(ed, null);
            if (symAlg == null)
                throw new CryptographicException(SR.Cryptography_Xml_MissingDecryptionKey);
            byte[] decrypted = EncryptedXml.DecryptData(ed, symAlg);

            ReplaceEncryptedData(encryptedDataElement, decrypted);
            return true;
        }

        private void ProcessElementRecursively(XmlNodeList encryptedDatas)
        {
            if (encryptedDatas == null || encryptedDatas.Count == 0)
                return;
            Queue encryptedDatasQueue = new Queue();
            foreach (XmlNode value in encryptedDatas)
            {
                encryptedDatasQueue.Enqueue(value);
            }
            XmlNode node = encryptedDatasQueue.Dequeue() as XmlNode;
            while (node != null)
            {
                XmlElement encryptedDataElement = node as XmlElement;
                if (encryptedDataElement != null && encryptedDataElement.LocalName == "EncryptedData" &&
                    encryptedDataElement.NamespaceURI == EncryptedXml.XmlEncNamespaceUrl)
                {
                    XmlNode sibling = encryptedDataElement.NextSibling;
                    XmlNode parent = encryptedDataElement.ParentNode;
                    if (ProcessEncryptedDataItem(encryptedDataElement))
                    {
                        // find the new decrypted element.
                        XmlNode child = parent.FirstChild;
                        while (child != null && child.NextSibling != sibling)
                            child = child.NextSibling;
                        if (child != null)
                        {
                            XmlNodeList nodes = child.SelectNodes("//enc:EncryptedData", _nsm);
                            if (nodes.Count > 0)
                            {
                                foreach (XmlNode value in nodes)
                                {
                                    encryptedDatasQueue.Enqueue(value);
                                }
                            }
                        }
                    }
                }
                if (encryptedDatasQueue.Count == 0)
                    break;
                node = encryptedDatasQueue.Dequeue() as XmlNode;
            }
        }

        public override object GetOutput()
        {
            // decrypt the encrypted sections
            if (_encryptedDataList != null)
                ProcessElementRecursively(_encryptedDataList);
            // propagate namespaces
            Utils.AddNamespaces(_containingDocument.DocumentElement, PropagatedNamespaces);
            return _containingDocument;
        }

        public override object GetOutput(Type type)
        {
            if (type == typeof(XmlDocument))
                return (XmlDocument)GetOutput();
            else
                throw new ArgumentException(SR.Cryptography_Xml_TransformIncorrectInputType, nameof(type));
        }
    }
}
