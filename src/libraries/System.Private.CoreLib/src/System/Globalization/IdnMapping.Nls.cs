// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public sealed partial class IdnMapping
    {
        private string NlsGetAsciiCore(string unicodeString, int index, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            ReadOnlySpan<char> unicode = unicodeString.AsSpan(index, count);
            uint flags = NlsFlags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToAscii(flags, unicode, count, Span<char>.Empty, 0);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: true);
            }

            // Do the conversion
            const int StackAllocThreshold = 512; // arbitrary limit to switch from stack to heap allocation
            if ((uint)length < StackAllocThreshold)
            {
                Span<char> output = stackalloc char[length];
                return NlsGetAsciiCore(unicodeString, unicode, flags, output);
            }
            else
            {
                char[] output = new char[length];
                return NlsGetAsciiCore(unicodeString, unicode, flags, output);
            }
        }

        private static string NlsGetAsciiCore(string unicodeString, ReadOnlySpan<char> unicode, uint flags, Span<char> output)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            int length = Interop.Normaliz.IdnToAscii(flags, unicode, unicode.Length, output, output.Length);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: true);
            }
            Debug.Assert(length == output.Length);
            return GetStringForOutput(unicodeString, unicode, output.Slice(0, length));
        }

        private bool NlsTryGetAsciiCore(ReadOnlySpan<char> unicode, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            uint flags = NlsFlags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToAscii(flags, unicode, unicode.Length, Span<char>.Empty, 0);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: true);
            }

            if (length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            // Do the conversion
            int actualLength = Interop.Normaliz.IdnToAscii(flags, unicode, unicode.Length, destination, destination.Length);
            if (actualLength == 0)
            {
                ThrowForZeroLength(unicode: true);
            }

            charsWritten = actualLength;
            return true;
        }

        private string NlsGetUnicodeCore(string asciiString, int index, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            ReadOnlySpan<char> ascii = asciiString.AsSpan(index, count);
            uint flags = NlsFlags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToUnicode(flags, ascii, count, Span<char>.Empty, 0);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: false);
            }

            // Do the conversion
            const int StackAllocThreshold = 512; // arbitrary limit to switch from stack to heap allocation
            if ((uint)length < StackAllocThreshold)
            {
                Span<char> output = stackalloc char[length];
                return NlsGetUnicodeCore(asciiString, ascii, flags, output);
            }
            else
            {
                char[] output = new char[length];
                return NlsGetUnicodeCore(asciiString, ascii, flags, output);
            }
        }

        private static string NlsGetUnicodeCore(string asciiString, ReadOnlySpan<char> ascii, uint flags, Span<char> output)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            int length = Interop.Normaliz.IdnToUnicode(flags, ascii, ascii.Length, output, output.Length);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: false);
            }
            Debug.Assert(length == output.Length);
            return GetStringForOutput(asciiString, ascii, output.Slice(0, length));
        }

        private bool NlsTryGetUnicodeCore(ReadOnlySpan<char> ascii, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

            uint flags = NlsFlags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToUnicode(flags, ascii, ascii.Length, Span<char>.Empty, 0);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: false);
            }

            if (length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            // Do the conversion
            int actualLength = Interop.Normaliz.IdnToUnicode(flags, ascii, ascii.Length, destination, destination.Length);
            if (actualLength == 0)
            {
                ThrowForZeroLength(unicode: false);
            }

            charsWritten = actualLength;
            return true;
        }

        private uint NlsFlags
        {
            get
            {
                int flags =
                    (AllowUnassigned ? Interop.Normaliz.IDN_ALLOW_UNASSIGNED : 0) |
                    (UseStd3AsciiRules ? Interop.Normaliz.IDN_USE_STD3_ASCII_RULES : 0);
                return (uint)flags;
            }
        }

        [DoesNotReturn]
        private static void ThrowForZeroLength(bool unicode)
        {
            int lastError = Marshal.GetLastPInvokeError();

            throw new ArgumentException(
                lastError == Interop.Errors.ERROR_INVALID_NAME ? SR.Argument_IdnIllegalName :
                    (unicode ? SR.Argument_InvalidCharSequenceNoIndex : SR.Argument_IdnBadPunycode),
                unicode ? "unicode" : "ascii");
        }
    }
}
