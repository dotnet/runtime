// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection.Internal;
using System.Text;
using TestUtilities;
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

            fixed (byte* heapPtr = (heap = Encoding.UTF8.GetBytes("Hello World!\0")))
            {
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix("Hello ".Length, "World"));
                Assert.False(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix("Hello ".Length, "World?"));
            }

            fixed (byte* heapPtr = (heap = Encoding.UTF8.GetBytes("x\0")))
            {
                Assert.False(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(0, "xyz"));
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(0, "x"));
            }

            // bad metadata (#String heap is not nul-terminated):
            fixed (byte* heapPtr = (heap = Encoding.UTF8.GetBytes("abcx")))
            {
                Assert.True(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(3, "x"));
                Assert.False(new MemoryBlock(heapPtr, heap.Length).Utf8NullTerminatedStringStartsWithAsciiPrefix(3, "xyz"));
            }
        }

        [Fact]
        public unsafe void EncodingLightUpHasSucceededAndTestsStillPassWithPortableFallbackAsWell()
        {
            Assert.True(EncodingHelper.TestOnly_LightUpEnabled); // tests run on desktop only right now.

            try
            {
                // Re-run them with forced portable implementation.
                EncodingHelper.TestOnly_LightUpEnabled = false;
                DefaultDecodingFallbackMatchesBcl();
                DecodingSuccessMatchesBcl();
                DecoderIsUsedCorrectly();
                LightUpTrickFromDifferentAssemblyWorks();
            }
            finally
            {
                EncodingHelper.TestOnly_LightUpEnabled = true;
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

                s = new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, Encoding.UTF8.GetBytes("Hello"), decoder, out bytesRead);
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
            str = String.Concat(Enumerable.Repeat(str, 10000));
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
        public unsafe void LightUpTrickFromDifferentAssemblyWorks()
        {
            // This is a trick to use our portable light up outside the reader assembly (that 
            // I will use in Roslyn). Check that it works with encoding other than UTF8 and that it 
            // validates arguments like the the real thing.
            var decoder = new MetadataStringDecoder(Encoding.Unicode);
            Assert.Throws<ArgumentNullException>(() => decoder.GetString(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetString((byte*)1, -1));

            byte[] bytes;
            fixed (byte* ptr = (bytes = Encoding.Unicode.GetBytes("\u00C7a marche tr\u00E8s bien.")))
            {
                Assert.Equal("\u00C7a marche tr\u00E8s bien.", decoder.GetString(ptr, bytes.Length));
            }
        }

        unsafe delegate string GetString(byte* bytes, int count);

        sealed class CustomDecoder : MetadataStringDecoder
        {
            private GetString _getString;
            public CustomDecoder(Encoding encoding, GetString getString)
                : base(encoding)
            {
                _getString = getString;
            }

            public override unsafe string GetString(byte* bytes, int byteCount)
            {
                return _getString(bytes, byteCount);
            }
        }

        [Fact]
        public unsafe void DecoderIsUsedCorrectly()
        {
            byte* ptr = null;
            byte[] buffer = null;
            int bytesRead;
            bool prefixed = false;

            var decoder = new CustomDecoder(
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

            fixed (byte* fixedPtr = (buffer = Encoding.UTF8.GetBytes("Test")))
            {
                ptr = fixedPtr;
                Assert.Equal("Intercepted", new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);

                prefixed = true;
                Assert.Equal("Intercepted", new MemoryBlock(ptr, buffer.Length).PeekUtf8NullTerminated(0, Encoding.UTF8.GetBytes("Prefix"), decoder, out bytesRead));
                Assert.Equal(buffer.Length, bytesRead);
            }

            // decoder will fail to intercept because we don't bother calling it for empty strings.
            Assert.Same(String.Empty, new MemoryBlock(null, 0).PeekUtf8NullTerminated(0, null, decoder, out bytesRead));
            Assert.Equal(0, bytesRead);

            Assert.Same(String.Empty, new MemoryBlock(null, 0).PeekUtf8NullTerminated(0, new byte[0], decoder, out bytesRead));
            Assert.Equal(0, bytesRead);
        }

        private unsafe void TestComparisons(string heapValue, int offset, string value, bool unicode = false)
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

                // equals:
                bool actualEq = block.Utf8NullTerminatedEquals(offset, value, decoder);
                bool expectedEq = StringComparer.Ordinal.Equals(heapSubstr, value);
                Assert.Equal(expectedEq, actualEq);

                // starts with:
                bool actualSW = block.Utf8NullTerminatedStartsWith(offset, value, decoder);
                bool expectedSW = heapSubstr.StartsWith(value, StringComparison.Ordinal);
                Assert.Equal(actualSW, expectedSW);
            }
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
                Assert.False(block.Utf8NullTerminatedStartsWith(0, new string((char)0xC0, 1), decoder));
                Assert.False(block.Utf8NullTerminatedEquals(0, new string((char)0xC0, 1), decoder));
                Assert.True(block.Utf8NullTerminatedStartsWith(0, "\uFFFD", decoder));
                Assert.True(block.Utf8NullTerminatedEquals(0, "\uFFFD", decoder));
            }

            // overlong encoding
            fixed (byte* ptr = (buffer = new byte[] { (byte)'a', 0xC0, 0xAF, (byte)'b', 0x0 }))
            {
                var block = new MemoryBlock(ptr, buffer.Length);
                Assert.False(block.Utf8NullTerminatedStartsWith(0, "a\\", decoder));
                Assert.False(block.Utf8NullTerminatedEquals(0, "a\\b", decoder));
                Assert.True(block.Utf8NullTerminatedStartsWith(0, "a\uFFFD", decoder));
                Assert.True(block.Utf8NullTerminatedEquals(0, "a\uFFFD\uFFFDb", decoder));
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

                Assert.Equal(0x0004U, block.PeekReference(6, smallRefSize: true));

                var actual = block.BuildPtrTable(rowCount, rowSize, secondColumnOffset, isReferenceSmall: true);
                var expected = new uint[] { 4, 2, 1, 5, 3 };
                AssertEx.Equal(expected, actual);
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

                Assert.Equal(0x00040010U, block.PeekReference(8, smallRefSize: false));

                var actual = block.BuildPtrTable(rowCount, rowSize, secondColumnOffset, isReferenceSmall: false);
                var expected = new uint[] { 4, 2, 1, 5, 3 };
                AssertEx.Equal(expected, actual);
            }
        }
    }
}
