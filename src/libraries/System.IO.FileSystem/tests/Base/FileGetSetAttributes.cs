// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    // Tests that are valid for File and FileInfo
    public abstract class FileGetSetAttributes : BaseGetSetAttributes
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void SettingAttributes_Unix_Normal()
        {
            string path = CreateItem();
            AssertSettingAttributes(path, FileAttributes.Normal);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void SettingAttributes_Unix_ReadOnly()
        {
            if (!CanBeReadOnly) return;
            string path = CreateItem();
            AssertSettingAttributes(path, FileAttributes.ReadOnly);
        }

        [Theory]
        [InlineData(FileAttributes.Hidden)]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.FreeBSD)]
        public void SettingAttributes_OSXAndFreeBSD(FileAttributes attributes)
        {
            string path = CreateItem();
            AssertSettingAttributes(path, attributes);
        }

        [Theory]
        [InlineData(FileAttributes.Hidden)]
        [InlineData(FileAttributes.System)]
        [InlineData(FileAttributes.Archive)]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Temporary)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SettingAttributes_Windows(FileAttributes attributes)
        {
            string path = CreateItem();
            AssertSettingAttributes(path, attributes);
        }

        [Theory]
        [InlineData(FileAttributes.ReadOnly)]
        [InlineData(FileAttributes.ReadOnly | FileAttributes.Hidden)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SettingAttributes_Windows_ReadOnly(FileAttributes attributes)
        {
            if (!CanBeReadOnly) return;
            string path = CreateItem();
            AssertSettingAttributes(path, attributes);
        }

        private void AssertSettingAttributes(string path, FileAttributes attributes)
        {
            SetAttributes(path, attributes);
            Assert.Equal(attributes, GetAttributes(path));
            SetAttributes(path, 0);
        }

        [Theory]
        [InlineData(FileAttributes.Temporary)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void SettingInvalidAttributes_Unix(FileAttributes attributes)
        {
            string path = CreateItem();
            AssertSettingInvalidAttributes(path, attributes);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.FileCreateCaseSensitive))]
        [InlineData(FileAttributes.Hidden)]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~(TestPlatforms.OSX | TestPlatforms.FreeBSD))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67853", TestPlatforms.tvOS)]
        public void SettingInvalidAttributes_UnixExceptOSXAndFreeBSD(FileAttributes attributes)
        {
            string path = CreateItem();
            AssertSettingInvalidAttributes(path, attributes);
        }

        [Theory]
        [InlineData(FileAttributes.Normal)]
        [InlineData(FileAttributes.Encrypted)]
        [InlineData(FileAttributes.SparseFile)]
        [InlineData(FileAttributes.ReparsePoint)]
        [InlineData(FileAttributes.Compressed)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SettingInvalidAttributes_Windows(FileAttributes attributes)
        {
            string path = CreateItem();
            AssertSettingInvalidAttributes(path, attributes);
        }

        private void AssertSettingInvalidAttributes(string path, FileAttributes attributes)
        {
            SetAttributes(path, attributes);
            Assert.Equal(FileAttributes.Normal, GetAttributes(path));
        }

        [Theory,
            InlineData(":bar"),
            InlineData(":bar:$DATA")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GettingAndSettingAttributes_AlternateDataStream_Windows(string streamName)
        {
            string path = CreateItem();
            streamName = path + streamName;
            File.Create(streamName).Dispose();

            FileAttributes attributes = GetAttributes(streamName);
            Assert.NotEqual((FileAttributes)0, attributes);
            Assert.NotEqual((FileAttributes)(-1), attributes);

            // Attributes are shared for the file and all streams
            SetAttributes(streamName, FileAttributes.Hidden);
            Assert.Equal(FileAttributes.Hidden, GetAttributes(streamName));
            Assert.Equal(FileAttributes.Hidden, GetAttributes(path));

            SetAttributes(path, FileAttributes.Normal);
            Assert.Equal(FileAttributes.Normal, GetAttributes(streamName));
            Assert.Equal(FileAttributes.Normal, GetAttributes(path));
        }
    }
}
