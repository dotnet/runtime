// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8500 // takes address of managed type

namespace System.Text.RegularExpressions
{
    // Callback class
    public delegate string MatchEvaluator(Match match);

    internal delegate bool MatchCallback<TState>(ref TState state, Match match);

    public partial class Regex
    {
        /// <summary>
        /// Replaces all occurrences of the pattern with the <paramref name="replacement"/> pattern, starting at
        /// the first character in the input string.
        /// </summary>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, string replacement) =>
            RegexCache.GetOrAdd(pattern).Replace(input, replacement);

        /// <summary>
        /// Replaces all occurrences of
        /// the <paramref name="pattern "/>with the <paramref name="replacement "/>
        /// pattern, starting at the first character in the input string.
        /// </summary>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, string replacement, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, replacement);

        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, replacement);

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public string Replace(string input, string replacement)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(input, replacement, -1, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public string Replace(string input, string replacement, int count)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(input, replacement, count, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the character position
        /// <paramref name="startat"/>.
        /// </summary>
        public string Replace(string input, string replacement, int count, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }
            if (replacement is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.replacement);
            }

            // Gets the weakly cached replacement helper or creates one if there isn't one already,
            // then uses it to perform the replace.
            return
                RegexReplacement.GetOrCreate(RegexReplacementWeakReference, replacement, caps!, capsize, capnames!, roptions).
                Replace(this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern.
        /// </summary>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, MatchEvaluator evaluator) =>
            RegexCache.GetOrAdd(pattern).Replace(input, evaluator);

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern, starting at the first character.
        /// </summary>
        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, MatchEvaluator evaluator, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, evaluator);

        public static string Replace(string input, [StringSyntax(StringSyntaxAttribute.Regex, nameof(options))] string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Replace(input, evaluator);

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(evaluator, this, input, -1, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator, int count)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(evaluator, this, input, count, RightToLeft ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the character position
        /// <paramref name="startat"/>.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Replace(evaluator, this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the regex in the string with the
        /// replacement evaluator.
        ///
        /// Note that the special case of no matches is handled on its own:
        /// with no matches, the input string is returned unchanged.
        /// The right-to-left case is split out because StringBuilder
        /// doesn't handle right-to-left string building directly very well.
        /// </summary>
        private static string Replace(MatchEvaluator evaluator, Regex regex, string input, int count, int startat)
        {
            if (evaluator is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.evaluator);
            }
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

            var state = (segments: new StructListBuilder<ReadOnlyMemory<char>>(), evaluator, prevat: 0, input, count);

            if (!regex.RightToLeft)
            {
                regex.RunAllMatchesWithCallback(input, startat, ref state, static (ref (StructListBuilder<ReadOnlyMemory<char>> segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match) =>
                {
                    state.segments.Add(state.input.AsMemory(state.prevat, match.Index - state.prevat));
                    state.prevat = match.Index + match.Length;
                    state.segments.Add(state.evaluator(match).AsMemory());
                    return --state.count != 0;
                }, RegexRunnerMode.FullMatchRequired, reuseMatchObject: false);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                state.segments.Add(input.AsMemory(state.prevat, input.Length - state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.RunAllMatchesWithCallback(input, startat, ref state, static (ref (StructListBuilder<ReadOnlyMemory<char>> segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match) =>
                {
                    state.segments.Add(state.input.AsMemory(match.Index + match.Length, state.prevat - match.Index - match.Length));
                    state.prevat = match.Index;
                    state.segments.Add(state.evaluator(match).AsMemory());
                    return --state.count != 0;
                }, RegexRunnerMode.FullMatchRequired, reuseMatchObject: false);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                state.segments.Add(input.AsMemory(0, state.prevat));
                state.segments.AsSpan().Reverse();
            }

            return SegmentsToStringAndDispose(ref state.segments);
        }

        /// <summary>Creates a string from all the segments in the builder and then disposes of the builder.</summary>
        internal static unsafe string SegmentsToStringAndDispose(ref StructListBuilder<ReadOnlyMemory<char>> segments)
        {
            Span<ReadOnlyMemory<char>> span = segments.AsSpan();

            int length = 0;
            for (int i = 0; i < span.Length; i++)
            {
                length += span[i].Length;
            }

            ReadOnlySpan<ReadOnlyMemory<char>> tmpSpan = span; // avoid address exposing the span and impacting the other code in the method that uses it
            string result = string.Create(length, (IntPtr)(&tmpSpan), static (dest, spanPtr) =>
            {
                Span<ReadOnlyMemory<char>> span = *(Span<ReadOnlyMemory<char>>*)spanPtr;
                for (int i = 0; i < span.Length; i++)
                {
                    ReadOnlySpan<char> segment = span[i].Span;
                    segment.CopyTo(dest);
                    dest = dest.Slice(segment.Length);
                }
            });

            segments.Dispose();

            return result;
        }
    }
}
