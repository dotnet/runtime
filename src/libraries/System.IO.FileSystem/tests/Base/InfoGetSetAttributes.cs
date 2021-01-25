// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Linq;

namespace System.IO.Tests
{
    // Tests that are valid for FileInfo and DirectoryInfo
    public abstract class InfoGetSetAttributes<T> : AllGetSetAttributes where T : FileSystemInfo
    {
        protected abstract T CreateInfo(string path);

        // In .NET Framework we ignore "not found" errors, which leaves the attributes
        // state as invalid (0xFFFFFFFF), which makes all flags true.

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_MissingFile(char trailingChar)
        {
            Assert.Equal((FileAttributes)(-1), GetAttributes(GetTestFilePath() + trailingChar));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_MissingDirectory(char trailingChar)
        {
            Assert.Equal((FileAttributes)(-1), GetAttributes(Path.Combine(GetTestFilePath(), "file" + trailingChar)));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_CreateAfter(char trailingChar)
        {
            string path = GetTestFilePath();
            T info = CreateInfo(trailingChar == 'a' ? path : path + trailingChar);
            CreateItem(path);

            // The actual value will vary depending on the OS and what is running.
            // Archive, NotContentIndexed, etc. might be set.
            Assert.NotEqual((FileAttributes)(-1), info.Attributes);
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_DeleteAfter(char trailingChar)
        {
            string path = CreateItem();
            T info = CreateInfo(trailingChar == 'a' ? path : path + trailingChar);
            DeleteItem(path);
            Assert.Equal((FileAttributes)(-1), info.Attributes);
        }

        public void GetAttributes_DeleteAfterEnumerate()
        {
            // When enumerating we populate the state as we already have it.
            string path = CreateItem();
            FileSystemInfo info = new DirectoryInfo(TestDirectory).EnumerateFileSystemInfos().First();
            DeleteItem(path);

            // The actual value will vary depending on the OS and what is running.
            // Archive, NotContentIndexed, etc. might be set.
            Assert.NotEqual((FileAttributes)(-1), info.Attributes);
            info.Refresh();
            Assert.Equal((FileAttributes)(-1), info.Attributes);
        }
    }
}
