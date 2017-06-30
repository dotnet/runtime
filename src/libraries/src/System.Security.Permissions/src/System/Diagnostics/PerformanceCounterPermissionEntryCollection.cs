// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Diagnostics
{
    public class PerformanceCounterPermissionEntryCollection : CollectionBase
    {
        internal PerformanceCounterPermissionEntryCollection() { }
        public PerformanceCounterPermissionEntry this[int index] { get { return null; } set { } }
        public int Add(PerformanceCounterPermissionEntry value) { return 0; }
        public void AddRange(PerformanceCounterPermissionEntryCollection value) { }
        public void AddRange(PerformanceCounterPermissionEntry[] value) { }
        public bool Contains(PerformanceCounterPermissionEntry value) { return false; }
        public void CopyTo(PerformanceCounterPermissionEntry[] array, int index) { }
        public int IndexOf(PerformanceCounterPermissionEntry value) { return 0; }
        public void Insert(int index, PerformanceCounterPermissionEntry value) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(PerformanceCounterPermissionEntry value) { }
    }
}
