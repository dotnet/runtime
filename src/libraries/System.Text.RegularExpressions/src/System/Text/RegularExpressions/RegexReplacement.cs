// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The RegexReplacement class represents a substitution string for
// use when using regexes to search/replace, etc. It's logically
// a sequence intermixed (1) constant strings and (2) group numbers.

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexReplacement
    {
        // Constants for special insertion patterns
        internal const int Specials = 4;
        internal const int LeftPortion = -1;
        internal const int RightPortion = -2;
        internal const int LastGroup = -3;
        internal const int WholeString = -4;

        private readonly string _rep;
        private readonly List<string> _strings; // table of string constants
        private readonly List<int> _rules;      // negative -> group #, positive -> string #

        /// <summary>
        /// Since RegexReplacement shares the same parser as Regex,
        /// the constructor takes a RegexNode which is a concatenation
        /// of constant strings and backreferences.
        /// </summary>
        internal RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
        {
            if (concat.Type() != RegexNode.Concatenate)
                throw new ArgumentException(SR.ReplacementError);

            StringBuilder sb = StringBuilderCache.Acquire();
            List<string> strings = new List<string>();
            List<int> rules = new List<int>();

            for (int i = 0; i < concat.ChildCount(); i++)
            {
                RegexNode child = concat.Child(i);

                switch (child.Type())
                {
                    case RegexNode.Multi:
                        sb.Append(child._str);
                        break;

                    case RegexNode.One:
                        sb.Append(child._ch);
                        break;

                    case RegexNode.Ref:
                        if (sb.Length > 0)
                        {
                            rules.Add(strings.Count);
                            strings.Add(sb.ToString());
                            sb.Length = 0;
                        }
                        int slot = child._m;

                        if (_caps != null && slot >= 0)
                            slot = (int)_caps[slot];

                        rules.Add(-Specials - 1 - slot);
                        break;

                    default:
                        throw new ArgumentException(SR.ReplacementError);
                }
            }

            if (sb.Length > 0)
            {
                rules.Add(strings.Count);
                strings.Add(sb.ToString());
            }

            StringBuilderCache.Release(sb);

            _rep = rep;
            _strings = strings;
            _rules = rules;
        }

        /// <summary>
        /// Given a Match, emits into the StringBuilder the evaluated
        /// substitution pattern.
        /// </summary>
        private void ReplacementImpl(StringBuilder sb, Match match)
        {
            for (int i = 0; i < _rules.Count; i++)
            {
                int r = _rules[i];
                if (r >= 0)   // string lookup
                    sb.Append(_strings[r]);
                else if (r < -Specials) // group lookup
                    sb.Append(match.GroupToStringImpl(-Specials - 1 - r));
                else
                {
                    switch (-Specials - 1 - r)
                    { // special insertion patterns
                        case LeftPortion:
                            sb.Append(match.GetLeftSubstring());
                            break;
                        case RightPortion:
                            sb.Append(match.GetRightSubstring());
                            break;
                        case LastGroup:
                            sb.Append(match.LastGroupToStringImpl());
                            break;
                        case WholeString:
                            sb.Append(match.GetOriginalString());
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Given a Match, emits into the List<string> the evaluated
        /// Right-to-Left substitution pattern.
        /// </summary>
        private void ReplacementImplRTL(List<string> al, Match match)
        {
            for (int i = _rules.Count - 1; i >= 0; i--)
            {
                int r = _rules[i];
                if (r >= 0)  // string lookup
                    al.Add(_strings[r]);
                else if (r < -Specials) // group lookup
                    al.Add(match.GroupToStringImpl(-Specials - 1 - r));
                else
                {
                    switch (-Specials - 1 - r)
                    { // special insertion patterns
                        case LeftPortion:
                            al.Add(match.GetLeftSubstring());
                            break;
                        case RightPortion:
                            al.Add(match.GetRightSubstring());
                            break;
                        case LastGroup:
                            al.Add(match.LastGroupToStringImpl());
                            break;
                        case WholeString:
                            al.Add(match.GetOriginalString());
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// The original pattern string
        /// </summary>
        internal string Pattern
        {
            get { return _rep; }
        }

        /// <summary>
        /// Returns the replacement result for a single match
        /// </summary>
        internal string Replacement(Match match)
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            ReplacementImpl(sb, match);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        // Three very similar algorithms appear below: replace (pattern),
        // replace (evaluator), and split.

        /// <summary>
        /// Replaces all occurrences of the regex in the string with the
        /// replacement pattern.
        ///
        /// Note that the special case of no matches is handled on its own:
        /// with no matches, the input string is returned unchanged.
        /// The right-to-left case is split out because StringBuilder
        /// doesn't handle right-to-left string building directly very well.
        /// </summary>
        internal string Replace(Regex regex, string input, int count, int startat)
        {
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
                StringBuilder sb = StringBuilderCache.Acquire();

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    do
                    {
                        if (match.Index != prevat)
                            sb.Append(input, prevat, match.Index - prevat);

                        prevat = match.Index + match.Length;
                        ReplacementImpl(sb, match);
                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
                        sb.Append(input, prevat, input.Length - prevat);
                }
                else
                {
                    List<string> al = new List<string>();
                    int prevat = input.Length;

                    do
                    {
                        if (match.Index + match.Length != prevat)
                            al.Add(input.Substring(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;
                        ReplacementImplRTL(al, match);
                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat > 0)
                        sb.Append(input, 0, prevat);

                    for (int i = al.Count - 1; i >= 0; i--)
                    {
                        sb.Append(al[i]);
                    }
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }
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
        internal static string Replace(MatchEvaluator evaluator, Regex regex,
                                       string input, int count, int startat)
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
                StringBuilder sb = StringBuilderCache.Acquire();

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    do
                    {
                        if (match.Index != prevat)
                            sb.Append(input, prevat, match.Index - prevat);

                        prevat = match.Index + match.Length;

                        sb.Append(evaluator(match));

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
                        sb.Append(input, prevat, input.Length - prevat);
                }
                else
                {
                    List<string> al = new List<string>();
                    int prevat = input.Length;

                    do
                    {
                        if (match.Index + match.Length != prevat)
                            al.Add(input.Substring(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;

                        al.Add(evaluator(match));

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat > 0)
                        sb.Append(input, 0, prevat);

                    for (int i = al.Count - 1; i >= 0; i--)
                    {
                        sb.Append(al[i]);
                    }
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        /// <summary>
        /// Does a split. In the right-to-left case we reorder the
        /// array to be forwards.
        /// </summary>
        internal static string[] Split(Regex regex, string input, int count, int startat)
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

                    for (; ;)
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

                    for (; ;)
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
