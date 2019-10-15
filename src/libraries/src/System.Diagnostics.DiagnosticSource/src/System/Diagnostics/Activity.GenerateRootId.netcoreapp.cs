// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace System.Diagnostics
{
    partial class Activity
    {
        private static string GenerateRootId()
        {
            // It is important that the part that changes frequently be first, because
            // some sampling functions don't sample from the high entropy part of their hash function.
            // This makes sampling based on this produce poor samples.
            Debug.Assert(s_uniqSuffix.Length < 50); // Ensure stackalloc not too large
            Span<char> result = stackalloc char[1 + 16 + s_uniqSuffix.Length]; // max length needed
            result[0] = '|';
            bool formatted = Interlocked.Increment(ref s_currentRootId).TryFormat(result.Slice(1), out int charsWritten, "x");
            Debug.Assert(formatted);
            s_uniqSuffix.AsSpan().CopyTo(result.Slice(1 + charsWritten));
            return new string(result.Slice(0, 1 + charsWritten + s_uniqSuffix.Length));
        }
    }
}
