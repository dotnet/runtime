// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class IsReadOnly : FileSystemTest
    {
        protected virtual FileSystemInfo Create(string path)
        {
            return new FileInfo(path);
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void NotReadOnly(char trailingChar)
        {
            string path = GetTestFilePath() + trailingChar;
            File.Create(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Dispose();
            FileInfo info = new FileInfo(path);
            Assert.False(info.IsReadOnly);
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void ReadOnly(char trailingChar)
        {
            string path = GetTestFilePath() + trailingChar;
            string trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            File.Create(trimmedPath).Dispose();
            var attributes = File.GetAttributes(path);
            File.SetAttributes(trimmedPath, attributes | FileAttributes.ReadOnly);
            FileInfo info = null;

            try
            {
                info = new FileInfo(path);
                Assert.True(info.IsReadOnly);
            }
            finally
            {
                File.SetAttributes(trimmedPath, attributes);
            }

            Assert.True(info.IsReadOnly);
            info.Refresh();
            Assert.Equal(attributes, File.GetAttributes(path));
            Assert.False(info.IsReadOnly);
        }

        // In NetFX we ignore "not found" errors, which leaves the attributes
        // state as invalid (0xFFFFFFFF), which makes all flags true.

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingFile(char trailingChar)
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(path + trailingChar);
            Assert.True(info.IsReadOnly);
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void MissingDirectory(char trailingChar)
        {
            string path = GetTestFilePath();
            FileInfo info = new FileInfo(Path.Combine(path, "file" + trailingChar));
            Assert.True(info.IsReadOnly);
        }
    }
}
