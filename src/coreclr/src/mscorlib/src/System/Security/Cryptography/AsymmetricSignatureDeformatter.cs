// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography {
    using System.Security;
    using System;
    using System.Diagnostics.Contracts;

[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class AsymmetricSignatureDeformatter  {
        //
        // protected constructors
        //
    
        protected AsymmetricSignatureDeformatter() {
        }
    
        //
        // public methods
        //
    
        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public void SetHashAlgorithm(String strName);
    
        public virtual bool VerifySignature(HashAlgorithm hash, byte[] rgbSignature) {
            if (hash == null) throw new ArgumentNullException("hash");
            Contract.EndContractBlock();
            SetHashAlgorithm(hash.ToString());
            return VerifySignature(hash.Hash, rgbSignature);
        }
        
        abstract public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
    }    
}    
