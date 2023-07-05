// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetAttributes : BaseGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path) => File.GetAttributes(path);
        protected override void SetAttributes(string path, FileAttributes attributes) => File.SetAttributes(path, attributes);
        protected override bool CanBeReadOnly => false;

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
