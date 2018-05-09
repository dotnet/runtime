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
    public class XmlLicenseTransform : Transform
    {
        private Type[] _inputTypes = { typeof(XmlDocument) };
        private Type[] _outputTypes = { typeof(XmlDocument) };
        private XmlNamespaceManager _namespaceManager = null;
        private XmlDocument _license = null;
        private IRelDecryptor _relDecryptor = null;
        private const string ElementIssuer = "issuer";
        private const string NamespaceUriCore = "urn:mpeg:mpeg21:2003:01-REL-R-NS";

        public XmlLicenseTransform()
        {
            Algorithm = SignedXml.XmlLicenseTransformUrl;
        }

        public override Type[] InputTypes
        {
            get { return _inputTypes; }
        }

        public override Type[] OutputTypes
        {
            get { return _outputTypes; }
        }

        public IRelDecryptor Decryptor
        {
            get { return _relDecryptor; }
            set { _relDecryptor = value; }
        }

        private void DecryptEncryptedGrants(XmlNodeList encryptedGrantList, IRelDecryptor decryptor)
        {
            XmlElement encryptionMethod = null;
            XmlElement keyInfo = null;
            XmlElement cipherData = null;
            EncryptionMethod encryptionMethodObj = null;
            KeyInfo keyInfoObj = null;
            CipherData cipherDataObj = null;

            for (int i = 0, count = encryptedGrantList.Count; i < count; i++)
            {
                encryptionMethod = encryptedGrantList[i].SelectSingleNode("//r:encryptedGrant/enc:EncryptionMethod", _namespaceManager) as XmlElement;
                keyInfo = encryptedGrantList[i].SelectSingleNode("//r:encryptedGrant/dsig:KeyInfo", _namespaceManager) as XmlElement;
                cipherData = encryptedGrantList[i].SelectSingleNode("//r:encryptedGrant/enc:CipherData", _namespaceManager) as XmlElement;
                if ((encryptionMethod != null) &&
                    (keyInfo != null) &&
                    (cipherData != null))
                {
                    encryptionMethodObj = new EncryptionMethod();
                    keyInfoObj = new KeyInfo();
                    cipherDataObj = new CipherData();

                    encryptionMethodObj.LoadXml(encryptionMethod);
                    keyInfoObj.LoadXml(keyInfo);
                    cipherDataObj.LoadXml(cipherData);

                    MemoryStream toDecrypt = null;
                    Stream decryptedContent = null;
                    StreamReader streamReader = null;

                    try
                    {
                        toDecrypt = new MemoryStream(cipherDataObj.CipherValue);
                        decryptedContent = _relDecryptor.Decrypt(encryptionMethodObj,
                                                                keyInfoObj, toDecrypt);

                        if ((decryptedContent == null) || (decryptedContent.Length == 0))
                            throw new CryptographicException(SR.Cryptography_Xml_XrmlUnableToDecryptGrant);

                        streamReader = new StreamReader(decryptedContent);
                        string clearContent = streamReader.ReadToEnd();

                        encryptedGrantList[i].ParentNode.InnerXml = clearContent;
                    }
                    finally
                    {
                        if (toDecrypt != null)
                            toDecrypt.Close();

                        if (decryptedContent != null)
                            decryptedContent.Close();

                        if (streamReader != null)
                            streamReader.Close();
                    }

                    encryptionMethodObj = null;
                    keyInfoObj = null;
                    cipherDataObj = null;
                }

                encryptionMethod = null;
                keyInfo = null;
                cipherData = null;
            }
        }

        // License transform has no inner XML elements
        protected override XmlNodeList GetInnerXml()
        {
            return null;
        }

        public override object GetOutput()
        {
            return _license;
        }

        public override object GetOutput(Type type)
        {
            if ((type != typeof(XmlDocument)) && (!type.IsSubclassOf(typeof(XmlDocument))))
                throw new ArgumentException(SR.Cryptography_Xml_TransformIncorrectInputType, nameof(type));

            return GetOutput();
        }

        // License transform has no inner XML elements
        public override void LoadInnerXml(XmlNodeList nodeList)
        {
            if (nodeList != null && nodeList.Count > 0)
                throw new CryptographicException(SR.Cryptography_Xml_UnknownTransform);
        }

        public override void LoadInput(object obj)
        {
            // Check if the Context property is set before this transform is invoked.
            if (Context == null)
                throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingContext);

            _license = new XmlDocument();
            _license.PreserveWhitespace = true;
            _namespaceManager = new XmlNamespaceManager(_license.NameTable);
            _namespaceManager.AddNamespace("dsig", SignedXml.XmlDsigNamespaceUrl);
            _namespaceManager.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            _namespaceManager.AddNamespace("r", NamespaceUriCore);

            XmlElement currentIssuerContext = null;
            XmlElement currentLicenseContext = null;
            XmlNode signatureNode = null;

            // Get the nearest issuer node
            currentIssuerContext = Context.SelectSingleNode("ancestor-or-self::r:issuer[1]", _namespaceManager) as XmlElement;
            if (currentIssuerContext == null)
                throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingIssuer);

            signatureNode = currentIssuerContext.SelectSingleNode("descendant-or-self::dsig:Signature[1]", _namespaceManager) as XmlElement;
            if (signatureNode != null)
                signatureNode.ParentNode.RemoveChild(signatureNode);

            // Get the nearest license node
            currentLicenseContext = currentIssuerContext.SelectSingleNode("ancestor-or-self::r:license[1]", _namespaceManager) as XmlElement;
            if (currentLicenseContext == null)
                throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingLicence);

            XmlNodeList issuerList = currentLicenseContext.SelectNodes("descendant-or-self::r:license[1]/r:issuer", _namespaceManager);

            // Remove all issuer nodes except current
            for (int i = 0, count = issuerList.Count; i < count; i++)
            {
                if (issuerList[i] == currentIssuerContext)
                    continue;

                if ((issuerList[i].LocalName == ElementIssuer) &&
                    (issuerList[i].NamespaceURI == NamespaceUriCore))
                    issuerList[i].ParentNode.RemoveChild(issuerList[i]);
            }

            XmlNodeList encryptedGrantList = currentLicenseContext.SelectNodes("/r:license/r:grant/r:encryptedGrant", _namespaceManager);

            if (encryptedGrantList.Count > 0)
            {
                if (_relDecryptor == null)
                    throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingIRelDecryptor);

                DecryptEncryptedGrants(encryptedGrantList, _relDecryptor);
            }

            _license.InnerXml = currentLicenseContext.OuterXml;
        }
    }
}
