// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;

namespace Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns
{
    public class PatternBuilder
    {
        private static readonly char[] _slashes = new[] { '/', '\\' };
        private static readonly char[] _star = new[] { '*' };

        public PatternBuilder()
        {
            ComparisonType = StringComparison.OrdinalIgnoreCase;
        }

        public PatternBuilder(StringComparison comparisonType)
        {
            ComparisonType = comparisonType;
        }

        public StringComparison ComparisonType { get; }

        public IPattern Build(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }

            pattern = pattern.TrimStart(_slashes);

            if (pattern.TrimEnd(_slashes).Length < pattern.Length)
            {
                // If the pattern end with a slash, it is considered as
                // a directory.
                pattern = pattern.TrimEnd(_slashes) + "/**";
            }

            var allSegments = new List<IPathSegment>();
            var isParentSegmentLegal = true;

            IList<IPathSegment> segmentsPatternStartsWith = null;
            IList<IList<IPathSegment>> segmentsPatternContains = null;
            IList<IPathSegment> segmentsPatternEndsWith = null;

            var endPattern = pattern.Length;
            for (int scanPattern = 0; scanPattern < endPattern;)
            {
                var beginSegment = scanPattern;
                var endSegment = NextIndex(pattern, _slashes, scanPattern, endPattern);

                IPathSegment segment = null;

                if (segment == null && endSegment - beginSegment == 3)
                {
                    if (pattern[beginSegment] == '*' &&
                        pattern[beginSegment + 1] == '.' &&
                        pattern[beginSegment + 2] == '*')
                    {
                        // turn *.* into *
                        beginSegment += 2;
                    }
                }

                if (segment == null && endSegment - beginSegment == 2)
                {
                    if (pattern[beginSegment] == '*' &&
                        pattern[beginSegment + 1] == '*')
                    {
                        // recognized **
                        segment = new RecursiveWildcardSegment();
                    }
                    else if (pattern[beginSegment] == '.' &&
                             pattern[beginSegment + 1] == '.')
                    {
                        // recognized ..

                        if (!isParentSegmentLegal)
                        {
                            throw new ArgumentException("\"..\" can be only added at the beginning of the pattern.");
                        }
                        segment = new ParentPathSegment();
                    }
                }

                if (segment == null && endSegment - beginSegment == 1)
                {
                    if (pattern[beginSegment] == '.')
                    {
                        // recognized .
                        segment = new CurrentPathSegment();
                    }
                }

                if (segment == null && endSegment - beginSegment > 2)
                {
                    if (pattern[beginSegment] == '*' &&
                        pattern[beginSegment + 1] == '*' &&
                        pattern[beginSegment + 2] == '.')
                    {
                        // recognize **.
                        // swallow the first *, add the recursive path segment and 
                        // the remaining part will be treat as wild card in next loop.
                        segment = new RecursiveWildcardSegment();
                        endSegment = beginSegment;
                    }
                }

                if (segment == null)
                {
                    var beginsWith = string.Empty;
                    var contains = new List<string>();
                    var endsWith = string.Empty;

                    for (int scanSegment = beginSegment; scanSegment < endSegment;)
                    {
                        var beginLiteral = scanSegment;
                        var endLiteral = NextIndex(pattern, _star, scanSegment, endSegment);

                        if (beginLiteral == beginSegment)
                        {
                            if (endLiteral == endSegment)
                            {
                                // and the only bit
                                segment = new LiteralPathSegment(Portion(pattern, beginLiteral, endLiteral), ComparisonType);
                            }
                            else
                            {
                                // this is the first bit
                                beginsWith = Portion(pattern, beginLiteral, endLiteral);
                            }
                        }
                        else if (endLiteral == endSegment)
                        {
                            // this is the last bit
                            endsWith = Portion(pattern, beginLiteral, endLiteral);
                        }
                        else
                        {
                            if (beginLiteral != endLiteral)
                            {
                                // this is a middle bit
                                contains.Add(Portion(pattern, beginLiteral, endLiteral));
                            }
                            else
                            {
                                // note: NOOP here, adjacent *'s are collapsed when they
                                // are mixed with literal text in a path segment
                            }
                        }

                        scanSegment = endLiteral + 1;
                    }

                    if (segment == null)
                    {
                        segment = new WildcardPathSegment(beginsWith, contains, endsWith, ComparisonType);
                    }
                }

                if (!(segment is ParentPathSegment))
                {
                    isParentSegmentLegal = false;
                }

                if (segment is CurrentPathSegment)
                {
                    // ignore ".\"
                }
                else
                {
                    if (segment is RecursiveWildcardSegment)
                    {
                        if (segmentsPatternStartsWith == null)
                        {
                            segmentsPatternStartsWith = new List<IPathSegment>(allSegments);
                            segmentsPatternEndsWith = new List<IPathSegment>();
                            segmentsPatternContains = new List<IList<IPathSegment>>();
                        }
                        else if (segmentsPatternEndsWith.Count != 0)
                        {
                            segmentsPatternContains.Add(segmentsPatternEndsWith);
                            segmentsPatternEndsWith = new List<IPathSegment>();
                        }
                    }
                    else if (segmentsPatternEndsWith != null)
                    {
                        segmentsPatternEndsWith.Add(segment);
                    }

                    allSegments.Add(segment);
                }

                scanPattern = endSegment + 1;
            }

            if (segmentsPatternStartsWith == null)
            {
                return new LinearPattern(allSegments);
            }
            else
            {
                return new RaggedPattern(allSegments, segmentsPatternStartsWith, segmentsPatternEndsWith, segmentsPatternContains);
            }
        }

        private static int NextIndex(string pattern, char[] anyOf, int beginIndex, int endIndex)
        {
            var index = pattern.IndexOfAny(anyOf, beginIndex, endIndex - beginIndex);
            return index == -1 ? endIndex : index;
        }

        private static string Portion(string pattern, int beginIndex, int endIndex)
        {
            return pattern.Substring(beginIndex, endIndex - beginIndex);
        }

        private class LinearPattern : ILinearPattern
        {
            public LinearPattern(List<IPathSegment> allSegments)
            {
                Segments = allSegments;
            }

            public IList<IPathSegment> Segments { get; }

            public IPatternContext CreatePatternContextForInclude()
            {
                return new PatternContextLinearInclude(this);
            }

            public IPatternContext CreatePatternContextForExclude()
            {
                return new PatternContextLinearExclude(this);
            }
        }

        private class RaggedPattern : IRaggedPattern
        {
            public RaggedPattern(List<IPathSegment> allSegments, IList<IPathSegment> segmentsPatternStartsWith, IList<IPathSegment> segmentsPatternEndsWith, IList<IList<IPathSegment>> segmentsPatternContains)
            {
                Segments = allSegments;
                StartsWith = segmentsPatternStartsWith;
                Contains = segmentsPatternContains;
                EndsWith = segmentsPatternEndsWith;
            }

            public IList<IList<IPathSegment>> Contains { get; }

            public IList<IPathSegment> EndsWith { get; }

            public IList<IPathSegment> Segments { get; }

            public IList<IPathSegment> StartsWith { get; }

            public IPatternContext CreatePatternContextForInclude()
            {
                return new PatternContextRaggedInclude(this);
            }

            public IPatternContext CreatePatternContextForExclude()
            {
                return new PatternContextRaggedExclude(this);
            }
        }
    }
}