// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
