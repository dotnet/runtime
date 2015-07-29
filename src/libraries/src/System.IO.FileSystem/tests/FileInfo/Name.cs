// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_Name : FileSystemTest
    {
        [Fact]
        public void ValidCase()
        {
            var info = new FileInfo(Path.Combine("Double", "single"));
            Assert.Equal("single", info.Name);
        }

        [Fact]
        public void UNCShareName()
        {
            var info = new FileInfo(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("contoso", "amusement", "device"));
            Assert.Equal("device", info.Name);
        }

        [Fact]
        public void RelativeSubPath()
        {
            var info = new FileInfo(Path.DirectorySeparatorChar + Path.Combine("Directory", "File"));
            Assert.Equal("File", info.Name);
        }
    }
}
