// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ServiceProcess
{
    public class ServiceControllerPermissionEntryCollection : CollectionBase
    {
        internal ServiceControllerPermissionEntryCollection() { }
        public ServiceControllerPermissionEntry this[int index] { get { return null; } set { } }
        public int Add(ServiceControllerPermissionEntry value) { return 0; }
        public void AddRange(ServiceControllerPermissionEntry[] value) { }
        public void AddRange(ServiceControllerPermissionEntryCollection value) { }
        public bool Contains(ServiceControllerPermissionEntry value) { return false; }
        public void CopyTo(ServiceControllerPermissionEntry[] array, int index) { }
        public int IndexOf(ServiceControllerPermissionEntry value) { return 0; }
        public void Insert(int index, ServiceControllerPermissionEntry value) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(ServiceControllerPermissionEntry value) { }
    }
}
