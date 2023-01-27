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
        }
        
        [Fact]
        public static void ZeroLengthCount_RosInt()
        {
            for (int i = 0; i <= 2; i++)
            {
                Assert.Equal(0, ReadOnlySpan<int>.Empty.Count(new int[i]));
            }
        }

        [Fact]
        public static void ZeroLengthNeedleCount_RosInt()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(new int[] { 5, 5, 5, 5, 5 });

            Assert.Equal(0, span.Count<int>(ReadOnlySpan<int>.Empty));
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
                ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    Assert.Equal(1, span.Count(a[targetIndex]));
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
                ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    Assert.Equal(1, span.Count(new int[] { a[targetIndex], a[targetIndex + 1] }));
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

                ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);
                Assert.Equal(2, span.Count(5555));
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

                ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);
                Assert.Equal(2, span.Count<int>(new int[] { 5555, 5555 }));
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
        }
        
        [Fact]
        public static void ZeroLengthCount_RosString()
        {
            Assert.Equal(0, ReadOnlySpan<string>.Empty.Count(new[] { "a", "b" }));
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
                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    Assert.Equal(1, span.Count(a[targetIndex]));
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
                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    Assert.Equal(1, span.Count(new string[] { a[targetIndex], a[targetIndex + 1] }));
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

                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);
                Assert.Equal(0, span.Count(target));
            }
        }
        
        [Fact]
        public static void TestNoMatchCount_RosString()
        {
            var rnd = new Random(42);
            for (int length = 0; length <= byte.MaxValue; length++)
            {
                string[] a = new string[length];
                ReadOnlySpan<string> target = new string[] { rnd.Next(0, 256).ToString(), "0" };
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == target[0] ? (target[0] + 1) : val;
                }

                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);
                Assert.Equal(0, span.Count(target));
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

                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);
                Assert.Equal(2, span.Count("5555"));
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

                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);
                Assert.Equal(2, span.Count(new string[] { "5555", "5555" }));
            }
        }

        [Fact]
        public static void TestOrdinalStringCount_String()
        {
            ReadOnlySpan<string> span = new string[] { "ii", "II", "μμ", "ii" };
            Assert.Equal(2, span.Count("ii"));
        }

        [Fact]
        public static void TestOrdinalStringCount_RosString()
        {
            ReadOnlySpan<string> span = new string[] { "ii", "II", "μμ", "ii", "μμ" };
            Assert.Equal(1, span.Count(new string[] { "ii", "II" }));
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
            ReadOnlySpan<string> theStrings = spanInput;
            Assert.Equal(expected, theStrings.Count((string)null));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.CountNullRosData), MemberType = typeof(TestHelpers))]
        public static void CountNull_RosString(string[] spanInput, int expected)
        {
            ReadOnlySpan<string> theStrings = spanInput;
            ReadOnlySpan<string> target = new string[] { null, "9" };
            Assert.Equal(expected, theStrings.Count(target));
        }
    }
}
