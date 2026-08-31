// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Text;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="StringBuilder"/>.</summary>
internal static class StringBuilderPolyfills
{
    extension(StringBuilder stringBuilder)
    {
        /// <summary>Polyfill for StringBuilder.Append(ReadOnlySpan&lt;char&gt;) which is not available on .NET Standard 2.0.</summary>
        public unsafe StringBuilder Append(ReadOnlySpan<char> span)
        {
            fixed (char* ptr = &MemoryMarshal.GetReference(span))
            {
                return stringBuilder.Append(ptr, span.Length);
            }
        }
    }
}
