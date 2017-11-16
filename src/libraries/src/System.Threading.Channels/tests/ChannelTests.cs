// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class ChannelTests
    {
        [Fact]
        public void ChannelOptimizations_Properties_Roundtrip()
        {
            var co = new UnboundedChannelOptions();

            Assert.False(co.SingleReader);
            Assert.False(co.SingleWriter);

            co.SingleReader = true;
            Assert.True(co.SingleReader);
            Assert.False(co.SingleWriter);
            co.SingleReader = false;
            Assert.False(co.SingleReader);

            co.SingleWriter = true;
            Assert.False(co.SingleReader);
            Assert.True(co.SingleWriter);
            co.SingleWriter = false;
            Assert.False(co.SingleWriter);

            co.SingleReader = true;
            co.SingleWriter = true;
            Assert.True(co.SingleReader);
            Assert.True(co.SingleWriter);

            Assert.False(co.AllowSynchronousContinuations);
            co.AllowSynchronousContinuations = true;
            Assert.True(co.AllowSynchronousContinuations);
            co.AllowSynchronousContinuations = false;
            Assert.False(co.AllowSynchronousContinuations);
        }

        [Fact]
        public void Create_ValidInputs_ProducesValidChannels()
        {
            Assert.NotNull(Channel.CreateBounded<int>(1));
            Assert.NotNull(Channel.CreateBounded<int>(new BoundedChannelOptions(1)));

            Assert.NotNull(Channel.CreateUnbuffered<int>());
            Assert.NotNull(Channel.CreateUnbuffered<int>(new UnbufferedChannelOptions()));

            Assert.NotNull(Channel.CreateUnbounded<int>());
            Assert.NotNull(Channel.CreateUnbounded<int>(new UnboundedChannelOptions()));
        }

        [Fact]
        public void Create_NullOptions_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException>("options", () => Channel.CreateUnbounded<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("options", () => Channel.CreateUnbuffered<int>(null));
            AssertExtensions.Throws<ArgumentNullException>("options", () => Channel.CreateBounded<int>(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void CreateBounded_InvalidBufferSizes_ThrowArgumentExceptions(int capacity)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => Channel.CreateBounded<int>(capacity));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new BoundedChannelOptions(capacity));
        }

        [Theory]
        [InlineData((BoundedChannelFullMode)(-1))]
        [InlineData((BoundedChannelFullMode)(4))]
        public void BoundedChannelOptions_InvalidModes_ThrowArgumentExceptions(BoundedChannelFullMode mode) =>
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new BoundedChannelOptions(1) { FullMode = mode });

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void BoundedChannelOptions_InvalidCapacity_ThrowArgumentExceptions(int capacity) =>
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => new BoundedChannelOptions(1) { Capacity = capacity });

        [Theory]
        [InlineData(1)]
        public void CreateBounded_ValidBufferSizes_Success(int bufferedCapacity) =>
            Assert.NotNull(Channel.CreateBounded<int>(bufferedCapacity));

        [Fact]
        public async Task DefaultWriteAsync_UsesWaitToWriteAsyncAndTryWrite()
        {
            var c = new TestChannelWriter<int>(10);
            Assert.False(c.TryComplete());
            Assert.Equal(TaskStatus.Canceled, c.WriteAsync(42, new CancellationToken(true)).Status);

            int count = 0;
            try
            {
                while (true)
                {
                    await c.WriteAsync(count++);
                }
            }
            catch (ChannelClosedException) { }
            Assert.Equal(11, count);
        }

        [Fact]
        public void DefaultCompletion_NeverCompletes()
        {
            Task t = new TestChannelReader<int>(Enumerable.Empty<int>()).Completion;
            Assert.False(t.IsCompleted);
        }

        [Fact]
        public async Task DefaultWriteAsync_CatchesTryWriteExceptions()
        {
            var w = new TryWriteThrowingWriter<int>();
            Task t = w.WriteAsync(42);
            Assert.Equal(TaskStatus.Faulted, t.Status);
            await Assert.ThrowsAsync<FormatException>(() => t);
        }

        [Fact]
        public async Task DefaultReadAsync_CatchesTryWriteExceptions()
        {
            var r = new TryReadThrowingReader<int>();
            Task<int> t = r.ReadAsync().AsTask();
            Assert.Equal(TaskStatus.Faulted, t.Status);
            await Assert.ThrowsAsync<FieldAccessException>(() => t);
        }

        private sealed class TestChannelWriter<T> : ChannelWriter<T>
        {
            private readonly Random _rand = new Random(42);
            private readonly int _max;
            private int _count;

            public TestChannelWriter(int max) => _max = max;

            public override bool TryWrite(T item) => _rand.Next(0, 2) == 0 && _count++ < _max; // succeed if we're under our limit, and add random failures

            public override Task<bool> WaitToWriteAsync(CancellationToken cancellationToken) =>
                _count >= _max ? Task.FromResult(false) :
                _rand.Next(0, 2) == 0 ? Task.Delay(1).ContinueWith(_ => true) : // randomly introduce delays
                Task.FromResult(true);
        }

        private sealed class TestChannelReader<T> : ChannelReader<T>
        {
            private Random _rand = new Random(42);
            private IEnumerator<T> _enumerator;
            private int _count;
            private bool _closed;

            public TestChannelReader(IEnumerable<T> enumerable) => _enumerator = enumerable.GetEnumerator();

            public override bool TryRead(out T item)
            {
                // Randomly fail to read
                if (_rand.Next(0, 2) == 0)
                {
                    item = default;
                    return false;
                }

                // If the enumerable is closed, fail the read.
                if (!_enumerator.MoveNext())
                {
                    _enumerator.Dispose();
                    _closed = true;
                    item = default;
                    return false;
                }

                // Otherwise return the next item.
                _count++;
                item = _enumerator.Current;
                return true;
            }

            public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken) =>
                _closed ? Task.FromResult(false) :
                _rand.Next(0, 2) == 0 ? Task.Delay(1).ContinueWith(_ => true) : // randomly introduce delays
                Task.FromResult(true);
        }

        private sealed class TryWriteThrowingWriter<T> : ChannelWriter<T>
        {
            public override bool TryWrite(T item) => throw new FormatException();
            public override Task<bool> WaitToWriteAsync(CancellationToken cancellationToken = default) => throw new InvalidDataException();
        }

        private sealed class TryReadThrowingReader<T> : ChannelReader<T>
        {
            public override bool TryRead(out T item) => throw new FieldAccessException();
            public override Task<bool> WaitToReadAsync(CancellationToken cancellationToken = default) => throw new DriveNotFoundException();
        }

        private sealed class CanReadFalseStream : MemoryStream
        {
            public override bool CanRead => false;
        }
    }
}
