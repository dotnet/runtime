// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetDirectories_str : Directory_GetFileSystemEntries_str
    {
        protected override bool TestFiles { get { return false; } }
        protected override bool TestDirectories { get { return true; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetDirectories(path);
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void EnumerateWithSymLinkToDirectory()
        {
            using (var containingFolder = new TemporaryDirectory())
            {
                // Test a symlink to a directory that does and then doesn't exist
                using (var targetDir = new TemporaryDirectory())
                {
                    // Create a symlink to a folder that exists
                    string linkPath = Path.Combine(containingFolder.Path, Path.GetRandomFileName());
                    Assert.Equal(0, symlink(targetDir.Path, linkPath));
                    Assert.True(Directory.Exists(linkPath));
                    Assert.Equal(1, GetEntries(containingFolder.Path).Count());
                }

                // The target file is gone and the symlink still exists; since it can't be resolved,
                // it's treated as a file rather than as a directory.
                Assert.Equal(0, GetEntries(containingFolder.Path).Count());
                Assert.Equal(1, Directory.GetFiles(containingFolder.Path).Count());
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(string path1, string path2);
    }

    public class Directory_GetDirectories_str_str : Directory_GetFileSystemEntries_str_str
    {
        protected override bool TestFiles { get { return false; } }
        protected override bool TestDirectories { get { return true; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetDirectories(path, "*");
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.GetDirectories(path, searchPattern);
        }
    }

    public class Directory_GetDirectories_str_str_so : Directory_GetFileSystemEntries_str_str_so
    {
        protected override bool TestFiles { get { return false; } }
        protected override bool TestDirectories { get { return true; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public override string[] GetEntries(string path, string searchPattern)
        {
            return Directory.GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public override string[] GetEntries(string path, string searchPattern, SearchOption option)
        {
            return Directory.GetDirectories(path, searchPattern, option);
        }
    }
}
