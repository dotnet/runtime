// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time)
        {
            // Try to set the attribute on the file system entry using setattrlist,
            // if we get ENOTSUP then it means that "The volume does not support
            // setattrlist()", so we fall back to the method used on other unix
            // platforms, otherwise we throw an error if we get one, or invalidate
            // the cache if successful because otherwise it has invalid information.
            Interop.Error error = SetCreationTimeCore(path, time);

            if (error == Interop.Error.ENOTSUP)
            {
                SetAccessOrWriteTimeCore(path, time, isAccessTime: false, checkCreationTime: false);
            }
            else if (error != Interop.Error.SUCCESS)
            {
                Interop.CheckIo(error, path, InitiallyDirectory);
            }
        }

        private unsafe Interop.Error SetCreationTimeCore(string path, DateTimeOffset time)
        {
            Interop.Sys.TimeSpec timeSpec = default;

            long seconds = time.ToUnixTimeSeconds();
            long nanoseconds = UnixTimeSecondsToNanoseconds(time, seconds);

            timeSpec.TvSec = seconds;
            timeSpec.TvNsec = nanoseconds;

            Interop.libc.AttrList attrList = default;
            attrList.bitmapCount = Interop.libc.AttrList.ATTR_BIT_MAP_COUNT;
            attrList.commonAttr = Interop.libc.AttrList.ATTR_CMN_CRTIME;

            Interop.Error error =  (Interop.Error)Interop.libc.setattrlist(path, &attrList, &timeSpec, sizeof(Interop.Sys.TimeSpec), default(CULong));

            if (error == Interop.Error.SUCCESS)
            {
                InvalidateCaches();
            }

            return error;
        }

        private void SetAccessOrWriteTime(string path, DateTimeOffset time, bool isAccessTime) =>
            SetAccessOrWriteTimeCore(path, time, isAccessTime, checkCreationTime: true);
    }
}
