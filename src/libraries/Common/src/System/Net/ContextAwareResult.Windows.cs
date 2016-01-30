// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace System.Net
{
    partial class ContextAwareResult
    {
        private WindowsIdentity _windowsIdentity;

        // Security: We need an assert for a call into WindowsIdentity.GetCurrent.
        private void SafeCaptureIdentity()
        {
            _windowsIdentity = WindowsIdentity.GetCurrent();
        }

        // Just like ContextCopy.
        internal WindowsIdentity Identity
        {
            get
            {
                if (InternalPeekCompleted)
                {
                    if ((_flags & StateFlags.ThreadSafeContextCopy) == 0)
                    {
                        if (GlobalLog.IsEnabled)
                        {
                            GlobalLog.AssertFormat("ContextAwareResult#{0}::Identity|Called on completed result.", LoggingHash.HashString(this));
                        }

                        Debug.Fail("ContextAwareResult#" + LoggingHash.HashString(this) + "::Identity |Called on completed result.");
                    }

                    throw new InvalidOperationException(SR.net_completed_result);
                }

                if (_windowsIdentity != null)
                {
                    return _windowsIdentity;
                }

                // Make sure the identity was requested.
                if ((_flags & StateFlags.CaptureIdentity) == 0)
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.AssertFormat("ContextAwareResult#{0}::Identity|No identity captured - specify captureIdentity.", LoggingHash.HashString(this));
                    }

                    Debug.Fail("ContextAwareResult#" + LoggingHash.HashString(this) + "::Identity |No identity captured - specify captureIdentity.");
                }

                // Just use the lock to block.  We might be on the thread that owns the lock which is great, it means we
                // don't need an identity anyway.
                if ((_flags & StateFlags.PostBlockFinished) == 0)
                {
                    if (_lock == null)
                    {
                        if (GlobalLog.IsEnabled)
                        {
                            GlobalLog.AssertFormat("ContextAwareResult#{0}::Identity|Must lock (StartPostingAsyncOp()) { ... FinishPostingAsyncOp(); } when calling Identity (unless it's only called after FinishPostingAsyncOp).", LoggingHash.HashString(this));
                        }

                        Debug.Fail("ContextAwareResult#" + LoggingHash.HashString(this) + "::Identity |Must lock (StartPostingAsyncOp()) { ... FinishPostingAsyncOp(); } when calling Identity (unless it's only called after FinishPostingAsyncOp).");
                    }

                    lock (_lock) { }
                }

                if (InternalPeekCompleted)
                {
                    if ((_flags & StateFlags.ThreadSafeContextCopy) == 0)
                    {
                        if (GlobalLog.IsEnabled)
                        {
                            GlobalLog.AssertFormat("ContextAwareResult#{0}::Identity|Result became completed during call.", LoggingHash.HashString(this));
                        }

                        Debug.Fail("ContextAwareResult#" + LoggingHash.HashString(this) + "::Identity |Result became completed during call.");
                    }

                    throw new InvalidOperationException(SR.net_completed_result);
                }

                return _windowsIdentity;
            }
        }

        private void CleanupInternal()
        {
            if (_windowsIdentity != null)
            {
                _windowsIdentity.Dispose();
                _windowsIdentity = null;
            }
        }
    }
}
