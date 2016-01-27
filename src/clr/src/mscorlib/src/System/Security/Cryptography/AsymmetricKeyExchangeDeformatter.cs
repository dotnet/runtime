// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class AsymmetricKeyExchangeDeformatter {
        //
        // protected constructors
        //
    
        protected AsymmetricKeyExchangeDeformatter() {
        }
    
        //
        // public properties
        //

        public abstract String Parameters {
            get;
            set;
        }

        //
        // public methods
        //

        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public byte[] DecryptKeyExchange(byte[] rgb);
    }
}    
