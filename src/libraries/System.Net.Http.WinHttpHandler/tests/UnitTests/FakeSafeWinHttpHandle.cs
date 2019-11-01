// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    internal class FakeSafeWinHttpHandle : Interop.WinHttp.SafeWinHttpHandle
    {
        private Interop.WinHttp.WINHTTP_STATUS_CALLBACK _callback = null;
        private IntPtr _context = IntPtr.Zero;

        public FakeSafeWinHttpHandle(bool markAsValid)
        {
            if (markAsValid)
            {
                SetHandle(Marshal.AllocHGlobal(1));
                Debug.WriteLine("FakeSafeWinHttpHandle.cctor, handle=#{0}", handle.GetHashCode());
            }
            else
            {
                SetHandleAsInvalid();
            }
        }

        public Interop.WinHttp.WINHTTP_STATUS_CALLBACK Callback
        {
            get
            {
                return _callback;
            }

            set
            {
                _callback = value;
            }
        }

        public IntPtr Context
        {
            get
            {
                return _context;
            }

            set
            {
                _context = value;
            }
        }

        public bool DelayOperation(int delay)
        {
            if (delay <= 0)
            {
                return true;
            }

            // Sleep for delay time specified.  Abort if handle becomes closed.
            var sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds <= delay)
            {
                if (IsClosed)
                {
                    sw.Stop();
                    return false;
                }

                Thread.Sleep(1);
            }

            sw.Stop();

            return true;
        }

        public void InvokeCallback(uint internetStatus, Interop.WinHttp.WINHTTP_ASYNC_RESULT asyncResult)
        {
            GCHandle pinnedAsyncResult = GCHandle.Alloc(asyncResult, GCHandleType.Pinned);
            IntPtr statusInformation = pinnedAsyncResult.AddrOfPinnedObject();
            uint statusInformationLength = (uint)Marshal.SizeOf<Interop.WinHttp.WINHTTP_ASYNC_RESULT>();

            InvokeCallback(internetStatus, statusInformation, statusInformationLength);

            pinnedAsyncResult.Free();
        }

        public void InvokeCallback(uint internetStatus, IntPtr statusInformation, uint statusInformationLength)
        {
            _callback(DangerousGetHandle(), _context, internetStatus, statusInformation, statusInformationLength);
        }

        protected override bool ReleaseHandle()
        {
            Debug.WriteLine("FakeSafeWinHttpHandle.ReleaseHandle, handle=#{0}", handle.GetHashCode());
            return base.ReleaseHandle();
        }
    }
}
