// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using SortedDictionary_SortedDictionaryUtils;

namespace SortedDictionary_SortedDictionaryUtils
{
    public class SimpleRef<T> : IComparable<SimpleRef<T>>, IComparable where T : IComparable
    {
        private T _valField;
        public SimpleRef(T t)
        {
            _valField = t;
        }
        public T Val
        {
            get { return _valField; }
            set { _valField = value; }
        }

        public int CompareTo(SimpleRef<T> obj)
        {
            return _valField.CompareTo(obj.Val);
        }

        public int CompareTo(Object obj)
        {
            return _valField.CompareTo(((SimpleRef<T>)obj).Val);
        }

        public override bool Equals(Object obj)
        {
            try
            {
                SimpleRef<T> localTestVar = (SimpleRef<T>)obj;
                if (_valField.Equals(localTestVar.Val))
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
            return _valField.GetHashCode();
        }


        public override string ToString()
        {
            return _valField.ToString();
        }
    }
    public struct SimpleVal<T> : IComparable<SimpleVal<T>> where T : IComparable
    {
        private T _valField;
        public SimpleVal(T t)
        {
            _valField = t;
        }
        public T Val
        {
            get { return _valField; }
            set { _valField = value; }
        }

        public int CompareTo(SimpleVal<T> obj)
        {
            return _valField.CompareTo(obj.Val);
        }

        public int CompareTo(Object obj)
        {
            return _valField.CompareTo(((SimpleVal<T>)obj).Val);
        }

        public override bool Equals(Object obj)
        {
            try
            {
                SimpleVal<T> localTestVar = (SimpleVal<T>)obj;
                if (_valField.Equals(localTestVar.Val))
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
            return _valField.GetHashCode();
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
        public T valField;
        public System.IComparable Val
        {
            get { return valField; }
            set { valField = (T)(object)value; }
        }

        public T publicVal
        {
            get { return valField; }
            set { valField = value; }
        }

        public RefX1(T t) { valField = t; }

        public int CompareTo(RefX1<T> obj)
        {
            return valField.CompareTo(obj.valField);
        }

        public int CompareTo(Object obj)
        {
            return valField.CompareTo(((RefX1<T>)obj).Val);
        }

        public bool Equals(RefX1<T> obj)
        {
            return 0 == CompareTo(obj);
        }

        public override string ToString()
        {
            if (null == valField)
                return "<null>";

            return valField.ToString();
        }
    }
    public struct ValX1<T> : IComparableValue, IPublicValue<T>, IComparable<ValX1<T>>, IComparable where T : System.IComparable
    {
        public T valField;
        public System.IComparable Val
        {
            get { return valField; }
            set { valField = (T)(object)value; }
        }

        public T publicVal
        {
            get { return valField; }
            set { valField = value; }
        }
        public ValX1(T t) { valField = t; }

        public int CompareTo(ValX1<T> obj)
        {
            if (valField == null)
            {
                if (obj.valField == null)
                {
                    return 0;
                }

                return -1;
            }
            return valField.CompareTo(obj.valField);
        }

        public int CompareTo(Object obj)
        {
            return valField.CompareTo(((ValX1<T>)obj).Val);
        }

        public bool Equals(ValX1<T> obj)
        {
            return 0 == CompareTo(obj);
        }

        public override string ToString()
        {
            if (null == valField)
                return "<null>";

            return valField.ToString();
        }
    }
    public class TestCollection<T> : ICollection<T>
    {
        //
        //Expose the array to give more test flexibility...
        //
        public T[] itemsField;

        public TestCollection() { }

        public TestCollection(T[] items)
        {
            itemsField = items;
        }
        //
        //ICollection<T> 
        //
        public void CopyTo(T[] array, int index)
        {
            Array.Copy(itemsField, 0, array, index, itemsField.Length);
        }

        public int Count
        {
            get
            {
                if (itemsField == null)
                    return 0;
                else
                    return itemsField.Length;
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
            get { throw new NotSupportedException(); }
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
                return (++_index < _col.itemsField.Length);
            }

            public T1 Current
            {
                get { return _col.itemsField[_index]; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _col.itemsField[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }

    public class TestSortedDictionary<K, V> : IDictionary<K, V>
    {
        //
        //Expose the arrays to give more test flexibility...
        //

        public K[] keysField;
        public V[] valFieldues;
        public int sizeField;
        public int countField;

        public TestSortedDictionary(K[] keys, V[] values)
        {
            keysField = keys;
            valFieldues = values;
            sizeField = keys.Length;
            countField = keys.Length;
        }


        private int IndexOfKey(K key)
        {
            for (int i = 0; i < keysField.Length; i++)
            {
                if (keysField[i].Equals(key))
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
                    throw new ArgumentException("Key not present in TestSortedDictionary");
                return valFieldues[index];
            }
            set
            {
                int index = IndexOfKey(key);
                if (-1 == index)
                    throw new ArgumentException("Key not present in TestSortedDictionary");
                valFieldues[index] = value;
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

            value = valFieldues[index];

            return true;
        }

        public ICollection<K> Keys
        {
            get { return new TestCollection<K>(keysField); }
        }

        public ICollection<V> Values
        {
            get { return new TestCollection<V>(valFieldues); }
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
            if (sizeField == 0)
            {
                sizeField = 16;
                keysField = new K[sizeField];
                valFieldues = new V[sizeField];
            }

            if (countField == sizeField)
            {
                sizeField *= 2;
                //ExpandArrays
                K[] tmpk = keysField;
                keysField = new K[sizeField];
                System.Array.Copy(keysField, 0, tmpk, 0, countField);
                V[] tmpv = valFieldues;
                valFieldues = new V[sizeField];
                System.Array.Copy(valFieldues, 0, tmpv, 0, countField);
            }
            keysField[countField] = key;
            valFieldues[countField] = value;
            countField++;
        }

        public void Clear()
        {
            keysField = null;
            valFieldues = null;
            countField = 0;
            sizeField = 0;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool Remove(K Key)
        {
            int index = IndexOfKey(Key);
            if (-1 == index)
                return false;

            for (int i = index; i < countField - 1; i++)
            {
                keysField[i] = keysField[i + 1];
                valFieldues[i] = valFieldues[i + 1];
            }
            countField--;
            return true;
        }

        //
        //ICollection<KeyValuePair<K,V>> 
        //

        public void CopyTo(KeyValuePair<K, V>[] array, int index)
        {
            array = new KeyValuePair<K, V>[countField];
            for (int i = 0; i < countField; i++)
            {
                array.SetValue(new KeyValuePair<K, V>(keysField[i], valFieldues[i]), i);
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
            get { return countField; }
        }


        //
        //IEnumerable<KeyValuePair<K,V>>
        //
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return new TestSortedDictionaryEnumerator<K, V>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestSortedDictionaryEnumerator<K, V>(this);
        }


        //
        //TestSortedDictionaryEnumerator<T>
        //    	
        private class TestSortedDictionaryEnumerator<K1, V1> : System.Collections.Generic.IEnumerator<KeyValuePair<K1, V1>>
        {
            private TestSortedDictionary<K1, V1> _dict;
            private int _index;

            public void Dispose() { }
            public TestSortedDictionaryEnumerator(TestSortedDictionary<K1, V1> dict)
            {
                _dict = dict;
                _index = -1;
            }

            public bool MoveNext()
            {
                return (++_index < _dict.countField);
            }

            public KeyValuePair<K1, V1> Current
            {
                get { return new KeyValuePair<K1, V1>(_dict.keysField[_index], _dict.valFieldues[_index]); }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return new KeyValuePair<K1, V1>(_dict.keysField[_index], _dict.valFieldues[_index]); }
            }

            public KeyValuePair<K1, V1> Entry
            {
                get { return new KeyValuePair<K1, V1>(_dict.keysField[_index], _dict.valFieldues[_index]); }
            }

            public K1 Key
            {
                get { return _dict.keysField[_index]; }
            }

            public V1 Value
            {
                get { return _dict.valFieldues[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
    public class SortedDictionaryUtils
    {
        private SortedDictionaryUtils() { }

        public static SortedDictionary<KeyType, ValueType> FillValues<KeyType, ValueType>(KeyType[] keys, ValueType[] values)
        {
            SortedDictionary<KeyType, ValueType> _dic = new SortedDictionary<KeyType, ValueType>();
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
}