// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class File_GetSetAttributes : BaseGetSetAttributes
    {
        protected override FileAttributes GetAttributes(string path) => File.GetAttributes(path);
#if TargetsWindows
        protected FileAttributes GetAttributes(SafeFileHandle fileHandle) => File.GetAttributes(fileHandle);
#endif
        protected override void SetAttributes(string path, FileAttributes attributes) => File.SetAttributes(path, attributes);
#if TargetsWindows
        protected void SetAttributes(SafeFileHandle fileHandle, FileAttributes attributes) => File.SetAttributes(fileHandle, attributes);
#endif
        // Getting only throws for File, not FileInfo
        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_MissingFile(char trailingChar)
        {
            Assert.Throws<FileNotFoundException>(() => GetAttributes(GetTestFilePath() + trailingChar));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetAttributes_MissingFile_SafeFileHandle(char trailingChar)
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                using var fileHandle = File.OpenHandle(GetTestFilePath() + trailingChar);
                GetAttributes(fileHandle);
            });
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

        [Theory,
         InlineData(":bar"),
         InlineData(":bar:$DATA")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetAttributes_MissingAlternateDataStream_Windows_SafeFileHandle(string streamName)
        {
            string path = CreateItem();
            streamName = path + streamName;

            Assert.Throws<FileNotFoundException>(() =>
            {
                using var fileHandle = File.OpenHandle(streamName);
                GetAttributes(fileHandle);
            });
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        public void GetAttributes_MissingDirectory(char trailingChar)
        {
            Assert.Throws<DirectoryNotFoundException>(() => GetAttributes(Path.Combine(GetTestFilePath(), "dir" + trailingChar)));
        }

        [Theory, MemberData(nameof(TrailingCharacters))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetAttributes_MissingDirectory_SafeFileHandle(char trailingChar)
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                using var fileHandle = File.OpenHandle(Path.Combine(GetTestFilePath(), "dir" + trailingChar));
                GetAttributes(fileHandle);
            });
        }
    }
}
