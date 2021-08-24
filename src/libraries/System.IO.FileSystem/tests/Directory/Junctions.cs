// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public class Directory_Junctions : BaseJunctions_FileSystem
    {
        protected override DirectoryInfo CreateDirectory(string path) =>
            Directory.CreateDirectory(path);

        protected override FileSystemInfo? ResolveLinkTarget(string junctionPath, bool returnFinalTarget) =>
            Directory.ResolveLinkTarget(junctionPath, returnFinalTarget);

        protected override void VerifyEnumerateMethods(string junctionPath, string[] expectedFiles, string[] expectedDirectories, string[] expectedEntries)
        {
            EnumerationOptions options = new() { RecurseSubdirectories = true };

            VerifyEnumeration(
                Directory.EnumerateFiles(junctionPath, "*", options),
                expectedFiles);

            VerifyEnumeration(
                Directory.EnumerateDirectories(junctionPath, "*", options),
                expectedDirectories);

            VerifyEnumeration(
                Directory.EnumerateFileSystemEntries(junctionPath, "*", options),
                expectedEntries);

            VerifyEnumeration(
                Directory.GetFiles(junctionPath, "*", options),
                expectedFiles);

            VerifyEnumeration(
                Directory.GetDirectories(junctionPath, "*", options),
                expectedDirectories);

            VerifyEnumeration(
                Directory.GetFileSystemEntries(junctionPath, "*", options),
                expectedEntries);
        }
    }
}
