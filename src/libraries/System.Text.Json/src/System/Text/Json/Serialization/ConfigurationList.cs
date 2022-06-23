// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// A list of configuration items that respects the options class being immutable once (de)serialization occurs.
    /// </summary>
    internal sealed class ConfigurationList<TItem> : IList<TItem>
    {
        private readonly List<TItem> _list;
        private readonly JsonSerializerOptions _options;

        public Action<TItem>? OnElementAdded { get; set; }

        public ConfigurationList(JsonSerializerOptions options)
        {
            _options = options;
            _list = new List<TItem>();
        }

        public ConfigurationList(JsonSerializerOptions options, IList<TItem> source)
        {
            _options = options;
            _list = new List<TItem>(source is ConfigurationList<TItem> cl ? cl._list : source);
        }

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

                _options.VerifyMutable();
                _list[index] = value;
                OnElementAdded?.Invoke(value);
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(TItem item)
        {
            if (item is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(item));
            }

            _options.VerifyMutable();
            _list.Add(item);
            OnElementAdded?.Invoke(item);
        }

        public void Clear()
        {
            _options.VerifyMutable();
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

            _options.VerifyMutable();
            _list.Insert(index, item);
            OnElementAdded?.Invoke(item);
        }

        public bool Remove(TItem item)
        {
            _options.VerifyMutable();
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _options.VerifyMutable();
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
