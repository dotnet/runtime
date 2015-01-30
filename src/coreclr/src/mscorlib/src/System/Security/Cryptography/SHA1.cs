// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

