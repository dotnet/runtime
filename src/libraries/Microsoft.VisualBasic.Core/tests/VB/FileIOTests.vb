' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports Microsoft.VisualBasic.FileIO
Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports Xunit
' Do not Imports System.IO
Namespace Microsoft.VisualBasic.Tests.VB
    Public NotInheritable Class FileIOTests
        Inherits FileIOTestBase

        Sub New()
        End Sub

        Shared ReadOnly DestData() As Char = {"x"c, "X"c, "y"c}
        Shared ReadOnly SourceData() As Char = {"a"c, "A"c, "b"c}

        Public Shared ReadOnly Property ManualTestsEnabled() As Boolean
            Get
                Return Not String.IsNullOrEmpty(Environment.GetEnvironmentVariable("MANUAL_TESTS"))
            End Get
        End Property

        Private Shared Function DirectoryListToString(DirectoryList As ReadOnlyCollection(Of String)) As String
            Dim S As New StringBuilder
            For Each d As String In DirectoryList
                S.Append($"{d} ")
            Next
            Return S.ToString
        End Function

        Private Shared Function HasExpectedData(FileNameWithPath As String, ExpectedData() As Char) As Boolean
            Using stream As New IO.StreamReader(IO.File.OpenRead(FileNameWithPath))
                Dim ReadData(ExpectedData.Length - 1) As Char
                stream.Read(ReadData, 0, SourceData.Length)
                Return ExpectedData = ReadData
            End Using
        End Function

        Private Shared Sub WriteFile(FileName As String, TestData As Char())
            Using IOStream As New IO.StreamWriter(IO.File.Create(FileName))
                IOStream.Write(TestData, 0, TestData.Length)
            End Using
        End Sub

        <Fact>
        Public Shared Sub CombinePathTest_BadBaseDirectory_RelativePath()
            Assert.Throws(Of ArgumentNullException)(Function() FileIO.FileSystem.CombinePath(Nothing, "Test2"))
            Assert.Throws(Of ArgumentNullException)(Function() FileIO.FileSystem.CombinePath("", "Test2"))
        End Sub

        <Fact>
        Public Shared Sub CombinePathTest_BaseDirectory_RelativePath()
            Using TestBase As New FileIOTests
                Dim TestDirInfo As New IO.DirectoryInfo(TestBase.TestDirectory)
                Dim Root As String = TestDirInfo.Root.Name
                Assert.Equal(FileIO.FileSystem.CombinePath(Root, "Test2"), IO.Path.Combine(Root, "Test2"))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CombinePathTest_RootDirectory_RelativePath()
            Using TestBase As New FileIOTests
                Assert.Equal(FileIO.FileSystem.CombinePath(TestBase.TestDirectory, Nothing), TestBase.TestDirectory)
                Assert.Equal(FileIO.FileSystem.CombinePath(TestBase.TestDirectory, ""), TestBase.TestDirectory)
                Assert.Equal(FileIO.FileSystem.CombinePath(TestBase.TestDirectory, "Test"), IO.Path.Combine(TestBase.TestDirectory, "Test"))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CopyDirectory_SourceDirectoryName_DestinationDirectoryName()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                Next
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory)
                Assert.Equal(IO.Directory.GetFiles(FullPathToSourceDirectory).Count, IO.Directory.GetFiles(FullPathToTargetDirectory).Count)
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    ' Ensure copy transferred written data
                    Assert.True(HasExpectedData(CurrentFile, SourceData))
                Next
                IO.Directory.Delete(FullPathToTargetDirectory, recursive:=True)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                CreateTestFile(TestBase, TestData:=SourceData, PathFromBase:="TargetDirectory", TestFileName:=$"NewFile0")
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CopyDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteFalse()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                Next
                FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite:=False)
                Assert.Equal(IO.Directory.GetFiles(FullPathToSourceDirectory).Count, IO.Directory.GetFiles(FullPathToTargetDirectory).Count)
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    ' Ensure copy transferred written data
                    Assert.True(HasExpectedData(CurrentFile, SourceData))
                Next
                IO.Directory.Delete(FullPathToTargetDirectory, recursive:=True)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                CreateTestFile(TestBase, DestData, PathFromBase:="TargetDirectory", TestFileName:=$"NewFile0")
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite:=False))
                Assert.Equal(IO.Directory.GetFiles(FullPathToTargetDirectory).Count, IO.Directory.GetFiles(FullPathToSourceDirectory).Count)
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    If CurrentFile.EndsWith("0") Then
                        ' Make sure file 0 is unchanged with DestData
                        Assert.True(HasExpectedData(CurrentFile, DestData))
                    Else
                        ' Ensure file 1 - 5 transferred SourceData
                        Assert.True(HasExpectedData(CurrentFile, SourceData))
                    End If
                Next
            End Using
        End Sub

        <Fact>
        Public Shared Sub CopyDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteTrue()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                Next
                FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite:=True)
                Assert.Equal(IO.Directory.GetFiles(FullPathToSourceDirectory).Count, IO.Directory.GetFiles(FullPathToTargetDirectory).Count)
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    ' Ensure copy transferred written data
                    Assert.True(HasExpectedData(CurrentFile, SourceData))
                Next
            End Using
        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub CopyDirectory_SourceDirectoryName_DestinationDirectoryName_SkipFile()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"Select_Skip_this_file{i}")
                Next
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                CreateTestFile(TestBase, DestData, PathFromBase:="TargetDirectory", TestFileName:=$"Select_Skip_this_file0")
                FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, UIOption.AllDialogs, onUserCancel:=UICancelOption.ThrowException)
                Assert.Equal(IO.Directory.GetFiles(FullPathToTargetDirectory).Count, IO.Directory.GetFiles(FullPathToSourceDirectory).Count)
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    If CurrentFile.EndsWith("0") Then
                        ' Make sure file 0 is unchanged with DestData
                        Assert.True(HasExpectedData(CurrentFile, DestData))
                    Else
                        ' Ensure file 1 - 5 transferred SourceData
                        Assert.True(HasExpectedData(CurrentFile, SourceData))
                    End If
                Next
            End Using

        End Sub

        <Fact>
        <PlatformSpecific(TestPlatforms.AnyUnix)>
        Public Shared Sub CopyDirectory_SourceDirectoryName_DestinationDirectoryName_UIOptionUnix()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                    CreateTestFile(TestBase, DestData, PathFromBase:="TargetDirectory", TestFileName:=$"NewFile{i}")
                Next
                Assert.Throws(Of PlatformNotSupportedException)(Sub() FileIO.FileSystem.CopyDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, UIOption.AllDialogs))
            End Using

        End Sub

        <Fact>
        Public Shared Sub CopyFile_FileSourceFileName_DestinationFileName()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = TestBase.GetTestFilePath()
                Dim testFileDest As String = TestBase.GetTestFilePath()

                ' Write and copy file
                WriteFile(testFileSource, SourceData)
                WriteFile(testFileDest, DestData)
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.CopyFile(testFileSource, testFileDest))

                ' Ensure copy didn't overwrite existing data
                Assert.True(HasExpectedData(testFileDest, DestData))

                ' Get a new destination name
                testFileDest = TestBase.GetTestFilePath()
                FileIO.FileSystem.CopyFile(testFileSource, testFileDest)

                ' Ensure copy transferred written data
                Assert.True(HasExpectedData(testFileDest, SourceData))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CopyFile_FileSourceFileName_DestinationFileName_OverwriteFalse()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = TestBase.GetTestFilePath()
                Dim testFileDest As String = TestBase.GetTestFilePath()

                ' Write and copy file
                WriteFile(testFileSource, SourceData)
                WriteFile(testFileDest, DestData)
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.CopyFile(testFileSource, testFileDest, overwrite:=False))

                ' Ensure copy didn't overwrite existing data
                Assert.True(HasExpectedData(testFileDest, DestData))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CopyFile_FileSourceFileName_DestinationFileName_OverwriteTrue()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = TestBase.GetTestFilePath()
                Dim testFileDest As String = TestBase.GetTestFilePath()

                ' Write and copy file
                WriteFile(testFileSource, SourceData)
                WriteFile(testFileDest, DestData)
                FileIO.FileSystem.CopyFile(testFileSource, testFileDest, overwrite:=True)

                ' Ensure copy transferred written data
                Assert.True(HasExpectedData(testFileDest, SourceData))
            End Using
        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub CopyFile_SourceFileName_DestinationFileName_UIOptionTestOverWriteFalse()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = CreateTestFile(TestBase:=TestBase, TestData:=SourceData, TestFileName:="Select_Skip_this_file")
                Dim testFileDest As String = TestBase.GetTestFilePath()

                ' Write and copy file
                WriteFile(testFileSource, SourceData)
                WriteFile(testFileDest, DestData)
                FileIO.FileSystem.CopyFile(testFileSource, testFileDest, showUI:=UIOption.AllDialogs, onUserCancel:=UICancelOption.DoNothing)

                ' Ensure copy transferred written data
                Assert.True(HasExpectedData(testFileDest, DestData))
            End Using
        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub CopyFile_SourceFileName_DestinationFileName_UIOptionTestOverWriteTrue()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = CreateTestFile(TestBase:=TestBase, TestData:=SourceData, TestFileName:="Select_Replace_the_file")
                Dim testFileDest As String = TestBase.GetTestFilePath()

                ' Write and copy file
                WriteFile(testFileSource, SourceData)
                WriteFile(testFileDest, DestData)

                FileIO.FileSystem.CopyFile(testFileSource, testFileDest, showUI:=UIOption.AllDialogs, onUserCancel:=UICancelOption.DoNothing)

                ' Ensure copy transferred written data
                Assert.True(HasExpectedData(testFileDest, SourceData))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CreateDirectory_Directory()
            Using TestBase As New FileIOTests
                Dim FullPathToNewDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "NewDirectory")
                Assert.False(IO.Directory.Exists(FullPathToNewDirectory))
                FileIO.FileSystem.CreateDirectory(FullPathToNewDirectory)
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
            End Using
        End Sub

        <Fact>
        Public Shared Sub CurrentDirectoryGet()
            If Not RuntimeInformation.IsOSPlatform(OSPlatform.OSX) Then
                ' Can't get current Directory on Mac before setting it.
                Dim CurrentDirectory As String = IO.Directory.GetCurrentDirectory()
                Assert.Equal(FileIO.FileSystem.CurrentDirectory, CurrentDirectory)
            End If
        End Sub

        <Fact>
        Public Shared Sub CurrentDirectorySet()
            If Not RuntimeInformation.IsOSPlatform(OSPlatform.OSX) Then
                Dim SavedCurrentDirectory As String = IO.Directory.GetCurrentDirectory()
                Using TestBase As New FileIOTests
                    ' On OSX, the temp directory /tmp/ is a symlink to /private/tmp, so setting the current
                    ' directory to a symlinked path will result in GetCurrentDirectory returning the absolute
                    ' path that followed the symlink.
                    FileIO.FileSystem.CurrentDirectory = TestBase.TestDirectory
                    Assert.Equal(TestBase.TestDirectory, FileIO.FileSystem.CurrentDirectory())
                    FileIO.FileSystem.CurrentDirectory = SavedCurrentDirectory
                    Assert.Equal(FileIO.FileSystem.CurrentDirectory, SavedCurrentDirectory)
                End Using
            End If
        End Sub

        <Fact>
        Public Shared Sub DeleteDirectory_Directory_DeleteAllContents()
            Using TestBase As New FileIOTests
                Dim FullPathToNewDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "NewDirectory")
                IO.Directory.CreateDirectory(FullPathToNewDirectory)
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
                Dim testFileSource As String = CreateTestFile(TestBase, SourceData, PathFromBase:="NewDirectory", TestFileName:="TestFile")
                Assert.True(IO.File.Exists(testFileSource))
                FileIO.FileSystem.DeleteDirectory(FullPathToNewDirectory, DeleteDirectoryOption.DeleteAllContents)
                Assert.False(IO.Directory.Exists(FullPathToNewDirectory))
            End Using
        End Sub

        <Fact>
        Public Shared Sub DeleteDirectory_Directory_ThrowIfDirectoryNonEmpty()
            Using TestBase As New FileIOTests
                Dim FullPathToNewDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "NewDirectory")
                FileIO.FileSystem.CreateDirectory(FullPathToNewDirectory)
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
                Dim testFileSource As String = CreateTestFile(TestBase, SourceData, PathFromBase:="NewDirectory", TestFileName:="TestFile")

                Assert.True(IO.File.Exists(testFileSource))
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.DeleteDirectory(FullPathToNewDirectory, DeleteDirectoryOption.ThrowIfDirectoryNonEmpty))
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
                Assert.True(IO.File.Exists(testFileSource))
            End Using
        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub DeleteDirectory_Directory_UIOption_Delete()
            Using TestBase As New FileIOTests
                Dim FullPathToNewDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "Select_Yes")
                FileIO.FileSystem.CreateDirectory(FullPathToNewDirectory)
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
                Dim testFileSource As String = CreateTestFile(TestBase, SourceData, PathFromBase:="Select_Yes", TestFileName:="DoNotCare")

                Assert.True(IO.File.Exists(testFileSource))
                FileIO.FileSystem.DeleteDirectory(FullPathToNewDirectory, showUI:=UIOption.AllDialogs, recycle:=RecycleOption.DeletePermanently, onUserCancel:=UICancelOption.ThrowException)
                Assert.False(IO.Directory.Exists(FullPathToNewDirectory))
                Assert.False(IO.File.Exists(testFileSource))
            End Using
        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub DeleteDirectory_Directory_UIOption_DoNotDelete()
            Using TestBase As New FileIOTests
                Dim FullPathToNewDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "Select_No")
                FileIO.FileSystem.CreateDirectory(FullPathToNewDirectory)
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
                Dim testFileSource As String = CreateTestFile(TestBase, SourceData, PathFromBase:="Select_No", TestFileName:="DoNotCare")

                Assert.True(IO.File.Exists(testFileSource))
                Assert.Throws(Of System.OperationCanceledException)(Sub() FileIO.FileSystem.DeleteDirectory(FullPathToNewDirectory, showUI:=UIOption.AllDialogs, recycle:=RecycleOption.DeletePermanently, onUserCancel:=UICancelOption.ThrowException))
                Assert.True(IO.Directory.Exists(FullPathToNewDirectory))
                Assert.True(IO.File.Exists(testFileSource))
            End Using
        End Sub

        <Fact>
        Public Shared Sub DeleteFile_File()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = CreateTestFile(TestBase, SourceData)

                Assert.True(IO.File.Exists(testFileSource))
                FileIO.FileSystem.DeleteFile(testFileSource)
                Assert.False(IO.File.Exists(testFileSource))
            End Using
        End Sub

        <Fact>
        Public Shared Sub DirectoryExists_Directory()
            Using TestBase As New FileIOTests
                Dim TestDirectory As String = TestBase.TestDirectory()
                Assert.True(FileIO.FileSystem.DirectoryExists(TestDirectory))
                Assert.False(FileIO.FileSystem.DirectoryExists(IO.Path.Combine(TestDirectory, "NewDirectory")))
            End Using
        End Sub

        <Fact>
        Public Shared Sub FileExists_File()
            Using TestBase As New FileIOTests
                Dim testFileSource As String = CreateTestFile(TestBase, SourceData)
                Assert.True(FileIO.FileSystem.FileExists(testFileSource))
                FileIO.FileSystem.FileExists(testFileSource)
                IO.File.Delete(testFileSource)
                Assert.False(FileIO.FileSystem.FileExists(testFileSource))
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetDirectories_Directory()
            Using TestBase As New FileIOTests
                Dim DirectoryList As ReadOnlyCollection(Of String) = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory)
                Assert.True(DirectoryList.Count = 0)
                For i As Integer = 0 To 5
                    IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"GetDirectories_DirectoryNewSubDirectory{i}"))
                Next
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory)
                Assert.True(DirectoryList.Count = 6)
                For i As Integer = 0 To 5
                    Assert.True(DirectoryList.Contains(IO.Path.Combine(TestBase.TestDirectory, $"GetDirectories_DirectoryNewSubDirectory{i}")))
                Next
                IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"GetDirectories_DirectoryNewSubDirectory0", $"NewSubSubDirectory"))
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory)
                Assert.True(DirectoryList.Count = 6)
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetDirectories_Directory_SearchOption()
            Using TestBase As New FileIOTests
                Dim DirectoryList As ReadOnlyCollection(Of String) = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly)
                Assert.True(DirectoryList.Count = 0)
                For i As Integer = 0 To 5
                    IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"GetDirectories_Directory_SearchOptionNewSubDirectory{i}"))
                Next
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly)
                Assert.True(DirectoryList.Count = 6)
                For i As Integer = 0 To 5
                    Assert.True(DirectoryList.Contains(IO.Path.Combine(TestBase.TestDirectory, $"GetDirectories_Directory_SearchOptionNewSubDirectory{i}")))
                Next
                IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"GetDirectories_Directory_SearchOptionNewSubDirectory0", $"NewSubSubDirectory"))
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly)
                Assert.True(DirectoryList.Count = 6)
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchAllSubDirectories)
                Assert.True(DirectoryList.Count = 7)
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetDirectories_Directory_SearchOption_Wildcards()
            Using TestBase As New FileIOTests
                Dim DirectoryList As ReadOnlyCollection(Of String) = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly, "*")
                Assert.True(DirectoryList.Count = 0)
                Dim CreatedDirectories As New List(Of String)
                For i As Integer = 0 To 5
                    CreatedDirectories.Add(IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"NewSubDirectory00{i}")).Name)
                Next
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly, "*000", "*001")
                Assert.True(DirectoryList.Count = 2, $"Search results Expected 2 Actual {DirectoryList.Count} DirectoryList = {DirectoryListToString(DirectoryList)} Created List = {DirectoryListToString(New ReadOnlyCollection(Of String)(CreatedDirectories))}")
                For i As Integer = 0 To 1
                    Dim DirectoryName As String = IO.Path.Combine(TestBase.TestDirectory, $"NewSubDirectory00{i}")
                    Assert.True(DirectoryList.Contains(DirectoryName), $"{DirectoryName} Is missing from Wildcard Search")
                Next
                IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"NewSubDirectory000", $"NewSubSubDirectory000"))
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly, "*000")
                Assert.True(DirectoryList.Count = 1, $"Search results Expected 1 Actual {DirectoryList.Count} {DirectoryListToString(DirectoryList)}")
                DirectoryList = FileIO.FileSystem.GetDirectories(TestBase.TestDirectory, SearchOption.SearchAllSubDirectories, "*000")
                Assert.True(DirectoryList.Count = 2, $"Search results Expected 2 Actual {DirectoryList.Count} {DirectoryListToString(DirectoryList)}")
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetDirectoryInfo_Directory()
            Using TestBase As New FileIOTests
                For i As Integer = 0 To 5
                    IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"NewSubDirectory{i}"))
                Next
                IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, $"NewSubDirectory0", $"NewSubSubDirectory"))
                Dim info As IO.DirectoryInfo = FileIO.FileSystem.GetDirectoryInfo(TestBase.TestDirectory)
                Dim infoFromIO As IO.DirectoryInfo = New IO.DirectoryInfo(TestBase.TestDirectory)
                Assert.True(info.CreationTime = infoFromIO.CreationTime, $"Creation Time info ({info.CreationTime}) <> IO.DriveInfo.CreateTime {infoFromIO.CreationTime})")
                Assert.True(info.Extension = infoFromIO.Extension, $"Extension {info.Extension} <> IO.DriveInfo.Extension {infoFromIO.Extension}")
                Assert.True(info.FullName = TestBase.TestDirectory, $"Fullname {info.FullName} <> TestBase.TestDirectory {TestBase.TestDirectory}")
                Assert.True(info.LastAccessTime = infoFromIO.LastAccessTime)
                Assert.True(info.Name = infoFromIO.Name, $"Name {info.Name} Doesn't match {infoFromIO.Name}")
                Assert.True(info.Parent.ToString = infoFromIO.Parent.ToString, "info.Parent<>infoFromIO.Parent")
                Assert.True(info.Root.Name = infoFromIO.Root.Name, $"info.Root.Name {info.Root.Name} <> IO.DriveInfo.Root.Name){infoFromIO.Root.Name}")
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetDriveInfo_Drive()
            Dim Drives() As IO.DriveInfo = IO.DriveInfo.GetDrives()
            Assert.True(Drives.Count > 0)
            Assert.Equal(FileIO.FileSystem.GetDriveInfo(Drives(0).Name).Name, New System.IO.DriveInfo(Drives(0).Name).Name)
        End Sub

        <Fact>
        Public Shared Sub GetFileInfo_File()
            Using TestBase As New FileIOTests
                Dim TestFile As String = CreateTestFile(TestBase, SourceData)

                Dim FileInfoFromSystemIO As IO.FileInfo = New IO.FileInfo(TestFile)
                Assert.NotNull(FileInfoFromSystemIO)

                Dim info As IO.FileInfo = FileIO.FileSystem.GetFileInfo(TestFile)
                Assert.NotNull(info)
                With FileInfoFromSystemIO
                    Assert.True(info.Exists)
                    Assert.True(info.Attributes = .Attributes)
                    Assert.True(info.CreationTime = .CreationTime)
                    Assert.True(info.CreationTime > Date.MinValue, "Starttime = 0")
                    Assert.True(info.DirectoryName = .DirectoryName)
                    Assert.True(info.Extension = .Extension)
                    Assert.True(info.FullName = .FullName)
                    Assert.True(info.IsReadOnly = .IsReadOnly)
                    Assert.True(info.LastAccessTime = .LastAccessTime)
                    Assert.True(info.LastWriteTime = .LastWriteTime)
                    Assert.True(info.Length = .Length)
                    Assert.True(info.Name = .Name)
                End With

            End Using
        End Sub

        <Fact>
        Public Shared Sub GetFiles_Directory()
            Using TestBase As New FileIOTests
                Dim FileList As ReadOnlyCollection(Of String) = FileIO.FileSystem.GetFiles(TestBase.TestDirectory)
                Assert.True(FileList.Count = 0)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, TestFileName:=$"NewFile{i}")
                Next
                FileList = FileIO.FileSystem.GetFiles(TestBase.TestDirectory)
                Assert.True(FileList.Count = 6)
                For i As Integer = 0 To 5
                    Assert.True(FileList.Contains(IO.Path.Combine(TestBase.TestDirectory, $"NewFile{i}")))
                Next
                IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, "GetFiles_DirectoryNewSubDirectory"))
                CreateTestFile(TestBase, SourceData, PathFromBase:="GetFiles_DirectoryNewSubDirectory", TestFileName:="NewFile")
                FileList = FileIO.FileSystem.GetFiles(TestBase.TestDirectory)
                Assert.True(FileList.Count = 6)
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetFiles_Directory_SearchOption()
            Using TestBase As New FileIOTests
                Dim NewSubDirectoryPath As String = IO.Path.Combine(TestBase.TestDirectory, "GetFiles_Directory_SearchOptionNewSubDirectory")
                IO.Directory.CreateDirectory(NewSubDirectoryPath)
                CreateTestFile(TestBase, SourceData, "GetFiles_Directory_SearchOptionNewSubDirectory", TestFileName:="NewFile")
                Dim FileList As ReadOnlyCollection(Of String) = FileIO.FileSystem.GetFiles(TestBase.TestDirectory)
                Assert.True(FileList.Count = 0)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, TestFileName:=$"NewFile{i}")
                Next
                FileList = FileIO.FileSystem.GetFiles(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly)
                CreateTestFile(TestBase, SourceData, TestFileName:="NewFile")
                Assert.True(FileList.Count = 6)
                For i As Integer = 0 To 5
                    Assert.True(FileList.Contains(IO.Path.Combine(TestBase.TestDirectory, $"NewFile{i}")))
                Next
                FileList = FileIO.FileSystem.GetFiles(TestBase.TestDirectory, SearchOption.SearchAllSubDirectories)
                Assert.True(FileList.Count = 8)
                For i As Integer = 0 To 7
                    Assert.True(IO.File.Exists(FileList(i)))
                Next
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetFiles_Directory_SearchOption_Wildcards()
            Using TestBase As New FileIOTests
                Dim FileList As ReadOnlyCollection(Of String) = FileIO.FileSystem.GetFiles(TestBase.TestDirectory)
                Assert.True(FileList.Count = 0)
                Dim TestFileList As New List(Of String)
                For i As Integer = 0 To 5
                    TestFileList.Add(CreateTestFile(TestBase, SourceData, TestFileName:=$"NewFile{i}{If(i Mod 2 = 0, ".vb", ".cs")}"))
                Next
                FileList = FileIO.FileSystem.GetFiles(TestBase.TestDirectory, SearchOption.SearchTopLevelOnly, "*.vb")
                Assert.True(FileList.Count = 3)
                For i As Integer = 0 To 2
                    Assert.True(TestFileList.Contains(FileList(i)))
                Next
                Dim NewSubDirectoryPath As String = IO.Path.Combine(TestBase.TestDirectory, "GetFiles_Directory_SearchOption_WildcardsNewSubDirectory")
                IO.Directory.CreateDirectory(NewSubDirectoryPath)
                TestFileList.Add(CreateTestFile(TestBase, SourceData, PathFromBase:="GetFiles_Directory_SearchOption_WildcardsNewSubDirectory", TestFileName:="NewFile.cs"))
                FileList = FileIO.FileSystem.GetFiles(TestBase.TestDirectory, SearchOption.SearchAllSubDirectories, "*.cs")
                Assert.True(FileList.Contains(TestFileList.Last), "File in Subdirectory not found")
                Assert.True(FileList.Count = 4, $"4 files expected, {FileList.Count} returned from FileIO.FileSystem.GetFiles")
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetName_Path()
            Using TestBase As New FileIOTests
                Assert.Equal(FileIO.FileSystem.GetName(TestBase.TestDirectory), IO.Path.GetFileName(TestBase.TestDirectory))
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetParentPath_Path()
            Using TestBase As New FileIOTests
                Dim TestDirectory As String = TestBase.TestDirectory
                Assert.Equal(FileIO.FileSystem.GetParentPath(TestDirectory), IO.Path.GetDirectoryName(TestDirectory.TrimSeparators))
            End Using
        End Sub

        <Fact>
        Public Shared Sub GetTempFileName()
            Dim TempFile As String = FileIO.FileSystem.GetTempFileName
            Assert.True(IO.File.Exists(TempFile))
            Assert.True((New IO.FileInfo(TempFile)).Length = 0)
            IO.File.Delete(TempFile)
        End Sub

        <Fact>
        Public Sub LongDirectoryPathTest()
            Using TestBase As New FileIOTests
                Dim PathLength As Integer = TestBase.TestDirectory().Length
                Assert.True(PathLength < 257) ' Need room for slash and new directory name
                Dim DirectoryName As String = New String("B"c, 30)

                Assert.True(DirectoryName.Length < 248, $"DirectoryBaseName.Length at {DirectoryName.Length} is not < 248")
                Assert.True(IO.Directory.Exists(TestBase.TestDirectory))

                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory, DirectoryName)
                Assert.True(FullPathToTargetDirectory.Length < 260, $"FullPathToTargetDirectory.Length at {FullPathToTargetDirectory.Length} is not < 260")

                FileIO.FileSystem.CreateDirectory(FullPathToTargetDirectory)
                Assert.True(IO.Directory.Exists(FullPathToTargetDirectory))

                Try
                    Dim VeryLongFullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory, New String("E"c, 239))
                    FileIO.FileSystem.CreateDirectory(VeryLongFullPathToTargetDirectory)
                    Assert.True(IO.Directory.Exists(VeryLongFullPathToTargetDirectory), $"Directory {VeryLongFullPathToTargetDirectory} does not exist")
                Catch e As IO.PathTooLongException
                    Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Unexpected Failure on non-Windows Platform")
                Catch e As IO.DirectoryNotFoundException
                    Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Unexpected Failure on non-Windows Platform")
                    Assert.Equal(8, IntPtr.Size)
                End Try
            End Using

        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub MoveDirectory_Source_DirectoryName_DestinationDirectoryName_UIOptionOverwriteFalse()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"Select_Skip_this_file{i}")
                Next
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                Dim NewFile0WithPath As String = CreateTestFile(TestBase, DestData, PathFromBase:="TargetDirectory", TestFileName:="Select_Skip_this_file0")
                FileIO.FileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, showUI:=UIOption.AllDialogs, onUserCancel:=UICancelOption.ThrowException)
                Dim RemainingSourceFilesWithPath As String() = IO.Directory.GetFiles(FullPathToSourceDirectory)
                ' We couldn't move one file
                Assert.Equal(1, RemainingSourceFilesWithPath.Count)
                ' Ensure the file left has correct data
                Assert.True(HasExpectedData(RemainingSourceFilesWithPath(0), SourceData))

                Dim DestinationFilesWithPath As String() = IO.Directory.GetFiles(FullPathToTargetDirectory)
                Assert.Equal(6, DestinationFilesWithPath.Count)
                For Each CurrentFile As String In DestinationFilesWithPath
                    If CurrentFile.EndsWith("0") Then
                        ' Make sure file 0 is unchanged with DestData
                        Assert.True(HasExpectedData(CurrentFile, DestData))
                    Else
                        ' Ensure file 1 - 5 transferred SourceData
                        Assert.True(HasExpectedData(CurrentFile, SourceData))
                    End If
                Next
            End Using
        End Sub

        <Fact>
        Public Shared Sub MoveDirectory_SourceDirectoryName_DestinationDirectoryName()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                Next
                FileIO.FileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory)
                Assert.Equal(6, IO.Directory.GetFiles(FullPathToTargetDirectory).Count)
                Assert.False(IO.Directory.Exists(FullPathToSourceDirectory))
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    ' Ensure move transferred written data
                    Assert.True(HasExpectedData(CurrentFile, SourceData))
                Next
                IO.Directory.Move(FullPathToTargetDirectory, FullPathToSourceDirectory)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                CreateTestFile(TestBase, SourceData, PathFromBase:="TargetDirectory", TestFileName:="NewFile0")
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory))
            End Using
        End Sub

        <Fact>
        Public Shared Sub MoveDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteFalse()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                Next
                FileIO.FileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite:=False)
                Assert.Equal(6, IO.Directory.GetFiles(FullPathToTargetDirectory).Count)
                Assert.False(IO.Directory.Exists(FullPathToSourceDirectory))
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    ' Ensure move transferred written data
                    Assert.True(HasExpectedData(CurrentFile, SourceData))
                Next
                IO.Directory.Move(FullPathToTargetDirectory, FullPathToSourceDirectory)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                Dim NewFile0WithPath As String = CreateTestFile(TestBase, DestData, PathFromBase:="TargetDirectory", TestFileName:="NewFile0")
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite:=False))
                Dim RemainingSourceFilesWithPath As String() = IO.Directory.GetFiles(FullPathToSourceDirectory)
                ' We couldn't move one file
                Assert.Equal(1, RemainingSourceFilesWithPath.Count)
                ' Ensure the file left has correct data
                Assert.True(HasExpectedData(RemainingSourceFilesWithPath(0), SourceData))

                Dim DestinationFilesWithPath As String() = IO.Directory.GetFiles(FullPathToTargetDirectory)
                Assert.Equal(6, DestinationFilesWithPath.Count)
                For Each CurrentFile As String In DestinationFilesWithPath
                    If CurrentFile.EndsWith("0") Then
                        ' Make sure file 0 is unchanged with DestData
                        Assert.True(HasExpectedData(CurrentFile, DestData))
                    Else
                        ' Ensure file 1 - 5 transferred SourceData
                        Assert.True(HasExpectedData(CurrentFile, SourceData))
                    End If
                Next
            End Using
        End Sub

        <Fact>
        Public Shared Sub MoveDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteTrue()
            Using TestBase As New FileIOTests
                Dim FullPathToSourceDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "SourceDirectory")
                Dim FullPathToTargetDirectory As String = IO.Path.Combine(TestBase.TestDirectory(), "TargetDirectory")
                IO.Directory.CreateDirectory(FullPathToSourceDirectory)
                IO.Directory.CreateDirectory(FullPathToTargetDirectory)
                For i As Integer = 0 To 5
                    CreateTestFile(TestBase, SourceData, PathFromBase:="SourceDirectory", TestFileName:=$"NewFile{i}")
                Next
                FileIO.FileSystem.MoveDirectory(FullPathToSourceDirectory, FullPathToTargetDirectory, overwrite:=True)
                Assert.False(IO.Directory.Exists(FullPathToSourceDirectory))
                Assert.Equal(6, IO.Directory.GetFiles(FullPathToTargetDirectory).Count)
                For Each CurrentFile As String In IO.Directory.GetFiles(FullPathToTargetDirectory)
                    ' Ensure copy transferred written data
                    Assert.True(HasExpectedData(CurrentFile, SourceData))
                Next
            End Using
        End Sub

        <Fact>
        Public Shared Sub MoveFile_SourceFileName_DestinationFileName()
            Using TestBase As New FileIOTests
                Dim SourceFileNameWithPath As String = CreateTestFile(TestBase, SourceData)
                Dim DestinationFileNameWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "NewName")
                FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath)
                Assert.False(IO.File.Exists(SourceFileNameWithPath))
                Assert.True(IO.File.Exists(DestinationFileNameWithPath))
                Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData))

                SourceFileNameWithPath = DestinationFileNameWithPath
                DestinationFileNameWithPath = CreateTestFile(TestBase, DestData)
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath))
                ' Make sure we did not override existing file
                Assert.True(HasExpectedData(DestinationFileNameWithPath, DestData))
                Assert.True(IO.File.Exists(SourceFileNameWithPath))
            End Using
        End Sub

        <Fact>
        Public Shared Sub MoveFile_SourceFileName_DestinationFileName_OverwriteFalse()
            Using TestBase As New FileIOTests
                Dim SourceFileNameWithPath As String = CreateTestFile(TestBase, SourceData)
                Dim DestinationFileNameWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "NewName")
                FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, overwrite:=False)
                Assert.False(IO.File.Exists(SourceFileNameWithPath))
                Assert.True(IO.File.Exists(DestinationFileNameWithPath))
                Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData))
                SourceFileNameWithPath = DestinationFileNameWithPath
                DestinationFileNameWithPath = CreateTestFile(TestBase, DestData)
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, overwrite:=False))
                ' Make sure we did not override existing file
                Assert.True(HasExpectedData(DestinationFileNameWithPath, DestData))
                Assert.True(IO.File.Exists(SourceFileNameWithPath))
            End Using
        End Sub

        <Fact>
        Public Shared Sub MoveFile_SourceFileName_DestinationFileName_OverwriteTrue()
            Using TestBase As New FileIOTests
                Dim SourceFileNameWithPath As String = CreateTestFile(TestBase, SourceData)
                Dim DestinationFileNameWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "NewName")
                FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, overwrite:=True)
                Assert.False(IO.File.Exists(SourceFileNameWithPath))
                Assert.True(IO.File.Exists(DestinationFileNameWithPath))
                Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData))
                CreateTestFile(TestBase, DestData, TestFileName:=(New IO.FileInfo(SourceFileNameWithPath)).Name)
                FileIO.FileSystem.MoveFile(sourceFileName:=DestinationFileNameWithPath, destinationFileName:=SourceFileNameWithPath, overwrite:=True)
                Assert.True(IO.File.Exists(SourceFileNameWithPath))
                Assert.False(IO.File.Exists(DestinationFileNameWithPath))
                Assert.True(HasExpectedData(SourceFileNameWithPath, SourceData))
            End Using
        End Sub

        <ConditionalFact(NameOf(ManualTestsEnabled))>
        <PlatformSpecific(TestPlatforms.Windows)>
        Public Shared Sub MoveFile_SourceFileName_DestinationFileName_UIOptionOverWriteFalse()
            Using TestBase As New FileIOTests
                Dim SourceFileNameWithPath As String = CreateTestFile(TestBase, SourceData)
                Dim DestinationFileNameWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "Select_Skip_this_file")
                FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, showUI:=UIOption.AllDialogs, onUserCancel:=UICancelOption.DoNothing)
                Assert.False(IO.File.Exists(SourceFileNameWithPath))
                Assert.True(IO.File.Exists(DestinationFileNameWithPath))
                Assert.True(HasExpectedData(DestinationFileNameWithPath, SourceData))
                SourceFileNameWithPath = DestinationFileNameWithPath
                DestinationFileNameWithPath = CreateTestFile(TestBase, DestData)
                FileIO.FileSystem.MoveFile(SourceFileNameWithPath, DestinationFileNameWithPath, showUI:=UIOption.AllDialogs, onUserCancel:=UICancelOption.ThrowException)
                ' Make sure we did not override existing file
                Assert.True(HasExpectedData(DestinationFileNameWithPath, DestData))
                Assert.True(IO.File.Exists(SourceFileNameWithPath))
            End Using
        End Sub

        <Fact>
        Public Shared Sub RenameDirectory_Directory_NewName()
            Using TestBase As New FileIOTests
                ' <exception cref="IO.FileNotFoundException">If directory does not point to an existing directory.</exception>
                Assert.Throws(Of IO.DirectoryNotFoundException)(Sub() FileIO.FileSystem.RenameDirectory(IO.Path.Combine(TestBase.TestDirectory, "DoesNotExistDirectory"), "NewDirectory"))
                Dim OrigDirectoryWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "OriginalDirectory")
                IO.Directory.CreateDirectory(OrigDirectoryWithPath)
                ' <exception cref="System.ArgumentException">If newName is Nothing or Empty String.</exception>
                Assert.Throws(Of ArgumentNullException)(Sub() FileIO.FileSystem.RenameDirectory(OrigDirectoryWithPath, ""))
                Dim DirectoryNameWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "DoesNotExist")
                ' <exception cref="System.ArgumentException">If contains path information.</exception>
                Assert.Throws(Of ArgumentException)(Sub() FileIO.FileSystem.RenameDirectory(OrigDirectoryWithPath, DirectoryNameWithPath))
                FileIO.FileSystem.RenameDirectory(OrigDirectoryWithPath, "NewFDirectory")
                Dim NewFDirectoryPath As String = IO.Path.Combine(TestBase.TestDirectory, "NewFDirectory")
                Assert.True(IO.Directory.Exists(NewFDirectoryPath))
                Assert.False(IO.Directory.Exists(OrigDirectoryWithPath))
                ' <exception cref="IO.IOException">If directory points to a root directory or if there's an existing directory or an existing file with the same name.</exception>
                IO.Directory.CreateDirectory(OrigDirectoryWithPath)
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.RenameDirectory(NewFDirectoryPath, "OriginalDirectory"))
            End Using
        End Sub

        <Fact>
        Public Shared Sub RenameFile_File_NewName()
            Using TestBase As New FileIOTests
                ' <exception cref="IO.FileNotFoundException">If file does not point to an existing file.</exception>
                Assert.Throws(Of IO.FileNotFoundException)(Sub() FileIO.FileSystem.RenameFile(IO.Path.Combine(TestBase.TestDirectory, "DoesNotExistFile"), "NewFile"))
                Dim OrigFileWithPath As String = CreateTestFile(TestBase, SourceData)
                Dim ExistingFileWithPath As String = CreateTestFile(TestBase, DestData)
                ' <exception cref="System.ArgumentException">If newName is Nothing or Empty String.</exception>
                Assert.Throws(Of ArgumentNullException)(Sub() FileIO.FileSystem.RenameFile(OrigFileWithPath, ""))
                ' <exception cref="System.ArgumentException">If contains path information.</exception>
                Assert.Throws(Of ArgumentException)(Sub() FileIO.FileSystem.RenameFile(OrigFileWithPath, ExistingFileWithPath))
                FileIO.FileSystem.RenameFile(OrigFileWithPath, "NewFile")
                Dim NewFileWithPath As String = IO.Path.Combine(TestBase.TestDirectory, "NewFile")
                Assert.True(IO.File.Exists(NewFileWithPath))
                Assert.False(IO.File.Exists(OrigFileWithPath))
                ' <exception cref="IO.IOException">If there's an existing directory or an existing file with the same name.</exception>
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.RenameFile(NewFileWithPath, "NewFile"))
                IO.Directory.CreateDirectory(IO.Path.Combine(TestBase.TestDirectory, "NewFDirectory"))
                Assert.Throws(Of IO.IOException)(Sub() FileIO.FileSystem.RenameFile(NewFileWithPath, "NewFDirectory"))
            End Using
        End Sub

    End Class
End Namespace
