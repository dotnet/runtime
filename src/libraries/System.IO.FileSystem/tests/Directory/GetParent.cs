// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetParent : FileSystemTest
    {
        protected virtual DirectoryInfo GetParent(string path)
        {
            return Directory.GetParent(path);
        }

        [Fact]
        public void Null_Path_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => GetParent(null));
        }

        [Fact]
        public void Empty_Path_Throws_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() => GetParent(string.Empty));
        }

        [Fact]
        public void ParentOfRoot()
        {
            Assert.Null(GetParent(Path.GetPathRoot(TestDirectory)));
        }

        [Fact]
        public void DotsInPathAreValid()
        {
            var test = GetParent(Path.Combine(TestDirectory, "Test", "..", ".", "Test"));
            Assert.Equal(TestDirectory, test.FullName);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void UNCShares()
        {
            Assert.Null(GetParent(new string(Path.DirectorySeparatorChar, 2) + Path.Combine("Machine", "Test")));
            Assert.Equal("Machine", GetParent(Path.DirectorySeparatorChar + Path.Combine("Machine", "Test")).Name);
        }
    }
}
