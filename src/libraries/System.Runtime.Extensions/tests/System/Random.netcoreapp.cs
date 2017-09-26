// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static partial class RandomTests
    {
        [Fact]
        public static void Empty_Span()
        {
            int seed = Environment.TickCount;
            Random r = new Random(seed);
            r.NextBytes(Span<byte>.Empty);
        }

        [Fact]
        public static void Seeded_Span()
        {
            int seed = Environment.TickCount;

            Random r1 = new Random(seed);
            Random r2 = new Random(seed);

            Span<byte> s1 = new Span<byte>(new byte[1000]);
            r1.NextBytes(s1);
            Span<byte> s2 = new Span<byte>(new byte[1000]);
            r2.NextBytes(s2);
            for (int i = 0; i < s1.Length; i++)
            {
                Assert.Equal(s1[i], s2[i]);
            }
            for (int i = 0; i < s1.Length; i++)
            {
                int x1 = r1.Next();
                int x2 = r2.Next();
                Assert.Equal(x1, x2);
            }
        }

        [Fact]
        public static void ExpectedValues_NextBytesSpan()
        {
            byte[][] expectedValues = ByteValues();
            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                byte[] actualValues = new byte[expectedValues[seed].Length];
                var r = new Random(seed);
                r.NextBytes(new Span<byte>(actualValues));
                Assert.Equal(expectedValues[seed], actualValues);
            }
        }
    }
}
