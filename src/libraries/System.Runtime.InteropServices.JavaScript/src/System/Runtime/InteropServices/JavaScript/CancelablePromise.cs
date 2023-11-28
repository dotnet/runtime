// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static partial class CancelablePromise
    {
        public static void CancelPromise(Task promise)
        {
            // this check makes sure that promiseGCHandle is still valid handle
            if (promise.IsCompleted)
            {
                return;
            }
            JSHostImplementation.PromiseHolder? holder = promise.AsyncState as JSHostImplementation.PromiseHolder;
            if (holder == null) throw new InvalidOperationException("Expected Task converted from JS Promise");


#if FEATURE_WASM_THREADS
            Interop.Runtime.CancelPromise(holder.OwnerTID, holder.GCHandle);
#else
            Interop.Runtime.CancelPromise(holder.GCHandle);
#endif
        }

        public static void CancelPromise<T>(Task promise, Action<T> callback, T state)
        {
            // this check makes sure that promiseGCHandle is still valid handle
            if (promise.IsCompleted)
            {
                return;
            }
            JSHostImplementation.PromiseHolder? holder = promise.AsyncState as JSHostImplementation.PromiseHolder;
            if (holder == null) throw new InvalidOperationException("Expected Task converted from JS Promise");

#if FEATURE_WASM_THREADS
            Interop.Runtime.CancelPromise(holder.OwnerTID, holder.GCHandle);
#else
            Interop.Runtime.CancelPromise(holder.GCHandle);
#endif
        }
    }
}
