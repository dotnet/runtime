// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    public sealed partial class ProcessStartInfo
    {
        public string PasswordInClearText { get { return default(string); } set { } }
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
