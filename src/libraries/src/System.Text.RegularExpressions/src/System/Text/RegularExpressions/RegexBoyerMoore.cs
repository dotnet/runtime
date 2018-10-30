// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The RegexBoyerMoore object precomputes the Boyer-Moore
// tables for fast string scanning. These tables allow
// you to scan for the first occurrence of a string within
// a large body of text without examining every character.
// The performance of the heuristic depends on the actual
// string and the text being searched, but usually, the longer
// the string that is being searched for, the fewer characters
// need to be examined.

using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexBoyerMoore
    {
        public readonly int[] Positive;
        public readonly int[] NegativeASCII;
        public readonly int[][] NegativeUnicode;
        public readonly string Pattern;
        public readonly int LowASCII;
        public readonly int HighASCII;
        public readonly bool RightToLeft;
        public readonly bool CaseInsensitive;
        private readonly CultureInfo _culture;

        /// <summary>
        /// Constructs a Boyer-Moore state machine for searching for the string
        /// pattern. The string must not be zero-length.
        /// </summary>
        public RegexBoyerMoore(string pattern, bool caseInsensitive, bool rightToLeft, CultureInfo culture)
        {
            // Sorry, you just can't use Boyer-Moore to find an empty pattern.
            // We're doing this for your own protection. (Really, for speed.)
            Debug.Assert(pattern.Length != 0, "RegexBoyerMoore called with an empty string. This is bad for perf");
            Debug.Assert(!caseInsensitive || pattern.ToLower(culture) == pattern, "RegexBoyerMoore called with a pattern which is not lowercased with caseInsensitive true.");

            Pattern = pattern;
            RightToLeft = rightToLeft;
            CaseInsensitive = caseInsensitive;
            _culture = culture;

            int beforefirst;
            int last;
            int bump;

            if (!rightToLeft)
            {
                beforefirst = -1;
                last = pattern.Length - 1;
                bump = 1;
            }
            else
            {
                beforefirst = pattern.Length;
                last = 0;
                bump = -1;
            }

            // PART I - the good-suffix shift table
            //
            // compute the positive requirement:
            // if char "i" is the first one from the right that doesn't match,
            // then we know the matcher can advance by _positive[i].
            //
            // This algorithm is a simplified variant of the standard
            // Boyer-Moore good suffix calculation.

            Positive = new int[pattern.Length];

            int examine = last;
            char ch = pattern[examine];
            Positive[examine] = bump;
            examine -= bump;
            int scan;
            int match;

            for (; ;)
            {
                // find an internal char (examine) that matches the tail

                for (; ;)
                {
                    if (examine == beforefirst)
                        goto OuterloopBreak;
                    if (pattern[examine] == ch)
                        break;
                    examine -= bump;
                }

                match = last;
                scan = examine;

                // find the length of the match

                for (; ;)
                {
                    if (scan == beforefirst || pattern[match] != pattern[scan])
                    {
                        // at the end of the match, note the difference in _positive
                        // this is not the length of the match, but the distance from the internal match
                        // to the tail suffix.
                        if (Positive[match] == 0)
                            Positive[match] = match - scan;

                        // System.Diagnostics.Debug.WriteLine("Set positive[" + match + "] to " + (match - scan));

                        break;
                    }

                    scan -= bump;
                    match -= bump;
                }

                examine -= bump;
            }

        OuterloopBreak:

            match = last - bump;

            // scan for the chars for which there are no shifts that yield a different candidate


            // The inside of the if statement used to say
            // "_positive[match] = last - beforefirst;"
            // This is slightly less aggressive in how much we skip, but at worst it
            // should mean a little more work rather than skipping a potential match.
            while (match != beforefirst)
            {
                if (Positive[match] == 0)
                    Positive[match] = bump;

                match -= bump;
            }

            // PART II - the bad-character shift table
            //
            // compute the negative requirement:
            // if char "ch" is the reject character when testing position "i",
            // we can slide up by _negative[ch];
            // (_negative[ch] = str.Length - 1 - str.LastIndexOf(ch))
            //
            // the lookup table is divided into ASCII and Unicode portions;
            // only those parts of the Unicode 16-bit code set that actually
            // appear in the string are in the table. (Maximum size with
            // Unicode is 65K; ASCII only case is 512 bytes.)

            NegativeASCII = new int[128];

            for (int i = 0; i < 128; i++)
                NegativeASCII[i] = last - beforefirst;

            LowASCII = 127;
            HighASCII = 0;

            for (examine = last; examine != beforefirst; examine -= bump)
            {
                ch = pattern[examine];

                if (ch < 128)
                {
                    if (LowASCII > ch)
                        LowASCII = ch;

                    if (HighASCII < ch)
                        HighASCII = ch;

                    if (NegativeASCII[ch] == last - beforefirst)
                        NegativeASCII[ch] = last - examine;
                }
                else
                {
                    int i = ch >> 8;
                    int j = ch & 0xFF;

                    if (NegativeUnicode == null)
                    {
                        NegativeUnicode = new int[256][];
                    }

                    if (NegativeUnicode[i] == null)
                    {
                        int[] newarray = new int[256];

                        for (int k = 0; k < newarray.Length; k++)
                            newarray[k] = last - beforefirst;

                        if (i == 0)
                        {
                            Array.Copy(NegativeASCII, 0, newarray, 0, 128);
                            NegativeASCII = newarray;
                        }

                        NegativeUnicode[i] = newarray;
                    }

                    if (NegativeUnicode[i][j] == last - beforefirst)
                        NegativeUnicode[i][j] = last - examine;
                }
            }
        }

        private bool MatchPattern(string text, int index)
        {
            if (CaseInsensitive)
            {
                if (text.Length - index < Pattern.Length)
                {
                    return false;
                }

                return (0 == string.Compare(Pattern, 0, text, index, Pattern.Length, CaseInsensitive, _culture));
            }
            else
            {
                return (0 == string.CompareOrdinal(Pattern, 0, text, index, Pattern.Length));
            }
        }

        /// <summary>
        /// When a regex is anchored, we can do a quick IsMatch test instead of a Scan
        /// </summary>
        public bool IsMatch(string text, int index, int beglimit, int endlimit)
        {
            if (!RightToLeft)
            {
                if (index < beglimit || endlimit - index < Pattern.Length)
                    return false;

                return MatchPattern(text, index);
            }
            else
            {
                if (index > endlimit || index - beglimit < Pattern.Length)
                    return false;

                return MatchPattern(text, index - Pattern.Length);
            }
        }

        /// <summary>
        /// Scan uses the Boyer-Moore algorithm to find the first occurrence
        /// of the specified string within text, beginning at index, and
        /// constrained within beglimit and endlimit.
        ///
        /// The direction and case-sensitivity of the match is determined
        /// by the arguments to the RegexBoyerMoore constructor.
        /// </summary>
        public int Scan(string text, int index, int beglimit, int endlimit)
        {
            int defadv;
            int test;
            int startmatch;
            int endmatch;
            int bump;

            if (!RightToLeft)
            {
                defadv = Pattern.Length;
                startmatch = Pattern.Length - 1;
                endmatch = 0;
                test = index + defadv - 1;
                bump = 1;
            }
            else
            {
                defadv = -Pattern.Length;
                startmatch = 0;
                endmatch = -defadv - 1;
                test = index + defadv;
                bump = -1;
            }

            char chMatch = Pattern[startmatch];
            char chTest;
            int test2;
            int match;
            int advance;
            int[] unicodeLookup;

            for (; ;)
            {
                if (test >= endlimit || test < beglimit)
                    return -1;

                chTest = text[test];

                if (CaseInsensitive)
                    chTest = _culture.TextInfo.ToLower(chTest);

                if (chTest != chMatch)
                {
                    if (chTest < 128)
                        advance = NegativeASCII[chTest];
                    else if (null != NegativeUnicode && (null != (unicodeLookup = NegativeUnicode[chTest >> 8])))
                        advance = unicodeLookup[chTest & 0xFF];
                    else
                        advance = defadv;

                    test += advance;
                }
                else
                { // if (chTest == chMatch)
                    test2 = test;
                    match = startmatch;

                    for (; ;)
                    {
                        if (match == endmatch)
                            return (RightToLeft ? test2 + 1 : test2);

                        match -= bump;
                        test2 -= bump;

                        chTest = text[test2];

                        if (CaseInsensitive)
                            chTest = _culture.TextInfo.ToLower(chTest);

                        if (chTest != Pattern[match])
                        {
                            advance = Positive[match];
                            if ((chTest & 0xFF80) == 0)
                                test2 = (match - startmatch) + NegativeASCII[chTest];
                            else if (null != NegativeUnicode && (null != (unicodeLookup = NegativeUnicode[chTest >> 8])))
                                test2 = (match - startmatch) + unicodeLookup[chTest & 0xFF];
                            else
                            {
                                test += advance;
                                break;
                            }

                            if (RightToLeft ? test2 < advance : test2 > advance)
                                advance = test2;

                            test += advance;
                            break;
                        }
                    }
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Used when dumping for debugging.
        /// </summary>
        public override string ToString() => Pattern;

        public string Dump(string indent)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(indent + "BM Pattern: " + Pattern + "\n");
            sb.Append(indent + "Positive: ");
            for (int i = 0; i < Positive.Length; i++)
            {
                sb.Append(Positive[i].ToString(CultureInfo.InvariantCulture) + " ");
            }
            sb.Append("\n");

            if (NegativeASCII != null)
            {
                sb.Append(indent + "Negative table\n");
                for (int i = 0; i < NegativeASCII.Length; i++)
                {
                    if (NegativeASCII[i] != Pattern.Length)
                    {
                        sb.Append(indent + "  " + Regex.Escape(Convert.ToString((char)i, CultureInfo.InvariantCulture)) + " " + NegativeASCII[i].ToString(CultureInfo.InvariantCulture) + "\n");
                    }
                }
            }

            return sb.ToString();
        }
#endif
    }
}
