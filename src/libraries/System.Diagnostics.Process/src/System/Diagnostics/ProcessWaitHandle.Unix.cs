// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace System.Diagnostics
{
    internal sealed class ProcessWaitHandle : WaitHandle
    {
        /// <summary>
        /// Holds a wait state object associated with this handle.
        /// </summary>
        private ProcessWaitState.Holder _waitStateHolder;

        internal ProcessWaitHandle(SafeProcessHandle processHandle)
        {
            // Get the process ID from the process handle.  The handle is just a facade that wraps
            // the process ID, and closing the handle won't affect the process or its ID at all.
            // So we can grab it, and it's not "dangerous".
            int processId = (int)processHandle.DangerousGetHandle();

            // Create a wait state holder for this process ID.  This gives us access to the shared
            // wait state associated with this process.
            _waitStateHolder = new ProcessWaitState.Holder(processId);

            // Get the wait state's event, and use that event's safe wait handle
            // in place of ours.  This will let code register for completion notifications
            // on this ProcessWaitHandle and be notified when the wait state's handle completes.
            ManualResetEvent mre = _waitStateHolder._state.EnsureExitedEvent();
            this.SetSafeWaitHandle(mre.GetSafeWaitHandle());
        }

        protected override void Dispose(bool explicitDisposing)
        {
            if (explicitDisposing)
            {
                if (_waitStateHolder != null)
                {
                    _waitStateHolder.Dispose();
                    _waitStateHolder = null;
                }
            }
            base.Dispose(explicitDisposing);
        }
    }
}
