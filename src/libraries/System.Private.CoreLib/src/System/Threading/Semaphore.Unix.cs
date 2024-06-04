// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Semaphore
    {
        private void CreateSemaphoreCore(int initialCount, int maximumCount, string? name, out bool createdNew)
        {
            if (name != null)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
            }

            SafeWaitHandle = WaitSubsystem.NewSemaphore(initialCount, maximumCount);
            createdNew = true;
        }

#pragma warning disable IDE0060
        private static OpenExistingResult OpenExistingWorker(string name, out Semaphore? result)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
        }
#pragma warning restore IDE0060

        private int ReleaseCore(int releaseCount)
        {
            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle waitHandle = SafeWaitHandle;
            if (waitHandle.IsInvalid)
            {
                ThrowInvalidHandleException();
            }

            waitHandle.DangerousAddRef();
            try
            {
                return WaitSubsystem.ReleaseSemaphore(waitHandle.DangerousGetHandle(), releaseCount);
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }
    }
}
