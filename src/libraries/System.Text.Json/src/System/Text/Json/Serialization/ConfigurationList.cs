// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// A list of configuration items that can be locked for modification
    /// </summary>
    internal abstract class ConfigurationList<TItem> : IList<TItem>
    {
        protected readonly List<TItem> _list;

        public ConfigurationList(IList<TItem>? source = null)
        {
            _list = source is null ? new List<TItem>() : new List<TItem>(source);
        }

        protected abstract bool IsImmutable { get; }
        protected abstract void VerifyMutable();
        protected virtual void OnAddingElement(TItem item) { }

        public TItem this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                VerifyMutable();
                OnAddingElement(value);
                _list[index] = value;
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => IsImmutable;

        public void Add(TItem item)
        {
            if (item is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(item));
            }

            VerifyMutable();
            OnAddingElement(item);
            _list.Add(item);
        }

        public void Clear()
        {
            VerifyMutable();
            _list.Clear();
        }

        public bool Contains(TItem item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(TItem item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, TItem item)
        {
            if (item is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(item));
            }

            VerifyMutable();
            OnAddingElement(item);
            _list.Insert(index, item);
        }

        public bool Remove(TItem item)
        {
            VerifyMutable();
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            VerifyMutable();
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
