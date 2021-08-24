// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace System.IO.Tests
{
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

        protected override void VerifyEnumerateMethods(string junctionPath, string[] expectedFiles, string[] expectedDirectories, string[] expectedEntries)
        {
            EnumerationOptions options = new() { RecurseSubdirectories = true };

            DirectoryInfo info = new(junctionPath);

            VerifyEnumeration(
                info.EnumerateFiles("*", options).Select(x => x.FullName),
                expectedFiles);

            VerifyEnumeration(
                info.EnumerateDirectories("*", options).Select(x => x.FullName),
                expectedDirectories);

            VerifyEnumeration(
                info.EnumerateFileSystemInfos("*", options).Select(x => x.FullName),
                expectedEntries);

            VerifyEnumeration(
                info.GetFiles("*", options).Select(x => x.FullName),
                expectedFiles);

            VerifyEnumeration(
                info.GetDirectories("*", options).Select(x => x.FullName),
                expectedDirectories);

            VerifyEnumeration(
                info.GetFileSystemInfos("*", options).Select(x => x.FullName),
                expectedEntries);
        }
    }
}
