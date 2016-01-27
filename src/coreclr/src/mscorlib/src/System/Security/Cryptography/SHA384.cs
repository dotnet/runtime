// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This abstract class represents the SHA-384 hash algorithm.
//

namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class SHA384 : HashAlgorithm
    {
        //
        // protected constructors
        //

        protected SHA384() {
            HashSizeValue = 384;
        }

        //
        // public methods
        //

        new static public SHA384 Create() {
            return Create("System.Security.Cryptography.SHA384");
        }

        new static public SHA384 Create(String hashName) {
            return (SHA384) CryptoConfig.CreateFromName(hashName);
        }
    }
}

