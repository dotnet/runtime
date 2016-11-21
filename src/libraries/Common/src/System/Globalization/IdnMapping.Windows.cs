// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    sealed partial class IdnMapping
    {
        private unsafe string GetAsciiCore(char* unicode, int count)
        {
            uint flags = Flags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToAscii(flags, new IntPtr(unicode), count, IntPtr.Zero, 0);
            if (length == 0)
            {
                ThrowForZeroLength(nameof(unicode), SR.Argument_IdnIllegalName, SR.Argument_InvalidCharSequenceNoIndex);
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
                fixed (char* pOutput = output)
                {
                    return GetAsciiCore(unicode, count, flags, pOutput, length);
                }
            }
        }

        private unsafe string GetAsciiCore(char* unicode, int count, uint flags, char* output, int outputLength)
        {
            int length = Interop.Normaliz.IdnToAscii(flags, new IntPtr(unicode), count, new IntPtr(output), outputLength);
            if (length == 0)
            {
                ThrowForZeroLength(nameof(unicode), SR.Argument_IdnIllegalName, SR.Argument_InvalidCharSequenceNoIndex);
            }
            Debug.Assert(length == outputLength);
            return new string(output, 0, length);
        }

        private unsafe string GetUnicodeCore(char* ascii, int count)
        {
            uint flags = Flags;

            // Determine the required length
            int length = Interop.Normaliz.IdnToUnicode(flags, new IntPtr(ascii), count, IntPtr.Zero, 0);
            if (length == 0)
            {
                ThrowForZeroLength(nameof(ascii), SR.Argument_IdnIllegalName, SR.Argument_IdnBadPunycode);
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
                fixed (char* pOutput = output)
                {
                    return GetUnicodeCore(ascii, count, flags, pOutput, length);
                }
            }
        }

        private unsafe string GetUnicodeCore(char* ascii, int count, uint flags, char* output, int outputLength)
        {
            int length = Interop.Normaliz.IdnToUnicode(flags, new IntPtr(ascii), count, new IntPtr(output), outputLength);
            if (length == 0)
            {
                ThrowForZeroLength(nameof(ascii), SR.Argument_IdnIllegalName, SR.Argument_IdnBadPunycode);
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

        private static void ThrowForZeroLength(string paramName, string invalidNameString, string otherString)
        {
            throw new ArgumentException(
                Marshal.GetLastWin32Error() == Interop.ERROR_INVALID_NAME ? invalidNameString : otherString,
                paramName);
        }
    }
}

