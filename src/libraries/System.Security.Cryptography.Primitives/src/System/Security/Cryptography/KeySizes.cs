// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    // This structure is used for returning the set of legal key sizes and
    // block sizes of the symmetric algorithms.
    public sealed class KeySizes
    {
        public KeySizes(int minSize, int maxSize, int skipSize)
        {
            MinSize = minSize;
            MaxSize = maxSize;
            SkipSize = skipSize;
        }

        public int MinSize { get; private set; }
        public int MaxSize { get; private set; }
        public int SkipSize { get; private set; }
    }
}
