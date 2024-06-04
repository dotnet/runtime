// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time, bool asDirectory)
            => SetCreationTime(handle: null, path, time, asDirectory);

        internal void SetCreationTime(SafeFileHandle handle, DateTimeOffset time, bool asDirectory)
            => SetCreationTime(handle, handle.Path, time, asDirectory);

        private void SetCreationTime(SafeFileHandle? handle, string? path, DateTimeOffset time, bool asDirectory)
        {
            // Either `handle` or `path` must not be null
            Debug.Assert(handle is not null || path is not null);

            // Try to set the attribute on the file system entry using setattrlist,
            // if we get ENOTSUP then it means that "The volume does not support
            // setattrlist()", so we fall back to the method used on other unix
            // platforms, otherwise we throw an error if we get one, or invalidate
            // the cache if successful because otherwise it has invalid information.
            // Note: the unix fallback implementation doesn't have a test as we are
            // yet to determine which volume types it can fail on, so modify with
            // great care.
            long seconds = time.ToUnixTimeSeconds();
            long nanoseconds = UnixTimeSecondsToNanoseconds(time, seconds);
            Interop.Error error = SetCreationTimeCore(handle, path, seconds, nanoseconds);

            if (error == Interop.Error.SUCCESS)
            {
                InvalidateCaches();
            }
            else if (error == Interop.Error.ENOTSUP)
            {
                SetAccessOrWriteTimeCore(handle, path, time, isAccessTime: false, checkCreationTime: false, asDirectory);
            }
            else
            {
                Interop.CheckIo(error, path, asDirectory);
            }
        }

        private static unsafe Interop.Error SetCreationTimeCore(SafeFileHandle? handle, string? path, long seconds, long nanoseconds)
        {
            Debug.Assert(handle is not null || path is not null);
            Interop.Sys.TimeSpec timeSpec = default;

            timeSpec.TvSec = seconds;
            timeSpec.TvNsec = nanoseconds;

            Interop.libc.AttrList attrList = default;
            attrList.bitmapCount = Interop.libc.AttrList.ATTR_BIT_MAP_COUNT;
            attrList.commonAttr = Interop.libc.AttrList.ATTR_CMN_CRTIME;

            int result = handle is not null
                ? Interop.libc.fsetattrlist(handle, &attrList, &timeSpec, sizeof(Interop.Sys.TimeSpec), new CULong(Interop.libc.FSOPT_NOFOLLOW))
                : Interop.libc.setattrlist(path!, &attrList, &timeSpec, sizeof(Interop.Sys.TimeSpec), new CULong(Interop.libc.FSOPT_NOFOLLOW));

            return result == 0 ?
                Interop.Error.SUCCESS :
                Interop.Sys.GetLastErrorInfo().Error;
        }

        private void SetAccessOrWriteTime(SafeFileHandle? handle, string? path, DateTimeOffset time, bool isAccessTime, bool asDirectory) =>
            SetAccessOrWriteTimeCore(handle, path, time, isAccessTime, checkCreationTime: true, asDirectory);
    }
}
