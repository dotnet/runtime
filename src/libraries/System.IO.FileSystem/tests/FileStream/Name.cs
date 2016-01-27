// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
        public void NameReturnsUnknownForHandle()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite))
            using (FileStream fsh = new FileStream(fs.SafeFileHandle, FileAccess.ReadWrite))
            {
                Assert.Equal("[Unknown]", fsh.Name);
            }
        }
    }
}
