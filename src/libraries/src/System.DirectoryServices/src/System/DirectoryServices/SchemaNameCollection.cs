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

    /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection"]/*' />
    /// <devdoc>
    /// <para>Contains a list of schema names used for the <see cref='System.DirectoryServices.DirectoryEntries.SchemaFilter'/> property of a <see cref='System.DirectoryServices.DirectoryEntries'/>.</para>
    /// </devdoc>
    public class SchemaNameCollection : IList
    {
        private VariantPropGetter _propGetter;
        private VariantPropSetter _propSetter;

        internal SchemaNameCollection(VariantPropGetter propGetter, VariantPropSetter propSetter)
        {
            _propGetter = propGetter;
            _propSetter = propSetter;
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.this"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets the object
        ///       at the given index.</para>
        /// </devdoc>
        public string this[int index]
        {
            get
            {
                object[] values = GetValue();
                return (string)values[index];
            }
            set
            {
                object[] values = GetValue();
                values[index] = value;
                _propSetter(values);
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.Count"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the number of objects available on this entry.
        ///    </para>
        /// </devdoc>
        public int Count
        {
            get
            {
                object[] values = GetValue();
                return values.Length;
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.Add"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Appends the value to the
        ///       collection.
        ///    </para>
        /// </devdoc>
        public int Add(string value)
        {
            object[] oldValues = GetValue();
            object[] newValues = new object[oldValues.Length + 1];
            for (int i = 0; i < oldValues.Length; i++)
                newValues[i] = oldValues[i];
            newValues[newValues.Length - 1] = value;
            _propSetter(newValues);
            return newValues.Length - 1;
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.AddRange"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Appends the values to the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(string[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            object[] oldValues = GetValue();
            object[] newValues = new object[oldValues.Length + value.Length];
            for (int i = 0; i < oldValues.Length; i++)
                newValues[i] = oldValues[i];
            for (int i = oldValues.Length; i < newValues.Length; i++)
                newValues[i] = value[i - oldValues.Length];
            _propSetter(newValues);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.AddRange1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddRange(SchemaNameCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            object[] oldValues = GetValue();
            object[] newValues = new object[oldValues.Length + value.Count];
            for (int i = 0; i < oldValues.Length; i++)
                newValues[i] = oldValues[i];
            for (int i = oldValues.Length; i < newValues.Length; i++)
                newValues[i] = value[i - oldValues.Length];
            _propSetter(newValues);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.Clear"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Removes all items from the collection.
        ///    </para>
        /// </devdoc>
        public void Clear()
        {
            object[] newValues = new object[0];
            _propSetter(newValues);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Determines if the collection contains a specific value.
        ///    </para>
        /// </devdoc>
        public bool Contains(string value)
        {
            return IndexOf(value) != -1;
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(String[] stringArray, int index)
        {
            object[] values = GetValue();
            values.CopyTo(stringArray, index);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.GetEnumerator"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public IEnumerator GetEnumerator()
        {
            object[] values = GetValue();
            return values.GetEnumerator();
        }

        private object[] GetValue()
        {
            object value = _propGetter();
            if (value == null)
                return new object[0];
            else
                return (object[])value;
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Determines the index of a specific item in the collection.
        ///    </para>
        /// </devdoc>
        public int IndexOf(string value)
        {
            object[] values = GetValue();
            for (int i = 0; i < values.Length; i++)
            {
                if (value == (string)values[i])
                    return i;
            }
            return -1;
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.Insert"]/*' />
        /// <devdoc>
        ///    <para>Inserts an item at the specified position in the collection.</para>
        /// </devdoc>
        public void Insert(int index, string value)
        {
            ArrayList tmpList = new ArrayList((ICollection)GetValue());
            tmpList.Insert(index, value);
            _propSetter(tmpList.ToArray());
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.Remove"]/*' />
        /// <devdoc>
        ///    <para>Removes an item from the collection.</para>
        /// </devdoc>
        public void Remove(string value)
        {
            // this does take two scans of the array, but value isn't guaranteed to be there.
            int index = IndexOf(value);
            RemoveAt(index);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.RemoveAt"]/*' />
        /// <devdoc>
        ///    <para>Removes the
        ///       item at the specified index from the collection.</para>
        /// </devdoc>
        public void RemoveAt(int index)
        {
            object[] oldValues = GetValue();
            if (index >= oldValues.Length || index < 0)
                throw new ArgumentOutOfRangeException("index");

            object[] newValues = new object[oldValues.Length - 1];
            for (int i = 0; i < index; i++)
                newValues[i] = oldValues[i];
            for (int i = index + 1; i < oldValues.Length; i++)
                newValues[i - 1] = oldValues[i];
            _propSetter(newValues);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.IsReadOnly"]/*' />
        /// <internalonly/>
        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.IsFixedSize"]/*' />
        /// <internalonly/>
        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.ICollection.CopyTo"]/*' />
        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index)
        {
            object[] values = GetValue();
            values.CopyTo(array, index);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.ICollection.IsSynchronized"]/*' />
        /// <internalonly/>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.ICollection.SyncRoot"]/*' />
        /// <internalonly/>
        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.this"]/*' />
        /// <internalonly/>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (string)value;
            }
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.Add"]/*' />
        /// <internalonly/>            
        int IList.Add(object value)
        {
            return Add((string)value);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.Contains"]/*' />
        /// <internalonly/>
        bool IList.Contains(object value)
        {
            return Contains((string)value);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.IndexOf"]/*' />
        /// <internalonly/>                           
        int IList.IndexOf(object value)
        {
            return IndexOf((string)value);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.Insert"]/*' />
        /// <internalonly/>
        void IList.Insert(int index, object value)
        {
            Insert(index, (string)value);
        }

        /// <include file='doc\SchemaNameCollection.uex' path='docs/doc[@for="SchemaNameCollection.IList.Remove"]/*' />
        /// <internalonly/>
        void IList.Remove(object value)
        {
            Remove((string)value);
        }

        internal delegate object VariantPropGetter();
        internal delegate void VariantPropSetter(object value);

        // this class and HintsDelegateWrapper exist only because you can't create
        // a delegate to a property's accessors. You have to supply methods. So these
        // classes wrap an object and supply properties as methods.
        internal class FilterDelegateWrapper
        {
            private UnsafeNativeMethods.IAdsContainer _obj;
            internal FilterDelegateWrapper(UnsafeNativeMethods.IAdsContainer wrapped)
            {
                _obj = wrapped;
            }
            public VariantPropGetter Getter
            {
                get
                {
                    return new VariantPropGetter(GetFilter);
                }
            }

            public VariantPropSetter Setter
            {
                get
                {
                    return new VariantPropSetter(SetFilter);
                }
            }

            private object GetFilter()
            {
                return _obj.Filter;
            }

            private void SetFilter(object value)
            {
                _obj.Filter = value;
            }
        }
    }
}

