// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal sealed partial class LegacyFileStreamStrategy : FileStreamStrategy
    {
        /// <summary>Prevents other processes from reading from or writing to the FileStream.</summary>
        /// <param name="position">The beginning of the range to lock.</param>
        /// <param name="length">The range to be locked.</param>
        internal override void Lock(long position, long length)
        {
            CheckFileCall(Interop.Sys.LockFileRegion(_fileHandle, position, length, CanWrite ? Interop.Sys.LockType.F_WRLCK : Interop.Sys.LockType.F_RDLCK));
        }

        /// <summary>Allows access by other processes to all or part of a file that was previously locked.</summary>
        /// <param name="position">The beginning of the range to unlock.</param>
        /// <param name="length">The range to be unlocked.</param>
        internal override void Unlock(long position, long length)
        {
            CheckFileCall(Interop.Sys.LockFileRegion(_fileHandle, position, length, Interop.Sys.LockType.F_UNLCK));
        }
    }
}
