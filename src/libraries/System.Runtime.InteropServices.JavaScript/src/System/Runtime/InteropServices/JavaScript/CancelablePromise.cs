// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class CancelablePromise
    {
        [JSImport("INTERNAL.mono_wasm_cancel_promise")]
        private static partial void _CancelPromise(IntPtr promiseGCHandle);

        public static void CancelPromise(Task promise)
        {
            // this check makes sure that promiseGCHandle is still valid handle
            if (promise.IsCompleted)
            {
                return;
            }
            GCHandle? promiseGCHandle = promise.AsyncState as GCHandle?;
            if (promiseGCHandle == null) throw new InvalidOperationException("Expected Task converted from JS Promise");

#if FEATURE_WASM_THREADS
            // TODO JSObject.AssertThreadAffinity(promise);
            // in order to remember the thread ID of the promise, we would have to allocate holder object for any Task,
            // which would hold thread ID and the GCHandle
            // that would be pretty expensive, so we don't do it for now
            // the consequences are that calling CancelPromise on wrong thread would do nothing
            // because there would not be any object on JS registered under the same GCHandle
            // perhaps that's the point when we could throw an exception on JS side.
#endif
            _CancelPromise((IntPtr)promiseGCHandle.Value);
        }
    }
}
