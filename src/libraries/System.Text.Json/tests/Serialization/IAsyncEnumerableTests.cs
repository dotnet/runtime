using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class IAsyncEnumerableTests
    {
        [Fact]
        public static async Task ReadIAsyncEnumerableOfReferenceType()
        {
            // Arrange
            string data = "[\"a\",\"b\",\"c\"]";

            var expected = new Queue<string>(JsonSerializer.Deserialize<IEnumerable<string>>(data));

            // Act
            var actual = JsonSerializer.Deserialize<IAsyncEnumerable<string>>(data);

            // Assert
            await foreach (var actualItem in actual)
                Assert.Equal(expected.Dequeue(), actualItem);
        }

        [Fact]
        public static async Task ReadNestedIAsyncEnumerableOfReferenceTypeDepthFirst()
        {
            // Arrange
            string data = "[[\"a\",\"b\",\"c\"],[\"d\",\"e\",\"f\"],[\"g\",\"h\",\"i\"]]";

            var expectedSequences = new Queue<Queue<string>>(
                JsonSerializer.Deserialize<IEnumerable<IEnumerable<string>>>(data)
                    .Select(x => new Queue<string>(x)));

            // Act
            var actual = JsonSerializer.Deserialize<IAsyncEnumerable<IAsyncEnumerable<string>>>(data);

            // Assert
            await foreach (var actualNested in actual)
            {
                var expectedNested = expectedSequences.Dequeue();

                await foreach (var actualItem in actualNested)
                    Assert.Equal(expectedNested.Dequeue(), actualItem);
            }
        }

        [Fact]
        public static async Task ReadNestedIAsyncEnumerableOfReferenceTypeInterleaved()
        {
            // Arrange
            string data = "[[\"a\",\"b\",\"c\"],[\"d\",\"e\",\"f\"],[\"g\",\"h\",\"i\"]]";

            var expectedSequences = JsonSerializer.Deserialize<IEnumerable<IEnumerable<string>>>(data)
                    .Select(x => new Queue<string>(x));

            // Act
            var actual = JsonSerializer.Deserialize<IAsyncEnumerable<IAsyncEnumerable<string>>>(data);

            // Assert
            var expectedEnumerators = expectedSequences.Select(seq => seq.GetEnumerator()).ToArray();
            var actualEnumerators = new List<IAsyncEnumerator<string>>();

            await foreach (var actualNested in actual)
                actualEnumerators.Add(actualNested.GetAsyncEnumerator());

            for (int elementIndex = 0; elementIndex < 3; elementIndex++)
            {
                for (int enumeratorIndex = 0; enumeratorIndex < 3; enumeratorIndex++)
                {
                    Assert.True(expectedEnumerators[enumeratorIndex].MoveNext(), $"Test integrity error: Expected value enumerator with index {enumeratorIndex} should have produced a value");
                    Assert.True(await actualEnumerators[enumeratorIndex].MoveNextAsync(), $"test failed: IAsyncEnumerator with index {enumeratorIndex} should have produced a value");

                    var expectedValue = expectedEnumerators[enumeratorIndex].Current;
                    var actualValue = actualEnumerators[enumeratorIndex].Current;

                    Assert.Equal(expectedValue, actualValue);
                }
            }

            for (int enumeratorIndex = 0; enumeratorIndex < 3; enumeratorIndex++)
            {
                Assert.False(expectedEnumerators[enumeratorIndex].MoveNext(), $"Test integrity error: Expected value enumerator with index {enumeratorIndex} should be at end state");
                Assert.False(await actualEnumerators[enumeratorIndex].MoveNextAsync(), $"Test failed: IAsyncEnumerator with index {enumeratorIndex} should be at end state");

                expectedEnumerators[enumeratorIndex].Dispose();
                await actualEnumerators[enumeratorIndex].DisposeAsync();
            }
        }

        [Fact]
        public static async Task ReadIAsyncEnumerableOfValueType()
        {
            // Arrange
            string data = "[12,34,56]";

            var expected = new Queue<int>(JsonSerializer.Deserialize<IEnumerable<int>>(data));

            // Act
            var actual = JsonSerializer.Deserialize<IAsyncEnumerable<int>>(data);

            // Assert
            await foreach (var actualItem in actual)
                Assert.Equal(expected.Dequeue(), actualItem);
        }

        [Fact]
        public static async Task ReadNestedIAsyncEnumerableOfValueTypeDepthFirst()
        {
            // Arrange
            string data = "[[12,34,56],[23,45,67],[345,456,567]]";

            var expectedSequences = new Queue<Queue<int>>(
                JsonSerializer.Deserialize<IEnumerable<IEnumerable<int>>>(data)
                    .Select(x => new Queue<int>(x)));

            // Act
            var actual = JsonSerializer.Deserialize<IAsyncEnumerable<IAsyncEnumerable<int>>>(data);

            // Assert
            await foreach (var actualNested in actual)
            {
                var expectedNested = expectedSequences.Dequeue();

                await foreach (var actualItem in actualNested)
                    Assert.Equal(expectedNested.Dequeue(), actualItem);
            }
        }

        [Fact]
        public static async Task ReadNestedIAsyncEnumerableOfValueTypeInterleaved()
        {
            // Arrange
            string data = "[[12,34,56],[23,45,67],[345,456,567]]";

            var expectedSequences = JsonSerializer.Deserialize<IEnumerable<IEnumerable<int>>>(data)
                    .Select(x => new Queue<int>(x));

            // Act
            var actual = JsonSerializer.Deserialize<IAsyncEnumerable<IAsyncEnumerable<int>>>(data);

            // Assert
            var expectedEnumerators = expectedSequences.Select(seq => seq.GetEnumerator()).ToArray();
            var actualEnumerators = new List<IAsyncEnumerator<int>>();

            await foreach (var actualNested in actual)
                actualEnumerators.Add(actualNested.GetAsyncEnumerator());

            for (int elementIndex = 0; elementIndex < 3; elementIndex++)
            {
                for (int enumeratorIndex = 0; enumeratorIndex < 3; enumeratorIndex++)
                {
                    Assert.True(expectedEnumerators[enumeratorIndex].MoveNext(), $"Test integrity error: Expected value enumerator with index {enumeratorIndex} should have produced a value");
                    Assert.True(await actualEnumerators[enumeratorIndex].MoveNextAsync(), $"test failed: IAsyncEnumerator with index {enumeratorIndex} should have produced a value");

                    var expectedValue = expectedEnumerators[enumeratorIndex].Current;
                    var actualValue = actualEnumerators[enumeratorIndex].Current;

                    Assert.Equal(expectedValue, actualValue);
                }
            }

            for (int enumeratorIndex = 0; enumeratorIndex < 3; enumeratorIndex++)
            {
                Assert.False(expectedEnumerators[enumeratorIndex].MoveNext(), $"Test integrity error: Expected value enumerator with index {enumeratorIndex} should be at end state");
                Assert.False(await actualEnumerators[enumeratorIndex].MoveNextAsync(), $"Test failed: IAsyncEnumerator with index {enumeratorIndex} should be at end state");

                expectedEnumerators[enumeratorIndex].Dispose();
                await actualEnumerators[enumeratorIndex].DisposeAsync();
            }
        }

        [Fact]
        public static void ReadIAsyncEnumerableIntoCustomType()
        {
            // Arrange
            var data = "[\"a\",\"b\"]";

            var expected = JsonSerializer.Deserialize<string[]>(data);

            // Act
            var actual = JsonSerializer.Deserialize<CustomAsyncEnumerableType>(data);

            // Assert
            Assert.Equal(expected, actual);
        }

        class CustomAsyncEnumerableType : List<string>, IAsyncEnumerable<string>
        {
            public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }

        [Fact]
        public static void WriteIAsyncEnumerableOfReferenceTypeWhenNull()
        {
            // Arrange
            var expected = JsonSerializer.Serialize<string[]>(null);

            // Act
            var actual = JsonSerializer.Serialize<TestingAsyncEnumerable<string>>(null);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void WriteIAsyncEnumerableOfReferenceType()
        {
            // Arrange
            string[] data =
                new[]
                {
                    "The best proof that there's",
                    "intelligent life in the universe is",
                    "the fact that it hasn't come here.",
                };

            var input = new TestingAsyncEnumerable<string>(data);

            var expected = JsonSerializer.Serialize(data);

            // Act
            var actual = JsonSerializer.Serialize(input);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void WriteNestedIAsyncEnumerableOfReferenceType()
        {
            // Arrange
            IAsyncEnumerable<string> CreateSequence()
            {
                var items = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid().ToString()).ToArray();

                return new TestingAsyncEnumerable<string>(items);
            }

            IAsyncEnumerable<IAsyncEnumerable<string>> CreateSequenceOfSequences()
            {
                var items = Enumerable.Range(1, 3).Select(_ => CreateSequence()).ToArray();

                return new TestingAsyncEnumerable<IAsyncEnumerable<string>>(items);
            }

            var input = CreateSequenceOfSequences();

            var data = Collapse(input);

            var expected = JsonSerializer.Serialize(data);

            // Act
            var actual = JsonSerializer.Serialize(input);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void WriteIAsyncEnumerableOfValueTypeWhenNull()
        {
            // Arrange
            var expected = JsonSerializer.Serialize<int[]>(null);

            // Act
            var actual = JsonSerializer.Serialize<TestingAsyncEnumerable<int>>(null);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void WriteIAsyncEnumerableOfValueType()
        {
            // Arrange
            long[] data =
                new[]
                {
                    1, 1, 2, 5, 14, 42, 132, 429, 1430, 4862, 16796, 58786, 208012, 742900, 2674440,
                    9694845, 35357670, 129644790, 477638700, 1767263190, 6564120420, 24466267020,
                    91482563640, 343059613650, 1289904147324, 4861946401452, 18367353072152,
                    69533550916004, 263747951750360, 1002242216651368, 3814986502092304
                };

            var input = new TestingAsyncEnumerable<long>(data);

            var expected = JsonSerializer.Serialize(data);

            // Act
            var actual = JsonSerializer.Serialize(input);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void WriteNestedIAsyncEnumerableOfValueType()
        {
            // Arrange
            var rnd = new Random();

            IAsyncEnumerable<int> CreateSequence()
            {
                var items = Enumerable.Range(1, 5).Select(_ => rnd.Next()).ToArray();

                return new TestingAsyncEnumerable<int>(items);
            }

            IAsyncEnumerable<IAsyncEnumerable<int>> CreateSequenceOfSequences()
            {
                var items = Enumerable.Range(1, 3).Select(_ => CreateSequence()).ToArray();

                return new TestingAsyncEnumerable<IAsyncEnumerable<int>>(items);
            }

            var input = CreateSequenceOfSequences();

            var data = Collapse(input);

            var expected = JsonSerializer.Serialize(data);

            // Act
            var actual = JsonSerializer.Serialize(input);

            // Assert
            Assert.Equal(expected, actual);
        }

        static System.Collections.IEnumerable Collapse<T>(IAsyncEnumerable<T> t)
        {
            // We know that TestingAsyncEnumerable has, in practice, synchronous implementations of
            // all the interface members. This method would not work on a generalized IAsyncEnumerable<T>
            // because it makes no attempt to await async results.

            var enumerator = t.GetAsyncEnumerator();

            Func<object, object> Unwrap = x => x;

            if (typeof(T).IsGenericType && (typeof(T).GetGenericTypeDefinition() == typeof(TestingAsyncEnumerable<>)))
            {
                var methodInfo = typeof(IAsyncEnumerableTests).GetMethod(nameof(Collapse), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(typeof(T).GetGenericArguments());

                Unwrap = x => methodInfo.Invoke(null, new object[] { x });
            }

            try
            {
                while (enumerator.MoveNextAsync().Result)
                    yield return Unwrap(enumerator.Current);
            }
            finally
            {
                enumerator.DisposeAsync();
            }
        }

        private class TestingAsyncEnumerable<TElement> : IAsyncEnumerable<TElement>
        {
            TElement[] _data;

            public TestingAsyncEnumerable(TElement[] data)
            {
                _data = data;
            }

            public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new TestingAsyncEnumerator<TElement>(_data);
            }
        }

        private class TestingAsyncEnumerator<TElement> : IAsyncEnumerator<TElement>
        {
            TElement[] _data;
            int _index = -1;

            public TestingAsyncEnumerator(TElement[] data)
            {
                _data = data;
            }

            public TElement Current => _data[_index];

            public ValueTask DisposeAsync() => default;

            public ValueTask<bool> MoveNextAsync()
            {
                _index++;
                return new ValueTask<bool>(_index < _data.Length);
            }
        }
    }
}
