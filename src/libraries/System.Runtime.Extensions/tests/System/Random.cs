// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class RandomTests
    {
        [Fact]
        public static void Unseeded()
        {
            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                int x = r.Next(20);
                Assert.True(x >= 0 && x < 20);
            }
            for (int i = 0; i < 1000; i++)
            {
                int x = r.Next(20, 30);
                Assert.True(x >= 20 && x < 30);
            }
            for (int i = 0; i < 1000; i++)
            {
                double x = r.NextDouble();
                Assert.True(x >= 0.0 && x < 1.0);
            }
        }

        [Fact]
        public static void Seeded()
        {
            int seed = Environment.TickCount;

            Random r1 = new Random(seed);
            Random r2 = new Random(seed);

            byte[] b1 = new byte[1000];
            r1.NextBytes(b1);
            byte[] b2 = new byte[1000];
            r2.NextBytes(b2);
            for (int i = 0; i < b1.Length; i++)
            {
                Assert.Equal(b1[i], b2[i]);
            }
            for (int i = 0; i < b1.Length; i++)
            {
                int x1 = r1.Next();
                int x2 = r2.Next();
                Assert.Equal(x1, x2);
            }
        }

        [Fact]
        public static void Sample()
        {
            SubRandom r = new SubRandom();

            for (int i = 0; i < 1000; i++)
            {
                double d = r.ExposeSample();
                Assert.True(d >= 0.0 && d < 1.0);
            }
        }

        private class SubRandom : Random
        {
            public double ExposeSample()
            {
                return Sample();
            }
        }
    }
}
