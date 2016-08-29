// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class DocumentNameTests
    {
        [Fact]
        public unsafe void GetString1()
        {
            var blobHeapData = new byte[] 
            {
                0,          // 0

                2,          // 1: blob size 
                (byte)'a',  // 2
                (byte)'b',  // 3

                3,          // 4: blob size
                (byte)'x',  // 5
                (byte)'y',  // 6
                (byte)'z',  // 7

                3,          // 8: blob size
                (byte)'\\', // 9: separator
                1,          // 10: part #1 
                4,          // 11: part #2 
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(8);

                var name = blobHeap.GetDocumentName(handle);
                Assert.Equal(@"ab\xyz", name);

                Assert.True(blobHeap.DocumentNameEquals(handle, @"ab\xyz", ignoreCase: false));
                Assert.True(blobHeap.DocumentNameEquals(handle, @"Ab\xYz", ignoreCase: true));

                Assert.False(blobHeap.DocumentNameEquals(handle, @"", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"a", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"ab", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"ab\", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"ab\x", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"ab\xy", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"abc\xyz", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"Ab\xYzz", ignoreCase: true));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"Ab\xYz\", ignoreCase: true));
            }
        }

        [Fact]
        public unsafe void GetString_EmptyParts()
        {
            var blobHeapData = new byte[]
            {
                0,          // 0

                1,          // 1: blob size 
                (byte)'a',  // 2

                6,          // 3: blob size
                (byte)'\\', // 4: separator
                0,          // 5: part #1 
                1,          // 6: part #2 
                0,          // 7: part #3 
                1,          // 8: part #4 
                0,          // 9: part #5 
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(3);

                var name = blobHeap.GetDocumentName(handle);
                Assert.Equal(@"\a\\a\", name);

                Assert.True(blobHeap.DocumentNameEquals(handle, @"\A\\A\", ignoreCase: true));

                Assert.False(blobHeap.DocumentNameEquals(handle, @"", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a\", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a\\", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a\\a", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a\\a\a", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a\\aa\", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"\a\\\", ignoreCase: false));
            }
        }

        [Fact]
        public unsafe void GetString_EmptySeparator()
        {
            var blobHeapData = new byte[]
            {
                0,          // 0

                1,          // 1: blob size 
                (byte)'a',  // 2

                6,          // 3: blob size
                0,          // 4: separator
                0,          // 5: part #1 
                1,          // 6: part #2 
                0,          // 7: part #3 
                1,          // 8: part #4 
                0,          // 9: part #5 
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(3);

                var name = blobHeap.GetDocumentName(handle);
                Assert.Equal(@"aa", name);

                Assert.True(blobHeap.DocumentNameEquals(handle, @"aa", ignoreCase: false));

                Assert.False(blobHeap.DocumentNameEquals(handle, @"", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"a", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"aaa", ignoreCase: false));
            }
        }

        [Fact]
        public unsafe void GetString_Empty()
        {
            var blobHeapData = new byte[]
            {
                0,          // 0

                1,          // 1: blob size 
                (byte)'a',  // 2

                6,          // 3: blob size
                0,          // 4: separator
                0,          // 5: part #1 
                0,          // 6: part #2 
                0,          // 7: part #3 
                0,          // 8: part #4 
                0,          // 9: part #5 
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(3);

                var name = blobHeap.GetDocumentName(handle);
                Assert.Equal(@"", name);

                Assert.True(blobHeap.DocumentNameEquals(handle, @"", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, @"a", ignoreCase: false));
            }
        }

        [Fact]
        public unsafe void GetString_IgnoreSeparatorCase()
        {
            var blobHeapData = new byte[]
            {
                0,          // 0

                1,          // 1: blob size 
                (byte)'b',  // 2

                3,          // 3: blob size
                (byte)'a',  // 4: separator
                1,          // 5: part #1 
                1,          // 6: part #2
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(3);

                var name = blobHeap.GetDocumentName(handle);
                Assert.Equal("bab", name);

                Assert.True(blobHeap.DocumentNameEquals(handle, "bab", ignoreCase: false));
                Assert.True(blobHeap.DocumentNameEquals(handle, "BAB", ignoreCase: true));
                Assert.True(blobHeap.DocumentNameEquals(handle, "bAb", ignoreCase: true));
                Assert.True(blobHeap.DocumentNameEquals(handle, "BaB", ignoreCase: true));

                Assert.False(blobHeap.DocumentNameEquals(handle, "", ignoreCase: true));
                Assert.False(blobHeap.DocumentNameEquals(handle, "B", ignoreCase: true));
                Assert.False(blobHeap.DocumentNameEquals(handle, "bA", ignoreCase: true));
                Assert.False(blobHeap.DocumentNameEquals(handle, "bAbA", ignoreCase: true));
            }
        }

        [Fact]
        public unsafe void GetString_NonAscii()
        {
            var blobHeapData = new byte[]
            {
                0,          // 0

                3,          // 1: blob size 
                0xe1,       // 2: U+1234 in UTF8
                0x88,       // 3
                0xb4,       // 4

                1,          // 5: blob size 
                (byte)'b',  // 6

                4,          // 7: blob size
                (byte)'a',  // 8: separator
                5,          // 9: part #1 
                1,          // 10: part #2 
                5,          // 11: part #3
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(7);

                var name = blobHeap.GetDocumentName(handle);
                Assert.Equal("ba\u1234ab", name);

                Assert.True(blobHeap.DocumentNameEquals(handle, "ba\u1234ab", ignoreCase: false));
                Assert.True(blobHeap.DocumentNameEquals(handle, "BA\u1234AB", ignoreCase: true));

                Assert.False(blobHeap.DocumentNameEquals(handle, "b\u1234ab", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, "a\u1234ab", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, "ba\u1234abb", ignoreCase: false));
            }
        }

        [Fact]
        public unsafe void GetString_Errors()
        {
            var blobHeapData = new byte[]
            {
                0,          // 0

                2,          // 1: blob size
                0x80,       // 2: separator
                0,          // 3: part #1 
            };

            fixed (byte* ptr = blobHeapData)
            {
                var blobHeap = new BlobHeap(new MemoryBlock(ptr, blobHeapData.Length), MetadataKind.Ecma335);

                var handle = DocumentNameBlobHandle.FromOffset(1);

                Assert.Throws<BadImageFormatException>(() => blobHeap.GetDocumentName(handle));
                Assert.False(blobHeap.DocumentNameEquals(handle, "", ignoreCase: false));
                Assert.False(blobHeap.DocumentNameEquals(handle, "a", ignoreCase: false));

                Assert.Throws<BadImageFormatException>(() => blobHeap.GetDocumentName(default(DocumentNameBlobHandle)));
                Assert.Throws<BadImageFormatException>(() => blobHeap.GetDocumentName(DocumentNameBlobHandle.FromOffset(8)));
            }
        }
    }
}
