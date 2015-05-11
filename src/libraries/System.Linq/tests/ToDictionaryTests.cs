using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Tests.Helpers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ToDictionaryTests
    {
        private class CustomComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) { return EqualityComparer<T>.Default.Equals(x, y); }
            public int GetHashCode(T obj) { return EqualityComparer<T>.Default.GetHashCode(obj); }
        }

        // =====================


        [Fact]
        public void ToDictionary_AlwaysCreateACopy()
        {
            Dictionary<int, int> source = new Dictionary<int, int>() { { 1, 1 }, { 2, 2 }, { 3, 3 } };
            Dictionary<int, int> result = source.ToDictionary(key => key.Key, val => val.Value);

            Assert.NotSame(source, result);
            Assert.Equal(source, result);
        }


        private void RunToDictionaryOnAllCollectionTypes<T>(T[] items, Action<Dictionary<T, T>> validation)
        {
            validation(Enumerable.ToDictionary(items, key => key));
            validation(Enumerable.ToDictionary(items, key => key, value => value));
            validation(Enumerable.ToDictionary(new List<T>(items), key => key));
            validation(Enumerable.ToDictionary(new List<T>(items), key => key, value => value));
            validation(new TestEnumerable<T>(items).ToDictionary(key => key));
            validation(new TestEnumerable<T>(items).ToDictionary(key => key, value => value));
            validation(new TestReadOnlyCollection<T>(items).ToDictionary(key => key));
            validation(new TestReadOnlyCollection<T>(items).ToDictionary(key => key, value => value));
            validation(new TestCollection<T>(items).ToDictionary(key => key));
            validation(new TestCollection<T>(items).ToDictionary(key => key, value => value));
        }


        [Fact]
        public void ToDictionary_WorkWithEmptyCollection()
        {
            RunToDictionaryOnAllCollectionTypes(new int[0],
                resultDictionary =>
                {
                    Assert.NotNull(resultDictionary);
                    Assert.Equal(0, resultDictionary.Count);
                });
        }


        [Fact]
        public void ToDictionary_ProduceCorrectDictionary()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            RunToDictionaryOnAllCollectionTypes(sourceArray,
                resultDictionary =>
                {
                    Assert.Equal(sourceArray.Length, resultDictionary.Count);
                    Assert.Equal(sourceArray, resultDictionary.Keys);
                    Assert.Equal(sourceArray, resultDictionary.Values);
                });

            string[] sourceStringArray = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            RunToDictionaryOnAllCollectionTypes(sourceStringArray,
                resultDictionary =>
                {
                    Assert.Equal(sourceStringArray.Length, resultDictionary.Count);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultDictionary[sourceStringArray[i]]);
                });
        }



        [Fact]
        public void ToDictionary_PassCustomComparer()
        {
            CustomComparer<int> comparer = new CustomComparer<int>();
            TestCollection<int> collection = new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result1 = collection.ToDictionary(key => key, comparer);
            Assert.Same(comparer, result1.Comparer);

            Dictionary<int, int> result2 = collection.ToDictionary(key => key, val => val, comparer);
            Assert.Same(comparer, result2.Comparer);
        }

        [Fact]
        public void ToDictionary_UseDefaultComparerOnNull()
        {
            CustomComparer<int> comparer = null;
            TestCollection<int> collection = new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result1 = collection.ToDictionary(key => key, comparer);
            Assert.Same(EqualityComparer<int>.Default, result1.Comparer);

            Dictionary<int, int> result2 = collection.ToDictionary(key => key, val => val, comparer);
            Assert.Same(EqualityComparer<int>.Default, result2.Comparer);
        }

        [Fact]
        public void ToDictionary_KeyValueSelectorsWork()
        {
            TestCollection<int> collection = new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result = collection.ToDictionary(key => key + 10, val => val + 100);

            Assert.Equal(collection.Items.Select(o => o + 10), result.Keys);
            Assert.Equal(collection.Items.Select(o => o + 100), result.Values);
        }


        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>(() => source.ToDictionary(key => key));
        }


        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenKeySelectorIsNull()
        {
            int[] source = new int[0];
            Func<int, int> keySelector = null;
            Assert.Throws<ArgumentNullException>(() => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ThrowArgumentNullExceptionWhenValueSelectorIsNull()
        {
            int[] source = new int[0];
            Func<int, int> keySelector = key => key;
            Func<int, int> valueSelector = null;
            Assert.Throws<ArgumentNullException>(() => source.ToDictionary(keySelector, valueSelector));
        }


        [Fact]
        public void ToDictionary_KeySelectorThrowException()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, int> keySelector = key =>
            {
                if (key == 1)
                    throw new InvalidOperationException();
                return key;
            };


            Assert.Throws<InvalidOperationException>(() => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ThrowWhenKeySelectorReturnNull()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, string> keySelector = key => null;

            Assert.Throws<ArgumentNullException>(() => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ThrowWhenKeySelectorReturnSameValueTwice()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, int> keySelector = key => 1;

            Assert.Throws<ArgumentException>(() => source.ToDictionary(keySelector));
        }

        [Fact]
        public void ToDictionary_ValueSelectorThrowException()
        {
            int[] source = new int[] { 1, 2, 3 };
            Func<int, int> keySelector = key => key;
            Func<int, int> valueSelector = value =>
            {
                if (value == 1)
                    throw new InvalidOperationException();
                return value;
            };

            Assert.Throws<InvalidOperationException>(() => source.ToDictionary(keySelector, valueSelector));
        }
    }
}
