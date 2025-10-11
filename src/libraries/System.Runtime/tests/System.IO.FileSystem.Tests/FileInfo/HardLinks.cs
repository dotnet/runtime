// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    [ConditionalClass(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
    public class FileInfo_HardLinks : BaseHardLinks_FileSystem
    {
        private FileInfo GetFileSystemInfo(string path) =>
            new FileInfo(path);

        protected override void CreateFile(string path) =>
            File.Create(path).Dispose();

        protected override void AssertLinkExists(FileSystemInfo linkInfo) =>
            Assert.True(linkInfo.Exists);

        protected override FileSystemInfo CreateHardLink(string path, string pathToTarget)
        {
            FileInfo link = GetFileSystemInfo(path);
            link.CreateAsHardLink(pathToTarget);
            return link;
        }

    }
}

