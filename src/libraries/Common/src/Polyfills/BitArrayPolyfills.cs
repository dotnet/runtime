// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Collections;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="BitArray"/>.</summary>
internal static class BitArrayPolyfills
{
    extension(BitArray bitArray)
    {
        public bool HasAllSet()
        {
            for (int i = 0; i < bitArray.Count; i++)
            {
                if (!bitArray[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
