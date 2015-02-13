// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using SortedList_SortedListUtils;

namespace SortedList_SortedListUtils
{
    public class SimpleRef<T> : IComparable<SimpleRef<T>>, IComparable where T : IComparable
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

        public int CompareTo(SimpleRef<T> obj)
        {
            return _val.CompareTo(obj.Val);
        }

        public int CompareTo(Object obj)
        {
            return _val.CompareTo(((SimpleRef<T>)obj).Val);
        }

        public override bool Equals(Object obj)
        {
            //		throw new InvalidOperationException("Err_10872zgap Object.Equals(Object) should not be called");
            try
            {
                SimpleRef<T> localTestVar = (SimpleRef<T>)obj;
                if (_val.Equals(localTestVar.Val))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public bool Equals(SimpleRef<T> obj)
        {
            return 0 == CompareTo(obj);
        }

        public override int GetHashCode()
        {
            return _val.GetHashCode();
        }


        public override string ToString()
        {
            return _val.ToString();
        }
    }
    public struct SimpleVal<T> : IComparable<SimpleVal<T>> where T : IComparable
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

        public int CompareTo(SimpleVal<T> obj)
        {
            return _val.CompareTo(obj.Val);
        }

        public int CompareTo(Object obj)
        {
            return _val.CompareTo(((SimpleVal<T>)obj).Val);
        }

        public override bool Equals(Object obj)
        {
            //		throw new InvalidOperationException("Err_10872zgap Object.Equals(Object) should not be called");		
            try
            {
                SimpleVal<T> localTestVar = (SimpleVal<T>)obj;
                if (_val.Equals(localTestVar.Val))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return _val.GetHashCode();
        }

        public bool Equals(SimpleVal<T> obj)
        {
            return 0 == CompareTo(obj);
        }
    }
    public class ValueKeyComparer<T> : System.Collections.Generic.IComparer<T> where T : IComparableValue
    {
        //
        //IComparer<T>
        //
        public bool Equals(T x, T y)
        {
            if (null == (object)x)
                return (null == (object)y);
            if (null == (object)x.Val)
                return (null == (object)y.Val);
            return (0 == x.Val.CompareTo(y.Val));
        }

        public int GetHashCode(T obj)
        {
            string str = obj.Val.ToString();
            int hash = 0;
            foreach (char c in str)
            {
                hash += (int)c;
            }
            if (hash < 0) return -hash;
            return hash;
        }

        //
        //IComparer<T>
        //
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
    }
    public interface IComparableValue
    {
        System.IComparable Val { get; set; }
    }
    public interface IPublicValue<T>
    {
        T publicVal { get; set; }
    }
    public class RefX1<T> : IComparableValue, IPublicValue<T>, IComparable<RefX1<T>>, IComparable where T : System.IComparable
    {
        public T _val;
        public System.IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }

        public T publicVal
        {
            get { return _val; }
            set { _val = value; }
        }

        public RefX1(T t) { _val = t; }

        public int CompareTo(RefX1<T> obj)
        {
            return _val.CompareTo(obj._val);
        }

        public int CompareTo(Object obj)
        {
            return _val.CompareTo(((RefX1<T>)obj).Val);
        }

        public bool Equals(RefX1<T> obj)
        {
            return 0 == CompareTo(obj);
        }

        public override string ToString()
        {
            if (null == _val)
                return "<null>";

            return _val.ToString();
        }
    }
    public struct ValX1<T> : IComparableValue, IPublicValue<T>, IComparable<ValX1<T>>, IComparable where T : System.IComparable
    {
        public T _val;
        public System.IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }

        public T publicVal
        {
            get { return _val; }
            set { _val = value; }
        }
        public ValX1(T t) { _val = t; }

        public int CompareTo(ValX1<T> obj)
        {
            if (_val == null)
            {
                if (obj._val == null)
                {
                    return 0;
                }

                return -1;
            }
            return _val.CompareTo(obj._val);
        }

        public int CompareTo(Object obj)
        {
            return _val.CompareTo(((ValX1<T>)obj).Val);
        }

        public bool Equals(ValX1<T> obj)
        {
            return 0 == CompareTo(obj);
        }

        public override string ToString()
        {
            if (null == _val)
                return "<null>";

            return _val.ToString();
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
    public class TestSortedList<K, V> : IDictionary<K, V>
    {
        //
        //Expose the arrays to give more test flexibility...
        //

        public K[] _keys;
        public V[] _values;
        public int _size;
        public int _count;

        public TestSortedList(K[] keys, V[] values)
        {
            _keys = keys;
            _values = values;
            _size = keys.Length;
            _count = keys.Length;
        }


        private int IndexOfKey(K key)
        {
            for (int i = 0; i < _keys.Length; i++)
            {
                if (_keys[i].Equals(key))
                    return i;
            }
            return -1;
        }

        //
        //IDictionary<K,V>
        //

        public V this[K key]
        {
            get
            {
                int index = IndexOfKey(key);
                if (-1 == index)
                    throw new ArgumentException("Key not present in TestSortedList");
                return _values[index];
            }
            set
            {
                int index = IndexOfKey(key);
                if (-1 == index)
                    throw new ArgumentException("Key not present in TestSortedList");
                _values[index] = value;
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            int index = IndexOfKey(key);

            if (-1 == index)
            {
                value = default(V);
                return false;
            }

            value = _values[index];

            return true;
        }

        public ICollection<K> Keys
        {
            get
            {
                return new TestCollection<K>(_keys);
            }
        }

        public ICollection<V> Values
        {
            get
            {
                return new TestCollection<V>(_values);
            }
        }

        public bool Contains(K Key)
        {
            return (-1 != IndexOfKey(Key));
        }

        public bool ContainsKey(K Key)
        {
            return (-1 != IndexOfKey(Key));
        }

        public void Add(K key, V value)
        {
            if (_size == 0)
            {
                _size = 16;
                _keys = new K[_size];
                _values = new V[_size];
            }

            if (_count == _size)
            {
                _size *= 2;
                //ExpandArrays
                K[] tmpk = _keys;
                _keys = new K[_size];
                System.Array.Copy(_keys, 0, tmpk, 0, _count);
                V[] tmpv = _values;
                _values = new V[_size];
                System.Array.Copy(_values, 0, tmpv, 0, _count);
            }
            _keys[_count] = key;
            _values[_count] = value;
            _count++;
        }

        public void Clear()
        {
            _keys = null;
            _values = null;
            _count = 0;
            _size = 0;
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool Remove(K Key)
        {
            int index = IndexOfKey(Key);
            if (-1 == index)
                return false;

            for (int i = index; i < _count - 1; i++)
            {
                _keys[i] = _keys[i + 1];
                _values[i] = _values[i + 1];
            }
            _count--;
            return true;
        }

        //
        //ICollection<KeyValuePair<K,V>> 
        //

        public void CopyTo(KeyValuePair<K, V>[] array, int index)
        {
            array = new KeyValuePair<K, V>[_count];
            for (int i = 0; i < _count; i++)
            {
                array.SetValue(new KeyValuePair<K, V>(_keys[i], _values[i]), i);
            }
        }

        public void Add(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }


        //
        //IEnumerable<KeyValuePair<K,V>>
        //

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return new TestSortedListEnumerator<K, V>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestSortedListEnumerator<K, V>(this);
        }

        //		IEnumerator<KeyValuePair<K,V>> IEnumerable<KeyValuePair<K,V>>.GetEnumerator()
        //    	{
        //    		return new TestSortedListEnumerator<K,V>(this);
        //    	}

        //
        //TestSortedListEnumerator<T>
        //

        private class TestSortedListEnumerator<K1, V1> : System.Collections.Generic.IEnumerator<KeyValuePair<K1, V1>>
        {
            private TestSortedList<K1, V1> _dict;
            private int _index;

            public void Dispose() { }
            public TestSortedListEnumerator(TestSortedList<K1, V1> dict)
            {
                _dict = dict;
                _index = -1;
            }

            public bool MoveNext()
            {
                return (++_index < _dict._count);
            }

            public KeyValuePair<K1, V1> Current
            {
                get { return new KeyValuePair<K1, V1>(_dict._keys[_index], _dict._values[_index]); }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return new KeyValuePair<K1, V1>(_dict._keys[_index], _dict._values[_index]); }
            }

            public KeyValuePair<K1, V1> Entry
            {
                get { return new KeyValuePair<K1, V1>(_dict._keys[_index], _dict._values[_index]); }
            }

            public K1 Key
            {
                get { return _dict._keys[_index]; }
            }

            public V1 Value
            {
                get { return _dict._values[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
    public class Test
    {
        public int counter = 0;
        public bool result = true;
        public void Eval(bool exp)
        {
            counter++;
            Eval(exp, null);
        }
        public bool Eval(bool exp, String errorMsg)
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

        public bool Eval(bool exp, String format, params object[] arg)
        {
            if (!exp)
            {
                return Eval(exp, String.Format(format, arg));
            }

            return true;
        }
    }
    public class SortedListUtils
    {
        private SortedListUtils() { }

        public static SortedList<KeyType, ValueType> FillValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
        {
            SortedList<KeyType, ValueType> _dic = new SortedList<KeyType, ValueType>();
            for (int i = 0; i < keys.Length; i++)
                _dic.Add(keys[i], values[i]);
            return _dic;
        }

        public static SimpleRef<int>[] GetSimpleInts(int count)
        {
            SimpleRef<int>[] ints = new SimpleRef<int>[count];
            for (int i = 0; i < count; i++)
                ints[i] = new SimpleRef<int>(i);
            return ints;
        }

        public static SimpleRef<String>[] GetSimpleStrings(int count)
        {
            SimpleRef<String>[] strings = new SimpleRef<String>[count];
            for (int i = 0; i < count; i++)
                strings[i] = new SimpleRef<String>(i.ToString());
            return strings;
        }
    }
}// Case sensitive key comparison infrastructure
// [Serializable()]
//internal class CaseSensitiveKeyComparer<T> : IComparer<T> {
//    private System.Collections.IComparer _comparer;

//    protected CaseSensitiveKeyComparer(CultureInfo culture, bool ignoreCase) {
//        if (ignoreCase) {
//            _comparer = new System.Collections.CaseInsensitiveComparer(culture);
//        } else {
//           _comparer = new System.Collections.Comparer(culture);
//        }
//    }

//    internal CaseSensitiveKeyComparer(CultureInfo culture) : this (culture, false){
//    }

//    public int Compare(T a, T b) {
//        return _comparer.Compare(a,b);
//    }

//    public bool Equals(T a, T b) {
//        if (Object.ReferenceEquals(a, b)) return true;
//        if (Object.ReferenceEquals(a, null) || Object.ReferenceEquals(b, null)) return false;
//        if (_comparer != null) {
//            // @TODO: Get rid of the special cases in here.  Put in a direct
//            // call to some string comparison function here.

//            /*
//            String sa = a as String;
//            String sb = b as String;
//            if (sa != null && sb != null)
//                return (_comparer.Compare(sa, sb) == 0);
//            */
//            //if (a is String && b is String)
//            return 0 == _comparer.Compare(a, b);
//        }
//        return a.Equals(b);
//    }

//    // The data structure consuming this will be responsible for dealing with null objects as keys.
//    public virtual int GetHashCode(T obj) {
//        return obj.GetHashCode();
//    }
//}

//// Provide a more optimal implementation of ordinal comparison.
//// [Serializable()]
//internal sealed class OrdinalKeyComparer<T> : IComparer<T>  {
//    internal OrdinalKeyComparer() {
//    }

//    public int Compare(T a, T b) {
//        if (Object.ReferenceEquals(a, b)) return 0;
//        if ((Object) a == null) return -1;
//        if ((Object) b == null) return 1;
//        // @TODO: Do something better once a C# bug gets fixed.
//        String sa = ((Object)a) as String;
//        if (sa != null) {
//            String sb = ((Object)b) as String;
//            if (sb != null)
//                return String.CompareOrdinal(sa, sb);
//        }
//        IComparable<T> ia = ((Object)a) as IComparable<T>;
//        if (ia != null)
//            return ia.CompareTo(b);
//        throw new ArgumentException("Argument_ImplementIComparable");
//    }

//    public bool Equals(T a, T b) {
//        if (Object.ReferenceEquals(a, b)) return true; // Reference equality checks
//          if (!Object.ReferenceEquals(a, null)) {
//            return a.Equals(b);
//        }
//        return false;
//    }

//    public int GetHashCode(T obj) {
//        return obj.GetHashCode();
//    }
//}
