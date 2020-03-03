// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamCopyToSpanTests
    {
        [Fact]
        public void CopyTo_InvalidArgsThrows()
        {
            using Stream s = new MemoryStream();

            AssertExtensions.Throws<ArgumentNullException>("callback", () => s.CopyTo(null, null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => s.CopyTo((_, __) => { }, null, 0));

            AssertExtensions.Throws<ArgumentNullException>("callback", () => s.CopyToAsync(null, null, 0, default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("bufferSize", () => s.CopyToAsync((_, __, ___) => default, null, 0, default));
        }

        [Fact]
        public void CopyToAsync_PrecanceledToken_Cancels()
        {
            using var src = new MemoryStream();
            Assert.Equal(TaskStatus.Canceled, src.CopyToAsync((_, __, ___) => default, null, 4096, new CancellationToken(true)).Status);
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public async Task CopyToAsync_CancellationToken_Propagated(MemoryStream input)
        {
            using var src = input;
            src.WriteByte(0);
            src.Position = 0;

            CancellationToken cancellationToken = new CancellationTokenSource().Token;
            CancellationToken expectedToken = (input is CustomMemoryStream cms && cms.Sync) ? default(CancellationToken) : cancellationToken;
            await src.CopyToAsync(
                (_, __, token) => new ValueTask(Task.Run(() => Assert.Equal(expectedToken, token))),
                null,
                4096,
                cancellationToken
            );
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public async Task CopyToAsync_State_Propagated(MemoryStream input)
        {
            using var src = input;
            src.WriteByte(0);
            src.Position = 0;

            const int expected = 42;
            await src.CopyToAsync(
                (_, state, __) => new ValueTask(Task.Run(() => Assert.Equal(expected, state))),
                expected,
                4096,
                default
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(42)]
        [InlineData(100000)]
        public void CopyToAsync_StreamToken_ExpectedBufferSizePropagated(int length)
        {
            using var src = new CustomMemoryStream_BufferSize();
            src.Write(new byte[length], 0, length);
            src.Position = 0;

            Assert.Equal(length, ((Task<int>)src.CopyToAsync((_, __, ___) => default, null, length, default(CancellationToken))).Result);
        }

        private sealed class CustomMemoryStream_BufferSize : MemoryStream
        {
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
                Task.FromResult(bufferSize);
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public void CopyTo_AllDataCopied(MemoryStream input)
        {
            using var src = input;
            src.Write(Enumerable.Range(0, 10000).Select(i => (byte)i).ToArray(), 0, 256);
            src.Position = 0;

            using var dst = new MemoryStream();
            src.CopyTo((span, _) => dst.Write(span), null, 4096);

            Assert.Equal<byte>(src.ToArray(), dst.ToArray());
        }

        [Theory]
        [MemberData(nameof(CopyTo_TestData))]
        public async Task CopyToAsync_AllDataCopied(MemoryStream input)
        {
            using var src = input;
            src.Write(Enumerable.Range(0, 10000).Select(i => (byte)i).ToArray(), 0, 256);
            src.Position = 0;

            using var dst = new MemoryStream();
            await src.CopyToAsync((memory, _, ___) => dst.WriteAsync(memory), null, 4096, default);

            Assert.Equal<byte>(src.ToArray(), dst.ToArray());
        }

        private sealed class CustomMemoryStream : MemoryStream
        {
            private readonly bool _spanCopy;
            private readonly bool _sync;

            public bool Sync => _sync;

            public CustomMemoryStream(bool spanCopy, bool sync)
                : base()
            {
                _spanCopy = spanCopy;
                _sync = sync;
            }

            public override void CopyTo(Stream destination, int bufferSize)
            {
                if (_sync)
                {
                    CopyToInternal(destination, bufferSize);
                }
                else
                {
                    CopyToAsyncInternal(destination, bufferSize, CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (!_sync)
                {
                    return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
                }
                else
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return Task.FromCanceled(cancellationToken);
                        }

                        CopyToInternal(destination, bufferSize);
                        return Task.CompletedTask;
                    }
                    catch (Exception e)
                    {
                        return Task.FromException(e);
                    }
                }
            }

            private void CopyToInternal(Stream destination, int bufferSize)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    int read;
                    while ((read = Read(buffer, 0, buffer.Length)) != 0)
                    {
                        if (_spanCopy)
                            destination.Write(new ReadOnlySpan<byte>(buffer, 0, read));
                        else
                            destination.Write(buffer, 0, read);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private async Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    while (true)
                    {
                        int bytesRead = await ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0) break;
                        if (_spanCopy)
                            await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                        else
                            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public static IEnumerable<object[]> CopyTo_TestData()
        {
            foreach (var sync in new[] { false, true })
                foreach (var spanCopy in new[] { false, true })
                    yield return new object[] { new CustomMemoryStream(spanCopy, sync) };

            yield return new object[] { new MemoryStream() };
        }

        [Fact]
        public void IfCanSeekIsFalseLengthAndPositionShouldNotBeCalled()
        {
            var baseStream = new DelegateStream(
                canReadFunc: () => true,
                canSeekFunc: () => false,
                readFunc: (buffer, offset, count) => 0);
            var trackingStream = new CallTrackingStream(baseStream);

            trackingStream.CopyTo((_, __) => { }, null, 1);

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

            await trackingStream.CopyToAsync((_, __, ___) => default, null, 1, default(CancellationToken));

            Assert.InRange(trackingStream.TimesCalled(nameof(trackingStream.CanSeek)), 0, 1);
            Assert.Equal(0, trackingStream.TimesCalled(nameof(trackingStream.Length)));
            Assert.Equal(0, trackingStream.TimesCalled(nameof(trackingStream.Position)));
            Assert.Equal(1, trackingStream.TimesCalled(nameof(trackingStream.CopyToAsync)));

            Assert.InRange(trackingStream.CopyToAsyncBufferSize, 1, int.MaxValue);
            Assert.Equal(default(CancellationToken), trackingStream.CopyToAsyncCancellationToken);
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

            trackingStream.CopyTo((_, __) => { }, null, 1);

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

            await trackingStream.CopyToAsync((_, __, ___) => default, null, 1, default(CancellationToken));

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

            trackingStream.CopyTo((_, __) => { }, null, 1);

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

            await trackingStream.CopyToAsync((_, __, ___) => default, null, 1, default(CancellationToken));

            Assert.InRange(trackingStream.CopyToAsyncBufferSize, 1, int.MaxValue);
            Assert.Equal(default(CancellationToken), trackingStream.CopyToAsyncCancellationToken);
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

            trackingStream.CopyTo((_, __) => { }, null, 1);

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

            await trackingStream.CopyToAsync((_, __, ___) => default, null, 1, default(CancellationToken));

            // Note: We can't check how many times ReadAsync was called
            // here, since trackingStream overrides CopyToAsync and forwards
            // to the inner (non-tracking) stream for the implementation

            Assert.InRange(trackingStream.CopyToAsyncBufferSize, 1, int.MaxValue);
            Assert.Equal(default(CancellationToken), trackingStream.CopyToAsyncCancellationToken);
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

            int timesCalled = 0;
            src.CopyTo((_, __) => { timesCalled++; }, null, 1);

            Assert.Equal(ReadLimit, src.TimesCalled(nameof(src.Read)));
            Assert.Equal(ReadLimit - 1, timesCalled);
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

            int timesCalled = 0;
            await src.CopyToAsync((_, __, ___) => { timesCalled++; return default; }, null, 1, default(CancellationToken));

            // Since we override CopyToAsync in CallTrackingStream,
            // src.Read will actually not get called ReadLimit
            // times, src.Inner.Read will. So, we just assert that
            // CopyToAsync was called once for src.

            Assert.Equal(1, src.TimesCalled(nameof(src.CopyToAsync)));
            Assert.Equal(ReadLimit - 1, timesCalled); // dest.WriteAsync will still get called repeatedly
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
