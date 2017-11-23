// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public sealed partial class IdnMapping
    {
        private unsafe string GetAsciiCore(char* unicode, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            uint flags = Flags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToAscii(flags, unicode, count, null, 0);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: true);
            }

            // Do the conversion
            const int StackAllocThreshold = 512; // arbitrary limit to switch from stack to heap allocation
            if (length < StackAllocThreshold)
            {
                char* output = stackalloc char[length];
                return GetAsciiCore(unicode, count, flags, output, length);
            }
            else
            {
                char[] output = new char[length];
                fixed (char* pOutput = &output[0])
                {
                    return GetAsciiCore(unicode, count, flags, pOutput, length);
                }
            }
        }

        private unsafe string GetAsciiCore(char* unicode, int count, uint flags, char* output, int outputLength)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int length = Interop.Normaliz.IdnToAscii(flags, unicode, count, output, outputLength);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: true);
            }
            Debug.Assert(length == outputLength);
            return new string(output, 0, length);
        }

        private unsafe string GetUnicodeCore(char* ascii, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            uint flags = Flags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToUnicode(flags, ascii, count, null, 0);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: false);
            }

            // Do the conversion
            const int StackAllocThreshold = 512; // arbitrary limit to switch from stack to heap allocation
            if (length < StackAllocThreshold)
            {
                char* output = stackalloc char[length];
                return GetUnicodeCore(ascii, count, flags, output, length);
            }
            else
            {
                char[] output = new char[length];
                fixed (char* pOutput = &output[0])
                {
                    return GetUnicodeCore(ascii, count, flags, pOutput, length);
                }
            }
        }

        private unsafe string GetUnicodeCore(char* ascii, int count, uint flags, char* output, int outputLength)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int length = Interop.Normaliz.IdnToUnicode(flags, ascii, count, output, outputLength);
            if (length == 0)
            {
                ThrowForZeroLength(unicode: false);
            }
            Debug.Assert(length == outputLength);
            return new string(output, 0, length);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private uint Flags
        {
            get
            {
                int flags =
                    (AllowUnassigned ? Interop.Normaliz.IDN_ALLOW_UNASSIGNED : 0) |
                    (UseStd3AsciiRules ? Interop.Normaliz.IDN_USE_STD3_ASCII_RULES : 0);
                return (uint)flags;
            }
        }

        private static void ThrowForZeroLength(bool unicode)
        {
            int lastError = Marshal.GetLastWin32Error();

            throw new ArgumentException(
                lastError == Interop.Errors.ERROR_INVALID_NAME ? SR.Argument_IdnIllegalName : 
                    (unicode ? SR.Argument_InvalidCharSequenceNoIndex : SR.Argument_IdnBadPunycode),
                unicode ? "unicode" : "ascii");
        }
    }
}

