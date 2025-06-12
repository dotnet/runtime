// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {    
        [Fact]
        public static void ZeroLengthCount_Int()
        {
            Assert.Equal(0, ReadOnlySpan<int>.Empty.Count(0));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(0, ReadOnlySpan<int>.Empty.Count(0, comparer)));
        }
        
        [Fact]
        public static void ZeroLengthCount_RosInt()
        {
            for (int i = 0; i <= 2; i++)
            {
                Assert.Equal(0, ReadOnlySpan<int>.Empty.Count(new int[i]));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(0, ReadOnlySpan<int>.Empty.Count(new int[i], comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthNeedleCount_RosInt()
        {
            int[] arr = [5, 5, 5, 5, 5];

            Assert.Equal(0, new ReadOnlySpan<int>(arr).Count(ReadOnlySpan<int>.Empty));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(0, new ReadOnlySpan<int>(arr).Count(ReadOnlySpan<int>.Empty, comparer)));
        }

        [Fact]
        public static void TestCount_Int()
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
                    Assert.Equal(1, new ReadOnlySpan<int>(a).Count(a[targetIndex]));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(1, new ReadOnlySpan<int>(a).Count(a[targetIndex], comparer)));
                    Assert.Equal(0, new ReadOnlySpan<int>(a).Count(a[targetIndex], GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestCount_RosInt()
        {
            for (int length = 0; length < 32; length++)
            {
                int[] a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 10 * (i + 1);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    Assert.Equal(1, new ReadOnlySpan<int>(a).Count(new int[] { a[targetIndex], a[targetIndex + 1] }));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(1, new ReadOnlySpan<int>(a).Count(new int[] { a[targetIndex], a[targetIndex + 1] }, comparer)));
                    Assert.Equal(0, new ReadOnlySpan<int>(a).Count(new int[] { a[targetIndex], a[targetIndex + 1] }, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestMultipleCount_Int()
        {
            for (int length = 2; length < 32; length++)
            {
                int[] a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 10 * (i + 1);
                }

                a[^1] = a[^2] = 5555;

                Assert.Equal(2, new ReadOnlySpan<int>(a).Count(5555));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(2, new ReadOnlySpan<int>(a).Count(5555, comparer)));
                Assert.Equal(0, new ReadOnlySpan<int>(a).Count(5555, GetFalseEqualityComparer<int>()));
            }
        }
        
        [Fact]
        public static void TestMultipleCount_RosInt()
        {
            for (int length = 4; length < 32; length++)
            {
                int[] a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = 10 * (i + 1);
                }
                a[0] = a[1] = a[^1] = a[^2] = 5555;

                Assert.Equal(2, new ReadOnlySpan<int>(a).Count(new int[] { 5555, 5555 }));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(2, new ReadOnlySpan<int>(a).Count(new int[] { 5555, 5555 }, comparer)));
                Assert.Equal(0, new ReadOnlySpan<int>(a).Count(new int[] { 5555, 5555 }, GetFalseEqualityComparer<int>()));
            }
        }

        [Fact]
        public static void OnNoMatchForCountMakeSureEveryElementIsCompared_TInt()
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
                Assert.Equal(0, span.Count(new TInt(9999, log)));

                // Since we asked for a non-existent value, make sure each element of the array was compared once.
                // (Strictly speaking, it would not be illegal for Count to compare an element more than once but
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
        public static void OnNoMatchForCountMakeSureEveryElementIsCompared_RosTInt()
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
                Assert.Equal(0, span.Count(new TInt[] { new TInt(9999, log), new TInt(10000, log) }));

                // Since we asked for a non-existent value, make sure each element of the array was compared once.
                // (Strictly speaking, it would not be illegal for Count to compare an element more than once but
                // that would be a non-optimal implementation and a red flag. So we'll stick with the stricter test.)
                if (length > 0)
                {
                    Assert.Equal(a.Length - 1, log.Count);
                }
                for (int i = 0; i < length - 1; i++)
                {
                    int numCompares = log.CountCompares(a[i].Value, 9999);
                    Assert.True(numCompares == 1, $"Expected {numCompares} == 1 for element {a[i].Value}.");
                    
                    numCompares = log.CountCompares(a[i].Value, 10000);
                    Assert.True(numCompares == 0, $"Expected {numCompares} == 0 for element {a[i].Value}.");
                }
            }
        }

        [Fact]
        public static void MakeSureNoChecksForCountGoOutOfRange_TInt()
        {
            const int GuardValue = 77777;
            const int GuardLength = 50;

            void CheckForOutOfRangeAccess(int x, int y) =>
                Assert.True(x != GuardValue && y != GuardValue, $"{x} or {y} == {GuardValue}");

            for (int length = 0; length < 100; length++)
            {
                TInt[] a = new TInt[GuardLength + length + GuardLength];
                for (int i = 0; i < a.Length; i++)
                {
                    a[i] = new TInt(GuardValue, CheckForOutOfRangeAccess);
                }

                for (int i = 0; i < length; i++)
                {
                    a[GuardLength + i] = new TInt(10 * (i + 1), CheckForOutOfRangeAccess);
                }

                ReadOnlySpan<TInt> span = new ReadOnlySpan<TInt>(a, GuardLength, length);
                Assert.Equal(0, span.Count(new TInt(9999, CheckForOutOfRangeAccess)));
            }
        }

        [Fact]
        public static void MakeSureNoChecksForCountGoOutOfRange_RosTInt()
        {
            const int GuardValue = 77777;
            const int GuardLength = 50;

            void CheckForOutOfRangeAccess(int x, int y) =>
                Assert.True(x != GuardValue && y != GuardValue, $"{x} or {y} == {GuardValue}");

            for (int length = 0; length < 100; length++)
            {
                TInt[] a = new TInt[GuardLength + length + GuardLength];
                for (int i = 0; i < a.Length; i++)
                {
                    a[i] = new TInt(GuardValue, CheckForOutOfRangeAccess);
                }

                for (int i = 0; i < length; i++)
                {
                    a[GuardLength + i] = new TInt(10 * (i + 1), CheckForOutOfRangeAccess);
                }

                ReadOnlySpan<TInt> span = new ReadOnlySpan<TInt>(a, GuardLength, length);
                Assert.Equal(0, span.Count(new TInt[] { new TInt(9999, CheckForOutOfRangeAccess), new TInt(9999, CheckForOutOfRangeAccess) }));
            }
        }

        [Fact]
        public static void ZeroLengthCount_String()
        {
            Assert.Equal(0, ReadOnlySpan<string>.Empty.Count("a"));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(0, ReadOnlySpan<string>.Empty.Count("a", comparer)));
        }

        [Fact]
        public static void ZeroLengthCount_RosString()
        {
            Assert.Equal(0, ReadOnlySpan<string>.Empty.Count(new[] { "a", "b" }));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(0, ReadOnlySpan<string>.Empty.Count(new[] { "a", "b" }, comparer)));
        }

        [Fact]
        public static void TestMatchCount_String()
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
                    Assert.Equal(1, new ReadOnlySpan<string>(a).Count(a[targetIndex]));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(1, new ReadOnlySpan<string>(a).Count(a[targetIndex], comparer)));
                    Assert.Equal(0, new ReadOnlySpan<string>(a).Count(a[targetIndex], GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestMatchCount_RosString()
        {
            for (int length = 0; length < 32; length++)
            {
                string[] a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (10 * (i + 1)).ToString();
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    Assert.Equal(1, new ReadOnlySpan<string>(a).Count(new string[] { a[targetIndex], a[targetIndex + 1] }));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(1, new ReadOnlySpan<string>(a).Count(new string[] { a[targetIndex], a[targetIndex + 1] }, comparer)));
                    Assert.Equal(0, new ReadOnlySpan<string>(a).Count(new string[] { a[targetIndex], a[targetIndex + 1] }, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestNoMatchCount_String()
        {
            var rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                string[] a = new string[length];
                string target = rnd.Next(0, 256).ToString();
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == target ? (target + 1) : val;
                }

                Assert.Equal(0, new ReadOnlySpan<string>(a).Count(target));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(0, new ReadOnlySpan<string>(a).Count(target, comparer)));
            }
        }
        
        [Fact]
        public static void TestNoMatchCount_RosString()
        {
            var rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                string[] a = new string[length];
                var target = new string[] { rnd.Next(0, 256).ToString(), "0" };
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == target[0] ? (target[0] + 1) : val;
                }

                Assert.Equal(0, new ReadOnlySpan<string>(a).Count(target));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(0, new ReadOnlySpan<string>(a).Count(target, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchCount_String()
        {
            for (int length = 2; length < 32; length++)
            {
                string[] a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (10 * (i + 1)).ToString();
                }

                a[^1] = a[^2] = "5555";

                Assert.Equal(2, new ReadOnlySpan<string>(a).Count("5555"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(2, new ReadOnlySpan<string>(a).Count("5555", comparer)));
                Assert.Equal(0, new ReadOnlySpan<string>(a).Count("5555", GetFalseEqualityComparer<string>()));
            }
        }

        [Fact]
        public static void TestMultipleMatchCount_RosString()
        {
            for (int length = 4; length < 32; length++)
            {
                string[] a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (10 * (i + 1)).ToString();
                }
                
                a[0] = a[1] = a[^1] = a[^2] = "5555";

                Assert.Equal(2, new ReadOnlySpan<string>(a).Count(new string[] { "5555", "5555" }));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(2, new ReadOnlySpan<string>(a).Count(new string[] { "5555", "5555" }, comparer)));
                Assert.Equal(0, new ReadOnlySpan<string>(a).Count(new string[] { "5555", "5555" }, GetFalseEqualityComparer<string>()));
            }
        }

        [Fact]
        public static void TestOrdinalStringCount_String()
        {
            var arr = new string[] { "ii", "II", "μμ", "ii" };
            Assert.Equal(2, new ReadOnlySpan<string>(arr).Count("ii"));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(2, new ReadOnlySpan<string>(arr).Count("ii", comparer)));
            Assert.Equal(0, new ReadOnlySpan<string>(arr).Count("ii", GetFalseEqualityComparer<string>()));
        }

        [Fact]
        public static void TestOrdinalStringCount_RosString()
        {
            var arr = new string[] { "ii", "II", "μμ", "ii", "μμ" };
            Assert.Equal(1, new ReadOnlySpan<string>(arr).Count(new string[] { "ii", "II" }));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(1, new ReadOnlySpan<string>(arr).Count(new string[] { "ii", "II" }, comparer)));
            Assert.Equal(0, new ReadOnlySpan<string>(arr).Count(new string[] { "ii", "II" }, GetFalseEqualityComparer<string>()));
        }
        
        [Fact]
        public static void TestOverlapDoNotCount_RosChar()
        {
            ReadOnlySpan<char> span = new string('a', 10);
            Assert.Equal(5, span.Count("aa"));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.CountNullData), MemberType = typeof(TestHelpers))]
        public static void CountNull_String(string[] spanInput, int expected)
        {
            Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).Count((string)null));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).Count((string)null, comparer)));
            Assert.Equal(0, new ReadOnlySpan<string>(spanInput).Count((string)null, GetFalseEqualityComparer<string>()));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.CountNullRosData), MemberType = typeof(TestHelpers))]
        public static void CountNull_RosString(string[] spanInput, int expected)
        {
            var target = new string[] { null, "9" };
            Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).Count(target));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).Count(target, comparer)));
            Assert.Equal(0, new ReadOnlySpan<string>(spanInput).Count(target, GetFalseEqualityComparer<string>()));
        }
    }
}
