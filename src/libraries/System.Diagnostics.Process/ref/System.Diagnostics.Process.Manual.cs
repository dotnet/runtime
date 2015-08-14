// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class Process : System.IDisposable
    {
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public partial class ProcessModuleCollection : System.Collections.ICollection
    {
        public int Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public partial class ProcessThreadCollection : System.Collections.ICollection
    {
        public int Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
}
namespace Microsoft.Win32.SafeHandles
{
    // Members from SafeHandleMinusOneOrZeroIsInvalid needed after removing base
    [System.Security.SecurityCritical]
    public sealed partial class SafeProcessHandle : System.Runtime.InteropServices.SafeHandle
    {
        public override bool IsInvalid {[System.Security.SecurityCritical] get { return false; } }
    }
}
