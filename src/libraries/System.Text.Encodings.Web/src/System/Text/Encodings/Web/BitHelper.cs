// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Text.Encodings.Web
{
    internal static class BitHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndexOfFirstNeedToEscape(int index)
        {
            // Found at least one byte that needs to be escaped, figure out the index of
            // the first one found that needed to be escaped within the 16 bytes.
            Debug.Assert(index > 0 && index <= 65_535);
            int tzc = BitOperations.TrailingZeroCount(index);
            Debug.Assert(tzc >= 0 && tzc < 16);

            return tzc;
        }
    }
}
