// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    // 
    // Internal interface that allows CngSymmetricAlgorithmCore to communicate back with the SymmetricAlgorithm it's embedded in.
    // Any class that implements interface also derives from SymmetricAlgorithm so they'll implement most of these methods already.
    // In addition to exposing a way to call the Key property non-virtually, this interface limits access to the outer object's
    // methods to only what's needed, which is always handy in avoiding introducing infinite-recursion bugs.
    //
    internal interface ICngSymmetricAlgorithm
    {
        // SymmetricAlgorithm members used by the core.
        int BlockSize { get; }
        CipherMode Mode { get; }
        PaddingMode Padding { get; }
        byte[] IV { get; set; }
        KeySizes[] LegalKeySizes { get; }

        // SymmetricAlgorithm members that need to be called non-virtually to avoid infinite recursion.
        byte[] BaseKey { get; set; }
        int BaseKeySize { get; set; }

        // Other members.
        bool IsWeakKey(byte[] key);
    }
}

