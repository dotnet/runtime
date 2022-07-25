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

            _CancelPromise((IntPtr)promiseGCHandle.Value);
        }
    }
}
