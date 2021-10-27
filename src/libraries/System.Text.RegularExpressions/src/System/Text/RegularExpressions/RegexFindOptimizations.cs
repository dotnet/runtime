// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>Contains state and provides operations related to finding the next location a match could possibly begin.</summary>
    internal sealed class RegexFindOptimizations
    {
        /// <summary>The minimum required length an input need be to match the pattern.  May be 0.</summary>
        private readonly int _minRequiredLength;
        /// <summary>True if the input should be processed right-to-left rather than left-to-right.</summary>
        private readonly bool _rightToLeft;
        /// <summary>Provides the ToLower routine for lowercasing characters.</summary>
        private readonly TextInfo _textInfo;

        public RegexFindOptimizations(RegexTree tree, CultureInfo culture)
        {
            _rightToLeft = (tree.Options & RegexOptions.RightToLeft) != 0;
            _minRequiredLength = tree.MinRequiredLength;
            _textInfo = culture.TextInfo;

            // Compute any anchor starting the expression.  If there is one, we won't need to search for anything.
            LeadingAnchor = RegexPrefixAnalyzer.FindLeadingAnchor(tree);
            if ((LeadingAnchor & (RegexPrefixAnalyzer.Beginning | RegexPrefixAnalyzer.Start | RegexPrefixAnalyzer.EndZ | RegexPrefixAnalyzer.End)) != 0)
            {
                FindMode = (LeadingAnchor, _rightToLeft) switch
                {
                    (RegexPrefixAnalyzer.Beginning, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning,
                    (RegexPrefixAnalyzer.Beginning, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning,
                    (RegexPrefixAnalyzer.Start, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start,
                    (RegexPrefixAnalyzer.Start, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start,
                    (RegexPrefixAnalyzer.End, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End,
                    (RegexPrefixAnalyzer.End, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End,
                    (_, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ,
                    (_, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ,
                };
                return;
            }

            bool compiled = (tree.Options & RegexOptions.Compiled) != 0;

            // If there's a leading substring, use it either for an IndexOf or Boyer-Moore optimization.
            (string Value, bool CaseInsensitive) prefix = RegexPrefixAnalyzer.ComputeLeadingSubstring(tree);
            if (prefix.Value.Length > 1) // if it's <= 1, perf is better using leadingCharClasses
            {
                if (!_rightToLeft && (!prefix.CaseInsensitive || !RegexCharClass.ParticipatesInCaseConversion(prefix.Value)))
                {
                    LeadingPrefix = prefix.Value;
                    FindMode = FindNextStartingPositionMode.IndexOf;
                    return;
                }

                if (prefix.Value.Length <= RegexBoyerMoore.MaxLimit &&
                    (!compiled || AsciiOnly(prefix.Value))) // compilation won't use Boyer-Moore if it has a negative Unicode table
                {
                    // Compute a Boyer-Moore prefix if we find a single string of sufficient length that always begins the expression.
                    BoyerMoorePrefix = new RegexBoyerMoore(prefix.Value, prefix.CaseInsensitive, (tree.Options & RegexOptions.RightToLeft) != 0, culture);
                    FindMode = FindNextStartingPositionMode.BoyerMoore;
                    return;
                }
            }

            // There were no anchors and no prefixes.  There might be individual characters or character classes we can look for.
            // First we employ a less aggressive but more valuable computation to see if we can find sets for each of the first N
            // characters in the string.  If that's unsuccessful, we employ a more aggressive check to compute a set for just
            // the first character in the string.

            if (compiled) // currently not utilized by the interpreter
            {
                LeadingCharClasses = RegexPrefixAnalyzer.ComputeMultipleCharClasses(tree, culture, maxChars: 5); // limit of 5 is based on experimentation and can be tweaked as needed
            }

            if (LeadingCharClasses is null)
            {
                LeadingCharClasses = RegexPrefixAnalyzer.ComputeFirstCharClass(tree, culture);
            }

            if (LeadingCharClasses is not null)
            {
                (string charClass, bool caseInsensitive) = LeadingCharClasses[0];
                bool isSet = !RegexCharClass.IsSingleton(charClass);
                FindMode = (_rightToLeft, caseInsensitive, isSet) switch
                {
                    (false, false, false) => FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseSensitive_Singleton,
                    (false, false, true) => FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseSensitive_Set,
                    (false, true, false) => FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseInsensitive_Singleton,
                    (false, true, true) => FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseInsensitive_Set,
                    (true, false, false) => FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseSensitive_Singleton,
                    (true, false, true) => FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseSensitive_Set,
                    (true, true, false) => FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseInsensitive_Singleton,
                    (true, true, true) => FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseInsensitive_Set,
                };
            }
        }

        /// <summary>Gets the selected mode for performing the next <see cref="TryFindNextStartingPosition"/> operation</summary>
        public FindNextStartingPositionMode FindMode { get; } = FindNextStartingPositionMode.NoSearch;

        /// <summary>
        /// the set of candidate first characters, if available.  Each entry corresponds to the next char in the input.
        /// </summary>
        public (string CharClass, bool CaseInsensitive)[]? LeadingCharClasses { get; }

        /// <summary>
        /// the ASCII lookup table optimization for LeadingCharClasses[0], if it exists; only used by the interpreter
        /// </summary>
        public int[]? LeadingCharClassAsciiLookup;

        public string LeadingPrefix { get; } = string.Empty;

        /// <summary>
        /// the fixed prefix string as a Boyer-Moore machine, if available
        /// </summary>
        public RegexBoyerMoore? BoyerMoorePrefix { get; }

        /// <summary>
        /// the leading anchor, if one exists (RegexPrefixAnalyzer.Bol, etc)
        /// </summary>
        public int LeadingAnchor { get; }

        /// <summary>Try to advance to the next starting position that might be a location for a match.</summary>
        /// <param name="text">The text to search.</param>
        /// <param name="pos">The position in <paramref name="text"/>.  This is updated with the found position.</param>
        /// <param name="beginning">The index in <paramref name="text"/> to consider the beginning for beginning anchor purposes.</param>
        /// <param name="start">The index in <paramref name="text"/> to consider the start for start anchor purposes.</param>
        /// <param name="end">The index in <paramref name="text"/> to consider the non-inclusive end of the string.</param>
        /// <returns>true if a position to attempt a match was found; false if none was found.</returns>
        public bool TryFindNextStartingPosition(string text, ref int pos, int beginning, int start, int end)
        {
            // Return early if we know there's not enough input left to match.
            if (!_rightToLeft)
            {
                if (pos > end - _minRequiredLength)
                {
                    pos = end;
                    return false;
                }
            }
            else
            {
                if (pos - _minRequiredLength < beginning)
                {
                    pos = beginning;
                    return false;
                }
            }

            // Optimize the handling of a Beginning-Of-Line (BOL) anchor.  BOL is special, in that unlike
            // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
            // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
            // to boost our position to the next line, and then continue normally with any Boyer-Moore or
            // leading char class searches.
            if (LeadingAnchor == RegexPrefixAnalyzer.Bol &&
                !_rightToLeft) // don't bother customizing this optimization for the very niche RTL + Multiline case
            {
                // If we're not currently positioned at the beginning of a line (either
                // the beginning of the string or just after a line feed), find the next
                // newline and position just after it.
                if (pos > beginning && text[pos - 1] != '\n')
                {
                    int newline = text.IndexOf('\n', pos);
                    if (newline == -1 || newline + 1 > end)
                    {
                        pos = end;
                        return false;
                    }

                    pos = newline + 1;
                }
            }

            switch (FindMode)
            {
                // If the pattern is anchored, we can update our position appropriately and return immediately.
                // If there's a Boyer-Moore prefix, we can also validate it.

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning:
                    if (pos > beginning)
                    {
                        pos = end;
                        return false;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start:
                    if (pos > start)
                    {
                        pos = end;
                        return false;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ:
                    if (pos < end - 1)
                    {
                        pos = end - 1;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End:
                    if (pos < end)
                    {
                        pos = end;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning:
                    if (pos > beginning)
                    {
                        pos = beginning;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start:
                    if (pos < start)
                    {
                        pos = beginning;
                        return false;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ:
                    if (pos < end - 1 || (pos == end - 1 && text[pos] != '\n'))
                    {
                        pos = beginning;
                        return false;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End:
                    if (pos < end)
                    {
                        pos = beginning;
                        return false;
                    }
                    return NoPrefixOrPrefixMatches(text, pos, beginning, end);

                // There was a prefix.  Scan for it.

                case FindNextStartingPositionMode.IndexOf:
                    {
                        int i = text.AsSpan(pos, end - pos).IndexOf(LeadingPrefix.AsSpan());
                        if (i >= 0)
                        {
                            pos += i;
                            return true;
                        }
                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.BoyerMoore:
                    pos = BoyerMoorePrefix!.Scan(text, pos, beginning, end);
                    if (pos >= 0)
                    {
                        return true;
                    }
                    pos = _rightToLeft ? beginning : end;
                    return false;

                // There's a leading character class. Search for it.

                case FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseSensitive_Singleton:
                    {
                        ReadOnlySpan<char> span = text.AsSpan(pos, end - pos);
                        int i = span.IndexOf(RegexCharClass.SingletonChar(LeadingCharClasses![0].CharClass));
                        if (i >= 0)
                        {
                            pos += i;
                            return true;
                        }
                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseSensitive_Set:
                    {
                        string set = LeadingCharClasses![0].CharClass;
                        ReadOnlySpan<char> span = text.AsSpan(pos, end - pos);
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (RegexCharClass.CharInClass(span[i], set, ref LeadingCharClassAsciiLookup))
                            {
                                pos += i;
                                return true;
                            }
                        }
                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseInsensitive_Singleton:
                    {
                        char ch = RegexCharClass.SingletonChar(LeadingCharClasses![0].CharClass);
                        TextInfo ti = _textInfo;
                        ReadOnlySpan<char> span = text.AsSpan(pos, end - pos);
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (ch == ti.ToLower(span[i]))
                            {
                                pos += i;
                                return true;
                            }
                        }
                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_LeftToRight_CaseInsensitive_Set:
                    {
                        string set = LeadingCharClasses![0].CharClass;
                        ReadOnlySpan<char> span = text.AsSpan(pos, end - pos);
                        TextInfo ti = _textInfo;
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref LeadingCharClassAsciiLookup))
                            {
                                pos += i;
                                return true;
                            }
                        }
                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseSensitive_Singleton:
                    {
                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        int i = span.LastIndexOf(RegexCharClass.SingletonChar(LeadingCharClasses![0].CharClass));
                        if (i >= 0)
                        {
                            pos = beginning + i + 1;
                            return true;
                        }
                        pos = beginning;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseSensitive_Set:
                    {
                        string set = LeadingCharClasses![0].CharClass;
                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (RegexCharClass.CharInClass(span[i], set, ref LeadingCharClassAsciiLookup))
                            {
                                pos = beginning + i + 1;
                                return true;
                            }
                        }
                        pos = beginning;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseInsensitive_Singleton:
                    {
                        char ch = RegexCharClass.SingletonChar(LeadingCharClasses![0].CharClass);
                        TextInfo ti = _textInfo;
                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (ch == ti.ToLower(span[i]))
                            {
                                pos = beginning + i + 1;
                                return true;
                            }
                        }
                        pos = beginning;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingCharClass_RightToLeft_CaseInsensitive_Set:
                    {
                        string set = LeadingCharClasses![0].CharClass;
                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        TextInfo ti = _textInfo;
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref LeadingCharClassAsciiLookup))
                            {
                                pos = beginning + i + 1;
                                return true;
                            }
                        }
                        pos = beginning;
                        return false;
                    }

                // Nothing special to look for.  Just return true indicating this is a valid position to try to match.

                default:
                    Debug.Assert(FindMode == FindNextStartingPositionMode.NoSearch);
                    return true;
            }
        }

        private bool NoPrefixOrPrefixMatches(string runtext, int runtextpos, int runtextbeg, int runtextend) =>
            BoyerMoorePrefix is not RegexBoyerMoore rbm ||
            rbm.IsMatch(runtext, runtextpos, runtextbeg, runtextend);

        private static bool AsciiOnly(string s)
        {
            // TODO: https://github.com/dotnet/runtime/issues/28230
            // Use GetIndexOfFirstNonAsciiChar when it's available

            foreach (char c in s)
            {
                if (c >= 0x80)
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal enum FindNextStartingPositionMode
    {
        LeadingAnchor_LeftToRight_Beginning,
        LeadingAnchor_LeftToRight_Start,
        LeadingAnchor_LeftToRight_EndZ,
        LeadingAnchor_LeftToRight_End,

        LeadingAnchor_RightToLeft_Beginning,
        LeadingAnchor_RightToLeft_Start,
        LeadingAnchor_RightToLeft_EndZ,
        LeadingAnchor_RightToLeft_End,

        IndexOf,
        BoyerMoore,

        LeadingCharClass_LeftToRight_CaseSensitive_Singleton,
        LeadingCharClass_LeftToRight_CaseSensitive_Set,
        LeadingCharClass_LeftToRight_CaseInsensitive_Singleton,
        LeadingCharClass_LeftToRight_CaseInsensitive_Set,

        LeadingCharClass_RightToLeft_CaseSensitive_Singleton,
        LeadingCharClass_RightToLeft_CaseSensitive_Set,
        LeadingCharClass_RightToLeft_CaseInsensitive_Singleton,
        LeadingCharClass_RightToLeft_CaseInsensitive_Set,

        NoSearch,
    }
}
