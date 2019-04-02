// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Synchronization primitive that can also be used for interprocess synchronization
    /// </summary>
    public sealed partial class Mutex : WaitHandle
    {
        public Mutex(bool initiallyOwned, string? name, out bool createdNew)
        {
            CreateMutexCore(initiallyOwned, name, out createdNew);
        }

        public Mutex(bool initiallyOwned, string? name)
        {
            CreateMutexCore(initiallyOwned, name, out _);
        }

        public Mutex(bool initiallyOwned)
        {
            CreateMutexCore(initiallyOwned, null, out _);
        }

        public Mutex()
        {
            CreateMutexCore(false, null, out _);
        }

        private Mutex(SafeWaitHandle handle)
        {
            SafeWaitHandle = handle;
        }

        public static Mutex OpenExisting(string name)
        {
            switch (OpenExistingWorker(name, out Mutex? result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, name));

                default:
                    Debug.Assert(result != null, "result should be non-null on success");
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Mutex? result) => // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            OpenExistingWorker(name, out result) == OpenExistingResult.Success;
    }
}
