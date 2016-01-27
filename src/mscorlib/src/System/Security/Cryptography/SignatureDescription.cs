// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal class RSAPKCS1SHA1SignatureDescription : SignatureDescription {
        public RSAPKCS1SHA1SignatureDescription() {
            KeyAlgorithm = "System.Security.Cryptography.RSACryptoServiceProvider";
            DigestAlgorithm = "System.Security.Cryptography.SHA1CryptoServiceProvider";
            FormatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureFormatter";
            DeformatterAlgorithm = "System.Security.Cryptography.RSAPKCS1SignatureDeformatter";
        }

        public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key) {
            AsymmetricSignatureDeformatter item = (AsymmetricSignatureDeformatter) CryptoConfig.CreateFromName(DeformatterAlgorithm);
            item.SetKey(key);
            item.SetHashAlgorithm("SHA1");
            return item;
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
