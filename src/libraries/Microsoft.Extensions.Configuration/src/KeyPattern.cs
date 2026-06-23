// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    // A glob-style key pattern used for both subjects and targets. Two wildcard forms are
    // supported:
    //   *   matches any (possibly empty) run of characters within a single segment.
    //   **  matches zero or more whole segments; permitted only as an entire segment.
    // Segments are separated by ConfigurationPath.KeyDelimiter and may not be empty.
    internal sealed class KeyPattern
    {
        private enum SegmentKind : byte
        {
            Literal,
            Glob,
            DoubleStar,
        }

        private readonly struct Segment
        {
            internal Segment(SegmentKind kind, string text)
            {
                Kind = kind;
                Text = text;
            }

            internal SegmentKind Kind { get; }
            internal string Text { get; }
        }

        private readonly Segment[] _segments;

        internal KeyPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException(SR.Format(SR.Error_ReferencePatternInvalid, pattern), nameof(pattern));
            }

            string[] raw = pattern.Split(ConfigurationPath.KeyDelimiter[0]);
            _segments = new Segment[raw.Length];

            int literalChars = 0;
            int stars = 0;
            int doubleStars = 0;

            for (int i = 0; i < raw.Length; i++)
            {
                string s = raw[i];
                if (s.Length == 0)
                {
                    throw new ArgumentException(SR.Format(SR.Error_ReferencePatternInvalid, pattern), nameof(pattern));
                }

                if (s == "**")
                {
                    _segments[i] = new Segment(SegmentKind.DoubleStar, s);
                    doubleStars++;
                    continue;
                }

                // ** within a segment (combined with literal characters) is not allowed.
                if (s.Contains("**", StringComparison.Ordinal))
                {
                    throw new ArgumentException(SR.Format(SR.Error_ReferencePatternInvalid, pattern), nameof(pattern));
                }

                if (s.Contains("*", StringComparison.Ordinal))
                {
                    _segments[i] = new Segment(SegmentKind.Glob, s);
                    foreach (char c in s)
                    {
                        if (c == '*') stars++;
                        else literalChars++;
                    }
                    continue;
                }

                _segments[i] = new Segment(SegmentKind.Literal, s);
                literalChars += s.Length;
            }

            Pattern = pattern;
            LiteralCharCount = literalChars;
            StarCount = stars;
            DoubleStarCount = doubleStars;
        }

        internal string Pattern { get; }

        internal bool HasWildcard => StarCount > 0 || DoubleStarCount > 0;

        internal int LiteralCharCount { get; }

        internal int StarCount { get; }

        internal int DoubleStarCount { get; }

        internal bool TryMatch(string candidate)
        {
            string[] segs = candidate.Split(ConfigurationPath.KeyDelimiter[0]);
            return TryMatch(segs);
        }

        internal bool TryMatch(string[] candidateSegments)
            => MatchFrom(candidateSegments, 0, 0);

        private bool MatchFrom(string[] cand, int pi, int ci)
        {
            while (pi < _segments.Length)
            {
                Segment seg = _segments[pi];

                if (seg.Kind == SegmentKind.DoubleStar)
                {
                    // ** at the tail trivially absorbs whatever remains.
                    if (pi + 1 == _segments.Length)
                    {
                        return true;
                    }
                    // Otherwise try every cut point for the remaining pattern segments.
                    for (int k = ci; k <= cand.Length; k++)
                    {
                        if (MatchFrom(cand, pi + 1, k))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                if (ci >= cand.Length)
                {
                    return false;
                }

                if (seg.Kind == SegmentKind.Literal)
                {
                    if (!string.Equals(seg.Text, cand[ci], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!GlobSegment(seg.Text, cand[ci]))
                    {
                        return false;
                    }
                }
                pi++;
                ci++;
            }
            return ci == cand.Length;
        }

        // Within-segment glob: * matches any (possibly empty) run of characters. Case-insensitive.
        private static bool GlobSegment(string pattern, string candidate)
        {
            int p = 0;
            int c = 0;
            int pStar = -1;
            int cMark = 0;

            while (c < candidate.Length)
            {
                if (p < pattern.Length && pattern[p] == '*')
                {
                    pStar = p++;
                    cMark = c;
                }
                else if (p < pattern.Length && CharsEqual(pattern[p], candidate[c]))
                {
                    p++;
                    c++;
                }
                else if (pStar != -1)
                {
                    p = pStar + 1;
                    c = ++cMark;
                }
                else
                {
                    return false;
                }
            }

            while (p < pattern.Length && pattern[p] == '*')
            {
                p++;
            }
            return p == pattern.Length;
        }

        private static bool CharsEqual(char a, char b)
            => a == b || char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
    }
}
