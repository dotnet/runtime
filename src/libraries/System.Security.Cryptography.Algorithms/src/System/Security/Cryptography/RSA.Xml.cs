// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        // We can provide a default implementation of FromXmlString because we require
        // every RSA implementation to implement ImportParameters
        // All we have to do here is parse the XML.
        public override void FromXmlString(String xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException(nameof(xmlString));

            RSAParameters rsaParams = new RSAParameters();

            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "Modulus":
                                // Modulus is always present
                                reader.Read();
                                if (string.IsNullOrEmpty(reader.Value)) 
                                    ThrowInvalidXmlException("Modulus");

                                rsaParams.Modulus = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                break;

                            case "Exponent":
                                // Exponent is always present
                                reader.Read();
                                if (string.IsNullOrEmpty(reader.Value))
                                    ThrowInvalidXmlException("Exponent");

                                rsaParams.Exponent = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                break;

                            case "P":
                                // P is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    rsaParams.P = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "Q":
                                // Q is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    rsaParams.Q = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "DP":
                                // DP is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    rsaParams.DP = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "DQ":
                                // DQ is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    rsaParams.DQ = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "InverseQ":
                                // InverseQ is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    rsaParams.InverseQ = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "D":
                                // D is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    rsaParams.D = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;
                        }
                    }
                }
            }

            ImportParameters(rsaParams);
        }

        // If includePrivateParameters is false, this is just an XMLDSIG RSAKeyValue
        // clause.  If includePrivateParameters is true, then we extend RSAKeyValue with 
        // the other (private) elements.
        public override String ToXmlString(bool includePrivateParameters)
        {
            // From the XMLDSIG spec, RFC 3075, Section 6.4.2, an RSAKeyValue looks like this:
            /* 
               <element name="RSAKeyValue"> 
                 <complexType> 
                   <sequence>
                     <element name="Modulus" type="ds:CryptoBinary"/> 
                     <element name="Exponent" type="ds:CryptoBinary"/>
                   </sequence> 
                 </complexType> 
               </element>
            */
            RSAParameters rsaParams = this.ExportParameters(includePrivateParameters);
            StringBuilder sb = new StringBuilder();

            var xmlSettings = new XmlWriterSettings()
            {
                ConformanceLevel = ConformanceLevel.Fragment,
            };

            using (XmlWriter writer = XmlWriter.Create(sb, xmlSettings))
            {
                writer.WriteStartElement("RSAKeyValue");

                writer.WriteElementString("Modulus", Convert.ToBase64String(rsaParams.Modulus));
                writer.WriteElementString("Exponent", Convert.ToBase64String(rsaParams.Exponent));

                if (includePrivateParameters)
                {
                    writer.WriteElementString("P", Convert.ToBase64String(rsaParams.P));
                    writer.WriteElementString("Q", Convert.ToBase64String(rsaParams.Q));
                    writer.WriteElementString("DP", Convert.ToBase64String(rsaParams.DP));
                    writer.WriteElementString("DQ", Convert.ToBase64String(rsaParams.DQ));
                    writer.WriteElementString("InverseQ", Convert.ToBase64String(rsaParams.InverseQ));
                    writer.WriteElementString("D", Convert.ToBase64String(rsaParams.D));
                }

                writer.WriteEndElement();
            }

            return sb.ToString();
        }

        private static void ThrowInvalidXmlException(string propertyName)
        {
            throw new CryptographicException(string.Format(SR.Cryptography_InvalidFromXmlString, "RSA", propertyName));
        }
    }
}
