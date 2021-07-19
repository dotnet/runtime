// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
#if !BUILDING_SOURCE_GENERATOR_TESTS
    public abstract partial class CollectionTests
    {
        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteRootLevelAsyncEnumerable<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            string expectedJson = await JsonSerializerWrapperForString.SerializeWrapper(source);

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await JsonSerializerWrapperForStream.SerializeWrapper(stream, asyncEnumerable, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.ToString());
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteNestedAsyncEnumerable<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            string expectedJson = await JsonSerializerWrapperForString.SerializeWrapper(new { Data = source });

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await JsonSerializerWrapperForStream.SerializeWrapper(stream, new { Data = asyncEnumerable }, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.ToString());
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteNestedAsyncEnumerable_DTO<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            string expectedJson = await JsonSerializerWrapperForString.SerializeWrapper(new { Data = source });

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await JsonSerializerWrapperForStream.SerializeWrapper(stream, new AsyncEnumerableDto<TElement> { Data = asyncEnumerable }, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.ToString());
            Assert.Equal(1, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Fact, OuterLoop]
        public async Task WriteAsyncEnumerable_LongRunningEnumeration_Cancellation()
        {
            var longRunningEnumerable = new MockedAsyncEnumerable<int>(
                source: Enumerable.Range(1, 100),
                delayInterval: 1,
                delay: TimeSpan.FromMinutes(1));

            using var utf8Stream = new Utf8MemoryStream();
            using var cts = new CancellationTokenSource(delay: TimeSpan.FromSeconds(5));
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await JsonSerializer.SerializeAsync(utf8Stream, longRunningEnumerable, cancellationToken: cts.Token));

            Assert.Equal(1, longRunningEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, longRunningEnumerable.TotalDisposedEnumerators);
        }

        public class AsyncEnumerableDto<TElement>
        {
            public IAsyncEnumerable<TElement> Data { get; set; }
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteSequentialNestedAsyncEnumerables<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            string expectedJson = await JsonSerializerWrapperForString.SerializeWrapper(new { Data1 = source, Data2 = source });

            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            await JsonSerializerWrapperForStream.SerializeWrapper(stream, new { Data1 = asyncEnumerable, Data2 = asyncEnumerable }, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.ToString());
            Assert.Equal(2, asyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(2, asyncEnumerable.TotalDisposedEnumerators);
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task WriteAsyncEnumerableOfAsyncEnumerables<TElement>(IEnumerable<TElement> source, int delayInterval, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            const int OuterEnumerableCount = 5;
            string expectedJson = await JsonSerializerWrapperForString.SerializeWrapper(Enumerable.Repeat(source, OuterEnumerableCount));

            var innerAsyncEnumerable = new MockedAsyncEnumerable<TElement>(source, delayInterval);
            var outerAsyncEnumerable =
                new MockedAsyncEnumerable<IAsyncEnumerable<TElement>>(
                    Enumerable.Repeat(innerAsyncEnumerable, OuterEnumerableCount), delayInterval);

            using var stream = new Utf8MemoryStream();
            await JsonSerializerWrapperForStream.SerializeWrapper(stream, outerAsyncEnumerable, options);

            JsonTestHelper.AssertJsonEqual(expectedJson, stream.ToString());
            Assert.Equal(1, outerAsyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(1, outerAsyncEnumerable.TotalDisposedEnumerators);
            Assert.Equal(OuterEnumerableCount, innerAsyncEnumerable.TotalCreatedEnumerators);
            Assert.Equal(OuterEnumerableCount, innerAsyncEnumerable.TotalDisposedEnumerators);
        }

        [Fact]
        public async Task WriteRootLevelAsyncEnumerableSync_ThrowsNotSupportedException()
        {
            IAsyncEnumerable<int> asyncEnumerable = new MockedAsyncEnumerable<int>(Enumerable.Range(1, 10));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(asyncEnumerable));
        }

        [Fact]
        public async Task WriteNestedAsyncEnumerableSync_ThrowsNotSupportedException()
        {
            IAsyncEnumerable<int> asyncEnumerable = new MockedAsyncEnumerable<int>(Enumerable.Range(1, 10));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(new { Data = asyncEnumerable }));
        }

        [Fact]
        public async Task WriteAsyncEnumerable_ElementSerializationThrows_ShouldDisposeEnumerator()
        {
            using var stream = new Utf8MemoryStream();
            var asyncEnumerable = new MockedAsyncEnumerable<IEnumerable<int>>(Enumerable.Repeat(ThrowingEnumerable(), 2));

            await Assert.ThrowsAsync<DivideByZeroException>(async () => await JsonSerializerWrapperForStream.SerializeWrapper(stream, new { Data = asyncEnumerable }));
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
            var utf8Stream = new Utf8MemoryStream("[0,1,2,3,4]");

            IAsyncEnumerable<int> result = await JsonSerializer.DeserializeAsync<IAsyncEnumerable<int>>(utf8Stream);
            Assert.Equal(new int[] { 0, 1, 2, 3, 4 }, await result.ToListAsync());
        }

        [Fact]
        public async Task ReadNestedAsyncEnumerable()
        {
            var utf8Stream = new Utf8MemoryStream(@"{ ""Data"" : [0,1,2,3,4] }");

            var result = await JsonSerializer.DeserializeAsync<AsyncEnumerableDto<int>>(utf8Stream);
            Assert.Equal(new int[] { 0, 1, 2, 3, 4 }, await result.Data.ToListAsync());
        }

        [Fact]
        public async Task ReadAsyncEnumerableOfAsyncEnumerables()
        {
            var utf8Stream = new Utf8MemoryStream("[[0,1,2,3,4], []]");

            var result = await JsonSerializer.DeserializeAsync<IAsyncEnumerable<IAsyncEnumerable<int>>>(utf8Stream);
            var resultArray = await result.ToListAsync();

            Assert.Equal(2, resultArray.Count);
            Assert.Equal(new int[] { 0, 1, 2, 3, 4 }, await resultArray[0].ToListAsync());
            Assert.Equal(Array.Empty<int>(), await resultArray[1].ToListAsync());
        }

        [Fact]
        public async Task ReadRootLevelAsyncEnumerableDerivative_ThrowsNotSupportedException()
        {
            var utf8Stream = new Utf8MemoryStream("[0,1,2,3,4]");
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializer.DeserializeAsync<MockedAsyncEnumerable<int>>(utf8Stream));
        }

        public static IEnumerable<object[]> GetAsyncEnumerableSources()
        {
            yield return WrapArgs(Enumerable.Empty<int>(), 0, 1);
            yield return WrapArgs(Enumerable.Range(0, 20), 0, 1);
            yield return WrapArgs(Enumerable.Range(0, 100), 20, 20);
            yield return WrapArgs(Enumerable.Range(0, 1000), 20, 20);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 20, 100);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 3, 100);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 20, 100);

            static object[] WrapArgs<TSource>(IEnumerable<TSource> source, int delayInterval, int bufferSize) => new object[]{ source, delayInterval, bufferSize };
        }

        private class MockedAsyncEnumerable<TElement> : IAsyncEnumerable<TElement>, IEnumerable<TElement>
        {
            private readonly IEnumerable<TElement> _source;
            private readonly TimeSpan _delay;
            private readonly int _delayInterval;

            internal int TotalCreatedEnumerators { get; private set; }
            internal int TotalDisposedEnumerators { get; private set; }
            internal int TotalEnumeratedElements { get; private set; }

            public MockedAsyncEnumerable(IEnumerable<TElement> source, int delayInterval = 0, TimeSpan? delay = null)
            {
                _source = source;
                _delay = delay ?? TimeSpan.FromMilliseconds(20);
                _delayInterval = delayInterval;
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
                        await Task.Delay(_delay, cancellationToken);
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

        public class Utf8MemoryStream : MemoryStream
        {
            public Utf8MemoryStream() : base()
            {
            }

            public Utf8MemoryStream(string text) : base(Encoding.UTF8.GetBytes(text))
            {
            }

            public override string ToString () => Encoding.UTF8.GetString(ToArray());
        }
    }
#endif
}
