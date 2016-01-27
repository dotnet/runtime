// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System.IO;
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

#if !FEATURE_CORECLR
        //
        // New RSA encrypt/decrypt/sign/verify RSA abstractions in .NET 4.6+ and .NET Core
        //
        // Methods that throw DerivedClassMustOverride are effectively abstract but we
        // cannot mark them as such as it would be a breaking change. We'll make them
        // abstract in .NET Core.

        public virtual byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) {
            throw DerivedClassMustOverride();
        }

        public virtual byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) {
            throw DerivedClassMustOverride();
        }

        public virtual byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            throw DerivedClassMustOverride();
        }

        public virtual bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            throw DerivedClassMustOverride();
        }

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) {
            throw DerivedClassMustOverride();
        }

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) {
            throw DerivedClassMustOverride();
        }

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            return SignData(data, 0, data.Length, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (offset < 0 || offset > data.Length) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > data.Length - offset) {
                throw new ArgumentOutOfRangeException("count");
            }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }
    
            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            return VerifyData(data, 0, data.Length, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (offset < 0 || offset > data.Length) {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0 || count > data.Length - offset) {
                throw new ArgumentOutOfRangeException("count"); 
            }
            if (signature == null) {
                throw new ArgumentNullException("signature");
            }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            if (signature == null) {
                throw new ArgumentNullException("signature");
            }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null) {
                throw new ArgumentNullException("padding");
            }
 
            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        private static Exception DerivedClassMustOverride() {
            return new NotImplementedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        internal static Exception HashAlgorithmNameNullOrEmpty() {
            return new ArgumentException(Environment.GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty"), "hashAlgorithm");
        }
#endif

        //
        // Legacy encrypt/decrypt RSA abstraction from .NET < 4.6
        //
        // These should be obsolete, but we can't mark them as such here due to rules around not introducing
        // source breaks to scenarios that compile against the GAC.
        //
        // They used to be abstract, but the only concrete implementation in RSACryptoServiceProvider threw
        // NotSupportedException! This has been moved up to the base so all subclasses can ignore them moving forward.
        // They will also be removed from .NET Core altogether.
        //
        // The original intent was for these to perform the RSA algorithm without padding/depadding. This can
        // be seen by how the RSAXxx(De)Formatter classes call them in the non-RSACryptoServiceProvider case -- 
        // they do the padding/depadding in managed code.
        //
        // Unfortunately, these formatter classes are still incompatible with RSACng or any derived class that does not
        // implement EncryptValue, DecryptValue as the formatters speculatively expected non-RSACryptoServiceProvider
        // to do. That needs to be fixed in a subsequent release. We can still do it as it would move an exception to a
        // correct result...
        //

        // [Obsolete]
        public virtual byte[] DecryptValue(byte[] rgb) {
           throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        // [Obsolete]
        public virtual byte[] EncryptValue(byte[] rgb) {
           throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
       }

        //
        // These should also be obsolete (on the base). They aren't well defined nor are they used
        // anywhere in the FX apart from checking that they're not null.
        //
        // For new derived RSA classes, we'll just return "RSA" which is analagous to what ECDsa
        // and ECDiffieHellman do.
        //
        // Note that for compat, RSACryptoServiceProvider still overrides and returns RSA-PKCS1-KEYEX 
        // and http://www.w3.org/2000/09/xmldsig#rsa-sha1
        //

        public override string KeyExchangeAlgorithm {
            get { return "RSA"; }
        }

        public override string SignatureAlgorithm {
            get { return "RSA"; }
        }


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
