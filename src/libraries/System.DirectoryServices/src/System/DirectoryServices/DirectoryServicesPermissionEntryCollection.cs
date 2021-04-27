// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.DirectoryServices
{
    public class DirectoryServicesPermissionEntryCollection : CollectionBase
    {
        internal DirectoryServicesPermissionEntryCollection() { }
        public DirectoryServicesPermissionEntry? this[int index] { get { return null; } set { } }
        public int Add(DirectoryServicesPermissionEntry value) { return 0; }
        public void AddRange(DirectoryServicesPermissionEntryCollection value) { }
        public void AddRange(DirectoryServicesPermissionEntry[] value) { }
        public bool Contains(DirectoryServicesPermissionEntry value) { return false; }
        public void CopyTo(DirectoryServicesPermissionEntry[] array, int index) { }
        public int IndexOf(DirectoryServicesPermissionEntry value) { return 0; }
        public void Insert(int index, DirectoryServicesPermissionEntry value) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object? value) { }
        protected override void OnRemove(int index, object? value) { }
        protected override void OnSet(int index, object? oldValue, object? newValue) { }
        public void Remove(DirectoryServicesPermissionEntry? value) { }
    }
}
