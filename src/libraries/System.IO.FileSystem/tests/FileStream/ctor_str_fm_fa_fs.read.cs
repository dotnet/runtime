// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
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
