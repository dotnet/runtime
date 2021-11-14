// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        /// <summary>Lookup table used for optimizing ASCII when doing set queries.</summary>
        private readonly uint[]?[]? _asciiLookups;

        public RegexFindOptimizations(RegexTree tree, CultureInfo culture)
        {
            _rightToLeft = (tree.Options & RegexOptions.RightToLeft) != 0;
            _minRequiredLength = tree.MinRequiredLength;
            _textInfo = culture.TextInfo;

            // Compute any anchor starting the expression.  If there is one, we won't need to search for anything,
            // as we can just match at that single location.
            LeadingAnchor = RegexPrefixAnalyzer.FindLeadingAnchor(tree);
            if (_rightToLeft)
            {
                // Filter out Bol for RightToLeft, as we don't currently optimize for it.
                LeadingAnchor &= ~RegexPrefixAnalyzer.Bol;
            }
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

            // If there's a leading case-sensitive substring, just use IndexOf and inherit all of its optimizations.
            string caseSensitivePrefix = RegexPrefixAnalyzer.FindCaseSensitivePrefix(tree);
            if (caseSensitivePrefix.Length > 1)
            {
                LeadingCaseSensitivePrefix = caseSensitivePrefix;
                FindMode = _rightToLeft ?
                    FindNextStartingPositionMode.LeadingPrefix_RightToLeft_CaseSensitive :
                    FindNextStartingPositionMode.LeadingPrefix_LeftToRight_CaseSensitive;
                return;
            }

            // At this point there are no fast-searchable anchors or case-sensitive prefixes. We can now analyze the
            // pattern for sets and then use any found sets to determine what kind of search to perform.

            // If we're compiling, then the compilation process already handles sets that reduce to a single literal,
            // so we can simplify and just always go for the sets.
            bool dfa = (tree.Options & RegexOptions.NonBacktracking) != 0;
            bool compiled = (tree.Options & RegexOptions.Compiled) != 0 && !dfa; // for now, we never generate code for NonBacktracking, so treat it as non-compiled
            bool interpreter = !compiled && !dfa;

            // For interpreter, we want to employ optimizations, but we don't want to make construction significantly
            // more expensive; someone who wants to pay to do more work can specify Compiled.  So for the interpreter
            // we focus only on creating a set for the first character.  Same for right-to-left, which is used very
            // rarely and thus we don't need to invest in special-casing it.
            if (_rightToLeft)
            {
                // Determine a set for anything that can possibly start the expression.
                if (RegexPrefixAnalyzer.FindFirstCharClass(tree, culture) is (string CharClass, bool CaseInsensitive) set)
                {
                    // See if the set is limited to holding only a few characters.
                    Span<char> scratch = stackalloc char[5]; // max optimized by IndexOfAny today
                    int scratchCount;
                    char[]? chars = null;
                    if (!RegexCharClass.IsNegated(set.CharClass) &&
                        (scratchCount = RegexCharClass.GetSetChars(set.CharClass, scratch)) > 0)
                    {
                        chars = scratch.Slice(0, scratchCount).ToArray();
                    }

                    if (!compiled &&
                        chars is { Length: 1 })
                    {
                        // The set contains one and only one character, meaning every match starts
                        // with the same literal value (potentially case-insensitive). Search for that.
                        FixedDistanceLiteral = (chars[0], 0);
                        FindMode = (_rightToLeft, set.CaseInsensitive) switch
                        {
                            (false, false) => FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseSensitive,
                            (false, true) => FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseInsensitive,
                            (true, false) => FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseSensitive,
                            (true, true) => FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseInsensitive,
                        };
                    }
                    else
                    {
                        // The set may match multiple characters.  Search for that.
                        FixedDistanceSets = new() { (chars, set.CharClass, 0, set.CaseInsensitive) };
                        FindMode = (_rightToLeft, set.CaseInsensitive) switch
                        {
                            (false, false) => FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseSensitive,
                            (false, true) => FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive,
                            (true, false) => FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseSensitive,
                            (true, true) => FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseInsensitive,
                        };
                        _asciiLookups = new uint[1][];
                    }
                }
                return;
            }

            // We're now left-to-right only and looking for sets.

            // Build up a list of all of the sets that are a fixed distance from the start of the expression.
            List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>? fixedDistanceSets = RegexPrefixAnalyzer.FindFixedDistanceSets(tree, culture, thorough: !interpreter);
            if (fixedDistanceSets is not null)
            {
                Debug.Assert(fixedDistanceSets.Count != 0);

                // Determine whether to do searching based on one or more sets or on a single literal. Compiled engines
                // don't need to special-case literals as they already do codegen to create the optimal lookup based on
                // the set's characteristics.
                if (!compiled &&
                    fixedDistanceSets.Count == 1 &&
                    fixedDistanceSets[0].Chars is { Length: 1 })
                {
                    FixedDistanceLiteral = (fixedDistanceSets[0].Chars![0], fixedDistanceSets[0].Distance);
                    FindMode = fixedDistanceSets[0].CaseInsensitive ?
                        FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseInsensitive :
                        FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseSensitive;
                }
                else
                {
                    // Limit how many sets we use to avoid doing lots of unnecessary work.  The list was already
                    // sorted from best to worst, so just keep the first ones up to our limit.
                    const int MaxSetsToUse = 3; // arbitrary tuned limit
                    if (fixedDistanceSets.Count > MaxSetsToUse)
                    {
                        fixedDistanceSets.RemoveRange(MaxSetsToUse, fixedDistanceSets.Count - MaxSetsToUse);
                    }

                    // Store the sets, and compute which mode to use.
                    FixedDistanceSets = fixedDistanceSets;
                    FindMode = (fixedDistanceSets.Count == 1 && fixedDistanceSets[0].Distance == 0, fixedDistanceSets[0].CaseInsensitive) switch
                    {
                        (true, true) => FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive,
                        (true, false) => FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseSensitive,
                        (false, true) => FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive,
                        (false, false) => FindNextStartingPositionMode.FixedSets_LeftToRight_CaseSensitive,
                    };
                    _asciiLookups = new uint[fixedDistanceSets.Count][];
                }
                return;
            }
        }

        /// <summary>Gets the selected mode for performing the next <see cref="TryFindNextStartingPosition"/> operation</summary>
        public FindNextStartingPositionMode FindMode { get; } = FindNextStartingPositionMode.NoSearch;

        /// <summary>Gets the leading anchor, if one exists (RegexPrefixAnalyzer.Bol, etc).</summary>
        public int LeadingAnchor { get; }

        /// <summary>Gets the leading prefix.  May be an empty string.</summary>
        public string LeadingCaseSensitivePrefix { get; } = string.Empty;

        /// <summary>When in fixed distance literal mode, gets the literal and how far it is from the start of the pattern.</summary>
        public (char Literal, int Distance) FixedDistanceLiteral { get; }

        /// <summary>When in fixed distance set mode, gets the set and how far it is from the start of the pattern.</summary>
        /// <remarks>The case-insensitivity of the 0th entry will always match the mode selected, but subsequent entries may not.</remarks>
        public List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>? FixedDistanceSets { get; }

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

            // Optimize the handling of a Beginning-Of-Line (BOL) anchor (only for left-to-right).  BOL is special, in that unlike
            // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
            // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
            // to boost our position to the next line, and then continue normally with any searches.
            if (LeadingAnchor == RegexPrefixAnalyzer.Bol)
            {
                // If we're not currently positioned at the beginning of a line (either
                // the beginning of the string or just after a line feed), find the next
                // newline and position just after it.
                Debug.Assert(!_rightToLeft);
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
                // There's an anchor.  For some, we can simply compare against the current position.
                // For others, we can jump to the relevant location.

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning:
                    if (pos > beginning)
                    {
                        pos = end;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start:
                    if (pos > start)
                    {
                        pos = end;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ:
                    if (pos < end - 1)
                    {
                        pos = end - 1;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End:
                    if (pos < end)
                    {
                        pos = end;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning:
                    if (pos > beginning)
                    {
                        pos = beginning;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start:
                    if (pos < start)
                    {
                        pos = beginning;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ:
                    if (pos < end - 1 || (pos == end - 1 && text[pos] != '\n'))
                    {
                        pos = beginning;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End:
                    if (pos < end)
                    {
                        pos = beginning;
                        return false;
                    }
                    return true;

                // There's a case-sensitive prefix.  Search for it with ordinal IndexOf.

                case FindNextStartingPositionMode.LeadingPrefix_LeftToRight_CaseSensitive:
                    {
                        int i = text.AsSpan(pos, end - pos).IndexOf(LeadingCaseSensitivePrefix.AsSpan());
                        if (i >= 0)
                        {
                            pos += i;
                            return true;
                        }

                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingPrefix_RightToLeft_CaseSensitive:
                    {
                        int i = text.AsSpan(beginning, pos - beginning).LastIndexOf(LeadingCaseSensitivePrefix.AsSpan());
                        if (i >= 0)
                        {
                            pos = beginning + i + LeadingCaseSensitivePrefix.Length;
                            return true;
                        }

                        pos = beginning;
                        return false;
                    }

                // There's a literal at the beginning of the pattern.  Search for it.

                case FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseSensitive:
                    {
                        int i = text.AsSpan(beginning, pos - beginning).LastIndexOf(FixedDistanceLiteral.Literal);
                        if (i >= 0)
                        {
                            pos = beginning + i + 1;
                            return true;
                        }

                        pos = beginning;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseInsensitive:
                    {
                        char ch = FixedDistanceLiteral.Literal;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (ti.ToLower(span[i]) == ch)
                            {
                                pos = beginning + i + 1;
                                return true;
                            }
                        }

                        pos = beginning;
                        return false;
                    }

                // There's a set at the beginning of the pattern.  Search for it.

                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseSensitive:
                    {
                        (char[]? chars, string set, _, _) = FixedDistanceSets![0];

                        ReadOnlySpan<char> span = text.AsSpan(pos, end - pos);
                        if (chars is not null)
                        {
                            int i = span.IndexOfAny(chars);
                            if (i >= 0)
                            {
                                pos += i;
                                return true;
                            }
                        }
                        else
                        {
                            ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                            for (int i = 0; i < span.Length; i++)
                            {
                                if (RegexCharClass.CharInClass(span[i], set, ref startingAsciiLookup))
                                {
                                    pos += i;
                                    return true;
                                }
                            }
                        }

                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive:
                    {
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                        string set = FixedDistanceSets![0].Set;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = text.AsSpan(pos, end - pos);
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref startingAsciiLookup))
                            {
                                pos += i;
                                return true;
                            }
                        }

                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseSensitive:
                    {
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                        string set = FixedDistanceSets![0].Set;

                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (RegexCharClass.CharInClass(span[i], set, ref startingAsciiLookup))
                            {
                                pos = beginning + i + 1;
                                return true;
                            }
                        }

                        pos = beginning;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseInsensitive:
                    {
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                        string set = FixedDistanceSets![0].Set;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = text.AsSpan(beginning, pos - beginning);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref startingAsciiLookup))
                            {
                                pos = beginning + i + 1;
                                return true;
                            }
                        }

                        pos = beginning;
                        return false;
                    }

                // There's a literal at a fixed offset from the beginning of the pattern.  Search for it.

                case FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseSensitive:
                    {
                        Debug.Assert(FixedDistanceLiteral.Distance <= _minRequiredLength);

                        int i = text.AsSpan(pos + FixedDistanceLiteral.Distance, end - pos - FixedDistanceLiteral.Distance).IndexOf(FixedDistanceLiteral.Literal);
                        if (i >= 0)
                        {
                            pos += i;
                            return true;
                        }

                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseInsensitive:
                    {
                        Debug.Assert(FixedDistanceLiteral.Distance <= _minRequiredLength);

                        char ch = FixedDistanceLiteral.Literal;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = text.AsSpan(pos + FixedDistanceLiteral.Distance, end - pos - FixedDistanceLiteral.Distance);
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (ti.ToLower(span[i]) == ch)
                            {
                                pos += i;
                                return true;
                            }
                        }

                        pos = end;
                        return false;
                    }

                // There are one or more sets at fixed offsets from the start of the pattern.

                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseSensitive:
                    {
                        List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)> sets = FixedDistanceSets!;
                        (char[]? primaryChars, string primarySet, int primaryDistance, _) = sets[0];
                        int endMinusRequiredLength = end - Math.Max(1, _minRequiredLength);

                        if (primaryChars is not null)
                        {
                            for (int inputPosition = pos; inputPosition <= endMinusRequiredLength; inputPosition++)
                            {
                                int offset = inputPosition + primaryDistance;
                                int index = text.IndexOfAny(primaryChars, offset, end - offset);
                                if (index < 0)
                                {
                                    break;
                                }

                                inputPosition = index - primaryDistance;
                                if (inputPosition > endMinusRequiredLength)
                                {
                                    break;
                                }

                                for (int i = 1; i < sets.Count; i++)
                                {
                                    (_, string nextSet, int nextDistance, bool nextCaseInsensitive) = sets[i];
                                    char c = text[inputPosition + nextDistance];
                                    if (!RegexCharClass.CharInClass(nextCaseInsensitive ? _textInfo.ToLower(c) : c, nextSet, ref _asciiLookups![i]))
                                    {
                                        goto Bumpalong;
                                    }
                                }

                                pos = inputPosition;
                                return true;

                            Bumpalong:;
                            }
                        }
                        else
                        {
                            ref uint[]? startingAsciiLookup = ref _asciiLookups![0];

                            for (int inputPosition = pos; inputPosition <= endMinusRequiredLength; inputPosition++)
                            {
                                char c = text[inputPosition + primaryDistance];
                                if (!RegexCharClass.CharInClass(c, primarySet, ref startingAsciiLookup))
                                {
                                    goto Bumpalong;
                                }

                                for (int i = 1; i < sets.Count; i++)
                                {
                                    (_, string nextSet, int nextDistance, bool nextCaseInsensitive) = sets[i];
                                    c = text[inputPosition + nextDistance];
                                    if (!RegexCharClass.CharInClass(nextCaseInsensitive ? _textInfo.ToLower(c) : c, nextSet, ref _asciiLookups![i]))
                                    {
                                        goto Bumpalong;
                                    }
                                }

                                pos = inputPosition;
                                return true;

                            Bumpalong:;
                            }
                        }

                        pos = end;
                        return false;
                    }

                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive:
                    {
                        List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)> sets = FixedDistanceSets!;
                        (_, string primarySet, int primaryDistance, _) = sets[0];

                        int endMinusRequiredLength = end - Math.Max(1, _minRequiredLength);
                        TextInfo ti = _textInfo;
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];

                        for (int inputPosition = pos; inputPosition <= endMinusRequiredLength; inputPosition++)
                        {
                            char c = text[inputPosition + primaryDistance];
                            if (!RegexCharClass.CharInClass(ti.ToLower(c), primarySet, ref startingAsciiLookup))
                            {
                                goto Bumpalong;
                            }

                            for (int i = 1; i < sets.Count; i++)
                            {
                                (_, string nextSet, int nextDistance, bool nextCaseInsensitive) = sets[i];
                                c = text[inputPosition + nextDistance];
                                if (!RegexCharClass.CharInClass(nextCaseInsensitive ? _textInfo.ToLower(c) : c, nextSet, ref _asciiLookups![i]))
                                {
                                    goto Bumpalong;
                                }
                            }

                            pos = inputPosition;
                            return true;

                        Bumpalong:;
                        }

                        pos = end;
                        return false;
                    }

                // Nothing special to look for.  Just return true indicating this is a valid position to try to match.

                default:
                    Debug.Assert(FindMode == FindNextStartingPositionMode.NoSearch);
                    return true;
            }
        }
    }

    /// <summary>Mode to use for searching for the next location of a possible match.</summary>
    internal enum FindNextStartingPositionMode
    {
        /// <summary>A "beginning" anchor at the beginning of the pattern.</summary>
        LeadingAnchor_LeftToRight_Beginning,
        /// <summary>A "start" anchor at the beginning of the pattern.</summary>
        LeadingAnchor_LeftToRight_Start,
        /// <summary>An "endz" anchor at the beginning of the pattern.  This is rare.</summary>
        LeadingAnchor_LeftToRight_EndZ,
        /// <summary>An "end" anchor at the beginning of the pattern.  This is rare.</summary>
        LeadingAnchor_LeftToRight_End,

        /// <summary>A "beginning" anchor at the beginning of the right-to-left pattern.</summary>
        LeadingAnchor_RightToLeft_Beginning,
        /// <summary>A "start" anchor at the beginning of the right-to-left pattern.</summary>
        LeadingAnchor_RightToLeft_Start,
        /// <summary>An "endz" anchor at the beginning of the right-to-left pattern.  This is rare.</summary>
        LeadingAnchor_RightToLeft_EndZ,
        /// <summary>An "end" anchor at the beginning of the right-to-left pattern.  This is rare.</summary>
        LeadingAnchor_RightToLeft_End,

        /// <summary>A case-sensitive multi-character substring at the beginning of the pattern.</summary>
        LeadingPrefix_LeftToRight_CaseSensitive,
        /// <summary>A case-sensitive multi-character substring at the beginning of the right-to-left pattern.</summary>
        LeadingPrefix_RightToLeft_CaseSensitive,

        /// <summary>A case-sensitive set starting the pattern.</summary>
        LeadingSet_LeftToRight_CaseSensitive,
        /// <summary>A case-insensitive set starting the pattern.</summary>
        LeadingSet_LeftToRight_CaseInsensitive,
        /// <summary>A case-sensitive set starting the right-to-left pattern.</summary>
        LeadingSet_RightToLeft_CaseSensitive,
        /// <summary>A case-insensitive set starting the right-to-left pattern.</summary>
        LeadingSet_RightToLeft_CaseInsensitive,

        /// <summary>A case-sensitive single character at a fixed distance from the start of the right-to-left pattern.</summary>
        LeadingLiteral_RightToLeft_CaseSensitive,
        /// <summary>A case-insensitive single character at a fixed distance from the start of the right-to-left pattern.</summary>
        LeadingLiteral_RightToLeft_CaseInsensitive,

        /// <summary>A case-sensitive single character at a fixed distance from the start of the pattern.</summary>
        FixedLiteral_LeftToRight_CaseSensitive,
        /// <summary>A case-insensitive single character at a fixed distance from the start of the pattern.</summary>
        FixedLiteral_LeftToRight_CaseInsensitive,

        /// <summary>One or more sets at a fixed distance from the start of the pattern.  At least the first set is case-sensitive.</summary>
        FixedSets_LeftToRight_CaseSensitive,
        /// <summary>One or more sets at a fixed distance from the start of the pattern.  At least the first set is case-insensitive.</summary>
        FixedSets_LeftToRight_CaseInsensitive,

        /// <summary>Nothing to search for. Nop.</summary>
        NoSearch,
    }
}
