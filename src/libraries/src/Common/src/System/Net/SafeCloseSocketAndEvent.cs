// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed class SafeCloseSocketAndEvent : SafeCloseSocket
    {
        private AutoResetEvent _waitHandle;

        internal SafeCloseSocketAndEvent() : base() { }

        internal static SafeCloseSocketAndEvent CreateWSASocketWithEvent(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, bool autoReset, bool signaled)
        {
            SafeCloseSocketAndEvent result = new SafeCloseSocketAndEvent();
            CreateSocket(InnerSafeCloseSocket.CreateWSASocket(addressFamily, socketType, protocolType), result);
            if (result.IsInvalid)
            {
                throw new SocketException();
            }

            result._waitHandle = new AutoResetEvent(false);
            CompleteInitialization(result);
            return result;
        }

        internal static void CompleteInitialization(SafeCloseSocketAndEvent socketAndEventHandle)
        {
            SafeWaitHandle handle = socketAndEventHandle._waitHandle.GetSafeWaitHandle();

            bool ignore = false;
            handle.DangerousAddRef(ref ignore);

            // TODO #3562: Investigate if this pattern is still correct.
            // Note that the handle still has a reference from the above DangerousAddRef.
            handle.Dispose();
        }

        internal WaitHandle GetEventHandle()
        {
            return _waitHandle;
        }

        protected override bool ReleaseHandle()
        {
            bool result = base.ReleaseHandle();
            DeleteEvent();
            return result;
        }

        private void DeleteEvent()
        {
            try
            {
                if (_waitHandle != null)
                {
                    var waitHandleSafeWaitHandle = _waitHandle.GetSafeWaitHandle();
                    waitHandleSafeWaitHandle.DangerousRelease();
                }
            }
            catch { }
        }
    }
}
