// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System;
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class RIPEMD160 : HashAlgorithm
    {
        //
        // public constructors
        //

        protected RIPEMD160()
        {
            HashSizeValue = 160;
        }

        //
        // public methods
        //

        new static public RIPEMD160 Create() {
            return Create("System.Security.Cryptography.RIPEMD160");
        }

        new static public RIPEMD160 Create(String hashName) {
            return (RIPEMD160) CryptoConfig.CreateFromName(hashName);
        }
    }
}

