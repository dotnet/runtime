// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    // This enum represents the padding method to use for filling out short blocks.
    // "None" means no padding (whole blocks required). 
    // "PKCS7" is the padding mode defined in RFC 2898, Section 6.1.1, Step 4, generalized
    // to whatever block size is required.
    // "Zeros" means pad with zero bytes to fill out the last block.
    [Serializable]
    public enum PaddingMode
    {
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
    }
}
