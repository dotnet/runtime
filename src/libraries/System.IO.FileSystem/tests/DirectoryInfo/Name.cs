// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class DirectoryInfo_Name : FileSystemTest
    {
        [Fact]
        public void CurrentDirectory()
        {
            var info = new DirectoryInfo(".");
            Assert.Equal(Path.GetFileName(Directory.GetCurrentDirectory()), info.Name);
        }

        [Fact]
        public void UNCShareName()
        {
            var info = new DirectoryInfo(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("contoso", "amusement", "device"));
            Assert.Equal("device", info.Name);
        }

        [Fact]
        public void RootName()
        {
            var info = new DirectoryInfo(Path.GetPathRoot(Directory.GetCurrentDirectory()));
            Assert.Equal(Path.GetPathRoot(Directory.GetCurrentDirectory()), info.Name);
        }
    }
}
