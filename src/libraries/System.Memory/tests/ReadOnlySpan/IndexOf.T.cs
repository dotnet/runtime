// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthIndexOf()
        {
            ReadOnlySpan<int> sp = new ReadOnlySpan<int>(Array.Empty<int>());
            int idx = sp.IndexOf(0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void TestMatch()
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
                    int idx = span.IndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestMultipleMatch()
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
                int idx = span.IndexOf(5555);
                Assert.Equal(length - 2, idx);
            }
        }

        [Fact]
        public static void OnNoMatchMakeSureEveryElementIsCompared()
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
                int idx = span.IndexOf(new TInt(9999, log));
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
        public static void MakeSureNoChecksGoOutOfRange()
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

                ReadOnlySpan<TInt> span = new ReadOnlySpan<TInt>(a, GuardLength, length);
                int idx = span.IndexOf(new TInt(9999, checkForOutOfRangeAccess));
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOf_String()
        {
            ReadOnlySpan<string> sp = new ReadOnlySpan<string>(Array.Empty<string>());
            int idx = sp.IndexOf("a");
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void TestMatchIndexOf_String()
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
                    int idx = span.IndexOf(target);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestNoMatchIndexOf_String()
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

                int idx = span.IndexOf(target);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOf_String()
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
                int idx = span.IndexOf("5555");
                Assert.Equal(length - 2, idx);
            }
        }
    }
}
