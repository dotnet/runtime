// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteRootLevelAsyncEnumerable<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.DefaultBufferSize = bufferSize;

            string expectedJson = await Serializer.SerializeWrapper(source, options);

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await StreamingSerializer.SerializeWrapper(stream, asyncEnumerable, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.AsString());
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteNestedAsyncEnumerable<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.DefaultBufferSize = bufferSize;

            string expectedJson = await Serializer.SerializeWrapper(new EnumerableDto<TElement> { Data = source }, options);

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDto<TElement> { Data = asyncEnumerable }, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.AsString());
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteNestedAsyncEnumerable_Nullable<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            // Primarily tests the ability of NullableConverter to flow async serialization state

            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.DefaultBufferSize = bufferSize;
            options.IncludeFields = true;

            string expectedJson = await Serializer.SerializeWrapper<(IEnumerable<TElement>, bool)?>((source, false), options);

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await StreamingSerializer.SerializeWrapper<(IAsyncEnumerable<TElement>, bool)?>(stream, (asyncEnumerable, false), options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.AsString());
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Fact]
        public async Task WriteAsyncEnumerable_CancellationToken_IsPassedToAsyncEnumerator()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/79556
            using var utf8Stream = new Utf8MemoryStream(ignoreCancellationTokenOnWriteAsync: true);
            using var cts = new CancellationTokenSource();

            IAsyncEnumerable<int> value = CreateEnumerable();
            await JsonSerializer.SerializeAsync(utf8Stream, value, Serializer.DefaultOptions.GetTypeInfo<IAsyncEnumerable<int>>(), cts.Token);
            Assert.Equal("[1,2]", utf8Stream.AsString());

            async IAsyncEnumerable<int> CreateEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return 1;
                await Task.Delay(20);
                Assert.False(cancellationToken.IsCancellationRequested);
                cts.Cancel();
                Assert.True(cancellationToken.IsCancellationRequested);
                yield return 2;
            }
        }

        [Theory, OuterLoop]
        [InlineData(5000, 1000, true)]
        [InlineData(5000, 1000, false)]
        [InlineData(1000, 10_000, true)]
        [InlineData(1000, 10_000, false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80020", TestRuntimes.Mono)]
        public async Task WriteAsyncEnumerable_LongRunningEnumeration_Cancellation(
            int cancellationTokenSourceDelayMilliseconds,
            int enumeratorDelayMilliseconds,
            bool passCancellationTokenToDelayTask)
        {
            var longRunningEnumerable = new MockedAsyncEnumerable<int>(
                source: Enumerable.Range(1, 1000),
                delayInterval: 1,
                delay: TimeSpan.FromMilliseconds(enumeratorDelayMilliseconds),
                passCancellationTokenToDelayTask);

            using var utf8Stream = new Utf8MemoryStream();
            using var cts = new CancellationTokenSource(delay: TimeSpan.FromMilliseconds(cancellationTokenSourceDelayMilliseconds));
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await JsonSerializer.SerializeAsync(utf8Stream, longRunningEnumerable, Serializer.DefaultOptions.GetTypeInfo<IAsyncEnumerable<int>>(), cts.Token));

            Assert.Equal(1, longRunningEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, longRunningEnumerable.TotalDisposedEnumerators);
        }

        public class EnumerableDto<TElement>
        {
            public IEnumerable<TElement> Data { get; set; }
        }

        public class AsyncEnumerableDto<TElement>
        {
            public IAsyncEnumerable<TElement> Data { get; set; }
        }

        public class EnumerableDtoWithTwoProperties<TElement>
        {
            public IEnumerable<TElement> Data1 { get; set; }
            public IEnumerable<TElement> Data2 { get; set; }
        }

        public class AsyncEnumerableDtoWithTwoProperties<TElement>
        {
            public IAsyncEnumerable<TElement> Data1 { get; set; }
            public IAsyncEnumerable<TElement> Data2 { get; set; }
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteSequentialNestedAsyncEnumerables<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.DefaultBufferSize = bufferSize;

            string expectedJson = await Serializer.SerializeWrapper(new EnumerableDtoWithTwoProperties<TElement> { Data1 = source, Data2 = source }, options);

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDtoWithTwoProperties<TElement> { Data1 = asyncEnumerable, Data2 = asyncEnumerable }, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.AsString());
            Assert.Equal(2, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(2, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteAsyncEnumerableOfAsyncEnumerables<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.DefaultBufferSize = bufferSize;

            const int OuterEnumerableCount = 5;
            string expectedJson = await Serializer.SerializeWrapper(Enumerable.Repeat(source, OuterEnumerableCount), options);

            var innerAsyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            var outerAsyncEnumerable =
                new MockedAsyncEnumerable<IAsyncEnumerable<TElement>>(
                    Enumerable.Repeat(innerAsyncEnumerable, OuterEnumerableCount), delayInterval);

            using var stream = new Utf8MemoryStream();
            await StreamingSerializer.SerializeWrapper(stream, outerAsyncEnumerable, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.AsString());
            Assert.Equal(1, outerAsyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, outerAsyncEnumerable.TotalDisposedEnumerators);
            Assert.Equal(OuterEnumerableCount, innerAsyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(OuterEnumerableCount, innerAsyncEnumerable.TotalDisposedEnumerators);
        }

        [Fact]
        public void WriteRootLevelAsyncEnumerableSync_ThrowsNotSupportedException()
        {
            IAsyncEnumerable<int> asyncEnumerable = new MockedAsyncEnumerable<int>(Enumerable.Range(1, 10));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(asyncEnumerable, Serializer.DefaultOptions.GetTypeInfo<IAsyncEnumerable<int>>()));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new MemoryStream(), asyncEnumerable, Serializer.DefaultOptions.GetTypeInfo<IAsyncEnumerable<int>>()));
        }

        [Fact]
        public void WriteNestedAsyncEnumerableSync_ThrowsNotSupportedException()
        {
            IAsyncEnumerable<int> asyncEnumerable = new MockedAsyncEnumerable<int>(Enumerable.Range(1, 10));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new AsyncEnumerableDto<int> { Data = asyncEnumerable }, Serializer.DefaultOptions.GetTypeInfo<AsyncEnumerableDto<int>>()));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new MemoryStream(), new AsyncEnumerableDto<int> { Data = asyncEnumerable }, Serializer.DefaultOptions.GetTypeInfo<AsyncEnumerableDto<int>>()));
        }

        [Fact]
        public async Task WriteAsyncEnumerable_ElementSerializationThrows_ShouldDisposeEnumerator()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<IEnumerable<int>>(Enumerable.Repeat(ThrowingEnumerable(), 2));

            await Assert.ThrowsAsync<DivideByZeroException>(async () => await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDto<IEnumerable<int>> { Data = asyncEnumerable }));
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);

            static IEnumerable<int> ThrowingEnumerable()
            {
                yield return 0;
                throw new DivideByZeroException();
            }
        }

        [Fact]
        public async Task ReadRootLevelAsyncEnumerable()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var utf8Stream = new Utf8MemoryStream("[0,1,2,3,4]");

            IAsyncEnumerable<int> result = await StreamingSerializer.DeserializeWrapper<IAsyncEnumerable<int>>(utf8Stream);
            Assert.Equal(new int[] { 0, 1, 2, 3, 4 }, await result.ToListAsync());
        }

        [Fact]
        public async Task ReadNestedAsyncEnumerable()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var utf8Stream = new Utf8MemoryStream("""{ "Data" : [0,1,2,3,4] }""");

            var result = await StreamingSerializer.DeserializeWrapper<AsyncEnumerableDto<int>>(utf8Stream);
            Assert.Equal(new int[] { 0, 1, 2, 3, 4 }, await result.Data.ToListAsync());
        }

        [Fact]
        public async Task ReadAsyncEnumerableOfAsyncEnumerables()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var utf8Stream = new Utf8MemoryStream("[[0,1,2,3,4], []]");

            var result = await StreamingSerializer.DeserializeWrapper<IAsyncEnumerable<IAsyncEnumerable<int>>>(utf8Stream);
            var resultArray = await result.ToListAsync();

            Assert.Equal(2, resultArray.Count);
            Assert.Equal(new int[] { 0, 1, 2, 3, 4 }, await resultArray[0].ToListAsync());
            Assert.Equal(Array.Empty<int>(), await resultArray[1].ToListAsync());
        }

        [Fact]
        public async Task ReadRootLevelAsyncEnumerableDerivative_ThrowsNotSupportedException()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var utf8Stream = new Utf8MemoryStream("[0,1,2,3,4]");
            await Assert.ThrowsAsync<NotSupportedException>(async () => await StreamingSerializer.DeserializeWrapper<MockedAsyncEnumerable<int>>(utf8Stream));
        }

        public static IEnumerable<object[]> GetAsyncEnumerableSources()
        {
            yield return WrapArgs(Enumerable.Empty<int>(), 0, 1);
            yield return WrapArgs(Enumerable.Range(0, 20), 0, 1);
            yield return WrapArgs(Enumerable.Range(0, 100), 20, 20);
            yield return WrapArgs(Enumerable.Range(0, 1000), 20, 20);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 20, 100);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new AsyncEnumerableElement(i, $"lorem ipsum dolor: {i}", (i % 2 == 0))), 3, 100);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => new AsyncEnumerableElement(i, $"lorem ipsum dolor: {i}", (i % 2 == 0))), 20, 100);

            static object[] WrapArgs<TSource>(IEnumerable<TSource> source, int delayInterval, int bufferSize) => new object[] { source, delayInterval, bufferSize };
        }

        public record AsyncEnumerableElement(int Field1, string Field2, bool Field3);

        [Fact]
        public async Task RegressionTest_DisposingEnumeratorOnPendingMoveNextAsyncOperation()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            // Regression test for https://github.com/dotnet/runtime/issues/57360
            using var stream = new Utf8MemoryStream();
            using var cts = new CancellationTokenSource(millisecondsDelay: 1000);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await JsonSerializer.SerializeAsync(stream, GetNumbersAsync(), Serializer.DefaultOptions.GetTypeInfo<IAsyncEnumerable<int>>(), cts.Token));

            static async IAsyncEnumerable<int> GetNumbersAsync()
            {
                int i = 0;
                while (true)
                {
                    await Task.Delay(100);
                    yield return i++;
                }
            }
        }

        [Fact]
        public async Task RegressionTest_ExceptionOnFirstMoveNextShouldNotFlushBuffer()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            // Regression test for https://github.com/dotnet/aspnetcore/issues/36977
            using var stream = new MemoryStream();
            await Assert.ThrowsAsync<NotImplementedException>(async () => await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDto<int> { Data = GetFailingAsyncEnumerable() }));
            Assert.Equal(0, stream.Length);

            static async IAsyncEnumerable<int> GetFailingAsyncEnumerable()
            {
                await Task.Yield();
                throw new NotImplementedException();
#pragma warning disable CS0162 // Unreachable code detected
                yield break;
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteSequentialNestedAsyncEnumerables_EachEnumeratorDisposedBeforeNextStarts(bool asyncDisposal)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var events = new List<string>();
            int firstDisposed = 0;
            int secondStarted = 0;
            var enumerable1 = new DisposalTrackingAsyncEnumerable<int>(
                new[] { 1, 2, 3 }, "A", events, asyncDisposal,
                onDisposed: () => Interlocked.Exchange(ref firstDisposed, 1));
            var enumerable2 = new DisposalTrackingAsyncEnumerable<int>(
                new[] { 4, 5, 6 }, "B", events, asyncDisposal,
                onFirstMoveNext: () =>
                {
                    if (Volatile.Read(ref firstDisposed) == 0)
                    {
                        throw new InvalidOperationException("First async enumerator was not fully disposed before second started.");
                    }

                    Interlocked.Exchange(ref secondStarted, 1);
                });

            using var stream = new Utf8MemoryStream();
            await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDtoWithTwoProperties<int>
            {
                Data1 = enumerable1,
                Data2 = enumerable2,
            });

            JsonTestHelper.AssertJsonEqual("""{"Data1":[1,2,3],"Data2":[4,5,6]}""", stream.AsString());
            Assert.Equal(1, Volatile.Read(ref firstDisposed));
            Assert.Equal(1, Volatile.Read(ref secondStarted));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteNestedAsyncEnumerableInsideAsyncEnumerable_InnerEnumeratorsDisposedPromptly(bool asyncDisposal)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var events = new List<string>();
            var inner1 = new DisposalTrackingAsyncEnumerable<int>(
                new[] { 1, 2 }, "Inner1", events, asyncDisposal);
            var inner2 = new DisposalTrackingAsyncEnumerable<int>(
                new[] { 3, 4 }, "Inner2", events, asyncDisposal);

            var outer = new DisposalTrackingAsyncEnumerable<IAsyncEnumerable<int>>(
                new IAsyncEnumerable<int>[] { inner1, inner2 }, "Outer", events, asyncDisposal);

            using var stream = new Utf8MemoryStream();
            await JsonSerializer.SerializeAsync(stream, (IAsyncEnumerable<IAsyncEnumerable<int>>)outer, Serializer.DefaultOptions.GetTypeInfo<IAsyncEnumerable<IAsyncEnumerable<int>>>());

            JsonTestHelper.AssertJsonEqual("[[1,2],[3,4]]", stream.AsString());

            Assert.Contains("Inner1:Disposed", events);
            Assert.Contains("Inner2:Disposed", events);
            Assert.Contains("Outer:Disposed", events);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteEmptyNestedAsyncEnumerables_AllDisposed(bool asyncDisposal)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            var events = new List<string>();
            int firstDisposed = 0;
            int secondStarted = 0;
            var enumerable1 = new DisposalTrackingAsyncEnumerable<int>(
                Array.Empty<int>(), "A", events, asyncDisposal,
                onDisposed: () => Interlocked.Exchange(ref firstDisposed, 1));
            var enumerable2 = new DisposalTrackingAsyncEnumerable<int>(
                Array.Empty<int>(), "B", events, asyncDisposal,
                onFirstMoveNext: () =>
                {
                    if (Volatile.Read(ref firstDisposed) == 0)
                    {
                        throw new InvalidOperationException("First async enumerator was not fully disposed before second started.");
                    }

                    Interlocked.Exchange(ref secondStarted, 1);
                });

            using var stream = new Utf8MemoryStream();
            await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDtoWithTwoProperties<int>
            {
                Data1 = enumerable1,
                Data2 = enumerable2,
            });

            JsonTestHelper.AssertJsonEqual("""{"Data1":[],"Data2":[]}""", stream.AsString());
            Assert.Equal(1, Volatile.Read(ref firstDisposed));
            Assert.Equal(1, Volatile.Read(ref secondStarted));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WriteAsyncEnumerable_DisposeAsyncThrows_ExceptionPropagated(bool throwAfterAsyncBoundary)
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            using var stream = new Utf8MemoryStream();
            var enumerable = new ThrowingDisposeAsyncEnumerable<int>(new[] { 1, 2 }, throwAfterAsyncBoundary);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await StreamingSerializer.SerializeWrapper(stream, new AsyncEnumerableDto<int> { Data = enumerable }));
        }

        private sealed class DisposalTrackingAsyncEnumerable<TElement>(
            IEnumerable<TElement> source,
            string id,
            List<string> events,
            bool asyncDisposal,
            Action? onFirstMoveNext = null,
            Action? onDisposed = null) : IAsyncEnumerable<TElement>
        {
            public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new Enumerator(source.GetEnumerator(), id, events, asyncDisposal, onFirstMoveNext, onDisposed);

            private sealed class Enumerator(
                IEnumerator<TElement> inner,
                string id,
                List<string> events,
                bool asyncDisposal,
                Action? onFirstMoveNext,
                Action? onDisposed) : IAsyncEnumerator<TElement>
            {
                private bool _isFirstMoveNext = true;

                public TElement Current => inner.Current;

                public ValueTask<bool> MoveNextAsync()
                {
                    if (_isFirstMoveNext)
                    {
                        _isFirstMoveNext = false;
                        onFirstMoveNext?.Invoke();
                    }

                    bool result = inner.MoveNext();
                    events.Add($"{id}:MoveNext");
                    return new ValueTask<bool>(result);
                }

                public ValueTask DisposeAsync()
                {
                    inner.Dispose();
                    if (asyncDisposal)
                    {
                        return CompleteAsyncDispose();
                    }

                    onDisposed?.Invoke();
                    events.Add($"{id}:Disposed");
                    return default;

                    async ValueTask CompleteAsyncDispose()
                    {
                        await Task.Yield();
                        onDisposed?.Invoke();
                        events.Add($"{id}:Disposed");
                    }
                }
            }
        }

        private sealed class ThrowingDisposeAsyncEnumerable<TElement>(IEnumerable<TElement> source, bool throwAfterAsyncBoundary) : IAsyncEnumerable<TElement>
        {
            public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new Enumerator(source.GetEnumerator(), throwAfterAsyncBoundary);

            private sealed class Enumerator(IEnumerator<TElement> inner, bool throwAfterAsyncBoundary) : IAsyncEnumerator<TElement>
            {
                public TElement Current => inner.Current;
                public ValueTask<bool> MoveNextAsync()
                {
                    bool result = inner.MoveNext();
                    return new ValueTask<bool>(result);
                }

                public ValueTask DisposeAsync()
                {
                    inner.Dispose();
                    if (throwAfterAsyncBoundary)
                    {
                        return ThrowAfterAsyncBoundary();
                    }

                    throw new InvalidOperationException("Simulated DisposeAsync failure for testing");

                    static async ValueTask ThrowAfterAsyncBoundary()
                    {
                        await Task.Yield();
                        throw new InvalidOperationException("Simulated asynchronous DisposeAsync failure for testing");
                    }
                }
            }
        }

        public class MockedAsyncEnumerable<TElement> : IAsyncEnumerable<TElement>, IEnumerable<TElement>
        {
            private readonly IEnumerable<TElement> _source;
            private readonly TimeSpan _delay;
            private readonly int _delayInterval;
            private readonly bool _passCancellationTokenToDelayTask;

            public int TotalCreatedEnumerators { get; private set; }
            public int TotalDisposedEnumerators { get; private set; }
            public int TotalEnumeratedElements { get; private set; }

            public MockedAsyncEnumerable(IEnumerable<TElement> source, int delayInterval = 0, TimeSpan? delay = null, bool passCancellationTokenToDelayTask = true)
            {
                _source = source;
                _delay = delay ?? TimeSpan.FromMilliseconds(20);
                _delayInterval = delayInterval;
                _passCancellationTokenToDelayTask = passCancellationTokenToDelayTask;
            }

            public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new MockedAsyncEnumerator(this, cancellationToken);
            }

            // Enumerator class required to instrument IAsyncDisposable calls
            private class MockedAsyncEnumerator : IAsyncEnumerator<TElement>
            {
                private readonly MockedAsyncEnumerable<TElement> _enumerable;
                private IAsyncEnumerator<TElement> _innerEnumerator;

                public MockedAsyncEnumerator(MockedAsyncEnumerable<TElement> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    _innerEnumerator = enumerable.GetAsyncEnumeratorInner(token);
                }

                public TElement Current => _innerEnumerator.Current;
                public ValueTask DisposeAsync()
                {
                    _enumerable.TotalDisposedEnumerators++;
                    return _innerEnumerator.DisposeAsync();
                }

                public ValueTask<bool> MoveNextAsync() => _innerEnumerator.MoveNextAsync();
            }

            private async IAsyncEnumerator<TElement> GetAsyncEnumeratorInner(CancellationToken cancellationToken = default)
            {
                TotalCreatedEnumerators++;
                int i = 0;
                foreach (TElement element in _source)
                {
                    if (i > 0 && _delayInterval > 0 && i % _delayInterval == 0)
                    {
                        await Task.Delay(_delay, _passCancellationTokenToDelayTask ? cancellationToken : default);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    TotalEnumeratedElements++;
                    yield return element;
                    i++;
                }
            }

            public IEnumerator<TElement> GetEnumerator() => throw new InvalidOperationException("Collection should not be enumerated synchronously.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
