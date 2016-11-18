// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>Microsoft</OWNER>
// 
 
//
// SignatureDescription.cs
//
 
namespace System.Security.Cryptography {
    using System.Security.Util;
    using System.Diagnostics.Contracts;
 
[System.Runtime.InteropServices.ComVisible(true)]
    public class SignatureDescription {
        private String _strKey;
        private String _strDigest;
        private String _strFormatter;
        private String _strDeformatter;
    
        //
        // public constructors
        //
 
        public SignatureDescription() {
        }
 
        public SignatureDescription(SecurityElement el) {
            if (el == null) throw new ArgumentNullException("el");
            Contract.EndContractBlock();
            _strKey = el.SearchForTextOfTag("Key");
            _strDigest = el.SearchForTextOfTag("Digest");
            _strFormatter = el.SearchForTextOfTag("Formatter");
            _strDeformatter = el.SearchForTextOfTag("Deformatter");
        }
 
        //
        // property methods
        //
 
        public String KeyAlgorithm { 
            get { return _strKey; }
            set { _strKey = value; }
        }
        public String DigestAlgorithm { 
            get { return _strDigest; }
            set { _strDigest = value; }
        }
        public String FormatterAlgorithm { 
            get { return _strFormatter; }
            set { _strFormatter = value; }
        }
        public String DeformatterAlgorithm { 
            get {return _strDeformatter; }
            set {_strDeformatter = value; }
        }
 
        //
        // public methods
        //
 
        public virtual AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key) {
            AsymmetricSignatureDeformatter     item;
 
            item = (AsymmetricSignatureDeformatter) CryptoConfig.CreateFromName(_strDeformatter);
            item.SetKey(key);
            return item;
        }
 
        public virtual AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key) {
            AsymmetricSignatureFormatter     item;
 
            item = (AsymmetricSignatureFormatter) CryptoConfig.CreateFromName(_strFormatter);
            item.SetKey(key);
            return item;
        }
 
        public virtual HashAlgorithm CreateDigest() {
            return (HashAlgorithm) CryptoConfig.CreateFromName(_strDigest);
        }
    }
 
    internal abstract class RSAPKCS1SignatureDescription : SignatureDescription {
        protected RSAPKCS1SignatureDescription(string hashAlgorithm, string digestAlgorithm) {
            KeyAlgorithm = "System.Security.Cryptography.RSA";
            DigestAlgorithm = digestAlgorithm;
            FormatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureFormatter";
            DeformatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureDeformatter";
            _hashAlgorithm = hashAlgorithm;
        }
 
        public sealed override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key) {
            AsymmetricSignatureDeformatter item = base.CreateDeformatter(key);
            item.SetHashAlgorithm(_hashAlgorithm);
            return item;
        }
 
        public sealed override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key) {
            AsymmetricSignatureFormatter item = base.CreateFormatter(key);
            item.SetHashAlgorithm(_hashAlgorithm);
            return item;
        }
 
        private string _hashAlgorithm;
    }
 
    internal class RSAPKCS1SHA1SignatureDescription : RSAPKCS1SignatureDescription {
        public RSAPKCS1SHA1SignatureDescription()
            : base("SHA1", "System.Security.Cryptography.SHA1Cng") {
        }
    }
 
    internal class RSAPKCS1SHA256SignatureDescription : RSAPKCS1SignatureDescription {
        public RSAPKCS1SHA256SignatureDescription()
            : base("SHA256", "System.Security.Cryptography.SHA256Cng") {
        }
    }
 
    internal class RSAPKCS1SHA384SignatureDescription : RSAPKCS1SignatureDescription {
        public RSAPKCS1SHA384SignatureDescription()
            : base("SHA384", "System.Security.Cryptography.SHA384Cng") {
        }
    }
 
    internal class RSAPKCS1SHA512SignatureDescription : RSAPKCS1SignatureDescription {
        public RSAPKCS1SHA512SignatureDescription()
            : base("SHA512", "System.Security.Cryptography.SHA512Cng") {
        }
    }
 
    internal class DSASignatureDescription : SignatureDescription {
        public DSASignatureDescription() {
            KeyAlgorithm = "System.Security.Cryptography.DSACryptoServiceProvider";
            DigestAlgorithm = "System.Security.Cryptography.SHA1CryptoServiceProvider";
            FormatterAlgorithm = "System.Security.Cryptography.DSASignatureFormatter";
            DeformatterAlgorithm = "System.Security.Cryptography.DSASignatureDeformatter";
        }
    }
}