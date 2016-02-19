// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System.Diagnostics.Contracts;

namespace System.Globalization
{
    // IdnMapping class used to map names to Punycode
#if INTERNAL_GLOBALIZATION_EXTENSIONS
    internal 
#else
    public
#endif
    sealed partial class IdnMapping
    {
        private bool _allowUnassigned;
        private bool _useStd3AsciiRules;

        public IdnMapping()
        {
        }

        public bool AllowUnassigned
        {
            get { return _allowUnassigned; }
            set { _allowUnassigned = value; }
        }

        public bool UseStd3AsciiRules
        {
            get { return _useStd3AsciiRules; }
            set { _useStd3AsciiRules = value; }
        }

        // Gets ASCII (Punycode) version of the string
        public string GetAscii(string unicode)
        {
            return GetAscii(unicode, 0);
        }

        public string GetAscii(string unicode, int index)
        {
            if (unicode == null) 
                throw new ArgumentNullException(nameof(unicode));
            Contract.EndContractBlock();
            return GetAscii(unicode, index, unicode.Length - index);
        }

        public string GetAscii(string unicode, int index, int count)
        {
            if (unicode == null) 
                throw new ArgumentNullException(nameof(unicode));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > unicode.Length)
                throw new ArgumentOutOfRangeException("byteIndex", SR.ArgumentOutOfRange_Index);
            if (index > unicode.Length - count)
                throw new ArgumentOutOfRangeException(nameof(unicode), SR.ArgumentOutOfRange_IndexCountBuffer);
            Contract.EndContractBlock();

            // We're only using part of the string
            unicode = unicode.Substring(index, count);

            if (unicode.Length == 0)
            {
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));
            }
            if (unicode[unicode.Length - 1] == 0)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, unicode.Length - 1), nameof(unicode));
            }

            return GetAsciiCore(unicode);
        }

        // Gets Unicode version of the string.  Normalized and limited to IDNA characters.
        public string GetUnicode(string ascii)
        {
            return GetUnicode(ascii, 0);
        }

        public string GetUnicode(string ascii, int index)
        {
            if (ascii == null) 
                throw new ArgumentNullException(nameof(ascii));
            Contract.EndContractBlock();
            return GetUnicode(ascii, index, ascii.Length - index);
        }

        public string GetUnicode(string ascii, int index, int count)
        {
            if (ascii == null) 
                throw new ArgumentNullException(nameof(ascii));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > ascii.Length)
                throw new ArgumentOutOfRangeException("byteIndex", SR.ArgumentOutOfRange_Index);
            if (index > ascii.Length - count)
                throw new ArgumentOutOfRangeException(nameof(ascii), SR.ArgumentOutOfRange_IndexCountBuffer);

            // This is a case (i.e. explicitly null-terminated input) where behavior in .NET and Win32 intentionally differ.
            // The .NET APIs should (and did in v4.0 and earlier) throw an ArgumentException on input that includes a terminating null.
            // The Win32 APIs fail on an embedded null, but not on a terminating null.
            if (count > 0 && ascii[index + count - 1] == (char)0)
                throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));
            Contract.EndContractBlock();

            // We're only using part of the string
            ascii = ascii.Substring(index, count);

            return GetUnicodeCore(ascii);
        }

        public override bool Equals(object obj)
        {
            IdnMapping that = obj as IdnMapping;
            return 
                that != null &&
                _allowUnassigned == that._allowUnassigned &&
                _useStd3AsciiRules == that._useStd3AsciiRules;
        }

        public override int GetHashCode()
        {
            return (_allowUnassigned ? 100 : 200) + (_useStd3AsciiRules ? 1000 : 2000);
        }
    }
}
