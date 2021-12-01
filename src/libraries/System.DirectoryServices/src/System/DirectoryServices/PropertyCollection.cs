// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;
using System.DirectoryServices.Interop;
using System.Globalization;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Contains the properties on a <see cref='System.DirectoryServices.DirectoryEntry'/>.
    /// </devdoc>
    public class PropertyCollection : IDictionary
    {
        private readonly DirectoryEntry _entry;
        internal readonly Hashtable valueTable;

        internal PropertyCollection(DirectoryEntry entry)
        {
            _entry = entry;
            Hashtable tempTable = new Hashtable();
            valueTable = Hashtable.Synchronized(tempTable);
        }

        /// <devdoc>
        /// Gets the property with the given name.
        /// </devdoc>
        public PropertyValueCollection this[string propertyName]
        {
            get
            {
                if (propertyName == null)
                    throw new ArgumentNullException(nameof(propertyName));

                string name = propertyName.ToLowerInvariant();
                if (valueTable.Contains(name))
                    return (PropertyValueCollection)valueTable[name]!;
                else
                {
                    PropertyValueCollection value = new PropertyValueCollection(_entry, propertyName);
                    valueTable.Add(name, value);
                    return value;
                }
            }
        }

        /// <devdoc>
        /// Gets the number of properties available on this entry.
        /// </devdoc>
        public int Count
        {
            get
            {
                if (!(_entry.AdsObject is UnsafeNativeMethods.IAdsPropertyList))
                    throw new NotSupportedException(SR.DSCannotCount);

                _entry.FillCache("");

                UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList)_entry.AdsObject;

                return propList.PropertyCount;
            }
        }

        public ICollection PropertyNames => new KeysCollection(this);

        public ICollection Values => new ValuesCollection(this);

        public bool Contains(string propertyName)
        {
            object? var;
            int unmanagedResult = _entry.AdsObject.GetEx(propertyName, out var);
            if (unmanagedResult != 0)
            {
                //  property not found (IIS provider returns 0x80005006, other provides return 0x8000500D).
                if ((unmanagedResult == unchecked((int)0x8000500D)) || (unmanagedResult == unchecked((int)0x80005006)))
                {
                    return false;
                }
                else
                {
                    throw COMExceptionHelper.CreateFormattedComException(unmanagedResult);
                }
            }

            return true;
        }

        /// <devdoc>
        /// Copies the elements of this instance into an <see cref='System.Array'/>, starting at a particular index into the array.
        /// </devdoc>
        public void CopyTo(PropertyValueCollection[] array, int index)
        {
            ((ICollection)this).CopyTo((Array)array, index);
        }

        /// <devdoc>
        /// Returns an enumerator, which can be used to iterate through the collection.
        /// </devdoc>
        public IDictionaryEnumerator GetEnumerator()
        {
            if (!(_entry.AdsObject is UnsafeNativeMethods.IAdsPropertyList))
                throw new NotSupportedException(SR.DSCannotEmunerate);

            // Once an object has been used for an enumerator once, it can't be used again, because it only
            // maintains a single cursor. Re-bind to the ADSI object to get a new instance.
            // That's why we must clone entry here. It will be automatically disposed inside Enumerator.
            DirectoryEntry entryToUse = _entry.CloneBrowsable();
            entryToUse.FillCache("");

            _ = (UnsafeNativeMethods.IAdsPropertyList)entryToUse.AdsObject;

            entryToUse.propertiesAlreadyEnumerated = true;
            return new PropertyEnumerator(_entry, entryToUse);
        }

        object? IDictionary.this[object key]
        {
            get => this[(string)key];
            set => throw new NotSupportedException(SR.DSPropertySetSupported);
        }

        bool IDictionary.IsFixedSize => true;

        bool IDictionary.IsReadOnly => true;

        ICollection IDictionary.Keys => new KeysCollection(this);

        void IDictionary.Add(object key, object? value)
        {
            throw new NotSupportedException(SR.DSAddNotSupported);
        }

        void IDictionary.Clear()
        {
            throw new NotSupportedException(SR.DSClearNotSupported);
        }

        bool IDictionary.Contains(object value) => Contains((string)value);

        void IDictionary.Remove(object key)
        {
            throw new NotSupportedException(SR.DSRemoveNotSupported);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException(SR.OnlyAllowSingleDimension, nameof(array));

            if (index < 0)
                throw new ArgumentOutOfRangeException(SR.LessThanZero, nameof(index));

            if (((index + Count) > array.Length) || ((index + Count) < index))
                throw new ArgumentException(SR.DestinationArrayNotLargeEnough);

            foreach (PropertyValueCollection value in this)
            {
                array.SetValue(value, index);
                index++;
            }
        }

        private sealed class PropertyEnumerator : IDictionaryEnumerator, IDisposable
        {
            private readonly DirectoryEntry _entry;               // clone (to be disposed)
            private readonly DirectoryEntry _parentEntry;         // original entry to pass to PropertyValueCollection
            private string? _currentPropName;

            public PropertyEnumerator(DirectoryEntry parent, DirectoryEntry clone)
            {
                _entry = clone;
                _parentEntry = parent;
            }

            ~PropertyEnumerator() => Dispose(true);

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _entry.Dispose();
                }
            }

            public object Current => Entry.Value!;

            public DictionaryEntry Entry
            {
                get
                {
                    if (_currentPropName == null)
                        throw new InvalidOperationException(SR.DSNoCurrentProperty);

                    return new DictionaryEntry(_currentPropName, new PropertyValueCollection(_parentEntry, _currentPropName));
                }
            }

            public object Key => Entry.Key;

            public object Value => Entry.Value!;

            public bool MoveNext()
            {
                object? prop;
                int hr = 0;
                try
                {
                    hr = ((UnsafeNativeMethods.IAdsPropertyList)_entry.AdsObject).Next(out prop);
                }
                catch (COMException e)
                {
                    hr = e.ErrorCode;
                    prop = null;
                }
                if (hr == 0)
                {
                    if (prop != null)
                        _currentPropName = ((UnsafeNativeMethods.IAdsPropertyEntry)prop).Name;
                    else
                        _currentPropName = null;

                    return true;
                }
                else
                {
                    _currentPropName = null;
                    return false;
                }
            }

            public void Reset()
            {
                ((UnsafeNativeMethods.IAdsPropertyList)_entry.AdsObject).Reset();
                _currentPropName = null;
            }
        }

        private class ValuesCollection : ICollection
        {
            protected PropertyCollection props;

            public ValuesCollection(PropertyCollection props)
            {
                this.props = props;
            }

            public int Count => props.Count;

            public bool IsReadOnly => true;

            public bool IsSynchronized => false;

            public object SyncRoot => ((ICollection)props).SyncRoot;

            public void CopyTo(Array array, int index)
            {
                foreach (object value in this)
                    array.SetValue(value, index++);
            }

            public virtual IEnumerator GetEnumerator() => new ValuesEnumerator(props);
        }

        private sealed class KeysCollection : ValuesCollection
        {
            public KeysCollection(PropertyCollection props) : base(props)
            {
            }

            public override IEnumerator GetEnumerator()
            {
                props._entry.FillCache("");
                return new KeysEnumerator(props);
            }
        }

        private class ValuesEnumerator : IEnumerator
        {
            private int _currentIndex = -1;
            protected PropertyCollection propCollection;

            public ValuesEnumerator(PropertyCollection propCollection)
            {
                this.propCollection = propCollection;
            }

            protected int CurrentIndex
            {
                get
                {
                    if (_currentIndex == -1)
                        throw new InvalidOperationException(SR.DSNoCurrentValue);
                    return _currentIndex;
                }
            }

            public virtual object Current
            {
                get
                {
                    UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList)propCollection._entry.AdsObject;
                    return propCollection[((UnsafeNativeMethods.IAdsPropertyEntry)propList.Item(CurrentIndex)).Name];
                }
            }

            public bool MoveNext()
            {
                _currentIndex++;
                if (_currentIndex >= propCollection.Count)
                {
                    _currentIndex = -1;
                    return false;
                }
                else
                    return true;
            }

            public void Reset() => _currentIndex = -1;
        }

        private sealed class KeysEnumerator : ValuesEnumerator
        {
            public KeysEnumerator(PropertyCollection collection) : base(collection)
            {
            }

            public override object Current
            {
                get
                {
                    UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList)propCollection._entry.AdsObject;

                    return ((UnsafeNativeMethods.IAdsPropertyEntry)propList.Item(CurrentIndex)).Name;
                }
            }
        }
    }
}
