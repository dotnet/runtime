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
    public class DSAKeyValue : KeyInfoClause
    {
        private DSA _key;

        //
        // public constructors
        //

        public DSAKeyValue()
        {
            _key = DSA.Create();
        }

        public DSAKeyValue(DSA key)
        {
            _key = key;
        }

        //
        // public properties
        //

        public DSA Key
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
            DSAParameters dsaParams = _key.ExportParameters(false);

            XmlElement keyValueElement = xmlDocument.CreateElement("KeyValue", SignedXml.XmlDsigNamespaceUrl);
            XmlElement dsaKeyValueElement = xmlDocument.CreateElement("DSAKeyValue", SignedXml.XmlDsigNamespaceUrl);

            XmlElement pElement = xmlDocument.CreateElement("P", SignedXml.XmlDsigNamespaceUrl);
            pElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.P)));
            dsaKeyValueElement.AppendChild(pElement);

            XmlElement qElement = xmlDocument.CreateElement("Q", SignedXml.XmlDsigNamespaceUrl);
            qElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.Q)));
            dsaKeyValueElement.AppendChild(qElement);

            XmlElement gElement = xmlDocument.CreateElement("G", SignedXml.XmlDsigNamespaceUrl);
            gElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.G)));
            dsaKeyValueElement.AppendChild(gElement);

            XmlElement yElement = xmlDocument.CreateElement("Y", SignedXml.XmlDsigNamespaceUrl);
            yElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.Y)));
            dsaKeyValueElement.AppendChild(yElement);

            // Add optional components if present
            if (dsaParams.J != null)
            {
                XmlElement jElement = xmlDocument.CreateElement("J", SignedXml.XmlDsigNamespaceUrl);
                jElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.J)));
                dsaKeyValueElement.AppendChild(jElement);
            }

            if (dsaParams.Seed != null)
            {  // note we assume counter is correct if Seed is present
                XmlElement seedElement = xmlDocument.CreateElement("Seed", SignedXml.XmlDsigNamespaceUrl);
                seedElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.Seed)));
                dsaKeyValueElement.AppendChild(seedElement);

                XmlElement counterElement = xmlDocument.CreateElement("PgenCounter", SignedXml.XmlDsigNamespaceUrl);
                counterElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(Utils.ConvertIntToByteArray(dsaParams.Counter))));
                dsaKeyValueElement.AppendChild(counterElement);
            }

            keyValueElement.AppendChild(dsaKeyValueElement);

            return keyValueElement;
        }

        public override void LoadXml(XmlElement value)
        {
            // Until DSA implements FromXmlString, throw here
            throw new PlatformNotSupportedException();
        }
    }
}
