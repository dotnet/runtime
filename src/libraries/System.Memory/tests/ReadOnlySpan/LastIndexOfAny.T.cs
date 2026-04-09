// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthLastIndexOfAny_TwoByte()
        {
            Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(0, 0));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(0, 0, comparer)));
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_TwoByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];

                int[] targets = { default, 99 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    int target0 = targets[index];
                    int target1 = targets[(index + 1) % 2];

                    Assert.Equal(a.Length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(a.Length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestMatchLastIndexOfAny_TwoByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = i + 1;
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = 0;

                    Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<int>()));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex + 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex + 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<int>()));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    int target0 = 0;
                    int target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex + 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex + 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOfAny_TwoByte()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                int target0 = rnd.Next(1, 256);
                int target1 = rnd.Next(1, 256);

                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOfAny_TwoByte()
        {
            for (int length = 3; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    int val = i + 1;
                    a[i] = val == 200 ? 201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;
                a[length - 3] = 200;

                Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(200, 200));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(200, 200, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(200, 200, GetFalseEqualityComparer<int>()));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOfAny_TwoByte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 98;

                Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 98));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 98, comparer)));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;

                Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 99));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 99, comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthIndexOf_ThreeByte()
        {
            Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(0, 0, 0));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(0, 0, 0, comparer)));
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_ThreeByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];

                int[] targets = { default, 99, 98 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    int target0 = targets[index];
                    int target1 = targets[(index + 1) % 2];
                    int target2 = targets[(index + 1) % 3];

                    Assert.Equal(a.Length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(a.Length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestMatchLastIndexOfAny_ThreeByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = i + 1;
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = 0;
                    int target2 = 0;

                    Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<int>()));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = a[targetIndex + 1];
                    int target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex + 2, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex + 2, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<int>()));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    int target0 = 0;
                    int target1 = 0;
                    int target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex + 2, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex + 2, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOfAny_ThreeByte()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                int target0 = rnd.Next(1, 256);
                int target1 = rnd.Next(1, 256);
                int target2 = rnd.Next(1, 256);

                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(target0, target1, target2, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOfAny_ThreeByte()
        {
            for (int length = 4; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    int val = i + 1;
                    a[i] = val == 200 ? 201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;
                a[length - 3] = 200;
                a[length - 4] = 200;

                Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(200, 200, 200));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(200, 200, 200, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(200, 200, 200, GetFalseEqualityComparer<int>()));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOfAny_ThreeByte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 98;

                Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 98, 99));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 98, 99, comparer)));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;

                Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 99, 99));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(99, 99, 99, comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOfAny_ManyByte()
        {
            var values = new int[] { 0, 0, 0, 0 };
            Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(values));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(values, comparer)));

            values = new int[] { };
            Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(values));
            Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(Array.Empty<int>()).LastIndexOfAny(values, comparer)));
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_ManyByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];

                var values = new int[] { default, 99, 98, 0 };

                for (int i = 0; i < length; i++)
                {
                    Assert.Equal(a.Length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(a.Length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestMatchLastIndexOfAny_ManyByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = i + 1;
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new int[] { a[targetIndex], 0, 0, 0 };

                    Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, GetFalseEqualityComparer<int>()));
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new int[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3] };

                    Assert.Equal(targetIndex + 3, new ReadOnlySpan<int>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex + 3, new ReadOnlySpan<int>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, GetFalseEqualityComparer<int>()));
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new int[] { 0, 0, 0, a[targetIndex + 3] };

                    Assert.Equal(targetIndex + 3, new ReadOnlySpan<int>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(targetIndex + 3, new ReadOnlySpan<int>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, GetFalseEqualityComparer<int>()));
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerLastIndexOfAny_ManyByte()
        {
            var rnd = new Random(42);
            for (int length = 2; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                int expectedIndex = length / 2;
                for (int i = 0; i < length; i++)
                {
                    if (i == expectedIndex)
                    {
                        continue;
                    }
                    a[i] = 255;
                }

                var targets = new int[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i == length + 1)
                    {
                        continue;
                    }
                    targets[i] = rnd.Next(1, 255);
                }

                Assert.Equal(expectedIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(targets));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(expectedIndex, new ReadOnlySpan<int>(a).LastIndexOfAny(targets, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(targets, GetFalseEqualityComparer<int>()));
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOfAny_ManyByte()
        {
            var rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                var targets = new int[length];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = rnd.Next(1, 256);
                }

                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(targets));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(targets, comparer)));
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerLastIndexOfAny_ManyByte()
        {
            var rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                var targets = new int[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = rnd.Next(1, 256);
                }

                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(targets));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(targets, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOfAny_ManyByte()
        {
            for (int length = 5; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                for (int i = 0; i < length; i++)
                {
                    int val = i + 1;
                    a[i] = val == 200 ? 201 : val;
                }

                a[length - 1] = 200;
                a[length - 2] = 200;
                a[length - 3] = 200;
                a[length - 4] = 200;
                a[length - 5] = 200;

                var values = new int[] { 200, 200, 200, 200, 200, 200, 200, 200, 200 };

                Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(values));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<int>(a).LastIndexOfAny(values, GetFalseEqualityComparer<int>()));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOfAny_ManyByte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                var values = new int[] { 99, 98, 99, 98, 99, 98 };

                Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(values));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(values, comparer)));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var values = new int[] { 99, 99, 99, 99, 99, 99 };

                Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(values));
                Assert.All(GetDefaultEqualityComparers<int>(), comparer => Assert.Equal(-1, new ReadOnlySpan<int>(a, 1, length - 1).LastIndexOfAny(values, comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOfAny_String_TwoByte()
        {
            Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOfAny("0", "0"));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOfAny("0", "0", comparer)));
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_String_TwoByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                Array.Fill(a, "");

                string[] targets = { "", "99" };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    string target0 = targets[index];
                    string target1 = targets[(index + 1) % 2];

                    Assert.Equal(a.Length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(a.Length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestMatchLastIndexOfAny_String_TwoByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (i + 1).ToString();
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = "0";

                    Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<string>()));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex + 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex + 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<string>()));
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    string target0 = "0";
                    string target1 = a[targetIndex + 1];

                    Assert.Equal(targetIndex + 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex + 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOfAny_String_TwoByte()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                string target0 = rnd.Next(1, 256).ToString();
                string target1 = rnd.Next(1, 256).ToString();

                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOfAny_String_TwoByte()
        {
            for (int length = 3; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == "200" ? "201" : val;
                }

                a[length - 1] = "200";
                a[length - 2] = "200";
                a[length - 3] = "200";

                Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny("200", "200"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny("200", "200", comparer)));
                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny("200", "200", GetFalseEqualityComparer<string>()));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOfAny_String_TwoByte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "98";

                Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "98"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "98", comparer)));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";

                Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "99"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "99", comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthIndexOf_String_ThreeByte()
        {
            Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOfAny("0", "0", "0"));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOfAny("0", "0", "0", comparer)));
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_String_ThreeByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                Array.Fill(a, "");

                string[] targets = { "", "99", "98" };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    string target0 = targets[index];
                    string target1 = targets[(index + 1) % 2];
                    string target2 = targets[(index + 1) % 3];

                    Assert.Equal(a.Length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(a.Length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestMatchLastIndexOfAny_String_ThreeByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (i + 1).ToString();
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = "0";
                    string target2 = "0";

                    Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<string>()));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = a[targetIndex + 1];
                    string target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex + 2, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex + 2, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<string>()));
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    string target0 = "0";
                    string target1 = "0";
                    string target2 = a[targetIndex + 2];

                    Assert.Equal(targetIndex + 2, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex + 2, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOfAny_String_ThreeByte()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                string target0 = rnd.Next(1, 256).ToString();
                string target1 = rnd.Next(1, 256).ToString();
                string target2 = rnd.Next(1, 256).ToString();

                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(target0, target1, target2, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOfAny_String_ThreeByte()
        {
            for (int length = 4; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == "200" ? "201" : val;
                }

                a[length - 1] = "200";
                a[length - 2] = "200";
                a[length - 3] = "200";
                a[length - 4] = "200";

                Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny("200", "200", "200"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny("200", "200", "200", comparer)));
                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny("200", "200", "200", GetFalseEqualityComparer<string>()));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOfAny_String_ThreeByte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "98";

                Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "98", "99"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "98", "99", comparer)));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";

                Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "99", "99"));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny("99", "99", "99", comparer)));
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOfAny_String_ManyByte()
        {
            var values = new string[] { "0", "0", "0", "0" };

            Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOfAny(values));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(Array.Empty<string>()).LastIndexOfAny(values, comparer)));

            Assert.Equal(-1, new ReadOnlySpan<string>(new string[] { }).LastIndexOfAny(values));
            Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(new string[] { }).LastIndexOfAny(values, comparer)));
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_String_ManyByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                Array.Fill(a, "");

                var values = new string[] { "", "99", "98", "0" };

                for (int i = 0; i < length; i++)
                {
                    Assert.Equal(a.Length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(a.Length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestMatchLastIndexOfAny_String_ManyByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    a[i] = (i + 1).ToString();
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new string[] { a[targetIndex], "0", "0", "0" };

                    Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, GetFalseEqualityComparer<string>()));
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new string[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3] };

                    Assert.Equal(targetIndex + 3, new ReadOnlySpan<string>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex + 3, new ReadOnlySpan<string>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, GetFalseEqualityComparer<string>()));
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new string[] { "0", "0", "0", a[targetIndex + 3] };

                    Assert.Equal(targetIndex + 3, new ReadOnlySpan<string>(a).LastIndexOfAny(values));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(targetIndex + 3, new ReadOnlySpan<string>(a).LastIndexOfAny(values, comparer)));
                    Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, GetFalseEqualityComparer<string>()));
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerLastIndexOfAny_String_ManyByte()
        {
            var rnd = new Random(42);
            for (int length = 2; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                int expectedIndex = length / 2;
                for (int i = 0; i < length; i++)
                {
                    if (i == expectedIndex)
                    {
                        a[i] = "val";
                        continue;
                    }
                    a[i] = "255";
                }

                var targets = new string[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i == length + 1)
                    {
                        targets[i] = "val";
                        continue;
                    }
                    targets[i] = rnd.Next(1, 255).ToString();
                }

                Assert.Equal(expectedIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(targets));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(expectedIndex, new ReadOnlySpan<string>(a).LastIndexOfAny(targets, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(targets, GetFalseEqualityComparer<string>()));
            }
        }

        [Fact]
        public static void TestNoMatchLastIndexOfAny_String_ManyByte()
        {
            var rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var targets = new string[length];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = rnd.Next(1, 256).ToString();
                }

                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(targets));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(targets, comparer)));
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerLastIndexOfAny_String_ManyByte()
        {
            var rnd = new Random(42);
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var targets = new string[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    targets[i] = rnd.Next(1, 256).ToString();
                }

                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(targets));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(targets, comparer)));
            }
        }

        [Fact]
        public static void TestMultipleMatchLastIndexOfAny_String_ManyByte()
        {
            for (int length = 5; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                for (int i = 0; i < length; i++)
                {
                    string val = (i + 1).ToString();
                    a[i] = val == "200" ? "201" : val;
                }

                a[length - 1] = "200";
                a[length - 2] = "200";
                a[length - 3] = "200";
                a[length - 4] = "200";
                a[length - 5] = "200";

                var values = new string[] { "200", "200", "200", "200", "200", "200", "200", "200", "200" };

                Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(values));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(length - 1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, comparer)));
                Assert.Equal(-1, new ReadOnlySpan<string>(a).LastIndexOfAny(values, GetFalseEqualityComparer<string>()));
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeLastIndexOfAny_String_ManyByte()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "98";

                var values = new string[] { "99", "98", "99", "98", "99", "98" };

                Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny(values));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny(values, comparer)));
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";

                var values = new string[] { "99", "99", "99", "99", "99", "99" };

                Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny(values));
                Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(-1, new ReadOnlySpan<string>(a, 1, length - 1).LastIndexOfAny(values, comparer)));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.LastIndexOfAnyNullSequenceData), MemberType = typeof(TestHelpers))]
        public static void LastIndexOfAnyNullSequence_String(string[] spanInput, string[] searchInput, int expected)
        {
            Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny(searchInput));
            Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny((ReadOnlySpan<string>)searchInput));

            Assert.All(GetDefaultEqualityComparers<string>(), comparer =>
            {
                Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny(searchInput, comparer));
                Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny((ReadOnlySpan<string>)searchInput, comparer));
            });

            if (searchInput != null)
            {
                if (searchInput.Length >= 3)
                {
                    Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny(searchInput[0], searchInput[1], searchInput[2]));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny(searchInput[0], searchInput[1], searchInput[2], comparer)));
                }

                if (searchInput.Length >= 2)
                {
                    Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny(searchInput[0], searchInput[1]));
                    Assert.All(GetDefaultEqualityComparers<string>(), comparer => Assert.Equal(expected, new ReadOnlySpan<string>(spanInput).LastIndexOfAny(searchInput[0], searchInput[1], comparer)));
                }
            }
        }
    }
}
