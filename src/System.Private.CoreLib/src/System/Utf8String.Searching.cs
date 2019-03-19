// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace System
{
    public sealed partial class Utf8String
    {
        // Ordinal search
        public bool Contains(char value)
        {
            return Rune.TryCreate(value, out Rune result) && Contains(result);
        }

        // Ordinal search
        public bool Contains(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return SpanHelpers.IndexOf(
                ref DangerousGetMutableReference(), Length,
                ref MemoryMarshal.GetReference(runeBytes), runeBytesWritten) >= 0;
        }

        // Ordinal search
        public bool EndsWith(char value)
        {
            return Rune.TryCreate(value, out Rune result) && EndsWith(result);
        }

        // Ordinal search
        public bool EndsWith(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return this.AsBytes().EndsWith(runeBytes.Slice(0, runeBytesWritten));
        }

        // Ordinal search
        public int IndexOf(char value)
        {
            return Rune.TryCreate(value, out Rune result) ? IndexOf(result) : -1;
        }

        // Ordinal search
        public int IndexOf(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return SpanHelpers.IndexOf(
                ref DangerousGetMutableReference(), Length,
                ref MemoryMarshal.GetReference(runeBytes), runeBytesWritten);
        }

        // Ordinal search
        public bool StartsWith(char value)
        {
            return Rune.TryCreate(value, out Rune result) && StartsWith(result);
        }

        // Ordinal search
        public bool StartsWith(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return this.AsBytes().StartsWith(runeBytes.Slice(0, runeBytesWritten));
        }
    }
}
