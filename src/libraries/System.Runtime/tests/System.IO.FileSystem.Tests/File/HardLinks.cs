// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    [ConditionalClass(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
    public class File_HardLinks : BaseHardLinks_FileSystem
    {
        protected override void CreateFile(string path) =>
            File.Create(path).Dispose();

        protected override void AssertLinkExists(FileSystemInfo linkInfo) =>
            Assert.True(linkInfo.Exists);

        protected override FileSystemInfo CreateHardLink(string path, string pathToTarget) =>
            File.CreateHardLink(path, pathToTarget);
    }
}
