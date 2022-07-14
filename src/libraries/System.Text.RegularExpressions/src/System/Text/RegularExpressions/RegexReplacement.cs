// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        private bool _hasBackreferences;    // true if the replacement has any backreferences; otherwise, false

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
        public void ReplacementImpl(ref SegmentStringBuilder segments, Match match)
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
        public void ReplacementImplRTL(ref SegmentStringBuilder segments, Match match)
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

            var state = (replacement: this, segments: SegmentStringBuilder.Create(), inputMemory: input.AsMemory(), prevat: 0, count);

            if (!regex.RightToLeft)
            {
                regex.RunAllMatchesWithCallback(input, startat, ref state, (ref (RegexReplacement thisRef, SegmentStringBuilder segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match) =>
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

                state.segments.Add(state.inputMemory.Slice(state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.RunAllMatchesWithCallback(input, startat, ref state, (ref (RegexReplacement thisRef, SegmentStringBuilder segments, ReadOnlyMemory<char> inputMemory, int prevat, int count) state, Match match) =>
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

                state.segments.Add(state.inputMemory.Slice(0, state.prevat));
                state.segments.AsSpan().Reverse();
            }

            return state.segments.ToString();
        }
    }
}
