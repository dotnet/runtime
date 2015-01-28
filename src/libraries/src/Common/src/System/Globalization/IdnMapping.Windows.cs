// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Globalization
{
    sealed partial class IdnMapping
    {
        private string GetAsciiCore(string unicode)
        {
            uint flags = Flags;
            
            // Determine the required length
            int length = Interop.mincore.IdnToAscii(flags, unicode, unicode.Length, null, 0);
            if (length == 0)
            {
                ThrowForZeroLength("unicode", SR.Argument_IdnIllegalName, SR.Argument_InvalidCharSequenceNoIndex);
            }

            // Do the conversion
            char[] output = new char[length];
            length = Interop.mincore.IdnToAscii(flags, unicode, unicode.Length, output, length);
            if (length == 0)
            {
                ThrowForZeroLength("unicode", SR.Argument_IdnIllegalName, SR.Argument_InvalidCharSequenceNoIndex);
            }

            return new string(output, 0, length);
        }

        private string GetUnicodeCore(string ascii)
        {
            uint flags = Flags;

            // Determine the required length
            int length = Interop.mincore.IdnToUnicode(flags, ascii, ascii.Length, null, 0);
            if (length == 0)
            {
                ThrowForZeroLength("ascii", SR.Argument_IdnIllegalName, SR.Argument_IdnBadPunycode);
            }

            char[] output = new char[length];

            // Do the conversion
            length = Interop.mincore.IdnToUnicode(flags, ascii, ascii.Length, output, length);
            if (length == 0)
            {
                ThrowForZeroLength("ascii", SR.Argument_IdnIllegalName, SR.Argument_IdnBadPunycode);
            }

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
                    (AllowUnassigned ? Interop.mincore.IDN_ALLOW_UNASSIGNED : 0) |
                    (UseStd3AsciiRules ? Interop.mincore.IDN_USE_STD3_ASCII_RULES : 0);
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

