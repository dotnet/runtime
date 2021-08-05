// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Xml;

#pragma warning disable CA5384 // Do Not Use Digital Signature Algorithm (DSA)

namespace System.Security.Cryptography.Xml
{
    public class DSAKeyValue : KeyInfoClause
    {
        private DSA _key;

        //
        // public constructors
        //

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
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

        /// <summary>
        /// Create an XML representation.
        /// </summary>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/xmldsig-core/#sec-DSAKeyValue.
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
        private const string DSAKeyValueElementName = "DSAKeyValue";

        //Optional {P,Q}-Sequence
        private const string PElementName = "P";
        private const string QElementName = "Q";

        //Optional Members
        private const string GElementName = "G";
        private const string JElementName = "J";

        //Mandatory Members
        private const string YElementName = "Y";

        //Optional {Seed,PgenCounter}-Sequence
        private const string SeedElementName = "Seed";
        private const string PgenCounterElementName = "PgenCounter";

        internal override XmlElement GetXml(XmlDocument xmlDocument)
        {
            DSAParameters dsaParams = _key.ExportParameters(false);

            XmlElement keyValueElement = xmlDocument.CreateElement(KeyValueElementName, SignedXml.XmlDsigNamespaceUrl);
            XmlElement dsaKeyValueElement = xmlDocument.CreateElement(DSAKeyValueElementName, SignedXml.XmlDsigNamespaceUrl);

            XmlElement pElement = xmlDocument.CreateElement(PElementName, SignedXml.XmlDsigNamespaceUrl);
            pElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.P)));
            dsaKeyValueElement.AppendChild(pElement);

            XmlElement qElement = xmlDocument.CreateElement(QElementName, SignedXml.XmlDsigNamespaceUrl);
            qElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.Q)));
            dsaKeyValueElement.AppendChild(qElement);

            XmlElement gElement = xmlDocument.CreateElement(GElementName, SignedXml.XmlDsigNamespaceUrl);
            gElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.G)));
            dsaKeyValueElement.AppendChild(gElement);

            XmlElement yElement = xmlDocument.CreateElement(YElementName, SignedXml.XmlDsigNamespaceUrl);
            yElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.Y)));
            dsaKeyValueElement.AppendChild(yElement);

            // Add optional components if present
            if (dsaParams.J != null)
            {
                XmlElement jElement = xmlDocument.CreateElement(JElementName, SignedXml.XmlDsigNamespaceUrl);
                jElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.J)));
                dsaKeyValueElement.AppendChild(jElement);
            }

            if (dsaParams.Seed != null)
            {  // note we assume counter is correct if Seed is present
                XmlElement seedElement = xmlDocument.CreateElement(SeedElementName, SignedXml.XmlDsigNamespaceUrl);
                seedElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(dsaParams.Seed)));
                dsaKeyValueElement.AppendChild(seedElement);

                XmlElement counterElement = xmlDocument.CreateElement(PgenCounterElementName, SignedXml.XmlDsigNamespaceUrl);
                counterElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(Utils.ConvertIntToByteArray(dsaParams.Counter))));
                dsaKeyValueElement.AppendChild(counterElement);
            }

            keyValueElement.AppendChild(dsaKeyValueElement);

            return keyValueElement;
        }

        /// <summary>
        /// Deserialize from the XML representation.
        /// </summary>
        /// <remarks>
        /// Based upon https://www.w3.org/TR/xmldsig-core/#sec-DSAKeyValue.
        /// </remarks>
        /// <param name="value">
        /// An <see cref="XmlElement"/> containing the XML representation. This cannot be null.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> cannot be null.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The XML has the incorrect schema or the DSA parameters are invalid.
        /// </exception>
        public override void LoadXml(XmlElement value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value.Name != KeyValueElementName
                || value.NamespaceURI != SignedXml.XmlDsigNamespaceUrl)
            {
                throw new CryptographicException(SR.Format(SR.WrongRootElement, KeyValueElementName, SignedXml.XmlDsigNamespaceUrl));
            }

            const string xmlDsigNamespacePrefix = "dsig";
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(value.OwnerDocument.NameTable);
            xmlNamespaceManager.AddNamespace(xmlDsigNamespacePrefix, SignedXml.XmlDsigNamespaceUrl);

            XmlNode dsaKeyValueElement = value.SelectSingleNode($"{xmlDsigNamespacePrefix}:{DSAKeyValueElementName}", xmlNamespaceManager);
            if (dsaKeyValueElement == null)
            {
                throw new CryptographicException(SR.Format(SR.MustContainChildElement, KeyValueElementName, DSAKeyValueElementName));
            }

            XmlNode yNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{YElementName}", xmlNamespaceManager);
            if (yNode == null)
                throw new CryptographicException(SR.Format(SR.ElementMissing, YElementName));

            XmlNode pNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{PElementName}", xmlNamespaceManager);
            XmlNode qNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{QElementName}", xmlNamespaceManager);

            if ((pNode == null && qNode != null) || (pNode != null && qNode == null))
                throw new CryptographicException(SR.Format(SR.ElementCombinationMissing, PElementName, QElementName));


            XmlNode gNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{GElementName}", xmlNamespaceManager);
            XmlNode jNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{JElementName}", xmlNamespaceManager);

            XmlNode seedNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{SeedElementName}", xmlNamespaceManager);
            XmlNode pgenCounterNode = dsaKeyValueElement.SelectSingleNode($"{xmlDsigNamespacePrefix}:{PgenCounterElementName}", xmlNamespaceManager);
            if ((seedNode == null && pgenCounterNode != null) || (seedNode != null && pgenCounterNode == null))
                throw new CryptographicException(SR.Format(SR.ElementCombinationMissing, SeedElementName, PgenCounterElementName));

            try
            {
                Key.ImportParameters(new DSAParameters
                {
                    P = (pNode != null) ? Convert.FromBase64String(pNode.InnerText) : null,
                    Q = (qNode != null) ? Convert.FromBase64String(qNode.InnerText) : null,
                    G = (gNode != null) ? Convert.FromBase64String(gNode.InnerText) : null,
                    Y = Convert.FromBase64String(yNode.InnerText),
                    J = (jNode != null) ? Convert.FromBase64String(jNode.InnerText) : null,
                    Seed = (seedNode != null) ? Convert.FromBase64String(seedNode.InnerText) : null,
                    Counter = (pgenCounterNode != null) ? Utils.ConvertByteArrayToInt(Convert.FromBase64String(pgenCounterNode.InnerText)) : 0
                });
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"An error occurred parsing the key components", ex);
            }
        }
    }
}
