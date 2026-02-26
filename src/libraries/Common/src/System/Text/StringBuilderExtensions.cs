// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Text
{
    /// <summary>Polyfills for StringBuilder.</summary>
    internal static class StringBuilderExtensions
    {
        /// <summary>Polyfill for StringBuilder.Append(ReadOnlySpan&lt;char&gt;) which is not available on .NET Standard 2.0.</summary>
        public static unsafe StringBuilder Append(this StringBuilder stringBuilder, ReadOnlySpan<char> span)
        {
            fixed (char* ptr = &MemoryMarshal.GetReference(span))
            {
                return stringBuilder.Append(ptr, span.Length);
            }
        }
    }
}
