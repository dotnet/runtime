// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable.Tests
{
    /// <summary>
    /// Defines a bridging API for testing various immutable list implementations.
    /// </summary>
    public abstract class ImmutableListQueries<T>(IList<T> underlyingList) : ICollection<T>, IList
    {
        public int Count => underlyingList.Count;
        public T this[int index] => underlyingList[index];
        public abstract ImmutableList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter);
        public abstract void ForEach(Action<T> action);
        public abstract ImmutableList<T> GetRange(int index, int count);
        public abstract void CopyTo(T[] array);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract void CopyTo(int index, T[] array, int arrayIndex, int count);
        public abstract bool Exists(Predicate<T> match);
        public abstract T? Find(Predicate<T> match);
        public abstract ImmutableList<T> FindAll(Predicate<T> match);
        public abstract int FindIndex(Predicate<T> match);
        public abstract int FindIndex(int startIndex, Predicate<T> match);
        public abstract int FindIndex(int startIndex, int count, Predicate<T> match);
        public abstract T? FindLast(Predicate<T> match);
        public abstract int FindLastIndex(Predicate<T> match);
        public abstract int FindLastIndex(int startIndex, Predicate<T> match);
        public abstract int FindLastIndex(int startIndex, int count, Predicate<T> match);
        public abstract bool TrueForAll(Predicate<T> match);
        public abstract int BinarySearch(T item);
        public abstract int BinarySearch(T item, IComparer<T>? comparer);
        public abstract int BinarySearch(int index, int count, T item, IComparer<T>? comparer);
        public IEnumerator<T> GetEnumerator() => underlyingList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => underlyingList.GetEnumerator();


        void ICollection<T>.Add(T item) => underlyingList.Add(item);
        void ICollection<T>.Clear() => underlyingList.Clear();
        bool ICollection<T>.Contains(T item) => underlyingList.Contains(item);
        bool ICollection<T>.Remove(T item) => underlyingList.Remove(item);
        int IList.Add(object? value) => ((IList)underlyingList).Add(value);
        void IList.Clear() => ((IList)underlyingList).Clear();
        bool IList.Contains(object? value) => ((IList)underlyingList).Contains(value);
        int IList.IndexOf(object? value) => ((IList)underlyingList).IndexOf(value);
        void IList.Insert(int index, object? value) => ((IList)underlyingList).Insert(index, value);
        void IList.Remove(object? value) => ((IList)underlyingList).Remove(value);
        void IList.RemoveAt(int index) => ((IList)underlyingList).RemoveAt(index);
        void ICollection.CopyTo(Array array, int index) => ((ICollection)underlyingList).CopyTo(array, index);
        bool ICollection<T>.IsReadOnly => underlyingList.IsReadOnly;
        bool IList.IsFixedSize => ((IList)underlyingList).IsFixedSize;
        bool IList.IsReadOnly => ((IList)underlyingList).IsReadOnly;
        bool ICollection.IsSynchronized => ((IList)underlyingList).IsSynchronized;
        object ICollection.SyncRoot => ((IList)underlyingList).SyncRoot;
        object? IList.this[int index]
        {
            get => underlyingList[index];
            set => underlyingList[index] = (T)value!;
        }
    }
}
