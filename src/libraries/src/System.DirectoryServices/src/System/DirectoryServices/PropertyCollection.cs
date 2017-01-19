// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;
    using System.DirectoryServices.Interop;
    using System.Security.Permissions;
    using System.Globalization;

    /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection"]/*' />
    /// <devdoc>
    /// <para>Contains the properties on a <see cref='System.DirectoryServices.DirectoryEntry'/>.</para>
    /// </devdoc>
    public class PropertyCollection : IDictionary
    {
        private DirectoryEntry _entry;
        internal Hashtable valueTable = null;

        internal PropertyCollection(DirectoryEntry entry)
        {
            _entry = entry;
            Hashtable tempTable = new Hashtable();
            valueTable = Hashtable.Synchronized(tempTable);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.this"]/*' />
        /// <devdoc>
        ///    <para>Gets the property with the given name.</para>
        /// </devdoc>
        public PropertyValueCollection this[string propertyName]
        {
            get
            {
                if (propertyName == null)
                    throw new ArgumentNullException("propertyName");

                string name = propertyName.ToLower(CultureInfo.InvariantCulture);
                if (valueTable.Contains(name))
                    return (PropertyValueCollection)valueTable[name];
                else
                {
                    PropertyValueCollection value = new PropertyValueCollection(_entry, propertyName);
                    valueTable.Add(name, value);
                    return value;
                }
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.Count"]/*' />
        /// <devdoc>
        ///    <para>Gets the number of properties available on this entry.</para>
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

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyNames"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ICollection PropertyNames
        {
            get
            {
                return new KeysCollection(this);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.Values"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ICollection Values
        {
            get
            {
                return new ValuesCollection(this);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(string propertyName)
        {
            //entry.FillCache(propertyName);
            object var;
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

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.CopyTo"]/*' />
        /// <devdoc>
        /// <para>Copies the elements of this instance into an <see cref='System.Array'/>, starting at a particular index into the array.</para>
        /// </devdoc>
        public void CopyTo(PropertyValueCollection[] array, int index)
        {
            ((ICollection)this).CopyTo((Array)array, index);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.GetEnumerator"]/*' />
        /// <devdoc>
        ///    <para>Returns an enumerator, which can be used to iterate through the collection.</para>
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

            UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList)entryToUse.AdsObject;

            entryToUse.propertiesAlreadyEnumerated = true;
            return new PropertyEnumerator(_entry, entryToUse);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.this"]/*' />
        ///<internalonly/>
        object IDictionary.this[object key]
        {
            get
            {
                return this[(string)key];
            }

            set
            {
                throw new NotSupportedException(SR.DSPropertySetSupported);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.IsFixedSize"]/*' />
        ///<internalonly/>
        bool IDictionary.IsFixedSize
        {
            get
            {
                return true;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.IsReadOnly"]/*' />
        ///<internalonly/>
        bool IDictionary.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Keys"]/*' />
        ///<internalonly/>
        ICollection IDictionary.Keys
        {
            get
            {
                return new KeysCollection(this);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Add"]/*' />
        ///<internalonly/>
        void IDictionary.Add(object key, object value)
        {
            throw new NotSupportedException(SR.DSAddNotSupported);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Clear"]/*' />
        ///<internalonly/>
        void IDictionary.Clear()
        {
            throw new NotSupportedException(SR.DSClearNotSupported);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Contains"]/*' />
        ///<internalonly/>
        bool IDictionary.Contains(object value)
        {
            return this.Contains((string)value);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Remove"]/*' />
        ///<internalonly/>
        void IDictionary.Remove(object key)
        {
            throw new NotSupportedException(SR.DSRemoveNotSupported);
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IEnumerable.GetEnumerator"]/*' />
        ///<internalonly/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ICollection.IsSynchronized"]/*' />
        ///<internalonly/>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ICollection.SyncRoot"]/*' />
        ///<internalonly/>
        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ICollection.CopyTo"]/*' />
        ///<internalonly/>
        void ICollection.CopyTo(Array array, Int32 index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (array.Rank != 1)
                throw new ArgumentException(SR.OnlyAllowSingleDimension, "array");

            if (index < 0)
                throw new ArgumentOutOfRangeException(SR.LessThanZero, "index");

            if (((index + Count) > array.Length) || ((index + Count) < index))
                throw new ArgumentException(SR.DestinationArrayNotLargeEnough);

            foreach (PropertyValueCollection value in this)
            {
                array.SetValue(value, index);
                index++;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator"]/*' />
        ///<internalonly/>
        private class PropertyEnumerator : IDictionaryEnumerator, IDisposable
        {
            private DirectoryEntry _entry;               // clone (to be disposed)
            private DirectoryEntry _parentEntry;         // original entry to pass to PropertyValueCollection
            private string _currentPropName = null;

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.PropertyEnumerator"]/*' />
            ///<internalonly/>
            public PropertyEnumerator(DirectoryEntry parent, DirectoryEntry clone)
            {
                _entry = clone;
                _parentEntry = parent;
            }

            ~PropertyEnumerator()
            {
                Dispose(true);      // finalizer is called => Dispose has not been called yet.
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyEnumerator.Dispose"]/*' />
            /// <devdoc>        
            /// </devdoc>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyEnumerator.Dispose1"]/*' />
            /// <devdoc>        
            /// </devdoc>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _entry.Dispose();
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Current"]/*' />
            ///<internalonly/>
            public object Current
            {
                get
                {
                    return Entry.Value;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Entry"]/*' />
            ///<internalonly/>
            public DictionaryEntry Entry
            {
                get
                {
                    if (_currentPropName == null)
                        throw new InvalidOperationException(SR.DSNoCurrentProperty);

                    return new DictionaryEntry(_currentPropName, new PropertyValueCollection(_parentEntry, _currentPropName));
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Key"]/*' />
            ///<internalonly/>
            public object Key
            {
                get
                {
                    return Entry.Key;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Value"]/*' />
            ///<internalonly/>
            public object Value
            {
                get
                {
                    return Entry.Value;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.MoveNext"]/*' />
            ///<internalonly/>
            public bool MoveNext()
            {
                object prop;
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

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Reset"]/*' />
            ///<internalonly/>
            public void Reset()
            {
                ((UnsafeNativeMethods.IAdsPropertyList)_entry.AdsObject).Reset();
                _currentPropName = null;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection"]/*' />
        ///<internalonly/>
        private class ValuesCollection : ICollection
        {
            protected PropertyCollection props;

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.ValuesCollection"]/*' />
            ///<internalonly/>
            public ValuesCollection(PropertyCollection props)
            {
                this.props = props;
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.Count"]/*' />
            ///<internalonly/>
            public int Count
            {
                get
                {
                    return props.Count;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.IsReadOnly"]/*' />
            ///<internalonly/>
            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.IsSynchronized"]/*' />
            ///<internalonly/>
            public bool IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.SyncRoot"]/*' />
            ///<internalonly/>
            public object SyncRoot
            {
                get
                {
                    return ((ICollection)props).SyncRoot;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.CopyTo"]/*' />
            ///<internalonly/>
            public void CopyTo(Array array, int index)
            {
                foreach (object value in this)
                    array.SetValue(value, index++);
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.GetEnumerator"]/*' />
            ///<internalonly/>
            public virtual IEnumerator GetEnumerator()
            {
                return new ValuesEnumerator(props);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysCollection"]/*' />
        ///<internalonly/>   
        private class KeysCollection : ValuesCollection
        {
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysCollection.KeysCollection"]/*' />
            ///<internalonly/>
            public KeysCollection(PropertyCollection props)
            : base(props)
            {
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysCollection.GetEnumerator"]/*' />
            ///<internalonly/>
            public override IEnumerator GetEnumerator()
            {
                props._entry.FillCache("");
                return new KeysEnumerator(props);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator"]/*' />
        ///<internalonly/>
        private class ValuesEnumerator : IEnumerator
        {
            private int _currentIndex = -1;
            protected PropertyCollection propCollection;

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.ValuesEnumerator"]/*' />
            ///<internalonly/>
            public ValuesEnumerator(PropertyCollection propCollection)
            {
                this.propCollection = propCollection;
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.CurrentIndex"]/*' />
            ///<internalonly/>
            protected int CurrentIndex
            {
                get
                {
                    if (_currentIndex == -1)
                        throw new InvalidOperationException(SR.DSNoCurrentValue);
                    return _currentIndex;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.Current"]/*' />
            ///<internalonly/>
            public virtual object Current
            {
                get
                {
                    UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList)propCollection._entry.AdsObject;
                    return propCollection[((UnsafeNativeMethods.IAdsPropertyEntry)propList.Item(CurrentIndex)).Name];
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.MoveNext"]/*' />
            ///<internalonly/>
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

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.Reset"]/*' />
            ///<internalonly/>
            public void Reset()
            {
                _currentIndex = -1;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysEnumerator"]/*' />
        ///<internalonly/>
        private class KeysEnumerator : ValuesEnumerator
        {
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysEnumerator.KeysEnumerator"]/*' />
            ///<internalonly/>
            public KeysEnumerator(PropertyCollection collection)
            : base(collection)
            {
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysEnumerator.Current"]/*' />
            ///<internalonly/>
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

