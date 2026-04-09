// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.NetworkInformation
{
    // This class wraps the native API NotifyStableUnicastIpAddressTable.  The native function's behavior is:
    //
    // 1. If the address table is already stable, it returns ERROR_SUCCESS and a Mib table handle that we must free.
    //    The passed-in callback will never be called, and the cancelHandle is set to NULL.
    //
    // 2. If the address table is not stable, it returns ERROR_IO_PENDING.  The table handle is set to NULL,
    //    and the cancelHandle is set to a valid handle.  The callback will be called (on a native threadpool thread)
    //    EVERY TIME the address table becomes stable until CancelMibChangeNotify2 is called on the cancelHandle
    //    (via cancelHandle.Dispose()).
    //
    // CancelMibChangeNotify2 guarantees that, by the time it returns, all calls to the callback will be complete
    // and that no new calls to the callback will be issued.
    //

    internal sealed class TeredoHelper
    {
        private readonly Action<object> _callback;
        private readonly object _state;

        private bool _runCallbackCalled;

        private GCHandle _gcHandle;

        // Used to cancel notification after receiving the first callback, or when the AppDomain is going down.
        private SafeCancelMibChangeNotify? _cancelHandle;

        private TeredoHelper(Action<object> callback, object state)
        {
            _callback = callback;
            _state = state;

            _gcHandle = GCHandle.Alloc(this);
        }

        // Returns true if the address table is already stable.  Otherwise, calls callback when it becomes stable.
        // 'Unsafe' because it does not flow ExecutionContext to the callback.
        public static unsafe bool UnsafeNotifyStableUnicastIpAddressTable(Action<object> callback, object state)
        {
            Debug.Assert(callback != null);

            TeredoHelper? helper = new TeredoHelper(callback, state);
            try
            {
                uint err = Interop.IpHlpApi.NotifyStableUnicastIpAddressTable(AddressFamily.Unspecified,
                    out SafeFreeMibTable table, &OnStabilized, GCHandle.ToIntPtr(helper._gcHandle), out helper._cancelHandle);

                table.Dispose();

                if (err == Interop.IpHlpApi.ERROR_IO_PENDING)
                {
                    Debug.Assert(helper._cancelHandle != null && !helper._cancelHandle.IsInvalid);

                    // Suppress synchronous Dispose. Dispose will be called asynchronously by the callback.
                    helper = null;
                    return false;
                }

                if (err != Interop.IpHlpApi.ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)err);
                }

                return true;
            }
            finally
            {
                helper?.Dispose();
            }
        }

        private void Dispose()
        {
            _cancelHandle?.Dispose();

            if (_gcHandle.IsAllocated)
                _gcHandle.Free();
        }

        // This callback gets run on a native worker thread, which we don't want to allow arbitrary user code to
        // execute on (it will block AppDomain unload, for one).  Free the MibTable and delegate (exactly once)
        // to the managed ThreadPool for the rest of the processing.
        [UnmanagedCallersOnly]
        private static void OnStabilized(IntPtr context, IntPtr table)
        {
            Interop.IpHlpApi.FreeMibTable(table);

            TeredoHelper helper = (TeredoHelper)GCHandle.FromIntPtr(context).Target!;

            // Lock the TeredoHelper instance to ensure that only the first call to OnStabilized will get to call
            // RunCallback.  This is the only place that TeredoHelpers get locked, as individual instances are not
            // exposed to higher layers, so there's no chance for deadlock.
            if (!helper._runCallbackCalled)
            {
                lock (helper)
                {
                    if (!helper._runCallbackCalled)
                    {
                        helper._runCallbackCalled = true;

                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            TeredoHelper helper = (TeredoHelper)o!;

                            // We are intentionally not calling Dispose synchronously inside the OnStabilized callback.
                            // According to MSDN, calling CancelMibChangeNotify2 inside the callback results into deadlock.
                            helper.Dispose();

                            helper._callback.Invoke(helper._state);
                        }, helper);
                    }
                }
            }
        }
    }
}
