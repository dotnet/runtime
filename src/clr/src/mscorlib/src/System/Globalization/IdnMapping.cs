// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

/*

The punycode implementation is based on the sample code in RFC 3492
        
Copyright (C) The Internet Society (2003).  All Rights Reserved.

This document and translations of it may be copied and furnished to
others, and derivative works that comment on or otherwise explain it
or assist in its implementation may be prepared, copied, published
and distributed, in whole or in part, without restriction of any
kind, provided that the above copyright notice and this paragraph are
included on all such copies and derivative works.  However, this
document itself may not be modified in any way, such as by removing
the copyright notice or references to the Internet Society or other
Internet organizations, except as needed for the purpose of
developing Internet standards in which case the procedures for
copyrights defined in the Internet Standards process must be
followed, or as required to translate it into languages other than
English.

The limited permissions granted above are perpetual and will not be
revoked by the Internet Society or its successors or assigns.

This document and the information contained herein is provided on an
"AS IS" basis and THE INTERNET SOCIETY AND THE INTERNET ENGINEERING
TASK FORCE DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, INCLUDING
BUT NOT LIMITED TO ANY WARRANTY THAT THE USE OF THE INFORMATION
HEREIN WILL NOT INFRINGE ANY RIGHTS OR ANY IMPLIED WARRANTIES OF
MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
*/

namespace System.Globalization
{
    using System;
    using System.Security;
    using System.Globalization;
    using System.Text;
    using System.Runtime.Versioning;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    // IdnMapping class used to map names to Punycode

    public sealed class IdnMapping
    {
        // Legal name lengths for domain names
        const int    M_labelLimit = 63;          // Not including dots
        const int    M_defaultNameLimit = 255;     // Including dots

        // IDNA prefix
        const String M_strAcePrefix = "xn--";

        // Legal "dot" seperators (i.e: . in www.microsoft.com)
        static char[] M_Dots =
        {
            '.', '\u3002', '\uFF0E', '\uFF61'
        };

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
            if (unicode==null) throw new ArgumentNullException("unicode");
            Contract.EndContractBlock();
            return GetAscii(unicode, index, unicode.Length - index);
        }

        public String GetAscii(String unicode, int index, int count)
        {
            throw null;
            /*if (unicode==null) throw new ArgumentNullException("unicode");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count",
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (index > unicode.Length)
                throw new ArgumentOutOfRangeException("byteIndex",
                    Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (index > unicode.Length - count)
                throw new ArgumentOutOfRangeException("unicode",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            // We're only using part of the string
            unicode = unicode.Substring(index, count);

            if (Environment.IsWindows8OrAbove)
            {
                return GetAsciiUsingOS(unicode);
            }

            // Check for ASCII only string, which will be unchanged
            if (ValidateStd3AndAscii(unicode, UseStd3AsciiRules, true))
            {
                return unicode;
            }

            // Cannot be null terminated (normalization won't help us with this one, and
            // may have returned false before checking the whole string above)
            Contract.Assert(unicode.Length >= 1, "[IdnMapping.GetAscii]Expected 0 length strings to fail before now.");
            if (unicode[unicode.Length - 1] <= 0x1f)
            {
                throw new ArgumentException(
                    Environment.GetResourceString("Argument_InvalidCharSequence", unicode.Length-1 ),
                    "unicode");
            }

            // Have to correctly IDNA normalize the string and Unassigned flags
            bool bHasLastDot = (unicode.Length > 0) && IsDot(unicode[unicode.Length - 1]);
            unicode = unicode.Normalize((NormalizationForm)(m_bAllowUnassigned ?
                ExtendedNormalizationForms.FormIdna : ExtendedNormalizationForms.FormIdnaDisallowUnassigned));

            // Make sure we didn't normalize away something after a last dot
            if ((!bHasLastDot) && unicode.Length > 0 && IsDot(unicode[unicode.Length - 1]))
            {
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadLabelSize"), "unicode");
            }

            // May need to check Std3 rules again for non-ascii
            if (UseStd3AsciiRules)
            {
                ValidateStd3AndAscii(unicode, true, false);
            }

            // Go ahead and encode it
            return punycode_encode(unicode);*/
        }


        [System.Security.SecuritySafeCritical]
        private String GetAsciiUsingOS(String unicode)
        {
            if (unicode.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString(
                        "Argument_IdnBadLabelSize"), "unicode");
            }

            if (unicode[unicode.Length - 1] == 0)
            {
                throw new ArgumentException(
                    Environment.GetResourceString("Argument_InvalidCharSequence", unicode.Length - 1),
                    "unicode");
            }
            
            uint flags =   (uint) ((AllowUnassigned ? IDN_ALLOW_UNASSIGNED : 0) | (UseStd3AsciiRules ? IDN_USE_STD3_ASCII_RULES : 0));
            int  length = IdnToAscii(flags, unicode, unicode.Length, null, 0);

            int lastError; 
            
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "unicode");
                }
                
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "unicode");
            }

            char [] output = new char[length];
            
            length = IdnToAscii(flags, unicode, unicode.Length, output, length);
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "unicode");
                }
                
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "unicode");
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
            if (ascii==null) throw new ArgumentNullException("ascii");
            Contract.EndContractBlock();
            return GetUnicode(ascii, index, ascii.Length - index);
        }

        public String GetUnicode(String ascii, int index, int count)
        {
            if (ascii==null) throw new ArgumentNullException("ascii");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count",
                      Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (index > ascii.Length)
                throw new ArgumentOutOfRangeException("byteIndex",
                    Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (index > ascii.Length - count)
                throw new ArgumentOutOfRangeException("ascii",
                      Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));

            // This is a case (i.e. explicitly null-terminated input) where behavior in .NET and Win32 intentionally differ.
            // The .NET APIs should (and did in v4.0 and earlier) throw an ArgumentException on input that includes a terminating null.
            // The Win32 APIs fail on an embedded null, but not on a terminating null.
            if (count > 0 && ascii[index + count - 1] == (char)0)
                throw new ArgumentException("ascii",
                    Environment.GetResourceString("Argument_IdnBadPunycode"));
            Contract.EndContractBlock();

            // We're only using part of the string
            ascii = ascii.Substring(index, count);

            if (Environment.IsWindows8OrAbove)
            {
                return GetUnicodeUsingOS(ascii);
            }

            // Convert Punycode to Unicode
            String strUnicode = punycode_decode(ascii);

            // Output name MUST obey IDNA rules & round trip (casing differences are allowed)
            if (!ascii.Equals(GetAscii(strUnicode), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnIllegalName"), "ascii");

            return strUnicode;
        }

        
        [System.Security.SecuritySafeCritical]
        private string GetUnicodeUsingOS(string ascii)
        {
            uint flags =  (uint)((AllowUnassigned ? IDN_ALLOW_UNASSIGNED : 0) | (UseStd3AsciiRules ? IDN_USE_STD3_ASCII_RULES : 0));
            int  length = IdnToUnicode(flags, ascii, ascii.Length, null, 0);
            int lastError; 
            
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "ascii");
                }
                
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
            }

            char [] output = new char[length];
            
            length = IdnToUnicode(flags, ascii, ascii.Length, output, length);
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "ascii");
                }
                
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
            }
            
            return new String(output, 0, length);
        }

        public override bool Equals(Object obj)
        {
            IdnMapping that = obj as IdnMapping;

            if (that != null)
            {
                return  this.m_bAllowUnassigned   == that.m_bAllowUnassigned &&
                        this.m_bUseStd3AsciiRules == that.m_bUseStd3AsciiRules;
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.m_bAllowUnassigned ? 100 : 200) + (this.m_bUseStd3AsciiRules ? 1000 : 2000);
        }

        // Helpers
        static bool IsSupplementary(int cTest)
        {
            return cTest >= 0x10000;
        }

        // Is it a dot?
        // are we U+002E (., full stop), U+3002 (ideographic full stop), U+FF0E (fullwidth full stop), or
        // U+FF61 (halfwidth ideographic full stop).
        // Note: IDNA Normalization gets rid of dots now, but testing for last dot is before normalization
        static bool IsDot(char c)
        {
            return c == '.' || c == '\u3002' || c == '\uFF0E' || c == '\uFF61';
        }


        // See if we're only ASCII
        static bool ValidateStd3AndAscii(string unicode, bool bUseStd3, bool bCheckAscii)
        {
            // If its empty, then its too small
            if (unicode.Length == 0)
                throw new ArgumentException(Environment.GetResourceString(
                        "Argument_IdnBadLabelSize"), "unicode");
            Contract.EndContractBlock();

            int iLastDot = -1;

            // Loop the whole string
            for (int i = 0; i < unicode.Length; i++)
            {
                // Aren't allowing control chars (or 7f, but idn tables catch that, they don't catch \0 at end though)
                if (unicode[i] <= 0x1f)
                {
                    throw new ArgumentException(
                        Environment.GetResourceString("Argument_InvalidCharSequence", i ),
                        "unicode");
                }

                // If its Unicode or a control character, return false (non-ascii)
                if (bCheckAscii && unicode[i] >= 0x7f)
                    return false;

                // Check for dots
                if (IsDot(unicode[i]))
                {
                    // Can't have 2 dots in a row
                    if (i == iLastDot + 1)
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadLabelSize"), "unicode");

                    // If its too far between dots then fail
                    if (i - iLastDot > M_labelLimit + 1)
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadLabelSize"), "Unicode");

                    // If validating Std3, then char before dot can't be - char
                    if (bUseStd3 && i > 0)
                        ValidateStd3(unicode[i-1], true);

                    // Remember where the last dot is
                    iLastDot = i;
                    continue;
                }

                // If necessary, make sure its a valid std3 character
                if (bUseStd3)
                {
                    ValidateStd3(unicode[i], (i == iLastDot + 1));
                }
            }

            // If we never had a dot, then we need to be shorter than the label limit
            if (iLastDot == -1 && unicode.Length > M_labelLimit)
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadLabelSize"), "unicode");

            // Need to validate entire string length, 1 shorter if last char wasn't a dot
            if (unicode.Length > M_defaultNameLimit - (IsDot(unicode[unicode.Length-1])? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadNameSize",
                    M_defaultNameLimit - (IsDot(unicode[unicode.Length-1]) ? 0 : 1)),
                    "unicode");

            // If last char wasn't a dot we need to check for trailing -
            if (bUseStd3 && !IsDot(unicode[unicode.Length-1]))
                ValidateStd3(unicode[unicode.Length-1], true);

            return true;
        }

        // Validate Std3 rules for a character
        static void ValidateStd3(char c, bool bNextToDot)
        {
            // Check for illegal characters
            if ((c <= ',' || c == '/' || (c >= ':' && c <= '@') ||      // Lots of characters not allowed
                (c >= '[' && c <= '`') || (c >= '{' && c <= (char)0x7F)) ||
                (c == '-' && bNextToDot))
                    throw new ArgumentException(Environment.GetResourceString(
                        "Argument_IdnBadStd3", c), "Unicode");
        }

        //
        // The following punycode implementation is ported from the sample punycode.c in RFC 3492
        // Original sample code was written by Adam M. Costello.
        //
 
        // Return whether a punycode code point is flagged as being upper case.

        static bool HasUpperCaseFlag(char punychar)
        {
            return (punychar >= 'A' && punychar <= 'Z');
        }


        /**********************************************************/
        /* Implementation (would normally go in its own .c file): */

        /*** Bootstring parameters for Punycode ***/
        const int punycodeBase = 36;
        const int tmin = 1;
        const int tmax = 26;
        const int skew = 38;
        const int damp = 700;
        const int initial_bias = 72;
        const int initial_n = 0x80;
        const char delimiter = '-';

        /* basic(cp) tests whether cp is a basic code point: */
        static bool basic(uint cp)
        {
            // Is it in ASCII range?
            return cp < 0x80;
        }

        // decode_digit(cp) returns the numeric value of a basic code */
        // point (for use in representing integers) in the range 0 to */
        // punycodeBase-1, or <0 if cp is does not represent a value. */

        static int decode_digit(char cp)
        {
            if (cp >= '0' && cp <= '9')
                return cp - '0' + 26;

            // Two flavors for case differences
            if (cp >= 'a' && cp <= 'z')
                return cp - 'a';

            if (cp >= 'A' && cp <= 'Z')
                return cp - 'A';

            // Expected 0-9, A-Z or a-z, everything else is illegal
            throw new ArgumentException(Environment.GetResourceString(
                "Argument_IdnBadPunycode"), "ascii");
        }

        /* encode_digit(d,flag) returns the basic code point whose value      */
        /* (when used for representing integers) is d, which needs to be in   */
        /* the range 0 to punycodeBase-1.  The lowercase form is used unless flag is  */
        /* true, in which case the uppercase form is used. */

        static char encode_digit(int d)
        {
            Contract.Assert(d >= 0 && d < punycodeBase, "[IdnMapping.encode_digit]Expected 0 <= d < punycodeBase");
            // 26-35 map to ASCII 0-9
            if (d > 25) return (char)(d - 26 + '0');

            //  0-25 map to a-z or A-Z
            return (char)(d + 'a');
        }



        /* encode_basic(bcp,flag) forces a basic code point to lowercase */
        /* if flag is false, uppercase if flag is true, and returns    */
        /* the resulting code point.  The code point is unchanged if it  */
        /* is caseless.  The behavior is undefined if bcp is not a basic */
        /* code point.                                                   */

        static char encode_basic(char bcp)
        {
            if (HasUpperCaseFlag(bcp))
                bcp += (char)('a' - 'A');

            return bcp;
        }

        /*** Platform-specific constants ***/

        /* maxint is the maximum value of a uint variable: */
        const int maxint = 0x7ffffff;

        /*** Bias adaptation function ***/

        static int adapt(
            int delta, int numpoints, bool firsttime )
        {
            uint k;

            delta = firsttime ? delta / damp : delta / 2;
            Contract.Assert(numpoints != 0, "[IdnMapping.adapt]Expected non-zero numpoints.");
            delta += delta / numpoints;

            for (k = 0;  delta > ((punycodeBase - tmin) * tmax) / 2;  k += punycodeBase)
            {
              delta /= punycodeBase - tmin;
            }

            Contract.Assert(delta + skew != 0, "[IdnMapping.adapt]Expected non-zero delta+skew.");
            return (int)(k + (punycodeBase - tmin + 1) * delta / (delta + skew));
        }

        /*** Main encode function ***/

        /* punycode_encode() converts Unicode to Punycode.  The input     */
        /* is represented as an array of Unicode code points (not code    */
        /* units; surrogate pairs are not allowed), and the output        */
        /* will be represented as an array of ASCII code points.  The     */
        /* output string is *not* null-terminated; it will contain        */
        /* zeros if and only if the input contains zeros.  (Of course     */
        /* the caller can leave room for a terminator and add one if      */
        /* needed.)  The input_length is the number of code points in     */
        /* the input.  The output_length is an in/out argument: the       */
        /* caller passes in the maximum number of code points that it     */

        /* can receive, and on successful return it will contain the      */
        /* number of code points actually output.  The case_flags array   */
        /* holds input_length boolean values, where nonzero suggests that */
        /* the corresponding Unicode character be forced to uppercase     */
        /* after being decoded (if possible), and zero suggests that      */
        /* it be forced to lowercase (if possible).  ASCII code points    */
        /* are encoded literally, except that ASCII letters are forced    */
        /* to uppercase or lowercase according to the corresponding       */
        /* uppercase flags.  If case_flags is a null pointer then ASCII   */
        /* letters are left as they are, and other code points are        */
        /* treated as if their uppercase flags were zero.  The return     */
        /* value can be any of the punycode_status values defined above   */
        /* except punycode_bad_input; if not punycode_success, then       */
        /* output_size and output might contain garbage.                  */

        static String punycode_encode(String unicode)
        {
            // 0 length strings aren't allowed
            if (unicode.Length == 0)
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadLabelSize"), "unicode");
            Contract.EndContractBlock();

            StringBuilder output = new StringBuilder(unicode.Length);
            int iNextDot = 0;
            int iAfterLastDot = 0;
            int iOutputAfterLastDot = 0;

            // Find the next dot
            while (iNextDot < unicode.Length)
            {
                // Find end of this segment
                iNextDot = unicode.IndexOfAny(M_Dots, iAfterLastDot);
                Contract.Assert(iNextDot <= unicode.Length, "[IdnMapping.punycode_encode]IndexOfAny is broken");
                if (iNextDot < 0)
                    iNextDot = unicode.Length;

                // Only allowed to have empty . section at end (www.microsoft.com.)
                if (iNextDot == iAfterLastDot)
                {
                    // Only allowed to have empty sections as trailing .
                    if (iNextDot != unicode.Length)
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadLabelSize"), "unicode");
                    // Last dot, stop
                    break;
                }

                // We'll need an Ace prefix
                output.Append(M_strAcePrefix);

                // Everything resets every segment.
                bool bRightToLeft = false;

                // Check for RTL.  If right-to-left, then 1st & last chars must be RTL
                BidiCategory eBidi = CharUnicodeInfo.GetBidiCategory(unicode, iAfterLastDot);
                if (eBidi == BidiCategory.RightToLeft || eBidi == BidiCategory.RightToLeftArabic)
                {
                    // It has to be right to left.
                    bRightToLeft = true;

                    // Check last char
                    int iTest = iNextDot - 1;
                    if (Char.IsLowSurrogate(unicode, iTest))
                    {
                        iTest--;
                    }

                    eBidi = CharUnicodeInfo.GetBidiCategory(unicode, iTest);
                    if (eBidi != BidiCategory.RightToLeft && eBidi != BidiCategory.RightToLeftArabic)
                    {
                        // Oops, last wasn't RTL, last should be RTL if first is RTL
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadBidi"), "unicode");
                    }
                }

                // Handle the basic code points
                int basicCount;
                int numProcessed = 0;           // Num code points that have been processed so far (this segment)
                for (basicCount = iAfterLastDot; basicCount < iNextDot; basicCount++)
                {
                    // Can't be lonely surrogate because it would've thrown in normalization
                    Contract.Assert(Char.IsLowSurrogate(unicode, basicCount) == false,
                        "[IdnMapping.punycode_encode]Unexpected low surrogate");

                    // Double check our bidi rules
                    BidiCategory testBidi = CharUnicodeInfo.GetBidiCategory(unicode, basicCount);

                    // If we're RTL, we can't have LTR chars
                    if (bRightToLeft && testBidi == BidiCategory.LeftToRight)
                    {
                        // Oops, throw error
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadBidi"), "unicode");
                    }

                    // If we're not RTL we can't have RTL chars
                    if (!bRightToLeft && (testBidi == BidiCategory.RightToLeft ||
                                          testBidi == BidiCategory.RightToLeftArabic))
                    {
                        // Oops, throw error
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadBidi"), "unicode");
                    }

                    // If its basic then add it
                    if (basic(unicode[basicCount]))
                    {
                        output.Append(encode_basic(unicode[basicCount]));
                        numProcessed++;
                    }
                    // If its a surrogate, skip the next since our bidi category tester doesn't handle it.
                    else if (Char.IsSurrogatePair(unicode, basicCount))
                        basicCount++;
                }

                int numBasicCodePoints = numProcessed;     // number of basic code points

                // Stop if we ONLY had basic code points
                if (numBasicCodePoints == iNextDot - iAfterLastDot)
                {
                    // Get rid of xn-- and this segments done
                    output.Remove(iOutputAfterLastDot, M_strAcePrefix.Length);
                }
                else
                {
                    // If it has some non-basic code points the input cannot start with xn--
                    if (unicode.Length - iAfterLastDot >= M_strAcePrefix.Length &&
                        unicode.Substring(iAfterLastDot, M_strAcePrefix.Length).Equals(
                            M_strAcePrefix, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadPunycode"), "unicode");

                    // Need to do ACE encoding
                    int numSurrogatePairs = 0;            // number of surrogate pairs so far

                    // Add a delimiter (-) if we had any basic code points (between basic and encoded pieces)
                    if (numBasicCodePoints > 0)
                    {
                        output.Append(delimiter);
                    }

                    // Initialize the state
                    int n = initial_n;
                    int delta = 0;
                    int bias = initial_bias;

                    // Main loop
                    while (numProcessed < (iNextDot - iAfterLastDot))
                    {
                        /* All non-basic code points < n have been     */
                        /* handled already.  Find the next larger one: */
                        int j;
                        int m;
                        int test = 0;
                        for (m = maxint, j = iAfterLastDot;
                             j < iNextDot;
                             j += IsSupplementary(test) ? 2 : 1)
                        {
                            test = Char.ConvertToUtf32(unicode, j);
                            if (test >= n && test < m) m = test;
                        }

                        /* Increase delta enough to advance the decoder's    */
                        /* <n,i> state to <m,0>, but guard against overflow: */
                        delta += (int)((m - n) * ((numProcessed - numSurrogatePairs) + 1));
                        Contract.Assert(delta > 0, "[IdnMapping.cs]1 punycode_encode - delta overflowed int");
                        n = m;

                        for (j = iAfterLastDot;  j < iNextDot;  j+= IsSupplementary(test) ? 2 : 1)
                        {
                            // Make sure we're aware of surrogates
                            test = Char.ConvertToUtf32(unicode, j);

                            // Adjust for character position (only the chars in our string already, some
                            // haven't been processed.

                            if (test < n)
                            {
                                delta++;
                                Contract.Assert(delta > 0, "[IdnMapping.cs]2 punycode_encode - delta overflowed int");
                            }

                            if (test == n)
                            {
                                // Represent delta as a generalized variable-length integer:
                                int q, k;
                                for (q = delta, k = punycodeBase;  ;  k += punycodeBase)
                                {
                                    int t = k <= bias ? tmin :
                                            k >= bias + tmax ? tmax : k - bias;
                                    if (q < t) break;
                                    Contract.Assert(punycodeBase != t, "[IdnMapping.punycode_encode]Expected punycodeBase (36) to be != t");
                                    output.Append(encode_digit(t + (q - t) % (punycodeBase - t)));
                                    q = (q - t) / (punycodeBase - t);
                                }

                                output.Append(encode_digit(q));
                                bias = adapt(delta, (numProcessed - numSurrogatePairs) + 1, numProcessed == numBasicCodePoints);
                                delta = 0;
                                numProcessed++;

                                if (IsSupplementary(m))
                                {
                                    numProcessed++;
                                    numSurrogatePairs++;
                                }
                            }
                        }
                        ++delta;
                        ++n;
                        Contract.Assert(delta > 0, "[IdnMapping.cs]3 punycode_encode - delta overflowed int");
                    }
                }

                // Make sure its not too big
                if (output.Length - iOutputAfterLastDot > M_labelLimit)
                    throw new ArgumentException(Environment.GetResourceString(
                        "Argument_IdnBadLabelSize"), "unicode");

                // Done with this segment, add dot if necessary
                if (iNextDot != unicode.Length)
                    output.Append('.');

                iAfterLastDot = iNextDot + 1;
                iOutputAfterLastDot = output.Length;
            }

            // Throw if we're too long
            if (output.Length > M_defaultNameLimit - (IsDot(unicode[unicode.Length-1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadNameSize",
                    M_defaultNameLimit - (IsDot(unicode[unicode.Length-1]) ? 0 : 1)),
                    "unicode");

            // Return our output string
            return output.ToString();
        }

        /*** Main decode function ***/

        /* punycode_decode() converts Punycode to Unicode.  The input is  */
        /* represented as an array of ASCII code points, and the output   */
        /* will be represented as an array of Unicode code points.  The   */
        /* input_length is the number of code points in the input.  The   */
        /* output_length is an in/out argument: the caller passes in      */
        /* the maximum number of code points that it can receive, and     */
        /* on successful return it will contain the actual number of      */
        /* code points output.  The case_flags array needs room for at    */
        /* least output_length values, or it can be a null pointer if the */
        /* case information is not needed.  A nonzero flag suggests that  */
        /* the corresponding Unicode character be forced to uppercase     */
        /* by the caller (if possible), while zero suggests that it be    */
        /* forced to lowercase (if possible).  ASCII code points are      */
        /* output already in the proper case, but their flags will be set */
        /* appropriately so that applying the flags would be harmless.    */
        /* The return value can be any of the punycode_status values      */
        /* defined above; if not punycode_success, then output_length,    */
        /* output, and case_flags might contain garbage.  On success, the */
        /* decoder will never need to write an output_length greater than */
        /* input_length, because of how the encoding is defined.          */

        static String punycode_decode( String ascii )
        {
            // 0 length strings aren't allowed
            if (ascii.Length == 0)
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadLabelSize"), "ascii");
            Contract.EndContractBlock();

            // Throw if we're too long
            if (ascii.Length > M_defaultNameLimit - (IsDot(ascii[ascii.Length-1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadNameSize",
                    M_defaultNameLimit - (IsDot(ascii[ascii.Length-1]) ? 0 : 1)), "ascii");

            // output stringbuilder
            StringBuilder output = new StringBuilder(ascii.Length);

            // Dot searching
            int iNextDot = 0;
            int iAfterLastDot = 0;
            int iOutputAfterLastDot = 0;

            while (iNextDot < ascii.Length)
            {
                // Find end of this segment
                iNextDot = ascii.IndexOf('.', iAfterLastDot);
                if (iNextDot < 0 || iNextDot > ascii.Length)
                    iNextDot = ascii.Length;

                // Only allowed to have empty . section at end (www.microsoft.com.)
                if (iNextDot == iAfterLastDot)
                {
                    // Only allowed to have empty sections as trailing .
                    if (iNextDot != ascii.Length)
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadLabelSize"), "ascii");

                    // Last dot, stop
                    break;
                }

                // In either case it can't be bigger than segment size
                if (iNextDot - iAfterLastDot > M_labelLimit)
                    throw new ArgumentException(Environment.GetResourceString(
                        "Argument_IdnBadLabelSize"), "ascii");

                // See if this section's ASCII or ACE
                if (ascii.Length < M_strAcePrefix.Length + iAfterLastDot ||
                    !ascii.Substring(iAfterLastDot, M_strAcePrefix.Length).Equals(
                        M_strAcePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Its supposed to be just ASCII
                    // Actually, for non xn-- stuff do we want to allow Unicode?
           //         for (int i = iAfterLastDot; i < iNextDot; i++)
             //       {
               //         // Only ASCII is allowed
                 //       if (ascii[i] >= 0x80)
                   //         throw new ArgumentException(Environment.GetResourceString(
                     //           "Argument_IdnBadPunycode"), "ascii");
//                    }

                    // Its ASCII, copy it
                    output.Append(ascii.Substring(iAfterLastDot, iNextDot - iAfterLastDot));

                    // ASCII doesn't have BIDI issues
                }
                else
                {
                    // Not ASCII, bump up iAfterLastDot to be after ACE Prefix
                    iAfterLastDot += M_strAcePrefix.Length;

                    // Get number of basic code points (where delimiter is)
                    // numBasicCodePoints < 0 if there're no basic code points
                    int iTemp = ascii.LastIndexOf(delimiter, iNextDot - 1);

                    // Trailing - not allowed
                    if (iTemp == iNextDot - 1)
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadPunycode"), "ascii");

                    int numBasicCodePoints;
                    if (iTemp <= iAfterLastDot)
                        numBasicCodePoints = 0;
                    else
                    {
                        numBasicCodePoints = iTemp - iAfterLastDot;

                        // Copy all the basic code points, making sure they're all in the allowed range,
                        // and losing the casing for all of them.
                        for (int copyAscii = iAfterLastDot;
                             copyAscii < iAfterLastDot + numBasicCodePoints;
                             copyAscii++)
                        {
                            // Make sure we don't allow unicode in the ascii part
                            if (ascii[copyAscii] > 0x7f)
                                throw new ArgumentException(Environment.GetResourceString(
                                    "Argument_IdnBadPunycode"), "ascii");

                            // When appending make sure they get lower cased
                            output.Append((char)(ascii[copyAscii] >= 'A' && ascii[copyAscii] <='Z' ?
                                                 ascii[copyAscii] - 'A' + 'a' :
                                                 ascii[copyAscii]));
                        }
                    }

                    // Get ready for main loop.  Start at beginning if we didn't have any
                    // basic code points, otherwise start after the -.
                    // asciiIndex will be next character to read from ascii
                    int asciiIndex = iAfterLastDot +
                        ( numBasicCodePoints > 0 ? numBasicCodePoints + 1 : 0);

                    // initialize our state
                    int n = initial_n;
                    int bias = initial_bias;
                    int i = 0;

                    int w, k;

                    // no Supplementary characters yet
                    int numSurrogatePairs = 0;

                    // Main loop, read rest of ascii
                    while (asciiIndex < iNextDot)
                    {
                        /* Decode a generalized variable-length integer into delta,  */
                        /* which gets added to i.  The overflow checking is easier   */
                        /* if we increase i as we go, then subtract off its starting */
                        /* value at the end to obtain delta.                         */
                        int oldi = i;

                        for (w = 1, k = punycodeBase;  ;  k += punycodeBase)
                        {
                            // Check to make sure we aren't overrunning our ascii string
                            if (asciiIndex >= iNextDot)
                                throw new ArgumentException(Environment.GetResourceString(
                                    "Argument_IdnBadPunycode"), "ascii");

                            // decode the digit from the next char
                            int digit = decode_digit(ascii[asciiIndex++]);

                            Contract.Assert(w > 0, "[IdnMapping.punycode_decode]Expected w > 0");
                            if (digit > (maxint - i) / w)
                                throw new ArgumentException(Environment.GetResourceString(
                                    "Argument_IdnBadPunycode"), "ascii");

                            i += (int)(digit * w);
                            int t = k <= bias ? tmin :
                                    k >= bias + tmax ? tmax : k - bias;
                            if (digit < t) break;
                            Contract.Assert(punycodeBase != t, "[IdnMapping.punycode_decode]Expected t != punycodeBase (36)");
                            if (w > maxint / (punycodeBase - t))
                                throw new ArgumentException(Environment.GetResourceString(
                                    "Argument_IdnBadPunycode"), "ascii");
                            w *= (punycodeBase - t);
                        }

                        bias = adapt(i - oldi,
                            (output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1, oldi == 0);

                        /* i was supposed to wrap around from output.Length to 0,   */
                        /* incrementing n each time, so we'll fix that now: */
                        Contract.Assert((output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1 > 0,
                            "[IdnMapping.punycode_decode]Expected to have added > 0 characters this segment");
                        if (i / ((output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1) > maxint - n)
                            throw new ArgumentException(Environment.GetResourceString(
                                "Argument_IdnBadPunycode"), "ascii");
                        n += (int)(i / (output.Length - iOutputAfterLastDot - numSurrogatePairs + 1));
                        i %= (output.Length - iOutputAfterLastDot - numSurrogatePairs + 1);

                        // If it was flagged it needs to be capitalized
        //                if (HasUpperCaseFlag(ascii[asciiIndex - 1]))
        //                {
        //                    /* Case of last character determines uppercase flag: */
        //                  // Any casing stuff need to happen last.
                            // If we wanted to reverse the IDNA casing data
        //                    n = MakeNUpperCase(n)
        //                }

                        // Make sure n is legal
                        if ((n < 0 || n > 0x10ffff) || (n >= 0xD800 && n <= 0xDFFF))
                            throw new ArgumentException(Environment.GetResourceString(
                                "Argument_IdnBadPunycode"), "ascii");

                        // insert n at position i of the output:  Really tricky if we have surrogates
                        int iUseInsertLocation;
                        String strTemp = Char.ConvertFromUtf32(n);

                        // If we have supplimentary characters
                        if (numSurrogatePairs > 0)
                        {
                            // Hard way, we have supplimentary characters
                            int iCount;
                            for (iCount = i, iUseInsertLocation = iOutputAfterLastDot;
                                 iCount > 0;
                                 iCount--, iUseInsertLocation++)
                            {
                                // If its a surrogate, we have to go one more
                                if (iUseInsertLocation >= output.Length)
                                    throw new ArgumentException(Environment.GetResourceString(
                                        "Argument_IdnBadPunycode"), "ascii");
                                if (Char.IsSurrogate(output[iUseInsertLocation]))
                                    iUseInsertLocation++;
                            }
                        }
                        else
                        {
                            // No Supplementary chars yet, just add i
                            iUseInsertLocation = iOutputAfterLastDot + i;
                        }

                        // Insert it
                        output.Insert(iUseInsertLocation, strTemp);

                        // If it was a surrogate increment our counter
                        if (IsSupplementary(n))
                            numSurrogatePairs++;

                        // Index gets updated
                        i++;
                    }

                    // Do BIDI testing
                    bool bRightToLeft = false;

                    // Check for RTL.  If right-to-left, then 1st & last chars must be RTL
                    BidiCategory eBidi = CharUnicodeInfo.GetBidiCategory(output.ToString(), iOutputAfterLastDot);
                    if (eBidi == BidiCategory.RightToLeft || eBidi == BidiCategory.RightToLeftArabic)
                    {
                        // It has to be right to left.
                        bRightToLeft = true;
                    }

                    // Check the rest of them to make sure RTL/LTR is consistent
                    for (int iTest = iOutputAfterLastDot; iTest < output.Length; iTest++)
                    {
                        // This might happen if we run into a pair
                        if (Char.IsLowSurrogate(output.ToString(), iTest)) continue;

                        // Check to see if its LTR
                        eBidi = CharUnicodeInfo.GetBidiCategory(output.ToString(), iTest);
                        if ((bRightToLeft && eBidi == BidiCategory.LeftToRight) ||
                            (!bRightToLeft && (eBidi == BidiCategory.RightToLeft ||
                                               eBidi == BidiCategory.RightToLeftArabic)))
                            throw new ArgumentException(Environment.GetResourceString(
                                "Argument_IdnBadBidi"), "ascii");

                        // Make it lower case if we must (so we can test IsNormalized later)
        //                if (output[iTest] >= 'A' && output[iTest] <= 'Z')
          //                  output[iTest] = (char)(output[iTest] + (char)('a' - 'A'));
                    }

                    // Its also a requirement that the last one be RTL if 1st is RTL
                    if (bRightToLeft && eBidi != BidiCategory.RightToLeft && eBidi != BidiCategory.RightToLeftArabic)
                    {
                        // Oops, last wasn't RTL, last should be RTL if first is RTL
                        throw new ArgumentException(Environment.GetResourceString(
                            "Argument_IdnBadBidi"), "ascii");
                    }
                }

                // See if this label was too long
                if (iNextDot - iAfterLastDot > M_labelLimit)
                    throw new ArgumentException(Environment.GetResourceString(
                        "Argument_IdnBadLabelSize"), "ascii");

                // Done with this segment, add dot if necessary
                if (iNextDot != ascii.Length)
                    output.Append('.');

                iAfterLastDot = iNextDot + 1;
                iOutputAfterLastDot = output.Length;
            }

            // Throw if we're too long
            if (output.Length > M_defaultNameLimit - (IsDot(output[output.Length-1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString(
                    "Argument_IdnBadNameSize",
                    M_defaultNameLimit -(IsDot(output[output.Length-1]) ? 0 : 1)), "ascii");

            // Return our output string
            return output.ToString();
        }

        /*
        The previous punycode implimentation is based on the sample code in RFC 3492

        Full Copyright Statement

           Copyright (C) The Internet Society (2003).  All Rights Reserved.

           This document and translations of it may be copied and furnished to
           others, and derivative works that comment on or otherwise explain it
           or assist in its implementation may be prepared, copied, published
           and distributed, in whole or in part, without restriction of any
           kind, provided that the above copyright notice and this paragraph are
           included on all such copies and derivative works.  However, this
           document itself may not be modified in any way, such as by removing
           the copyright notice or references to the Internet Society or other
           Internet organizations, except as needed for the purpose of
           developing Internet standards in which case the procedures for
           copyrights defined in the Internet Standards process must be
           followed, or as required to translate it into languages other than
           English.

           The limited permissions granted above are perpetual and will not be
           revoked by the Internet Society or its successors or assigns.

           This document and the information contained herein is provided on an
           "AS IS" basis and THE INTERNET SOCIETY AND THE INTERNET ENGINEERING
           TASK FORCE DISCLAIMS ALL WARRANTIES, EXPRESS OR IMPLIED, INCLUDING
           BUT NOT LIMITED TO ANY WARRANTY THAT THE USE OF THE INFORMATION
           HEREIN WILL NOT INFRINGE ANY RIGHTS OR ANY IMPLIED WARRANTIES OF
           MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
        */


        private const int IDN_ALLOW_UNASSIGNED      = 0x1;
        private const int IDN_USE_STD3_ASCII_RULES  = 0x2;
        
        private const int ERROR_INVALID_NAME = 123;


        [System.Security.SecurityCritical]
        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern int IdnToAscii(
                                        uint    dwFlags, 
                                        [InAttribute()]
                                        [MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
                                        String  lpUnicodeCharStr, 
                                        int     cchUnicodeChar, 
                                        [System.Runtime.InteropServices.OutAttribute()]

                                        char    [] lpASCIICharStr, 
                                        int     cchASCIIChar);

        [System.Security.SecurityCritical]
        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern int IdnToUnicode(
                                        uint    dwFlags, 
                                        [InAttribute()]
                                        [MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
                                        string  lpASCIICharStr, 
                                        int     cchASCIIChar, 
                                        [System.Runtime.InteropServices.OutAttribute()]

                                        char    []  lpUnicodeCharStr,
                                        int     cchUnicodeChar);
    }
}

