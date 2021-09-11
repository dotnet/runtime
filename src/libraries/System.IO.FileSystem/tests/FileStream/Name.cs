// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Tests;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Name : FileSystemTest
    {
        [Fact]
        public void NameBasicFunctionality()
        {
            string fileName = GetTestFilePath();
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                Assert.Equal(fileName, fs.Name);
            }
        }

        [Fact]
        public void NameNormalizesPath()
        {
            string path = GetTestFilePath();
            string name = Path.GetFileName(path);
            string dir = Path.GetDirectoryName(path);

            string fileName = dir + Path.DirectorySeparatorChar + "." + Path.AltDirectorySeparatorChar + "." + Path.DirectorySeparatorChar + name;

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                Assert.Equal(path, fs.Name);
            }
        }

        [Fact]
        public void ConstructFileStreamFromHandle_NameMatchesOriginal()
        {
            string path = GetTestFilePath();
            using var _ = new ThreadCultureChange(CultureInfo.InvariantCulture);

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            Assert.Equal(path, fs.Name);

            using FileStream fsh = new FileStream(fs.SafeFileHandle, FileAccess.ReadWrite);
            Assert.Equal(path, fsh.Name);
        }

        [Fact]
        public void ConstructFileStreamFromHandleClone_NameReturnsUnknown()
        {
            string path = GetTestFilePath();
            using var _ = new ThreadCultureChange(CultureInfo.InvariantCulture);

            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
            Assert.Equal(path, fs.Name);

            using FileStream fsh = new FileStream(new SafeFileHandle(fs.SafeFileHandle.DangerousGetHandle(), ownsHandle: false), FileAccess.ReadWrite);
            Assert.Equal("[Unknown]", fsh.Name);
        }
    }
}
