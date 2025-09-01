// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.PrivateUri.Tests
{
    public class PathCompressionTests
    {
        [Fact]
        public void TestCompressRegressions()
        {
            const string Alphabet = "/.A";

            var rng = new Random(42);
            char[] buffer1 = new char[64];
            char[] buffer2 = new char[64];

            for (int i = 0; i < 1_000_000; i++)
            {
                Span<char> pathBuffer1 = buffer1.AsSpan(0, rng.Next(buffer1.Length));
                Span<char> pathBuffer2 = buffer2.AsSpan(0, pathBuffer1.Length);

                rng.GetItems(Alphabet, pathBuffer1);
                pathBuffer1.CopyTo(pathBuffer2);

                bool canonicalizeAsFilePath = rng.Next(2) == 0;

                int expectedNewLength = LegacyCompressImpl(pathBuffer1, canonicalizeAsFilePath);
                int actualNewLength = UriHelper.Compress(pathBuffer2, convertPathSlashes: false, canonicalizeAsFilePath);

                Assert.Equal(pathBuffer1.Slice(0, expectedNewLength), pathBuffer2.Slice(0, actualNewLength));
            }
        }

        private static int LegacyCompressImpl(Span<char> span, bool canonicalizeAsFilePath)
        {
            int slashCount = 0;
            int lastSlash = 0;
            int dotCount = 0;
            int removeSegments = 0;

            for (int i = span.Length - 1; i >= 0; i--)
            {
                char ch = span[i];

                // compress multiple '/' for file URI
                if (ch == '/')
                {
                    ++slashCount;
                }
                else
                {
                    if (slashCount > 1)
                    {
                        // else preserve repeated slashes
                        lastSlash = i + 1;
                    }
                    slashCount = 0;
                }

                if (ch == '.')
                {
                    ++dotCount;
                    continue;
                }
                else if (dotCount != 0)
                {
                    bool skipSegment = canonicalizeAsFilePath && (dotCount > 2 || ch != '/');

                    // Cases:
                    // /./                  = remove this segment
                    // /../                 = remove this segment, mark next for removal
                    // /....x               = DO NOT TOUCH, leave as is
                    // x.../                = DO NOT TOUCH, leave as is, except for V2 legacy mode
                    if (!skipSegment && ch == '/')
                    {
                        if ((lastSlash == i + dotCount + 1 // "/..../"
                                || (lastSlash == 0 && i + dotCount + 1 == span.Length)) // "/..."
                            && (dotCount <= 2))
                        {
                            //  /./ or /.<eos> or /../ or /..<eos>

                            // span.Remove(i + 1, dotCount + (lastSlash == 0 ? 0 : 1));
                            lastSlash = i + 1 + dotCount + (lastSlash == 0 ? 0 : 1);
                            span.Slice(lastSlash).CopyTo(span.Slice(i + 1));
                            span = span.Slice(0, span.Length - (lastSlash - i - 1));

                            lastSlash = i;
                            if (dotCount == 2)
                            {
                                // We have 2 dots in between like /../ or /..<eos>,
                                // Mark next segment for removal and remove this /../ or /..
                                ++removeSegments;
                            }
                            dotCount = 0;
                            continue;
                        }
                    }
                    // .NET 4.5 no longer removes trailing dots in a path segment x.../  or  x...<eos>
                    dotCount = 0;

                    // Here all other cases go such as
                    // x.[..]y or /.[..]x or (/x.[...][/] && removeSegments !=0)
                }

                // Now we may want to remove a segment because of previous /../
                if (ch == '/')
                {
                    if (removeSegments != 0)
                    {
                        --removeSegments;

                        span.Slice(lastSlash + 1).CopyTo(span.Slice(i + 1));
                        span = span.Slice(0, span.Length - (lastSlash - i));
                    }
                    lastSlash = i;
                }
            }

            if (span.Length != 0 && canonicalizeAsFilePath)
            {
                if (slashCount <= 1)
                {
                    if (removeSegments != 0 && span[0] != '/')
                    {
                        //remove first not rooted segment
                        lastSlash++;
                        span.Slice(lastSlash).CopyTo(span);
                        return span.Length - lastSlash;
                    }
                    else if (dotCount != 0)
                    {
                        // If final string starts with a segment looking like .[...]/ or .[...]<eos>
                        // then we remove this first segment
                        if (lastSlash == dotCount || (lastSlash == 0 && dotCount == span.Length))
                        {
                            dotCount += lastSlash == 0 ? 0 : 1;
                            span.Slice(dotCount).CopyTo(span);
                            return span.Length - dotCount;
                        }
                    }
                }
            }

            return span.Length;
        }
    }
}
