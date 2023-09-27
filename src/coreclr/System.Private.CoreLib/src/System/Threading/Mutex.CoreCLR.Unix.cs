// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        private const int SystemCallErrorsBufferSize = 256;

        private static unsafe SafeWaitHandle CreateMutexCore(
            nint mutexAttributes,
            bool initialOwner,
            string? name,
            uint desiredAccess,
            out int errorCode,
            out string? errorDetails)
        {
            byte* systemCallErrors = stackalloc byte[SystemCallErrorsBufferSize];
            SafeWaitHandle mutexHandle =
                CreateMutex(
                    mutexAttributes,
                    initialOwner,
                    name,
                    systemCallErrors,
                    SystemCallErrorsBufferSize);

            // Get the error code even if the handle is valid, as it could be ERROR_ALREADY_EXISTS, indicating that the mutex
            // already exists and was opened
            errorCode = Marshal.GetLastPInvokeError();

            errorDetails = mutexHandle.IsInvalid ? GetErrorDetails(systemCallErrors) : null;
            return mutexHandle;
        }

        private static unsafe SafeWaitHandle OpenMutexCore(
            uint desiredAccess,
            bool inheritHandle,
            string name,
            out int errorCode,
            out string? errorDetails)
        {
            byte* systemCallErrors = stackalloc byte[SystemCallErrorsBufferSize];
            SafeWaitHandle mutexHandle =
                OpenMutex(desiredAccess, inheritHandle, name, systemCallErrors, SystemCallErrorsBufferSize);
            errorCode = mutexHandle.IsInvalid ? Marshal.GetLastPInvokeError() : Interop.Errors.ERROR_SUCCESS;
            errorDetails = mutexHandle.IsInvalid ? GetErrorDetails(systemCallErrors) : null;
            return mutexHandle;
        }

        private static unsafe string? GetErrorDetails(byte* systemCallErrors)
        {
            int systemCallErrorsLength =
                new ReadOnlySpan<byte>(systemCallErrors, SystemCallErrorsBufferSize).IndexOf((byte)'\0');
            if (systemCallErrorsLength > 0)
            {
                try
                {
                    return
                        SR.Format(SR.Unix_SystemCallErrors, Encoding.UTF8.GetString(systemCallErrors, systemCallErrorsLength));
                }
                catch { } // avoid hiding the original error due to an error here
            }

            return null;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PAL_CreateMutexW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial SafeWaitHandle CreateMutex(nint mutexAttributes, [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string? name, byte *systemCallErrors, uint systemCallErrorsBufferSize);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PAL_OpenMutexW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial SafeWaitHandle OpenMutex(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, string name, byte* systemCallErrors, uint systemCallErrorsBufferSize);
    }
}
