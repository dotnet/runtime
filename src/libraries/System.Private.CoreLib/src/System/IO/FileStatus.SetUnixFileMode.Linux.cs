// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal partial struct FileStatus
    {
        private int ChModNoFollowLink(string path, int mode)
        {
            // Linux doesn't support file modes on links.
            // lchmod is not implemented and returns ENOTSUP even for non-links.
            // To support changing mode on non-links, we first check if the file
            // is a link. If it isn't, we use ChMod (which would follow links)
            // to change the mode.

            EnsureCachesInitialized(path);

            if (!EntryExists)
                FileSystemInfo.ThrowNotFound(path);

            return HasSymbolicLinkFlag ? Interop.Sys.LChMod(path!, mode)
                                       : Interop.Sys.ChMod(path, mode);
        }
    }
}
