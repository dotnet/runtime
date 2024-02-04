// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class CreateDirectoryWithUnixFileMode : FileSystemTest
    {
        [Fact]
        public void NotSupported()
        {
            string path = GetRandomDirPath();
            Assert.Throws<PlatformNotSupportedException>(() => Directory.CreateDirectory(path, UnixFileMode.UserRead));
        }
    }
}
