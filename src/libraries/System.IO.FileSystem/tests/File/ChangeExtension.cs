// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
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
