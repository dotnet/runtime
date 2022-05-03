// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Globalization
{
    // IdnMapping class used to map names to Punycode
    public sealed partial class IdnMapping
    {
        private bool _allowUnassigned;
        private bool _useStd3AsciiRules;

        public IdnMapping()
        {
        }

        public bool AllowUnassigned
        {
            get => _allowUnassigned;
            set => _allowUnassigned = value;
        }

        public bool UseStd3AsciiRules
        {
            get => _useStd3AsciiRules;
            set => _useStd3AsciiRules = value;
        }

        // Gets ASCII (Punycode) version of the string
        public string GetAscii(string unicode) =>
            GetAscii(unicode, 0);

        public string GetAscii(string unicode, int index)
        {
            ArgumentNullException.ThrowIfNull(unicode);

            return GetAscii(unicode, index, unicode.Length - index);
        }

        public string GetAscii(string unicode, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(unicode);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > unicode.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            if (index > unicode.Length - count)
                throw new ArgumentOutOfRangeException(nameof(unicode), SR.ArgumentOutOfRange_IndexCountBuffer);

            if (count == 0)
            {
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));
            }
            if (unicode[index + count - 1] == 0)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, index + count - 1), nameof(unicode));
            }

            if (GlobalizationMode.Invariant)
            {
                return GetAsciiInvariant(unicode, index, count);
            }

            unsafe
            {
                fixed (char* pUnicode = unicode)
                {
                    return GlobalizationMode.UseNls ?
                        NlsGetAsciiCore(unicode, pUnicode + index, count) :
                        IcuGetAsciiCore(unicode, pUnicode + index, count);
                }
            }
        }

        // Gets Unicode version of the string.  Normalized and limited to IDNA characters.
        public string GetUnicode(string ascii) =>
            GetUnicode(ascii, 0);

        public string GetUnicode(string ascii, int index)
        {
            ArgumentNullException.ThrowIfNull(ascii);

            return GetUnicode(ascii, index, ascii.Length - index);
        }

        public string GetUnicode(string ascii, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(ascii);

            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? nameof(index) : nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > ascii.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            if (index > ascii.Length - count)
                throw new ArgumentOutOfRangeException(nameof(ascii), SR.ArgumentOutOfRange_IndexCountBuffer);

            // This is a case (i.e. explicitly null-terminated input) where behavior in .NET and Win32 intentionally differ.
            // The .NET APIs should (and did in v4.0 and earlier) throw an ArgumentException on input that includes a terminating null.
            // The Win32 APIs fail on an embedded null, but not on a terminating null.
            if (count > 0 && ascii[index + count - 1] == (char)0)
                throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));

            if (GlobalizationMode.Invariant)
            {
                return GetUnicodeInvariant(ascii, index, count);
            }

            unsafe
            {
                fixed (char* pAscii = ascii)
                {
                    return GlobalizationMode.UseNls ?
                        NlsGetUnicodeCore(ascii, pAscii + index, count) :
                        IcuGetUnicodeCore(ascii, pAscii + index, count);
                }
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is IdnMapping that &&
            _allowUnassigned == that._allowUnassigned &&
            _useStd3AsciiRules == that._useStd3AsciiRules;

        public override int GetHashCode() =>
            (_allowUnassigned ? 100 : 200) + (_useStd3AsciiRules ? 1000 : 2000);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string GetStringForOutput(string originalString, char* input, int inputLength, char* output, int outputLength)
        {
            Debug.Assert(inputLength > 0);

            if (originalString.Length == inputLength &&
                inputLength == outputLength &&
                Ordinal.EqualsIgnoreCase(ref *input, ref *output, inputLength))
            {
                return originalString;
            }

            return new string(output, 0, outputLength);
        }

        //
        // Invariant implementation
        //

        private const char c_delimiter = '-';
        private const string c_strAcePrefix = "xn--";
        private const int c_labelLimit = 63;          // Not including dots
        private const int c_defaultNameLimit = 255;   // Including dots
        private const int c_initialN = 0x80;
        private const int c_maxint = 0x7ffffff;
        private const int c_initialBias = 72;
        private const int c_punycodeBase = 36;
        private const int c_tmin = 1;
        private const int c_tmax = 26;
        private const int c_skew = 38;
        private const int c_damp = 700;

        // Legal "dot" separators (i.e: . in www.microsoft.com)
        private static readonly char[] s_dotSeparators = { '.', '\u3002', '\uFF0E', '\uFF61' };

        private string GetAsciiInvariant(string unicode, int index, int count)
        {
            if (index > 0 || count < unicode.Length)
            {
                unicode = unicode.Substring(index, count);
            }

            // Check for ASCII only string, which will be unchanged
            if (ValidateStd3AndAscii(unicode, UseStd3AsciiRules, true))
            {
                return unicode;
            }

            // Cannot be null terminated (normalization won't help us with this one, and
            // may have returned false before checking the whole string above)
            Debug.Assert(count >= 1, "[IdnMapping.GetAscii] Expected 0 length strings to fail before now.");
            if (unicode[^1] <= 0x1f)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, unicode.Length - 1), nameof(unicode));
            }

            // May need to check Std3 rules again for non-ascii
            if (UseStd3AsciiRules)
            {
                ValidateStd3AndAscii(unicode, true, false);
            }

            // Go ahead and encode it
            return PunycodeEncode(unicode);
        }

        // See if we're only ASCII
        private static bool ValidateStd3AndAscii(string unicode, bool bUseStd3, bool bCheckAscii)
        {
            // If its empty, then its too small
            if (unicode.Length == 0)
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));

            int iLastDot = -1;

            // Loop the whole string
            for (int i = 0; i < unicode.Length; i++)
            {
                // Aren't allowing control chars (or 7f, but idn tables catch that, they don't catch \0 at end though)
                if (unicode[i] <= 0x1f)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidCharSequence, i), nameof(unicode));
                }

                // If its Unicode or a control character, return false (non-ascii)
                if (bCheckAscii && unicode[i] >= 0x7f)
                    return false;

                // Check for dots
                if (IsDot(unicode[i]))
                {
                    // Can't have 2 dots in a row
                    if (i == iLastDot + 1)
                        throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));

                    // If its too far between dots then fail
                    if (i - iLastDot > c_labelLimit + 1)
                        throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));

                    // If validating Std3, then char before dot can't be - char
                    if (bUseStd3 && i > 0)
                        ValidateStd3(unicode[i - 1], true);

                    // Remember where the last dot is
                    iLastDot = i;
                    continue;
                }

                // If necessary, make sure its a valid std3 character
                if (bUseStd3)
                {
                    ValidateStd3(unicode[i], i == iLastDot + 1);
                }
            }

            // If we never had a dot, then we need to be shorter than the label limit
            if (iLastDot == -1 && unicode.Length > c_labelLimit)
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));

            // Need to validate entire string length, 1 shorter if last char wasn't a dot
            if (unicode.Length > c_defaultNameLimit - (IsDot(unicode[^1]) ? 0 : 1))
                throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize,
                                                        c_defaultNameLimit - (IsDot(unicode[^1]) ? 0 : 1)), nameof(unicode));

            // If last char wasn't a dot we need to check for trailing -
            if (bUseStd3 && !IsDot(unicode[^1]))
                ValidateStd3(unicode[^1], true);

            return true;
        }

        /* PunycodeEncode() converts Unicode to Punycode.  The input     */
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
        private static string PunycodeEncode(string unicode)
        {
            // 0 length strings aren't allowed
            if (unicode.Length == 0)
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));

            StringBuilder output = new StringBuilder(unicode.Length);
            int iNextDot = 0;
            int iAfterLastDot = 0;
            int iOutputAfterLastDot = 0;

            // Find the next dot
            while (iNextDot < unicode.Length)
            {
                // Find end of this segment
                iNextDot = unicode.IndexOfAny(s_dotSeparators, iAfterLastDot);
                Debug.Assert(iNextDot <= unicode.Length, "[IdnMapping.punycode_encode]IndexOfAny is broken");
                if (iNextDot < 0)
                    iNextDot = unicode.Length;

                // Only allowed to have empty . section at end (www.microsoft.com.)
                if (iNextDot == iAfterLastDot)
                {
                    // Only allowed to have empty sections as trailing .
                    if (iNextDot != unicode.Length)
                        throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));
                    // Last dot, stop
                    break;
                }

                // We'll need an Ace prefix
                output.Append(c_strAcePrefix);

                // Everything resets every segment.
                bool bRightToLeft = false;

                // Check for RTL.  If right-to-left, then 1st & last chars must be RTL
                StrongBidiCategory eBidi = CharUnicodeInfo.GetBidiCategory(unicode, iAfterLastDot);
                if (eBidi == StrongBidiCategory.StrongRightToLeft)
                {
                    // It has to be right to left.
                    bRightToLeft = true;

                    // Check last char
                    int iTest = iNextDot - 1;
                    if (char.IsLowSurrogate(unicode, iTest))
                    {
                        iTest--;
                    }

                    eBidi = CharUnicodeInfo.GetBidiCategory(unicode, iTest);
                    if (eBidi != StrongBidiCategory.StrongRightToLeft)
                    {
                        // Oops, last wasn't RTL, last should be RTL if first is RTL
                        throw new ArgumentException(SR.Argument_IdnBadBidi, nameof(unicode));
                    }
                }

                // Handle the basic code points
                int basicCount;
                int numProcessed = 0;           // Num code points that have been processed so far (this segment)
                for (basicCount = iAfterLastDot; basicCount < iNextDot; basicCount++)
                {
                    // Can't be lonely surrogate because it would've thrown in normalization
                    Debug.Assert(!char.IsLowSurrogate(unicode, basicCount), "[IdnMapping.punycode_encode]Unexpected low surrogate");

                    // Double check our bidi rules
                    StrongBidiCategory testBidi = CharUnicodeInfo.GetBidiCategory(unicode, basicCount);

                    // If we're RTL, we can't have LTR chars
                    if (bRightToLeft && testBidi == StrongBidiCategory.StrongLeftToRight)
                    {
                        // Oops, throw error
                        throw new ArgumentException(SR.Argument_IdnBadBidi, nameof(unicode));
                    }

                    // If we're not RTL we can't have RTL chars
                    if (!bRightToLeft && testBidi == StrongBidiCategory.StrongRightToLeft)
                    {
                        // Oops, throw error
                        throw new ArgumentException(SR.Argument_IdnBadBidi, nameof(unicode));
                    }

                    // If its basic then add it
                    if (Basic(unicode[basicCount]))
                    {
                        output.Append(EncodeBasic(unicode[basicCount]));
                        numProcessed++;
                    }
                    // If its a surrogate, skip the next since our bidi category tester doesn't handle it.
                    else if (char.IsSurrogatePair(unicode, basicCount))
                        basicCount++;
                }

                int numBasicCodePoints = numProcessed;     // number of basic code points

                // Stop if we ONLY had basic code points
                if (numBasicCodePoints == iNextDot - iAfterLastDot)
                {
                    // Get rid of xn-- and this segments done
                    output.Remove(iOutputAfterLastDot, c_strAcePrefix.Length);
                }
                else
                {
                    // If it has some non-basic code points the input cannot start with xn--
                    if (unicode.Length - iAfterLastDot >= c_strAcePrefix.Length &&
                        unicode.Substring(iAfterLastDot, c_strAcePrefix.Length).Equals(
                            c_strAcePrefix, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(unicode));

                    // Need to do ACE encoding
                    int numSurrogatePairs = 0;            // number of surrogate pairs so far

                    // Add a delimiter (-) if we had any basic code points (between basic and encoded pieces)
                    if (numBasicCodePoints > 0)
                    {
                        output.Append(c_delimiter);
                    }

                    // Initialize the state
                    int n = c_initialN;
                    int delta = 0;
                    int bias = c_initialBias;

                    // Main loop
                    while (numProcessed < (iNextDot - iAfterLastDot))
                    {
                        /* All non-basic code points < n have been     */
                        /* handled already.  Find the next larger one: */
                        int j;
                        int m;
                        int test;
                        for (m = c_maxint, j = iAfterLastDot;
                             j < iNextDot;
                             j += IsSupplementary(test) ? 2 : 1)
                        {
                            test = char.ConvertToUtf32(unicode, j);
                            if (test >= n && test < m) m = test;
                        }

                        /* Increase delta enough to advance the decoder's    */
                        /* <n,i> state to <m,0>, but guard against overflow: */
                        delta += (int)((m - n) * ((numProcessed - numSurrogatePairs) + 1));
                        Debug.Assert(delta > 0, "[IdnMapping.cs]1 punycode_encode - delta overflowed int");
                        n = m;

                        for (j = iAfterLastDot; j < iNextDot; j += IsSupplementary(test) ? 2 : 1)
                        {
                            // Make sure we're aware of surrogates
                            test = char.ConvertToUtf32(unicode, j);

                            // Adjust for character position (only the chars in our string already, some
                            // haven't been processed.

                            if (test < n)
                            {
                                delta++;
                                Debug.Assert(delta > 0, "[IdnMapping.cs]2 punycode_encode - delta overflowed int");
                            }

                            if (test == n)
                            {
                                // Represent delta as a generalized variable-length integer:
                                int q, k;
                                for (q = delta, k = c_punycodeBase; ; k += c_punycodeBase)
                                {
                                    int t = k <= bias ? c_tmin : k >= bias + c_tmax ? c_tmax : k - bias;
                                    if (q < t) break;
                                    Debug.Assert(c_punycodeBase != t, "[IdnMapping.punycode_encode]Expected c_punycodeBase (36) to be != t");
                                    output.Append(EncodeDigit(t + (q - t) % (c_punycodeBase - t)));
                                    q = (q - t) / (c_punycodeBase - t);
                                }

                                output.Append(EncodeDigit(q));
                                bias = Adapt(delta, (numProcessed - numSurrogatePairs) + 1, numProcessed == numBasicCodePoints);
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
                        Debug.Assert(delta > 0, "[IdnMapping.cs]3 punycode_encode - delta overflowed int");
                    }
                }

                // Make sure its not too big
                if (output.Length - iOutputAfterLastDot > c_labelLimit)
                    throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(unicode));

                // Done with this segment, add dot if necessary
                if (iNextDot != unicode.Length)
                    output.Append('.');

                iAfterLastDot = iNextDot + 1;
                iOutputAfterLastDot = output.Length;
            }

            // Throw if we're too long
            if (output.Length > c_defaultNameLimit - (IsDot(unicode[^1]) ? 0 : 1))
                throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize,
                                                c_defaultNameLimit - (IsDot(unicode[^1]) ? 0 : 1)), nameof(unicode));
            // Return our output string
            return output.ToString();
        }

        // Is it a dot?
        // are we U+002E (., full stop), U+3002 (ideographic full stop), U+FF0E (fullwidth full stop), or
        // U+FF61 (halfwidth ideographic full stop).
        // Note: IDNA Normalization gets rid of dots now, but testing for last dot is before normalization
        private static bool IsDot(char c) =>
            c == '.' || c == '\u3002' || c == '\uFF0E' || c == '\uFF61';

        private static bool IsSupplementary(int cTest) =>
            cTest >= 0x10000;

        private static bool Basic(uint cp) =>
            // Is it in ASCII range?
            cp < 0x80;

        // Validate Std3 rules for a character
        private static void ValidateStd3(char c, bool bNextToDot)
        {
            // Check for illegal characters
            if (c <= ',' || c == '/' || (c >= ':' && c <= '@') ||      // Lots of characters not allowed
                (c >= '[' && c <= '`') || (c >= '{' && c <= (char)0x7F) ||
                (c == '-' && bNextToDot))
                throw new ArgumentException(SR.Format(SR.Argument_IdnBadStd3, c), nameof(c));
        }

        private string GetUnicodeInvariant(string ascii, int index, int count)
        {
            if (index > 0 || count < ascii.Length)
            {
                // We're only using part of the string
                ascii = ascii.Substring(index, count);
            }
            // Convert Punycode to Unicode
            string strUnicode = PunycodeDecode(ascii);

            // Output name MUST obey IDNA rules & round trip (casing differences are allowed)
            if (!ascii.Equals(GetAscii(strUnicode), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(ascii));

            return strUnicode;
        }

        /* PunycodeDecode() converts Punycode to Unicode.  The input is  */
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

        private static string PunycodeDecode(string ascii)
        {
            // 0 length strings aren't allowed
            if (ascii.Length == 0)
                throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(ascii));

            // Throw if we're too long
            if (ascii.Length > c_defaultNameLimit - (IsDot(ascii[^1]) ? 0 : 1))
                throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize,
                                            c_defaultNameLimit - (IsDot(ascii[^1]) ? 0 : 1)), nameof(ascii));

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
                        throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(ascii));

                    // Last dot, stop
                    break;
                }

                // In either case it can't be bigger than segment size
                if (iNextDot - iAfterLastDot > c_labelLimit)
                    throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(ascii));

                // See if this section's ASCII or ACE
                if (ascii.Length < c_strAcePrefix.Length + iAfterLastDot ||
                    string.Compare(ascii, iAfterLastDot, c_strAcePrefix, 0, c_strAcePrefix.Length, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    // Its ASCII, copy it
                    output.Append(ascii, iAfterLastDot, iNextDot - iAfterLastDot);
                }
                else
                {
                    // Not ASCII, bump up iAfterLastDot to be after ACE Prefix
                    iAfterLastDot += c_strAcePrefix.Length;

                    // Get number of basic code points (where delimiter is)
                    // numBasicCodePoints < 0 if there're no basic code points
                    int iTemp = ascii.LastIndexOf(c_delimiter, iNextDot - 1);

                    // Trailing - not allowed
                    if (iTemp == iNextDot - 1)
                        throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));

                    int numBasicCodePoints;
                    if (iTemp <= iAfterLastDot)
                        numBasicCodePoints = 0;
                    else
                    {
                        numBasicCodePoints = iTemp - iAfterLastDot;

                        // Copy all the basic code points, making sure they're all in the allowed range,
                        // and losing the casing for all of them.
                        for (int copyAscii = iAfterLastDot; copyAscii < iAfterLastDot + numBasicCodePoints; copyAscii++)
                        {
                            // Make sure we don't allow unicode in the ascii part
                            if (ascii[copyAscii] > 0x7f)
                                throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));

                            // When appending make sure they get lower cased
                            output.Append((char)(ascii[copyAscii] >= 'A' && ascii[copyAscii] <= 'Z' ? ascii[copyAscii] - 'A' + 'a' : ascii[copyAscii]));
                        }
                    }

                    // Get ready for main loop.  Start at beginning if we didn't have any
                    // basic code points, otherwise start after the -.
                    // asciiIndex will be next character to read from ascii
                    int asciiIndex = iAfterLastDot + (numBasicCodePoints > 0 ? numBasicCodePoints + 1 : 0);

                    // initialize our state
                    int n = c_initialN;
                    int bias = c_initialBias;
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

                        for (w = 1, k = c_punycodeBase; ; k += c_punycodeBase)
                        {
                            // Check to make sure we aren't overrunning our ascii string
                            if (asciiIndex >= iNextDot)
                                throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));

                            // decode the digit from the next char
                            int digit = DecodeDigit(ascii[asciiIndex++]);

                            Debug.Assert(w > 0, "[IdnMapping.punycode_decode]Expected w > 0");
                            if (digit > (c_maxint - i) / w)
                                throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));

                            i += (int)(digit * w);
                            int t = k <= bias ? c_tmin : k >= bias + c_tmax ? c_tmax : k - bias;
                            if (digit < t)
                                break;
                            Debug.Assert(c_punycodeBase != t, "[IdnMapping.punycode_decode]Expected t != c_punycodeBase (36)");
                            if (w > c_maxint / (c_punycodeBase - t))
                                throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));
                            w *= (c_punycodeBase - t);
                        }

                        bias = Adapt(i - oldi, (output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1, oldi == 0);

                        /* i was supposed to wrap around from output.Length to 0,   */
                        /* incrementing n each time, so we'll fix that now: */
                        Debug.Assert((output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1 > 0,
                            "[IdnMapping.punycode_decode]Expected to have added > 0 characters this segment");
                        if (i / ((output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1) > c_maxint - n)
                            throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));
                        n += (int)(i / (output.Length - iOutputAfterLastDot - numSurrogatePairs + 1));
                        i %= (output.Length - iOutputAfterLastDot - numSurrogatePairs + 1);

                        // Make sure n is legal
                        if (n < 0 || n > 0x10ffff || (n >= 0xD800 && n <= 0xDFFF))
                            throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));

                        // insert n at position i of the output:  Really tricky if we have surrogates
                        int iUseInsertLocation;
                        string strTemp = char.ConvertFromUtf32(n);

                        // If we have supplimentary characters
                        if (numSurrogatePairs > 0)
                        {
                            // Hard way, we have supplimentary characters
                            int iCount;
                            for (iCount = i, iUseInsertLocation = iOutputAfterLastDot; iCount > 0; iCount--, iUseInsertLocation++)
                            {
                                // If its a surrogate, we have to go one more
                                if (iUseInsertLocation >= output.Length)
                                    throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(ascii));
                                if (char.IsSurrogate(output[iUseInsertLocation]))
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
                    StrongBidiCategory eBidi = CharUnicodeInfo.GetBidiCategory(output, iOutputAfterLastDot);
                    if (eBidi == StrongBidiCategory.StrongRightToLeft)
                    {
                        // It has to be right to left.
                        bRightToLeft = true;
                    }

                    // Check the rest of them to make sure RTL/LTR is consistent
                    for (int iTest = iOutputAfterLastDot; iTest < output.Length; iTest++)
                    {
                        // This might happen if we run into a pair
                        if (char.IsLowSurrogate(output[iTest]))
                            continue;

                        // Check to see if its LTR
                        eBidi = CharUnicodeInfo.GetBidiCategory(output, iTest);
                        if ((bRightToLeft && eBidi == StrongBidiCategory.StrongLeftToRight) ||
                            (!bRightToLeft && eBidi == StrongBidiCategory.StrongRightToLeft))
                            throw new ArgumentException(SR.Argument_IdnBadBidi, nameof(ascii));
                    }

                    // Its also a requirement that the last one be RTL if 1st is RTL
                    if (bRightToLeft && eBidi != StrongBidiCategory.StrongRightToLeft)
                    {
                        // Oops, last wasn't RTL, last should be RTL if first is RTL
                        throw new ArgumentException(SR.Argument_IdnBadBidi, nameof(ascii));
                    }
                }

                // See if this label was too long
                if (iNextDot - iAfterLastDot > c_labelLimit)
                    throw new ArgumentException(SR.Argument_IdnBadLabelSize, nameof(ascii));

                // Done with this segment, add dot if necessary
                if (iNextDot != ascii.Length)
                    output.Append('.');

                iAfterLastDot = iNextDot + 1;
                iOutputAfterLastDot = output.Length;
            }

            // Throw if we're too long
            if (output.Length > c_defaultNameLimit - (IsDot(output[output.Length - 1]) ? 0 : 1))
                throw new ArgumentException(SR.Format(SR.Argument_IdnBadNameSize, c_defaultNameLimit - (IsDot(output[output.Length - 1]) ? 0 : 1)), nameof(ascii));

            // Return our output string
            return output.ToString();
        }

        // DecodeDigit(cp) returns the numeric value of a basic code */
        // point (for use in representing integers) in the range 0 to */
        // c_punycodeBase-1, or <0 if cp is does not represent a value. */

        private static int DecodeDigit(char cp)
        {
            if (cp >= '0' && cp <= '9')
                return cp - '0' + 26;

            // Two flavors for case differences
            if (cp >= 'a' && cp <= 'z')
                return cp - 'a';

            if (cp >= 'A' && cp <= 'Z')
                return cp - 'A';

            // Expected 0-9, A-Z or a-z, everything else is illegal
            throw new ArgumentException(SR.Argument_IdnBadPunycode, nameof(cp));
        }

        private static int Adapt(int delta, int numpoints, bool firsttime)
        {
            uint k;

            delta = firsttime ? delta / c_damp : delta / 2;
            Debug.Assert(numpoints != 0, "[IdnMapping.adapt]Expected non-zero numpoints.");
            delta += delta / numpoints;

            for (k = 0; delta > ((c_punycodeBase - c_tmin) * c_tmax) / 2; k += c_punycodeBase)
            {
                delta /= c_punycodeBase - c_tmin;
            }

            Debug.Assert(delta + c_skew != 0, "[IdnMapping.adapt]Expected non-zero delta+skew.");
            return (int)(k + (c_punycodeBase - c_tmin + 1) * delta / (delta + c_skew));
        }

        /* EncodeBasic(bcp,flag) forces a basic code point to lowercase */
        /* if flag is false, uppercase if flag is true, and returns    */
        /* the resulting code point.  The code point is unchanged if it  */
        /* is caseless.  The behavior is undefined if bcp is not a basic */
        /* code point.                                                   */

        private static char EncodeBasic(char bcp)
        {
            if (HasUpperCaseFlag(bcp))
                bcp += (char)('a' - 'A');

            return bcp;
        }

        // Return whether a punycode code point is flagged as being upper case.
        private static bool HasUpperCaseFlag(char punychar) =>
            punychar >= 'A' && punychar <= 'Z';

        /* EncodeDigit(d,flag) returns the basic code point whose value      */
        /* (when used for representing integers) is d, which needs to be in   */
        /* the range 0 to punycodeBase-1.  The lowercase form is used unless flag is  */
        /* true, in which case the uppercase form is used. */

        private static char EncodeDigit(int d)
        {
            Debug.Assert(d >= 0 && d < c_punycodeBase, "[IdnMapping.encode_digit]Expected 0 <= d < punycodeBase");
            // 26-35 map to ASCII 0-9
            if (d > 25) return (char)(d - 26 + '0');

            // 0-25 map to a-z or A-Z
            return (char)(d + 'a');
        }
    }
}
