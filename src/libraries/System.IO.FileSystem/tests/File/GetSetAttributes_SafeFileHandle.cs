// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public sealed class GetSetAttributes_SafeFileHandle : BaseGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path)
        {
            using var fileHandle =
                File.OpenHandle(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            return File.GetAttributes(fileHandle);
        }

        protected override void SetAttributes(string path, FileAttributes attributes)
        {
            using var fileHandle =
                File.OpenHandle(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            File.SetAttributes(fileHandle, attributes);
        }

        // Getting only throws for File, not FileInfo
        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_MissingFile(char trailingChar)
        {
            Assert.Throws<FileNotFoundException>(() => GetAttributes(GetTestFilePath() + trailingChar));
        }

        // Getting only throws for File, not FileInfo
        [Theory,
         InlineData(":bar"),
         InlineData(":bar:$DATA")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetAttributes_MissingAlternateDataStream_Windows(string streamName)
        {
            string path = CreateItem();
            streamName = path + streamName;

            Assert.Throws<FileNotFoundException>(() => GetAttributes(streamName));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_MissingDirectory(char trailingChar)
        {
            Assert.Throws<DirectoryNotFoundException>(() => GetAttributes(Path.Combine(GetTestFilePath(), "dir" + trailingChar)));
        }
    }
}
