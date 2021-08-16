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
        public void RecursiveDelete_NoListDirectoryPermission() // https://github.com/dotnet/runtime/issues/56922
        {
            string parentPath = GetTestFilePath();
            var parent = Directory.CreateDirectory(parentPath);
            var ac = parent.GetAccessControl();
            ac.SetAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.ListDirectory, AccessControlType.Deny));
            parent.SetAccessControl(ac);

            var subDir = parent.CreateSubdirectory("subdir");
            File.Create(Path.Combine(subDir.FullName, GetTestFileName())).Dispose();
            Delete(subDir.FullName, recursive: true);
            Assert.False(subDir.Exists);
        }
    }
}
