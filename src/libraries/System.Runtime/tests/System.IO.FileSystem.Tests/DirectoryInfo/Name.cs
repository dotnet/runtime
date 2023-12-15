// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class DirectoryInfo_Name : FileSystemTest
    {
        [Fact]
        public void Ctor_NullArgument_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => new DirectoryInfo(null));
        }

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
