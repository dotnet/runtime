// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System.Text;
    using System.Runtime.Serialization;
    using System.Security.Util;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    // DSAParameters is serializable so that one could pass the public parameters
    // across a remote call, but we explicitly make the private key X non-serializable
    // so you cannot accidently send it along with the public parameters.
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct DSAParameters {
        public byte[]      P;
        public byte[]      Q;
        public byte[]      G;
        public byte[]      Y;
        public byte[]      J;
        [NonSerialized] public byte[]      X;
        public byte[]      Seed;
        public int         Counter;
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class DSA : AsymmetricAlgorithm
    {
        //
        //  Extending this class allows us to know that you are really implementing
        //  an DSA key.  This is required for anybody providing a new DSA key value
        //  implemention.
        //
        //  The class provides no methods, fields or anything else.  Its only purpose is
        //  as a heirarchy member for identification of the algorithm.
        //

        protected DSA() { }

        //
        // public methods
        //

        new static public DSA Create() {
            return Create("System.Security.Cryptography.DSA");
        }

        new static public DSA Create(String algName) {
            return (DSA) CryptoConfig.CreateFromName(algName);
        }

        abstract public byte[] CreateSignature(byte[] rgbHash);

        abstract public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature); 

        // We can provide a default implementation of FromXmlString because we require 
        // every DSA implementation to implement ImportParameters
        // All we have to do here is parse the XML.

        public override void FromXmlString(String xmlString) {
            if (xmlString == null) throw new ArgumentNullException("xmlString");
            Contract.EndContractBlock();
            DSAParameters dsaParams = new DSAParameters();
            Parser p = new Parser(xmlString);
            SecurityElement topElement = p.GetTopElement();

            // P is always present
            String pString = topElement.SearchForTextOfLocalName("P");
            if (pString == null) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","DSA","P"));
            }
            dsaParams.P = Convert.FromBase64String(Utils.DiscardWhiteSpaces(pString));

            // Q is always present
            String qString = topElement.SearchForTextOfLocalName("Q");
            if (qString == null) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","DSA","Q"));
            }
            dsaParams.Q = Convert.FromBase64String(Utils.DiscardWhiteSpaces(qString));

            // G is always present
            String gString = topElement.SearchForTextOfLocalName("G");
            if (gString == null) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","DSA","G"));
            }
            dsaParams.G = Convert.FromBase64String(Utils.DiscardWhiteSpaces(gString));

            // Y is always present
            String yString = topElement.SearchForTextOfLocalName("Y");
            if (yString == null) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","DSA","Y"));
            }
            dsaParams.Y = Convert.FromBase64String(Utils.DiscardWhiteSpaces(yString));

            // J is optional
            String jString = topElement.SearchForTextOfLocalName("J");
            if (jString != null) dsaParams.J = Convert.FromBase64String(Utils.DiscardWhiteSpaces(jString));

            // X is optional -- private key if present
            String xString = topElement.SearchForTextOfLocalName("X");
            if (xString != null) dsaParams.X = Convert.FromBase64String(Utils.DiscardWhiteSpaces(xString));

            // Seed and PgenCounter are optional as a unit -- both present or both absent
            String seedString = topElement.SearchForTextOfLocalName("Seed");
            String pgenCounterString = topElement.SearchForTextOfLocalName("PgenCounter");
            if ((seedString != null) && (pgenCounterString != null)) {
                dsaParams.Seed = Convert.FromBase64String(Utils.DiscardWhiteSpaces(seedString));
                dsaParams.Counter = Utils.ConvertByteArrayToInt(Convert.FromBase64String(Utils.DiscardWhiteSpaces(pgenCounterString)));
            } else if ((seedString != null) || (pgenCounterString != null)) {
                if (seedString == null) {
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","DSA","Seed"));
                } else {
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","DSA","PgenCounter"));
                }
            }

            ImportParameters(dsaParams);
        }

        // We can provide a default implementation of ToXmlString because we require 
        // every DSA implementation to implement ImportParameters
        // If includePrivateParameters is false, this is just an XMLDSIG DSAKeyValue
        // clause.  If includePrivateParameters is true, then we extend DSAKeyValue with 
        // the other (private) elements.

        public override String ToXmlString(bool includePrivateParameters) {
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
            sb.Append("<DSAKeyValue>");
            // Add P, Q, G and Y
            sb.Append("<P>"+Convert.ToBase64String(dsaParams.P)+"</P>");
            sb.Append("<Q>"+Convert.ToBase64String(dsaParams.Q)+"</Q>");
            sb.Append("<G>"+Convert.ToBase64String(dsaParams.G)+"</G>");
            sb.Append("<Y>"+Convert.ToBase64String(dsaParams.Y)+"</Y>");
            // Add optional components if present
            if (dsaParams.J != null) {
                sb.Append("<J>"+Convert.ToBase64String(dsaParams.J)+"</J>");
            }
            if ((dsaParams.Seed != null)) {  // note we assume counter is correct if Seed is present
                sb.Append("<Seed>"+Convert.ToBase64String(dsaParams.Seed)+"</Seed>");
                sb.Append("<PgenCounter>"+Convert.ToBase64String(Utils.ConvertIntToByteArray(dsaParams.Counter))+"</PgenCounter>");
            }

            if (includePrivateParameters) {
                // Add the private component
                sb.Append("<X>"+Convert.ToBase64String(dsaParams.X)+"</X>");
            } 
            sb.Append("</DSAKeyValue>");
            return(sb.ToString());
        }

        abstract public DSAParameters ExportParameters(bool includePrivateParameters);

        abstract public void ImportParameters(DSAParameters parameters);
    }
}
