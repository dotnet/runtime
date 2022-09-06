// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly CanonicalXmlNodeList _referencedItems;
        private SignedXml _signedXml;

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
            get => _keyInfo ??= new KeyInfo();
            set => _keyInfo = value;
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
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Signature
            XmlElement signatureElement = value;
            if (!signatureElement.LocalName.Equals("Signature"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "Signature");

            // Id attribute -- optional
            _id = Utils.GetAttribute(signatureElement, "Id", SignedXml.XmlDsigNamespaceUrl);
            if (!Utils.VerifyAttributes(signatureElement, "Id"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "Signature");

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
            int expectedChildNodes = 0;

            // SignedInfo
            XmlNodeList signedInfoNodes = signatureElement.SelectNodes("ds:SignedInfo", nsm);
            if (signedInfoNodes == null || signedInfoNodes.Count == 0 || signedInfoNodes.Count > 1)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignedInfo");
            XmlElement signedInfoElement = signedInfoNodes[0] as XmlElement;
            expectedChildNodes += signedInfoNodes.Count;

            SignedInfo = new SignedInfo();
            SignedInfo.LoadXml(signedInfoElement);

            // SignatureValue
            XmlNodeList signatureValueNodes = signatureElement.SelectNodes("ds:SignatureValue", nsm);
            if (signatureValueNodes == null || signatureValueNodes.Count == 0 || signatureValueNodes.Count > 1)
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignatureValue");
            XmlElement signatureValueElement = signatureValueNodes[0] as XmlElement;
            expectedChildNodes += signatureValueNodes.Count;
            _signatureValue = Convert.FromBase64String(Utils.DiscardWhiteSpaces(signatureValueElement.InnerText));
            _signatureValueId = Utils.GetAttribute(signatureValueElement, "Id", SignedXml.XmlDsigNamespaceUrl);
            if (!Utils.VerifyAttributes(signatureValueElement, "Id"))
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "SignatureValue");

            // KeyInfo - optional single element
            XmlNodeList keyInfoNodes = signatureElement.SelectNodes("ds:KeyInfo", nsm);
            _keyInfo = new KeyInfo();
            if (keyInfoNodes != null)
            {
                if (keyInfoNodes.Count > 1)
                {
                    throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "KeyInfo");
                }
                foreach (XmlNode node in keyInfoNodes)
                {
                    XmlElement keyInfoElement = node as XmlElement;
                    if (keyInfoElement != null)
                        _keyInfo.LoadXml(keyInfoElement);
                }
                expectedChildNodes += keyInfoNodes.Count;
            }

            // Object - zero or more elements allowed
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
                expectedChildNodes += objectNodes.Count;
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
            // Verify that there aren't any extra nodes that aren't allowed
            if (signatureElement.SelectNodes("*").Count != expectedChildNodes)
            {
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "Signature");
            }
        }

        public void AddObject(DataObject dataObject)
        {
            _embeddedObjects.Add(dataObject);
        }
    }
}
