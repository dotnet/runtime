// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Name : FileSystemTest
    {
        [Fact]
        public void Ctor_NullArgument_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => new FileInfo(null));
        }

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
