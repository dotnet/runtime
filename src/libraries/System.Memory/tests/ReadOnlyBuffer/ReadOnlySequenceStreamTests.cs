// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Memory.Tests
{
    /// <summary>
    /// Additional specific tests for ReadOnlySequenceStream beyond conformance tests.
    /// </summary>
    public class ReadOnlySequenceStreamTests
    {
        // NOTE: Conformance tests' coverage: Ctor correctness, stream capabilities,
        // Position, Length, Seek, Read, exceptions for unsupported operations.

        // Not covered in conformance tests: Stream + multi-segment sequences
        // ReadOnlySequence{byte} can represent data spread across
        // multiple memory segments (linked list of ReadOnlyMemory{byte}).
        // This is common in network buffers and pooled memory scenarios.
        [Fact]
        public void Read_MultiSegmentSequence_ReturnsCorrectData()
        {
            // Create multi-segment sequence:  [1,2,3] -> [4,5,6] -> [7,8,9]
            var segment1 = new TestSegment(new byte[] { 1, 2, 3 });
            var segment2 = segment1.Append(new byte[] { 4, 5, 6 });
            var segment3 = segment2.Append(new byte[] { 7, 8, 9 });

            var sequence = new ReadOnlySequence<byte>(segment1, 0, segment3, 3);
            var stream = new ReadOnlySequenceStream(sequence);

            // Read all data
            byte[] buffer = new byte[9];
            int totalRead = 0;

            while (totalRead < 9)
            {
                int bytesRead = stream.Read(buffer, totalRead, 9 - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }

            Assert.Equal(9, totalRead);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, buffer);
        }

        [Fact]
        public void Seek_MultiSegmentSequence_WorksCorrectly()
        {
            // Create multi-segment sequence: [1,2,3] -> [4,5,6]
            var segment1 = new TestSegment(new byte[] { 1, 2, 3 });
            var segment2 = segment1.Append(new byte[] { 4, 5, 6 });

            var sequence = new ReadOnlySequence<byte>(segment1, 0, segment2, 3);
            var stream = new ReadOnlySequenceStream(sequence);

            // Seek into second segment
            stream.Seek(4, SeekOrigin.Begin); // Should be at byte '5'

            byte[] buffer = new byte[1];
            stream.Read(buffer, 0, 1);

            Assert.Equal(5, buffer[0]);
            Assert.Equal(5, stream.Position);
        }

        [Fact]
        public void Seek_AcrossSegments_BothDirections()
        {
            // Arrange: [10,20,30] -> [40,50,60]
            var segment1 = new TestSegment(new byte[] { 10, 20, 30 });
            var segment2 = segment1.Append(new byte[] { 40, 50, 60 });

            var sequence = new ReadOnlySequence<byte>(segment1, 0, segment2, 3);
            var stream = new ReadOnlySequenceStream(sequence);

            byte[] buffer = new byte[1];

            // Act & Assert: Start at position 2 (byte 30)
            stream.Position = 2;
            stream.Read(buffer, 0, 1);
            Assert.Equal(30, buffer[0]);

            // Seek forward into segment 2
            stream.Seek(2, SeekOrigin.Current); // Now at position 5 (byte 60)
            stream.Read(buffer, 0, 1);
            Assert.Equal(60, buffer[0]);

            // Seek backward into segment 1
            stream.Seek(-4, SeekOrigin.Current); // Now at position 2 (byte 30)
            stream.Read(buffer, 0, 1);
            Assert.Equal(30, buffer[0]);
        }

        [Fact]
        public void Position_MultiSegmentSequence_TracksCorrectly()
        {
            // Arrange: [1,2] -> [3,4] -> [5,6]
            var segment1 = new TestSegment(new byte[] { 1, 2 });
            var segment2 = segment1.Append(new byte[] { 3, 4 });
            var segment3 = segment2.Append(new byte[] { 5, 6 });

            var sequence = new ReadOnlySequence<byte>(segment1, 0, segment3, 2);
            var stream = new ReadOnlySequenceStream(sequence);

            byte[] buffer = new byte[1];

            // Act & Assert: Position advances correctly through segments
            Assert.Equal(0, stream.Position);

            stream.Read(buffer, 0, 1); // Read from segment 1
            Assert.Equal(1, stream.Position);

            stream.Read(buffer, 0, 1); // Read from segment 1
            Assert.Equal(2, stream.Position);

            stream.Read(buffer, 0, 1); // Read from segment 2 (boundary cross)
            Assert.Equal(3, stream.Position);

            stream.Read(buffer, 0, 1); // Read from segment 2
            Assert.Equal(4, stream.Position);

            stream.Read(buffer, 0, 1); // Read from segment 3 (boundary cross)
            Assert.Equal(5, stream.Position);

            stream.Read(buffer, 0, 1); // Read from segment 3
            Assert.Equal(6, stream.Position);
        }

        /// <summary>
        /// Helper class for creating multi-segment ReadOnlySequence{byte} for testing.
        /// </summary>
        private class TestSegment : ReadOnlySequenceSegment<byte>
        {
            public TestSegment(byte[] data)
            {
                Memory = data;
            }

            public TestSegment Append(byte[] data)
            {
                var segment = new TestSegment(data)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = segment;
                return segment;
            }
        }

        // Basic edge cases
        [Fact]
        public void Read_ZeroBytes_ReturnsZero()
        {
            var data = new byte[] { 1, 2, 3 };
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));
            byte[] buffer = new byte[10];

            int bytesRead = stream.Read(buffer, 0, 0);

            Assert.Equal(0, bytesRead);
            Assert.Equal(0, stream.Position); // Position shouldn't change
        }

        [Fact]
        public void EmptySequence_BehavesCorrectly()
        {
            var stream = new ReadOnlySequenceStream(ReadOnlySequence<byte>.Empty);

            Assert.Equal(0, stream.Length);
            Assert.Equal(0, stream.Position);

            byte[] buffer = new byte[10];
            int bytesRead = stream.Read(buffer, 0, 10);
            Assert.Equal(0, bytesRead);

            // Seek to position 0 should succeed
            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(0, stream.Position);

            // Seeking beyond empty buffer is allowed
            long newPosition = stream.Seek(1, SeekOrigin.Begin);
            Assert.Equal(1, newPosition);
            Assert.Equal(1, stream.Position);
        }

        [Fact]
        public async Task ReadAsync_SameResultSize_ReusesCachedTask()
        {
            var data = new byte[20];
            for (int i = 0; i < 20; i++) data[i] = (byte)i;
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));

            byte[] buffer1 = new byte[5];
            byte[] buffer2 = new byte[5];
            byte[] buffer3 = new byte[5];

            Task<int> task1 = stream.ReadAsync(buffer1, 0, 5);
            Task<int> task2 = stream.ReadAsync(buffer2, 0, 5);
            Task<int> task3 = stream.ReadAsync(buffer3, 0, 5);

            await task1;
            await task2;
            await task3;

            Assert.Same(task1, task2);
            Assert.Same(task2, task3);

            Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, buffer1);
            Assert.Equal(new byte[] { 5, 6, 7, 8, 9 }, buffer2);
            Assert.Equal(new byte[] { 10, 11, 12, 13, 14 }, buffer3);
        }

        [Fact]
        public async Task ReadAsync_DifferentResultSize_CreatesNewTask()
        {
            var data = new byte[10];
            for (int i = 0; i < 10; i++) data[i] = (byte)i;
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));

            byte[] buffer1 = new byte[5];
            byte[] buffer2 = new byte[3];
            byte[] buffer3 = new byte[2];

            Task<int> task1 = stream.ReadAsync(buffer1, 0, 5);  // Returns 5
            Task<int> task2 = stream.ReadAsync(buffer2, 0, 3);  // Returns 3
            Task<int> task3 = stream.ReadAsync(buffer3, 0, 2);  // Returns 2

            await task1;
            await task2;
            await task3;

            Assert.NotSame(task1, task2);
            Assert.NotSame(task2, task3);
        }

        [Fact]
        public async Task ReadAsync_ArrayBackedMemory_UsesFastPath()
        {
            var data = new byte[] { 10, 20, 30, 40, 50 };
            var stream = new ReadOnlySequenceStream(new ReadOnlySequence<byte>(data));

            byte[] arrayBuffer = new byte[3];
            Memory<byte> memory = arrayBuffer.AsMemory();
            int bytesRead = await stream.ReadAsync(memory);

            Assert.Equal(3, bytesRead);
            Assert.Equal(new byte[] { 10, 20, 30 }, arrayBuffer);
        }
    }
}
