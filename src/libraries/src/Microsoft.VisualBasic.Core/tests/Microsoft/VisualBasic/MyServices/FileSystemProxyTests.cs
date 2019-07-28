// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using Xunit;

namespace Microsoft.VisualBasic.MyServices.Tests
{
    // File tests cloned from Microsoft.VisualBasic.FileIO.Tests.FileSystemTests.
    public class FileSystemProxyTests : System.IO.FileCleanupTestBase
    {
        private static readonly string DestData = "xXy";
        private static readonly string SourceData = "aAb";

        private readonly FileSystemProxy _fileSystem = new Microsoft.VisualBasic.Devices.ServerComputer().FileSystem;

        private static bool HasExpectedData(string FileNameWithPath, string ExpectedData)
        {
            string actualData = System.IO.File.ReadAllText(FileNameWithPath);
            return ExpectedData == actualData;
        }

        private static void WriteFile(string FileName, string TestData)
        {
            System.IO.File.WriteAllText(FileName, TestData);
        }

        [Fact]
        public void CombinePathTest_BadBaseDirectory_RelativePath()
        {
            Assert.Throws<ArgumentNullException>(() => _fileSystem.CombinePath(null, "Test2"));
            Assert.Throws<ArgumentNullException>(() => _fileSystem.CombinePath("", "Test2"));
        }

        [Fact]
        public void CombinePathTest_BaseDirectory_RelativePath()
        {
            var TestDirInfo = new System.IO.DirectoryInfo(TestDirectory);
            var Root = TestDirInfo.Root.Name;
            Assert.Equal(_fileSystem.CombinePath(Root, "Test2"), System.IO.Path.Combine(Root, "Test2"));
        }

        [Fact]
        public void CombinePathTest_RootDirectory_RelativePath()
        {
            Assert.Equal(_fileSystem.CombinePath(TestDirectory, null), TestDirectory);
            Assert.Equal(_fileSystem.CombinePath(TestDirectory, ""), TestDirectory);
            Assert.Equal(_fileSystem.CombinePath(TestDirectory, "Test"), System.IO.Path.Combine(TestDirectory, "Test"));
        }

        [Fact]
        public void CopyDirectory_SourceDirectoryName_DestinationDirectoryName()
        {
            var FullPathToSourceDirectory = System.IO.Path.Combine(TestDirectory, "SourceDirectory");
            System.IO.Directory.CreateDirectory(FullPathToSourceDirectory);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: "SourceDirectory", TestFileName: $"NewFile{i}");
            }
            var FullPathToTargetDirectory = System.IO.Path.Combine(TestDirectory, "TargetDirectory");
            _fileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory);
            Assert.Equal(System.IO.Directory.GetFiles(FullPathToSourceDirectory).Length, System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length);
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                // Ensure copy transferred written data
                Assert.True(HasExpectedData(CurrentFile, SourceData));
            }
            System.IO.Directory.Delete(FullPathToTargetDirectory, recursive: true);
            System.IO.Directory.CreateDirectory(FullPathToTargetDirectory);
            CreateTestFile(TestData: SourceData, PathFromBase: "TargetDirectory", TestFileName: $"NewFile0");
            Assert.Throws<System.IO.IOException>(() => _fileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory));
        }

        [Fact]
        public void CopyDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteFalse()
        {
            var FullPathToSourceDirectory = System.IO.Path.Combine(TestDirectory, "SourceDirectory");
            var FullPathToTargetDirectory = System.IO.Path.Combine(TestDirectory, "TargetDirectory");
            System.IO.Directory.CreateDirectory(FullPathToSourceDirectory);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: "SourceDirectory", TestFileName: $"NewFile{i}");
            }
            _fileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite: false);
            Assert.Equal(System.IO.Directory.GetFiles(FullPathToSourceDirectory).Length, System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length);
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                // Ensure copy transferred written data
                Assert.True(HasExpectedData(CurrentFile, SourceData));
            }
            System.IO.Directory.Delete(FullPathToTargetDirectory, recursive: true);
            System.IO.Directory.CreateDirectory(FullPathToTargetDirectory);
            CreateTestFile(DestData, PathFromBase: "TargetDirectory", TestFileName: $"NewFile0");
            Assert.Throws<System.IO.IOException>(() => _fileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite: false));
            Assert.Equal(System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length, System.IO.Directory.GetFiles(FullPathToSourceDirectory).Length);
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                Assert.True(HasExpectedData(CurrentFile, CurrentFile.EndsWith("0") ? DestData : SourceData));
            }
        }

        [Fact]
        public void CopyDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteTrue()
        {
            var FullPathToSourceDirectory = System.IO.Path.Combine(TestDirectory, "SourceDirectory");
            var FullPathToTargetDirectory = System.IO.Path.Combine(TestDirectory, "TargetDirectory");
            System.IO.Directory.CreateDirectory(FullPathToSourceDirectory);
            System.IO.Directory.CreateDirectory(FullPathToTargetDirectory);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: "SourceDirectory", TestFileName: $"NewFile{i}");
            }
            _fileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite: true);
            Assert.Equal(System.IO.Directory.GetFiles(FullPathToSourceDirectory).Length, System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length);
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                // Ensure copy transferred written data
                Assert.True(HasExpectedData(CurrentFile, SourceData));
            }
        }

        [Fact]
        public void CopyFile_FileSourceFileName_DestinationFileName()
        {
            var testFileSource = GetTestFilePath();
            var testFileDest = GetTestFilePath();

            // Write and copy file
            WriteFile(testFileSource, SourceData);
            WriteFile(testFileDest, DestData);
            Assert.Throws<System.IO.IOException>(() => _fileSystem.CopyFile(testFileSource, testFileDest));

            // Ensure copy didn't overwrite existing data
            Assert.True(HasExpectedData(testFileDest, DestData));

            // Get a new destination name
            testFileDest = GetTestFilePath();
            _fileSystem.CopyFile(testFileSource, testFileDest);

            // Ensure copy transferred written data
            Assert.True(HasExpectedData(testFileDest, SourceData));
        }

        [Fact]
        public void CopyFile_FileSourceFileName_DestinationFileName_OverwriteFalse()
        {
            var testFileSource = GetTestFilePath();
            var testFileDest = GetTestFilePath();

            // Write and copy file
            WriteFile(testFileSource, SourceData);
            WriteFile(testFileDest, DestData);
            Assert.Throws<System.IO.IOException>(() => _fileSystem.CopyFile(testFileSource, testFileDest, overwrite: false));

            // Ensure copy didn't overwrite existing data
            Assert.True(HasExpectedData(testFileDest, DestData));
        }

        [Fact]
        public void CopyFile_FileSourceFileName_DestinationFileName_OverwriteTrue()
        {
            var testFileSource = GetTestFilePath();
            var testFileDest = GetTestFilePath();

            // Write and copy file
            WriteFile(testFileSource, SourceData);
            WriteFile(testFileDest, DestData);
            _fileSystem.CopyFile(testFileSource, testFileDest, overwrite: true);

            // Ensure copy transferred written data
            Assert.True(HasExpectedData(testFileDest, SourceData));
        }

        [Fact]
        public void CreateDirectory_Directory()
        {
            var FullPathToNewDirectory = System.IO.Path.Combine(TestDirectory, "NewDirectory");
            Assert.False(System.IO.Directory.Exists(FullPathToNewDirectory));
            _fileSystem.CreateDirectory(FullPathToNewDirectory);
            Assert.True(System.IO.Directory.Exists(FullPathToNewDirectory));
        }

        // Can't get current directory on OSX before setting it.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotOSX))]
        public void CurrentDirectoryGet()
        {
            var CurrentDirectory = System.IO.Directory.GetCurrentDirectory();
            Assert.Equal(_fileSystem.CurrentDirectory, CurrentDirectory);
        }

        // On OSX, the temp directory /tmp/ is a symlink to /private/tmp, so setting the current
        // directory to a symlinked path will result in GetCurrentDirectory returning the absolute
        // path that followed the symlink.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotOSX))]
        public void CurrentDirectorySet()
        {
            var SavedCurrentDirectory = System.IO.Directory.GetCurrentDirectory();
            _fileSystem.CurrentDirectory = TestDirectory;
            Assert.Equal(TestDirectory, _fileSystem.CurrentDirectory);
            _fileSystem.CurrentDirectory = SavedCurrentDirectory;
            Assert.Equal(_fileSystem.CurrentDirectory, SavedCurrentDirectory);
        }

        [Fact]
        public void DeleteDirectory_Directory_DeleteAllContents()
        {
            var FullPathToNewDirectory = System.IO.Path.Combine(TestDirectory, "NewDirectory");
            System.IO.Directory.CreateDirectory(FullPathToNewDirectory);
            Assert.True(System.IO.Directory.Exists(FullPathToNewDirectory));
            var testFileSource = CreateTestFile(SourceData, PathFromBase: "NewDirectory", TestFileName: "TestFile");
            Assert.True(System.IO.File.Exists(testFileSource));
            _fileSystem.DeleteDirectory(FullPathToNewDirectory, DeleteDirectoryOption.DeleteAllContents);
            Assert.False(System.IO.Directory.Exists(FullPathToNewDirectory));
        }

        [Fact]
        public void DeleteDirectory_Directory_ThrowIfDirectoryNonEmpty()
        {
            var FullPathToNewDirectory = System.IO.Path.Combine(TestDirectory, "NewDirectory");
            _fileSystem.CreateDirectory(FullPathToNewDirectory);
            Assert.True(System.IO.Directory.Exists(FullPathToNewDirectory));
            var testFileSource = CreateTestFile(SourceData, PathFromBase: "NewDirectory", TestFileName: "TestFile");

            Assert.True(System.IO.File.Exists(testFileSource));
            Assert.Throws<System.IO.IOException>(() => _fileSystem.DeleteDirectory(FullPathToNewDirectory, DeleteDirectoryOption.ThrowIfDirectoryNonEmpty));
            Assert.True(System.IO.Directory.Exists(FullPathToNewDirectory));
            Assert.True(System.IO.File.Exists(testFileSource));
        }

        [Fact]
        public void DeleteFile_File()
        {
            var testFileSource = CreateTestFile(SourceData, TestFileName: GetTestFileName());

            Assert.True(System.IO.File.Exists(testFileSource));
            _fileSystem.DeleteFile(testFileSource);
            Assert.False(System.IO.File.Exists(testFileSource));
        }

        [Fact]
        public void DirectoryExists_Directory()
        {
            Assert.True(_fileSystem.DirectoryExists(TestDirectory));
            Assert.False(_fileSystem.DirectoryExists(System.IO.Path.Combine(TestDirectory, "NewDirectory")));
        }

        // Not tested:
        //   public System.Collections.ObjectModel.ReadOnlyCollection<System.IO.DriveInfo> Drives { get { throw null; } }

        [Fact]
        public void FileExists_File()
        {
            var testFileSource = CreateTestFile(SourceData, TestFileName: GetTestFileName());
            Assert.True(_fileSystem.FileExists(testFileSource));
            _fileSystem.FileExists(testFileSource);
            System.IO.File.Delete(testFileSource);
            Assert.False(_fileSystem.FileExists(testFileSource));
        }

        // Not tested:
        //   public System.Collections.ObjectModel.ReadOnlyCollection<string> FindInFiles(string directory, string containsText, bool ignoreCase, FileIO.SearchOption searchType) { throw null; }
        //   public System.Collections.ObjectModel.ReadOnlyCollection<string> FindInFiles(string directory, string containsText, bool ignoreCase, FileIO.SearchOption searchType, params string[] fileWildcards) { throw null; }

        [Fact]
        public void GetDirectories_Directory()
        {
            var DirectoryList = _fileSystem.GetDirectories(TestDirectory);
            Assert.Equal(0, DirectoryList.Count);
            for (int i = 0; i < 6; i++)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"GetDirectories_DirectoryNewSubDirectory{i}"));
            }
            DirectoryList = _fileSystem.GetDirectories(TestDirectory);
            Assert.Equal(6, DirectoryList.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.True(DirectoryList.Contains(System.IO.Path.Combine(TestDirectory, $"GetDirectories_DirectoryNewSubDirectory{i}")));
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"GetDirectories_DirectoryNewSubDirectory0", $"NewSubSubDirectory"));
            DirectoryList = _fileSystem.GetDirectories(TestDirectory);
            Assert.Equal(6, DirectoryList.Count);
        }

        [Fact]
        public void GetDirectories_Directory_SearchOption()
        {
            var DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchTopLevelOnly);
            Assert.Equal(0, DirectoryList.Count);
            for (int i = 0; i < 6; i++)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"GetDirectories_Directory_SearchOptionNewSubDirectory{i}"));
            }
            DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchTopLevelOnly);
            Assert.Equal(6, DirectoryList.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.True(DirectoryList.Contains(System.IO.Path.Combine(TestDirectory, $"GetDirectories_Directory_SearchOptionNewSubDirectory{i}")));
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"GetDirectories_Directory_SearchOptionNewSubDirectory0", $"NewSubSubDirectory"));
            DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchTopLevelOnly);
            Assert.Equal(6, DirectoryList.Count);
            DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchAllSubDirectories);
            Assert.Equal(7, DirectoryList.Count);
        }

        [Fact]
        public void GetDirectories_Directory_SearchOption_Wildcards()
        {
            var DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchTopLevelOnly, "*");
            Assert.Equal(0, DirectoryList.Count);
            var CreatedDirectories = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                CreatedDirectories.Add(System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"NewSubDirectory00{i}")).Name);
            }
            DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchTopLevelOnly, "*000", "*001");
            Assert.Equal(2, DirectoryList.Count);
            for (int i = 0; i < 2; i++)
            {
                var DirectoryName = System.IO.Path.Combine(TestDirectory, $"NewSubDirectory00{i}");
                Assert.True(DirectoryList.Contains(DirectoryName), $"{DirectoryName} Is missing from Wildcard Search");
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"NewSubDirectory000", $"NewSubSubDirectory000"));
            DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchTopLevelOnly, "*000");
            Assert.Equal(1, DirectoryList.Count);
            DirectoryList = _fileSystem.GetDirectories(TestDirectory, SearchOption.SearchAllSubDirectories, "*000");
            Assert.Equal(2, DirectoryList.Count);
        }

        [Fact]
        public void GetDirectoryInfo_Directory()
        {
            for (int i = 0; i < 6; i++)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"NewSubDirectory{i}"));
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, $"NewSubDirectory0", $"NewSubSubDirectory"));
            var info = _fileSystem.GetDirectoryInfo(TestDirectory);
            var infoFromIO = new System.IO.DirectoryInfo(TestDirectory);
            Assert.Equal(info.CreationTime, infoFromIO.CreationTime);
            Assert.Equal(info.Extension, infoFromIO.Extension);
            Assert.Equal(info.FullName, TestDirectory);
            Assert.Equal(info.LastAccessTime, infoFromIO.LastAccessTime);
            Assert.Equal(info.Name, infoFromIO.Name);
            Assert.Equal(info.Parent.ToString(), infoFromIO.Parent.ToString());
            Assert.Equal(info.Root.Name, infoFromIO.Root.Name);
        }

        [Fact]
        public void GetDriveInfo_Drive()
        {
            var Drives = System.IO.DriveInfo.GetDrives();
            Assert.True(Drives.Length > 0);
            Assert.Equal(_fileSystem.GetDriveInfo(Drives[0].Name).Name, new System.IO.DriveInfo(Drives[0].Name).Name);
        }

        [Fact]
        public void GetFileInfo_File()
        {
            var TestFile = CreateTestFile(SourceData, TestFileName: GetTestFileName());

            var FileInfoFromSystemIO = new System.IO.FileInfo(TestFile);
            Assert.NotNull(FileInfoFromSystemIO);

            var info = _fileSystem.GetFileInfo(TestFile);
            Assert.NotNull(info);
            Assert.True(info.Exists);
            Assert.Equal(info.Attributes, FileInfoFromSystemIO.Attributes);
            Assert.Equal(info.CreationTime, FileInfoFromSystemIO.CreationTime);
            Assert.True(info.CreationTime > DateTime.MinValue);
            Assert.Equal(info.DirectoryName, FileInfoFromSystemIO.DirectoryName);
            Assert.Equal(info.Extension, FileInfoFromSystemIO.Extension);
            Assert.Equal(info.FullName, FileInfoFromSystemIO.FullName);
            Assert.Equal(info.IsReadOnly, FileInfoFromSystemIO.IsReadOnly);
            Assert.Equal(info.LastAccessTime, FileInfoFromSystemIO.LastAccessTime);
            Assert.Equal(info.LastWriteTime, FileInfoFromSystemIO.LastWriteTime);
            Assert.Equal(info.Length, FileInfoFromSystemIO.Length);
            Assert.Equal(info.Name, FileInfoFromSystemIO.Name);
        }

        [Fact]
        public void GetFiles_Directory()
        {
            var FileList = _fileSystem.GetFiles(TestDirectory);
            Assert.Equal(0, FileList.Count);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: null, TestFileName: $"NewFile{i}");
            }
            FileList = _fileSystem.GetFiles(TestDirectory);
            Assert.Equal(6, FileList.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.True(FileList.Contains(System.IO.Path.Combine(TestDirectory, $"NewFile{i}")));
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, "GetFiles_DirectoryNewSubDirectory"));
            CreateTestFile(SourceData, PathFromBase: "GetFiles_DirectoryNewSubDirectory", TestFileName: "NewFile");
            FileList = _fileSystem.GetFiles(TestDirectory);
            Assert.Equal(6, FileList.Count);
        }

        [Fact]
        public void GetFiles_Directory_SearchOption()
        {
            var NewSubDirectoryPath = System.IO.Path.Combine(TestDirectory, "GetFiles_Directory_SearchOptionNewSubDirectory");
            System.IO.Directory.CreateDirectory(NewSubDirectoryPath);
            CreateTestFile(SourceData, PathFromBase: "GetFiles_Directory_SearchOptionNewSubDirectory", TestFileName: "NewFile");
            var FileList = _fileSystem.GetFiles(TestDirectory);
            Assert.Equal(0, FileList.Count);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: null, TestFileName: $"NewFile{i}");
            }
            FileList = _fileSystem.GetFiles(TestDirectory, SearchOption.SearchTopLevelOnly);
            CreateTestFile(SourceData, PathFromBase: null, TestFileName: "NewFile");
            Assert.Equal(6, FileList.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.True(FileList.Contains(System.IO.Path.Combine(TestDirectory, $"NewFile{i}")));
            }
            FileList = _fileSystem.GetFiles(TestDirectory, SearchOption.SearchAllSubDirectories);
            Assert.Equal(8, FileList.Count);
            for (int i = 0; i < 7; i++)
            {
                Assert.True(System.IO.File.Exists(FileList[i]));
            }
        }

        [Fact]
        public void GetFiles_Directory_SearchOption_Wildcards()
        {
            var FileList = _fileSystem.GetFiles(TestDirectory);
            Assert.Equal(0, FileList.Count);
            var TestFileList = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                TestFileList.Add(CreateTestFile(SourceData, PathFromBase: null, TestFileName: $"NewFile{i}{(i % 2 == 0 ? ".vb" : ".cs")}"));
            }
            FileList = _fileSystem.GetFiles(TestDirectory, SearchOption.SearchTopLevelOnly, "*.vb");
            Assert.Equal(3, FileList.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.True(TestFileList.Contains(FileList[i]));
            }
            var NewSubDirectoryPath = System.IO.Path.Combine(TestDirectory, "GetFiles_Directory_SearchOption_WildcardsNewSubDirectory");
            System.IO.Directory.CreateDirectory(NewSubDirectoryPath);
            TestFileList.Add(CreateTestFile(SourceData, PathFromBase: "GetFiles_Directory_SearchOption_WildcardsNewSubDirectory", TestFileName: "NewFile.cs"));
            FileList = _fileSystem.GetFiles(TestDirectory, SearchOption.SearchAllSubDirectories, "*.cs");
            Assert.True(FileList.Contains(TestFileList[TestFileList.Count - 1]), "File in Subdirectory not found");
            Assert.Equal(4, FileList.Count);
        }

        [Fact]
        public void GetName_Path()
        {
            Assert.Equal(_fileSystem.GetName(TestDirectory), System.IO.Path.GetFileName(TestDirectory));
        }

        [Fact]
        public void GetParentPath_Path()
        {
            Assert.Equal(_fileSystem.GetParentPath(TestDirectory), System.IO.Path.GetDirectoryName(TestDirectory));
        }

        [Fact]
        public void GetTempFileName()
        {
            var TempFile = _fileSystem.GetTempFileName();
            Assert.True(System.IO.File.Exists(TempFile));
            Assert.Equal(0, (new System.IO.FileInfo(TempFile)).Length);
            System.IO.File.Delete(TempFile);
        }

        [Fact]
        public void MoveDirectory_SourceDirectoryName_DestinationDirectoryName()
        {
            var FullPathToSourceDirectory = System.IO.Path.Combine(TestDirectory, "SourceDirectory");
            var FullPathToTargetDirectory = System.IO.Path.Combine(TestDirectory, "TargetDirectory");
            System.IO.Directory.CreateDirectory(FullPathToSourceDirectory);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: "SourceDirectory", TestFileName: $"NewFile{i}");
            }
            _fileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory);
            Assert.Equal(6, System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length);
            Assert.False(System.IO.Directory.Exists(FullPathToSourceDirectory));
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                // Ensure move transferred written data
                Assert.True(HasExpectedData(CurrentFile, SourceData));
            }
            System.IO.Directory.Move(FullPathToTargetDirectory, FullPathToSourceDirectory);
            System.IO.Directory.CreateDirectory(FullPathToTargetDirectory);
            CreateTestFile(SourceData, PathFromBase: "TargetDirectory", TestFileName: "NewFile0");
            Assert.Throws<System.IO.IOException>(() => _fileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory));
        }

        [Fact]
        public void MoveDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteFalse()
        {
            var FullPathToSourceDirectory = System.IO.Path.Combine(TestDirectory, "SourceDirectory");
            var FullPathToTargetDirectory = System.IO.Path.Combine(TestDirectory, "TargetDirectory");
            System.IO.Directory.CreateDirectory(FullPathToSourceDirectory);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: "SourceDirectory", TestFileName: $"NewFile{i}");
            }
            _fileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite: false);
            Assert.Equal(6, System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length);
            Assert.False(System.IO.Directory.Exists(FullPathToSourceDirectory));
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                // Ensure move transferred written data
                Assert.True(HasExpectedData(CurrentFile, SourceData));
            }
            System.IO.Directory.Move(FullPathToTargetDirectory, FullPathToSourceDirectory);
            System.IO.Directory.CreateDirectory(FullPathToTargetDirectory);
            var NewFile0WithPath = CreateTestFile(DestData, PathFromBase: "TargetDirectory", TestFileName: "NewFile0");
            Assert.Throws<System.IO.IOException>(() => _fileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite: false));
            string[] RemainingSourceFilesWithPath = System.IO.Directory.GetFiles(FullPathToSourceDirectory);
            // We couldn't move one file
            Assert.Equal(1, RemainingSourceFilesWithPath.Length);
            // Ensure the file left has correct data
            Assert.True(HasExpectedData(RemainingSourceFilesWithPath[0], SourceData));

            string[] DestinationFilesWithPath = System.IO.Directory.GetFiles(FullPathToTargetDirectory);
            Assert.Equal(6, DestinationFilesWithPath.Length);
            foreach (var CurrentFile in DestinationFilesWithPath)
            {
                Assert.True(HasExpectedData(CurrentFile, CurrentFile.EndsWith("0") ? DestData : SourceData));
            }
        }

        [Fact]
        public void MoveDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteTrue()
        {
            var FullPathToSourceDirectory = System.IO.Path.Combine(TestDirectory, "SourceDirectory");
            var FullPathToTargetDirectory = System.IO.Path.Combine(TestDirectory, "TargetDirectory");
            System.IO.Directory.CreateDirectory(FullPathToSourceDirectory);
            System.IO.Directory.CreateDirectory(FullPathToTargetDirectory);
            for (int i = 0; i < 6; i++)
            {
                CreateTestFile(SourceData, PathFromBase: "SourceDirectory", TestFileName: $"NewFile{i}");
            }
            _fileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite: true);
            Assert.False(System.IO.Directory.Exists(FullPathToSourceDirectory));
            Assert.Equal(6, System.IO.Directory.GetFiles(FullPathToTargetDirectory).Length);
            foreach (var CurrentFile in System.IO.Directory.GetFiles(FullPathToTargetDirectory))
            {
                // Ensure copy transferred written data
                Assert.True(HasExpectedData(CurrentFile, SourceData));
            }
        }

        [Fact]
        public void MoveFile_SourceFileName_DestinationFileName()
        {
            var SourceFileNameWithPath = CreateTestFile(SourceData, TestFileName: GetTestFileName());
            var DestinationFileNameWithPath = System.IO.Path.Combine(TestDirectory, "NewName");
            _fileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath);
            Assert.False(System.IO.File.Exists(SourceFileNameWithPath));
            Assert.True(System.IO.File.Exists(DestinationFileNameWithPath));
            Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData));

            SourceFileNameWithPath = DestinationFileNameWithPath;
            DestinationFileNameWithPath = CreateTestFile(DestData, TestFileName: GetTestFileName());
            Assert.Throws<System.IO.IOException>(() => _fileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath));
            // Make sure we did not override existing file
            Assert.True(HasExpectedData(DestinationFileNameWithPath, DestData));
            Assert.True(System.IO.File.Exists(SourceFileNameWithPath));
        }

        [Fact]
        public void MoveFile_SourceFileName_DestinationFileName_OverwriteFalse()
        {
            var SourceFileNameWithPath = CreateTestFile(SourceData, TestFileName: GetTestFileName());
            var DestinationFileNameWithPath = System.IO.Path.Combine(TestDirectory, "NewName");
            _fileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, overwrite: false);
            Assert.False(System.IO.File.Exists(SourceFileNameWithPath));
            Assert.True(System.IO.File.Exists(DestinationFileNameWithPath));
            Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData));
            SourceFileNameWithPath = DestinationFileNameWithPath;
            DestinationFileNameWithPath = CreateTestFile(DestData, TestFileName: GetTestFileName());
            Assert.Throws<System.IO.IOException>(() => _fileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, overwrite: false));
            // Make sure we did not override existing file
            Assert.True(HasExpectedData(DestinationFileNameWithPath, DestData));
            Assert.True(System.IO.File.Exists(SourceFileNameWithPath));
        }

        [Fact]
        public void MoveFile_SourceFileName_DestinationFileName_OverwriteTrue()
        {
            var SourceFileNameWithPath = CreateTestFile(SourceData, TestFileName: GetTestFileName());
            var DestinationFileNameWithPath = System.IO.Path.Combine(TestDirectory, "NewName");
            _fileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, overwrite: true);
            Assert.False(System.IO.File.Exists(SourceFileNameWithPath));
            Assert.True(System.IO.File.Exists(DestinationFileNameWithPath));
            Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData));
            CreateTestFile(DestData, PathFromBase: null, TestFileName: (new System.IO.FileInfo(SourceFileNameWithPath)).Name);
            _fileSystem.MoveFile(sourceFileName: DestinationFileNameWithPath, destinationFileName: SourceFileNameWithPath, overwrite: true);
            Assert.True(System.IO.File.Exists(SourceFileNameWithPath));
            Assert.False(System.IO.File.Exists(DestinationFileNameWithPath));
            Assert.True(HasExpectedData(SourceFileNameWithPath, SourceData));
        }

        // Not tested:
        //   public Microsoft.VisualBasic.FileIO.TextFieldParser OpenTextFieldParser(string file) { throw null; }
        //   public Microsoft.VisualBasic.FileIO.TextFieldParser OpenTextFieldParser(string file, params int[] fieldWidths) { throw null; }
        //   public Microsoft.VisualBasic.FileIO.TextFieldParser OpenTextFieldParser(string file, params string[] delimiters) { throw null; }
        //   public System.IO.StreamReader OpenTextFileReader(string file) { throw null; }
        //   public System.IO.StreamReader OpenTextFileReader(string file, System.Text.Encoding encoding) { throw null; }
        //   public System.IO.StreamWriter OpenTextFileWriter(string file, bool append) { throw null; }
        //   public System.IO.StreamWriter OpenTextFileWriter(string file, bool append, System.Text.Encoding encoding) { throw null; }
        //   public byte[] ReadAllBytes(string file) { throw null; }
        //   public string ReadAllText(string file) { throw null; }
        //   public string ReadAllText(string file, System.Text.Encoding encoding) { throw null; }

        [Fact]
        public void RenameDirectory_Directory_NewName()
        {
            // <exception cref="IO.FileNotFoundException">If directory does not point to an existing directory.</exception>
            Assert.Throws<System.IO.DirectoryNotFoundException>(() => _fileSystem.RenameDirectory(System.IO.Path.Combine(TestDirectory, "DoesNotExistDirectory"), "NewDirectory"));
            var OrigDirectoryWithPath = System.IO.Path.Combine(TestDirectory, "OriginalDirectory");
            System.IO.Directory.CreateDirectory(OrigDirectoryWithPath);
            // <exception cref="System.ArgumentException">If newName is null or Empty String.</exception>
            Assert.Throws<ArgumentNullException>(() => _fileSystem.RenameDirectory(OrigDirectoryWithPath, ""));
            var DirectoryNameWithPath = System.IO.Path.Combine(TestDirectory, "DoesNotExist");
            // <exception cref="System.ArgumentException">If contains path information.</exception>
            Assert.Throws<ArgumentException>(() => _fileSystem.RenameDirectory(OrigDirectoryWithPath, DirectoryNameWithPath));
            _fileSystem.RenameDirectory(OrigDirectoryWithPath, "NewFDirectory");
            var NewFDirectoryPath = System.IO.Path.Combine(TestDirectory, "NewFDirectory");
            Assert.True(System.IO.Directory.Exists(NewFDirectoryPath));
            Assert.False(System.IO.Directory.Exists(OrigDirectoryWithPath));
            // <exception cref="IO.IOException">If directory points to a root directory or if there's an existing directory or an existing file with the same name.</exception>
            System.IO.Directory.CreateDirectory(OrigDirectoryWithPath);
            Assert.Throws<System.IO.IOException>(() => _fileSystem.RenameDirectory(NewFDirectoryPath, "OriginalDirectory"));
        }

        [Fact]
        public void RenameFile_File_NewName()
        {
            // <exception cref="IO.FileNotFoundException">If file does not point to an existing file.</exception>
            Assert.Throws<System.IO.FileNotFoundException>(() => _fileSystem.RenameFile(System.IO.Path.Combine(TestDirectory, "DoesNotExistFile"), "NewFile"));
            var OrigFileWithPath = CreateTestFile(SourceData, TestFileName: GetTestFileName());
            var ExistingFileWithPath = CreateTestFile(DestData, TestFileName: GetTestFileName());
            // <exception cref="System.ArgumentException">If newName is null or Empty String.</exception>
            Assert.Throws<ArgumentNullException>(() => _fileSystem.RenameFile(OrigFileWithPath, ""));
            // <exception cref="System.ArgumentException">If contains path information.</exception>
            Assert.Throws<ArgumentException>(() => _fileSystem.RenameFile(OrigFileWithPath, ExistingFileWithPath));
            _fileSystem.RenameFile(OrigFileWithPath, "NewFile");
            var NewFileWithPath = System.IO.Path.Combine(TestDirectory, "NewFile");
            Assert.True(System.IO.File.Exists(NewFileWithPath));
            Assert.False(System.IO.File.Exists(OrigFileWithPath));
            // <exception cref="IO.IOException">If there's an existing directory or an existing file with the same name.</exception>
            Assert.Throws<System.IO.IOException>(() => _fileSystem.RenameFile(NewFileWithPath, "NewFile"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(TestDirectory, "NewFDirectory"));
            Assert.Throws<System.IO.IOException>(() => _fileSystem.RenameFile(NewFileWithPath, "NewFDirectory"));
        }

        [Fact]
        public void SpecialDirectories()
        {
            var specialDirectories = _fileSystem.SpecialDirectories;
            Assert.NotNull(specialDirectories);
            Assert.Same(specialDirectories, _fileSystem.SpecialDirectories);
        }

        // Not tested:
        //   public void WriteAllBytes(string file, byte[] data, bool append) { }
        //   public void WriteAllText(string file, string text, bool append) { }
        //   public void WriteAllText(string file, string text, bool append, System.Text.Encoding encoding) { }

        private string CreateTestFile(string TestData, string TestFileName, string PathFromBase = null)
        {
            Assert.False(String.IsNullOrEmpty(TestFileName));
            var TempFileNameWithPath = TestDirectory;
            if (!string.IsNullOrEmpty(PathFromBase))
            {
                TempFileNameWithPath = System.IO.Path.Combine(TempFileNameWithPath, PathFromBase);
            }
            TempFileNameWithPath = System.IO.Path.Combine(TempFileNameWithPath, TestFileName);
            Assert.False(System.IO.File.Exists(TempFileNameWithPath), $"File {TempFileNameWithPath} should not exist!");
            WriteFile(TempFileNameWithPath, TestData);
            return TempFileNameWithPath;
        }
    }
}
