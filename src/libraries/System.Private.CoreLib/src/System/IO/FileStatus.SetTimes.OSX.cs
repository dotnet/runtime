// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time, bool asDirectory)
        {
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
            Interop.Error error = SetCreationTimeCore(path, seconds, nanoseconds);

            if (error == Interop.Error.SUCCESS)
            {
                InvalidateCaches();
            }
            else if (error == Interop.Error.ENOTSUP)
            {
                SetAccessOrWriteTimeCore(path, time, isAccessTime: false, checkCreationTime: false, asDirectory);
            }
            else
            {
                Interop.CheckIo(error, path, asDirectory);
            }
        }

        private static unsafe Interop.Error SetCreationTimeCore(string path, long seconds, long nanoseconds)
        {
            Interop.Sys.TimeSpec timeSpec = default;

            timeSpec.TvSec = seconds;
            timeSpec.TvNsec = nanoseconds;

            Interop.libc.AttrList attrList = default;
            attrList.bitmapCount = Interop.libc.AttrList.ATTR_BIT_MAP_COUNT;
            attrList.commonAttr = Interop.libc.AttrList.ATTR_CMN_CRTIME;

            Interop.Error error =
                Interop.libc.setattrlist(path, &attrList, &timeSpec, sizeof(Interop.Sys.TimeSpec), new CULong(Interop.libc.FSOPT_NOFOLLOW)) == 0 ?
                Interop.Error.SUCCESS :
                Interop.Sys.GetLastErrorInfo().Error;

            return error;
        }

        private void SetAccessOrWriteTime(string path, DateTimeOffset time, bool isAccessTime, bool asDirectory) =>
            SetAccessOrWriteTimeCore(path, time, isAccessTime, checkCreationTime: true, asDirectory);
    }
}
