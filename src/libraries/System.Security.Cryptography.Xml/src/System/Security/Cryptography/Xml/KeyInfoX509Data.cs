// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public class KeyInfoX509Data : KeyInfoClause
    {
        // An array of certificates representing the certificate chain
        private ArrayList _certificates;
        // An array of issuer serial structs
        private ArrayList _issuerSerials;
        // An array of SKIs
        private ArrayList _subjectKeyIds;
        // An array of subject names
        private ArrayList _subjectNames;
        // A raw byte data representing a certificate revocation list
        private byte[] _CRL;

        //
        // public constructors
        //

        public KeyInfoX509Data() { }

        public KeyInfoX509Data(byte[] rgbCert)
        {
            X509Certificate2 certificate = new X509Certificate2(rgbCert);
            AddCertificate(certificate);
        }

        public KeyInfoX509Data(X509Certificate cert)
        {
            AddCertificate(cert);
        }

        public KeyInfoX509Data(X509Certificate cert, X509IncludeOption includeOption)
        {
            if (cert is null)
            {
                throw new ArgumentNullException(nameof(cert));
            }

            X509Certificate2 certificate = new X509Certificate2(cert);
            X509ChainElementCollection elements;
            X509Chain chain;
            switch (includeOption)
            {
                case X509IncludeOption.ExcludeRoot:
                    // Build the certificate chain
                    chain = new X509Chain();
                    chain.Build(certificate);

                    // Can't honor the option if we only have a partial chain.
                    if ((chain.ChainStatus.Length > 0) &&
                        ((chain.ChainStatus[0].Status & X509ChainStatusFlags.PartialChain) == X509ChainStatusFlags.PartialChain))
                    {
                        throw new CryptographicException(SR.Cryptography_Partial_Chain);
                    }

                    elements = (X509ChainElementCollection)chain.ChainElements;
                    for (int index = 0; index < (Utils.IsSelfSigned(chain) ? 1 : elements.Count - 1); index++)
                    {
                        AddCertificate(elements[index].Certificate);
                    }
                    break;
                case X509IncludeOption.EndCertOnly:
                    AddCertificate(certificate);
                    break;
                case X509IncludeOption.WholeChain:
                    // Build the certificate chain
                    chain = new X509Chain();
                    chain.Build(certificate);

                    // Can't honor the option if we only have a partial chain.
                    if ((chain.ChainStatus.Length > 0) &&
                        ((chain.ChainStatus[0].Status & X509ChainStatusFlags.PartialChain) == X509ChainStatusFlags.PartialChain))
                    {
                        throw new CryptographicException(SR.Cryptography_Partial_Chain);
                    }

                    elements = (X509ChainElementCollection)chain.ChainElements;
                    foreach (X509ChainElement element in elements)
                    {
                        AddCertificate(element.Certificate);
                    }
                    break;
            }
        }

        //
        // public properties
        //

        public ArrayList Certificates
        {
            get { return _certificates; }
        }

        public void AddCertificate(X509Certificate certificate)
        {
            if (certificate is null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (_certificates == null)
                _certificates = new ArrayList();

            X509Certificate2 x509 = new X509Certificate2(certificate);
            _certificates.Add(x509);
        }

        public ArrayList SubjectKeyIds
        {
            get { return _subjectKeyIds; }
        }

        public void AddSubjectKeyId(byte[] subjectKeyId)
        {
            if (_subjectKeyIds == null)
                _subjectKeyIds = new ArrayList();
            _subjectKeyIds.Add(subjectKeyId);
        }

        public void AddSubjectKeyId(string subjectKeyId)
        {
            if (_subjectKeyIds == null)
                _subjectKeyIds = new ArrayList();
            _subjectKeyIds.Add(Utils.DecodeHexString(subjectKeyId));
        }

        public ArrayList SubjectNames
        {
            get { return _subjectNames; }
        }

        public void AddSubjectName(string subjectName)
        {
            if (_subjectNames == null)
                _subjectNames = new ArrayList();
            _subjectNames.Add(subjectName);
        }

        public ArrayList IssuerSerials
        {
            get { return _issuerSerials; }
        }

        public void AddIssuerSerial(string issuerName, string serialNumber)
        {
            if (string.IsNullOrEmpty(issuerName))
                throw new ArgumentException(SR.Arg_EmptyOrNullString, nameof(issuerName));

            if (string.IsNullOrEmpty(serialNumber))
                throw new ArgumentException(SR.Arg_EmptyOrNullString, nameof(serialNumber));

            BigInteger h;
            if (!BigInteger.TryParse(serialNumber, NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out h))
                throw new ArgumentException(SR.Cryptography_Xml_InvalidX509IssuerSerialNumber, nameof(serialNumber));

            if (_issuerSerials == null)
                _issuerSerials = new ArrayList();
            _issuerSerials.Add(Utils.CreateX509IssuerSerial(issuerName, h.ToString()));
        }

        // When we load an X509Data from Xml, we know the serial number is in decimal representation.
        internal void InternalAddIssuerSerial(string issuerName, string serialNumber)
        {
            if (_issuerSerials == null)
                _issuerSerials = new ArrayList();
            _issuerSerials.Add(Utils.CreateX509IssuerSerial(issuerName, serialNumber));
        }

        public byte[] CRL
        {
            get { return _CRL; }
            set { _CRL = value; }
        }

        //
        // private methods
        //

        private void Clear()
        {
            _CRL = null;
            if (_subjectKeyIds != null) _subjectKeyIds.Clear();
            if (_subjectNames != null) _subjectNames.Clear();
            if (_issuerSerials != null) _issuerSerials.Clear();
            if (_certificates != null) _certificates.Clear();
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
            XmlElement x509DataElement = xmlDocument.CreateElement("X509Data", SignedXml.XmlDsigNamespaceUrl);

            if (_issuerSerials != null)
            {
                foreach (X509IssuerSerial issuerSerial in _issuerSerials)
                {
                    XmlElement issuerSerialElement = xmlDocument.CreateElement("X509IssuerSerial", SignedXml.XmlDsigNamespaceUrl);
                    XmlElement issuerNameElement = xmlDocument.CreateElement("X509IssuerName", SignedXml.XmlDsigNamespaceUrl);
                    issuerNameElement.AppendChild(xmlDocument.CreateTextNode(issuerSerial.IssuerName));
                    issuerSerialElement.AppendChild(issuerNameElement);
                    XmlElement serialNumberElement = xmlDocument.CreateElement("X509SerialNumber", SignedXml.XmlDsigNamespaceUrl);
                    serialNumberElement.AppendChild(xmlDocument.CreateTextNode(issuerSerial.SerialNumber));
                    issuerSerialElement.AppendChild(serialNumberElement);
                    x509DataElement.AppendChild(issuerSerialElement);
                }
            }

            if (_subjectKeyIds != null)
            {
                foreach (byte[] subjectKeyId in _subjectKeyIds)
                {
                    XmlElement subjectKeyIdElement = xmlDocument.CreateElement("X509SKI", SignedXml.XmlDsigNamespaceUrl);
                    subjectKeyIdElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(subjectKeyId)));
                    x509DataElement.AppendChild(subjectKeyIdElement);
                }
            }

            if (_subjectNames != null)
            {
                foreach (string subjectName in _subjectNames)
                {
                    XmlElement subjectNameElement = xmlDocument.CreateElement("X509SubjectName", SignedXml.XmlDsigNamespaceUrl);
                    subjectNameElement.AppendChild(xmlDocument.CreateTextNode(subjectName));
                    x509DataElement.AppendChild(subjectNameElement);
                }
            }

            if (_certificates != null)
            {
                foreach (X509Certificate certificate in _certificates)
                {
                    XmlElement x509Element = xmlDocument.CreateElement("X509Certificate", SignedXml.XmlDsigNamespaceUrl);
                    x509Element.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(certificate.GetRawCertData())));
                    x509DataElement.AppendChild(x509Element);
                }
            }

            if (_CRL != null)
            {
                XmlElement crlElement = xmlDocument.CreateElement("X509CRL", SignedXml.XmlDsigNamespaceUrl);
                crlElement.AppendChild(xmlDocument.CreateTextNode(Convert.ToBase64String(_CRL)));
                x509DataElement.AppendChild(crlElement);
            }

            return x509DataElement;
        }

        public override void LoadXml(XmlElement element)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            XmlNamespaceManager nsm = new XmlNamespaceManager(element.OwnerDocument.NameTable);
            nsm.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            XmlNodeList x509IssuerSerialNodes = element.SelectNodes("ds:X509IssuerSerial", nsm);
            XmlNodeList x509SKINodes = element.SelectNodes("ds:X509SKI", nsm);
            XmlNodeList x509SubjectNameNodes = element.SelectNodes("ds:X509SubjectName", nsm);
            XmlNodeList x509CertificateNodes = element.SelectNodes("ds:X509Certificate", nsm);
            XmlNodeList x509CRLNodes = element.SelectNodes("ds:X509CRL", nsm);

            if ((x509CRLNodes.Count == 0 && x509IssuerSerialNodes.Count == 0 && x509SKINodes.Count == 0
                    && x509SubjectNameNodes.Count == 0 && x509CertificateNodes.Count == 0)) // Bad X509Data tag, or Empty tag
                throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "X509Data");

            // Flush anything in the lists
            Clear();

            if (x509CRLNodes.Count != 0)
                _CRL = Convert.FromBase64String(Utils.DiscardWhiteSpaces(x509CRLNodes.Item(0).InnerText));

            foreach (XmlNode issuerSerialNode in x509IssuerSerialNodes)
            {
                XmlNode x509IssuerNameNode = issuerSerialNode.SelectSingleNode("ds:X509IssuerName", nsm);
                XmlNode x509SerialNumberNode = issuerSerialNode.SelectSingleNode("ds:X509SerialNumber", nsm);
                if (x509IssuerNameNode == null || x509SerialNumberNode == null)
                    throw new CryptographicException(SR.Cryptography_Xml_InvalidElement, "IssuerSerial");
                InternalAddIssuerSerial(x509IssuerNameNode.InnerText.Trim(), x509SerialNumberNode.InnerText.Trim());
            }

            foreach (XmlNode node in x509SKINodes)
            {
                AddSubjectKeyId(Convert.FromBase64String(Utils.DiscardWhiteSpaces(node.InnerText)));
            }

            foreach (XmlNode node in x509SubjectNameNodes)
            {
                AddSubjectName(node.InnerText.Trim());
            }

            foreach (XmlNode node in x509CertificateNodes)
            {
                AddCertificate(new X509Certificate2(Convert.FromBase64String(Utils.DiscardWhiteSpaces(node.InnerText))));
            }
        }
    }
}
