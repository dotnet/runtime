// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace List_List_InterfaceTests
{
    public class Driver<T>
    {
        #region List<T>.GetEnumerator / IEnumerator<T>

        public void List_GetEnumeratorBasic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            List<T>.Enumerator enumerator = list.GetEnumerator();
            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(enumerator.MoveNext()); //"25- Enumerator unexpectedly did not move"
                T t = enumerator.Current;
                Assert.Equal(t, items[i]); //"22- Expected current to equal " + items[i] + ". Actually equalled " + t
            }
            Assert.False(enumerator.MoveNext()); //"Should not be able to move past list."

            enumerator.Dispose();
        }

        public void List_GetEnumeratorValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            List<T>.Enumerator enumerator = list.GetEnumerator();
            T currentValue;

            currentValue = enumerator.Current;

            Assert.Equal(currentValue, default(T)); //"Err_282haie Expected Current to return defalut(T) when enumerator is positioned before the first item in the collection instead got:" + currentValue

            try
            {
                currentValue = (T)((IEnumerator)enumerator).Current;
                Assert.True(false); //"Err_231htyw Expected invalid operation exception not thrown"
            }
            catch (InvalidOperationException) { }

            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(enumerator.MoveNext()); //"26- Enumerator unexpectedly did not move"
                currentValue = enumerator.Current;

                Assert.Equal(currentValue, items[i]); //"23- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
                currentValue = (T)((IEnumerator)enumerator).Current;

                Assert.Equal(currentValue, items[i]); //"24- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
            }
            Assert.False(enumerator.MoveNext()); //"Should not be able to move past list."

            currentValue = enumerator.Current;

            Assert.Equal(default(T), currentValue); //"Err_051984ajied Expected Current to return defalut(T) when enumerator is positioned after the first item in the collection instead got:" + currentValue

            try
            {
                currentValue = (T)((IEnumerator)enumerator).Current;
                Assert.True(false); //"Err_751hryw Expected invalid operation exception not thrown"
            }
            catch (InvalidOperationException) { }

            IEnumerator<T> enumerator2 = list.GetEnumerator();
            ((IEnumerator)enumerator2).Reset(); ((IEnumerator)enumerator2).Reset(); ((IEnumerator)enumerator2).Reset();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(enumerator2.MoveNext()); //"13- Enumerator unexpectedly did not move"
                currentValue = enumerator2.Current;
                Assert.Equal(currentValue, items[i]); //"1- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
                currentValue = (T)((IEnumerator)enumerator2).Current;
                Assert.Equal(currentValue, items[i]); //"2- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
            }
            Assert.False(enumerator2.MoveNext()); //"Should not move past list."

            ((IEnumerator)enumerator2).Reset();

            for (int i = 0; i < items.Length / 2; i++)
            {
                Assert.True(enumerator2.MoveNext()); //"12- Enumerator unexpectedly did not move"
                currentValue = enumerator2.Current;
                Assert.Equal(currentValue, items[i]); //"4- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
                currentValue = (T)((IEnumerator)enumerator2).Current;
                Assert.Equal(currentValue, items[i]); //"6- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
            }

            ((IEnumerator)enumerator2).Reset(); ((IEnumerator)enumerator2).Reset();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(enumerator2.MoveNext()); //"7- Enumerator unexpectedly did not move"
                currentValue = enumerator2.Current;
                Assert.Equal(currentValue, items[i]); //"9- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
                currentValue = (T)((IEnumerator)enumerator2).Current;
                Assert.Equal(currentValue, items[i]); //"11- Expected current to equal " + items[i] + ". Actually equalled " + currentValue
            }
            Assert.False(enumerator2.MoveNext()); //"11A- Enumerator moved unexpectedly"

            ((IEnumerator)enumerator2).Reset();
            Assert.True((enumerator2.MoveNext() || (list.Count == 0))); //"14- Enumerator unexpectedly did not move"

            T oldValue = enumerator2.Current;

            //We have to use add here since we Remove will change Count and the enumerator will then think it
            //is positioned after the last item in the collection. See VS Whidbey: 400663 for more information.
            //list.Remove(currentValue);
            list.Add(currentValue);


            currentValue = enumerator2.Current;
            Assert.Equal(currentValue, oldValue); //"16- Expected current to equal " + oldValue + ". Actually equalled " + currentValue

            currentValue = (T)((IEnumerator)enumerator2).Current;

            Assert.Equal(currentValue, oldValue); //"18- Expected current to equal " + oldValue + ". Actually equalled " + currentValue

            try
            {
                ((IEnumerator)enumerator2).Reset();
                Assert.True(false); //"Err_901hrya Expected invalid operation exception not thrown"
            }
            catch (InvalidOperationException) { }

            enumerator.Dispose();
            enumerator2.Dispose();
        }

        #endregion

        #region Non-Generic IEnumerator

        public void GetEnumeratorBasic(T[] items)
        {
            List<T> myList = BuildList(items);
            IEnumerable listIEnumerable = myList;
            IEnumerator Enum = listIEnumerable.GetEnumerator();
            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(Enum.MoveNext()); //"Should be able to move to next"
                Object t = Enum.Current;
                Assert.Equal(t, items[i]); //"Err_005498ahuea Expected Items. Index: " + i
            }
            Assert.False(Enum.MoveNext()); //"cannot move past end of list."

            Enum.Reset();
            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(Enum.MoveNext()); //"Should be able to move to next"
                Object t = Enum.Current;
                Assert.Equal(t, items[i]); //"Err_51877ahided Expected Items Index: " + i
            }
            Assert.False(Enum.MoveNext()); //"cannot move past end of list."
            myList.Add(items[0]);
            try
            {
                Enum.MoveNext();
                Assert.True(false, "Err_848489aheid Expected InvalidOperationException from MoveNext() on modified collection and nothing was thrown");
            }
            catch (InvalidOperationException) { }
            try
            {
                Enum.Reset();
                Assert.True(false, "Err_587aujeid Expected InvalidOperationException from Reset() on modified collection and nothing was thrown");
            }
            catch (InvalidOperationException) { }
        }

        public void GetEnumeratorValidations(T[] items)
        {
            List<T> myList = BuildList(items);
            IEnumerable listIEnumerable = myList;
            IEnumerator Enum = listIEnumerable.GetEnumerator();
            try
            {
                //This behavior is undefined. As long as we dont format the machine, we dont care what happens here
                Object t = Enum.Current;
            }
            catch (Exception)
            {
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.True(Enum.MoveNext()); //"Should be able to move to next"
                Object t = Enum.Current;
                Assert.Equal(t, items[i]); //"Err_005498ahuea Expected Items. Index: " + i
            }
            Assert.False(Enum.MoveNext()); //"Err_158885ajeizpz Expected MoveNext to return false after iterating through the colleciton"
            try
            {
                //This behavior is undefined. As long as we dont format the machine, we dont care what happens here
                Object t = Enum.Current;
            }
            catch (Exception)
            {
            }
        }

        public static List<T> BuildList(T[] builtFrom)
        {
            List<T> builtList = new List<T>(builtFrom.Length);

            for (int i = 0; i < builtFrom.Length; i++)
            {
                builtList.Add(builtFrom[i]);
            }

            return builtList;
        }

        #endregion

        #region ICollection Tests

        public void TestICollection(T[] items)
        {
            List<T> _list = BuildList(items);
            ICollection collection = _list;
            Assert.NotNull(collection.SyncRoot); //"Should not be null."
            Assert.False(collection.IsSynchronized); //"Shouldn't be synchronised"
            Assert.Equal(collection.SyncRoot.GetType(), typeof(Object)); //"Err_47235fsd! Expected SyncRoot to be an object"
        }

        #endregion
    }

    public class List_InterfaceTests
    {
        [Fact]
        public static void GenericGetEnumeratorTests()
        {
            Driver<RefX1<int>> RefIntDriver = new Driver<RefX1<int>>();
            Driver<ValX1<int>> ValIntDriver = new Driver<ValX1<int>>();
            Driver<int> IntDriver = new Driver<int>();

            Driver<RefX1<String>> RefStringDriver = new Driver<RefX1<String>>();
            Driver<ValX1<String>> ValStringDriver = new Driver<ValX1<String>>();
            Driver<String> StringDriver = new Driver<String>();

            RefX1<int>[] refIntArr = new RefX1<int>[100];
            ValX1<int>[] valIntArr = new ValX1<int>[100];
            int[] intArr = new int[100];

            RefX1<String>[] refStringArr = new RefX1<String>[100];
            ValX1<String>[] valStringArr = new ValX1<String>[100];
            String[] stringArr = new String[100];

            for (int i = 0; i < 100; i++)
            {
                refIntArr[i] = new RefX1<int>(i);
                valIntArr[i] = new ValX1<int>(i);
                intArr[i] = i;

                refStringArr[i] = new RefX1<String>(i.ToString());
                valStringArr[i] = new ValX1<String>(i.ToString());
                stringArr[i] = i.ToString();
            }


            /*****************************************************
            Int
            *****************************************************/
            //GetEnumeratorBasic
            RefIntDriver.List_GetEnumeratorBasic(refIntArr);
            ValIntDriver.List_GetEnumeratorBasic(valIntArr);
            IntDriver.List_GetEnumeratorBasic(intArr);

            RefIntDriver.List_GetEnumeratorBasic(new RefX1<int>[0]);
            ValIntDriver.List_GetEnumeratorBasic(new ValX1<int>[0]);
            IntDriver.List_GetEnumeratorBasic(new int[0]);

            RefIntDriver.List_GetEnumeratorBasic(new RefX1<int>[] { null });

            //GetEnumeratorValidations
            RefIntDriver.List_GetEnumeratorValidations(refIntArr);
            ValIntDriver.List_GetEnumeratorValidations(valIntArr);
            IntDriver.List_GetEnumeratorValidations(intArr);

            RefIntDriver.List_GetEnumeratorValidations(new RefX1<int>[0]);
            ValIntDriver.List_GetEnumeratorValidations(new ValX1<int>[0]);
            IntDriver.List_GetEnumeratorValidations(new int[0]);

            RefIntDriver.List_GetEnumeratorValidations(new RefX1<int>[] { null });


            /*****************************************************
            String
            *****************************************************/
            //GetEnumeratorBasic
            RefStringDriver.List_GetEnumeratorBasic(refStringArr);
            ValStringDriver.List_GetEnumeratorBasic(valStringArr);
            StringDriver.List_GetEnumeratorBasic(stringArr);

            RefStringDriver.List_GetEnumeratorBasic(new RefX1<String>[0]);
            ValStringDriver.List_GetEnumeratorBasic(new ValX1<String>[0]);
            StringDriver.List_GetEnumeratorBasic(new String[0]);

            RefStringDriver.List_GetEnumeratorBasic(new RefX1<String>[] { null });

            //GetEnumeratorValidations
            RefStringDriver.List_GetEnumeratorValidations(refStringArr);
            ValStringDriver.List_GetEnumeratorValidations(valStringArr);
            StringDriver.List_GetEnumeratorValidations(stringArr);

            RefStringDriver.List_GetEnumeratorValidations(new RefX1<String>[0]);
            ValStringDriver.List_GetEnumeratorValidations(new ValX1<String>[0]);
            StringDriver.List_GetEnumeratorValidations(new String[0]);

            RefStringDriver.List_GetEnumeratorValidations(new RefX1<String>[] { null });
        }

        [Fact]
        public static void NonGenericGetEnumeratorTests()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1<int>[] intArr = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr[i] = new RefX1<int>(i);
            }

            IntDriver.GetEnumeratorBasic(intArr);
            IntDriver.GetEnumeratorValidations(intArr);

            Driver<ValX1<string>> StringDriver = new Driver<ValX1<string>>();
            ValX1<string>[] stringArr = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }

            StringDriver.GetEnumeratorBasic(stringArr);
            StringDriver.GetEnumeratorValidations(stringArr);
        }

        [Fact]
        public static void ICollectionTests()
        {
            Driver<RefX1<int>> IntDriver = new Driver<RefX1<int>>();
            RefX1<int>[] intArr = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr[i] = new RefX1<int>(i);
            }

            IntDriver.TestICollection(intArr);

            Driver<ValX1<string>> StringDriver = new Driver<ValX1<string>>();
            ValX1<string>[] stringArr = new ValX1<string>[100];
            for (int i = 0; i < 100; i++)
            {
                stringArr[i] = new ValX1<string>("SomeTestString" + i.ToString());
            }

            StringDriver.TestICollection(stringArr);
        }
    }

    #region Helper Classes

    /// <summary>
    /// Helper class that implements ICollection.
    /// </summary>
    public class TestCollection<T> : ICollection<T>
    {
        /// <summary>
        /// Expose the Items in Array to give more test flexibility...
        /// </summary>
        public readonly T[] m_items;

        public TestCollection(T[] items)
        {
            m_items = items;
        }

        public void CopyTo(T[] array, int index)
        {
            Array.Copy(m_items, 0, array, index, m_items.Length);
        }

        public int Count
        {
            get
            {
                if (m_items == null)
                    return 0;
                else
                    return m_items.Length;
            }
        }

        public Object SyncRoot { get { return this; } }

        public bool IsSynchronized { get { return false; } }

        public IEnumerator<T> GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        private class TestCollectionEnumerator<T1> : IEnumerator<T1>
        {
            private TestCollection<T1> _col;
            private int _index;

            public void Dispose() { }

            public TestCollectionEnumerator(TestCollection<T1> col)
            {
                _col = col;
                _index = -1;
            }

            public bool MoveNext()
            {
                return (++_index < _col.m_items.Length);
            }

            public T1 Current
            {
                get { return _col.m_items[_index]; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _col.m_items[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        #region Non Implemented methods

        public void Add(T item) { throw new NotSupportedException(); }

        public void Clear() { throw new NotSupportedException(); }
        public bool Contains(T item) { throw new NotSupportedException(); }

        public bool Remove(T item) { throw new NotSupportedException(); }

        public bool IsReadOnly { get { throw new NotSupportedException(); } }

        #endregion
    }

    public class RefX1<T> : IComparable<RefX1<T>> where T : IComparable
    {
        private T _val;
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
        public RefX1(T t) { _val = t; }
        public int CompareTo(RefX1<T> obj)
        {
            if (null == obj)
                return 1;
            if (null == _val)
                if (null == obj.Val)
                    return 0;
                else
                    return -1;
            return _val.CompareTo(obj.Val);
        }
        public override bool Equals(object obj)
        {
            if (obj is RefX1<T>)
            {
                RefX1<T> v = (RefX1<T>)obj;
                return (CompareTo(v) == 0);
            }
            return false;
        }
        public override int GetHashCode() { return base.GetHashCode(); }

        public bool Equals(RefX1<T> x)
        {
            return 0 == CompareTo(x);
        }
    }

    public struct ValX1<T> : IComparable<ValX1<T>> where T : IComparable
    {
        private T _val;
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
        public ValX1(T t) { _val = t; }
        public int CompareTo(ValX1<T> obj)
        {
            if (Object.ReferenceEquals(_val, obj._val)) return 0;

            if (null == _val)
                return -1;

            return _val.CompareTo(obj.Val);
        }
        public override bool Equals(object obj)
        {
            if (obj is ValX1<T>)
            {
                ValX1<T> v = (ValX1<T>)obj;
                return (CompareTo(v) == 0);
            }
            return false;
        }
        public override int GetHashCode() { return ((object)this).GetHashCode(); }

        public bool Equals(ValX1<T> x)
        {
            return 0 == CompareTo(x);
        }
    }
    #endregion
}
