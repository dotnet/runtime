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
        /// <summary>True if the input should be processed right-to-left rather than left-to-right.</summary>
        private readonly bool _rightToLeft;
        /// <summary>Provides the ToLower routine for lowercasing characters.</summary>
        private readonly TextInfo _textInfo;
        /// <summary>Lookup table used for optimizing ASCII when doing set queries.</summary>
        private readonly uint[]?[]? _asciiLookups;

        public RegexFindOptimizations(RegexNode root, RegexOptions options, CultureInfo culture)
        {
            _rightToLeft = (options & RegexOptions.RightToLeft) != 0;
            _textInfo = culture.TextInfo;

            MinRequiredLength = root.ComputeMinLength();

            // Compute any anchor starting the expression.  If there is one, we won't need to search for anything,
            // as we can just match at that single location.
            LeadingAnchor = RegexPrefixAnalyzer.FindLeadingAnchor(root);
            if (_rightToLeft && LeadingAnchor == RegexNodeKind.Bol)
            {
                // Filter out Bol for RightToLeft, as we don't currently optimize for it.
                LeadingAnchor = RegexNodeKind.Unknown;
            }
            if (LeadingAnchor is RegexNodeKind.Beginning or RegexNodeKind.Start or RegexNodeKind.EndZ or RegexNodeKind.End)
            {
                FindMode = (LeadingAnchor, _rightToLeft) switch
                {
                    (RegexNodeKind.Beginning, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning,
                    (RegexNodeKind.Beginning, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning,
                    (RegexNodeKind.Start, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start,
                    (RegexNodeKind.Start, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start,
                    (RegexNodeKind.End, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End,
                    (RegexNodeKind.End, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End,
                    (_, false) => FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ,
                    (_, true) => FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ,
                };
                return;
            }

            // Compute any anchor trailing the expression.  If there is one, and we can also compute a fixed length
            // for the whole expression, we can use that to quickly jump to the right location in the input.
            if (!_rightToLeft) // haven't added FindNextStartingPositionMode trailing anchor support for RTL
            {
                bool triedToComputeMaxLength = false;

                TrailingAnchor = RegexPrefixAnalyzer.FindTrailingAnchor(root);
                if (TrailingAnchor is RegexNodeKind.End or RegexNodeKind.EndZ)
                {
                    triedToComputeMaxLength = true;
                    if (root.ComputeMaxLength() is int maxLength)
                    {
                        Debug.Assert(maxLength >= MinRequiredLength, $"{maxLength} should have been greater than {MinRequiredLength} minimum");
                        MaxPossibleLength = maxLength;
                        if (MinRequiredLength == maxLength)
                        {
                            FindMode = TrailingAnchor == RegexNodeKind.End ?
                                FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End :
                                FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ;
                            return;
                        }
                    }
                }

                if ((options & RegexOptions.NonBacktracking) != 0 && !triedToComputeMaxLength)
                {
                    // NonBacktracking also benefits from knowing whether the pattern is a fixed length, as it can use that
                    // knowledge to avoid multiple match phases in some situations.
                    MaxPossibleLength = root.ComputeMaxLength();
                }
            }

            // If there's a leading case-sensitive substring, just use IndexOf and inherit all of its optimizations.
            string caseSensitivePrefix = RegexPrefixAnalyzer.FindCaseSensitivePrefix(root);
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
            bool dfa = (options & RegexOptions.NonBacktracking) != 0;
            bool compiled = (options & RegexOptions.Compiled) != 0 && !dfa; // for now, we never generate code for NonBacktracking, so treat it as non-compiled
            bool interpreter = !compiled && !dfa;

            // For interpreter, we want to employ optimizations, but we don't want to make construction significantly
            // more expensive; someone who wants to pay to do more work can specify Compiled.  So for the interpreter
            // we focus only on creating a set for the first character.  Same for right-to-left, which is used very
            // rarely and thus we don't need to invest in special-casing it.
            if (_rightToLeft)
            {
                // Determine a set for anything that can possibly start the expression.
                if (RegexPrefixAnalyzer.FindFirstCharClass(root, culture) is (string CharClass, bool CaseInsensitive) set)
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
                        FindMode = set.CaseInsensitive ?
                            FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseInsensitive :
                            FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseSensitive;
                    }
                    else
                    {
                        // The set may match multiple characters.  Search for that.
                        FixedDistanceSets = new List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>()
                        {
                            (chars, set.CharClass, 0, set.CaseInsensitive)
                        };
                        FindMode = set.CaseInsensitive ?
                            FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseInsensitive :
                            FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseSensitive;
                        _asciiLookups = new uint[1][];
                    }
                }
                return;
            }

            // We're now left-to-right only and looking for sets.

            // As a backup, see if we can find a literal after a leading atomic loop.  That might be better than whatever sets we find, so
            // we want to know whether we have one in our pocket before deciding whether to use a leading set.
            (RegexNode LoopNode, (char Char, string? String, char[]? Chars) Literal)? literalAfterLoop = RegexPrefixAnalyzer.FindLiteralFollowingLeadingLoop(root);

            // Build up a list of all of the sets that are a fixed distance from the start of the expression.
            List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>? fixedDistanceSets = RegexPrefixAnalyzer.FindFixedDistanceSets(root, culture, thorough: !interpreter);
            Debug.Assert(fixedDistanceSets is null || fixedDistanceSets.Count != 0);

            // If we got such sets, we'll likely use them.  However, if the best of them is something that doesn't support a vectorized
            // search and we did successfully find a literal after an atomic loop we could search instead, we prefer the vectorizable search.
            if (fixedDistanceSets is not null &&
                (fixedDistanceSets[0].Chars is not null || literalAfterLoop is null))
            {
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

            // If we found a literal we can search for after a leading set loop, use it.
            if (literalAfterLoop is not null)
            {
                FindMode = FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight_CaseSensitive;
                LiteralAfterLoop = literalAfterLoop;
                _asciiLookups = new uint[1][];
                return;
            }
        }

        /// <summary>Gets the selected mode for performing the next <see cref="TryFindNextStartingPosition"/> operation</summary>
        public FindNextStartingPositionMode FindMode { get; } = FindNextStartingPositionMode.NoSearch;

        /// <summary>Gets the leading anchor (e.g. RegexNodeKind.Bol) if one exists and was computed.</summary>
        public RegexNodeKind LeadingAnchor { get; }

        /// <summary>Gets the trailing anchor (e.g. RegexNodeKind.Bol) if one exists and was computed.</summary>
        public RegexNodeKind TrailingAnchor { get; }

        /// <summary>Gets the minimum required length an input need be to match the pattern.</summary>
        /// <remarks>0 is a valid minimum length.  This value may also be the max (and hence fixed) length of the expression.</remarks>
        public int MinRequiredLength { get; }

        /// <summary>The maximum possible length an input could be to match the pattern.</summary>
        /// <remarks>
        /// This is currently only set when <see cref="TrailingAnchor"/> is found to be an end anchor.
        /// That can be expanded in the future as needed.
        /// </remarks>
        public int? MaxPossibleLength { get; }

        /// <summary>Gets the leading prefix.  May be an empty string.</summary>
        public string LeadingCaseSensitivePrefix { get; } = string.Empty;

        /// <summary>When in fixed distance literal mode, gets the literal and how far it is from the start of the pattern.</summary>
        public (char Literal, int Distance) FixedDistanceLiteral { get; }

        /// <summary>When in fixed distance set mode, gets the set and how far it is from the start of the pattern.</summary>
        /// <remarks>The case-insensitivity of the 0th entry will always match the mode selected, but subsequent entries may not.</remarks>
        public List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>? FixedDistanceSets { get; }

        /// <summary>When in literal after set loop node, gets the literal to search for and the RegexNode representing the leading loop.</summary>
        public (RegexNode LoopNode, (char Char, string? String, char[]? Chars) Literal)? LiteralAfterLoop { get; }

        /// <summary>Try to advance to the next starting position that might be a location for a match.</summary>
        /// <param name="textSpan">The text to search.</param>
        /// <param name="pos">The position in <paramref name="textSpan"/>.  This is updated with the found position.</param>
        /// <param name="start">The index in <paramref name="textSpan"/> to consider the start for start anchor purposes.</param>
        /// <returns>true if a position to attempt a match was found; false if none was found.</returns>
        public bool TryFindNextStartingPosition(ReadOnlySpan<char> textSpan, ref int pos, int start)
        {
            // Return early if we know there's not enough input left to match.
            if (!_rightToLeft)
            {
                if (pos > textSpan.Length - MinRequiredLength)
                {
                    pos = textSpan.Length;
                    return false;
                }
            }
            else
            {
                if (pos < MinRequiredLength)
                {
                    pos = 0;
                    return false;
                }
            }

            // Optimize the handling of a Beginning-Of-Line (BOL) anchor (only for left-to-right).  BOL is special, in that unlike
            // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
            // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
            // to boost our position to the next line, and then continue normally with any searches.
            if (LeadingAnchor == RegexNodeKind.Bol)
            {
                // If we're not currently positioned at the beginning of a line (either
                // the beginning of the string or just after a line feed), find the next
                // newline and position just after it.
                Debug.Assert(!_rightToLeft);
                int posm1 = pos - 1;
                if ((uint)posm1 < (uint)textSpan.Length && textSpan[posm1] != '\n')
                {
                    int newline = textSpan.Slice(pos).IndexOf('\n');
                    if ((uint)newline > textSpan.Length - 1 - pos)
                    {
                        pos = textSpan.Length;
                        return false;
                    }

                    // We've updated the position.  Make sure there's still enough room in the input for a possible match.
                    pos = newline + 1 + pos;
                    if (pos > textSpan.Length - MinRequiredLength)
                    {
                        pos = textSpan.Length;
                        return false;
                    }
                }
            }

            switch (FindMode)
            {
                // There's an anchor.  For some, we can simply compare against the current position.
                // For others, we can jump to the relevant location.

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning:
                    if (pos != 0)
                    {
                        // If we're not currently at the beginning, we'll never be, so fail immediately.
                        pos = textSpan.Length;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start:
                    if (pos != start)
                    {
                        // If we're not currently at the start, we'll never be, so fail immediately.
                        pos = textSpan.Length;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ:
                    if (pos < textSpan.Length - 1)
                    {
                        // If we're not currently at the end (or a newline just before it), skip ahead
                        // since nothing until then can possibly match.
                        pos = textSpan.Length - 1;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End:
                    if (pos < textSpan.Length)
                    {
                        // If we're not currently at the end (or a newline just before it), skip ahead
                        // since nothing until then can possibly match.
                        pos = textSpan.Length;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning:
                    if (pos != 0)
                    {
                        // If we're not currently at the beginning, skip ahead (or, rather, backwards)
                        // since nothing until then can possibly match. (We're iterating from the end
                        // to the beginning in RightToLeft mode.)
                        pos = 0;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start:
                    if (pos != start)
                    {
                        // If we're not currently at the starting position, we'll never be, so fail immediately.
                        // This is different from beginning, since beginning is the fixed location of 0 whereas
                        // start is wherever the iteration actually starts from; in left-to-right, that's often
                        // the same as beginning, but in RightToLeft it rarely is.
                        pos = 0;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ:
                    if (pos < textSpan.Length - 1 || ((uint)pos < (uint)textSpan.Length && textSpan[pos] != '\n'))
                    {
                        // If we're not currently at the end, we'll never be (we're iterating from end to beginning),
                        // so fail immediately.
                        pos = 0;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End:
                    if (pos < textSpan.Length)
                    {
                        // If we're not currently at the end, we'll never be (we're iterating from end to beginning),
                        // so fail immediately.
                        pos = 0;
                        return false;
                    }
                    return true;

                case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ:
                    if (pos < textSpan.Length - MinRequiredLength - 1)
                    {
                        pos = textSpan.Length - MinRequiredLength - 1;
                    }
                    return true;

                case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End:
                    if (pos < textSpan.Length - MinRequiredLength)
                    {
                        pos = textSpan.Length - MinRequiredLength;
                    }
                    return true;

                // There's a case-sensitive prefix.  Search for it with ordinal IndexOf.

                case FindNextStartingPositionMode.LeadingPrefix_LeftToRight_CaseSensitive:
                    {
                        int i = textSpan.Slice(pos).IndexOf(LeadingCaseSensitivePrefix.AsSpan());
                        if (i >= 0)
                        {
                            pos += i;
                            return true;
                        }

                        pos = textSpan.Length;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingPrefix_RightToLeft_CaseSensitive:
                    {
                        int i = textSpan.Slice(0, pos).LastIndexOf(LeadingCaseSensitivePrefix.AsSpan());
                        if (i >= 0)
                        {
                            pos = i + LeadingCaseSensitivePrefix.Length;
                            return true;
                        }

                        pos = 0;
                        return false;
                    }

                // There's a literal at the beginning of the pattern.  Search for it.

                case FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseSensitive:
                    {
                        int i = textSpan.Slice(0, pos).LastIndexOf(FixedDistanceLiteral.Literal);
                        if (i >= 0)
                        {
                            pos = i + 1;
                            return true;
                        }

                        pos = 0;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseInsensitive:
                    {
                        char ch = FixedDistanceLiteral.Literal;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = textSpan.Slice(0, pos);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (ti.ToLower(span[i]) == ch)
                            {
                                pos = i + 1;
                                return true;
                            }
                        }

                        pos = 0;
                        return false;
                    }

                // There's a set at the beginning of the pattern.  Search for it.

                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseSensitive:
                    {
                        (char[]? chars, string set, _, _) = FixedDistanceSets![0];

                        ReadOnlySpan<char> span = textSpan.Slice(pos);
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

                        pos = textSpan.Length;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive:
                    {
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                        string set = FixedDistanceSets![0].Set;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = textSpan.Slice(pos);
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref startingAsciiLookup))
                            {
                                pos += i;
                                return true;
                            }
                        }

                        pos = textSpan.Length;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseSensitive:
                    {
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                        string set = FixedDistanceSets![0].Set;

                        ReadOnlySpan<char> span = textSpan.Slice(0, pos);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (RegexCharClass.CharInClass(span[i], set, ref startingAsciiLookup))
                            {
                                pos = i + 1;
                                return true;
                            }
                        }

                        pos = 0;
                        return false;
                    }

                case FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseInsensitive:
                    {
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];
                        string set = FixedDistanceSets![0].Set;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = textSpan.Slice(0, pos);
                        for (int i = span.Length - 1; i >= 0; i--)
                        {
                            if (RegexCharClass.CharInClass(ti.ToLower(span[i]), set, ref startingAsciiLookup))
                            {
                                pos = i + 1;
                                return true;
                            }
                        }

                        pos = 0;
                        return false;
                    }

                // There's a literal at a fixed offset from the beginning of the pattern.  Search for it.

                case FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseSensitive:
                    {
                        Debug.Assert(FixedDistanceLiteral.Distance <= MinRequiredLength);

                        int i = textSpan.Slice(pos + FixedDistanceLiteral.Distance).IndexOf(FixedDistanceLiteral.Literal);
                        if (i >= 0)
                        {
                            pos += i;
                            return true;
                        }

                        pos = textSpan.Length;
                        return false;
                    }

                case FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseInsensitive:
                    {
                        Debug.Assert(FixedDistanceLiteral.Distance <= MinRequiredLength);

                        char ch = FixedDistanceLiteral.Literal;
                        TextInfo ti = _textInfo;

                        ReadOnlySpan<char> span = textSpan.Slice(pos + FixedDistanceLiteral.Distance);
                        for (int i = 0; i < span.Length; i++)
                        {
                            if (ti.ToLower(span[i]) == ch)
                            {
                                pos += i;
                                return true;
                            }
                        }

                        pos = textSpan.Length;
                        return false;
                    }

                // There are one or more sets at fixed offsets from the start of the pattern.

                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseSensitive:
                    {
                        List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)> sets = FixedDistanceSets!;
                        (char[]? primaryChars, string primarySet, int primaryDistance, _) = sets[0];
                        int endMinusRequiredLength = textSpan.Length - Math.Max(1, MinRequiredLength);

                        if (primaryChars is not null)
                        {
                            for (int inputPosition = pos; inputPosition <= endMinusRequiredLength; inputPosition++)
                            {
                                int offset = inputPosition + primaryDistance;
                                int index = textSpan.Slice(offset).IndexOfAny(primaryChars);
                                if (index < 0)
                                {
                                    break;
                                }

                                index += offset; // The index here will be offset indexed due to the use of span, so we add offset to get
                                                 // real position on the string.
                                inputPosition = index - primaryDistance;
                                if (inputPosition > endMinusRequiredLength)
                                {
                                    break;
                                }

                                for (int i = 1; i < sets.Count; i++)
                                {
                                    (_, string nextSet, int nextDistance, bool nextCaseInsensitive) = sets[i];
                                    char c = textSpan[inputPosition + nextDistance];
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
                                char c = textSpan[inputPosition + primaryDistance];
                                if (!RegexCharClass.CharInClass(c, primarySet, ref startingAsciiLookup))
                                {
                                    goto Bumpalong;
                                }

                                for (int i = 1; i < sets.Count; i++)
                                {
                                    (_, string nextSet, int nextDistance, bool nextCaseInsensitive) = sets[i];
                                    c = textSpan[inputPosition + nextDistance];
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

                        pos = textSpan.Length;
                        return false;
                    }

                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive:
                    {
                        List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)> sets = FixedDistanceSets!;
                        (_, string primarySet, int primaryDistance, _) = sets[0];

                        int endMinusRequiredLength = textSpan.Length - Math.Max(1, MinRequiredLength);
                        TextInfo ti = _textInfo;
                        ref uint[]? startingAsciiLookup = ref _asciiLookups![0];

                        for (int inputPosition = pos; inputPosition <= endMinusRequiredLength; inputPosition++)
                        {
                            char c = textSpan[inputPosition + primaryDistance];
                            if (!RegexCharClass.CharInClass(ti.ToLower(c), primarySet, ref startingAsciiLookup))
                            {
                                goto Bumpalong;
                            }

                            for (int i = 1; i < sets.Count; i++)
                            {
                                (_, string nextSet, int nextDistance, bool nextCaseInsensitive) = sets[i];
                                c = textSpan[inputPosition + nextDistance];
                                if (!RegexCharClass.CharInClass(nextCaseInsensitive ? _textInfo.ToLower(c) : c, nextSet, ref _asciiLookups![i]))
                                {
                                    goto Bumpalong;
                                }
                            }

                            pos = inputPosition;
                            return true;

                        Bumpalong:;
                        }

                        pos = textSpan.Length;
                        return false;
                    }

                // There's a literal after a leading set loop.  Find the literal, then walk backwards through the loop to find the starting position.
                case FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight_CaseSensitive:
                    {
                        Debug.Assert(LiteralAfterLoop is not null);
                        (RegexNode loopNode, (char Char, string? String, char[]? Chars) literal) = LiteralAfterLoop.GetValueOrDefault();

                        Debug.Assert(loopNode.Kind is RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic);
                        Debug.Assert(loopNode.N == int.MaxValue);

                        int startingPos = pos;
                        while (true)
                        {
                            ReadOnlySpan<char> slice = textSpan.Slice(startingPos);

                            // Find the literal.  If we can't find it, we're done searching.
                            int i = literal.String is not null ? slice.IndexOf(literal.String.AsSpan()) :
                                    literal.Chars is not null ? slice.IndexOfAny(literal.Chars.AsSpan()) :
                                    slice.IndexOf(literal.Char);
                            if (i < 0)
                            {
                                break;
                            }

                            // We found the literal.  Walk backwards from it finding as many matches as we can against the loop.
                            int prev = i;
                            while ((uint)--prev < (uint)slice.Length && RegexCharClass.CharInClass(slice[prev], loopNode.Str!, ref _asciiLookups![0])) ;

                            // If we found fewer than needed, loop around to try again.  The loop doesn't overlap with the literal,
                            // so we can start from after the last place the literal matched.
                            if ((i - prev - 1) < loopNode.M)
                            {
                                startingPos += i + 1;
                                continue;
                            }

                            // We have a winner.  The starting position is just after the last position that failed to match the loop.
                            // TODO: It'd be nice to be able to communicate literalPos as a place the matching engine can start matching
                            // after the loop, so that it doesn't need to re-match the loop.  This might only be feasible for RegexCompiler
                            // and the source generator after we refactor them to generate a single Scan method rather than separate
                            // FindFirstChar / Go methods.
                            pos = startingPos + prev + 1;
                            return true;
                        }

                        pos = textSpan.Length;
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

        /// <summary>An "end" anchor at the end of the pattern, with the pattern always matching a fixed-length expression.</summary>
        TrailingAnchor_FixedLength_LeftToRight_End,
        /// <summary>An "endz" anchor at the end of the pattern, with the pattern always matching a fixed-length expression.</summary>
        TrailingAnchor_FixedLength_LeftToRight_EndZ,

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

        /// <summary>A literal after a non-overlapping set loop at the start of the pattern.  The literal is case-sensitive.</summary>
        LiteralAfterLoop_LeftToRight_CaseSensitive,

        /// <summary>Nothing to search for. Nop.</summary>
        NoSearch,
    }
}
