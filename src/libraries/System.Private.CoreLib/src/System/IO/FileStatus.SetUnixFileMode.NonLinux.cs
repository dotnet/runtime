// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal partial struct FileStatus
    {
        private int ChModNoFollowLink(string path, int mode)
        {
            EnsureCachesInitialized(path);

            if (!EntryExists)
                FileSystemInfo.ThrowNotFound(path);

            return Interop.Sys.LChMod(path, mode);
        }
    }
}
