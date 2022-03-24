// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// A list of JsonConverters that respects the options class being immuttable once (de)serialization occurs.
    /// </summary>
    internal sealed class ConverterList : IList<JsonConverter>
    {
        private readonly List<JsonConverter> _list;
        private readonly JsonSerializerOptions _options;

        public ConverterList(JsonSerializerOptions options)
        {
            _options = options;
            _list = new List<JsonConverter>();
        }

        public ConverterList(JsonSerializerOptions options, ConverterList source)
        {
            _options = options;
            _list = new List<JsonConverter>(source._list);
        }

        public JsonConverter this[int index]
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
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(JsonConverter item!!)
        {
            _options.VerifyMutable();
            _list.Add(item);
        }

        public void Clear()
        {
            _options.VerifyMutable();
            _list.Clear();
        }

        public bool Contains(JsonConverter item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(JsonConverter[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<JsonConverter> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(JsonConverter item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, JsonConverter item!!)
        {
            _options.VerifyMutable();
            _list.Insert(index, item);
        }

        public bool Remove(JsonConverter item)
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
