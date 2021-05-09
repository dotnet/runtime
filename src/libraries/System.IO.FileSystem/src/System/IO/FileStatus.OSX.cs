// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal unsafe void SetCreationTime(string path, DateTimeOffset time)
        {
            Interop.Sys.TimeSpec timeSpec = default;

            long seconds = time.ToUnixTimeSeconds();
            long nanoseconds = UnixTimeSecondsToNanoseconds(time, seconds);

            timeSpec.TvSec = seconds;
            timeSpec.TvNsec = nanoseconds;

            Interop.libc.AttrList attrList = default;
            attrList.bitmapCount = Interop.libc.AttrList.ATTR_BIT_MAP_COUNT;
            attrList.commonAttr = Interop.libc.AttrList.ATTR_CMN_CRTIME;

            // Try to set the attribute on the file system entry using setattrlist,
            // otherwise fall back to the method used on other unix platforms as the
            // path may either be on an unsupported volume type (for setattrlist) or it
            // could be another error (eg. no file) which the fallback implementation can throw.
            bool succeeded = Interop.libc.setattrlist(path, &attrList, &timeSpec, sizeof(Interop.Sys.TimeSpec), new CULong(Interop.libc.FSOPT_NOFOLLOW)) == 0;
            if (!succeeded)
            {
                SetCreationTime_StandardUnixImpl(path, time);
            }
            else
            {
                InvalidateCaches();
            }
        }

        private unsafe void SetAccessOrWriteTime(string path, DateTimeOffset time, bool isAccessTime)
        {
            // Force an update so GetCreationTime is up-to-date.
            InvalidateCaches();
            EnsureCachesInitialized(path);

            // Get the creation time here in case the modification time is less than it.
            var creationTime = GetCreationTime(path);

            SetAccessOrWriteTimeImpl(path, time, isAccessTime);

            if ((isAccessTime ? GetLastWriteTime(path) : time) < creationTime)
            {
                // In this case, the creation time is moved back to the modification time on OSX.
                // So this code makes sure that the creation time is not changed when it shouldn't be.
                SetCreationTime(path, creationTime);
            }
            else
            {
                InvalidateCaches();
            }
        }
    }
}
