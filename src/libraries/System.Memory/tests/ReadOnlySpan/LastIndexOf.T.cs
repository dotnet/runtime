// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthLastIndexOf()
        {
            Assert.Equal(-1, Array.Empty<int>().LastIndexOf(0));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, Array.Empty<int>().LastIndexOf(0, comparer)));
        }

        [Fact]
        public static void TestMatchLastIndexOf()
        {
            for (int length = 0; length < 32; length++)
            {
                int[] a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 10 * (i + 1);
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target = a[targetIndex];

                    Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOf(target));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOf(target, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOf(target, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOf()
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

                Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOf(5555));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOf(5555, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOf(5555, GetFalseEqualityComparer<int>()));
            }
        }

        [Fact]
        public static void OnNoMatchMakeSureEveryElementIsComparedLastIndexOf()
        {
            for (int length = 0; length < 100; length++)
            {
                TIntLog log = new TIntLog();

                TInt[] a = new TInt[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = new TInt(10 * (i + 1), log);
                }
                ReadOnlySpan<TInt> span = new ReadOnlySpan<TInt>(a);
                int idx = span.LastIndexOf(new TInt(9999, log));
                Assert.Equal(-1, idx);

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
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOf()
        {
            const int GuardValue = 77777;
            const int GuardLength = 50;

            Action<int, int> checkForOutOfRangeAccess =
                delegate (int x, int y)
                {
                    if (x == GuardValue || y == GuardValue)
                        throw new Exception("Detected out of range access in IndexOf()");
                };

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

                Assert.Equal(-1, new ReadOnlySpan<TInt>(a, GuardLength, length).LastIndexOf(new TInt(9999, checkForOutOfRangeAccess)));
                Assert.All(GetDefaultEqualityComparers<TInt>(), comparer => Assert.Equal(-1, new ReadOnlySpan<TInt>(a, GuardLength, length).LastIndexOf(new TInt(9999, checkForOutOfRangeAccess), comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOf_String()
        {
            Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOf("a"));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOf("a", comparer)));
        }

        [Fact]
        public static void TestMatchLastIndexOf_String()
        {
            for (int length = 0; length < 32; length++)
            {
                string[] a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (10 * (i + 1)).ToString();
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target = a[targetIndex];

                    Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOf(target));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOf(target, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOf(target, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOf_String()
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

                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOf(target));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOf(target, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOf_String()
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

                Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOf("5555"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOf("5555", comparer)));
                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOf("5555", GetFalseEqualityComparer<string>()));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.LastIndexOfNullData), MemberType = typeof(TestHelpers))]
        public static void LastIndexOfNull_String(string[] spanInput, int expected)
        {
            Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOf((string)null));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOf((string)null, comparer)));
        }
    }
}
