// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Windows.Storage;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnTargetFramework(~(TargetFrameworkMonikers.Uap | TargetFrameworkMonikers.UapAot))]
    public partial class WinRT_BrokeredFunctions : FileSystemTest
    {
        private static string s_musicFolder = StorageLibrary.GetLibraryAsync(KnownLibraryId.Music).AsTask().Result.SaveFolder.Path;

        [Fact]
        public void CopyFile_ToBrokeredLocation()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            string destination = Path.Combine(s_musicFolder, "CopyToBrokeredLocation_" + Path.GetRandomFileName());
            try
            {
                Assert.False(File.Exists(destination), "destination shouldn't exist before copying");
                File.Copy(testFile, destination);
                Assert.True(File.Exists(testFile), "testFile should exist after copying");
                Assert.True(File.Exists(destination), "destination should exist after copying");
            }
            finally
            {
                File.Delete(destination);
            }
        }

        [Fact]
        public void CreateDirectory()
        {
            string testFolder = Path.Combine(s_musicFolder, "CreateDirectory_" + Path.GetRandomFileName());
            try
            {
                Assert.False(Directory.Exists(testFolder), "destination shouldn't exist");
                Directory.CreateDirectory(testFolder);
                Assert.True(Directory.Exists(testFolder), "destination should exist");
            }
            finally
            {
                Directory.Delete(testFolder);
            }
        }

        [Fact]
        public void DeleteFile()
        {
            string testFile = Path.Combine(s_musicFolder, "DeleteFile_" + Path.GetRandomFileName());
            CreateFileInBrokeredLocation(testFile);
            Assert.True(File.Exists(testFile), "testFile should exist before deleting");
            File.Delete(testFile);
            Assert.False(File.Exists(testFile), "testFile shouldn't exist after deleting");
        }

        [Fact]
        public void FindFirstFile()
        {
            string subFolder = Path.Combine(s_musicFolder, "FindFirstFile_SubFolder_" + Path.GetRandomFileName());
            Directory.CreateDirectory(subFolder);
            string testFile = null;

            try
            {
                testFile = Path.Combine(subFolder, "FindFirstFile_SubFile_" + Path.GetRandomFileName());
                CreateFileInBrokeredLocation(testFile);
                Assert.True(File.Exists(testFile), "testFile should exist");
            }
            finally
            {
                Directory.Delete(subFolder, true);
            }
            Assert.False(File.Exists(testFile), "testFile shouldn't exist after a recursive delete");
            Assert.False(Directory.Exists(subFolder), "subFolder shouldn't exist after a recursive delete");
        }

        [Fact]
        [ActiveIssue(23444)]
        public void GetFileAttributesEx()
        {
            string destination = Path.Combine(s_musicFolder, "GetFileAttributesEx_" + Path.GetRandomFileName());
            CreateFileInBrokeredLocation(destination);
            try
            {
                FileAttributes attr = File.GetAttributes(destination);
                Assert.False((attr & FileAttributes.ReadOnly) == 0, "new file in brokered location should not be readonly");
            }
            finally
            {
                File.Delete(destination);
            }
        }

        [Fact]
        public void MoveFile()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            string destination = Path.Combine(s_musicFolder, "MoveFile_" + Path.GetRandomFileName());
            try
            {
                Assert.False(File.Exists(destination), "destination shouldn't exist before moving");
                File.Move(testFile, destination);
                Assert.False(File.Exists(testFile), "testFile shouldn't exist after moving");
                Assert.True(File.Exists(destination), "destination should exist after moving");
            }
            finally
            {
                File.Delete(destination);
            }
        }

        [Fact]
        public void RemoveDirectory()
        {
            string testFolder = Path.Combine(s_musicFolder, "CreateDirectory_" + Path.GetRandomFileName());
            Assert.False(Directory.Exists(testFolder), "destination shouldn't exist");
            Directory.CreateDirectory(testFolder);
            Assert.True(Directory.Exists(testFolder), "destination should exist");
            Directory.Delete(testFolder);
            Assert.False(Directory.Exists(testFolder), "destination shouldn't exist");
        }

        [Fact]
        public void ReplaceFile()
        {
            string testFile = GetTestFilePath();
            File.Create(testFile).Dispose();

            string destination = Path.Combine(s_musicFolder, "ReplaceFile_" + Path.GetRandomFileName());
            File.Copy(testFile, destination);

            // Need to be on the same drive
            Assert.Equal(testFile[0], destination[0]);

            string destinationBackup = Path.Combine(s_musicFolder, "ReplaceFile_" + Path.GetRandomFileName());
            try
            {
                Assert.True(File.Exists(destination), "destination should exist before replacing");
                Assert.False(File.Exists(destinationBackup), "destination shouldn't exist before replacing");
                File.Replace(testFile, destination, destinationBackup);
                Assert.False(File.Exists(testFile), "testFile shouldn't exist after replacing");
                Assert.True(File.Exists(destination), "destination should exist after replacing");
                Assert.True(File.Exists(destinationBackup), "destinationBackup should exist after replacing");
            }
            finally
            {
                File.Delete(destination);
                File.Delete(destinationBackup);
            }
        }

        [Fact]
        [ActiveIssue(23444)]
        public void SetFileAttributes()
        {
            string destination = Path.Combine(s_musicFolder, "SetFileAttributes_" + Path.GetRandomFileName());
            CreateFileInBrokeredLocation(destination);
            FileAttributes attr = File.GetAttributes(destination);
            try
            {
                Assert.False(((attr & FileAttributes.ReadOnly) > 0), "new file in brokered location should not be readonly");
                File.SetAttributes(destination, attr | FileAttributes.ReadOnly);
                Assert.True(((File.GetAttributes(destination) & FileAttributes.ReadOnly) > 0), "file in brokered location should be readonly after setting FileAttributes");
            }
            finally
            {
                File.SetAttributes(destination, attr);
                Assert.False(((File.GetAttributes(destination) & FileAttributes.ReadOnly) > 0), "file in brokered location should NOT be readonly after setting FileAttributes");
                File.Delete(destination);
            }
        }

        private void CreateFileInBrokeredLocation(string path)
        {
            // Temporary hack until FileStream is updated to support brokering
            string testFile = GetTestFilePath();
            File.WriteAllText(testFile, "CoreFX test file");
            File.Copy(testFile, path);
        }

        // Temporarily blocking until the CoreCLR change is made [Fact]
        public void WriteReadAllText()
        {
            string destination = Path.Combine(s_musicFolder, "WriteReadAllText_" + Path.GetRandomFileName());
            string content = "WriteReadAllText";
            File.WriteAllText(destination, content);
            try
            {
                Assert.True(File.Exists(destination));
                Assert.Equal(content, File.ReadAllText(destination));
            }
            finally
            {
                File.Delete(destination);
            }
        }
    }
}
