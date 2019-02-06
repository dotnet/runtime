' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports System
Imports System.Runtime.CompilerServices
Imports Xunit
Namespace Microsoft.VisualBasic.Tests.VB
    Public MustInherit Class FileIOTestBase
        Inherits IO.FileCleanupTestBase

        ''' <summary>
        ''' All "Public" tests are Named for the FileIO function they test followed by _ParameterName for each Parameter and if there are options
        ''' they are separated into additional test and the Option Value is the last  part of the name.
        ''' For example CopyDirectory_SourceDirectoryName_DestinationDirectoryName_OverwriteFalse tests CopyDirectory with 3 arguments
        ''' SourceDirectoryName, DestinationDirectoryName and Overwrite and the value of Overwrite being tested is False
        ''' </summary>
        Public Shared Function CreateTestFile(TestBase As FileIOTestBase, TestData() As Char, <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber> Optional lineNumber As Integer = 0) As String
            Return CreateTestFile(TestBase, TestData, "", "", memberName, lineNumber)
        End Function

        Public Shared Function CreateTestFile(TestBase As FileIOTestBase, TestData() As Char, TestFileName As String, <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber> Optional lineNumber As Integer = 0) As String
            Return CreateTestFile(TestBase, TestData, "", TestFileName, memberName, lineNumber)
        End Function

        ''' <summary>
        ''' Create a new file with TestData
        ''' </summary>
        ''' <param name="TestBase">Object to manage temporary Files</param>
        ''' <param name="TestData">Data to be written to file</param>
        ''' <param name="PathFromBase">Optional additional subdirectories that file will be created under</param>
        ''' <param name="TestFileName">Optional Filename, If TestFileName is not provided, the name is based on memberName and lineNumber rather than being random./param>
        ''' <returns>Full Path to New File</returns>
        Public Shared Function CreateTestFile(TestBase As FileIOTestBase, TestData() As Char, PathFromBase As String, TestFileName As String, <CallerMemberName> Optional memberName As String = Nothing, <CallerLineNumber> Optional lineNumber As Integer = 0) As String
            Dim TempFileNameWithPath As String
            If String.IsNullOrEmpty(TestFileName) Then
                TempFileNameWithPath = TestBase.GetTestFilePath(memberName:=memberName, lineNumber:=lineNumber)
            Else
                Assert.False(IO.Path.IsPathRooted(TestFileName))
                If String.IsNullOrEmpty(PathFromBase) Then
                    TempFileNameWithPath = IO.Path.Combine(TestBase.TestDirectory, TestFileName)
                Else
                    ' If we have a Base we must have a filename
                    Assert.False(String.IsNullOrWhiteSpace(TestFileName))
                    TempFileNameWithPath = IO.Path.Combine(TestBase.TestDirectory, PathFromBase, TestFileName)
                End If
            End If
            Assert.False(IO.File.Exists(TempFileNameWithPath), $"File {TempFileNameWithPath} should not exist!")
            ' Write and copy file
            Using writer As New IO.StreamWriter(IO.File.Create(TempFileNameWithPath))
                writer.Write(TestData, 0, TestData.Length)
            End Using

            Return TempFileNameWithPath
        End Function

    End Class
End Namespace
