// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class PathFile_Exists : File_Exists
    {
        public override bool Exists(string path) => Path.Exists(path);

        [Fact]
        public void PathAlreadyExistsAsDirectory()
        {
            string path = GetTestFilePath();
            Directory.CreateDirectory(path);

            Assert.True(Exists(IOServices.RemoveTrailingSlash(path)));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.RemoveTrailingSlash(path))));
            Assert.True(Exists(IOServices.RemoveTrailingSlash(IOServices.AddTrailingSlashIfNeeded(path))));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser & ~TestPlatforms.iOS & ~TestPlatforms.tvOS)]  // Uses P/Invokes
        public void TrueForNonRegularFile()
        {
            string fileName = GetTestFilePath();
            Assert.Equal(0, mkfifo(fileName, 0));
            Assert.True(Exists(fileName));
        }
    }
}
