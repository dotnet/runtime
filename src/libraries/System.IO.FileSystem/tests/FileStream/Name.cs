// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
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
