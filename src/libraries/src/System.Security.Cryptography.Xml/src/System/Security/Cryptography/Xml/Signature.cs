// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class Signature
    {
        private string _id;
        private SignedInfo _signedInfo;
        private byte[] _signatureValue;
        private string _signatureValueId;
        private KeyInfo _keyInfo;
        private IList _embeddedObjects;
        private CanonicalXmlNodeList _referencedItems;
        private SignedXml _signedXml = null;

        internal SignedXml SignedXml
        {
            get { return _signedXml; }
            set { _signedXml = value; }
        }

        //
        // public constructors
        //

        public Signature()
        {
            _embeddedObjects = new ArrayList();
            _referencedItems = new CanonicalXmlNodeList();
        }

        //
        // public properties
        //

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public SignedInfo SignedInfo
        {
            get { return _signedInfo; }
            set
            {
                _signedInfo = value;
                if (SignedXml != null && _signedInfo != null)
                    _signedInfo.SignedXml = SignedXml;
            }
        }

        public byte[] SignatureValue
        {
            get { return _signatureValue; }
            set { _signatureValue = value; }
        }

        public KeyInfo KeyInfo
        {
            get
            {
                if (_keyInfo == null)
                    _keyInfo = new KeyInfo();
                return _keyInfo;
            }
            set { _keyInfo = value; }
        }

        public IList ObjectList
        {
            get { return _embeddedObjects; }
            set { _embeddedObjects = value; }
        }

        internal CanonicalXmlNodeList ReferencedItems
        {
            get { return _referencedItems; }
        }

        //
        // public methods
        //

        public XmlElement GetXml()
        {
            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            // Create the Signature
            XmlElement signatureElement = (XmlElement)document.CreateElement("Signature", SignedXml.XmlDsigNamespaceUrl);
            if (!string.IsNullOrEmpty(_id))
                signatureElement.SetAttribute("Id", _id);

            // Add the SignedInfo
            if (_signedInfo == null)
                throw new CryptographicException(SR.Cryptography_Xml_SignedInfoRequired);

            signatureElement.AppendChild(_signedInfo.GetXml(document));

            // Add the SignatureValue
            if (_signatureValue == null)
                throw new CryptographicException(SR.Cryptography_Xml_SignatureValueRequired);

            XmlElement signatureValueElement = document.CreateElement("SignatureValue", SignedXml.XmlDsigNamespaceUrl);
            signatureValueElement.AppendChild(document.CreateTextNode(Convert.ToBase64String(_signatureValue)));
            if (!string.IsNullOrEmpty(_signatureValueId))
                signatureValueElement.SetAttribute("Id", _signatureValueId);
            signatureElement.AppendChild(signatureValueElement);

            // Add the KeyInfo
            if (KeyInfo.Count > 0)
                signatureElement.AppendChild(KeyInfo.GetXml(document));

            // Add the Objects
            foreach (object obj in _embeddedObjects)
            {
                DataObject dataObj = obj as DataObject;
                if (dataObj != null)
                {
                    signatureElement.AppendChild(dataObj.GetXml(document));
                }
            }

            return signatureElement;
        }

        public void LoadXml(XmlElement value)
        {
            // Make sure we don't get passed null
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Signature
            XmlElement signatureElement = value;
            if (!signatureElement.LocalName.Equals("Signature"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "Signature");

            // Id attribute -- optional
            _id = Utils.GetAttribute(signatureElement, "Id", SignedXml.XmlDsigNamespaceUrl);

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            // SignedInfo
            XmlElement signedInfoElement = signatureElement.SelectSingleNode("ds:SignedInfo", nsm) as XmlElement;
            if (signedInfoElement == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo");

            SignedInfo = new SignedInfo();
            SignedInfo.LoadXml(signedInfoElement);

            // SignatureValue
            XmlElement signatureValueElement = signatureElement.SelectSingleNode("ds:SignatureValue", nsm) as XmlElement;
            if (signatureValueElement == null)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo/SignatureValue");
            _signatureValue = Convert.FromBase64String(Utils.DiscardWhiteSpaces(signatureValueElement.InnerText));
            _signatureValueId = Utils.GetAttribute(signatureValueElement, "Id", SignedXml.XmlDsigNamespaceUrl);

            XmlNodeList keyInfoNodes = signatureElement.SelectNodes("ds:KeyInfo", nsm);
            _keyInfo = new KeyInfo();
            if (keyInfoNodes != null)
            {
                foreach (XmlNode node in keyInfoNodes)
                {
                    XmlElement keyInfoElement = node as XmlElement;
                    if (keyInfoElement != null)
                        _keyInfo.LoadXml(keyInfoElement);
                }
            }

            XmlNodeList objectNodes = signatureElement.SelectNodes("ds:Object", nsm);
            _embeddedObjects.Clear();
            if (objectNodes != null)
            {
                foreach (XmlNode node in objectNodes)
                {
                    XmlElement objectElement = node as XmlElement;
                    if (objectElement != null)
                    {
                        DataObject dataObj = new DataObject();
                        dataObj.LoadXml(objectElement);
                        _embeddedObjects.Add(dataObj);
                    }
                }
            }

            // Select all elements that have Id attributes
            XmlNodeList nodeList = signatureElement.SelectNodes("//*[@Id]", nsm);
            if (nodeList != null)
            {
                foreach (XmlNode node in nodeList)
                {
                    _referencedItems.Add(node);
                }
            }
        }

        public void AddObject(DataObject dataObject)
        {
            _embeddedObjects.Add(dataObject);
        }
    }
}

