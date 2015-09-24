// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization
{
    sealed partial class IdnMapping
    {
        private string GetAsciiCore(string unicode)
        {
            char[] buf = new char[unicode.Length];

            for (int attempts = 2; attempts > 0; attempts--)
            {
                int realLen = Interop.GlobalizationNative.ToAscii(Flags, unicode, unicode.Length, buf, buf.Length);

                if (realLen == 0)
                {
                    break;
                }

                if (realLen <= buf.Length)
                {
                    return new string(buf, 0, realLen);
                }

                buf = new char[realLen];
            }

            throw new ArgumentException(SR.Argument_IdnIllegalName, "unicode");
        }

        private string GetUnicodeCore(string ascii)
        {
            char[] buf = new char[ascii.Length];

            for (int attempts = 2; attempts > 0; attempts--)
            {
                int realLen = Interop.GlobalizationNative.ToUnicode(Flags, ascii, ascii.Length, buf, buf.Length);

                if (realLen == 0)
                {
                    break;
                }

                if (realLen <= buf.Length)
                {
                    return new string(buf, 0, realLen);
                }

                buf = new char[realLen];
            }

            throw new ArgumentException(SR.Argument_IdnIllegalName, "ascii");
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private uint Flags
        {
            get
            {
                int flags =
                    (AllowUnassigned ? Interop.GlobalizationNative.AllowUnassigned : 0) |
                    (UseStd3AsciiRules ? Interop.GlobalizationNative.UseStd3AsciiRules : 0);
                return (uint)flags;
            }
        }
    }
}
