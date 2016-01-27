// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
