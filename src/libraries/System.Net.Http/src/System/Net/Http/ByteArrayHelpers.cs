// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System
{
    internal static class ByteArrayHelpers
    {
        internal static bool EqualsOrdinalAsciiIgnoreCase(string left, ReadOnlySpan<byte> right)
        {
            Debug.Assert(left != null, "Expected non-null string");
            Debug.Assert(Ascii.IsValid(left) || Ascii.IsValid(right), "Expected at least one of the inputs to be valid ASCII");

            return Ascii.EqualsIgnoreCase(right, left);
        }

        internal static bool EqualsOrdinalAscii(string left, ReadOnlySpan<byte> right)
        {
            Debug.Assert(left != null, "Expected non-null string");
            Debug.Assert(Ascii.IsValid(left) || Ascii.IsValid(right), "Expected at least one of the inputs to be valid ASCII");

            return Ascii.Equals(right, left);
        }
    }
}
