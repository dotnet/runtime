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

        public override XmlElement GetXml()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            return GetXml(xmlDocument);
        }

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            RSAParameters rsaParams = _key.ExportParameters(false);

            XmlElement keyValueElement = xmlDocument.CreateElement("KeyValue", SignedXml.XmlDsigNamespaceUrl);
            XmlElement rsaKeyValueElement = xmlDocument.CreateElement("RSAKeyValue", SignedXml.XmlDsigNamespaceUrl);

            XmlElement modulusElement = xmlDocument.CreateElement("Modulus", SignedXml.XmlDsigNamespaceUrl);
            modulusElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(rsaParams.Modulus)));
            rsaKeyValueElement.AppendChild(modulusElement);

            XmlElement exponentElement = xmlDocument.CreateElement("Exponent", SignedXml.XmlDsigNamespaceUrl);
            exponentElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(rsaParams.Exponent)));
            rsaKeyValueElement.AppendChild(exponentElement);

            keyValueElement.AppendChild(rsaKeyValueElement);

            return keyValueElement;
        }

        public override void LoadXml(XmlElement value)
        {
            // Until RSA implements FromXmlString, throw here
            throw new PlatformNotSupportedException();
        }
    }
}
