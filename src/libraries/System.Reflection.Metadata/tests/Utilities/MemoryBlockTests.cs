// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection.Internal;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class MemoryBlockTests
    {
        [Fact]
        public unsafe void Utf8NullTerminatedStringStartsWithAsciiPrefix()
        {
            byte[] heap;

            fixed (byte* heapPtr = (heap = new byte[] { 0 }))
            {
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(0, ""));
            }

            fixed (byte* heapPtr = (heap = "Hello World!\0"u8.ToArray()))
            {
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix("Hello ".Length, "World"));
                Assert.False(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix("Hello ".Length, "World?"));
            }

            fixed (byte* heapPtr = (heap = "x\0"u8.ToArray()))
            {
                Assert.False(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(0, "xyz"));
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(0, "x"));
            }

            // bad metadata (#String heap is not nul-terminated):
            fixed (byte* heapPtr = (heap = "abcx"u8.ToArray()))
            {
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(3, "x"));
                Assert.False(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(3, "xyz"));
            }
        }

        [Fact]
        public unsafe void DefaultDecodingFallbackMatchesBcl()
        {
            byte[] buffer;
            int bytesRead;

            var decoder = MetadataStringDecoder.DefaultUTF8;

            // dangling lead byte
            fixed (byte* ptr = (buffer = new byte[] { 0xC0 }))
            {
                string s = new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, null, decoder, out bytesRead);
                Assert.Equal("\uFFFD", new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
                Assert.Equal(s, Encoding.UTF8.GetString(buffer));
                Assert.Equal(buffer.Length, bytesRead);

                s = new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, "Hello"u8.ToArray(), decoder, out bytesRead);
                Assert.Equal("Hello\uFFFD", s);
                Assert.Equal(s, "Hello" + Encoding.UTF8.GetString(buffer));
                Assert.Equal(buffer.Length, bytesRead);

                Assert.Equal("\uFFFD", new MemoryBlock(ptr, buffer.Length).PeekUtf8(0, buffer.Length));
            }

            // overlong encoding
            fixed (byte* ptr = (buffer = new byte[] { (byte)'a', 0xC0, 0xAF, (byte)'b', 0x0 }))
            {
                var block = new MemoryBlock(ptr, buffer.Length);
                Assert.Equal("a\uFFFD\uFFFDb", block.PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);
            }
            // TODO: There are a bunch more error cases of course, but this is enough to break the old code
            // and we now just call the BCL, so from a white box perspective, we needn't get carried away.
        }

        [Fact]
        public unsafe void DecodingSuccessMatchesBcl()
        {
            var utf8 = Encoding.GetEncoding("utf-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            byte[] buffer;
            int bytesRead;
            string str = "\u4F60\u597D. Comment \u00E7a va?";

            var decoder = MetadataStringDecoder.DefaultUTF8;

            fixed (byte* ptr = (buffer = utf8.GetBytes(str)))
            {
                Assert.Equal(str, new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);

                Assert.Equal(str + str, new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, buffer, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);

                Assert.Equal(str, new MemoryBlock(ptr, buffer.Length).PeekUtf8(0, buffer.Length));
            }

            // To cover to big to pool case.
            str = string.Concat(Enumerable.Repeat(str, 10000));
            fixed (byte* ptr = (buffer = utf8.GetBytes(str)))
            {
                Assert.Equal(str, new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);

                Assert.Equal(str + str, new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, buffer, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);

                Assert.Equal(str, new MemoryBlock(ptr, buffer.Length).PeekUtf8(0, buffer.Length));
            }
        }

        [Fact]
        public unsafe void DecoderIsUsedCorrectly()
        {
            byte* ptr = null;
            byte[] buffer = null;
            int bytesRead;
            bool prefixed = false;

            var decoder = new TestMetadataStringDecoder(
                Encoding.UTF8,
                (bytes, byteCount) =>
                {
                    Assert.True(ptr != null);
                    Assert.True(prefixed != (ptr == bytes));
                    Assert.Equal(prefixed ? "PrefixTest".Length : "Test".Length, byteCount);
                    string s = Encoding.UTF8.GetString(bytes, byteCount);
                    Assert.Equal(s, prefixed ? "PrefixTest" : "Test");
                    return "Intercepted";
                }
             );

            fixed (byte* fixedPtr = (buffer = "Test"u8.ToArray()))
            {
                ptr = fixedPtr;
                Assert.Equal("Intercepted", new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);

                prefixed = true;
                Assert.Equal("Intercepted", new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, "Prefix"u8.ToArray(), decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);
            }

            // decoder will fail to intercept because we don't bother calling it for empty strings.
            Assert.Same(string.Empty, new MemoryBlock(null, 0).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
            Assert.Equal(0, bytesRead);

            Assert.Same(string.Empty, new MemoryBlock(null, 0).PeekUtf8NullTerminated(0, new byte[0], decoder, out bytesRead));
            Assert.Equal(0, bytesRead);
        }

        private unsafe void TestComparisons(string heapValue, int offset, string value, bool unicode = false, bool ignoreCase = false)
        {
            byte[] heap;
            MetadataStringDecoder decoder = MetadataStringDecoder.DefaultUTF8;

            fixed (byte* heapPtr = (heap = Encoding.UTF8.GetBytes(heapValue)))
            {
                var block = new MemoryBlock(heapPtr, heap.Length);
                string heapSubstr = GetStringHeapValue(heapValue, offset);

                // compare:
                if (!unicode)
                {
                    int actualCmp = block.CompareUtf8NullTerminatedStringWithAsciiString(offset, value);
                    int expectedCmp = StringComparer.Ordinal.Compare(heapSubstr, value);
                    Assert.Equal(Math.Sign(expectedCmp), Math.Sign(actualCmp));
                }

                if (!ignoreCase)
                {
                    TestComparison(block, offset, value, heapSubstr, decoder, ignoreCase: true);
                }

                TestComparison(block, offset, value, heapSubstr, decoder, ignoreCase);
            }
        }

        private static unsafe void TestComparison(MemoryBlock block, int offset, string value, string heapSubstr, MetadataStringDecoder decoder, bool ignoreCase)
        {
            // equals:
            bool actualEq = block.Utf8NullTerminatedEquals(offset, value, decoder, terminator: '\0', ignoreCase: ignoreCase);
            bool expectedEq = string.Equals(heapSubstr, value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            Assert.Equal(expectedEq, actualEq);

            // starts with:
            bool actualSW = block.Utf8NullTerminatedStartsWith(offset, value, decoder, terminator: '\0', ignoreCase: ignoreCase);
            bool expectedSW = heapSubstr.StartsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            Assert.Equal(actualSW, expectedSW);
        }

        [Fact]
        public unsafe void ComparisonToInvalidByteSequenceMatchesFallback()
        {
            MetadataStringDecoder decoder = MetadataStringDecoder.DefaultUTF8;

            // dangling lead byte
            byte[] buffer;
            fixed (byte* ptr = (buffer = new byte[] { 0xC0 }))
            {
                var block = new MemoryBlock(ptr, buffer.Length);
                Assert.False(block.Utf8NullTerminatedStartsWith(0, new string((char)0xC0, 1), decoder, terminator: '\0', ignoreCase: false));
                Assert.False(block.Utf8NullTerminatedEquals(0, new string((char)0xC0, 1), decoder, terminator: '\0', ignoreCase: false));
                Assert.True(block.Utf8NullTerminatedStartsWith(0, "\uFFFD", decoder, terminator: '\0', ignoreCase: false));
                Assert.True(block.Utf8NullTerminatedEquals(0, "\uFFFD", decoder, terminator: '\0', ignoreCase: false));
            }

            // overlong encoding
            fixed (byte* ptr = (buffer = new byte[] { (byte)'a', 0xC0, 0xAF, (byte)'b', 0x0 }))
            {
                var block = new MemoryBlock(ptr, buffer.Length);
                Assert.False(block.Utf8NullTerminatedStartsWith(0, "a\\", decoder, terminator: '\0', ignoreCase: false));
                Assert.False(block.Utf8NullTerminatedEquals(0, "a\\b", decoder, terminator: '\0', ignoreCase: false));
                Assert.True(block.Utf8NullTerminatedStartsWith(0, "a\uFFFD", decoder, terminator: '\0', ignoreCase: false));
                Assert.True(block.Utf8NullTerminatedEquals(0, "a\uFFFD\uFFFDb", decoder, terminator: '\0', ignoreCase: false));
            }
        }

        private string GetStringHeapValue(string heapValue, int offset)
        {
            int heapEnd = heapValue.IndexOf('\0');
            return (heapEnd < 0) ? heapValue.Substring(offset) : heapValue.Substring(offset, heapEnd - offset);
        }

        [Fact]
        public void Utf8NullTerminatedString_Comparisons()
        {
            TestComparisons("\0", 0, "");
            TestComparisons("Foo\0", 0, "Foo");
            TestComparisons("Foo\0", 1, "oo");
            TestComparisons("Foo\0", 1, "oops");
            TestComparisons("Foo", 1, "oo");
            TestComparisons("Foo", 1, "oops");
            TestComparisons("x", 1, "");
            TestComparisons("hello\0", 0, "h");
            TestComparisons("hello\0", 0, "he");
            TestComparisons("hello\0", 0, "hel");
            TestComparisons("hello\0", 0, "hell");
            TestComparisons("hello\0", 0, "hello");
            TestComparisons("hello\0", 0, "hello!");
            TestComparisons("IVector`1\0", 0, "IVectorView`1");
            TestComparisons("IVector`1\0", 0, "IVector");
            TestComparisons("IVectorView`1\0", 0, "IVector`1");
            TestComparisons("Matrix\0", 0, "Matrix3D");
            TestComparisons("Matrix3D\0", 0, "Matrix");
            TestComparisons("\u1234\0", 0, "\u1234", unicode: true);
            TestComparisons("a\u1234\0", 0, "a", unicode: true);
            TestComparisons("\u1001\u1002\u1003\0", 0, "\u1001\u1002", unicode: true);
            TestComparisons("\u1001a\u1002\u1003\0", 0, "\u1001a\u1002", unicode: true);
            TestComparisons("\u1001\u1002\u1003\0", 0, "\u1001a\u1002", unicode: true);
            TestComparisons("\uD808\uDF45abc\0", 0, "\uD808\uDF45", unicode: true);
            TestComparisons("abc\u1234", 0, "abc\u1234", unicode: true);
            TestComparisons("abc\u1234", 0, "abc\u1235", unicode: true);
            TestComparisons("abc\u1234", 0, "abcd", unicode: true);
            TestComparisons("abcd", 0, "abc\u1234", unicode: true);
            TestComparisons("AAaa\0", 0, "aAAa");
            TestComparisons("A\0", 0, "a", ignoreCase: true);
            TestComparisons("AAaa\0", 0, "aAAa", ignoreCase: true);
            TestComparisons("matrix3d\0", 0, "Matrix3D", ignoreCase: true);
        }

        [Fact]
        public unsafe void Utf8NullTerminatedFastCompare()
        {
            byte[] heap;
            MetadataStringDecoder decoder = MetadataStringDecoder.DefaultUTF8;
            const bool HonorCase = false;
            const bool IgnoreCase = true;
            const char terminator_0 = '\0';
            const char terminator_F = 'F';
            const char terminator_X = 'X';
            const char terminator_x = 'x';

            fixed (byte* heapPtr = (heap = new byte[] { (byte)'F', 0, (byte)'X', (byte)'Y', /* U+12345 (\ud808\udf45) */ 0xf0, 0x92, 0x8d, 0x85 }))
            {
                var block = new MemoryBlock(heapPtr, heap.Length);

                TestUtf8NullTerminatedFastCompare(block, 0, terminator_0, "F", 0, HonorCase, MemoryBlock.FastComparisonResult.Equal, 1);
                TestUtf8NullTerminatedFastCompare(block, 0, terminator_0, "f", 0, IgnoreCase, MemoryBlock.FastComparisonResult.Equal, 1);

                TestUtf8NullTerminatedFastCompare(block, 0, terminator_F, "", 0, IgnoreCase, MemoryBlock.FastComparisonResult.Equal, 0);
                TestUtf8NullTerminatedFastCompare(block, 0, terminator_F, "*", 1, IgnoreCase, MemoryBlock.FastComparisonResult.Equal, 1);

                TestUtf8NullTerminatedFastCompare(block, 0, terminator_0, "FF", 0, HonorCase, MemoryBlock.FastComparisonResult.TextStartsWithBytes, 1);
                TestUtf8NullTerminatedFastCompare(block, 0, terminator_0, "fF", 0, IgnoreCase, MemoryBlock.FastComparisonResult.TextStartsWithBytes, 1);
                TestUtf8NullTerminatedFastCompare(block, 0, terminator_0, "F\0", 0, HonorCase, MemoryBlock.FastComparisonResult.TextStartsWithBytes, 1);
                TestUtf8NullTerminatedFastCompare(block, 0, terminator_X, "F\0", 0, HonorCase, MemoryBlock.FastComparisonResult.TextStartsWithBytes, 1);

                TestUtf8NullTerminatedFastCompare(block, 2, terminator_0, "X", 0, HonorCase, MemoryBlock.FastComparisonResult.BytesStartWithText, 1);
                TestUtf8NullTerminatedFastCompare(block, 2, terminator_0, "x", 0, IgnoreCase, MemoryBlock.FastComparisonResult.BytesStartWithText, 1);
                TestUtf8NullTerminatedFastCompare(block, 2, terminator_x, "XY", 0, IgnoreCase, MemoryBlock.FastComparisonResult.BytesStartWithText, 2);

                TestUtf8NullTerminatedFastCompare(block, 3, terminator_0, "yZ", 0, IgnoreCase, MemoryBlock.FastComparisonResult.Unequal, 1);
                TestUtf8NullTerminatedFastCompare(block, 4, terminator_0, "a", 0, HonorCase, MemoryBlock.FastComparisonResult.Unequal, 0);
                TestUtf8NullTerminatedFastCompare(block, 4, terminator_0, "\ud808", 0, HonorCase, MemoryBlock.FastComparisonResult.Inconclusive, 0);
                TestUtf8NullTerminatedFastCompare(block, 4, terminator_0, "\ud808\udf45", 0, HonorCase, MemoryBlock.FastComparisonResult.Inconclusive, 0);

            }
        }

        private void TestUtf8NullTerminatedFastCompare(
            MemoryBlock block,
            int offset,
            char terminator,
            string comparand,
            int comparandOffset,
            bool ignoreCase,
            MemoryBlock.FastComparisonResult expectedResult,
            int expectedFirstDifferenceIndex)
        {
            int actualFirstDifferenceIndex;
            var actualResult = block.Utf8NullTerminatedFastCompare(offset, comparand, comparandOffset, out actualFirstDifferenceIndex, terminator, ignoreCase);

            Assert.Equal(expectedResult, actualResult);
            Assert.Equal(expectedFirstDifferenceIndex, actualFirstDifferenceIndex);
        }

        private unsafe void TestSearch(string heapValue, int offset, string[] values)
        {
            byte[] heap;

            fixed (byte* heapPtr = (heap = Encoding.UTF8.GetBytes(heapValue)))
            {
                int actual = new MemoryBlock(heapPtr, heap.Length).BinarySearch(values, offset);

                string heapSubstr = GetStringHeapValue(heapValue, offset);
                int expected = Array.BinarySearch(values, heapSubstr);

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void BinarySearch()
        {
            TestSearch("a\0", 0, new string[0]);

            TestSearch("a\0", 0, new[] { "b" });
            TestSearch("b\0", 0, new[] { "b" });
            TestSearch("c\0", 0, new[] { "b" });

            TestSearch("a\0", 0, new[] { "b", "d" });
            TestSearch("b\0", 0, new[] { "b", "d" });
            TestSearch("c\0", 0, new[] { "b", "d" });
            TestSearch("d\0", 0, new[] { "b", "d" });
            TestSearch("e\0", 0, new[] { "b", "d" });

            TestSearch("a\0", 0, new[] { "b", "d", "f" });
            TestSearch("b\0", 0, new[] { "b", "d", "f" });
            TestSearch("c\0", 0, new[] { "b", "d", "f" });
            TestSearch("d\0", 0, new[] { "b", "d", "f" });
            TestSearch("e\0", 0, new[] { "b", "d", "f" });
            TestSearch("f\0", 0, new[] { "b", "d", "f" });
            TestSearch("g\0", 0, new[] { "b", "d", "f" });

            TestSearch("a\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("b\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("c\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("d\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("e\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("f\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("g\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("m\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("n\0", 0, new[] { "b", "d", "f", "m" });
            TestSearch("z\0", 0, new[] { "b", "d", "f", "m" });
        }

        [Fact]
        public unsafe void BuildPtrTable_SmallRefs()
        {
            const int rowSize = 4;
            const int secondColumnOffset = 2;

            var table = new byte[]
            {
                0xAA, 0xAA, 0x05, 0x00,
                0xBB, 0xBB, 0x04, 0x00,
                0xCC, 0xCC, 0x02, 0x01,
                0xDD, 0xDD, 0x02, 0x00,
                0xEE, 0xEE, 0x01, 0x01,
            };

            int rowCount = table.Length / rowSize;

            fixed (byte* tablePtr = table)
            {
                var block = new MemoryBlock(tablePtr, table.Length);

                Assert.Equal(0x0004, block.PeekReference(6, smallRefSize: true));

                var actual = block.BuildPtrTable(rowCount, rowSize, secondColumnOffset, isReferenceSmall: true);
                var expected = new int[] { 4, 2, 1, 5, 3 };
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public unsafe void BuildPtrTable_LargeRefs()
        {
            const int rowSize = 6;
            const int secondColumnOffset = 2;

            var table = new byte[]
            {
                0xAA, 0xAA, 0x10, 0x00, 0x05, 0x00,
                0xBB, 0xBB, 0x10, 0x00, 0x04, 0x00,
                0xCC, 0xCC, 0x10, 0x00, 0x02, 0x01,
                0xDD, 0xDD, 0x10, 0x00, 0x02, 0x00,
                0xEE, 0xEE, 0x10, 0x00, 0x01, 0x01,
            };

            int rowCount = table.Length / rowSize;

            fixed (byte* tablePtr = table)
            {
                var block = new MemoryBlock(tablePtr, table.Length);

                Assert.Equal(0x00040010, block.PeekReference(8, smallRefSize: false));

                var actual = block.BuildPtrTable(rowCount, rowSize, secondColumnOffset, isReferenceSmall: false);
                var expected = new int[] { 4, 2, 1, 5, 3 };
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public unsafe void PeekReference()
        {
            var table = new byte[]
            {
                0xff, 0xff, 0xff, 0x00, // offset 0
                0xff, 0xff, 0xff, 0x01, // offset 4
                0xff, 0xff, 0xff, 0x1f, // offset 8
                0xff, 0xff, 0xff, 0x2f, // offset 12
                0xff, 0xff, 0xff, 0xff, // offset 16
            };

            fixed (byte* tablePtr = table)
            {
                var block = new MemoryBlock(tablePtr, table.Length);

                Assert.Equal(0x0000ffff, block.PeekReference(0, smallRefSize: true));
                Assert.Equal(0x0000ffff, block.PeekHeapReference(0, smallRefSize: true));
                Assert.Equal(0x0000ffffu, block.PeekReferenceUnchecked(0, smallRefSize: true));

                Assert.Equal(0x00ffffff, block.PeekReference(0, smallRefSize: false));
                Assert.Throws<BadImageFormatException>(() => block.PeekReference(4, smallRefSize: false));
                Assert.Throws<BadImageFormatException>(() => block.PeekReference(16, smallRefSize: false));

                Assert.Equal(0x1fffffff, block.PeekHeapReference(8, smallRefSize: false));
                Assert.Throws<BadImageFormatException>(() => block.PeekHeapReference(12, smallRefSize: false));
                Assert.Throws<BadImageFormatException>(() => block.PeekHeapReference(16, smallRefSize: false));

                Assert.Equal(0x01ffffffu, block.PeekReferenceUnchecked(4, smallRefSize: false));
                Assert.Equal(0x1fffffffu, block.PeekReferenceUnchecked(8, smallRefSize: false));
                Assert.Equal(0x2fffffffu, block.PeekReferenceUnchecked(12, smallRefSize: false));
                Assert.Equal(0xffffffffu, block.PeekReferenceUnchecked(16, smallRefSize: false));
            }
        }
    }
}
