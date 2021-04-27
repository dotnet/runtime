// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined
        /// by <paramref name="pattern"/>.
        /// </summary>
        public static string[] Split(string input, string pattern) =>
            RegexCache.GetOrAdd(pattern).Split(input);

        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined by <paramref name="pattern"/>.
        /// </summary>
        public static string[] Split(string input, string pattern, RegexOptions options) =>
            RegexCache.GetOrAdd(pattern, options, s_defaultMatchTimeout).Split(input);

        public static string[] Split(string input, string pattern, RegexOptions options, TimeSpan matchTimeout) =>
            RegexCache.GetOrAdd(pattern, options, matchTimeout).Split(input);

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public string[] Split(string input)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Split(this, input, 0, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public string[] Split(string input, int count)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Split(this, input, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a previous pattern.
        /// </summary>
        public string[] Split(string input, int count, int startat)
        {
            if (input is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.input);
            }

            return Split(this, input, count, startat);
        }

        /// <summary>
        /// Does a split. In the right-to-left case we reorder the
        /// array to be forwards.
        /// </summary>
        private static string[] Split(Regex regex, string input, int count, int startat)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.CountTooSmall);
            }
            if ((uint)startat > (uint)input.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
            }

            if (count == 1)
            {
                return new[] { input };
            }

            count--;
            var state = (results: new List<string>(), prevat: 0, input, count);

            if (!regex.RightToLeft)
            {
                regex.Run(input, startat, ref state, static (ref (List<string> results, int prevat, string input, int count) state, Match match) =>
                {
                    state.results.Add(state.input.Substring(state.prevat, match.Index - state.prevat));
                    state.prevat = match.Index + match.Length;

                    // add all matched capture groups to the list.
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.IsMatched(i))
                        {
                            state.results.Add(match.Groups[i].ToString());
                        }
                    }

                    return --state.count != 0;
                }, reuseMatchObject: true);

                if (state.results.Count == 0)
                {
                    return new[] { input };
                }

                state.results.Add(input.Substring(state.prevat, input.Length - state.prevat));
            }
            else
            {
                state.prevat = input.Length;

                regex.Run(input, startat, ref state, static (ref (List<string> results, int prevat, string input, int count) state, Match match) =>
                {
                    state.results.Add(state.input.Substring(match.Index + match.Length, state.prevat - match.Index - match.Length));
                    state.prevat = match.Index;

                    // add all matched capture groups to the list.
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (match.IsMatched(i))
                        {
                            state.results.Add(match.Groups[i].ToString());
                        }
                    }

                    return --state.count != 0;
                }, reuseMatchObject: true);

                if (state.results.Count == 0)
                {
                    return new[] { input };
                }

                state.results.Add(input.Substring(0, state.prevat));
                state.results.Reverse(0, state.results.Count);
            }

            return state.results.ToArray();
        }
    }
}
