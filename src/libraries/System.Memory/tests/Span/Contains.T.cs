// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    // Adapted from IndexOf.T.cs
    public static partial class SpanTests // .Contains<T>
    {
        [Fact]
        public static void ZeroLengthContains()
        {
            Span<int> span = new Span<int>(Array.Empty<int>());

            bool found = span.Contains(0);
            Assert.False(found);
        }

        [Fact]
        public static void TestContains()
        {
            for (int length = 0; length < 32; length++)
            {
                int[] a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 10 * (i + 1);
                }
                Span<int> span = new Span<int>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target = a[targetIndex];
                    bool found = span.Contains(target);
                    Assert.True(found);
                }
            }
        }

        [Fact]
        public static void TestMultipleContains()
        {
            for (int length = 2; length < 32; length++)
            {
                int[] a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 10 * (i + 1);
                }

                a[length - 1] = 5555;
                a[length - 2] = 5555;

                Span<int> span = new Span<int>(a);
                bool found = span.Contains(5555);
                Assert.True(found);
            }
        }

        [Fact]
        public static void OnNoMatchForContainsMakeSureEveryElementIsCompared()
        {
            for (int length = 0; length < 100; length++)
            {
                TIntLog log = new TIntLog();

                TInt[] a = new TInt[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = new TInt(10 * (i + 1), log);
                }
                Span<TInt> span = new Span<TInt>(a);
                bool found = span.Contains(new TInt(9999, log));
                Assert.False(found);

                // Since we asked for a non-existent value, make sure each element of the array was compared once.
                // (Strictly speaking, it would not be illegal for IndexOf to compare an element more than once but
                // that would be a non-optimal implementation and a red flag. So we'll stick with the stricter test.)
                Assert.Equal(a.Length, log.Count);
                foreach (TInt elem in a)
                {
                    int numCompares = log.CountCompares(elem.Value, 9999);
                    Assert.True(numCompares == 1, $"Expected {numCompares} == 1 for element {elem.Value}.");
                }
            }
        }

        [Fact]
        public static void MakeSureNoChecksForContainsGoOutOfRange()
        {
            const int GuardValue = 77777;
            const int GuardLength = 50;

            void checkForOutOfRangeAccess(int x, int y)
            {
                if (x == GuardValue || y == GuardValue)
                    throw new Exception("Detected out of range access in IndexOf()");
            }

            for (int length = 0; length < 100; length++)
            {
                TInt[] a = new TInt[GuardLength + length + GuardLength];
                for (int i = 0; i < a.Length; i++)
                {
                    a[i] = new TInt(GuardValue, checkForOutOfRangeAccess);
                }

                for (int i = 0; i < length; i++)
                {
                    a[GuardLength + i] = new TInt(10 * (i + 1), checkForOutOfRangeAccess);
                }

                Span<TInt> span = new Span<TInt>(a, GuardLength, length);
                bool found = span.Contains(new TInt(9999, checkForOutOfRangeAccess));
                Assert.False(found);
            }
        }

        [Fact]
        public static void ZeroLengthContains_String()
        {
            Span<string> span = new Span<string>(Array.Empty<string>());
            bool found = span.Contains("a");
            Assert.False(found);
        }

        [Fact]
        public static void TestMatchContains_String()
        {
            for (int length = 0; length < 32; length++)
            {
                string[] a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (10 * (i + 1)).ToString();
                }
                Span<string> span = new Span<string>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target = a[targetIndex];
                    bool found = span.Contains(target);
                    Assert.True(found);
                }
            }
        }

        [Fact]
        public static void TestNoMatchContains_String()
        {
            var rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                string[] a = new string[length];
                string target = (rnd.Next(0, 256)).ToString();
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == target ? (target + 1) : val;
                }
                Span<string> span = new Span<string>(a);

                bool found = span.Contains(target);
                Assert.False(found);
            }
        }

        [Fact]
        public static void TestMultipleMatchContains_String()
        {
            for (int length = 2; length < 32; length++)
            {
                string[] a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (10 * (i + 1)).ToString();
                }

                a[length - 1] = "5555";
                a[length - 2] = "5555";

                Span<string> span = new Span<string>(a);
                bool found = span.Contains("5555");
                Assert.True(found);
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ContainsNullData), MemberType = typeof(TestHelpers))]
        public static void ContainsNull_String(string[] spanInput, bool expected)
        {
            Span<string> theStrings = spanInput;
            Assert.Equal(expected, theStrings.Contains(null));
        }
    }
}
