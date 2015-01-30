// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class MaskGenerationMethod {
        [System.Runtime.InteropServices.ComVisible(true)]
        abstract public byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
    }
}
