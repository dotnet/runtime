// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        internal void SetCreationTime(string path, DateTimeOffset time) => SetTimeOnFile(path, time, Interop.libc.AttrList.ATTR_CMN_CRTIME);

        internal void SetLastWriteTime(string path, DateTimeOffset time) => SetTimeOnFile(path, time, Interop.libc.AttrList.ATTR_CMN_MODTIME);

        private unsafe void SetTimeOnFile(string path, DateTimeOffset time, uint commonAttr)
        {
            Interop.Sys.TimeSpec timeSpec = default;

            long seconds = time.ToUnixTimeSeconds();

            const long TicksPerMillisecond = 10000;
            const long TicksPerSecond = TicksPerMillisecond * 1000;
            long nanoseconds = (time.UtcDateTime.Ticks - DateTimeOffset.UnixEpoch.Ticks - seconds * TicksPerSecond) * NanosecondsPerTick;

            timeSpec.TvSec = seconds;
            timeSpec.TvNsec = nanoseconds;

            Interop.libc.AttrList attrList = default;
            attrList.bitmapCount = Interop.libc.AttrList.ATTR_BIT_MAP_COUNT;
            attrList.reserved = 0;
            attrList.commonAttr = commonAttr;
            attrList.dirAttr = 0;
            attrList.fileAttr = 0;
            attrList.forkAttr = 0;
            attrList.volAttr = 0;

            Interop.libc.setattrlist(path, &attrList, &timeSpec, sizeof(Interop.Sys.TimeSpec), Interop.libc.FSOPT_NOFOLLOW);
        }
    }
}
