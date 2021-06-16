// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static partial class FileStreamHelpers
    {
        // in the future we are most probably going to introduce more strategies (io_uring etc)
        private static FileStreamStrategy ChooseStrategyCore(SafeFileHandle handle, FileAccess access, FileShare share, int bufferSize, bool isAsync)
            => new Net5CompatFileStreamStrategy(handle, access, bufferSize == 0 ? 1 : bufferSize, isAsync);

        private static FileStreamStrategy ChooseStrategyCore(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
            => new Net5CompatFileStreamStrategy(path, mode, access, share, bufferSize == 0 ? 1 : bufferSize, options, preallocationSize);

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
    }
}
