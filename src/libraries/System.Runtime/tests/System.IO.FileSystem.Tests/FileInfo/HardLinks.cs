// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_HardLinks : BaseHardLinks_FileSystem
    {
        protected override void CreateFile(string path) =>
            File.Create(path).Dispose();

        protected override void AssertLinkExists(FileSystemInfo linkInfo) =>
            Assert.True(linkInfo.Exists);

        protected override FileSystemInfo CreateHardLink(string path, string pathToTarget)
        {
            FileInfo link = new FileInfo(path);
            link.CreateAsHardLink(pathToTarget);
            return link;
        }

    }
}

