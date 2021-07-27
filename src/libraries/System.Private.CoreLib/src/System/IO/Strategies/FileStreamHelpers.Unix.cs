// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static partial class FileStreamHelpers
    {
        private static OSFileStreamStrategy ChooseStrategyCore(SafeFileHandle handle, FileAccess access, FileShare share, bool isAsync) =>
            new UnixFileStreamStrategy(handle, access, share);

        private static FileStreamStrategy ChooseStrategyCore(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize) =>
            new UnixFileStreamStrategy(path, mode, access, share, options, preallocationSize);

        internal static long CheckFileCall(long result, string? path, bool ignoreNotSupported = false)
        {
            if (result < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (!(ignoreNotSupported && errorInfo.Error == Interop.Error.ENOTSUP))
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, path, isDirectory: false);
                }
            }

            return result;
        }

        internal static long Seek(SafeFileHandle handle, long offset, SeekOrigin origin, bool closeInvalidHandle = false) =>
            CheckFileCall(Interop.Sys.LSeek(handle, offset, (Interop.Sys.SeekWhence)(int)origin), handle.Path); // SeekOrigin values are the same as Interop.libc.SeekWhence values

        internal static void ThrowInvalidArgument(SafeFileHandle handle) =>
            throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EINVAL), handle.Path);

        internal static unsafe void SetFileLength(SafeFileHandle handle, long length) =>
            CheckFileCall(Interop.Sys.FTruncate(handle, length), handle.Path);

        /// <summary>Flushes the file's OS buffer.</summary>
        internal static void FlushToDisk(SafeFileHandle handle)
        {
            if (Interop.Sys.FSync(handle) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                switch (errorInfo.Error)
                {
                    case Interop.Error.EROFS:
                    case Interop.Error.EINVAL:
                    case Interop.Error.ENOTSUP:
                        // Ignore failures for special files that don't support synchronization.
                        // In such cases there's nothing to flush.
                        break;
                    default:
                        throw Interop.GetExceptionForIoErrno(errorInfo, handle.Path, isDirectory: false);
                }
            }
        }

        internal static void Lock(SafeFileHandle handle, bool canWrite, long position, long length)
        {
            if (OperatingSystem.IsOSXLike())
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
            }

            CheckFileCall(Interop.Sys.LockFileRegion(handle, position, length, canWrite ? Interop.Sys.LockType.F_WRLCK : Interop.Sys.LockType.F_RDLCK), handle.Path);
        }

        internal static void Unlock(SafeFileHandle handle, long position, long length)
        {
            if (OperatingSystem.IsOSXLike())
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
            }

            CheckFileCall(Interop.Sys.LockFileRegion(handle, position, length, Interop.Sys.LockType.F_UNLCK), handle.Path);
        }
    }
}
