// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Security.Cryptography
{
    public abstract partial class DSA : AsymmetricAlgorithm
    {
        // We can provide a default implementation of FromXmlString because we require
        // every DSA implementation to implement ImportParameters
        // All we have to do here is parse the XML.
        public override void FromXmlString(string xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException(nameof(xmlString));

            string seed = null;
            string pgenCounter = null;
            DSAParameters dsaParams = new DSAParameters();

            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "P":
                                // P is always present
                                reader.Read();
                                if (string.IsNullOrEmpty(reader.Value))
                                    ThrowInvalidXmlException("P");

                                dsaParams.P = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                break;

                            case "Q":
                                // Q is always present
                                reader.Read();
                                if (string.IsNullOrEmpty(reader.Value))
                                    ThrowInvalidXmlException("Q");

                                dsaParams.Q = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                break;

                            case "G":
                                // G is always present
                                reader.Read();
                                if (string.IsNullOrEmpty(reader.Value))
                                    ThrowInvalidXmlException("G");

                                dsaParams.G = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                break;

                            case "Y":
                                // Y is always present
                                reader.Read();
                                if (string.IsNullOrEmpty(reader.Value))
                                    ThrowInvalidXmlException("Y");

                                dsaParams.Y = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                break;

                            case "J":
                                // J is optional
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    dsaParams.J = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "X":
                                // X is optional -- private key
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    dsaParams.X = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(reader.Value));
                                }
                                break;

                            case "Seed":
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    seed = reader.Value;
                                }

                                break;

                            case "PgenCounter":
                                reader.Read();
                                if (!string.IsNullOrEmpty(reader.Value))
                                {
                                    pgenCounter = reader.Value;
                                }
                                break;
                        }
                    }
                }
            }

            // Seed and PgenCounter are optional as a unit -- both present or both absent
            if ((seed != null) && (pgenCounter != null))
            {
                dsaParams.Seed = Convert.FromBase64String(Helpers.DiscardWhiteSpaces(seed));
                dsaParams.Counter = Helpers.FromBigEndian(Convert.FromBase64String(Helpers.DiscardWhiteSpaces(pgenCounter)));
            }
            else if ((seed != null) || (pgenCounter != null))
            {
                if (seed == null)
                {
                    ThrowInvalidXmlException("Seed");
                }
                else
                {
                    ThrowInvalidXmlException("PgenCounter");
                }
            }

            ImportParameters(dsaParams);
        }

        // If includePrivateParameters is false, this is just an XMLDSIG DSAKeyValue
        // clause.  If includePrivateParameters is true, then we extend DSAKeyValue with 
        // the other (private) elements.
        public override String ToXmlString(bool includePrivateParameters)
        {
            // From the XMLDSIG spec, RFC 3075, Section 6.4.1, a DSAKeyValue looks like this:
            /* 
               <element name="DSAKeyValue"> 
                 <complexType> 
                   <sequence>
                     <sequence>
                       <element name="P" type="ds:CryptoBinary"/> 
                       <element name="Q" type="ds:CryptoBinary"/> 
                       <element name="G" type="ds:CryptoBinary"/> 
                       <element name="Y" type="ds:CryptoBinary"/> 
                       <element name="J" type="ds:CryptoBinary" minOccurs="0"/> 
                     </sequence>
                     <sequence minOccurs="0">
                       <element name="Seed" type="ds:CryptoBinary"/> 
                       <element name="PgenCounter" type="ds:CryptoBinary"/> 
                     </sequence>
                   </sequence>
                 </complexType>
               </element>
            */
            // we extend appropriately for private component X
            DSAParameters dsaParams = this.ExportParameters(includePrivateParameters);

            StringBuilder sb = new StringBuilder();

            var xmlSettings = new XmlWriterSettings()
            {
                ConformanceLevel = ConformanceLevel.Fragment,
            };

            using (XmlWriter writer = XmlWriter.Create(sb, xmlSettings))
            {
                writer.WriteStartElement("DSAKeyValue");

                // Add required parameters
                writer.WriteElementString("P", Convert.ToBase64String(dsaParams.P));
                writer.WriteElementString("Q", Convert.ToBase64String(dsaParams.Q));
                writer.WriteElementString("G", Convert.ToBase64String(dsaParams.G));
                writer.WriteElementString("Y", Convert.ToBase64String(dsaParams.Y));

                // Add optional parameters
                if (dsaParams.J != null)
                {
                    writer.WriteElementString("J", Convert.ToBase64String(dsaParams.J));
                }

                if ((dsaParams.Seed != null))
                {
                    // Note we assume counter is correct if Seed is present
                    writer.WriteElementString("Seed", Convert.ToBase64String(dsaParams.Seed));
                    writer.WriteElementString("PgenCounter", Convert.ToBase64String(Helpers.ToBigEndian(dsaParams.Counter)));
                }

                if (includePrivateParameters)
                {
                    writer.WriteElementString("X", Convert.ToBase64String(dsaParams.X));
                }

                writer.WriteEndElement();
            }

            return sb.ToString();
        }

        private static void ThrowInvalidXmlException(string propertyName)
        {
            throw new CryptographicException(string.Format(SR.Cryptography_InvalidFromXmlString, "DSA", propertyName));
        }
    }
}
