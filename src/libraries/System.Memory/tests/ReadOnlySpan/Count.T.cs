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
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(Array.Empty<int>());

            int count = span.Count(0);
            Assert.Equal(0, count);
        }
        
        [Fact]
        public static void ZeroLengthCount_RosInt()
        {
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(Array.Empty<int>());

            int count = span.Count<int>(new ReadOnlySpan<int>(new int[] { 0, 0 }));
            Assert.Equal(0, count);
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
                    int target = a[targetIndex];
                    int count = span.Count(target);
                    Assert.Equal(1, count);
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
                    ReadOnlySpan<int> target = stackalloc int[] { a[targetIndex], a[targetIndex + 1] };

                    int count = span.Count(target);
                    Assert.Equal(1, count);
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

                a[length - 1] = 5555;
                a[length - 2] = 5555;

                ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);
                int count = span.Count(5555);
                Assert.Equal(2, count);
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
                a[0] = 5555;
                a[1] = 5555;
                a[length - 1] = 5555;
                a[length - 2] = 5555;

                ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);
                ReadOnlySpan<int> target = new int[] { 5555, 5555 };

                int count = span.Count<int>(target);
                Assert.Equal(2, count);
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
                int count = span.Count(new TInt(9999, log));
                Assert.Equal(0, count);

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
                ReadOnlySpan<TInt> target = new TInt[] { new TInt(9999, log), new TInt(10000, log) };

                int count = span.Count(target);
                Assert.Equal(0, count);

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

                ReadOnlySpan<TInt> span = new ReadOnlySpan<TInt>(a, GuardLength, length);
                int count = span.Count(new TInt(9999, checkForOutOfRangeAccess));
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void MakeSureNoChecksForCountGoOutOfRange_RosTInt()
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

                ReadOnlySpan<TInt> span = new ReadOnlySpan<TInt>(a, GuardLength, length);
                ReadOnlySpan<TInt> target = new TInt[] { new TInt(9999, checkForOutOfRangeAccess), new TInt(9999, checkForOutOfRangeAccess) };

                int count = span.Count(target);
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public static void ZeroLengthCount_String()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(Array.Empty<string>());
            int count = span.Count("a");
            Assert.Equal(0, count);
        }
        
        [Fact]
        public static void ZeroLengthCount_RosString()
        {
            ReadOnlySpan<string> span = new ReadOnlySpan<string>(Array.Empty<string>());
            ReadOnlySpan<string> target = new [] { "a", "b" };
            int count = span.Count(target);
            Assert.Equal(0, count);
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
                    string target = a[targetIndex];
                    int count = span.Count(target);
                    Assert.Equal(1, count);
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
                    ReadOnlySpan<string> target = new string[] { a[targetIndex], a[targetIndex + 1] };

                    int count = span.Count(target);
                    Assert.Equal(1, count);
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
                string target = (rnd.Next(0, 256)).ToString();
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == target ? (target + 1) : val;
                }
                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);

                int count = span.Count(target);
                Assert.Equal(0, count);
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

                int count = span.Count(target);
                Assert.Equal(0, count);
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

                a[length - 1] = "5555";
                a[length - 2] = "5555";

                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);
                int count = span.Count("5555");
                Assert.Equal(2, count);
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
                
                a[0] = "5555";
                a[1] = "5555";
                a[length - 1] = "5555";
                a[length - 2] = "5555";

                ReadOnlySpan<string> span = new ReadOnlySpan<string>(a);
                ReadOnlySpan<string> target = new string[] { "5555", "5555" };
                int count = span.Count(target);
                Assert.Equal(2, count);
            }
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
