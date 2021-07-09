// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading
{
    public static partial class EventWaitHandleAcl
    {
        public static System.Threading.EventWaitHandle Create(bool initialState, System.Threading.EventResetMode mode, string? name, out bool createdNew, System.Security.AccessControl.EventWaitHandleSecurity? eventSecurity) { throw null; }
        public static System.Threading.EventWaitHandle OpenExisting(string name, System.Security.AccessControl.EventWaitHandleRights rights) { throw null; }
        public static bool TryOpenExisting(string name, System.Security.AccessControl.EventWaitHandleRights rights, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Threading.EventWaitHandle? result) { throw null; }
    }
    public static partial class MutexAcl
    {
        public static System.Threading.Mutex Create(bool initiallyOwned, string? name, out bool createdNew, System.Security.AccessControl.MutexSecurity? mutexSecurity) { throw null; }
        public static System.Threading.Mutex OpenExisting(string name, System.Security.AccessControl.MutexRights rights) { throw null; }
        public static bool TryOpenExisting(string name, System.Security.AccessControl.MutexRights rights, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Threading.Mutex? result) { throw null; }
    }
    public static partial class SemaphoreAcl
    {
        public static System.Threading.Semaphore Create(int initialCount, int maximumCount, string? name, out bool createdNew, System.Security.AccessControl.SemaphoreSecurity? semaphoreSecurity) { throw null; }
        public static System.Threading.Semaphore OpenExisting(string name, System.Security.AccessControl.SemaphoreRights rights) { throw null; }
        public static bool TryOpenExisting(string name, System.Security.AccessControl.SemaphoreRights rights, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Threading.Semaphore? result) { throw null;  }
    }
    public static partial class ThreadingAclExtensions
    {
        public static System.Security.AccessControl.EventWaitHandleSecurity GetAccessControl(this System.Threading.EventWaitHandle handle) { throw null; }
        public static System.Security.AccessControl.MutexSecurity GetAccessControl(this System.Threading.Mutex mutex) { throw null; }
        public static System.Security.AccessControl.SemaphoreSecurity GetAccessControl(this System.Threading.Semaphore semaphore) { throw null; }
        public static void SetAccessControl(this System.Threading.EventWaitHandle handle, System.Security.AccessControl.EventWaitHandleSecurity eventSecurity) { }
        public static void SetAccessControl(this System.Threading.Mutex mutex, System.Security.AccessControl.MutexSecurity mutexSecurity) { }
        public static void SetAccessControl(this System.Threading.Semaphore semaphore, System.Security.AccessControl.SemaphoreSecurity semaphoreSecurity) { }
    }
}
