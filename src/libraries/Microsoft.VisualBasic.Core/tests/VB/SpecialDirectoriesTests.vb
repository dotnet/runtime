' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports System
Imports System.Environment
Imports Microsoft.VisualBasic.FileIO
Imports Xunit
Namespace Microsoft.VisualBasic.Tests.VB
    Public NotInheritable Class SpecialDirectoriesTests

        Private Shared Sub CheckSpecialFolder(folder As SpecialFolder, getSpecialDirectory As Func(Of String))
            Dim LocalFolderPath As String = Environment.GetFolderPath(folder)
            If LocalFolderPath = "" Then
                Assert.Throws(Of IO.DirectoryNotFoundException)(Function() getSpecialDirectory.Invoke)
            Else
                Assert.Equal(LocalFolderPath.TrimSeparators, getSpecialDirectory().TrimSeparators)
            End If

        End Sub

        <Fact>
        Public Shared Sub AllUsersApplicationDataFolderTest()
            Assert.Throws(Of PlatformNotSupportedException)(Function() SpecialDirectories.AllUsersApplicationData)
        End Sub

        <Fact>
        Public Shared Sub CurrentUserApplicationDataFolderTest()
            Assert.Throws(Of PlatformNotSupportedException)(Function() SpecialDirectories.CurrentUserApplicationData)
        End Sub

        <Fact>
        Public Shared Sub DesktopFolderTest()
            CheckSpecialFolder(SpecialFolder.Desktop, Function() SpecialDirectories.Desktop)
        End Sub

        <Fact>
        Public Shared Sub MyDocumentsFolderTest()
            If PlatformDetection.IsWindowsNanoServer Then
                Assert.Throws(Of IO.DirectoryNotFoundException)(Function() SpecialDirectories.MyDocuments)
                Exit Sub
            End If

            CheckSpecialFolder(SpecialFolder.MyDocuments, Function() SpecialDirectories.MyDocuments)
        End Sub

        <Fact>
        Public Shared Sub MyMusicFolderTest()
            CheckSpecialFolder(SpecialFolder.MyMusic, Function() SpecialDirectories.MyMusic)
        End Sub

        <Fact>
        Public Shared Sub MyPicturesFolderTest()
            CheckSpecialFolder(SpecialFolder.MyPictures, Function() SpecialDirectories.MyPictures)
        End Sub

        <Fact>
        Public Shared Sub ProgramFilesFolderTest()
            CheckSpecialFolder(SpecialFolder.ProgramFiles, Function() SpecialDirectories.ProgramFiles)
        End Sub

        <Fact>
        Public Shared Sub ProgramsFolderTest()
            CheckSpecialFolder(SpecialFolder.Programs, Function() SpecialDirectories.Programs)
        End Sub

        <Fact>
        Public Shared Sub TempFolderTest()
            Assert.Equal(IO.Path.GetTempPath.TrimSeparators, SpecialDirectories.Temp.TrimSeparators)
        End Sub

    End Class
End Namespace
