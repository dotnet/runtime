// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class Directory_Junctions : BaseJunctions_FileSystem
    {
        protected override DirectoryInfo CreateDirectory(string path) =>
            Directory.CreateDirectory(path);

        protected override FileSystemInfo? ResolveLinkTarget(string junctionPath, bool returnFinalTarget) =>
            Directory.ResolveLinkTarget(junctionPath, returnFinalTarget);
    }
}
