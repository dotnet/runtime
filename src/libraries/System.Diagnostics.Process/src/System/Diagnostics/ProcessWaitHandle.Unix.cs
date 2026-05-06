// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal sealed class ProcessWaitHandle : WaitHandle
    {
        internal ProcessWaitHandle(ProcessWaitState processWaitState)
        {
            // Get the wait state's event, and use that event's safe wait handle
            // in place of ours.  This will let code register for completion notifications
            // on this ProcessWaitHandle and be notified when the wait state's handle completes.
            ManualResetEvent mre = processWaitState.EnsureExitedEvent();
            this.SetSafeWaitHandle(mre.GetSafeWaitHandle());
        }

        protected override void Dispose(bool explicitDisposing)
        {
            // ProcessWaitState will dispose the handle
            this.SafeWaitHandle = null;
            base.Dispose(explicitDisposing);
        }
    }
}
