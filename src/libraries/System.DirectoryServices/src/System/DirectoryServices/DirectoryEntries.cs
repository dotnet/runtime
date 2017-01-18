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

    /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries"]/*' />
    /// <devdoc>
    ///    <para>Contains the children (child entries) of an entry in the Active Directory.</para>
    /// </devdoc>    
    public class DirectoryEntries : IEnumerable
    {
        // the parent of the children in this collection
        private DirectoryEntry _container;

        internal DirectoryEntries(DirectoryEntry parent)
        {
            _container = parent;
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.SchemaFilter"]/*' />
        /// <devdoc>
        ///    <para>Gets the schemas that specify which children are shown.</para>
        /// </devdoc>
        public SchemaNameCollection SchemaFilter
        {
            get
            {
                CheckIsContainer();
                SchemaNameCollection.FilterDelegateWrapper filter = new SchemaNameCollection.FilterDelegateWrapper(_container.ContainerObject);
                return new SchemaNameCollection(filter.Getter, filter.Setter);
            }
        }

        private void CheckIsContainer()
        {
            if (!_container.IsContainer)
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, SR.DSNotAContainer , _container.Path));
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.Add"]/*' />
        /// <devdoc>
        ///    <para>Creates a request to create a new entry in the container.</para>
        /// </devdoc>                
        public DirectoryEntry Add(string name, string schemaClassName)
        {
            CheckIsContainer();
            object newChild = _container.ContainerObject.Create(schemaClassName, name);
            DirectoryEntry entry = new DirectoryEntry(newChild, _container.UsePropertyCache, _container.GetUsername(), _container.GetPassword(), _container.AuthenticationType);
            entry.JustCreated = true;       // suspend writing changes until CommitChanges() is called
            return entry;
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.Find"]/*' />
        /// <devdoc>
        ///    <para>Returns the child with the given name.</para>
        /// </devdoc>                
        public DirectoryEntry Find(string name)
        {
            // For IIS: and WinNT: providers  schemaClassName == "" does general search.
            return Find(name, null);
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.Find1"]/*' />
        /// <devdoc>
        ///    <para>Returns the child with the given name and of the given type.</para>
        /// </devdoc>                
        public DirectoryEntry Find(string name, string schemaClassName)
        {
            CheckIsContainer();
            // Note: schemaClassName == null does not work for IIS: provider.
            object o = null;
            try
            {
                o = _container.ContainerObject.GetObject(schemaClassName, name);
            }
            catch (COMException e)
            {
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
            return new DirectoryEntry(o, _container.UsePropertyCache, _container.GetUsername(), _container.GetPassword(), _container.AuthenticationType);
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.Remove"]/*' />
        /// <devdoc>
        /// <para>Deletes a child <see cref='System.DirectoryServices.DirectoryEntry'/> from this collection.</para>
        /// </devdoc>                
        public void Remove(DirectoryEntry entry)
        {
            CheckIsContainer();
            try
            {
                _container.ContainerObject.Delete(entry.SchemaClassName, entry.Name);
            }
            catch (COMException e)
            {
                throw COMExceptionHelper.CreateFormattedComException(e);
            }
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.GetEnumerator"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>        
        public IEnumerator GetEnumerator()
        {
            return new ChildEnumerator(_container);
        }

        /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.ChildEnumerator"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Supports a simple ForEach-style iteration over a collection and defines
        ///       enumerators, size, and synchronization methods.
        ///    </para>
        /// </devdoc>
        private class ChildEnumerator : IEnumerator
        {
            private DirectoryEntry _container;
            private SafeNativeMethods.EnumVariant _enumVariant;
            private DirectoryEntry _currentEntry;

            internal ChildEnumerator(DirectoryEntry container)
            {
                _container = container;
                if (container.IsContainer)
                {
                    _enumVariant = new SafeNativeMethods.EnumVariant((SafeNativeMethods.IEnumVariant)container.ContainerObject._NewEnum);
                }
            }

            /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.ChildEnumerator.Current"]/*' />
            /// <devdoc>
            ///    <para>Gets the current element in the collection.</para>
            /// </devdoc>
            public DirectoryEntry Current
            {
                get
                {
                    if (_enumVariant == null)
                        throw new InvalidOperationException(SR.DSNoCurrentChild);

                    if (_currentEntry == null)
                        _currentEntry = new DirectoryEntry(_enumVariant.GetValue(), _container.UsePropertyCache, _container.GetUsername(), _container.GetPassword(), _container.AuthenticationType);

                    return _currentEntry;
                }
            }

            /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.ChildEnumerator.MoveNext"]/*' />
            /// <devdoc>
            ///    <para>Advances
            ///       the enumerator to the next element of the collection
            ///       and returns a Boolean value indicating whether a valid element is available.</para>
            /// </devdoc>                        
            public bool MoveNext()
            {
                if (_enumVariant == null)
                    return false;

                _currentEntry = null;
                return _enumVariant.GetNext();
            }

            /// <include file='doc\DirectoryEntries.uex' path='docs/doc[@for="DirectoryEntries.ChildEnumerator.Reset"]/*' />
            /// <devdoc>
            ///    <para>Resets the enumerator back to its initial position before the first element in the collection.</para>
            /// </devdoc>
            public void Reset()
            {
                if (_enumVariant != null)
                {
                    try
                    {
                        _enumVariant.Reset();
                    }
                    catch (NotImplementedException)
                    {
                        //Some providers might not implement Reset, workaround the problem.
                        _enumVariant = new SafeNativeMethods.EnumVariant((SafeNativeMethods.IEnumVariant)_container.ContainerObject._NewEnum);
                    }
                    _currentEntry = null;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }
    }
}

