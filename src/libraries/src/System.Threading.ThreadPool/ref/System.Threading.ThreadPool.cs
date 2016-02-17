// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Threading
{
    public sealed partial class RegisteredWaitHandle
    {
        internal RegisteredWaitHandle() { }
        public bool Unregister(System.Threading.WaitHandle waitObject) { return default(bool); }
    }
    public static partial class ThreadPool
    {
        [System.Security.SecurityCriticalAttribute]
        public static bool BindHandle(System.Runtime.InteropServices.SafeHandle osHandle) { return default(bool); }
        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack) { return default(bool); }
        public static bool QueueUserWorkItem(System.Threading.WaitCallback callBack, object state) { return default(bool); }
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, int millisecondsTimeOutInterval, bool executeOnlyOnce) { return default(System.Threading.RegisteredWaitHandle); }
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, long millisecondsTimeOutInterval, bool executeOnlyOnce) { return default(System.Threading.RegisteredWaitHandle); }
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, System.TimeSpan timeout, bool executeOnlyOnce) { return default(System.Threading.RegisteredWaitHandle); }
        [System.CLSCompliantAttribute(false)]
        public static System.Threading.RegisteredWaitHandle RegisterWaitForSingleObject(System.Threading.WaitHandle waitObject, System.Threading.WaitOrTimerCallback callBack, object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce) { return default(System.Threading.RegisteredWaitHandle); }
    }
    public delegate void WaitCallback(object state);
    public delegate void WaitOrTimerCallback(object state, bool timedOut);
}
