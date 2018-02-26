// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined
        /// by <paramref name="pattern"/>.
        /// </summary>
        public static string[] Split(string input, string pattern)
        {
            return Split(input, pattern, RegexOptions.None, s_defaultMatchTimeout);
        }

        /// <summary>
        /// Splits the <paramref name="input "/>string at the position defined by <paramref name="pattern"/>.
        /// </summary>
        public static string[] Split(string input, string pattern, RegexOptions options)
        {
            return Split(input, pattern, options, s_defaultMatchTimeout);
        }

        public static string[] Split(string input, string pattern, RegexOptions options, TimeSpan matchTimeout)
        {
            return new Regex(pattern, options, matchTimeout, true).Split(input);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public string[] Split(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Split(input, 0, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a
        /// previous pattern.
        /// </summary>
        public string[] Split(string input, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Split(this, input, count, UseOptionR() ? input.Length : 0);
        }

        /// <summary>
        /// Splits the <paramref name="input"/> string at the position defined by a previous pattern.
        /// </summary>
        public string[] Split(string input, int count, int startat)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return Split(this, input, count, startat);
        }

        /// <summary>
        /// Does a split. In the right-to-left case we reorder the
        /// array to be forwards.
        /// </summary>
        private static string[] Split(Regex regex, string input, int count, int startat)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.CountTooSmall);
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            string[] result;

            if (count == 1)
            {
                result = new string[1];
                result[0] = input;
                return result;
            }

            count -= 1;

            Match match = regex.Match(input, startat);

            if (!match.Success)
            {
                result = new string[1];
                result[0] = input;
                return result;
            }
            else
            {
                List<string> al = new List<string>();

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    for (; ; )
                    {
                        al.Add(input.Substring(prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;

                        // add all matched capture groups to the list.
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.IsMatched(i))
                                al.Add(match.Groups[i].ToString());
                        }

                        if (--count == 0)
                            break;

                        match = match.NextMatch();

                        if (!match.Success)
                            break;
                    }

                    al.Add(input.Substring(prevat, input.Length - prevat));
                }
                else
                {
                    int prevat = input.Length;

                    for (; ; )
                    {
                        al.Add(input.Substring(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;

                        // add all matched capture groups to the list.
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.IsMatched(i))
                                al.Add(match.Groups[i].ToString());
                        }

                        if (--count == 0)
                            break;

                        match = match.NextMatch();

                        if (!match.Success)
                            break;
                    }

                    al.Add(input.Substring(0, prevat));

                    al.Reverse(0, al.Count);
                }

                return al.ToArray();
            }
        }
    }
}
