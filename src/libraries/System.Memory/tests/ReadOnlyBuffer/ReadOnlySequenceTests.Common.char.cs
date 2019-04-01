// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Memory.Tests
{
    public class ReadOnlySequenceTestsCommonChar: ReadOnlySequenceTestsCommon<char>
    {
        #region Constructor 

        [Fact]
        public void Ctor_Array_Offset()
        {
            var buffer = new ReadOnlySequence<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 }, 2, 3);
            Assert.Equal(buffer.ToArray(), new char[] { (char)3, (char)4, (char)5 });
        }

        [Fact]
        public void Ctor_Array_NoOffset()
        {
            var buffer = new ReadOnlySequence<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 });
            Assert.Equal(buffer.ToArray(), new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 });
        }

        [Fact]
        public void Ctor_Memory()
        {
            var memory = new ReadOnlyMemory<char>(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 });
            var buffer = new ReadOnlySequence<char>(memory.Slice(2, 3));
            Assert.Equal(new char[] { (char)3, (char)4, (char)5 }, buffer.ToArray());
        }

        [Fact]
        public void Ctor_String()
        {
            ReadOnlyMemory<char> memory = "12345".AsMemory();
            var buffer = new ReadOnlySequence<char>(memory.Slice(2, 3));
            Assert.Equal("12345".Substring(2, 3).ToArray(), buffer.ToArray());
        }

        #endregion

        [Fact]
        public void HelloWorldAcrossTwoBlocks()
        {
            //     block 1       ->    block2
            // [padding..hello]  ->  [  world   ]
            const int blockSize = 4096;

            string items = "Hello World";
            string firstItems = new string('a', blockSize - 5) + items.Substring(0, 5);
            string secondItems = items.Substring(5) + new string('a', blockSize - (items.Length - 5));
            
            var firstSegment = new BufferSegment<char>(firstItems.AsMemory());
            BufferSegment<char> secondSegment = firstSegment.Append(secondItems.AsMemory());

            var  buffer = new ReadOnlySequence<char>(firstSegment, 0, secondSegment, items.Length - 5);
            Assert.False(buffer.IsSingleSegment);
            ReadOnlySequence<char> helloBuffer = buffer.Slice(blockSize - 5);
            Assert.False(helloBuffer.IsSingleSegment);
            var memory = new List<ReadOnlyMemory<char>>();
            foreach (ReadOnlyMemory<char> m in helloBuffer)
            {
                memory.Add(m);
            }

            List<ReadOnlyMemory<char>> spans = memory;

            Assert.Equal(2, memory.Count);
            var helloBytes = new char[spans[0].Length];
            spans[0].Span.CopyTo(helloBytes);
            var worldBytes = new char[spans[1].Length];
            spans[1].Span.CopyTo(worldBytes);
            Assert.Equal("Hello", new string(helloBytes));
            Assert.Equal(" World", new string(worldBytes));
        }

        [Fact]
        public void AsString_CanGetFirst()
        {
            const string SampleString = "12345";
            var buffer = new ReadOnlySequence<char>(SampleString.AsMemory());
            VerifyCanGetFirst(buffer, expectedSize: 5);
        }
    }
}
