// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Xunit;

namespace System.IO.Tests
{
    partial class File_Copy_Single
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public unsafe void WindowsCheckSparseness()
        {
            string sourceFile = GetTestFilePath();
            string destFile = GetTestFilePath();

            File.WriteAllText(sourceFile, "abc");
            File.WriteAllText(destFile, "def");

            Assert.True((File.GetAttributes(sourceFile) & FileAttributes.SparseFile) == 0);
            File.Copy(sourceFile, destFile, true);
            Assert.True((File.GetAttributes(destFile) & FileAttributes.SparseFile) == 0);
            Assert.Equal("abc", File.ReadAllText(sourceFile));

            using (FileStream file = File.Open(sourceFile, FileMode.Open))
            {
                Assert.True(Interop.Kernel32.DeviceIoControl(file.SafeFileHandle, Interop.Kernel32.FSCTL_SET_SPARSE, null, 0, null, 0, out _, 0));
            }
            File.WriteAllText(destFile, "def");

            Assert.True((File.GetAttributes(sourceFile) & FileAttributes.SparseFile) != 0);
            File.Copy(sourceFile, destFile, true);
            Assert.True((File.GetAttributes(destFile) & FileAttributes.SparseFile) != 0);
            Assert.Equal("abc", File.ReadAllText(sourceFile));
        }

        // Todo: add a way to run all these on ReFS, and a test to check we actually cloned the reference, not just the data on ReFS.
    }
}
