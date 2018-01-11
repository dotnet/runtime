// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthLastIndexOfAny_TwoByte()
        {
            var sp = new Span<int>(Array.Empty<int>());
            int idx = sp.LastIndexOfAny(0, 0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_TwoByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                var span = new Span<int>(a);

                int[] targets = { default, 99 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    int target0 = targets[index];
                    int target1 = targets[(index + 1) % 2];
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(span.Length - 1, idx);
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
                var span = new Span<int>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = 0;
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = a[targetIndex + 1];
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(targetIndex + 1, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    int target0 = 0;
                    int target1 = a[targetIndex + 1];
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(targetIndex + 1, idx);
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
                var span = new Span<int>(a);

                int idx = span.LastIndexOfAny(target0, target1);
                Assert.Equal(-1, idx);
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

                var span = new Span<int>(a);
                int idx = span.LastIndexOfAny(200, 200);
                Assert.Equal(length - 1, idx);
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
                var span = new Span<int>(a, 1, length - 1);
                int index = span.LastIndexOfAny(99, 98);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var span = new Span<int>(a, 1, length - 1);
                int index = span.LastIndexOfAny(99, 99);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOf_ThreeByte()
        {
            var sp = new Span<int>(Array.Empty<int>());
            int idx = sp.LastIndexOfAny(0, 0, 0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_ThreeByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                var span = new Span<int>(a);

                int[] targets = { default, 99, 98 };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    int target0 = targets[index];
                    int target1 = targets[(index + 1) % 2];
                    int target2 = targets[(index + 1) % 3];
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(span.Length - 1, idx);
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
                var span = new Span<int>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = 0;
                    int target2 = 0;
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    int target0 = a[targetIndex];
                    int target1 = a[targetIndex + 1];
                    int target2 = a[targetIndex + 2];
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(targetIndex + 2, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    int target0 = 0;
                    int target1 = 0;
                    int target2 = a[targetIndex + 2];
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(targetIndex + 2, idx);
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
                var span = new Span<int>(a);

                int idx = span.LastIndexOfAny(target0, target1, target2);
                Assert.Equal(-1, idx);
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

                var span = new Span<int>(a);
                int idx = span.LastIndexOfAny(200, 200, 200);
                Assert.Equal(length - 1, idx);
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
                var span = new Span<int>(a, 1, length - 1);
                int index = span.LastIndexOfAny(99, 98, 99);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var span = new Span<int>(a, 1, length - 1);
                int index = span.LastIndexOfAny(99, 99, 99);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOfAny_ManyByte()
        {
            var sp = new Span<int>(Array.Empty<int>());
            var values = new ReadOnlySpan<int>(new int[] { 0, 0, 0, 0 });
            int idx = sp.LastIndexOfAny(values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<int>(new int[] { });
            idx = sp.LastIndexOfAny(values);
            Assert.Equal(0, idx);
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_ManyByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                var span = new Span<int>(a);

                var values = new ReadOnlySpan<int>(new int[] { default, 99, 98, 0 });

                for (int i = 0; i < length; i++)
                {
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(span.Length - 1, idx);
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
                var span = new Span<int>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new ReadOnlySpan<int>(new int[] { a[targetIndex], 0, 0, 0 });
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new ReadOnlySpan<int>(new int[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3] });
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(targetIndex + 3, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new ReadOnlySpan<int>(new int[] { 0, 0, 0, a[targetIndex + 3] });
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(targetIndex + 3, idx);
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
                var span = new Span<int>(a);

                var targets = new int[length * 2];
                for (int i = 0; i < targets.Length; i++)
                {
                    if (i == length + 1)
                    {
                        continue;
                    }
                    targets[i] = rnd.Next(1, 255);
                }

                var values = new ReadOnlySpan<int>(targets);
                int idx = span.LastIndexOfAny(values);
                Assert.Equal(expectedIndex, idx);
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
                var span = new Span<int>(a);
                var values = new ReadOnlySpan<int>(targets);

                int idx = span.LastIndexOfAny(values);
                Assert.Equal(-1, idx);
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
                var span = new Span<int>(a);
                var values = new ReadOnlySpan<int>(targets);

                int idx = span.LastIndexOfAny(values);
                Assert.Equal(-1, idx);
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

                var span = new Span<int>(a);
                var values = new ReadOnlySpan<int>(new int[] { 200, 200, 200, 200, 200, 200, 200, 200, 200 });
                int idx = span.LastIndexOfAny(values);
                Assert.Equal(length - 1, idx);
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
                var span = new Span<int>(a, 1, length - 1);
                var values = new ReadOnlySpan<int>(new int[] { 99, 98, 99, 98, 99, 98 });
                int index = span.LastIndexOfAny(values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var span = new Span<int>(a, 1, length - 1);
                var values = new ReadOnlySpan<int>(new int[] { 99, 99, 99, 99, 99, 99 });
                int index = span.LastIndexOfAny(values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOfAny_String_TwoByte()
        {
            var sp = new Span<string>(Array.Empty<string>());
            int idx = sp.LastIndexOfAny("0", "0");
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_String_TwoByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var span = new Span<string>(a);
                span.Fill("");

                string[] targets = { "", "99" };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    string target0 = targets[index];
                    string target1 = targets[(index + 1) % 2];
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(span.Length - 1, idx);
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
                var span = new Span<string>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = "0";
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = a[targetIndex + 1];
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(targetIndex + 1, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    string target0 = "0";
                    string target1 = a[targetIndex + 1];
                    int idx = span.LastIndexOfAny(target0, target1);
                    Assert.Equal(targetIndex + 1, idx);
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
                var span = new Span<string>(a);

                int idx = span.LastIndexOfAny(target0, target1);
                Assert.Equal(-1, idx);
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

                var span = new Span<string>(a);
                int idx = span.LastIndexOfAny("200", "200");
                Assert.Equal(length - 1, idx);
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
                var span = new Span<string>(a, 1, length - 1);
                int index = span.LastIndexOfAny("99", "98");
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";
                var span = new Span<string>(a, 1, length - 1);
                int index = span.LastIndexOfAny("99", "99");
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOf_String_ThreeByte()
        {
            var sp = new Span<string>(Array.Empty<string>());
            int idx = sp.LastIndexOfAny("0", "0", "0");
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_String_ThreeByte()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var span = new Span<string>(a);
                span.Fill("");

                string[] targets = { "", "99", "98" };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    string target0 = targets[index];
                    string target1 = targets[(index + 1) % 2];
                    string target2 = targets[(index + 1) % 3];
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(span.Length - 1, idx);
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
                var span = new Span<string>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = "0";
                    string target2 = "0";
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    string target0 = a[targetIndex];
                    string target1 = a[targetIndex + 1];
                    string target2 = a[targetIndex + 2];
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(targetIndex + 2, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    string target0 = "0";
                    string target1 = "0";
                    string target2 = a[targetIndex + 2];
                    int idx = span.LastIndexOfAny(target0, target1, target2);
                    Assert.Equal(targetIndex + 2, idx);
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
                var span = new Span<string>(a);

                int idx = span.LastIndexOfAny(target0, target1, target2);
                Assert.Equal(-1, idx);
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

                var span = new Span<string>(a);
                int idx = span.LastIndexOfAny("200", "200", "200");
                Assert.Equal(length - 1, idx);
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
                var span = new Span<string>(a, 1, length - 1);
                int index = span.LastIndexOfAny("99", "98", "99");
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";
                var span = new Span<string>(a, 1, length - 1);
                int index = span.LastIndexOfAny("99", "99", "99");
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthLastIndexOfAny_String_ManyByte()
        {
            var sp = new Span<string>(Array.Empty<string>());
            var values = new ReadOnlySpan<string>(new string[] { "0", "0", "0", "0" });
            int idx = sp.LastIndexOfAny(values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<string>(new string[] { });
            idx = sp.LastIndexOfAny(values);
            Assert.Equal(0, idx);
        }

        [Fact]
        public static void DefaultFilledLastIndexOfAny_String_ManyByte()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var span = new Span<string>(a);
                span.Fill("");

                var values = new ReadOnlySpan<string>(new string[] { "", "99", "98", "0" });

                for (int i = 0; i < length; i++)
                {
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(span.Length - 1, idx);
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
                var span = new Span<string>(a);

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new ReadOnlySpan<string>(new string[] { a[targetIndex], "0", "0", "0" });
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new ReadOnlySpan<string>(new string[] { a[targetIndex], a[targetIndex + 1], a[targetIndex + 2], a[targetIndex + 3] });
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(targetIndex + 3, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    var values = new ReadOnlySpan<string>(new string[] { "0", "0", "0", a[targetIndex + 3] });
                    int idx = span.LastIndexOfAny(values);
                    Assert.Equal(targetIndex + 3, idx);
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
                var span = new Span<string>(a);

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

                var values = new ReadOnlySpan<string>(targets);
                int idx = span.LastIndexOfAny(values);
                Assert.Equal(expectedIndex, idx);
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
                var span = new Span<string>(a);
                var values = new ReadOnlySpan<string>(targets);

                int idx = span.LastIndexOfAny(values);
                Assert.Equal(-1, idx);
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
                var span = new Span<string>(a);
                var values = new ReadOnlySpan<string>(targets);

                int idx = span.LastIndexOfAny(values);
                Assert.Equal(-1, idx);
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

                var span = new Span<string>(a);
                var values = new ReadOnlySpan<string>(new string[] { "200", "200", "200", "200", "200", "200", "200", "200", "200" });
                int idx = span.LastIndexOfAny(values);
                Assert.Equal(length - 1, idx);
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
                var span = new Span<string>(a, 1, length - 1);
                var values = new ReadOnlySpan<string>(new string[] { "99", "98", "99", "98", "99", "98" });
                int index = span.LastIndexOfAny(values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";
                var span = new Span<string>(a, 1, length - 1);
                var values = new ReadOnlySpan<string>(new string[] { "99", "99", "99", "99", "99", "99" });
                int index = span.LastIndexOfAny(values);
                Assert.Equal(-1, index);
            }
        }
    }
}
