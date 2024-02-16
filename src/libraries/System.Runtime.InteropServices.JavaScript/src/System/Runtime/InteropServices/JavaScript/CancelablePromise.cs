// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading;
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

#if !FEATURE_WASM_MANAGED_THREADS
            if (holder.IsDisposed)
            {
                return;
            }
            holder.IsCanceling = true;
            Interop.Runtime.CancelPromise(holder.GCHandle);
#else

            lock (holder.ProxyContext)
            {
                if (promise.IsCompleted || holder.IsDisposed || holder.ProxyContext._isDisposed)
                {
                    return;
                }
                holder.IsCanceling = true;

                if (holder.ProxyContext.IsCurrentThread())
                {
                    Interop.Runtime.CancelPromise(holder.GCHandle);
                }
                else
                {
                    // FIXME: race condition
                    // we know that holder.GCHandle is still valid because we hold the ProxyContext lock
                    // but the message may arrive to the target thread after it was resolved, making GCHandle invalid
                    Interop.Runtime.CancelPromisePost(holder.ProxyContext.JSNativeTID, holder.GCHandle);
                }
            }
#endif
        }
    }
}
