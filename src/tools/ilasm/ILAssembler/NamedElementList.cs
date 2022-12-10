// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace ILAssembler
{
    internal interface INamed
    {
        string Name { get; }
    }

    internal class NamedElementList<T> : IList<T>
        where T : INamed
    {
        private readonly List<T> _elements = new();
        private readonly Dictionary<string, T> _elementsByName = new();

        public T this[int index]
        {
            get => _elements[index];
            set
            {
                T oldElement = _elements[index];
                _elements[index] = value;
                _elementsByName.Remove(oldElement.Name);
                _elementsByName[value.Name] = value;
            }
        }

        public T this[string name]
        {
            get => _elementsByName[name];
        }

        public int Count => _elements.Count;

        public bool IsReadOnly => ((ICollection<T>)_elements).IsReadOnly;

        public void Add(T item)
        {
            _elements.Add(item);
            _elementsByName.Add(item.Name, item);
        }

        public void Clear()
        {
            _elements.Clear();
            _elementsByName.Clear();
        }

        public bool Contains(T item) => _elements.Contains(item);

        public bool Contains(string name) => _elementsByName.ContainsKey(name);

        public void CopyTo(T[] array, int arrayIndex) => _elements.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _elements.GetEnumerator();
        public int IndexOf(T item) => _elements.IndexOf(item);
        public void Insert(int index, T item)
        {
            _elements.Insert(index, item);
            _elementsByName.Add(item.Name, item);
        }

        public bool Remove(T item)
        {
            bool result = _elements.Remove(item);
            if (result)
            {
                _elementsByName.Remove(item.Name);
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            T element = _elements[index];
            _elements.RemoveAt(index);
            _elementsByName.Remove(element.Name);
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_elements).GetEnumerator();
    }
}
