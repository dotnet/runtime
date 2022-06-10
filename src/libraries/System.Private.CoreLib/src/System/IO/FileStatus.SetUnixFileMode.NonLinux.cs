// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    internal partial struct FileStatus
    {
        private static int ChModNoFollowLink(string path, int mode) =>
            Interop.Sys.LChMod(path!, mode);
    }
}
