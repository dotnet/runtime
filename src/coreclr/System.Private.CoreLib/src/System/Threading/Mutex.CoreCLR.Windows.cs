// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        private static SafeWaitHandle CreateMutexCore(
            nint mutexAttributes,
            bool initialOwner,
            string? name,
            uint desiredAccess,
            out int errorCode,
            out string? errorDetails)
        {
            errorDetails = null;
            SafeWaitHandle mutexHandle =
                Interop.Kernel32.CreateMutexEx(
                    mutexAttributes,
                    name,
                    initialOwner ? Interop.Kernel32.CREATE_MUTEX_INITIAL_OWNER : 0,
                    desiredAccess);

            // Get the error code even if the handle is valid, as it could be ERROR_ALREADY_EXISTS, indicating that the mutex
            // already exists and was opened
            errorCode = Marshal.GetLastPInvokeError();

            return mutexHandle;
        }

        private static SafeWaitHandle OpenMutexCore(
            uint desiredAccess,
            bool inheritHandle,
            string name,
            out int errorCode,
            out string? errorDetails)
        {
            errorDetails = null;
            SafeWaitHandle mutexHandle = Interop.Kernel32.OpenMutex(desiredAccess, inheritHandle, name);
            errorCode = mutexHandle.IsInvalid ? Marshal.GetLastPInvokeError() : Interop.Errors.ERROR_SUCCESS;
            return mutexHandle;
        }
    }
}
