// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_GetSetAttributes : InfoGetSetAttributes<FileInfo>
    {
        protected override bool CanBeReadOnly => true;
        protected override FileAttributes GetAttributes(string path) => new FileInfo(path).Attributes;
        protected override void SetAttributes(string path, FileAttributes attributes) => new FileInfo(path).Attributes = attributes;
        protected override FileInfo CreateInfo(string path) => new FileInfo(path);

        [Fact]
        public void IsReadOnly_SetAndGet()
        {
            FileInfo test = new FileInfo(GetTestFilePath());
            test.Create().Dispose();

            // Set to True
            test.IsReadOnly = true;
            test.Refresh();
            Assert.True(test.IsReadOnly);

            // Set To False
            test.IsReadOnly = false;
            test.Refresh();
            Assert.False(test.IsReadOnly);
        }

        [Theory]
        [InlineData(".", true)]
        [InlineData("", false)]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void HiddenAttributeSetCorrectly_OSX(string filePrefix, bool hidden)
        {
            string testFilePath = Path.Combine(TestDirectory, $"{filePrefix}{GetTestFileName()}");
            FileInfo fileInfo = new FileInfo(testFilePath);
            fileInfo.Create().Dispose();

            Assert.Equal(hidden, (fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
        }
    }
}
