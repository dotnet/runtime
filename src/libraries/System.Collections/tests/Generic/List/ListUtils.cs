// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using List_ListUtils;

namespace List_ListUtils
{
    public class SimpleRef<T>
    {
        private T _val;
        public SimpleRef(T t)
        {
            _val = t;
        }
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
    }
    public struct SimpleVal<T>
    {
        private T _val;
        public SimpleVal(T t)
        {
            _val = t;
        }
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
    }
    public interface IComparableValue
    {
        System.IComparable Val { get; set; }
        int CompareTo(IComparableValue obj);
    }
    public class ValueComparer<T> : System.Collections.Generic.IComparer<T> where T : IComparableValue
    {
        public int Compare(T x, T y)
        {
            if (null == (object)x)
                if (null == (object)y)
                    return 0;
                else
                    return -1;
            if (null == (object)y)
                return 1;
            if (null == (object)x.Val)
                if (null == (object)y.Val)
                    return 0;
                else
                    return -1;
            return x.Val.CompareTo(y.Val);
        }

        public bool Equals(T x, T y)
        {
            return 0 == Compare(x, y);
        }

        public int GetHashCode(T x)
        {
            return x.GetHashCode();
        }
    }
    public class IValueComparer<T> : System.Collections.Generic.IComparer<T> where T : IComparableValue
    {
        public int Compare(T x, T y)
        {
            if (null == (object)x)
                if (null == (object)y)
                    return 0;
                else
                    return -1;
            if (null == (object)x.Val)
                if (null == (object)y.Val)
                    return 0;
                else
                    return -1;
            return x.Val.CompareTo(y.Val);
        }

        public bool Equals(T x, T y)
        {
            return 0 == Compare(x, y);
        }

        public int GetHashCode(T x)
        {
            return x.GetHashCode();
        }
    }

    public class RefX1<T> : System.IComparable<RefX1<T>> where T : System.IComparable
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
    public struct ValX1<T> : System.IComparable<ValX1<T>> where T : System.IComparable
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

    public class RefX1_IC<T> : IComparableValue where T : System.IComparable
    {
        private T _val;
        public System.IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }
        public RefX1_IC(T t) { _val = t; }
        public int CompareTo(IComparableValue obj)
        {
            if (null == (object)obj)
                return 1;
            if (null == (object)_val)
                if (null == (object)obj.Val)
                    return 0;
                else
                    return -1;
            return _val.CompareTo(obj.Val);
        }
    }

    public struct ValX1_IC<T> : IComparableValue where T : System.IComparable
    {
        private T _val;
        public System.IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }
        public ValX1_IC(T t) { _val = t; }
        public int CompareTo(IComparableValue obj)
        {
            return _val.CompareTo(obj.Val);
        }
    }
    public class TestCollection<T> : ICollection<T>
    {
        //
        //Expose the array to give more test flexibility...
        //

        public T[] _items;

        public TestCollection() { }

        public TestCollection(T[] items)
        {
            _items = items;
        }
        //
        //ICollection<T> 
        //

        public void CopyTo(T[] array, int index)
        {
            Array.Copy(_items, 0, array, index, _items.Length);
        }

        public int Count
        {
            get
            {
                if (_items == null)
                    return 0;
                else
                    return _items.Length;
            }
        }

        public Object SyncRoot
        { get { return this; } }

        public bool IsSynchronized
        { get { return false; } }


        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotSupportedException();
            }
        }


        //
        //IEnumerable<T>
        //

        public IEnumerator<T> GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        //
        //TestCollectionEnumerator<T>
        //

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
                return (++_index < _col._items.Length);
            }

            public T1 Current
            {
                get { return _col._items[_index]; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _col._items[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
    public class Test
    {
        public static int counter = 0;
        public static bool result = true;

        public static bool Eval(bool exp)
        {
            counter++;
            return Eval(exp, null);
        }
        public static bool Eval(bool exp, String errorMsg)
        {
            if (!exp)
            {
                //This would never be reset, since we start with true and only set it to false if the Eval fails
                result = exp;
                String err = errorMsg;
                if (err == null)
                    err = "Test Failed at location: " + counter;
                Console.WriteLine(err);
            }

            return exp;
        }

        public static bool Eval(bool exp, String format, params object[] arg)
        {
            if (!exp)
            {
                return Eval(exp, String.Format(format, arg));
            }

            return true;
        }

        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }


        public static bool Eval<T>(T expected, T actual, String format, params object[] arg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, String.Format(format, arg) +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }
    }
}