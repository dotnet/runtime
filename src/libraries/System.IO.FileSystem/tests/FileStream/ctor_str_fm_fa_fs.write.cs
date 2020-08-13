// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40065", TestPlatforms.Browser)]
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
