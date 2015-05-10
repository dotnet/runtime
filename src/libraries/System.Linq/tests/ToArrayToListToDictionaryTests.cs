using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace System.Linq.Tests
{
    public class ToArrayToListToDictionaryTests
    {
        #region =============  Helper classes =============

        private class EnumerableCollectionTest<T> : IEnumerable<T>
        {
            public T[] Items = new T[0];
            public EnumerableCollectionTest(T[] items) { Items = items; }

            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)Items).GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return Items.GetEnumerator(); }
        }
        private class ReadOnlyCollectionTest<T> : IReadOnlyCollection<T>
        {
            public T[] Items = new T[0];
            public int CountTouched = 0;
            public ReadOnlyCollectionTest(T[] items) { Items = items; }

            public int Count { get { CountTouched++; return Items.Length; } }
            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)Items).GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return Items.GetEnumerator(); }
        }
        private class CollectionTest<T> : ICollection<T>
        {
            public T[] Items = new T[0];
            public int CountTouched = 0;
            public int CopyToTouched = 0;
            public CollectionTest(T[] items) { Items = items; }

            public virtual int Count { get { CountTouched++; return Items.Length; } }
            public bool IsReadOnly { get { return false; } }
            public void Add(T item) { throw new NotImplementedException(); }
            public void Clear() { throw new NotImplementedException(); }
            public bool Contains(T item) { return Items.Contains(item); }
            public bool Remove(T item) { throw new NotImplementedException(); }
            public void CopyTo(T[] array, int arrayIndex) { CopyToTouched++; Items.CopyTo(array, arrayIndex); }
            public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)Items).GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return Items.GetEnumerator(); }
        }

        private class LargeSequenceTest: IEnumerable<byte>
        {
            public long MaxSize = 2 * (long)int.MaxValue;
            public IEnumerator<byte> GetEnumerator()
            {
                for (long i = 0; i < MaxSize; i++) yield return (byte)1;
            }
            IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
        }

        /// <summary>
        /// Emulation of async collection change
        /// </summary>
        private class AsyncCollectionTest : CollectionTest<int>
        {
            public AsyncCollectionTest(int[] items) : base(items) { }

            public override int Count
            {
                get
                {
                    var result = base.Count;
                    Array.Resize(ref Items, Items.Length + 1);
                    return result;
                }
            }
        }

        private class CustomComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) { return EqualityComparer<T>.Default.Equals(x, y); }
            public int GetHashCode(T obj) { return EqualityComparer<T>.Default.GetHashCode(obj); }
        }

        #endregion


        #region ============= ToArray =============

        [Fact]
        public void ToArray_AlwaysCreateACopy()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5 };
            int[] resultArray = sourceArray.ToArray();

            Assert.NotSame(sourceArray, resultArray);
            Assert.Equal(sourceArray, resultArray);
        }


        private void RunToArrayOnAllCollectionTypes<T>(T[] items, Action<T[]> validation)
        {
            validation(Enumerable.ToArray(items));
            validation(Enumerable.ToArray(new List<T>(items)));
            validation(new EnumerableCollectionTest<T>(items).ToArray());
            validation(new ReadOnlyCollectionTest<T>(items).ToArray());
            validation(new CollectionTest<T>(items).ToArray());
        }


        [Fact]
        public void ToArray_WorkWithEmptyCollection()
        {
            RunToArrayOnAllCollectionTypes(new int[0],
                resultArray =>
                {
                    Assert.NotNull(resultArray);
                    Assert.Equal(0, resultArray.Length);
                });
        }

        [Fact]
        public void ToArray_ProduceCorrectArray()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            RunToArrayOnAllCollectionTypes(sourceArray,
                resultArray =>
                {
                    Assert.Equal(sourceArray.Length, resultArray.Length);
                    Assert.Equal(sourceArray, resultArray);
                });

            string[] sourceStringArray = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            RunToArrayOnAllCollectionTypes(sourceStringArray,
                resultStringArray =>
                {
                    Assert.Equal(sourceStringArray.Length, resultStringArray.Length);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultStringArray[i]);
                });
        }


        [Fact]
        public void ToArray_TouchCountWithICollection()
        {
            CollectionTest<int> source = new CollectionTest<int>(new int[] { 1, 2, 3, 4 });
            var resultArray = source.ToArray();

            Assert.Equal(source, resultArray);
            Assert.Equal(1, source.CountTouched);
        }


        [Fact]
        public void ToArray_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>(() => source.ToArray());
        }


        // Later this behaviour can be changed
        [Fact]
        public void ToArray_UseCopyToWithICollection()
        {
            CollectionTest<int> source = new CollectionTest<int>(new int[] { 1, 2, 3, 4 });
            var resultArray = source.ToArray();

            Assert.Equal(source, resultArray);
            Assert.Equal(1, source.CopyToTouched);
        }


        // Currently not passed
        //[Fact]
        public void ToArray_WorkWhenCountChangedAsynchronously()
        {
            AsyncCollectionTest source = new AsyncCollectionTest(new int[] { 1, 2, 3, 4 });
            var resultArray = source.ToArray();

            Assert.True(resultArray.Length >= 4);
            Assert.Equal(1, resultArray[0]);
            Assert.Equal(2, resultArray[0]);
            Assert.Equal(3, resultArray[0]);
            Assert.Equal(4, resultArray[0]);
        }


        //[Fact]
        public void ToArray_FailOnExtremelyLargeCollection()
        {
            LargeSequenceTest largeSeq = new LargeSequenceTest();
            Assert.ThrowsAny<OverflowException>(() => { largeSeq.ToArray(); });
        }


        #endregion


        #region ============= ToList =============

        [Fact]
        public void ToList_AlwaysCreateACopy()
        {
            List<int> sourceList = new List<int>() { 1, 2, 3, 4, 5 };
            List<int> resultList = sourceList.ToList();

            Assert.NotSame(sourceList, resultList);
            Assert.Equal(sourceList, resultList);
        }


        private void RunToListOnAllCollectionTypes<T>(T[] items, Action<List<T>> validation)
        {
            validation(Enumerable.ToList(items));
            validation(Enumerable.ToList(new List<T>(items)));
            validation(new EnumerableCollectionTest<T>(items).ToList());
            validation(new ReadOnlyCollectionTest<T>(items).ToList());
            validation(new CollectionTest<T>(items).ToList());
        }
        

        [Fact]
        public void ToList_WorkWithEmptyCollection()
        {
            RunToListOnAllCollectionTypes(new int[0],
                resultList =>
                {
                    Assert.NotNull(resultList);
                    Assert.Equal(0, resultList.Count);
                });
        }

        [Fact]
        public void ToList_ProduceCorrectList()
        {
            int[] sourceArray = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            RunToListOnAllCollectionTypes(sourceArray,
                resultList =>
                {
                    Assert.Equal(sourceArray.Length, resultList.Count);
                    Assert.Equal(sourceArray, resultList);
                });

            string[] sourceStringArray = new string[] { "1", "2", "3", "4", "5", "6", "7", "8" };
            RunToListOnAllCollectionTypes(sourceStringArray,
                resultStringList =>
                {
                    Assert.Equal(sourceStringArray.Length, resultStringList.Count);
                    for (int i = 0; i < sourceStringArray.Length; i++)
                        Assert.Same(sourceStringArray[i], resultStringList[i]);
                });
        }


        [Fact]
        public void ToList_TouchCountWithICollection()
        {
            CollectionTest<int> source = new CollectionTest<int>(new int[] { 1, 2, 3, 4 });
            var resultList = source.ToList();

            Assert.Equal(source, resultList);
            Assert.Equal(1, source.CountTouched);
        }


        [Fact]
        public void ToList_ThrowArgumentNullExceptionWhenSourceIsNull()
        {
            int[] source = null;
            Assert.Throws<ArgumentNullException>(() => source.ToList());
        }


        // Later this behaviour can be changed
        [Fact]
        public void ToList_UseCopyToWithICollection()
        {
            CollectionTest<int> source = new CollectionTest<int>(new int[] { 1, 2, 3, 4 });
            var resultList = source.ToList();

            Assert.Equal(source, resultList);
            Assert.Equal(1, source.CopyToTouched);
        }


        // Currently not passed
        //[Fact]
        public void ToList_WorkWhenCountChangedAsynchronously()
        {
            AsyncCollectionTest source = new AsyncCollectionTest(new int[] { 1, 2, 3, 4 });
            var resultList = source.ToList();

            Assert.True(resultList.Count >= 4);
            Assert.Equal(1, resultList[0]);
            Assert.Equal(2, resultList[0]);
            Assert.Equal(3, resultList[0]);
            Assert.Equal(4, resultList[0]);
        }



        #endregion


        #region ============= ToDictionary =============

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
            validation(new EnumerableCollectionTest<T>(items).ToDictionary(key => key));
            validation(new EnumerableCollectionTest<T>(items).ToDictionary(key => key, value => value));
            validation(new ReadOnlyCollectionTest<T>(items).ToDictionary(key => key));
            validation(new ReadOnlyCollectionTest<T>(items).ToDictionary(key => key, value => value));
            validation(new CollectionTest<T>(items).ToDictionary(key => key));
            validation(new CollectionTest<T>(items).ToDictionary(key => key, value => value));
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
            CollectionTest<int> collection = new CollectionTest<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result1 = collection.ToDictionary(key => key, comparer);
            Assert.Same(comparer, result1.Comparer);

            Dictionary<int, int> result2 = collection.ToDictionary(key => key, val => val, comparer);
            Assert.Same(comparer, result2.Comparer);
        }

        [Fact]
        public void ToDictionary_UseDefaultComparerOnNull()
        {
            CustomComparer<int> comparer = null;
            CollectionTest<int> collection = new CollectionTest<int>(new int[] { 1, 2, 3, 4, 5, 6 });

            Dictionary<int, int> result1 = collection.ToDictionary(key => key, comparer);
            Assert.Same(EqualityComparer<int>.Default, result1.Comparer);

            Dictionary<int, int> result2 = collection.ToDictionary(key => key, val => val, comparer);
            Assert.Same(EqualityComparer<int>.Default, result2.Comparer);
        }

        [Fact]
        public void ToDictionary_KeyValueSelectorsWork()
        {
            CollectionTest<int> collection = new CollectionTest<int>(new int[] { 1, 2, 3, 4, 5, 6 });

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


        #endregion
    }
}
