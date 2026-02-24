// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;
using System.Security.Principal;
using System.Security.AccessControl;

namespace System.IO.Tests
{
    public partial class Directory_Delete_str_bool : Directory_Delete_str
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void RecursiveDelete_DirectoryContainingJunction()
        {
            // Junctions (NTFS directory junctions) share the IO_REPARSE_TAG_MOUNT_POINT reparse
            // tag with volume mount points, but DeleteVolumeMountPoint only works for volume mount
            // points. Ensure that recursive delete succeeds when the directory contains a junction.
            string target = GetTestFilePath();
            Directory.CreateDirectory(target);

            string linkParent = GetTestFilePath();
            Directory.CreateDirectory(linkParent);

            string junctionPath = Path.Combine(linkParent, GetTestFileName());
            Assert.True(MountHelper.CreateJunction(junctionPath, target));

            // Both the junction and the target exist before deletion
            Assert.True(Directory.Exists(junctionPath), "junction should exist before delete");
            Assert.True(Directory.Exists(target), "target should exist before delete");

            // Recursive delete of the parent should succeed and remove the junction without following it
            Delete(linkParent, recursive: true);

            Assert.False(Directory.Exists(linkParent), "parent should be deleted");
            Assert.True(Directory.Exists(target), "target should still exist after deleting junction");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void RecursiveDelete_NoListDirectoryPermission() // https://github.com/dotnet/runtime/issues/56922
        {
            using WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();

            string parentPath = GetTestFilePath();
            var parent = Directory.CreateDirectory(parentPath);
            var ac = parent.GetAccessControl();
            var rule = new FileSystemAccessRule(currentIdentity.User, FileSystemRights.ListDirectory, AccessControlType.Deny);
            ac.SetAccessRule(rule);
            parent.SetAccessControl(ac);

            var subDir = parent.CreateSubdirectory("subdir");
            File.Create(Path.Combine(subDir.FullName, GetTestFileName())).Dispose();
            Delete(subDir.FullName, recursive: true);
            Assert.False(subDir.Exists);

            // Cleanup
            ac.RemoveAccessRule(rule);
            parent.SetAccessControl(ac);
        }
    }
}
