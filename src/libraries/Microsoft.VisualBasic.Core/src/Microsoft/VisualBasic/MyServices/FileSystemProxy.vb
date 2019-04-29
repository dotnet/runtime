' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On

Imports System
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Text
Imports Microsoft.VisualBasic.FileIO

Namespace Microsoft.VisualBasic.MyServices

    '''******************************************************************************
    ''' ;FileSystemProxy
    ''' <summary>
    ''' An extremely thin wrapper around Microsoft.VisualBasic.FileIO.FileSystem to expose the type through My.
    ''' </summary>
    <System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)>
    Public Class FileSystemProxy

        '= PUBLIC =============================================================

        Public ReadOnly Property Drives() As ReadOnlyCollection(Of IO.DriveInfo)
            Get
                Return Microsoft.VisualBasic.FileIO.FileSystem.Drives
            End Get
        End Property

        Public ReadOnly Property SpecialDirectories() As MyServices.SpecialDirectoriesProxy
            Get
                If m_SpecialDirectoriesProxy Is Nothing Then
                    m_SpecialDirectoriesProxy = New SpecialDirectoriesProxy
                End If
                Return m_SpecialDirectoriesProxy
            End Get
        End Property

        Public Property CurrentDirectory() As String
            Get
                Return Microsoft.VisualBasic.FileIO.FileSystem.CurrentDirectory
            End Get
            Set(ByVal value As String)
                Microsoft.VisualBasic.FileIO.FileSystem.CurrentDirectory = value
            End Set
        End Property

        Public Function DirectoryExists(ByVal directory As String) As Boolean
            Return Microsoft.VisualBasic.FileIO.FileSystem.DirectoryExists(directory)
        End Function

        Public Function FileExists(ByVal file As String) As Boolean
            Return Microsoft.VisualBasic.FileIO.FileSystem.FileExists(file)
        End Function

        Public Sub CreateDirectory(ByVal directory As String)
            Microsoft.VisualBasic.FileIO.FileSystem.CreateDirectory(directory)
        End Sub

        Public Function GetDirectoryInfo(ByVal directory As String) As System.IO.DirectoryInfo
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetDirectoryInfo(directory)
        End Function

        Public Function GetFileInfo(ByVal file As String) As System.IO.FileInfo
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetFileInfo(file)
        End Function

        Public Function GetDriveInfo(ByVal drive As String) As System.IO.DriveInfo
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetDriveInfo(drive)
        End Function

        Public Function GetFiles(ByVal directory As String) As ReadOnlyCollection(Of String)
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetFiles(directory)
        End Function

        Public Function GetFiles(ByVal directory As String, ByVal searchType As SearchOption,
            ByVal ParamArray wildcards() As String) As ReadOnlyCollection(Of String)

            Return Microsoft.VisualBasic.FileIO.FileSystem.GetFiles(directory, searchType, wildcards)
        End Function

        Public Function GetDirectories(ByVal directory As String) As ReadOnlyCollection(Of String)
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetDirectories(directory)
        End Function

        Public Function GetDirectories(ByVal directory As String, ByVal searchType As SearchOption,
            ByVal ParamArray wildcards() As String) As ReadOnlyCollection(Of String)

            Return Microsoft.VisualBasic.FileIO.FileSystem.GetDirectories(directory, searchType, wildcards)
        End Function

        Public Function FindInFiles(ByVal directory As String,
            ByVal containsText As String, ByVal ignoreCase As Boolean, ByVal searchType As SearchOption) As ReadOnlyCollection(Of String)

            Return Microsoft.VisualBasic.FileIO.FileSystem.FindInFiles(directory, containsText, ignoreCase, searchType)
        End Function

        Public Function FindInFiles(ByVal directory As String, ByVal containsText As String, ByVal ignoreCase As Boolean,
            ByVal searchType As SearchOption, ByVal ParamArray fileWildcards() As String) As ReadOnlyCollection(Of String)

            Return Microsoft.VisualBasic.FileIO.FileSystem.FindInFiles(directory, containsText, ignoreCase, searchType, fileWildcards)
        End Function

        Public Function GetParentPath(ByVal path As String) As String
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetParentPath(path)
        End Function

        Public Function CombinePath(ByVal baseDirectory As String, ByVal relativePath As String) As String
            Return Microsoft.VisualBasic.FileIO.FileSystem.CombinePath(baseDirectory, relativePath)
        End Function

        Public Function GetName(ByVal path As String) As String
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetName(path)
        End Function

        Public Function GetTempFileName() As String
            Return Microsoft.VisualBasic.FileIO.FileSystem.GetTempFileName()
        End Function

        Public Function ReadAllText(ByVal file As String) As String
            Return Microsoft.VisualBasic.FileIO.FileSystem.ReadAllText(file)
        End Function

        Public Function ReadAllText(ByVal file As String, ByVal encoding As Encoding) As String
            Return Microsoft.VisualBasic.FileIO.FileSystem.ReadAllText(file, encoding)
        End Function

        Public Function ReadAllBytes(ByVal file As String) As Byte()
            Return Microsoft.VisualBasic.FileIO.FileSystem.ReadAllBytes(file)
        End Function

        Public Sub WriteAllText(ByVal file As String, ByVal text As String, ByVal append As Boolean)
            Microsoft.VisualBasic.FileIO.FileSystem.WriteAllText(file, text, append)
        End Sub

        Public Sub WriteAllText(ByVal file As String, ByVal text As String, ByVal append As Boolean,
            ByVal encoding As Encoding)

            Microsoft.VisualBasic.FileIO.FileSystem.WriteAllText(file, text, append, encoding)
        End Sub

        Public Sub WriteAllBytes(ByVal file As String, ByVal data() As Byte, ByVal append As Boolean)
            Microsoft.VisualBasic.FileIO.FileSystem.WriteAllBytes(file, data, append)
        End Sub

        Public Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(sourceFileName, destinationFileName)
        End Sub

        Public Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal overwrite As Boolean)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(sourceFileName, destinationFileName, overwrite)
        End Sub

        Public Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(sourceFileName, destinationFileName, showUI)
        End Sub

        Public Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyFile(sourceFileName, destinationFileName, showUI, onUserCancel)
        End Sub

        Public Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(sourceFileName, destinationFileName)
        End Sub

        Public Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal overwrite As Boolean)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(sourceFileName, destinationFileName, overwrite)
        End Sub

        Public Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(sourceFileName, destinationFileName, showUI)
        End Sub

        Public Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(sourceFileName, destinationFileName, showUI, onUserCancel)
        End Sub

        Public Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourceDirectoryName, destinationDirectoryName)
        End Sub

        Public Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal overwrite As Boolean)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourceDirectoryName, destinationDirectoryName, overwrite)
        End Sub

        Public Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourceDirectoryName, destinationDirectoryName, showUI)
        End Sub

        Public Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(sourceDirectoryName, destinationDirectoryName, showUI, onUserCancel)
        End Sub

        Public Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(sourceDirectoryName, destinationDirectoryName)
        End Sub

        Public Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal overwrite As Boolean)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(sourceDirectoryName, destinationDirectoryName, overwrite)
        End Sub

        Public Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(sourceDirectoryName, destinationDirectoryName, showUI)
        End Sub

        Public Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(sourceDirectoryName, destinationDirectoryName, showUI, onUserCancel)
        End Sub

        Public Sub DeleteFile(ByVal file As String)
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file)
        End Sub

        Public Sub DeleteFile(ByVal file As String, ByVal showUI As UIOption, ByVal recycle As RecycleOption)
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, showUI, recycle)
        End Sub

        Public Sub DeleteFile(ByVal file As String, ByVal showUI As UIOption, ByVal recycle As RecycleOption,
            ByVal onUserCancel As UICancelOption)

            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, showUI, recycle, onUserCancel)
        End Sub

        Public Sub DeleteDirectory(ByVal directory As String, ByVal onDirectoryNotEmpty As DeleteDirectoryOption)
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(directory, onDirectoryNotEmpty)
        End Sub

        Public Sub DeleteDirectory(ByVal directory As String, ByVal showUI As UIOption, ByVal recycle As RecycleOption)

            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(directory, showUI, recycle)
        End Sub

        Public Sub DeleteDirectory(ByVal directory As String,
            ByVal showUI As UIOption, ByVal recycle As RecycleOption, ByVal onUserCancel As UICancelOption)

            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(directory, showUI, recycle, onUserCancel)
        End Sub

        Public Sub RenameFile(ByVal file As String, ByVal newName As String)
            Microsoft.VisualBasic.FileIO.FileSystem.RenameFile(file, newName)
        End Sub

        Public Sub RenameDirectory(ByVal directory As String, ByVal newName As String)
            Microsoft.VisualBasic.FileIO.FileSystem.RenameDirectory(directory, newName)
        End Sub

        Public Function OpenTextFieldParser(ByVal file As String) As TextFieldParser
            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFieldParser(file)
        End Function

        Public Function OpenTextFieldParser(ByVal file As String, ByVal ParamArray delimiters As String()) As TextFieldParser
            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFieldParser(file, delimiters)
        End Function

        Public Function OpenTextFieldParser(ByVal file As String, ByVal ParamArray fieldWidths As Integer()) As TextFieldParser
            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFieldParser(file, fieldWidths)
        End Function

        Public Function OpenTextFileReader(ByVal file As String) As IO.StreamReader
            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFileReader(file)
        End Function

        Public Function OpenTextFileReader(ByVal file As String, ByVal encoding As Encoding) As IO.StreamReader
            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFileReader(file, encoding)
        End Function

        Public Function OpenTextFileWriter(ByVal file As String, ByVal append As Boolean) As IO.StreamWriter
            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFileWriter(file, append)
        End Function

        Public Function OpenTextFileWriter(ByVal file As String, ByVal append As Boolean,
            ByVal encoding As Encoding) As IO.StreamWriter

            Return Microsoft.VisualBasic.FileIO.FileSystem.OpenTextFileWriter(file, append, encoding)
        End Function

        '= FRIEND =============================================================

        '''******************************************************************************
        ''' ;New
        ''' <summary>
        ''' Proxy class can only created by internal classes.
        ''' </summary>
        Friend Sub New()
        End Sub

        Private m_SpecialDirectoriesProxy As SpecialDirectoriesProxy = Nothing
    End Class
End Namespace
