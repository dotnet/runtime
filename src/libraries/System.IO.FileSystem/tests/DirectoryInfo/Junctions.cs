// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class DirectoryInfo_Junctions : BaseJunctions_FileSystem
    {
        protected override DirectoryInfo CreateDirectory(string path)
        {
            DirectoryInfo dirInfo = new(path);
            dirInfo.Create();
            return dirInfo;
        }

        protected override FileSystemInfo? ResolveLinkTarget(string junctionPath, bool returnFinalTarget) =>
            new DirectoryInfo(junctionPath).ResolveLinkTarget(returnFinalTarget);
    }
}
