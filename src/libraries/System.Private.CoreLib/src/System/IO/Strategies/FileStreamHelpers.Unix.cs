// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static partial class FileStreamHelpers
    {
#pragma warning disable IDE0060
        private static UnixFileStreamStrategy ChooseStrategyCore(SafeFileHandle handle, FileAccess access, bool isAsync) =>
            new UnixFileStreamStrategy(handle, access);
#pragma warning restore IDE0060

        private static UnixFileStreamStrategy ChooseStrategyCore(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, UnixFileMode? unixCreateMode) =>
            new UnixFileStreamStrategy(path, mode, access, share, options, preallocationSize, unixCreateMode);

        internal static long CheckFileCall(long result, string? path, bool ignoreNotSupported = false)
        {
            if (result < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (!(ignoreNotSupported && errorInfo.Error == Interop.Error.ENOTSUP))
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, path);
                }
            }

            return result;
        }

        internal static long Seek(SafeFileHandle handle, long offset, SeekOrigin origin) =>
            CheckFileCall(Interop.Sys.LSeek(handle, offset, (Interop.Sys.SeekWhence)(int)origin), handle.Path); // SeekOrigin values are the same as Interop.libc.SeekWhence values

        internal static void ThrowInvalidArgument(SafeFileHandle handle) =>
            throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(Interop.Error.EINVAL), handle.Path);

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
                        throw Interop.GetExceptionForIoErrno(errorInfo, handle.Path);
                }
            }
        }

        internal static void Lock(SafeFileHandle handle, bool canWrite, long position, long length)
        {
            if (OperatingSystem.IsApplePlatform() || OperatingSystem.IsFreeBSD())
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
            }

            CheckFileCall(Interop.Sys.LockFileRegion(handle, position, length, canWrite ? Interop.Sys.LockType.F_WRLCK : Interop.Sys.LockType.F_RDLCK), handle.Path);
        }

        internal static void Unlock(SafeFileHandle handle, long position, long length)
        {
            if (OperatingSystem.IsApplePlatform() || OperatingSystem.IsFreeBSD())
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_OSXFileLocking);
            }

            CheckFileCall(Interop.Sys.LockFileRegion(handle, position, length, Interop.Sys.LockType.F_UNLCK), handle.Path);
        }
    }
}
