// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class RSAKeyValue : KeyInfoClause
    {
        private RSA _key;

        //
        // public constructors
        //

        public RSAKeyValue()
        {
            _key = RSA.Create();
        }

        public RSAKeyValue(RSA key)
        {
            _key = key;
        }

        //
        // public properties
        //

        public RSA Key
        {
            get { return _key; }
            set { _key = value; }
        }

        //
        // public methods
        //

        /// <summary>
        /// Create an XML representation.
        /// </summary>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/xmldsig-core/#sec-RSAKeyValue. 
        /// </remarks>
        /// <returns>
        /// An <see cref="XmlElement"/> containing the XML representation.
        /// </returns>
        public override XmlElement GetXml()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            return GetXml(xmlDocument);
        }

        private const string KeyValueElementName = "KeyValue";
        private const string RSAKeyValueElementName = "RSAKeyValue";
        private const string ModulusElementName = "Modulus";
        private const string ExponentElementName = "Exponent";

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            RSAParameters rsaParams = _key.ExportParameters(false);

            XmlElement keyValueElement = xmlDocument.CreateElement(KeyValueElementName, SignedXml.XmlDsigNamespaceUrl);
            XmlElement rsaKeyValueElement = xmlDocument.CreateElement(RSAKeyValueElementName, SignedXml.XmlDsigNamespaceUrl);

            XmlElement modulusElement = xmlDocument.CreateElement(ModulusElementName, SignedXml.XmlDsigNamespaceUrl);
            modulusElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(rsaParams.Modulus)));
            rsaKeyValueElement.AppendChild(modulusElement);

            XmlElement exponentElement = xmlDocument.CreateElement(ExponentElementName, SignedXml.XmlDsigNamespaceUrl);
            exponentElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(rsaParams.Exponent)));
            rsaKeyValueElement.AppendChild(exponentElement);

            keyValueElement.AppendChild(rsaKeyValueElement);

            return keyValueElement;
        }

        /// <summary>
        /// Deserialize from the XML representation.
        /// </summary>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/xmldsig-core/#sec-RSAKeyValue. 
        /// </remarks>
        /// <param name="value">
        /// An <see cref="XmlElement"/> containing the XML representation. This cannot be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> cannot be null.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The XML has the incorrect schema or the RSA parameters are invalid.
        /// </exception>
        public override void LoadXml(XmlElement value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value.LocalName != KeyValueElementName
                || value.NamespaceURI != SignedXml.XmlDsigNamespaceUrl)
            {
                throw new CryptographicException($"Root element must be {KeyValueElementName} element in namespace {SignedXml.XmlDsigNamespaceUrl}");
            }

            const string xmlDsigNamespacePrefix = "dsig";
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            xmlNamespaceManager.AddNamespace(xmlDsigNamespacePrefix, SignedXml.XmlDsigNamespaceUrl);

            XmlNode rsaKeyValueElement = value.SelectSingleNode($"{xmlDsigNamespacePrefix}:{RSAKeyValueElementName}", xmlNamespaceManager);
            if (rsaKeyValueElement == null)
            {
                throw new CryptographicException($"{KeyValueElementName} must contain child element {RSAKeyValueElementName}");
            }

            try
            {
                Key.ImportParameters(new RSAParameters
                {
                    Modulus = Convert.FromBase64String(rsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{ModulusElementName}", xmlNamespaceManager).InnerText),
                    Exponent = Convert.FromBase64String(rsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{ExponentElementName}", xmlNamespaceManager).InnerText)
                });
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"An error occurred parsing the {ModulusElementName} and {ExponentElementName} elements", ex);
            }
        }
    }
}
