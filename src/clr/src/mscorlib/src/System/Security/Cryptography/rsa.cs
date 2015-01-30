// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography {
    using System.Text;
    using System.Runtime.Serialization;
    using System.Security.Util;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    // We allow only the public components of an RSAParameters object, the Modulus and Exponent
    // to be serializable.
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct RSAParameters {
        public byte[]      Exponent;
        public byte[]      Modulus;

        [NonSerialized] public byte[]      P;
        [NonSerialized] public byte[]      Q;
        [NonSerialized] public byte[]      DP;
        [NonSerialized] public byte[]      DQ;
        [NonSerialized] public byte[]      InverseQ;
        [NonSerialized] public byte[]      D;
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RSA : AsymmetricAlgorithm
    {
        //
        //  Extending this class allows us to know that you are really implementing
        //  an RSA key.  This is required for anybody providing a new RSA key value
        //  implemention.
        //
        //  The class provides no methods, fields or anything else.  Its only purpose is
        //  as a heirarchy member for identification of algorithm.
        //
    
        protected RSA() { }

        //
        // public methods
        //

        new static public RSA Create() {
            return Create("System.Security.Cryptography.RSA");
        }

        new static public RSA Create(String algName) {
            return (RSA) CryptoConfig.CreateFromName(algName);
        }

        // Apply the private key to the data.  This function represents a
        // raw RSA operation -- no implicit depadding of the imput value
        abstract public byte[] DecryptValue(byte[] rgb);

        // Apply the public key to the data.  Again, this is a raw operation, no
        // automatic padding.
        abstract public byte[] EncryptValue(byte[] rgb);

        // Import/export functions

        // We can provide a default implementation of FromXmlString because we require 
        // every RSA implementation to implement ImportParameters
        // All we have to do here is parse the XML.

        public override void FromXmlString(String xmlString) {
            if (xmlString == null) throw new ArgumentNullException("xmlString");
            Contract.EndContractBlock();
            RSAParameters rsaParams = new RSAParameters();
            Parser p = new Parser(xmlString);
            SecurityElement topElement = p.GetTopElement();

            // Modulus is always present
            String modulusString = topElement.SearchForTextOfLocalName("Modulus");
            if (modulusString == null) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","RSA","Modulus"));
            }
            rsaParams.Modulus = Convert.FromBase64String(Utils.DiscardWhiteSpaces(modulusString));

            // Exponent is always present
            String exponentString = topElement.SearchForTextOfLocalName("Exponent");
            if (exponentString == null) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFromXmlString","RSA","Exponent"));
            }
            rsaParams.Exponent = Convert.FromBase64String(Utils.DiscardWhiteSpaces(exponentString));

            // P is optional
            String pString = topElement.SearchForTextOfLocalName("P");
            if (pString != null) rsaParams.P = Convert.FromBase64String(Utils.DiscardWhiteSpaces(pString));

            // Q is optional
            String qString = topElement.SearchForTextOfLocalName("Q");
            if (qString != null) rsaParams.Q = Convert.FromBase64String(Utils.DiscardWhiteSpaces(qString));

            // DP is optional
            String dpString = topElement.SearchForTextOfLocalName("DP");
            if (dpString != null) rsaParams.DP = Convert.FromBase64String(Utils.DiscardWhiteSpaces(dpString));

            // DQ is optional
            String dqString = topElement.SearchForTextOfLocalName("DQ");
            if (dqString != null) rsaParams.DQ = Convert.FromBase64String(Utils.DiscardWhiteSpaces(dqString));

            // InverseQ is optional
            String inverseQString = topElement.SearchForTextOfLocalName("InverseQ");
            if (inverseQString != null) rsaParams.InverseQ = Convert.FromBase64String(Utils.DiscardWhiteSpaces(inverseQString));

            // D is optional
            String dString = topElement.SearchForTextOfLocalName("D");
            if (dString != null) rsaParams.D = Convert.FromBase64String(Utils.DiscardWhiteSpaces(dString));

            ImportParameters(rsaParams);
        }

        // We can provide a default implementation of ToXmlString because we require 
        // every RSA implementation to implement ImportParameters
        // If includePrivateParameters is false, this is just an XMLDSIG RSAKeyValue
        // clause.  If includePrivateParameters is true, then we extend RSAKeyValue with 
        // the other (private) elements.
        public override String ToXmlString(bool includePrivateParameters) {
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
            // we extend appropriately for private components
            RSAParameters rsaParams = this.ExportParameters(includePrivateParameters);
            StringBuilder sb = new StringBuilder();
            sb.Append("<RSAKeyValue>");
            // Add the modulus
            sb.Append("<Modulus>"+Convert.ToBase64String(rsaParams.Modulus)+"</Modulus>");
            // Add the exponent
            sb.Append("<Exponent>"+Convert.ToBase64String(rsaParams.Exponent)+"</Exponent>");
            if (includePrivateParameters) {
                // Add the private components
                sb.Append("<P>"+Convert.ToBase64String(rsaParams.P)+"</P>");
                sb.Append("<Q>"+Convert.ToBase64String(rsaParams.Q)+"</Q>");
                sb.Append("<DP>"+Convert.ToBase64String(rsaParams.DP)+"</DP>");
                sb.Append("<DQ>"+Convert.ToBase64String(rsaParams.DQ)+"</DQ>");
                sb.Append("<InverseQ>"+Convert.ToBase64String(rsaParams.InverseQ)+"</InverseQ>");
                sb.Append("<D>"+Convert.ToBase64String(rsaParams.D)+"</D>");
            } 
            sb.Append("</RSAKeyValue>");
            return(sb.ToString());
        }

        abstract public RSAParameters ExportParameters(bool includePrivateParameters);

        abstract public void ImportParameters(RSAParameters parameters);
    }
}
