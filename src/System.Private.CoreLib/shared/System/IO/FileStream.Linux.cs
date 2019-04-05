// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        /// <summary>Prevents other processes from reading from or writing to the FileStream.</summary>
        /// <param name="position">The beginning of the range to lock.</param>
        /// <param name="length">The range to be locked.</param>
        private void LockInternal(long position, long length)
        {
            CheckFileCall(Interop.Sys.LockFileRegion(_fileHandle, position, length, Interop.Sys.LockType.F_WRLCK));
        }

        /// <summary>Allows access by other processes to all or part of a file that was previously locked.</summary>
        /// <param name="position">The beginning of the range to unlock.</param>
        /// <param name="length">The range to be unlocked.</param>
        private void UnlockInternal(long position, long length)
        {
            CheckFileCall(Interop.Sys.LockFileRegion(_fileHandle, position, length, Interop.Sys.LockType.F_UNLCK));
        }
    }
}
