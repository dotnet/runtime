// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Text.RegularExpressions
{
    // Callback class
    public delegate string MatchEvaluator(Match match);

    public partial class Regex
    {
        private const int ReplaceBufferSize = 256;

        /// <summary>
        /// Replaces all occurrences of the pattern with the <paramref name="replacement"/> pattern, starting at
        /// the first character in the input string.
        /// </summary>
        public static string Replace(string input, string pattern, string replacement)
        {
            return Replace(input, pattern, replacement, RegexOptions.None, s_defaultMatchTimeout);
        }

        /// <summary>
        /// Replaces all occurrences of
        /// the <paramref name="pattern "/>with the <paramref name="replacement "/>
        /// pattern, starting at the first character in the input string.
        /// </summary>
        public static string Replace(string input, string pattern, string replacement, RegexOptions options)
        {
            return Replace(input, pattern, replacement, options, s_defaultMatchTimeout);
        }

        public static string Replace(string input, string pattern, string replacement, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Replace(input, replacement);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public string Replace(string input, string replacement)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, replacement, -1, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the first character in the
        /// input string.
        /// </summary>
        public string Replace(string input, string replacement, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, replacement, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the
        /// <paramref name="replacement"/> pattern, starting at the character position
        /// <paramref name="startat"/>.
        /// </summary>
        public string Replace(string input, string replacement, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            // Gets the weakly cached replacement helper or creates one if there isn't one already.
            RegexReplacement repl = RegexReplacement.GetOrCreate(_replref, replacement, caps, capsize, capnames, roptions);

            return repl.Replace(this, input, count, startat);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern.
        /// </summary>
        public static string Replace(string input, string pattern, MatchEvaluator evaluator)
        {
            return Replace(input, pattern, evaluator, RegexOptions.None, s_defaultMatchTimeout);
        }

        /// <summary>
        /// Replaces all occurrences of the <paramref name="pattern"/> with the recent
        /// replacement pattern, starting at the first character.
        /// </summary>
        public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options)
        {
            return Replace(input, pattern, evaluator, options, s_defaultMatchTimeout);
        }

        public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Replace(input, evaluator);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, evaluator, -1, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the first character position.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Replace(input, evaluator, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Replaces all occurrences of the previously defined pattern with the recent
        /// replacement pattern, starting at the character position
        /// <paramref name="startat"/>.
        /// </summary>
        public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

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
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));
            if (count < -1)
                throw new ArgumentOutOfRangeException(nameof(count), SR.CountTooSmall);
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            if (count == 0)
                return input;

            Match match = regex.Match(input, startat);

            if (!match.Success)
            {
                return input;
            }
            else
            {
                Span<char> charInitSpan = stackalloc char[ReplaceBufferSize];
                var vsb = new ValueStringBuilder(charInitSpan);

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    do
                    {
                        if (match.Index != prevat)
                            vsb.Append(input.AsSpan(prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;
                        vsb.Append(evaluator(match));

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
                        vsb.Append(input.AsSpan(prevat, input.Length - prevat));
                }
                else
                {
                    // In right to left mode append all the inputs in reversed order to avoid an extra dynamic data structure
                    // and to be able to work with Spans. A final reverse of the transformed reversed input string generates
                    // the desired output. Similar to Tower of Hanoi.

                    int prevat = input.Length;

                    do
                    {
                        if (match.Index + match.Length != prevat)
                            vsb.AppendReversed(input.AsSpan(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;
                        vsb.AppendReversed(evaluator(match));

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat > 0)
                        vsb.AppendReversed(input.AsSpan(0, prevat));

                    vsb.Reverse();
                }

                return vsb.ToString();
            }
        }
    }
}
