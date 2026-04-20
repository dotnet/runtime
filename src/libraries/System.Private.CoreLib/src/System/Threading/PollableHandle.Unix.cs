// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class PollableHandle
    {
        internal bool IsRegistered
        {
            get;
            private set;
        }

        private bool TryRegisterWithPollThread(out Interop.Error error)
        {
            // Multiple callers may try to register concurrently.
            using (_writeQueue.Lock()) // Lock is used for IsDisposed.
            {
                if (IsDisposed || IsRegistered)
                {
                    // Already registered/disposed.
                    error = Interop.Error.SUCCESS;
                    return true;
                }

                bool addedRef = false;
                try
                {
                    Handle.DangerousAddRef(ref addedRef);
                    IntPtr rawHandle = Handle.DangerousGetHandle();
                    if (PollThread.TryRegister(rawHandle, this, out error))
                    {
                        IsRegistered = true;
                        return true; // Succesfully registered.
                    }
                    else
                    {
                        return false; // Registration failed.
                    }
                }
                finally
                {
                    if (addedRef)
                    {
                        Handle.DangerousRelease();
                    }
                }
            }
        }

        private bool Register(PollTriggeredOperation node)
        {
            if (TryRegisterWithPollThread(out Interop.Error error))
            {
                // Registration was a success. Operation will be triggered by poll thread.
                return true;
            }

            // macOS: kevent returns EPIPE when adding pipe fd for which the other end is closed.
            if (error == Interop.Error.EPIPE && node.TryCompleteOperation(Handle))
            {
                return false;
            }

            if (error == Interop.Error.ENOMEM || error == Interop.Error.ENOSPC)
            {
                throw new OutOfMemoryException();
            }

            throw new InvalidOperationException($"Unexpected error: {error}");
        }

        private void Unregister()
        {
            Debug.Assert(IsDisposed);
            if (IsRegistered)
            {
                PollThread.Unregister(this);
            }
        }
    }
}
