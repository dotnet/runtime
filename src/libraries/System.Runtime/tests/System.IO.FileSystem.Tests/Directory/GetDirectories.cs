// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetDirectories_str : Directory_GetFileSystemEntries_str
    {
        protected override bool TestFiles { get { return false; } }
        protected override bool TestDirectories { get { return true; } }

        public override string[] GetEntries(string path)
        {
            return Directory.GetDirectories(path);
        }

        [ConditionalFact(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        public void EnumerateWithSymLinkToDirectory()
        {
            DirectoryInfo containingFolder = Directory.CreateDirectory(GetTestFilePath());

            // Test a symlink to a directory that does and then doesn't exist
            DirectoryInfo targetDir = Directory.CreateDirectory(GetTestFilePath());
            {
                // Create a symlink to a folder that exists
                string linkPath = Path.Combine(containingFolder.FullName, GetRandomLinkName());
                Assert.True(MountHelper.CreateSymbolicLink(linkPath, targetDir.FullName, isDirectory: true));

                Assert.True(Directory.Exists(linkPath));
                Assert.Equal(1, GetEntries(containingFolder.FullName).Count());
            }
            targetDir.Delete(recursive: true);

            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(1, GetEntries(containingFolder.FullName).Count());
                Assert.Equal(0, Directory.GetFiles(containingFolder.FullName).Count());
            }
            else
            {
                // The target file is gone and the symlink still exists; since it can't be resolved,
                // on Unix it's treated as a file rather than as a directory.
                Assert.Equal(0, GetEntries(containingFolder.FullName).Count());
                Assert.Equal(1, Directory.GetFiles(containingFolder.FullName).Count());
            }
        }
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
