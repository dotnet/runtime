// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetDirectories_str : Directory_GetFileSystemEntries_str
    {
        #region Utilities

        protected override bool TestFiles { get { return false; } }
        protected override bool TestDirectories { get { return true; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetDirectories(path);
        }

        #endregion

        #region PlatformSpecific

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

    #endregion
}
