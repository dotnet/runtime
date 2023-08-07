// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthIndexOfAny_TwoInteger()
        {
            var sp = new Span<int>(Array.Empty<int>());
            int idx = IndexOfAny(sp, 0, 0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfAny_TwoInteger()
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
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchIndexOfAny_TwoInteger()
        {
            var rnd = new Random(42);

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
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    int target0 = a[targetIndex + index];
                    int target1 = a[targetIndex + (index + 1) % 2];
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target0 = 0;
                    int target1 = a[targetIndex];
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestNoMatchIndexOfAny_TwoInteger()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                int target0 = rnd.Next(1, 256);
                int target1 = rnd.Next(1, 256);
                var span = new Span<int>(a);

                int idx = IndexOfAny(span, target0, target1);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOfAny_TwoInteger()
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
                int idx = IndexOfAny(span, 200, 200);
                Assert.Equal(length - 3, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeIndexOfAny_TwoInteger()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                var span = new Span<int>(a, 1, length - 1);
                int index = IndexOfAny(span, 99, 98);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var span = new Span<int>(a, 1, length - 1);
                int index = IndexOfAny(span, 99, 99);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfAny_ThreeInteger()
        {
            var sp = new Span<int>(Array.Empty<int>());
            int idx = IndexOfAny(sp, 0, 0, 0);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfAny_ThreeInteger()
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
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchIndexOfAny_ThreeInteger()
        {
            var rnd = new Random(42);

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
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    int index = rnd.Next(0, 3);
                    int target0 = a[targetIndex + index];
                    int target1 = a[targetIndex + (index + 1) % 2];
                    int target2 = a[targetIndex + (index + 1) % 3];
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    int target0 = 0;
                    int target1 = 0;
                    int target2 = a[targetIndex];
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestNoMatchIndexOfAny_ThreeInteger()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                int target0 = rnd.Next(1, 256);
                int target1 = rnd.Next(1, 256);
                int target2 = rnd.Next(1, 256);
                var span = new Span<int>(a);

                int idx = IndexOfAny(span, target0, target1, target2);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOfAny_ThreeInteger()
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
                int idx = IndexOfAny(span, 200, 200, 200);
                Assert.Equal(length - 4, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeIndexOfAny_ThreeInteger()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                var span = new Span<int>(a, 1, length - 1);
                int index = IndexOfAny(span, 99, 98, 99);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var span = new Span<int>(a, 1, length - 1);
                int index = IndexOfAny(span, 99, 99, 99);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfAny_ManyInteger()
        {
            var sp = new Span<int>(Array.Empty<int>());
            var values = new ReadOnlySpan<int>(new int[] { 0, 0, 0, 0 });
            int idx = IndexOfAny(sp, values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<int>(new int[] { });
            idx = IndexOfAny(sp, values);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfAny_ManyInteger()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new int[length];
                var span = new Span<int>(a);

                var values = new ReadOnlySpan<int>(new int[] { default, 99, 98, 0 });

                for (int i = 0; i < length; i++)
                {
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchIndexOfAny_ManyInteger()
        {
            var rnd = new Random(42);

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
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    int index = rnd.Next(0, 4) == 0 ? 0 : 1;
                    var values = new ReadOnlySpan<int>(new int[]
                        {
                            a[targetIndex + index],
                            a[targetIndex + (index + 1) % 2],
                            a[targetIndex + (index + 1) % 3],
                            a[targetIndex + (index + 1) % 4]
                        });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new ReadOnlySpan<int>(new int[] { 0, 0, 0, a[targetIndex] });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerIndexOfAny_ManyInteger()
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
                int idx = IndexOfAny(span, values);
                Assert.Equal(expectedIndex, idx);
            }
        }

        [Fact]
        public static void TestNoMatchIndexOfAny_ManyInteger()
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

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerIndexOfAny_ManyInteger()
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

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOfAny_ManyInteger()
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
                int idx = IndexOfAny(span, values);
                Assert.Equal(length - 5, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeIndexOfAny_ManyInteger()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 98;
                var span = new Span<int>(a, 1, length - 1);
                var values = new Span<int>(new int[] { 99, 98, 99, 98, 99, 98 });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new int[length + 2];
                a[0] = 99;
                a[length + 1] = 99;
                var span = new Span<int>(a, 1, length - 1);
                var values = new ReadOnlySpan<int>(new int[] { 99, 99, 99, 99, 99, 99 });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfAny_TwoString()
        {
            var sp = new Span<string>(Array.Empty<string>());
            int idx = IndexOfAny(sp, "0", "0");
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfAny_TwoString()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var tempSpan = new Span<string>(a);
                tempSpan.Fill("");
                Span<string> span = tempSpan;

                string[] targets = { "", "99" };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    string target0 = targets[index];
                    string target1 = targets[(index + 1) % 2];
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchIndexOfAny_TwoString()
        {
            var rnd = new Random(42);

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
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    int index = rnd.Next(0, 2) == 0 ? 0 : 1;
                    string target0 = a[targetIndex + index];
                    string target1 = a[targetIndex + (index + 1) % 2];
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 1; targetIndex++)
                {
                    string target0 = "0";
                    string target1 = a[targetIndex + 1];
                    int idx = IndexOfAny(span, target0, target1);
                    Assert.Equal(targetIndex + 1, idx);
                }
            }
        }

        [Fact]
        public static void TestNoMatchIndexOfAny_TwoString()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                string target0 = rnd.Next(1, 256).ToString();
                string target1 = rnd.Next(1, 256).ToString();
                var span = new Span<string>(a);

                int idx = IndexOfAny(span, target0, target1);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOfAny_TwoString()
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
                int idx = IndexOfAny(span, "200", "200");
                Assert.Equal(length - 3, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeIndexOfAny_TwoString()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "98";
                var span = new Span<string>(a, 1, length - 1);
                int index = IndexOfAny(span, "99", "98");
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";
                var span = new Span<string>(a, 1, length - 1);
                int index = IndexOfAny(span, "99", "99");
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOf_ThreeString()
        {
            var sp = new Span<string>(Array.Empty<string>());
            int idx = IndexOfAny(sp, "0", "0", "0");
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfAny_ThreeString()
        {
            var rnd = new Random(42);

            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var tempSpan = new Span<string>(a);
                tempSpan.Fill("");
                Span<string> span = tempSpan;

                string[] targets = { "", "99", "98" };

                for (int i = 0; i < length; i++)
                {
                    int index = rnd.Next(0, 3);
                    string target0 = targets[index];
                    string target1 = targets[(index + 1) % 2];
                    string target2 = targets[(index + 1) % 3];
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchIndexOfAny_ThreeString()
        {
            Random rnd = new Random(42);

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
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 2; targetIndex++)
                {
                    int index = rnd.Next(0, 3) == 0 ? 0 : 1;
                    string target0 = a[targetIndex + index];
                    string target1 = a[targetIndex + (index + 1) % 2];
                    string target2 = a[targetIndex + (index + 1) % 3];
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    string target0 = "0";
                    string target1 = "0";
                    string target2 = a[targetIndex];
                    int idx = IndexOfAny(span, target0, target1, target2);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestNoMatchIndexOfAny_ThreeString()
        {
            var rnd = new Random(42);
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                string target0 = rnd.Next(1, 256).ToString();
                string target1 = rnd.Next(1, 256).ToString();
                string target2 = rnd.Next(1, 256).ToString();
                var span = new Span<string>(a);

                int idx = IndexOfAny(span, target0, target1, target2);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOfAny_ThreeString()
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
                int idx = IndexOfAny(span, "200", "200", "200");
                Assert.Equal(length - 4, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeIndexOfAny_ThreeString()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "98";
                var span = new Span<string>(a, 1, length - 1);
                int index = IndexOfAny(span, "99", "98", "99");
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";
                var span = new Span<string>(a, 1, length - 1);
                int index = IndexOfAny(span, "99", "99", "99");
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void ZeroLengthIndexOfAny_ManyString()
        {
            var sp = new Span<string>(Array.Empty<string>());
            var values = new ReadOnlySpan<string>(new string[] { "0", "0", "0", "0" });
            int idx = IndexOfAny(sp, values);
            Assert.Equal(-1, idx);

            values = new ReadOnlySpan<string>(new string[] { });
            idx = IndexOfAny(sp, values);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public static void DefaultFilledIndexOfAny_ManyString()
        {
            for (int length = 0; length < byte.MaxValue; length++)
            {
                var a = new string[length];
                var tempSpan = new Span<string>(a);
                tempSpan.Fill("");
                Span<string> span = tempSpan;

                var values = new ReadOnlySpan<string>(new string[] { "", "99", "98", "0" });

                for (int i = 0; i < length; i++)
                {
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(0, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchIndexOfAny_ManyString()
        {
            Random rnd = new Random(42);

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
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length - 3; targetIndex++)
                {
                    int index = rnd.Next(0, 4) == 0 ? 0 : 1;
                    var values = new ReadOnlySpan<string>(new string[]
                    {
                        a[targetIndex + index],
                        a[targetIndex + (index + 1) % 2],
                        a[targetIndex + (index + 1) % 3],
                        a[targetIndex + (index + 1) % 4]
                    });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }

                for (int targetIndex = 0; targetIndex < length; targetIndex++)
                {
                    var values = new ReadOnlySpan<string>(new string[] { "0", "0", "0", a[targetIndex] });
                    int idx = IndexOfAny(span, values);
                    Assert.Equal(targetIndex, idx);
                }
            }
        }

        [Fact]
        public static void TestMatchValuesLargerIndexOfAny_ManyString()
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
                int idx = IndexOfAny(span, values);
                Assert.Equal(expectedIndex, idx);
            }
        }

        [Fact]
        public static void TestNoMatchIndexOfAny_ManyString()
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

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestNoMatchValuesLargerIndexOfAny_ManyString()
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

                int idx = IndexOfAny(span, values);
                Assert.Equal(-1, idx);
            }
        }

        [Fact]
        public static void TestMultipleMatchIndexOfAny_ManyString()
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
                int idx = IndexOfAny(span, values);
                Assert.Equal(length - 5, idx);
            }
        }

        [Fact]
        public static void MakeSureNoChecksGoOutOfRangeIndexOfAny_ManyString()
        {
            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "98";
                var span = new Span<string>(a, 1, length - 1);
                var values = new ReadOnlySpan<string>(new string[] { "99", "98", "99", "98", "99", "98" });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }

            for (int length = 1; length < byte.MaxValue; length++)
            {
                var a = new string[length + 2];
                a[0] = "99";
                a[length + 1] = "99";
                var span = new Span<string>(a, 1, length - 1);
                var values = new ReadOnlySpan<string>(new string[] { "99", "99", "99", "99", "99", "99" });
                int index = IndexOfAny(span, values);
                Assert.Equal(-1, index);
            }
        }

        [Fact]
        public static void IndexOfAnyExceptWorksOnAvx512_Integer()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/89512

            var arr = new int[32];
            arr[1] = 1;
            Assert.Equal(1, arr.AsSpan().IndexOfAnyExcept(0));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.IndexOfAnyNullSequenceData), MemberType = typeof(TestHelpers))]
        public static void IndexOfAnyNullSequence_String(string[] spanInput, string[] searchInput, int expected)
        {
            Span<string> theStrings = spanInput;
            Assert.Equal(expected, IndexOfAny(theStrings, searchInput));

            if (searchInput != null)
            {
                if (searchInput.Length >= 3)
                {
                    Assert.Equal(expected, IndexOfAny(theStrings, searchInput[0], searchInput[1], searchInput[2]));
                }

                if (searchInput.Length >= 2)
                {
                    Assert.Equal(expected, IndexOfAny(theStrings, searchInput[0], searchInput[1]));
                }
            }
        }

        private static int IndexOf<T>(Span<T> span, T value) where T : IEquatable<T>?
        {
            int index = span.IndexOf(value);
            Assert.Equal(index, ((ReadOnlySpan<T>)span).IndexOf(value));

            Assert.Equal(index >= 0, span.Contains(value));
            Assert.Equal(index >= 0, ((ReadOnlySpan<T>)span).Contains(value));

            AssertSearchValues(span, new ReadOnlySpan<T>(in value), index);
            return index;
        }

        private static int IndexOfAny<T>(Span<T> span, T value0, T value1) where T : IEquatable<T>?
        {
            int index = span.IndexOfAny(value0, value1);
            Assert.Equal(index, ((ReadOnlySpan<T>)span).IndexOfAny(value0, value1));

            Assert.Equal(index >= 0, span.ContainsAny(value0, value1));
            Assert.Equal(index >= 0, ((ReadOnlySpan<T>)span).ContainsAny(value0, value1));

            AssertSearchValues<T>(span, new[] { value0, value1 }, index);
            return index;
        }

        private static int IndexOfAny<T>(Span<T> span, T value0, T value1, T value2) where T : IEquatable<T>?
        {
            int index = span.IndexOfAny(value0, value1, value2);
            Assert.Equal(index, ((ReadOnlySpan<T>)span).IndexOfAny(value0, value1, value2));

            Assert.Equal(index >= 0, span.ContainsAny(value0, value1, value2));
            Assert.Equal(index >= 0, ((ReadOnlySpan<T>)span).ContainsAny(value0, value1, value2));

            AssertSearchValues<T>(span, new[] { value0, value1, value2 }, index);
            return index;
        }

        private static int IndexOfAny<T>(Span<T> span, ReadOnlySpan<T> values) where T : IEquatable<T>?
        {
            int index = span.IndexOfAny(values);
            Assert.Equal(index, ((ReadOnlySpan<T>)span).IndexOfAny(values));

            Assert.Equal(index >= 0, span.ContainsAny(values));
            Assert.Equal(index >= 0, ((ReadOnlySpan<T>)span).ContainsAny(values));

            AssertSearchValues(span, values, index);
            return index;
        }

        private static void AssertSearchValues<T>(Span<T> span, ReadOnlySpan<T> values, int expectedIndex) where T : IEquatable<T>?
        {
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(char))
            {
                SearchValues<T> searchValuesInstance = (SearchValues<T>)(object)(typeof(T) == typeof(byte)
                    ? SearchValues.Create(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)), values.Length))
                    : SearchValues.Create(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, char>(ref MemoryMarshal.GetReference(values)), values.Length)));

                Assert.Equal(expectedIndex, span.IndexOfAny(searchValuesInstance));
                Assert.Equal(expectedIndex, ((ReadOnlySpan<T>)span).IndexOfAny(searchValuesInstance));

                Assert.Equal(expectedIndex >= 0, span.ContainsAny(searchValuesInstance));
                Assert.Equal(expectedIndex >= 0, ((ReadOnlySpan<T>)span).ContainsAny(searchValuesInstance));
            }
        }
    }
}
