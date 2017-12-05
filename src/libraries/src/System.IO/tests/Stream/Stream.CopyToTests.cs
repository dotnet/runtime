// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamCopyToTests
    {
        [Fact]
        public void IfCanSeekIsFalseLengthAndPositionShouldNotBeCalled()
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => false,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            trackingStream.CopyTo(dest);

            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.CanSeek)), 0, 1);
            Assert.Equal(0, trackingStream.TimesCalled(nameof(trackingStream.Length)));
            Assert.Equal(0, trackingStream.TimesCalled(nameof(trackingStream.Position)));
            // We can't override CopyTo since it's not virtual, so checking TimesCalled
            // for CopyTo will result in 0. Instead, we check that Read was called,
            // and validate the parameters passed there.
            Assert.Equal(1, trackingStream.TimesCalled(nameof(trackingStream.Read)));

            byte[] outerBuffer = trackingStream.ReadBuffer;
            int outerOffset = trackingStream.ReadOffset;
            int outerCount = trackingStream.ReadCount;

            Assert.NotNull(outerBuffer);
            Assert.InRange(outerOffset, 0, outerBuffer.Length - outerCount);
            Assert.InRange(outerCount, 1, int.MaxValue); // the buffer can't be size 0
        }

        [Fact]
        public async Task AsyncIfCanSeekIsFalseLengthAndPositionShouldNotBeCalled()
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => false,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            await trackingStream.CopyToAsync(dest);

            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.CanSeek)), 0, 1);
            Assert.Equal(0, trackingStream.TimesCalled(nameof(trackingStream.Length)));
            Assert.Equal(0, trackingStream.TimesCalled(nameof(trackingStream.Position)));
            Assert.Equal(1, trackingStream.TimesCalled(nameof(trackingStream.CopyToAsync)));

            Assert.Same(dest, trackingStream.CopyToAsyncDestination);
            Assert.InRange(trackingStream.CopyToAsyncBufferSize, 1, int.MaxValue);
            Assert.Equal(CancellationToken.None, trackingStream.CopyToAsyncCancellationToken);
        }

        [Fact]
        public void IfCanSeekIsTrueLengthAndPositionShouldOnlyBeCalledOnce()
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                readFunc: (buffer, offset, count) => 0,
                lengthFunc: () => 0L,
                positionGetFunc: () => 0L);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            trackingStream.CopyTo(dest);

            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.Length)), 0, 1);
            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.Position)), 0, 1);
        }

        [Fact]
        public async Task AsyncIfCanSeekIsTrueLengthAndPositionShouldOnlyBeCalledOnce()
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                readFunc: (buffer, offset, count) => 0,
                lengthFunc: () => 0L,
                positionGetFunc: () => 0L);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            await trackingStream.CopyToAsync(dest);

            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.Length)), 0, 1);
            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.Position)), 0, 1);
        }

        [Theory]
        [MemberData(nameof(LengthIsLessThanOrEqualToPosition))]
        public void IfLengthIsLessThanOrEqualToPositionCopyToShouldStillBeCalledWithAPositiveBufferSize(long length, long position)
        {
            // Streams with their Lengths <= their Positions, e.g.
            // new MemoryStream { Position = 3 }.SetLength(1)
            // should still be called CopyTo{Async} on with a
            // bufferSize of at least 1.

            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                lengthFunc: () => length,
                positionGetFunc: () => position,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            trackingStream.CopyTo(dest);

            // CopyTo is not virtual, so we can't override it in
            // CallTrackingStream and record the arguments directly.
            // Instead, validate the arguments passed to Read.
            
            Assert.Equal(1, trackingStream.TimesCalled(nameof(trackingStream.Read)));

            byte[] outerBuffer = trackingStream.ReadBuffer;
            int outerOffset = trackingStream.ReadOffset;
            int outerCount = trackingStream.ReadCount;

            Assert.NotNull(outerBuffer);
            Assert.InRange(outerOffset, 0, outerBuffer.Length - outerCount);
            Assert.InRange(outerCount, 1, int.MaxValue);
        }

        [Theory]
        [MemberData(nameof(LengthIsLessThanOrEqualToPosition))]
        public async Task AsyncIfLengthIsLessThanOrEqualToPositionCopyToShouldStillBeCalledWithAPositiveBufferSize(long length, long position)
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                lengthFunc: () => length,
                positionGetFunc: () => position,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            await trackingStream.CopyToAsync(dest);

            Assert.Same(dest, trackingStream.CopyToAsyncDestination);
            Assert.InRange(trackingStream.CopyToAsyncBufferSize, 1, int.MaxValue);
            Assert.Equal(CancellationToken.None, trackingStream.CopyToAsyncCancellationToken);
        }

        [Theory]
        [MemberData(nameof(LengthMinusPositionPositiveOverflows))]
        public void IfLengthMinusPositionPositiveOverflowsBufferSizeShouldStillBePositive(long length, long position)
        {
            // The new implementation of Stream.CopyTo calculates the bytes left
            // in the Stream by calling Length - Position. This can overflow to a
            // negative number, so this tests that if that happens we don't send
            // in a negative bufferSize.

            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                lengthFunc: () => length,
                positionGetFunc: () => position,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            trackingStream.CopyTo(dest);

            // CopyTo is not virtual, so we can't override it in
            // CallTrackingStream and record the arguments directly.
            // Instead, validate the arguments passed to Read.
            
            Assert.Equal(1, trackingStream.TimesCalled(nameof(trackingStream.Read)));

            byte[] outerBuffer = trackingStream.ReadBuffer;
            int outerOffset = trackingStream.ReadOffset;
            int outerCount = trackingStream.ReadCount;

            Assert.NotNull(outerBuffer);
            Assert.InRange(outerOffset, 0, outerBuffer.Length - outerCount);
            Assert.InRange(outerCount, 1, int.MaxValue);
        }

        [Theory]
        [MemberData(nameof(LengthMinusPositionPositiveOverflows))]
        public async Task AsyncIfLengthMinusPositionPositiveOverflowsBufferSizeShouldStillBePositive(long length, long position)
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                lengthFunc: () => length,
                positionGetFunc: () => position,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            var dest = Stream.Null;
            await trackingStream.CopyToAsync(dest);

            // Note: We can't check how many times ReadAsync was called
            // here, since trackingStream overrides CopyToAsync and forwards
            // to the inner (non-tracking) stream for the implementation

            Assert.Same(dest, trackingStream.CopyToAsyncDestination);
            Assert.InRange(trackingStream.CopyToAsyncBufferSize, 1, int.MaxValue);
            Assert.Equal(CancellationToken.None, trackingStream.CopyToAsyncCancellationToken);
        }

        [Theory]
        [MemberData(nameof(LengthIsGreaterThanPositionAndDoesNotOverflow))]
        public void IfLengthIsGreaterThanPositionAndDoesNotOverflowEverythingShouldGoNormally(long length, long position)
        {
            const int ReadLimit = 7;

            // Lambda state
            byte[] outerBuffer = null;
            int? outerOffset = null;
            int? outerCount = null;
            int readsLeft = ReadLimit;

            var srcBase = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                lengthFunc: () => length,
                positionGetFunc: () => position,
                readFunc: (buffer, offset, count) =>
                {
                    Assert.NotNull(buffer);
                    Assert.InRange(offset, 0, buffer.Length - count);
                    Assert.InRange(count, 1, int.MaxValue);

                    // CopyTo should always pass in the same buffer/offset/count
                    
                    if (outerBuffer != null) Assert.Same(outerBuffer, buffer);
                    else outerBuffer = buffer;

                    if (outerOffset != null) Assert.Equal(outerOffset, offset);
                    else outerOffset = offset;

                    if (outerCount != null) Assert.Equal(outerCount, count);
                    else outerCount = count;

                    return --readsLeft; // CopyTo will call Read on this ReadLimit times before stopping 
                });

	        var src = new CallTrackingStream(srcBase);

            var destBase = new DelegateStream(
                canWriteFunc: () => true,
                writeFunc: (buffer, offset, count) =>
                {
                    Assert.Same(outerBuffer, buffer);
                    Assert.Equal(outerOffset, offset);
                    Assert.Equal(readsLeft, count);
                });

            var dest = new CallTrackingStream(destBase);
            src.CopyTo(dest);

            Assert.Equal(ReadLimit, src.TimesCalled(nameof(src.Read)));
            Assert.Equal(ReadLimit - 1, dest.TimesCalled(nameof(dest.Write)));
        }

        [Theory]
        [MemberData(nameof(LengthIsGreaterThanPositionAndDoesNotOverflow))]
        public async Task AsyncIfLengthIsGreaterThanPositionAndDoesNotOverflowEverythingShouldGoNormally(long length, long position)
        {
            const int ReadLimit = 7;

            // Lambda state
            byte[] outerBuffer = null;
            int? outerOffset = null;
            int? outerCount = null;
            int readsLeft = ReadLimit;

            var srcBase = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => true,
                lengthFunc: () => length,
                positionGetFunc: () => position,
                readFunc: (buffer, offset, count) =>
                {
                    Assert.NotNull(buffer);
                    Assert.InRange(offset, 0, buffer.Length - count);
                    Assert.InRange(count, 1, int.MaxValue);

                    // CopyTo should always pass in the same buffer/offset/count
                    
                    if (outerBuffer != null) Assert.Same(outerBuffer, buffer);
                    else outerBuffer = buffer;

                    if (outerOffset != null) Assert.Equal(outerOffset, offset);
                    else outerOffset = offset;

                    if (outerCount != null) Assert.Equal(outerCount, count);
                    else outerCount = count;

                    return --readsLeft; // CopyTo will call Read on this ReadLimit times before stopping 
                });

	        var src = new CallTrackingStream(srcBase);

            var destBase = new DelegateStream(
                canWriteFunc: () => true,
                writeFunc: (buffer, offset, count) =>
                {
                    Assert.Same(outerBuffer, buffer);
                    Assert.Equal(outerOffset, offset);
                    Assert.Equal(readsLeft, count);
                });

            var dest = new CallTrackingStream(destBase);
            await src.CopyToAsync(dest);

            // Since we override CopyToAsync in CallTrackingStream,
            // src.Read will actually not get called ReadLimit
            // times, src.Inner.Read will. So, we just assert that
            // CopyToAsync was called once for src.

            Assert.Equal(1, src.TimesCalled(nameof(src.CopyToAsync)));
            Assert.Equal(ReadLimit - 1, dest.TimesCalled(nameof(dest.WriteAsync))); // dest.WriteAsync will still get called repeatedly
        }

        // Member data

        public static IEnumerable<object[]> LengthIsLessThanOrEqualToPosition()
        {
            yield return new object[] { 5L, 5L }; // same number
            yield return new object[] { 3L, 5L }; // length is less than position
            yield return new object[] { -1L, -1L }; // negative numbers
            yield return new object[] { 0L, 0L }; // both zero
            yield return new object[] { -500L, 0L }; // negative number and zero
            yield return new object[] { 0L, 500L }; // zero and positive number
            yield return new object[] { -500L, 500L }; // negative and positive number
            yield return new object[] { long.MinValue, long.MaxValue }; // length - position <= 0 will fail (overflow), but length <= position won't
        }

        public static IEnumerable<object[]> LengthMinusPositionPositiveOverflows()
        {
            yield return new object[] { long.MaxValue, long.MinValue }; // length - position will be -1
            yield return new object[] { 1L, -long.MaxValue };
        }

        public static IEnumerable<object[]> LengthIsGreaterThanPositionAndDoesNotOverflow()
        {
            yield return new object[] { 5L, 3L };
            yield return new object[] { -3L, -6L };
            yield return new object[] { 0L, -3L };
            yield return new object[] { long.MaxValue, 0 }; // should not overflow or OOM
            yield return new object[] { 85000, 123 }; // at least in the current implementation, we max out the bufferSize at 81920
        }
    }
}
