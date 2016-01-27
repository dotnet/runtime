// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class DictionaryKeyCollection<TKey, TValue> : ICollection<TKey>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

        public DictionaryKeyCollection(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            this.dictionary = dictionary;
        }

        public void CopyTo(TKey[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (array.Length <= index && this.Count > 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_IndexOutOfRangeException"));
            if (array.Length - index < dictionary.Count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));

            int i = index;
            foreach (KeyValuePair<TKey, TValue> mapping in dictionary)
            {
                array[i++] = mapping.Key;
            }
        }

        public int Count {
            get { return dictionary.Count; }
        }

        bool ICollection<TKey>.IsReadOnly {
            get { return true; }
        }

        void ICollection<TKey>.Add(TKey item)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
        }

        void ICollection<TKey>.Clear()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
        }

        public bool Contains(TKey item)
        {
            return dictionary.ContainsKey(item);
        }

        bool ICollection<TKey>.Remove(TKey item)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TKey>)this).GetEnumerator();
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            return new DictionaryKeyEnumerator<TKey, TValue>(dictionary);
        }
    }  // public class DictionaryKeyCollection<TKey, TValue>


    [Serializable]
    internal sealed class DictionaryKeyEnumerator<TKey, TValue> : IEnumerator<TKey>
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;

        public DictionaryKeyEnumerator(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            this.dictionary = dictionary;
            this.enumeration = dictionary.GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            enumeration.Dispose();
        }

        public bool MoveNext()
        {
            return enumeration.MoveNext();
        }

        Object IEnumerator.Current {
            get { return ((IEnumerator<TKey>)this).Current; }
        }

        public TKey Current {
            get { return enumeration.Current.Key; }
        }

        public void Reset()
        {
            enumeration = dictionary.GetEnumerator();
        }
    }  // class DictionaryKeyEnumerator<TKey, TValue>
}

// DictionaryKeyCollection.cs
