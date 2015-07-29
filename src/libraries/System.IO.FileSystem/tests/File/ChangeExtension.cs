// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_ChangeExtension : FileSystemTest
    {
        [Theory]
        [InlineData("", ".tmp", "")]
        [InlineData(null, ".tmp", null)]
        [InlineData("filename", ".tmp", "filename.tmp")]
        [InlineData("filename.tmp", "", "filename.")]
        [InlineData("filename.tmp", "...", "filename...")]
        [InlineData("filename.tmp", null, "filename")]
        [InlineData("filename.tmp.doc", ".tmp", "filename.tmp.tmp")]
        public void ValidExtensions(string original, string newExtension, string expected)
        {
            string newPath = Path.ChangeExtension(original, newExtension);
            Assert.Equal(expected, newPath);
        }
    }
}
