// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

            Assert.NotNull(Channel.CreateUnbounded<int>());
            Assert.NotNull(Channel.CreateUnbounded<int>(new UnboundedChannelOptions()));
        }

        [Fact]
        public void Create_NullOptions_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException>("options", () => Channel.CreateUnbounded<int>(null));
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DefaultWriteAsync_UsesWaitToWriteAsyncAndTryWrite()
        {
            var c = new TestChannelWriter<int>(10);
            Assert.False(c.TryComplete());
            Assert.Equal(TaskStatus.Canceled, c.WriteAsync(42, new CancellationToken(true)).AsTask().Status);

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
            ValueTask t = w.WriteAsync(42);
            Assert.True(t.IsFaulted);
            await Assert.ThrowsAsync<FormatException>(async () => await t);
        }

        [Fact]
        public async Task DefaultReadAsync_CatchesTryWriteExceptions()
        {
            var r = new TryReadThrowingReader<int>();
            Task<int> t = r.ReadAsync().AsTask();
            Assert.Equal(TaskStatus.Faulted, t.Status);
            await Assert.ThrowsAsync<FieldAccessException>(() => t);
        }

        [Fact]
        public async Task TestBaseClassReadAsync()
        {
            WrapperChannel<int> channel = new WrapperChannel<int>(10);
            ChannelReader<int> reader = channel.Reader;
            ChannelWriter<int> writer = channel.Writer;

            // 1- do it through synchronous TryRead()
            writer.TryWrite(50);
            Assert.Equal(50, await reader.ReadAsync());

            // 2- do it through async
            ValueTask<int> readTask = reader.ReadAsync();
            writer.TryWrite(100);
            Assert.Equal(100, await readTask);

            // 3- use cancellation token
            CancellationToken ct = new CancellationToken(true); // cancelled token
            await Assert.ThrowsAsync<TaskCanceledException>(() => reader.ReadAsync(ct).AsTask());

            // 4- throw during reading
            readTask = reader.ReadAsync();
            ((WrapperChannelReader<int>)reader).ForceThrowing = true;
            writer.TryWrite(200);
            await Assert.ThrowsAsync<InvalidOperationException>(() => readTask.AsTask());

            // 5- close the channel while waiting reading
            ((WrapperChannelReader<int>)reader).ForceThrowing = false;
            Assert.Equal(200, await reader.ReadAsync());
            readTask = reader.ReadAsync();
            channel.Writer.TryComplete();
            await Assert.ThrowsAsync<ChannelClosedException>(() => readTask.AsTask());
        }

        [Fact]
        public void TestBaseClassTryPeek()
        {
            var reader = new TryPeekNoOverrideReader<int>();
            Assert.False(reader.CanPeek);
            Assert.False(reader.TryPeek(out int item));
            Assert.Equal(0, item);
        }

        // This reader doesn't override ReadAsync to force using the base class ReadAsync method
        private sealed class WrapperChannelReader<T> : ChannelReader<T>
        {
            private ChannelReader<T> _reader;
            internal bool ForceThrowing { get; set; }

            public WrapperChannelReader(Channel<T> channel) {_reader = channel.Reader; }

            public override bool TryRead(out T item)
            {
                if (ForceThrowing)
                    throw new InvalidOperationException();

                return _reader.TryRead(out item);
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
            {
                return _reader.WaitToReadAsync(cancellationToken);
            }
        }

        public class WrapperChannel<T> : Channel<T>
        {
            public WrapperChannel(int capacity)
            {
                Channel<T> channel = Channel.CreateBounded<T>(capacity);
                Writer = channel.Writer;
                Reader = new WrapperChannelReader<T>(channel);
            }
        }

        private sealed class TestChannelWriter<T> : ChannelWriter<T>
        {
            private readonly Random _rand = new Random(42);
            private readonly int _max;
            private int _count;

            public TestChannelWriter(int max) => _max = max;

            public override bool TryWrite(T item) => _rand.Next(0, 2) == 0 && _count++ < _max; // succeed if we're under our limit, and add random failures

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken) =>
                _count >= _max ? new ValueTask<bool>(Task.FromResult(false)) :
                _rand.Next(0, 2) == 0 ? new ValueTask<bool>(Task.Delay(1).ContinueWith(_ => true)) : // randomly introduce delays
                new ValueTask<bool>(Task.FromResult(true));
        }

        private sealed class TestChannelReader<T> : ChannelReader<T>
        {
            private Random _rand = new Random(42);
            private IEnumerator<T> _enumerator;
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
                item = _enumerator.Current;
                return true;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) => new ValueTask<bool>(
                _closed ? Task.FromResult(false) :
                _rand.Next(0, 2) == 0 ? Task.Delay(1).ContinueWith(_ => true) : // randomly introduce delays
                Task.FromResult(true));
        }

        private sealed class TryWriteThrowingWriter<T> : ChannelWriter<T>
        {
            public override bool TryWrite(T item) => throw new FormatException();
            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default) => throw new InvalidDataException();
        }

        private sealed class TryReadThrowingReader<T> : ChannelReader<T>
        {
            public override bool TryRead(out T item) => throw new FieldAccessException();
            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default) => throw new DriveNotFoundException();
        }

        private sealed class TryPeekNoOverrideReader<T> : ChannelReader<T>
        {
            public override bool TryRead([MaybeNullWhen(false)] out T item)
            {
                item = default;
                return false;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) => default;
        }

        private sealed class CanReadFalseStream : MemoryStream
        {
            public override bool CanRead => false;
        }
    }
}
