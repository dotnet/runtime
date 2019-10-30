// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Diagnostics
{
    public class EventLogPermissionEntryCollection : CollectionBase
    {
        internal EventLogPermissionEntryCollection() { }
        public EventLogPermissionEntry this[int index] { get { return null; } set { } }
        public int Add(EventLogPermissionEntry value) { return 0; }
        public void AddRange(EventLogPermissionEntryCollection value) { }
        public void AddRange(EventLogPermissionEntry[] value) { }
        public bool Contains(EventLogPermissionEntry value) { return false; }
        public void CopyTo(EventLogPermissionEntry[] array, int index) { }
        public int IndexOf(EventLogPermissionEntry value) { return 0; }
        public void Insert(int index, EventLogPermissionEntry value) { }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object value) { }
        protected override void OnRemove(int index, object value) { }
        protected override void OnSet(int index, object oldValue, object newValue) { }
        public void Remove(EventLogPermissionEntry value) { }
    }
}
