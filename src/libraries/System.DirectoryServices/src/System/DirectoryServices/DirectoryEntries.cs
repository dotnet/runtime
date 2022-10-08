// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections;
using System.DirectoryServices.Interop;

namespace System.DirectoryServices
{
    /// <devdoc>
    /// Contains the children (child entries) of an entry in the Active Directory.
    /// </devdoc>
    public class DirectoryEntries : IEnumerable
    {
        // the parent of the children in this collection
        private readonly DirectoryEntry _container;

        internal DirectoryEntries(DirectoryEntry parent)
        {
            _container = parent;
        }

        /// <devdoc>
        /// Gets the schemas that specify which children are shown.
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
                throw new InvalidOperationException(SR.Format(SR.DSNotAContainer, _container.Path));
        }

        /// <devdoc>
        /// Creates a request to create a new entry in the container.
        /// </devdoc>
        public DirectoryEntry Add(string name, string schemaClassName)
        {
            CheckIsContainer();
            object newChild = _container.ContainerObject.Create(schemaClassName, name);
            DirectoryEntry entry = new DirectoryEntry(newChild, _container.UsePropertyCache, _container.GetUsername(), _container.GetPassword(), _container.AuthenticationType);
            entry.JustCreated = true;       // suspend writing changes until CommitChanges() is called
            return entry;
        }

        /// <devdoc>
        /// Returns the child with the given name.
        /// </devdoc>
        public DirectoryEntry Find(string name)
        {
            // For IIS: and WinNT: providers  schemaClassName == "" does general search.
            return Find(name, null);
        }

        /// <devdoc>
        /// Returns the child with the given name and of the given type.
        /// </devdoc>
        public DirectoryEntry Find(string name, string? schemaClassName)
        {
            CheckIsContainer();
            // Note: schemaClassName == null does not work for IIS: provider.
            object? o = null;
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

        /// <devdoc>
        /// Deletes a child <see cref='System.DirectoryServices.DirectoryEntry'/> from this collection.
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

        public IEnumerator GetEnumerator() => new ChildEnumerator(_container);

        /// <devdoc>
        /// Supports a simple ForEach-style iteration over a collection and defines
        /// enumerators, size, and synchronization methods.
        /// </devdoc>
        private sealed class ChildEnumerator : IEnumerator
        {
            private readonly DirectoryEntry _container;
            private SafeNativeMethods.EnumVariant? _enumVariant;
            private DirectoryEntry? _currentEntry;

            internal ChildEnumerator(DirectoryEntry container)
            {
                _container = container;
                if (container.IsContainer)
                {
                    _enumVariant = new SafeNativeMethods.EnumVariant((SafeNativeMethods.IEnumVariant)container.ContainerObject._NewEnum);
                }
            }

            /// <devdoc>
            /// Gets the current element in the collection.
            /// </devdoc>
            public DirectoryEntry Current
            {
                get
                {
                    if (_enumVariant == null)
                        throw new InvalidOperationException(SR.DSNoCurrentChild);

                    return _currentEntry ??= new DirectoryEntry(_enumVariant.GetValue(), _container.UsePropertyCache, _container.GetUsername(), _container.GetPassword(), _container.AuthenticationType);
                }
            }

            /// <devdoc>
            /// Advances the enumerator to the next element of the collection
            /// and returns a Boolean value indicating whether a valid element is available.
            /// </devdoc>
            public bool MoveNext()
            {
                if (_enumVariant == null)
                    return false;

                _currentEntry = null;
                return _enumVariant.GetNext();
            }

            /// <devdoc>
            /// Resets the enumerator back to its initial position before the first element in the collection.
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

            object IEnumerator.Current => Current;
        }
    }
}
