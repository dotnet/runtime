// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_CreateText_str : FileSystemTest
    {
        [Fact]
        public void FileInfoInvalidAfterCreateText()
        {
            FileInfo info = new FileInfo(GetTestFilePath());
            using (StreamWriter streamWriter = info.CreateText())
            {
                Assert.True(info.Exists);
            }
        }
    }
}
