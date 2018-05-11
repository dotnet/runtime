// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The RegexReplacement class represents a substitution string for
// use when using regexes to search/replace, etc. It's logically
// a sequence intermixed (1) constant strings and (2) group numbers.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexReplacement
    {
        // Constants for special insertion patterns
        private const int Specials = 4;
        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;

        private readonly List<string> _strings; // table of string constants
        private readonly List<int> _rules;      // negative -> group #, positive -> string #

        /// <summary>
        /// Since RegexReplacement shares the same parser as Regex,
        /// the constructor takes a RegexNode which is a concatenation
        /// of constant strings and backreferences.
        /// </summary>
        public RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
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
                        sb.Append(child.Str);
                        break;

                    case RegexNode.One:
                        sb.Append(child.Ch);
                        break;

                    case RegexNode.Ref:
                        if (sb.Length > 0)
                        {
                            rules.Add(strings.Count);
                            strings.Add(sb.ToString());
                            sb.Length = 0;
                        }
                        int slot = child.M;

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

            Pattern = rep;
            _strings = strings;
            _rules = rules;
        }

        /// <summary>
        /// Either returns a weakly cached RegexReplacement helper or creates one and caches it.
        /// </summary>
        /// <returns></returns>
        public static RegexReplacement GetOrCreate(WeakReference<RegexReplacement> replRef, string replacement, Hashtable caps,
            int capsize, Hashtable capnames, RegexOptions roptions)
        {
            RegexReplacement repl;

            if (!replRef.TryGetTarget(out repl) || !repl.Pattern.Equals(replacement))
            {
                repl = RegexParser.ParseReplacement(replacement, caps, capsize, capnames, roptions);
                replRef.SetTarget(repl);
            }

            return repl;
        }

        /// <summary>
        /// The original pattern string
        /// </summary>
        public string Pattern { get; }

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
                            sb.Append(match.Text);
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
                    al.Add(match.GroupToStringImpl(-Specials - 1 - r).ToString());
                else
                {
                    switch (-Specials - 1 - r)
                    { // special insertion patterns
                        case LeftPortion:
                            al.Add(match.GetLeftSubstring().ToString());
                            break;
                        case RightPortion:
                            al.Add(match.GetRightSubstring().ToString());
                            break;
                        case LastGroup:
                            al.Add(match.LastGroupToStringImpl().ToString());
                            break;
                        case WholeString:
                            al.Add(match.Text);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the replacement result for a single match
        /// </summary>
        public string Replacement(Match match)
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
        public string Replace(Regex regex, string input, int count, int startat)
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
    }
}
