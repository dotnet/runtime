// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class Directory_GetFiles_str : Directory_GetFileSystemEntries_str
    {
        #region Utilities

        protected override bool TestFiles { get { return true; } }
        protected override bool TestDirectories { get { return false; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetFiles(path);
        }

        #endregion

        #region PlatformSpecific

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void EnumerateWithSymLinkToFile()
        {
            using (var containingFolder = new TemporaryDirectory())
            {
                string linkPath;

                // Test a symlink to a file that does and then doesn't exist
                using (var targetFile = new TemporaryFile())
                {
                    linkPath = Path.Combine(containingFolder.Path, Path.GetRandomFileName());
                    Assert.Equal(0, symlink(targetFile.Path, linkPath));
                    Assert.True(File.Exists(linkPath));
                    Assert.Equal(1, GetEntries(containingFolder.Path).Count());
                }

                // The symlink still exists even though the target file is gone.
                Assert.Equal(1, GetEntries(containingFolder.Path).Count());

                // The symlink is gone
                File.Delete(linkPath);
                Assert.Equal(0, GetEntries(containingFolder.Path).Count());
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(string path1, string path2);

        #endregion
    }
}
