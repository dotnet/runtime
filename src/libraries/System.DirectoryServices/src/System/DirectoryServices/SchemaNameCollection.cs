// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.DirectoryServices.Interop;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Contains a list of schema names used for the <see cref='System.DirectoryServices.DirectoryEntries.SchemaFilter'/> property of a <see cref='System.DirectoryServices.DirectoryEntries'/>.
    /// </devdoc>
    public class SchemaNameCollection : IList
    {
        private readonly VariantPropGetter _propGetter;
        private readonly VariantPropSetter _propSetter;

        internal SchemaNameCollection(VariantPropGetter propGetter, VariantPropSetter propSetter)
        {
            _propGetter = propGetter;
            _propSetter = propSetter;
        }

        /// <devdoc>
        ///  Gets or sets the object at the given index.
        ///  </devdoc>
        public string? this[int index]
        {
            get
            {
                object?[] values = GetValue();
                return (string?)values[index];
            }
            set
            {
                object?[] values = GetValue();
                values[index] = value;
                _propSetter(values);
            }
        }

        /// <devdoc>
        /// Gets the number of objects available on this entry.
        /// </devdoc>
        public int Count
        {
            get
            {
                object?[] values = GetValue();
                return values.Length;
            }
        }

        /// <devdoc>
        /// Appends the value to the collection.
        /// </devdoc>
        public int Add(string? value)
        {
            object?[] oldValues = GetValue();
            object?[] newValues = new object[oldValues.Length + 1];
            for (int i = 0; i < oldValues.Length; i++)
                newValues[i] = oldValues[i];
            newValues[newValues.Length - 1] = value;
            _propSetter(newValues);
            return newValues.Length - 1;
        }

        /// <devdoc>
        /// Appends the values to the collection.
        /// </devdoc>
        public void AddRange(string?[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            object?[] oldValues = GetValue();
            object?[] newValues = new object[oldValues.Length + value.Length];
            for (int i = 0; i < oldValues.Length; i++)
                newValues[i] = oldValues[i];
            for (int i = oldValues.Length; i < newValues.Length; i++)
                newValues[i] = value[i - oldValues.Length];
            _propSetter(newValues);
        }

        public void AddRange(SchemaNameCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            object?[] oldValues = GetValue();
            object?[] newValues = new object?[oldValues.Length + value.Count];
            for (int i = 0; i < oldValues.Length; i++)
                newValues[i] = oldValues[i];
            for (int i = oldValues.Length; i < newValues.Length; i++)
                newValues[i] = value[i - oldValues.Length];
            _propSetter(newValues);
        }

        /// <devdoc>
        /// Removes all items from the collection.
        /// </devdoc>
        public void Clear()
        {
            _propSetter(Array.Empty<object>());
        }

        /// <devdoc>
        /// Determines if the collection contains a specific value.
        /// </devdoc>
        public bool Contains(string? value) => IndexOf(value) != -1;

        public void CopyTo(string?[] stringArray, int index)
        {
            object?[] values = GetValue();
            values.CopyTo(stringArray, index);
        }

        public IEnumerator GetEnumerator()
        {
            object?[] values = GetValue();
            return values.GetEnumerator();
        }

        private object?[] GetValue()
        {
            object? value = _propGetter();
            if (value == null)
                return Array.Empty<object>();
            else
                return (object?[])value;
        }

        /// <devdoc>
        /// Determines the index of a specific item in the collection.
        /// </devdoc>
        public int IndexOf(string? value)
        {
            object?[] values = GetValue();
            for (int i = 0; i < values.Length; i++)
            {
                if (value == (string?)values[i])
                    return i;
            }
            return -1;
        }

        /// <devdoc>
        /// Inserts an item at the specified position in the collection.
        /// </devdoc>
        public void Insert(int index, string? value)
        {
            ArrayList tmpList = new ArrayList((ICollection)GetValue());
            tmpList.Insert(index, value);
            _propSetter(tmpList.ToArray());
        }

        /// <devdoc>
        /// Removes an item from the collection.
        /// </devdoc>
        public void Remove(string? value)
        {
            // this does take two scans of the array, but value isn't guaranteed to be there.
            int index = IndexOf(value);
            RemoveAt(index);
        }

        /// <devdoc>
        /// Removes the item at the specified index from the collection.
        /// </devdoc>
        public void RemoveAt(int index)
        {
            object?[] oldValues = GetValue();
            if (index >= oldValues.Length || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            object?[] newValues = new object[oldValues.Length - 1];
            for (int i = 0; i < index; i++)
                newValues[i] = oldValues[i];
            for (int i = index + 1; i < oldValues.Length; i++)
                newValues[i - 1] = oldValues[i];
            _propSetter(newValues);
        }

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        void ICollection.CopyTo(Array array, int index)
        {
            object?[] values = GetValue();
            values.CopyTo(array, index);
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object? IList.this[int index]
        {
            get => this[index];
            set => this[index] = (string?)value;
        }

        int IList.Add(object? value) => Add((string?)value);

        bool IList.Contains(object? value) => Contains((string?)value);

        int IList.IndexOf(object? value) => IndexOf((string?)value);

        void IList.Insert(int index, object? value) => Insert(index, (string?)value);

        void IList.Remove(object? value) => Remove((string?)value);

        internal delegate object? VariantPropGetter();
        internal delegate void VariantPropSetter(object? value);

        // this class and HintsDelegateWrapper exist only because you can't create
        // a delegate to a property's accessors. You have to supply methods. So these
        // classes wrap an object and supply properties as methods.
        internal sealed class FilterDelegateWrapper
        {
            private readonly UnsafeNativeMethods.IAdsContainer _obj;
            internal FilterDelegateWrapper(UnsafeNativeMethods.IAdsContainer wrapped)
            {
                _obj = wrapped;
            }

            public VariantPropGetter Getter => new VariantPropGetter(GetFilter);

            public VariantPropSetter Setter => new VariantPropSetter(SetFilter);

            private object? GetFilter() => _obj.Filter;

            private void SetFilter(object? value) => _obj.Filter = value;
        }
    }
}
