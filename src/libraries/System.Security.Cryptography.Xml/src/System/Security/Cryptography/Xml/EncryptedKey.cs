// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public sealed class EncryptedKey : EncryptedType
    {
        private string _recipient;
        private string _carriedKeyName;
        private ReferenceList _referenceList;

        public EncryptedKey() { }

        public string Recipient
        {
            get => _recipient ??= string.Empty; // an unspecified value for an XmlAttribute is string.Empty
            set
            {
                _recipient = value;
                _cachedXml = null;
            }
        }

        public string CarriedKeyName
        {
            get { return _carriedKeyName; }
            set
            {
                _carriedKeyName = value;
                _cachedXml = null;
            }
        }

        public ReferenceList ReferenceList => _referenceList ??= new ReferenceList();

        public void AddReference(DataReference dataReference)
        {
            ReferenceList.Add(dataReference);
        }

        public void AddReference(KeyReference keyReference)
        {
            ReferenceList.Add(keyReference);
        }

        public override void LoadXml(XmlElement value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            XmlNamespaceManager nsm = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            nsm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            Id = Utils.GetAttribute(value, "Id", EncryptedXml.XmlEncNamespaceUrl);
            Type = Utils.GetAttribute(value, "Type", EncryptedXml.XmlEncNamespaceUrl);
            MimeType = Utils.GetAttribute(value, "MimeType", EncryptedXml.XmlEncNamespaceUrl);
            Encoding = Utils.GetAttribute(value, "Encoding", EncryptedXml.XmlEncNamespaceUrl);
            Recipient = Utils.GetAttribute(value, "Recipient", EncryptedXml.XmlEncNamespaceUrl);

            XmlNode encryptionMethodNode = value.SelectSingleNode("enc:EncryptionMethod", nsm);

            // EncryptionMethod
            EncryptionMethod = new EncryptionMethod();
            if (encryptionMethodNode != null)
                EncryptionMethod.LoadXml(encryptionMethodNode as XmlElement);

            // Key Info
            KeyInfo = new KeyInfo();
            XmlNode keyInfoNode = value.SelectSingleNode("ds:KeyInfo", nsm);
            if (keyInfoNode != null)
                KeyInfo.LoadXml(keyInfoNode as XmlElement);

            // CipherData
            XmlNode cipherDataNode = value.SelectSingleNode("enc:CipherData", nsm);
            if (cipherDataNode == null)
                throw new CryptographicException(SR.Cryptography_Xml_MissingCipherData);

            CipherData = new CipherData();
            CipherData.LoadXml(cipherDataNode as XmlElement);

            // EncryptionProperties
            XmlNode encryptionPropertiesNode = value.SelectSingleNode("enc:EncryptionProperties", nsm);
            if (encryptionPropertiesNode != null)
            {
                // Select the EncryptionProperty elements inside the EncryptionProperties element
                XmlNodeList encryptionPropertyNodes = encryptionPropertiesNode.SelectNodes("enc:EncryptionProperty", nsm);
                if (encryptionPropertyNodes != null)
                {
                    foreach (XmlNode node in encryptionPropertyNodes)
                    {
                        EncryptionProperty ep = new EncryptionProperty();
                        ep.LoadXml(node as XmlElement);
                        EncryptionProperties.Add(ep);
                    }
                }
            }

            // CarriedKeyName
            XmlNode carriedKeyNameNode = value.SelectSingleNode("enc:CarriedKeyName", nsm);
            if (carriedKeyNameNode != null)
            {
                CarriedKeyName = carriedKeyNameNode.InnerText;
            }

            // ReferenceList
            XmlNode referenceListNode = value.SelectSingleNode("enc:ReferenceList", nsm);
            if (referenceListNode != null)
            {
                // Select the DataReference elements inside the ReferenceList element
                XmlNodeList dataReferenceNodes = referenceListNode.SelectNodes("enc:DataReference", nsm);
                if (dataReferenceNodes != null)
                {
                    foreach (XmlNode node in dataReferenceNodes)
                    {
                        DataReference dr = new DataReference();
                        dr.LoadXml(node as XmlElement);
                        ReferenceList.Add(dr);
                    }
                }
                // Select the KeyReference elements inside the ReferenceList element
                XmlNodeList keyReferenceNodes = referenceListNode.SelectNodes("enc:KeyReference", nsm);
                if (keyReferenceNodes != null)
                {
                    foreach (XmlNode node in keyReferenceNodes)
                    {
                        KeyReference kr = new KeyReference();
                        kr.LoadXml(node as XmlElement);
                        ReferenceList.Add(kr);
                    }
                }
            }

            // Save away the cached value
            _cachedXml = value;
        }

        public override XmlElement GetXml()
        {
            if (CacheValid) return _cachedXml;

            XmlDocument document = new XmlDocument();
            document.PreserveWhitespace = true;
            return GetXml(document);
        }

        internal XmlElement GetXml(XmlDocument document)
        {
            // Create the EncryptedKey element
            XmlElement encryptedKeyElement = (XmlElement)document.CreateElement("EncryptedKey", EncryptedXml.XmlEncNamespaceUrl);

            // Deal with attributes
            if (!string.IsNullOrEmpty(Id))
                encryptedKeyElement.SetAttribute("Id", Id);
            if (!string.IsNullOrEmpty(Type))
                encryptedKeyElement.SetAttribute("Type", Type);
            if (!string.IsNullOrEmpty(MimeType))
                encryptedKeyElement.SetAttribute("MimeType", MimeType);
            if (!string.IsNullOrEmpty(Encoding))
                encryptedKeyElement.SetAttribute("Encoding", Encoding);
            if (!string.IsNullOrEmpty(Recipient))
                encryptedKeyElement.SetAttribute("Recipient", Recipient);

            // EncryptionMethod
            if (EncryptionMethod != null)
                encryptedKeyElement.AppendChild(EncryptionMethod.GetXml(document));

            // KeyInfo
            if (KeyInfo.Count > 0)
                encryptedKeyElement.AppendChild(KeyInfo.GetXml(document));

            // CipherData
            if (CipherData == null)
                throw new CryptographicException(SR.Cryptography_Xml_MissingCipherData);
            encryptedKeyElement.AppendChild(CipherData.GetXml(document));

            // EncryptionProperties
            if (EncryptionProperties.Count > 0)
            {
                XmlElement encryptionPropertiesElement = document.CreateElement("EncryptionProperties", EncryptedXml.XmlEncNamespaceUrl);
                for (int index = 0; index < EncryptionProperties.Count; index++)
                {
                    EncryptionProperty ep = EncryptionProperties.Item(index);
                    encryptionPropertiesElement.AppendChild(ep.GetXml(document));
                }
                encryptedKeyElement.AppendChild(encryptionPropertiesElement);
            }

            // ReferenceList
            if (ReferenceList.Count > 0)
            {
                XmlElement referenceListElement = document.CreateElement("ReferenceList", EncryptedXml.XmlEncNamespaceUrl);
                for (int index = 0; index < ReferenceList.Count; index++)
                {
                    referenceListElement.AppendChild(ReferenceList[index].GetXml(document));
                }
                encryptedKeyElement.AppendChild(referenceListElement);
            }

            // CarriedKeyName
            if (CarriedKeyName != null)
            {
                XmlElement carriedKeyNameElement = (XmlElement)document.CreateElement("CarriedKeyName", EncryptedXml.XmlEncNamespaceUrl);
                XmlText carriedKeyNameText = document.CreateTextNode(CarriedKeyName);
                carriedKeyNameElement.AppendChild(carriedKeyNameText);
                encryptedKeyElement.AppendChild(carriedKeyNameElement);
            }

            return encryptedKeyElement;
        }
    }
}
