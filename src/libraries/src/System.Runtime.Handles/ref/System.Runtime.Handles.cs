// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.Win32.SafeHandles
{
    [System.Security.SecurityCriticalAttribute]
    public sealed partial class SafeWaitHandle : System.Runtime.InteropServices.SafeHandle
    {
        public SafeWaitHandle(System.IntPtr existingHandle, bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        [System.Security.SecurityCriticalAttribute]
        protected override bool ReleaseHandle() { return default(bool); }
    }
}
namespace System.IO
{
    public enum HandleInheritability
    {
        Inheritable = 1,
        None = 0,
    }
}
namespace System.Runtime.InteropServices
{
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class CriticalHandle : System.IDisposable
    {
        protected System.IntPtr handle;
        protected CriticalHandle(System.IntPtr invalidHandleValue) { }
        public bool IsClosed { get { return default(bool); } }
        public abstract bool IsInvalid { get; }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Dispose() { }
        [System.Security.SecurityCriticalAttribute]
        protected virtual void Dispose(bool disposing) { }
        protected abstract bool ReleaseHandle();
        protected void SetHandle(System.IntPtr handle) { }
        public void SetHandleAsInvalid() { }
    }
    [System.Security.SecurityCriticalAttribute]
    public abstract partial class SafeHandle : System.IDisposable
    {
        protected System.IntPtr handle;
        protected SafeHandle(System.IntPtr invalidHandleValue, bool ownsHandle) { }
        public bool IsClosed { get { return default(bool); } }
        public abstract bool IsInvalid { get; }
        [System.Security.SecurityCriticalAttribute]
        public void DangerousAddRef(ref bool success) { }
        public System.IntPtr DangerousGetHandle() { return default(System.IntPtr); }
        [System.Security.SecurityCriticalAttribute]
        public void DangerousRelease() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public void Dispose() { }
        [System.Security.SecurityCriticalAttribute]
        protected virtual void Dispose(bool disposing) { }
        protected abstract bool ReleaseHandle();
        protected void SetHandle(System.IntPtr handle) { }
        [System.Security.SecurityCriticalAttribute]
        public void SetHandleAsInvalid() { }
    }
}
namespace System.Threading
{
    public static partial class WaitHandleExtensions
    {
        [System.Security.SecurityCriticalAttribute]
        public static Microsoft.Win32.SafeHandles.SafeWaitHandle GetSafeWaitHandle(this System.Threading.WaitHandle waitHandle) { return default(Microsoft.Win32.SafeHandles.SafeWaitHandle); }
        [System.Security.SecurityCriticalAttribute]
        public static void SetSafeWaitHandle(this System.Threading.WaitHandle waitHandle, Microsoft.Win32.SafeHandles.SafeWaitHandle value) { }
    }
}
