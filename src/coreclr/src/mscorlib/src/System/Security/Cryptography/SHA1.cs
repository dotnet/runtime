// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class SHA1 : HashAlgorithm
    {
        protected SHA1() {
            HashSizeValue = 160;
        }

        //
        // public methods
        //

        new static public SHA1 Create() {
            return Create("System.Security.Cryptography.SHA1");
        }

        new static public SHA1 Create(String hashName) {
            return (SHA1) CryptoConfig.CreateFromName(hashName);
        }
    }
}

