// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.Add(System.Object,System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.set_Item(System.Int32,System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.set_Item(System.Object,System.Object):System.Void")]

/*
 This class has the HostProtectionAttribute. The purpose of this attribute is to enforce host-specific programming model guidelines, not security behavior. 
 Suppress FxCop message - BUT REVISIT IF ADDING NEW SECURITY ATTRIBUTES.
*/
[assembly: SuppressMessage("Microsoft.Security", "CA2112:SecuredTypesShouldNotExposeFields", Scope = "type", Target = "System.ComponentModel.PropertyDescriptorCollection")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.get_IsFixedSize():System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IEnumerable.GetEnumerator():System.Collections.IEnumerator")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.get_IsFixedSize():System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.ICollection.get_SyncRoot():System.Object")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.Remove(System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.ICollection.get_IsSynchronized():System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.get_IsReadOnly():System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.get_Keys():System.Collections.ICollection")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.get_IsReadOnly():System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.Clear():System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.IndexOf(System.Object):System.Int32")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.Contains(System.Object):System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.RemoveAt(System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.GetEnumerator():System.Collections.IEnumerator")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.Add(System.Object,System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.Clear():System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.get_Values():System.Collections.ICollection")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.Contains(System.Object):System.Boolean")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.GetEnumerator():System.Collections.IDictionaryEnumerator")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.Remove(System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.get_Item(System.Int32):System.Object")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.set_Item(System.Int32,System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.Add(System.Object):System.Int32")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.CopyTo(System.Array,System.Int32):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.get_Item(System.Object):System.Object")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IDictionary.set_Item(System.Object,System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.IList.Insert(System.Int32,System.Object):System.Void")]
[assembly: SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.System.Collections.ICollection.get_Count():System.Int32")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection..ctor(System.ComponentModel.PropertyDescriptor[])")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.get_Count():System.Int32")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.get_Item(System.String):System.ComponentModel.PropertyDescriptor")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection..ctor(System.ComponentModel.PropertyDescriptor[],System.Boolean)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.GetEnumerator():System.Collections.IEnumerator")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.Find(System.String,System.Boolean):System.ComponentModel.PropertyDescriptor")]
[assembly: SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Scope = "member", Target = "System.ComponentModel.PropertyDescriptorCollection.Sort(System.String[]):System.ComponentModel.PropertyDescriptorCollection")]

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>
    ///       Represents a collection of properties.
    ///    </para>
    /// </devdoc>
    [System.Security.Permissions.HostProtection(Synchronization = true)]
    public class PropertyDescriptorCollection : ICollection, IList, IDictionary
    {
        /// <devdoc>
        /// An empty PropertyDescriptorCollection that can used instead of creating a new one with no items.
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2112:SecuredTypesShouldNotExposeFields")] // ReadOnly fields - already shipped.
        public static readonly PropertyDescriptorCollection Empty = new PropertyDescriptorCollection(null, true);

        private IDictionary _cachedFoundProperties;
        private bool _cachedIgnoreCase;
        private PropertyDescriptor[] _properties;
        private int _propCount = 0;
        private string[] _namedSort;
        private IComparer _comparer;
        private bool _propsOwned = true;
        private bool _needSort = false;
        private bool _readOnly = false;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.PropertyDescriptorCollection'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public PropertyDescriptorCollection(PropertyDescriptor[] properties)
        {
            _properties = properties;
            if (properties == null)
            {
                _properties = new PropertyDescriptor[0];
                _propCount = 0;
            }
            else
            {
                _propCount = properties.Length;
            }
            _propsOwned = true;
        }

        /// <devdoc>
        ///     Initializes a new instance of a property descriptor collection, and allows you to mark the
        ///     collection as read-only so it cannot be modified.
        /// </devdoc>
        public PropertyDescriptorCollection(PropertyDescriptor[] properties, bool readOnly) : this(properties)
        {
            _readOnly = readOnly;
        }

        private PropertyDescriptorCollection(PropertyDescriptor[] properties, int propCount, string[] namedSort, IComparer comparer)
        {
            _propsOwned = false;
            if (namedSort != null)
            {
                _namedSort = (string[])namedSort.Clone();
            }
            _comparer = comparer;
            _properties = properties;
            _propCount = propCount;
            _needSort = true;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the number
        ///       of property descriptors in the
        ///       collection.
        ///    </para>
        /// </devdoc>
        public int Count
        {
            get
            {
                return _propCount;
            }
        }

        /// <devdoc>
        ///    <para>Gets the property with the specified index
        ///       number.</para>
        /// </devdoc>
        public virtual PropertyDescriptor this[int index]
        {
            get
            {
                if (index >= _propCount)
                {
                    throw new IndexOutOfRangeException();
                }
                EnsurePropsOwned();
                return _properties[index];
            }
        }

        /// <devdoc>
        ///    <para>Gets the property with the specified name.</para>
        /// </devdoc>
        public virtual PropertyDescriptor this[string name]
        {
            get
            {
                return Find(name, false);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(PropertyDescriptor value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            EnsureSize(_propCount + 1);
            _properties[_propCount++] = value;
            return _propCount - 1;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Clear()
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            _propCount = 0;
            _cachedFoundProperties = null;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(PropertyDescriptor value)
        {
            return IndexOf(value) >= 0;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(Array array, int index)
        {
            EnsurePropsOwned();
            Array.Copy(_properties, 0, array, index, Count);
        }

        private void EnsurePropsOwned()
        {
            if (!_propsOwned)
            {
                _propsOwned = true;
                if (_properties != null)
                {
                    PropertyDescriptor[] newProps = new PropertyDescriptor[Count];
                    Array.Copy(_properties, 0, newProps, 0, Count);
                    _properties = newProps;
                }
            }

            if (_needSort)
            {
                _needSort = false;
                InternalSort(_namedSort);
            }
        }

        private void EnsureSize(int sizeNeeded)
        {
            if (sizeNeeded <= _properties.Length)
            {
                return;
            }

            if (_properties == null || _properties.Length == 0)
            {
                _propCount = 0;
                _properties = new PropertyDescriptor[sizeNeeded];
                return;
            }

            EnsurePropsOwned();

            int newSize = Math.Max(sizeNeeded, _properties.Length * 2);
            PropertyDescriptor[] newProps = new PropertyDescriptor[newSize];
            Array.Copy(_properties, 0, newProps, 0, _propCount);
            _properties = newProps;
        }

        /// <devdoc>
        ///    <para>Gets the description of the property with the specified
        ///       name.</para>
        /// </devdoc>
        public virtual PropertyDescriptor Find(string name, bool ignoreCase)
        {
            lock (this)
            {
                PropertyDescriptor p = null;

                if (_cachedFoundProperties == null || _cachedIgnoreCase != ignoreCase)
                {
                    _cachedIgnoreCase = ignoreCase;
                    _cachedFoundProperties = new HybridDictionary(ignoreCase);
                }

                // first try to find it in the cache
                //
                object cached = _cachedFoundProperties[name];

                if (cached != null)
                {
                    return (PropertyDescriptor)cached;
                }

                // Now start walking from where we last left off, filling
                // the cache as we go.
                //
                for (int i = 0; i < _propCount; i++)
                {
                    if (ignoreCase)
                    {
                        if (String.Equals(_properties[i].Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            _cachedFoundProperties[name] = _properties[i];
                            p = _properties[i];
                            break;
                        }
                    }
                    else
                    {
                        if (_properties[i].Name.Equals(name))
                        {
                            _cachedFoundProperties[name] = _properties[i];
                            p = _properties[i];
                            break;
                        }
                    }
                }

                return p;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(PropertyDescriptor value)
        {
            return Array.IndexOf(_properties, value, 0, _propCount);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, PropertyDescriptor value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            EnsureSize(_propCount + 1);
            if (index < _propCount)
            {
                Array.Copy(_properties, index, _properties, index + 1, _propCount - index);
            }
            _properties[index] = value;
            _propCount++;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(PropertyDescriptor value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            int index = IndexOf(value);

            if (index != -1)
            {
                RemoveAt(index);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void RemoveAt(int index)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            if (index < _propCount - 1)
            {
                Array.Copy(_properties, index + 1, _properties, index, _propCount - index - 1);
            }
            _properties[_propCount - 1] = null;
            _propCount--;
        }

        /// <devdoc>
        ///    <para>
        ///       Sorts the members of this PropertyDescriptorCollection, using the default sort for this collection, 
        ///       which is usually alphabetical.
        ///    </para>
        /// </devdoc>
        public virtual PropertyDescriptorCollection Sort()
        {
            return new PropertyDescriptorCollection(_properties, _propCount, _namedSort, _comparer);
        }


        /// <devdoc>
        ///    <para>
        ///       Sorts the members of this PropertyDescriptorCollection.  Any specified NamedSort arguments will 
        ///       be applied first, followed by sort using the specified IComparer.
        ///    </para>
        /// </devdoc>
        public virtual PropertyDescriptorCollection Sort(string[] names)
        {
            return new PropertyDescriptorCollection(_properties, _propCount, names, _comparer);
        }

        /// <devdoc>
        ///    <para>
        ///       Sorts the members of this PropertyDescriptorCollection.  Any specified NamedSort arguments will 
        ///       be applied first, followed by sort using the specified IComparer.
        ///    </para>
        /// </devdoc>
        public virtual PropertyDescriptorCollection Sort(string[] names, IComparer comparer)
        {
            return new PropertyDescriptorCollection(_properties, _propCount, names, comparer);
        }

        /// <devdoc>
        ///    <para>
        ///       Sorts the members of this PropertyDescriptorCollection, using the specified IComparer to compare, 
        ///       the PropertyDescriptors contained in the collection.
        ///    </para>
        /// </devdoc>
        public virtual PropertyDescriptorCollection Sort(IComparer comparer)
        {
            return new PropertyDescriptorCollection(_properties, _propCount, _namedSort, comparer);
        }

        /// <devdoc>
        ///    <para>
        ///       Sorts the members of this PropertyDescriptorCollection.  Any specified NamedSort arguments will 
        ///       be applied first, followed by sort using the specified IComparer.
        ///    </para>
        /// </devdoc>
        protected void InternalSort(string[] names)
        {
            if (_properties == null || _properties.Length == 0)
            {
                return;
            }

            this.InternalSort(_comparer);

            if (names != null && names.Length > 0)
            {
                ArrayList propArrayList = new ArrayList(_properties);
                int foundCount = 0;
                int propCount = _properties.Length;

                for (int i = 0; i < names.Length; i++)
                {
                    for (int j = 0; j < propCount; j++)
                    {
                        PropertyDescriptor currentProp = (PropertyDescriptor)propArrayList[j];

                        // Found a matching property.  Here, we add it to our array.  We also
                        // mark it as null in our array list so we don't add it twice later.
                        //
                        if (currentProp != null && currentProp.Name.Equals(names[i]))
                        {
                            _properties[foundCount++] = currentProp;
                            propArrayList[j] = null;
                            break;
                        }
                    }
                }

                // At this point we have filled in the first "foundCount" number of propeties, one for each
                // name in our name array.  If a name didn't match, then it is ignored.  Next, we must fill
                // in the rest of the properties.  We now have a sparse array containing the remainder, so
                // it's easy.
                //
                for (int i = 0; i < propCount; i++)
                {
                    if (propArrayList[i] != null)
                    {
                        _properties[foundCount++] = (PropertyDescriptor)propArrayList[i];
                    }
                }

                Debug.Assert(foundCount == propCount, "We did not completely fill our property array");
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Sorts the members of this PropertyDescriptorCollection using the specified IComparer.
        ///    </para>
        /// </devdoc>
        protected void InternalSort(IComparer sorter)
        {
            if (sorter == null)
            {
                TypeDescriptor.SortDescriptorArray(this);
            }
            else
            {
                Array.Sort(_properties, sorter);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets an enumerator for this <see cref='System.ComponentModel.PropertyDescriptorCollection'/>.
        ///    </para>
        /// </devdoc>
        public virtual IEnumerator GetEnumerator()
        {
            EnsurePropsOwned();
            // we can only return an enumerator on the props we actually have...
            if (_properties.Length != _propCount)
            {
                PropertyDescriptor[] enumProps = new PropertyDescriptor[_propCount];
                Array.Copy(_properties, 0, enumProps, 0, _propCount);
                return enumProps.GetEnumerator();
            }
            return _properties.GetEnumerator();
        }

        /// <internalonly/>
        int ICollection.Count
        {
            get
            {
                return Count;
            }
        }

        /// <internalonly/>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <internalonly/>
        object ICollection.SyncRoot
        {
            get
            {
                return null;
            }
        }

        /// <internalonly/>
        void IDictionary.Add(object key, object value)
        {
            PropertyDescriptor newProp = value as PropertyDescriptor;

            if (newProp == null)
            {
                throw new ArgumentException("value");
            }
            Add(newProp);
        }

        /// <internalonly/>
        void IDictionary.Clear()
        {
            Clear();
        }

        /// <internalonly/>
        bool IDictionary.Contains(object key)
        {
            if (key is string)
            {
                return this[(string)key] != null;
            }
            return false;
        }

        /// <internalonly/>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new PropertyDescriptorEnumerator(this);
        }

        /// <internalonly/>
        bool IDictionary.IsFixedSize
        {
            get
            {
                return _readOnly;
            }
        }

        /// <internalonly/>
        bool IDictionary.IsReadOnly
        {
            get
            {
                return _readOnly;
            }
        }

        /// <internalonly/>
        object IDictionary.this[object key]
        {
            get
            {
                if (key is string)
                {
                    return this[(string)key];
                }
                return null;
            }

            set
            {
                if (_readOnly)
                {
                    throw new NotSupportedException();
                }

                if (value != null && !(value is PropertyDescriptor))
                {
                    throw new ArgumentException("value");
                }

                int index = -1;

                if (key is int)
                {
                    index = (int)key;

                    if (index < 0 || index >= _propCount)
                    {
                        throw new IndexOutOfRangeException();
                    }
                }
                else if (key is string)
                {
                    for (int i = 0; i < _propCount; i++)
                    {
                        if (_properties[i].Name.Equals((string)key))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("key");
                }

                if (index == -1)
                {
                    Add((PropertyDescriptor)value);
                }
                else
                {
                    EnsurePropsOwned();
                    _properties[index] = (PropertyDescriptor)value;
                    if (_cachedFoundProperties != null && key is string)
                    {
                        _cachedFoundProperties[key] = value;
                    }
                }
            }
        }

        /// <internalonly/>
        ICollection IDictionary.Keys
        {
            get
            {
                string[] keys = new string[_propCount];
                for (int i = 0; i < _propCount; i++)
                {
                    keys[i] = _properties[i].Name;
                }
                return keys;
            }
        }

        /// <internalonly/>
        ICollection IDictionary.Values
        {
            get
            {
                // we can only return an enumerator on the props we actually have...
                //
                if (_properties.Length != _propCount)
                {
                    PropertyDescriptor[] newProps = new PropertyDescriptor[_propCount];
                    Array.Copy(_properties, 0, newProps, 0, _propCount);
                    return newProps;
                }
                else
                {
                    return (ICollection)_properties.Clone();
                }
            }
        }

        /// <internalonly/>
        void IDictionary.Remove(object key)
        {
            if (key is string)
            {
                PropertyDescriptor pd = this[(string)key];
                if (pd != null)
                {
                    ((IList)this).Remove(pd);
                }
            }
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <internalonly/>
        int IList.Add(object value)
        {
            return Add((PropertyDescriptor)value);
        }

        /// <internalonly/>
        void IList.Clear()
        {
            Clear();
        }

        /// <internalonly/>
        bool IList.Contains(object value)
        {
            return Contains((PropertyDescriptor)value);
        }

        /// <internalonly/>
        int IList.IndexOf(object value)
        {
            return IndexOf((PropertyDescriptor)value);
        }

        /// <internalonly/>
        void IList.Insert(int index, object value)
        {
            Insert(index, (PropertyDescriptor)value);
        }

        /// <internalonly/>
        bool IList.IsReadOnly
        {
            get
            {
                return _readOnly;
            }
        }

        /// <internalonly/>
        bool IList.IsFixedSize
        {
            get
            {
                return _readOnly;
            }
        }

        /// <internalonly/>
        void IList.Remove(object value)
        {
            Remove((PropertyDescriptor)value);
        }

        /// <internalonly/>
        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        /// <internalonly/>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (_readOnly)
                {
                    throw new NotSupportedException();
                }

                if (index >= _propCount)
                {
                    throw new IndexOutOfRangeException();
                }


                if (value != null && !(value is PropertyDescriptor))
                {
                    throw new ArgumentException("value");
                }

                EnsurePropsOwned();
                _properties[index] = (PropertyDescriptor)value;
            }
        }

        private class PropertyDescriptorEnumerator : IDictionaryEnumerator
        {
            private PropertyDescriptorCollection _owner;
            private int _index = -1;

            public PropertyDescriptorEnumerator(PropertyDescriptorCollection owner)
            {
                _owner = owner;
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    PropertyDescriptor curProp = _owner[_index];
                    return new DictionaryEntry(curProp.Name, curProp);
                }
            }

            public object Key
            {
                get
                {
                    return _owner[_index].Name;
                }
            }

            public object Value
            {
                get
                {
                    return _owner[_index].Name;
                }
            }

            public bool MoveNext()
            {
                if (_index < (_owner.Count - 1))
                {
                    _index++;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}

