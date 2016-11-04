// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_str_fm_fa_fs
    {
        [Fact]
        public void FileShareReadNew()
        {
            // Open with read sharing
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                using (FileStream reader = CreateFileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                { }
            }
        }

        [Fact]
        public void FileShareReadExisting()
        {
            // create the file
            string fileName = GetTestFilePath();
            using (CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            { }

            Assert.True(File.Exists(fileName));

            // Open with read sharing
            using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                using (FileStream reader = CreateFileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                { }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // file sharing restriction limitations on Unix
        public void FileShareWithoutReadThrows()
        {
            string fileName = GetTestFilePath();

            // Open without read sharing
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write | FileShare.Delete))
            {
                FSAssert.ThrowsSharingViolation(() => CreateFileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
            }
        }
    }
}
