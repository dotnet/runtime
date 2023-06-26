// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static partial class CancelablePromise
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
            JSHostImplementation.TaskCallback? holder = promise.AsyncState as JSHostImplementation.TaskCallback;
            if (holder == null) throw new InvalidOperationException("Expected Task converted from JS Promise");


#if FEATURE_WASM_THREADS
            holder.SynchronizationContext!.Send(static (JSHostImplementation.TaskCallback holder) =>
            {
#endif
            _CancelPromise(holder.GCHandle);
#if FEATURE_WASM_THREADS
            }, holder);
#endif
        }

        public static void CancelPromise<T1, T2>(Task promise, Action<T1, T2> callback, T1 state1, T2 state2)
        {
            // this check makes sure that promiseGCHandle is still valid handle
            if (promise.IsCompleted)
            {
                return;
            }
            JSHostImplementation.TaskCallback? holder = promise.AsyncState as JSHostImplementation.TaskCallback;
            if (holder == null) throw new InvalidOperationException("Expected Task converted from JS Promise");


#if FEATURE_WASM_THREADS
            holder.SynchronizationContext!.Send((JSHostImplementation.TaskCallback holder) =>
            {
#endif
                _CancelPromise(holder.GCHandle);
                callback.Invoke(state1, state2);
#if FEATURE_WASM_THREADS
            }, holder);
#endif
        }
    }
}
