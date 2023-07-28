// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    [RequiresDynamicCode(CryptoHelpers.XsltRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(CryptoHelpers.CreateFromNameUnreferencedCodeMessage)]
    public class XmlLicenseTransform : Transform
    {
        private readonly Type[] _inputTypes = { typeof(XmlDocument) };
        private readonly Type[] _outputTypes = { typeof(XmlDocument) };
        private XmlNamespaceManager? _namespaceManager;
        private XmlDocument? _license;
        private IRelDecryptor? _relDecryptor;

        // work around https://github.com/dotnet/runtime/issues/81864 by splitting these into a separate class.
        internal static class Consts
        {
            internal const string ElementIssuer = "issuer";
            internal const string NamespaceUriCore = "urn:mpeg:mpeg21:2003:01-REL-R-NS";
        }

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

        public IRelDecryptor? Decryptor
        {
            get { return _relDecryptor; }
            set { _relDecryptor = value; }
        }

        private void DecryptEncryptedGrants(XmlNodeList encryptedGrantList)
        {
            XmlElement? encryptionMethod;
            XmlElement? keyInfo;
            XmlElement? cipherData;
            EncryptionMethod encryptionMethodObj;
            KeyInfo keyInfoObj;
            CipherData cipherDataObj;

            for (int i = 0, count = encryptedGrantList.Count; i < count; i++)
            {
                encryptionMethod = encryptedGrantList[i]!.SelectSingleNode("//r:encryptedGrant/enc:EncryptionMethod", _namespaceManager!) as XmlElement;
                keyInfo = encryptedGrantList[i]!.SelectSingleNode("//r:encryptedGrant/dsig:KeyInfo", _namespaceManager!) as XmlElement;
                cipherData = encryptedGrantList[i]!.SelectSingleNode("//r:encryptedGrant/enc:CipherData", _namespaceManager!) as XmlElement;
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

                    MemoryStream? toDecrypt = null;
                    Stream? decryptedContent = null;
                    StreamReader? streamReader = null;

                    try
                    {
                        toDecrypt = new MemoryStream(cipherDataObj.CipherValue!);
                        decryptedContent = _relDecryptor!.Decrypt(encryptionMethodObj,
                                                                keyInfoObj, toDecrypt);

                        if ((decryptedContent == null) || (decryptedContent.Length == 0))
                            throw new CryptographicException(SR.Cryptography_Xml_XrmlUnableToDecryptGrant);

                        streamReader = new StreamReader(decryptedContent);
                        string clearContent = streamReader.ReadToEnd();

                        // red flag
                        encryptedGrantList[i]!.ParentNode!.InnerXml = clearContent;
                    }
                    finally
                    {
                        toDecrypt?.Close();
                        decryptedContent?.Close();
                        streamReader?.Close();
                    }
                }
            }
        }

        // License transform has no inner XML elements
        protected override XmlNodeList? GetInnerXml()
        {
            return null;
        }

        public override object GetOutput()
        {
            return _license!;
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
            _namespaceManager.AddNamespace("r", Consts.NamespaceUriCore);

            XmlElement? currentIssuerContext;
            XmlElement? currentLicenseContext;
            XmlNode? signatureNode;

            // Get the nearest issuer node
            currentIssuerContext = Context.SelectSingleNode("ancestor-or-self::r:issuer[1]", _namespaceManager) as XmlElement;
            if (currentIssuerContext == null)
                throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingIssuer);

            signatureNode = currentIssuerContext.SelectSingleNode("descendant-or-self::dsig:Signature[1]", _namespaceManager) as XmlElement;
            signatureNode?.ParentNode!.RemoveChild(signatureNode);

            // Get the nearest license node
            currentLicenseContext = currentIssuerContext.SelectSingleNode("ancestor-or-self::r:license[1]", _namespaceManager) as XmlElement;
            if (currentLicenseContext == null)
                throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingLicence);

            XmlNodeList issuerList = currentLicenseContext.SelectNodes("descendant-or-self::r:license[1]/r:issuer", _namespaceManager)!;

            // Remove all issuer nodes except current
            for (int i = 0, count = issuerList.Count; i < count; i++)
            {
                if (issuerList[i]! == currentIssuerContext)
                    continue;

                if ((issuerList[i]!.LocalName == Consts.ElementIssuer) &&
                    (issuerList[i]!.NamespaceURI == Consts.NamespaceUriCore))
                    issuerList[i]!.ParentNode!.RemoveChild(issuerList[i]!);
            }

            XmlNodeList encryptedGrantList = currentLicenseContext.SelectNodes("/r:license/r:grant/r:encryptedGrant", _namespaceManager)!;

            if (encryptedGrantList.Count > 0)
            {
                if (_relDecryptor == null)
                    throw new CryptographicException(SR.Cryptography_Xml_XrmlMissingIRelDecryptor);

                DecryptEncryptedGrants(encryptedGrantList);
            }

            _license.InnerXml = currentLicenseContext.OuterXml;
        }
    }
}
