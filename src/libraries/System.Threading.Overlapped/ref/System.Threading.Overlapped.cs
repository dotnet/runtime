// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading
{
    [System.CLSCompliantAttribute(false)]
    public unsafe delegate void IOCompletionCallback(uint errorCode, uint numBytes, System.Threading.NativeOverlapped* pOVERLAP);
    public partial struct NativeOverlapped
    {
        public System.IntPtr EventHandle;
        public System.IntPtr InternalHigh;
        public System.IntPtr InternalLow;
        public int OffsetHigh;
        public int OffsetLow;
    }
    public partial class Overlapped
    {
        public Overlapped() { }
        [System.ObsoleteAttribute("This constructor is not 64-bit compatible.  Use the constructor that takes an IntPtr for the event handle.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public Overlapped(int offsetLo, int offsetHi, int hEvent, System.IAsyncResult ar) { }
        public Overlapped(int offsetLo, int offsetHi, System.IntPtr hEvent, System.IAsyncResult ar) { }
        public System.IAsyncResult AsyncResult { get { throw null; } set { } }
        [System.ObsoleteAttribute("This property is not 64-bit compatible.  Use EventHandleIntPtr instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public int EventHandle { get { throw null; } set { } }
        public System.IntPtr EventHandleIntPtr { get { throw null; } set { } }
        public int OffsetHigh { get { throw null; } set { } }
        public int OffsetLow { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        public unsafe static void Free(System.Threading.NativeOverlapped* nativeOverlappedPtr) { }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("This method is not safe.  Use Pack (iocb, userData) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public unsafe System.Threading.NativeOverlapped* Pack(System.Threading.IOCompletionCallback iocb) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe System.Threading.NativeOverlapped* Pack(System.Threading.IOCompletionCallback iocb, object userData) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe static System.Threading.Overlapped Unpack(System.Threading.NativeOverlapped* nativeOverlappedPtr) { throw null; }
        [System.CLSCompliantAttribute(false)]
        [System.ObsoleteAttribute("This method is not safe.  Use UnsafePack (iocb, userData) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public unsafe System.Threading.NativeOverlapped* UnsafePack(System.Threading.IOCompletionCallback iocb) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe System.Threading.NativeOverlapped* UnsafePack(System.Threading.IOCompletionCallback iocb, object userData) { throw null; }
    }
    public sealed partial class PreAllocatedOverlapped : System.IDisposable
    {
        [System.CLSCompliantAttribute(false)]
        public PreAllocatedOverlapped(System.Threading.IOCompletionCallback callback, object state, object pinData) { }
        public void Dispose() { }
    }
    public sealed partial class ThreadPoolBoundHandle : System.IDisposable
    {
        internal ThreadPoolBoundHandle() { }
        public System.Runtime.InteropServices.SafeHandle Handle { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe System.Threading.NativeOverlapped* AllocateNativeOverlapped(System.Threading.IOCompletionCallback callback, object state, object pinData) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe System.Threading.NativeOverlapped* AllocateNativeOverlapped(System.Threading.PreAllocatedOverlapped preAllocated) { throw null; }
        public static System.Threading.ThreadPoolBoundHandle BindHandle(System.Runtime.InteropServices.SafeHandle handle) { throw null; }
        public void Dispose() { }
        [System.CLSCompliantAttribute(false)]
        public unsafe void FreeNativeOverlapped(System.Threading.NativeOverlapped* overlapped) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe static object GetNativeOverlappedState(System.Threading.NativeOverlapped* overlapped) { throw null; }
    }
}
