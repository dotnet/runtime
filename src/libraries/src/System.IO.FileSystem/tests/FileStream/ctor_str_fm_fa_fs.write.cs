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
        public void FileShareWriteNew()
        {
            // Open with write sharing
            string fileName = GetTestFilePath();
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                using (FileStream reader = CreateFileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                { }
            }
        }

        [Fact]
        public void FileShareWriteExisting()
        {
            // create the file
            string fileName = GetTestFilePath();
            using (CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            { }

            Assert.True(File.Exists(fileName));

            // Open with read sharing
            using (FileStream fs = CreateFileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Write))
            {
                using (FileStream reader = CreateFileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                { }
            }
        }

        [Fact]
        public void FileShareWithoutWriteThrows()
        {
            string fileName = GetTestFilePath();

            // Open without write sharing
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                FSAssert.ThrowsSharingViolation(() => CreateFileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete));
            }
            
            // Then try the other way around
            using (FileStream fs = CreateFileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                FSAssert.ThrowsSharingViolation(() => CreateFileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.None));
            }
        }
    }
}
