// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// This file contains the IDN functions and implementation.
//
// This allows encoding of non-ASCII domain names in a "punycode" form,
// for example:
//
//     \u5B89\u5BA4\u5948\u7F8E\u6075-with-SUPER-MONKEYS
//
// is encoded as:
//
//     xn---with-SUPER-MONKEYS-pc58ag80a8qai00g7n9n
//
// Additional options are provided to allow unassigned IDN characters and
// to validate according to the Std3ASCII Rules (like DNS names).
//
// There are also rules regarding bidirectionality of text and the length
// of segments.
//
// For additional rules see also:
//  RFC 3490 - Internationalizing Domain Names in Applications (IDNA)
//  RFC 3491 - Nameprep: A Stringprep Profile for Internationalized Domain Names (IDN)
//  RFC 3492 - Punycode: A Bootstring encoding of Unicode for Internationalized Domain Names in Applications (IDNA)
//

using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System.Globalization
{
    // IdnMapping class used to map names to Punycode
#if INTERNAL_GLOBALIZATION_EXTENSIONS
    internal 
#else
    public
#endif
    sealed class IdnMapping
    {
        bool m_bAllowUnassigned;
        bool m_bUseStd3AsciiRules;

        public IdnMapping()
        {
        }

        public bool AllowUnassigned
        {
            get
            {
                return this.m_bAllowUnassigned;
            }

            set
            {
                this.m_bAllowUnassigned = value;
            }
        }

        public bool UseStd3AsciiRules
        {
            get
            {
                return this.m_bUseStd3AsciiRules;
            }

            set
            {
                this.m_bUseStd3AsciiRules = value;
            }
        }

        // Gets ASCII (Punycode) version of the string
        public String GetAscii(String unicode)
        {
            return GetAscii(unicode, 0);
        }

        public String GetAscii(String unicode, int index)
        {
            if (unicode == null) throw new ArgumentNullException("unicode");
            Contract.EndContractBlock();
            return GetAscii(unicode, index, unicode.Length - index);
        }

        public String GetAscii(String unicode, int index, int count)
        {
            if (unicode == null) throw new ArgumentNullException("unicode");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > unicode.Length)
                throw new ArgumentOutOfRangeException("byteIndex", SR.ArgumentOutOfRange_Index);
            if (index > unicode.Length - count)
                throw new ArgumentOutOfRangeException("unicode", SR.ArgumentOutOfRange_IndexCountBuffer);
            Contract.EndContractBlock();

            // We're only using part of the string
            unicode = unicode.Substring(index, count);

            return GetAsciiUsingOS(unicode);
        }

        [System.Security.SecuritySafeCritical]
        private String GetAsciiUsingOS(String unicode)
        {
            if (unicode.Length == 0)
            {
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, "unicode");
            }

            if (unicode[unicode.Length - 1] == 0)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, unicode.Length - 1), "unicode");
            }

            uint flags = (uint)((AllowUnassigned ? IDN_ALLOW_UNASSIGNED : 0) | (UseStd3AsciiRules ? IDN_USE_STD3_ASCII_RULES : 0));
            int length = Interop.mincore.IdnToAscii(flags, unicode, unicode.Length, null, 0);

            int lastError;

            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, "unicode");
                }

                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "unicode");
            }

            char[] output = new char[length];

            length = Interop.mincore.IdnToAscii(flags, unicode, unicode.Length, output, length);
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, "unicode");
                }

                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "unicode");
            }

            return new String(output, 0, length);
        }

        // Gets Unicode version of the string.  Normalized and limited to IDNA characters.
        public String GetUnicode(String ascii)
        {
            return GetUnicode(ascii, 0);
        }

        public String GetUnicode(String ascii, int index)
        {
            if (ascii == null) throw new ArgumentNullException("ascii");
            Contract.EndContractBlock();
            return GetUnicode(ascii, index, ascii.Length - index);
        }

        public String GetUnicode(String ascii, int index, int count)
        {
            if (ascii == null) throw new ArgumentNullException("ascii");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > ascii.Length)
                throw new ArgumentOutOfRangeException("byteIndex", SR.ArgumentOutOfRange_Index);
            if (index > ascii.Length - count)
                throw new ArgumentOutOfRangeException("ascii", SR.ArgumentOutOfRange_IndexCountBuffer);

            // This is a case (i.e. explicitly null-terminated input) where behavior in .NET and Win32 intentionally differ.
            // The .NET APIs should (and did in v4.0 and earlier) throw an ArgumentException on input that includes a terminating null.
            // The Win32 APIs fail on an embedded null, but not on a terminating null.
            if (count > 0 && ascii[index + count - 1] == (char)0)
                throw new ArgumentException("ascii", SR.Argument_IdnBadPunycode);
            Contract.EndContractBlock();

            // We're only using part of the string
            ascii = ascii.Substring(index, count);

            return GetUnicodeUsingOS(ascii);
        }


        [System.Security.SecuritySafeCritical]
        private string GetUnicodeUsingOS(string ascii)
        {
            uint flags = (uint)((AllowUnassigned ? IDN_ALLOW_UNASSIGNED : 0) | (UseStd3AsciiRules ? IDN_USE_STD3_ASCII_RULES : 0));
            int length = Interop.mincore.IdnToUnicode(flags, ascii, ascii.Length, null, 0);
            int lastError;

            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, "ascii");
                }

                throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
            }

            char[] output = new char[length];

            length = Interop.mincore.IdnToUnicode(flags, ascii, ascii.Length, output, length);
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, "ascii");
                }

                throw new ArgumentException(SR.Argument_IdnBadPunycode, "ascii");
            }

            return new String(output, 0, length);
        }

        public override bool Equals(Object obj)
        {
            IdnMapping that = obj as IdnMapping;

            if (that != null)
            {
                return this.m_bAllowUnassigned == that.m_bAllowUnassigned &&
                        this.m_bUseStd3AsciiRules == that.m_bUseStd3AsciiRules;
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.m_bAllowUnassigned ? 100 : 200) + (this.m_bUseStd3AsciiRules ? 1000 : 2000);
        }

        private const int IDN_ALLOW_UNASSIGNED = 0x1;
        private const int IDN_USE_STD3_ASCII_RULES = 0x2;
        private const int ERROR_INVALID_NAME = 123;
    }
}

