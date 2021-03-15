// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class Mutex
    {
        private void CreateMutexCore(bool initiallyOwned, string? name, out bool createdNew)
        {
            // See https://github.com/dotnet/runtime/issues/48720
            if (name != null)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
            }

            SafeWaitHandle = WaitSubsystem.NewMutex(initiallyOwned);
            createdNew = true;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Mutex? result)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
        }

        public void ReleaseMutex()
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
                WaitSubsystem.ReleaseMutex(waitHandle.DangerousGetHandle());
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }
    }
}
