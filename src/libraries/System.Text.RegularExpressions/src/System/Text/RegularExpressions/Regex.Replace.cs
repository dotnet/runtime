// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static string Replace(string input, string pattern, string replacement) =>
            RegexCache.GetOrAdd(pattern).Replace(input, replacement);

        /// <summary>
        /// Replaces all occurrences of
        /// the <paramref name="pattern "/>with the <paramref name="replacement "/>
        /// pattern, starting at the first character in the input string.
        /// </summary>
        public static string Replace(string input, string pattern, string replacement, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, replacement);

        public static string Replace(string input, string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout) =>
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

            return Replace(input, replacement, -1, UseOptionR() ? input.Length : 0);
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

            return Replace(input, replacement, count, UseOptionR() ? input.Length : 0);
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
                RegexReplacement.GetOrCreate(_replref!, replacement, caps!, capsize, capnames!, roptions).
                Replace(this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern.
        /// </summary>
        public static string Replace(string input, string pattern, MatchEvaluator evaluator) =>
            RegexCache.GetOrAdd(pattern).Replace(input, evaluator);

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern, starting at the first character.
        /// </summary>
        public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Replace(input, evaluator);

        public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout) =>
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

            return Replace(evaluator, this, input, -1, UseOptionR() ? input.Length : 0);
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

            return Replace(evaluator, this, input, count, UseOptionR() ? input.Length : 0);
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

            var state = (segments: SegmentStringBuilder.Create(), evaluator, prevat: 0, input, count);

            if (!regex.RightToLeft)
            {
                regex.Run(input, startat, ref state, static (ref (SegmentStringBuilder segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match) =>
                {
                    state.segments.Add(state.input.AsMemory(state.prevat, match.Index - state.prevat));
                    state.prevat = match.Index + match.Length;
                    state.segments.Add(state.evaluator(match).AsMemory());
                    return --state.count != 0;
                }, reuseMatchObject: false);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                state.segments.Add(input.AsMemory(state.prevat, input.Length - state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.Run(input, startat, ref state, static (ref (SegmentStringBuilder segments, MatchEvaluator evaluator, int prevat, string input, int count) state, Match match) =>
                {
                    state.segments.Add(state.input.AsMemory(match.Index + match.Length, state.prevat - match.Index - match.Length));
                    state.prevat = match.Index;
                    state.segments.Add(state.evaluator(match).AsMemory());
                    return --state.count != 0;
                }, reuseMatchObject: false);

                if (state.segments.Count == 0)
                {
                    return input;
                }

                state.segments.Add(input.AsMemory(0, state.prevat));
                state.segments.AsSpan().Reverse();
            }

            return state.segments.ToString();
        }
    }
}
