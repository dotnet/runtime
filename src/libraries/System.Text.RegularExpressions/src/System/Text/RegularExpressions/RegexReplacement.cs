// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // takes address of managed type

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// The RegexReplacement class represents a substitution string for
    /// use when using regexes to search/replace, etc. It's logically
    /// a sequence intermixed (1) constant strings and (2) group numbers.
    /// </summary>
    internal sealed class RegexReplacement
    {
        // Constants for special insertion patterns
        private const int Specials = 4;
        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;

        private readonly string[] _strings; // table of string constants
        private readonly int[] _rules;      // negative -> group #, positive -> string #
        private readonly bool _hasBackreferences;    // true if the replacement has any backreferences; otherwise, false

        /// <summary>
        /// Since RegexReplacement shares the same parser as Regex,
        /// the constructor takes a RegexNode which is a concatenation
        /// of constant strings and backreferences.
        /// </summary>
        public RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
        {
            Debug.Assert(concat.Kind == RegexNodeKind.Concatenate, $"Expected Concatenate, got {concat.Kind}");

            var vsb = new ValueStringBuilder(stackalloc char[256]);
            FourStackStrings stackStrings = default;
            var strings = new ValueListBuilder<string>(MemoryMarshal.CreateSpan(ref stackStrings.Item1!, 4));
            var rules = new ValueListBuilder<int>(stackalloc int[64]);

            int childCount = concat.ChildCount();
            for (int i = 0; i < childCount; i++)
            {
                RegexNode child = concat.Child(i);

                switch (child.Kind)
                {
                    case RegexNodeKind.Multi:
                        vsb.Append(child.Str!);
                        break;

                    case RegexNodeKind.One:
                        vsb.Append(child.Ch);
                        break;

                    case RegexNodeKind.Backreference:
                        if (vsb.Length > 0)
                        {
                            rules.Append(strings.Length);
                            strings.Append(vsb.AsSpan().ToString());
                            vsb.Length = 0;
                        }
                        int slot = child.M;

                        if (_caps != null && slot >= 0)
                        {
                            slot = (int)_caps[slot]!;
                        }

                        rules.Append(-Specials - 1 - slot);
                        _hasBackreferences = true;
                        break;

                    default:
                        Debug.Fail($"Unexpected child kind {child.Kind}");
                        break;
                }
            }

            if (vsb.Length > 0)
            {
                rules.Append(strings.Length);
                strings.Append(vsb.ToString());
            }
            vsb.Dispose();

            Pattern = rep;
            _strings = strings.AsSpan().ToArray();
            _rules = rules.AsSpan().ToArray();

            rules.Dispose();
        }

        /// <summary>Simple struct of four strings.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct FourStackStrings // used to do the equivalent of: Span<string> strings = stackalloc string[4];
        {
            public string Item1;
            public string Item2;
            public string Item3;
            public string Item4;
        }

        /// <summary>
        /// Either returns a weakly cached RegexReplacement helper or creates one and caches it.
        /// </summary>
        /// <returns></returns>
        public static RegexReplacement GetOrCreate(WeakReference<RegexReplacement?> replRef, string replacement, Hashtable caps,
            int capsize, Hashtable capnames, RegexOptions roptions)
        {
            if (!replRef.TryGetTarget(out RegexReplacement? repl) || !repl.Pattern.Equals(replacement))
            {
                repl = RegexParser.ParseReplacement(replacement, roptions, caps, capsize, capnames);
                replRef.SetTarget(repl);
            }

            return repl;
        }

        /// <summary>The original pattern string</summary>
        public string Pattern { get; }

        /// <summary>
        /// Given a Match, emits into the StringBuilder the evaluated
        /// substitution pattern.
        /// </summary>
        public void ReplacementImpl(ref StructListBuilder<ReadOnlyMemory<char>> segments, Match match)
        {
            foreach (int rule in _rules)
            {
                // Get the segment to add.
                ReadOnlyMemory<char> segment =
                    rule >= 0 ? _strings[rule].AsMemory() : // string lookup
                    rule < -Specials ? match.GroupToStringImpl(-Specials - 1 - rule) : // group lookup
                    (-Specials - 1 - rule) switch // special insertion patterns
                    {
                        LeftPortion => match.GetLeftSubstring(),
                        RightPortion => match.GetRightSubstring(),
                        LastGroup => match.LastGroupToStringImpl(),
                        WholeString => match.Text.AsMemory(),
                        _ => default
                    };

                // Add the segment if it's not empty.  A common case for it being empty
                // is if the developer is using Regex.Replace as a way to implement
                // Regex.Remove, where the replacement string is empty.
                if (segment.Length != 0)
                {
                    segments.Add(segment);
                }
            }
        }

        /// <summary>
        /// Given a Match, emits into the builder the evaluated
        /// Right-to-Left substitution pattern.
        /// </summary>
        public void ReplacementImplRTL(ref StructListBuilder<ReadOnlyMemory<char>> segments, Match match)
        {
            for (int i = _rules.Length - 1; i >= 0; i--)
            {
                int rule = _rules[i];

                ReadOnlyMemory<char> segment =
                    rule >= 0 ? _strings[rule].AsMemory() : // string lookup
                    rule < -Specials ? match.GroupToStringImpl(-Specials - 1 - rule) : // group lookup
                    (-Specials - 1 - rule) switch // special insertion patterns
                    {
                        LeftPortion => match.GetLeftSubstring(),
                        RightPortion => match.GetRightSubstring(),
                        LastGroup => match.LastGroupToStringImpl(),
                        WholeString => match.Text.AsMemory(),
                        _ => default
                    };

                // Add the segment to the list if it's not empty.  A common case for it being
                // empty is if the developer is using Regex.Replace as a way to implement
                // Regex.Remove, where the replacement string is empty.
                if (segment.Length != 0)
                {
                    segments.Add(segment);
                }
            }
        }

        /// <summary>
        /// Replaces all occurrences of the regex in the string with the
        /// replacement pattern.
        ///
        /// Note that the special case of no matches is handled on its own:
        /// with no matches, the input string is returned unchanged.
        /// The right-to-left case is split out because StringBuilder
        /// doesn't handle right-to-left string building directly very well.
        /// </summary>
        public string Replace(Regex regex, string input, int count, int startat)
        {
            if (count < -1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
            }
            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }

            if (count == 0)
            {
                return input;
            }

            // Handle the common case of a left-to-right pattern with no backreferences in the replacement pattern such that the replacement is just a string of text.
            if (!regex.RightToLeft && !_hasBackreferences)
            {
                // With no backreferences, there should either be no rules (in the case of an empty replacement)
                // or one rule (in the case of a single text string).
                Debug.Assert(_rules.Length <= 1);
                Debug.Assert(_rules.Length == 0 || (_rules[0] == 0 && _strings.Length == 1));

                return ReplaceSimpleText(regex, input, _rules.Length != 0 ? _strings[0] : "", count, startat);
            }
            else
            {
                return ReplaceNonSimpleText(regex, input, count, startat);
            }
        }

        private static unsafe string ReplaceSimpleText(Regex regex, string input, string replacement, int count, int startat)
        {
            // As the replacement text is the same for every match, for every match we can simply store the offset/count for the match.
            // As we only split the input when there's a replacement, we know that there's then replacement text to be inserted between
            // every offset/count pair in the list.

            var state = (input, replacement, offsetAndCounts: new StructListBuilder<int>(), inputMemory: input.AsMemory(), prevat: 0, count);
            string result = input;

            regex.RunAllMatchesWithCallback(input, startat, ref state, (ref (string input, string replacement, StructListBuilder<int> segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match) =>
            {
                // Store the offset/count pair for the match.
                state.segments.Add(state.prevat);
                state.segments.Add(match.Index - state.prevat);

                // Update the previous offset to be the end of the match.
                state.prevat = match.Index + match.Length;

                // Update the number of matches and return whether to continue.
                return --state.count != 0;
            }, RegexRunnerMode.BoundsRequired, reuseMatchObject: true);

            // If the list is empty, there were no matches and we can just return the input string.
            // If the list isn't empty, we need to compose the result string.
            if (state.offsetAndCounts.Count != 0)
            {
                // Add the final offset/count pair for the text after the last match.
                state.offsetAndCounts.Add(state.prevat);
                state.offsetAndCounts.Add(input.Length - state.prevat);

                // There should now be an even number of items in the list, as each offset and count is its
                // own entry and they're added in pairs.  And there should be at least four entries, one for
                // the first segment and one for the last.
                Debug.Assert(state.offsetAndCounts.Count % 2 == 0, $"{state.offsetAndCounts.Count}");
                Debug.Assert(state.offsetAndCounts.Count >= 4, $"{state.offsetAndCounts.Count}");

                Span<int> span = state.offsetAndCounts.AsSpan();

                // Determine the final string length.
                int length = ((span.Length / 2) - 1) * replacement.Length;
                for (int i = 1; i < span.Length; i += 2) // the count of each pair is the second item
                {
                    length += span[i];
                }

                ReadOnlySpan<int> tmpSpan = span; // avoid address exposing the span and impacting the other code in the method that uses it
                result = string.Create(length, ((IntPtr)(&tmpSpan), input, replacement), static (dest, state) =>
                {
                    Span<int> span = *(Span<int>*)state.Item1;
                    for (int i = 0; i < span.Length; i += 2)
                    {
                        if (i != 0)
                        {
                            state.replacement.CopyTo(dest);
                            dest = dest.Slice(state.replacement.Length);
                        }

                        (int offset, int count) = (span[i], span[i + 1]);
                        state.input.AsSpan(offset, count).CopyTo(dest);
                        dest = dest.Slice(count);
                    }
                });
            }

            state.offsetAndCounts.Dispose();

            return result;
        }

        /// <summary>Handles cases other than left-to-right with a simple replacement string.</summary>
        private string ReplaceNonSimpleText(Regex regex, string input, int count, int startat)
        {
            var state = (replacement: this, segments: new StructListBuilder<ReadOnlyMemory<char>>(), inputMemory: input.AsMemory(), prevat: 0, count);

            if (!regex.RightToLeft)
            {
                regex.RunAllMatchesWithCallback(input, startat, ref state, (ref (RegexReplacement thisRef, StructListBuilder<ReadOnlyMemory<char>> segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match) =>
                {
                    state.segments.Add(state.inputMemory.Slice(state.prevat, match.Index - state.prevat));
                    state.prevat = match.Index + match.Length;
                    state.thisRef.ReplacementImpl(ref state.segments, match);
                    return --state.count != 0;
                }, _hasBackreferences ? RegexRunnerMode.FullMatchRequired : RegexRunnerMode.BoundsRequired, reuseMatchObject: true);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                // Final segment of the input string after the last match.
                state.segments.Add(state.inputMemory.Slice(state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.RunAllMatchesWithCallback(input, startat, ref state, (ref (RegexReplacement thisRef, StructListBuilder<ReadOnlyMemory<char>> segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match) =>
                {
                    state.segments.Add(state.inputMemory.Slice(match.Index + match.Length, state.prevat - match.Index - match.Length));
                    state.prevat = match.Index;
                    state.thisRef.ReplacementImplRTL(ref state.segments, match);
                    return --state.count != 0;
                }, _hasBackreferences ? RegexRunnerMode.FullMatchRequired : RegexRunnerMode.BoundsRequired, reuseMatchObject: true);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                // Final segment of the input string after the last match.
                state.segments.Add(state.inputMemory.Slice(0, state.prevat));

                // Reverse the segments as we're dealing with right-to-left handling.
                state.segments.AsSpan().Reverse();
            }

            // Compose the final string from the built up segments.
            return Regex.SegmentsToStringAndDispose(ref state.segments);
        }
    }
}
