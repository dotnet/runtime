' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
Option Strict On
Option Explicit On

Imports System
Imports System.Collections
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.Security
Imports System.Text

Imports Microsoft.VisualBasic.CompilerServices
#If TARGET_WINDOWS Then
Imports Microsoft.VisualBasic.CompilerServices.NativeMethods
Imports Microsoft.VisualBasic.CompilerServices.NativeTypes
#End If
Imports ExUtils = Microsoft.VisualBasic.CompilerServices.ExceptionUtils

Namespace Microsoft.VisualBasic.FileIO
    ''' <summary>
    '''  This class represents the file system on a computer. It allows browsing the existing drives, special directories;
    '''  and also contains some commonly use methods for IO tasks.
    ''' </summary>
    Public Class FileSystem
        ''' <summary>
        ''' Return the names of all available drives on the computer.
        ''' </summary>
        ''' <value>A ReadOnlyCollection(Of DriveInfo) containing all the current drives' names.</value>
        Public Shared ReadOnly Property Drives() As ObjectModel.ReadOnlyCollection(Of System.IO.DriveInfo)
            Get
                ' NOTE: Don't cache the collection since it may change without us knowing.
                ' The performance hit will be small since it's a small collection.
                Dim DriveInfoCollection As New ObjectModel.Collection(Of System.IO.DriveInfo)
                For Each DriveInfo As System.IO.DriveInfo In IO.DriveInfo.GetDrives()
                    DriveInfoCollection.Add(DriveInfo)
                Next
                Return New ObjectModel.ReadOnlyCollection(Of System.IO.DriveInfo)(DriveInfoCollection)
            End Get
        End Property

        ''' <summary>
        ''' Get or set the current working directory.
        ''' </summary>
        ''' <value>A String containing the path to the directory.</value>
        Public Shared Property CurrentDirectory() As String
            Get
                Return NormalizePath(IO.Directory.GetCurrentDirectory())
            End Get
            Set(ByVal value As String)
                IO.Directory.SetCurrentDirectory(value)
            End Set
        End Property

        ''' <summary>
        ''' Combines two path strings by adding a path separator.
        ''' </summary>
        ''' <param name="baseDirectory">The first part of the path.</param>
        ''' <param name="relativePath">The second part of the path, must be a relative path.</param>
        ''' <returns>A String contains the combined path.</returns>
        Public Shared Function CombinePath(ByVal baseDirectory As String, ByVal relativePath As String) As String
            If baseDirectory = "" Then
                Throw ExUtils.GetArgumentNullException("baseDirectory", SR.General_ArgumentEmptyOrNothing_Name, "baseDirectory")
            End If
            If relativePath = "" Then
                Return baseDirectory
            End If

            baseDirectory = IO.Path.GetFullPath(baseDirectory) ' Throw exceptions if BaseDirectoryPath is invalid.

            Return NormalizePath(IO.Path.Combine(baseDirectory, relativePath))
        End Function

        ''' <summary>
        '''  Determines whether the given path refers to an existing directory on disk.
        ''' </summary>
        ''' <param name="directory">The path to verify.</param>
        ''' <returns>True if DirectoryPath refers to an existing directory. Otherwise, False.</returns>
        Public Shared Function DirectoryExists(ByVal directory As String) As Boolean
            Return IO.Directory.Exists(directory)
        End Function

        ''' <summary>
        '''  Determines whether the given path refers to an existing file on disk.
        ''' </summary>
        ''' <param name="file">The path to verify.</param>
        ''' <returns>True if FilePath refers to an existing file on disk. Otherwise, False.</returns>
        Public Shared Function FileExists(ByVal file As String) As Boolean
            If Not String.IsNullOrEmpty(file) AndAlso
                (file.EndsWith(IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) Or
                file.EndsWith(IO.Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Then
                Return False
            End If

            Return IO.File.Exists(file)
        End Function

        ''' <summary>
        ''' Find files in the given folder that contain the given text.
        ''' </summary>
        ''' <param name="directory">The folder path to start from.</param>
        ''' <param name="containsText">The text to be found in file.</param>
        ''' <param name="ignoreCase">True to ignore case. Otherwise, False.</param>
        ''' <param name="searchType">SearchAllSubDirectories to find recursively. Otherwise, SearchTopLevelOnly.</param>
        ''' <returns>A string array containing the files that match the search condition.</returns>
        Public Shared Function FindInFiles(ByVal directory As String,
                                           ByVal containsText As String, ByVal ignoreCase As Boolean, ByVal searchType As SearchOption) As ObjectModel.ReadOnlyCollection(Of String)
            Return FindInFiles(directory, containsText, ignoreCase, searchType, Nothing)
        End Function

        ''' <summary>
        ''' Find files in the given folder that contain the given text.
        ''' </summary>
        ''' <param name="directory">The folder path to start from.</param>
        ''' <param name="containsText">The text to be found in file.</param>
        ''' <param name="ignoreCase">True to ignore case. Otherwise, False.</param>
        ''' <param name="searchType">SearchAllSubDirectories to find recursively. Otherwise, SearchTopLevelOnly.</param>
        ''' <param name="fileWildcards">The search patterns to use for the file name ("*.*")</param>
        ''' <returns>A string array containing the files that match the search condition.</returns>
        ''' <exception cref="System.ArgumentNullException">If one of the pattern is Null, Empty or all-spaces string.</exception>
        Public Shared Function FindInFiles(ByVal directory As String, ByVal containsText As String, ByVal ignoreCase As Boolean,
                                           ByVal searchType As SearchOption, ByVal ParamArray fileWildcards() As String) As ObjectModel.ReadOnlyCollection(Of String)
            ' Find the files with matching name.
            Dim NameMatchFiles As ObjectModel.ReadOnlyCollection(Of String) = FindFilesOrDirectories(
                FileOrDirectory.File, directory, searchType, fileWildcards)

            ' Find the files containing the given text.
            If containsText <> "" Then
                Dim ContainTextFiles As New ObjectModel.Collection(Of String)
                For Each FilePath As String In NameMatchFiles
                    If (FileContainsText(FilePath, containsText, ignoreCase)) Then
                        ContainTextFiles.Add(FilePath)
                    End If
                Next
                Return New ObjectModel.ReadOnlyCollection(Of String)(ContainTextFiles)
            Else
                Return NameMatchFiles
            End If
        End Function

        ''' <summary>
        ''' Return the paths of sub directories found directly under a directory.
        ''' </summary>
        ''' <param name="directory">The directory to find the sub directories inside.</param>
        ''' <returns>A ReadOnlyCollection(Of String) containing the matched directories' paths.</returns>
        Public Shared Function GetDirectories(ByVal directory As String) As ObjectModel.ReadOnlyCollection(Of String)

            Return FindFilesOrDirectories(FileOrDirectory.Directory, directory, SearchOption.SearchTopLevelOnly, Nothing)
        End Function

        ''' <summary>
        ''' Return the paths of sub directories found under a directory with the specified name patterns.
        ''' </summary>
        ''' <param name="directory">The directory to find the sub directories inside.</param>
        ''' <param name="searchType">SearchAllSubDirectories to find recursively. Otherwise, SearchTopLevelOnly.</param>
        ''' <param name="wildcards">The wildcards for the file name, for example "*.bmp", "*.txt"</param>
        ''' <returns>A ReadOnlyCollection(Of String) containing the matched directories' paths.</returns>
        Public Shared Function GetDirectories(ByVal directory As String, ByVal searchType As SearchOption,
                                              ByVal ParamArray wildcards() As String) As ObjectModel.ReadOnlyCollection(Of String)

            Return FindFilesOrDirectories(FileOrDirectory.Directory, directory, searchType, wildcards)
        End Function

        ''' <summary>
        '''  Returns the information object about the specified directory.
        ''' </summary>
        ''' <param name="directory">The path to the directory.</param>
        ''' <returns>A DirectoryInfo object containing the information about the specified directory.</returns>
        Public Shared Function GetDirectoryInfo(ByVal directory As String) As System.IO.DirectoryInfo
            Return New System.IO.DirectoryInfo(directory)
        End Function

        ''' <summary>
        ''' Return the information about the specified drive.
        ''' </summary>
        ''' <param name="drive">The path to the drive.</param>
        ''' <returns>A DriveInfo object containing the information about the specified drive.</returns>
        Public Shared Function GetDriveInfo(ByVal drive As String) As System.IO.DriveInfo
            Return New System.IO.DriveInfo(drive)
        End Function

        ''' <summary>
        '''  Returns the information about the specified file.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <returns>A FileInfo object containing the information about the specified file.</returns>
        Public Shared Function GetFileInfo(ByVal file As String) As System.IO.FileInfo
            file = NormalizeFilePath(file, "file")
            Return New System.IO.FileInfo(file)
        End Function

        ''' <summary>
        ''' Return an unordered collection of file paths found directly under a directory.
        ''' </summary>
        ''' <param name="directory">The directory to find the files inside.</param>
        ''' <returns>A ReadOnlyCollection(Of String) containing the matched files' paths.</returns>
        Public Shared Function GetFiles(ByVal directory As String) As ObjectModel.ReadOnlyCollection(Of String)
            Return FindFilesOrDirectories(FileOrDirectory.File, directory, SearchOption.SearchTopLevelOnly, Nothing)
        End Function

        ''' <summary>
        ''' Return an unordered collection of file paths found under a directory with the specified name patterns and containing the specified text.
        ''' </summary>
        ''' <param name="directory">The directory to find the files inside.</param>
        ''' <param name="searchType">SearchAllSubDirectories to find recursively. Otherwise, SearchTopLevelOnly.</param>
        ''' <param name="wildcards">The wildcards for the file name, for example "*.bmp", "*.txt"</param>
        ''' <returns>A ReadOnlyCollection(Of String) containing the matched files' paths.</returns>
        Public Shared Function GetFiles(ByVal directory As String, ByVal searchType As SearchOption,
                                        ByVal ParamArray wildcards() As String) As ObjectModel.ReadOnlyCollection(Of String)

            Return FindFilesOrDirectories(FileOrDirectory.File, directory, searchType, wildcards)
        End Function

        ''' <summary>
        ''' Return the name (and extension) from the given path string.
        ''' </summary>
        ''' <param name="path">The path string from which to obtain the file name (and extension).</param>
        ''' <returns>A String containing the name of the file or directory.</returns>
        ''' <exception cref="ArgumentException">path contains one or more of the invalid characters defined in InvalidPathChars.</exception>
        Public Shared Function GetName(ByVal path As String) As String
            Return IO.Path.GetFileName(path)
        End Function

        ''' <summary>
        ''' Returns the parent directory's path from a specified path.
        ''' </summary>
        ''' <param name="path">The path to a file or directory, this can be absolute or relative.</param>
        ''' <returns>
        ''' The path to the parent directory of that file or directory (whether absolute or relative depends on the input),
        ''' or an empty string if Path is a root directory.
        ''' </returns>
        ''' <exception cref="IO.Path.GetFullPath">See IO.Path.GetFullPath: If path is an invalid path.</exception>
        ''' <remarks>
        ''' The path will be normalized (for example: C:\Dir1////\\\Dir2 will become C:\Dir1\Dir2)
        ''' but will not be resolved (for example: C:\Dir1\Dir2\..\Dir3 WILL NOT become C:\Dir1\Dir3). Use CombinePath.
        ''' </remarks>
        Public Shared Function GetParentPath(ByVal path As String) As String
            ' Call IO.Path.GetFullPath to handle exception cases. Don't use the full path returned.
            IO.Path.GetFullPath(path)

            If IsRoot(path) Then
                Throw ExUtils.GetArgumentExceptionWithArgName("path", SR.IO_GetParentPathIsRoot_Path, path)
            Else
                Return IO.Path.GetDirectoryName(path.TrimEnd(
                    IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar))
            End If
        End Function

        ''' <summary>
        ''' Create a uniquely named zero-byte temporary file on disk and return the full path to that file.
        ''' </summary>
        ''' <returns>A String containing the name of the temporary file.</returns>
        Public Shared Function GetTempFileName() As String
            Return System.IO.Path.GetTempFileName()
        End Function

        ''' <summary>
        ''' Return an instance of a TextFieldParser for the given file.
        ''' </summary>
        ''' <param name="file">The path to the file to parse.</param>
        ''' <returns>An instance of a TextFieldParser.</returns>
        Public Shared Function OpenTextFieldParser(ByVal file As String) As TextFieldParser
            Return New TextFieldParser(file)
        End Function

        ''' <summary>
        ''' Return an instance of a TextFieldParser for the given file using the given delimiters.
        ''' </summary>
        ''' <param name="file">The path to the file to parse.</param>
        ''' <param name="delimiters">A list of delimiters.</param>
        ''' <returns>An instance of a TextFieldParser</returns>
        Public Shared Function OpenTextFieldParser(ByVal file As String, ByVal ParamArray delimiters As String()) As TextFieldParser
            Dim Result As New TextFieldParser(file)
            Result.SetDelimiters(delimiters)
            Result.TextFieldType = FieldType.Delimited
            Return Result
        End Function

        ''' <summary>
        ''' Return an instance of a TextFieldParser for the given file using the given field widths.
        ''' </summary>
        ''' <param name="file">The path to the file to parse.</param>
        ''' <param name="fieldWidths">A list of field widths.</param>
        ''' <returns>An instance of a TextFieldParser</returns>
        Public Shared Function OpenTextFieldParser(ByVal file As String, ByVal ParamArray fieldWidths As Integer()) As TextFieldParser
            Dim Result As New TextFieldParser(file)
            Result.SetFieldWidths(fieldWidths)
            Result.TextFieldType = FieldType.FixedWidth
            Return Result
        End Function

        ''' <summary>
        ''' Return a StreamReader for reading the given file using UTF-8 as preferred encoding.
        ''' </summary>
        ''' <param name="file">The file to open the StreamReader on.</param>
        ''' <returns>An instance of System.IO.StreamReader opened on the file (with FileShare.Read).</returns>
        Public Shared Function OpenTextFileReader(ByVal file As String) As IO.StreamReader
            Return OpenTextFileReader(file, Encoding.UTF8)
        End Function

        ''' <summary>
        ''' Return a StreamReader for reading the given file using the given encoding as preferred encoding.
        ''' </summary>
        ''' <param name="file">The file to open the StreamReader on.</param>
        ''' <param name="Encoding">The preferred encoding that will be used if the encoding of the file could not be detected.</param>
        ''' <returns>An instance of System.IO.StreamReader opened on the file (with FileShare.Read).</returns>
        Public Shared Function OpenTextFileReader(ByVal file As String, ByVal encoding As Encoding) As IO.StreamReader

            file = NormalizeFilePath(file, "file")
            Return New IO.StreamReader(file, encoding, detectEncodingFromByteOrderMarks:=True)
        End Function

        ''' <summary>
        ''' Return a StreamWriter for writing to the given file using UTF-8 encoding.
        ''' </summary>
        ''' <param name="file">The file to write to.</param>
        ''' <param name="Append">True to append to the content of the file. False to overwrite the content of the file.</param>
        ''' <returns>An instance of StreamWriter opened on the file (with FileShare.Read).</returns>
        Public Shared Function OpenTextFileWriter(ByVal file As String, ByVal append As Boolean) As IO.StreamWriter
            Return OpenTextFileWriter(file, append, Encoding.UTF8)
        End Function

        ''' <summary>
        ''' Return a StreamWriter for writing to the given file using the given encoding.
        ''' </summary>
        ''' <param name="file">The file to write to.</param>
        ''' <param name="Append">True to append to the content of the file. False to overwrite the content of the file.</param>
        ''' <param name="Encoding">The encoding to use to write to the file.</param>
        ''' <returns>An instance of StreamWriter opened on the file (with FileShare.Read).</returns>
        Public Shared Function OpenTextFileWriter(ByVal file As String, ByVal append As Boolean,
            ByVal encoding As Encoding) As IO.StreamWriter

            file = NormalizeFilePath(file, "file")
            Return New IO.StreamWriter(file, append, encoding)
        End Function

        ''' <summary>
        ''' Read the whole content of a file into a byte array.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <returns>A byte array contains the content of the file.</returns>
        ''' <exception cref="IO.IOException">If the length of the file is larger than Integer.MaxValue (~2GB).</exception>
        ''' <exception cref="IO.FileStream">See FileStream constructor and Read: for other exceptions.</exception>
        Public Shared Function ReadAllBytes(ByVal file As String) As Byte()
            Return IO.File.ReadAllBytes(file)
        End Function

        ''' <summary>
        ''' Read the whole content of a text file into a string using UTF-8 encoding.
        ''' </summary>
        ''' <param name="file">The path to the text file.</param>
        ''' <returns>A String contains the content of the given file.</returns>
        ''' <exception cref="IO.StreamReader">See StreamReader constructor and ReadToEnd.</exception>
        Public Shared Function ReadAllText(ByVal file As String) As String
            Return IO.File.ReadAllText(file)
        End Function

        ''' <summary>
        ''' Read the whole content of a text file into a string using the given encoding.
        ''' </summary>
        ''' <param name="file">The path to the text file.</param>
        ''' <param name="encoding">The character encoding to use if the encoding was not detected.</param>
        ''' <returns>A String contains the content of the given file.</returns>
        ''' <exception cref="IO.StreamReader">See StreamReader constructor and ReadToEnd.</exception>
        Public Shared Function ReadAllText(ByVal file As String, ByVal encoding As Encoding) As String
            Return IO.File.ReadAllText(file, encoding)
        End Function

        ''' <summary>
        ''' Copy an existing directory to a new directory,
        ''' throwing exception if there are existing files with the same name.
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        Public Shared Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String)
            CopyOrMoveDirectory(CopyOrMove.Copy, sourceDirectoryName, destinationDirectoryName,
                                overwrite:=False, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Copy an existing directory to a new directory,
        ''' overwriting existing files with the same name if specified.
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="overwrite">True to overwrite existing files with the same name. Otherwise False.</param>
        Public Shared Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal overwrite As Boolean)
            CopyOrMoveDirectory(CopyOrMove.Copy, sourceDirectoryName, destinationDirectoryName,
                                overwrite, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Copy an existing directory to a new directory,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' throwing exception if user cancels the operation (only applies if displaying progress dialog and confirmation dialogs).
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        Public Shared Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption)
            CopyOrMoveDirectory(CopyOrMove.Copy, sourceDirectoryName, destinationDirectoryName,
                                overwrite:=False, ToUIOptionInternal(showUI), UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Copy an existing directory to a new directory,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' throwing exception if user cancels the operation if specified. (only applies if displaying progress dialog and confirmation dialogs).
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="onUserCancel">ThrowException to throw exception if user cancels the operation. Otherwise DoNothing.</param>
        Public Shared Sub CopyDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            CopyOrMoveDirectory(CopyOrMove.Copy, sourceDirectoryName, destinationDirectoryName,
                                overwrite:=False, ToUIOptionInternal(showUI), onUserCancel)
        End Sub

        ''' <summary>
        ''' Copy an existing file to a new file. Overwriting a file of the same name is not allowed.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        Public Shared Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String)
            CopyOrMoveFile(CopyOrMove.Copy, sourceFileName, destinationFileName,
                           overwrite:=False, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Copy an existing file to a new file. Overwriting a file of the same name if specified.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="overwrite">True to overwrite existing file with the same name. Otherwise False.</param>
        Public Shared Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal overwrite As Boolean)
            CopyOrMoveFile(CopyOrMove.Copy, sourceFileName, destinationFileName,
                           overwrite, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Copy an existing file to a new file,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' will throw exception if user cancels the operation.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        Public Shared Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption)
            CopyOrMoveFile(CopyOrMove.Copy, sourceFileName, destinationFileName,
                           overwrite:=False, ToUIOptionInternal(showUI), UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Copy an existing file to a new file,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' will throw exception if user cancels the operation if specified.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="onUserCancel">ThrowException to throw exception if user cancels the operation. Otherwise DoNothing.</param>
        ''' <remarks>onUserCancel will be ignored if showUI = HideDialogs.</remarks>
        Public Shared Sub CopyFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            CopyOrMoveFile(CopyOrMove.Copy, sourceFileName, destinationFileName,
                           overwrite:=False, ToUIOptionInternal(showUI), onUserCancel)
        End Sub

        ''' <summary>
        '''  Creates a directory from the given path (including all parent directories).
        ''' </summary>
        ''' <param name="directory">The path to create the directory at.</param>
        Public Shared Sub CreateDirectory(ByVal directory As String)
            ' Get the full path. GetFullPath will throw if invalid path.
            directory = IO.Path.GetFullPath(directory)

            If IO.File.Exists(directory) Then
                Throw ExUtils.GetIOException(SR.IO_FileExists_Path, directory)
            End If

            ' CreateDirectory will create the full structure and not throw if directory exists.
            System.IO.Directory.CreateDirectory(directory)
        End Sub

        ''' <summary>
        ''' Delete the given directory, with options to recursively delete.
        ''' </summary>
        ''' <param name="directory">The path to the directory.</param>
        ''' <param name="onDirectoryNotEmpty">DeleteAllContents to delete everything. ThrowIfDirectoryNonEmpty to throw exception if the directory is not empty.</param>
        Public Shared Sub DeleteDirectory(ByVal directory As String, ByVal onDirectoryNotEmpty As DeleteDirectoryOption)
            DeleteDirectoryInternal(directory, onDirectoryNotEmpty,
                UIOptionInternal.NoUI, RecycleOption.DeletePermanently, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Delete the given directory, with options to recursively delete, show progress UI, send file to Recycle Bin; throwing exception if user cancels.
        ''' </summary>
        ''' <param name="directory">The path to the directory.</param>
        ''' <param name="showUI">True to shows progress window. Otherwise, False.</param>
        ''' <param name="recycle">SendToRecycleBin to delete to Recycle Bin. Otherwise DeletePermanently.</param>
        Public Shared Sub DeleteDirectory(ByVal directory As String, ByVal showUI As UIOption, ByVal recycle As RecycleOption)
            DeleteDirectoryInternal(directory, DeleteDirectoryOption.DeleteAllContents,
                                    ToUIOptionInternal(showUI), recycle, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Delete the given directory, with options to recursively delete, show progress UI, send file to Recycle Bin, and whether to throw exception if user cancels.
        ''' </summary>
        ''' <param name="directory">The path to the directory.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="recycle">SendToRecycleBin to delete to Recycle Bin. Otherwise DeletePermanently.</param>
        ''' <param name="onUserCancel">Throw exception when user cancel the UI operation or not.</param>
        Public Shared Sub DeleteDirectory(ByVal directory As String,
            ByVal showUI As UIOption, ByVal recycle As RecycleOption, ByVal onUserCancel As UICancelOption)
            DeleteDirectoryInternal(directory, DeleteDirectoryOption.DeleteAllContents,
                                    ToUIOptionInternal(showUI), recycle, onUserCancel)
        End Sub

        ''' <summary>
        ''' Delete the given file.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        Public Shared Sub DeleteFile(ByVal file As String)
            DeleteFileInternal(file, UIOptionInternal.NoUI, RecycleOption.DeletePermanently, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Delete the given file, with options to show progress UI, delete to recycle bin.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="recycle">SendToRecycleBin to delete to Recycle Bin. Otherwise DeletePermanently.</param>
        Public Shared Sub DeleteFile(ByVal file As String, ByVal showUI As UIOption, ByVal recycle As RecycleOption)
            DeleteFileInternal(file, ToUIOptionInternal(showUI), recycle, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Delete the given file, with options to show progress UI, delete to recycle bin, and whether to throw exception if user cancels.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="recycle">SendToRecycleBin to delete to Recycle Bin. Otherwise DeletePermanently.</param>
        ''' <param name="onUserCancel">Throw exception when user cancel the UI operation or not.</param>
        ''' <exception cref="IO.Path.GetFullPath">IO.Path.GetFullPath() exceptions: if FilePath is invalid.</exception>
        ''' <exception cref="IO.FileNotFoundException">if a file does not exist at FilePath</exception>
        Public Shared Sub DeleteFile(ByVal file As String, ByVal showUI As UIOption, ByVal recycle As RecycleOption,
                                     ByVal onUserCancel As UICancelOption)

            DeleteFileInternal(file, ToUIOptionInternal(showUI), recycle, onUserCancel)
        End Sub

        ''' <summary>
        ''' Move an existing directory to a new directory,
        ''' throwing exception if there are existing files with the same name.
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        Public Shared Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String)
            CopyOrMoveDirectory(CopyOrMove.Move, sourceDirectoryName, destinationDirectoryName,
                overwrite:=False, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Move an existing directory to a new directory,
        ''' overwriting existing files with the same name if specified.
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>        ''' <param name="overwrite">True to overwrite existing files with the same name. Otherwise False.</param>
        Public Shared Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal overwrite As Boolean)
            CopyOrMoveDirectory(CopyOrMove.Move, sourceDirectoryName, destinationDirectoryName,
                overwrite, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Move an existing directory to a new directory,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' throwing exception if user cancels the operation (only applies if displaying progress dialog and confirmation dialogs).
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        Public Shared Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption)
            CopyOrMoveDirectory(CopyOrMove.Move, sourceDirectoryName, destinationDirectoryName,
                                overwrite:=False, ToUIOptionInternal(showUI), UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Move an existing directory to a new directory,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' throwing exception if user cancels the operation if specified. (only applies if displaying progress dialog and confirmation dialogs).
        ''' </summary>
        ''' <param name="sourceDirectoryName">The path to the source directory, can be relative or absolute.</param>
        ''' <param name="destinationDirectoryName">The path to the target directory, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="onUserCancel">ThrowException to throw exception if user cancels the operation. Otherwise DoNothing.</param>
        Public Shared Sub MoveDirectory(ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            CopyOrMoveDirectory(CopyOrMove.Move, sourceDirectoryName, destinationDirectoryName,
                                overwrite:=False, ToUIOptionInternal(showUI), onUserCancel)
        End Sub

        ''' <summary>
        ''' Move an existing file to a new file. Overwriting a file of the same name is not allowed.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        Public Shared Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String)
            CopyOrMoveFile(CopyOrMove.Move, sourceFileName, destinationFileName,
                           overwrite:=False, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Move an existing file to a new file. Overwriting a file of the same name if specified.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="overwrite">True to overwrite existing file with the same name. Otherwise False.</param>
        Public Shared Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal overwrite As Boolean)
            CopyOrMoveFile(CopyOrMove.Move, sourceFileName, destinationFileName,
                           overwrite, UIOptionInternal.NoUI, UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Move an existing file to a new file,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' will throw exception if user cancels the operation.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        Public Shared Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption)
            CopyOrMoveFile(CopyOrMove.Move, sourceFileName, destinationFileName,
                           overwrite:=False, ToUIOptionInternal(showUI), UICancelOption.ThrowException)
        End Sub

        ''' <summary>
        ''' Move an existing file to a new file,
        ''' displaying progress dialog and confirmation dialogs if specified,
        ''' will throw exception if user cancels the operation if specified.
        ''' </summary>
        ''' <param name="sourceFileName">The path to the source file, can be relative or absolute.</param>
        ''' <param name="destinationFileName">The path to the destination file, can be relative or absolute. Parent directory will always be created.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="onUserCancel">ThrowException to throw exception if user cancels the operation. Otherwise DoNothing.</param>
        ''' <remarks>onUserCancel will be ignored if showUI = HideDialogs.</remarks>
        Public Shared Sub MoveFile(ByVal sourceFileName As String, ByVal destinationFileName As String, ByVal showUI As UIOption, ByVal onUserCancel As UICancelOption)
            CopyOrMoveFile(CopyOrMove.Move, sourceFileName, destinationFileName,
                           overwrite:=False, ToUIOptionInternal(showUI), onUserCancel)
        End Sub

        ''' <summary>
        ''' Rename a directory, does not act like a move.
        ''' </summary>
        ''' <param name="directory">The path of the directory to be renamed.</param>
        ''' <param name="newName">The new name to change to. This must not contain path information.</param>
        ''' <exception cref="IO.Path.GetFullPath">IO.Path.GetFullPath exceptions: If directory is invalid.</exception>
        ''' <exception cref="System.ArgumentException">If newName is Nothing or Empty String or contains path information.</exception>
        ''' <exception cref="IO.FileNotFoundException">If directory does not point to an existing directory.</exception>
        ''' <exception cref="IO.IOException">If directory points to a root directory.
        '''     Or if there's an existing directory or an existing file with the same name.</exception>
        Public Shared Sub RenameDirectory(ByVal directory As String, ByVal newName As String)
            ' Get the full path. This will handle invalid path exceptions.
            directory = IO.Path.GetFullPath(directory)
            ' Throw if device path.
            ThrowIfDevicePath(directory)

            ' Directory is a root directory. This does not require IO access so it's cheaper up front.
            If IsRoot(directory) Then
                Throw ExUtils.GetIOException(SR.IO_DirectoryIsRoot_Path, directory)
            End If

            ' Throw if directory does not exist.
            If Not IO.Directory.Exists(directory) Then
                Throw ExUtils.GetDirectoryNotFoundException(SR.IO_DirectoryNotFound_Path, directory)
            End If

            ' Verify newName is not null.
            If newName = "" Then
                Throw ExUtils.GetArgumentNullException(
                    "newName", SR.General_ArgumentEmptyOrNothing_Name, "newName")
            End If

            ' Calculate new path. GetFullPathFromNewName will verify newName is only a name.
            Dim FullNewPath As String = GetFullPathFromNewName(GetParentPath(directory), newName, "newName")
            Debug.Assert(GetParentPath(FullNewPath).Equals(GetParentPath(directory),
                                                           StringComparison.OrdinalIgnoreCase), "Invalid FullNewPath")

            ' Verify that the new path does not conflict.
            EnsurePathNotExist(FullNewPath)

            IO.Directory.Move(directory, FullNewPath)
        End Sub

        ''' <summary>
        ''' Renames a file, does not change the file location.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <param name="newName">The new name to change to. This must not contain path information.</param>
        ''' <exception cref="IO.Path.GetFullPath">IO.Path.GetFullPath exceptions: If file is invalid.</exception>
        ''' <exception cref="System.ArgumentException">If newName is Nothing or Empty String or contains path information.</exception>
        ''' <exception cref="IO.FileNotFoundException">If file does not point to an existing file.</exception>
        ''' <exception cref="IO.IOException">If there's an existing directory or an existing file with the same name.</exception>
        Public Shared Sub RenameFile(ByVal file As String, ByVal newName As String)
            ' Get the full path. This will handle invalid path exceptions.
            file = NormalizeFilePath(file, "file")
            ' Throw if device path.
            ThrowIfDevicePath(file)

            ' Throw if file does not exist.
            If Not IO.File.Exists(file) Then
                Throw ExUtils.GetFileNotFoundException(file, SR.IO_FileNotFound_Path, file)
            End If

            ' Verify newName is not null.
            If newName = "" Then
                Throw ExUtils.GetArgumentNullException(
                    "newName", SR.General_ArgumentEmptyOrNothing_Name, "newName")
            End If

            ' Calculate new path. GetFullPathFromNewName will verify that newName is only a name.
            Dim FullNewPath As String = GetFullPathFromNewName(GetParentPath(file), newName, "newName")
            Debug.Assert(GetParentPath(FullNewPath).Equals(GetParentPath(file),
                                                           StringComparison.OrdinalIgnoreCase), "Invalid FullNewPath")

            ' Verify that the new path does not conflict.
            EnsurePathNotExist(FullNewPath)

            IO.File.Move(file, FullNewPath)
        End Sub

        ''' <summary>
        ''' Overwrites or appends the specified byte array to the specified file,
        ''' creating the file if it does not exist.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <param name="data">The byte array to write to the file.</param>
        ''' <param name="append">True to append the text to the existing content. False to overwrite the existing content.</param>
        ''' <exception cref="IO.FileStream">See FileStream constructor and Write: For other exceptions.</exception>
        Public Shared Sub WriteAllBytes(ByVal file As String, ByVal data() As Byte, ByVal append As Boolean)

            ' Cannot call through IO.File.WriteAllBytes (since they don't support append)
            ' so only check for trailing separator
            CheckFilePathTrailingSeparator(file, NameOf(file))

            Dim FileStream As IO.FileStream = Nothing
            Try
                Dim IOFileMode As IO.FileMode
                If append Then
                    IOFileMode = IO.FileMode.Append
                Else
                    IOFileMode = IO.FileMode.Create ' CreateNew or Truncate.
                End If

                FileStream = New IO.FileStream(file,
                    mode:=IOFileMode, access:=IO.FileAccess.Write, share:=IO.FileShare.Read)
                FileStream.Write(data, 0, data.Length)
            Finally
                If Not FileStream Is Nothing Then
                    FileStream.Close()
                End If
            End Try
        End Sub

        ''' <summary>
        ''' Overwrites or appends the given text using UTF-8 encoding to the given file,
        ''' creating the file if it does not exist.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <param name="text">The text to write to the file.</param>
        ''' <param name="append">True to append the text to the existing content. False to overwrite the existing content.</param>
        ''' <exception cref="IO.StreamWriter">See StreamWriter constructor and Write: For other exceptions.</exception>
        Public Shared Sub WriteAllText(ByVal file As String, ByVal text As String, ByVal append As Boolean)
            WriteAllText(file, text, append, Encoding.UTF8)
        End Sub

        ''' <summary>
        ''' Overwrites or appends the given text using the given encoding to the given file,
        ''' creating the file if it does not exist.
        ''' </summary>
        ''' <param name="file">The path to the file.</param>
        ''' <param name="text">The text to write to the file.</param>
        ''' <param name="append">True to append the text to the existing content. False to overwrite the existing content.</param>
        ''' <param name="encoding">The encoding to use.</param>
        ''' <exception cref="IO.StreamWriter">See StreamWriter constructor and Write: For other exceptions.</exception>
        Public Shared Sub WriteAllText(ByVal file As String, ByVal text As String, ByVal append As Boolean,
                                       ByVal encoding As Encoding)

            'Cannot call through IO.File.WriteAllText (since they don't support: append, prefer current encoding than specified one)
            ' so only check for trailing separator.
            CheckFilePathTrailingSeparator(file, NameOf(file))

            Dim StreamWriter As IO.StreamWriter = Nothing
            Try
                ' If appending to a file and it exists, attempt to detect the current encoding and use it.
                If append AndAlso IO.File.Exists(file) Then
                    Dim StreamReader As IO.StreamReader = Nothing
                    Try
                        StreamReader = New IO.StreamReader(file, encoding, detectEncodingFromByteOrderMarks:=True)
                        Dim Chars(10 - 1) As Char
                        StreamReader.Read(Chars, 0, 10) ' Read the next 10 characters to activate auto detect encoding.
                        encoding = StreamReader.CurrentEncoding ' Set encoding to the detected encoding.
                    Catch ex As IO.IOException
                        ' Ignore IOException.
                    Finally
                        If StreamReader IsNot Nothing Then
                            StreamReader.Close()
                        End If
                    End Try
                End If

                ' StreamWriter uses FileShare.Read by default.
                StreamWriter = New IO.StreamWriter(file, append, encoding)
                StreamWriter.Write(text)
            Finally
                If Not StreamWriter Is Nothing Then
                    StreamWriter.Close()
                End If
            End Try
        End Sub

        ''' <summary>
        ''' Normalize the path, but throw exception if the path ends with separator.
        ''' </summary>
        ''' <param name="Path">The input path.</param>
        ''' <param name="ParamName">The parameter name to include in the exception if one is raised.</param>
        ''' <returns>The normalized path.</returns>
        Friend Shared Function NormalizeFilePath(ByVal Path As String, ByVal ParamName As String) As String
            CheckFilePathTrailingSeparator(Path, ParamName)
            Return NormalizePath(Path)
        End Function

        ''' <summary>
        ''' Get full path, get long format, and remove any pending separator.
        ''' </summary>
        ''' <param name="Path">The path to be normalized.</param>
        ''' <returns>The normalized path.</returns>
        ''' <exception cref="IO.Path.GetFullPath">See IO.Path.GetFullPath for possible exceptions.</exception>
        ''' <remarks>Keep this function since we might change the implementation / behavior later.</remarks>
        Friend Shared Function NormalizePath(ByVal Path As String) As String
            Return GetLongPath(RemoveEndingSeparator(IO.Path.GetFullPath(Path)))
        End Function

        ''' <summary>
        ''' Throw ArgumentException if the file path ends with a separator.
        ''' </summary>
        ''' <param name="path">The file path.</param>
        ''' <param name="paramName">The parameter name to include in ArgumentException.</param>
        Friend Shared Sub CheckFilePathTrailingSeparator(ByVal path As String, ByVal paramName As String)
            If path = "" Then ' Check for argument null
                Throw ExUtils.GetArgumentNullException(paramName)
            End If
            If path.EndsWith(IO.Path.DirectorySeparatorChar, StringComparison.Ordinal) Or
                path.EndsWith(IO.Path.AltDirectorySeparatorChar, StringComparison.Ordinal) Then
                Throw ExUtils.GetArgumentExceptionWithArgName(paramName, SR.IO_FilePathException)
            End If
        End Sub

        ''' <summary>
        ''' Add an array of string into a Generic Collection of String.
        ''' </summary>
        Private Shared Sub AddToStringCollection(ByVal StrCollection As ObjectModel.Collection(Of String), ByVal StrArray() As String)
            ' CONSIDER: : BCL to support adding an array of string directly into a generic string collection?
            Debug.Assert(StrCollection IsNot Nothing, "StrCollection is NULL")

            If StrArray IsNot Nothing Then
                For Each Str As String In StrArray
                    If Not StrCollection.Contains(Str) Then
                        StrCollection.Add(Str)
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Handles exception cases and calls shell or framework to copy / move directory.
        ''' </summary>
        ''' <param name="operation">select Copy or Move operation.</param>
        ''' <param name="sourceDirectoryName">the source directory</param>
        ''' <param name="destinationDirectoryName">the target directory</param>
        ''' <param name="overwrite">overwrite files</param>
        ''' <param name="showUI">calls into shell to copy / move directory</param>
        ''' <param name="onUserCancel">throw exception if user cancels the operation or not.</param>
        ''' <exception cref="IO.Path.GetFullPath">IO.Path.GetFullPath exceptions: If SourceDirectoryPath or TargetDirectoryPath is invalid.
        '''     Or if NewName contains path information.</exception>
        ''' <exception cref="System.ArgumentException">If Source or Target is device path (\\.\).</exception>
        ''' <exception cref="IO.DirectoryNotFoundException">Source directory does not exist as a directory.</exception>
        ''' <exception cref="System.ArgumentNullException">If NewName = "".</exception>
        ''' <exception cref="IO.IOException">SourceDirectoryPath and TargetDirectoryPath are the same.
        '''     IOException: Target directory is under source directory - cyclic operation.
        '''     IOException: TargetDirectoryPath points to an existing file.
        '''     IOException: Some files and directories can not be copied.</exception>
        Private Shared Sub CopyOrMoveDirectory(ByVal operation As CopyOrMove,
                                               ByVal sourceDirectoryName As String, ByVal destinationDirectoryName As String,
                                               ByVal overwrite As Boolean, ByVal showUI As UIOptionInternal, ByVal onUserCancel As UICancelOption)
            Debug.Assert(System.Enum.IsDefined(GetType(CopyOrMove), operation), "Invalid Operation")

            ' Verify enums.
            VerifyUICancelOption("onUserCancel", onUserCancel)

            ' Get the full path and remove any separators at the end. This will handle invalid path exceptions.
            ' IMPORTANT: sourceDirectoryName and destinationDirectoryName should be used for exception throwing ONLY.
            Dim SourceDirectoryFullPath As String = NormalizePath(sourceDirectoryName)
            Dim TargetDirectoryFullPath As String = NormalizePath(destinationDirectoryName)

            ' Throw if device path.
            ThrowIfDevicePath(SourceDirectoryFullPath)
            ThrowIfDevicePath(TargetDirectoryFullPath)

            ' Throw if source directory does not exist.
            If Not IO.Directory.Exists(SourceDirectoryFullPath) Then
                Throw ExUtils.GetDirectoryNotFoundException(SR.IO_DirectoryNotFound_Path, sourceDirectoryName)
            End If

            ' Throw if source directory is a root directory.
            If IsRoot(SourceDirectoryFullPath) Then
                Throw ExUtils.GetIOException(SR.IO_DirectoryIsRoot_Path, sourceDirectoryName)
            End If

            ' Throw if there's a file at TargetDirectoryFullPath.
            If IO.File.Exists(TargetDirectoryFullPath) Then
                Throw ExUtils.GetIOException(SR.IO_FileExists_Path, destinationDirectoryName)
            End If

            ' Throw if source and target are the same.
            If TargetDirectoryFullPath.Equals(SourceDirectoryFullPath, StringComparison.OrdinalIgnoreCase) Then
                Throw ExUtils.GetIOException(SR.IO_SourceEqualsTargetDirectory)
            End If

            ' Throw if cyclic operation (target is under source). A sample case is
            '   Source = C:\Dir1\Dir2
            '   Target = C:\Dir1\Dir2\Dir3\Dir4.
            ' NOTE: Do not use StartWith since it does not allow specifying InvariantCultureIgnoreCase.
            If TargetDirectoryFullPath.Length > SourceDirectoryFullPath.Length AndAlso
                TargetDirectoryFullPath.Substring(0, SourceDirectoryFullPath.Length).Equals(
                SourceDirectoryFullPath, StringComparison.OrdinalIgnoreCase) Then
                Debug.Assert(TargetDirectoryFullPath.Length > SourceDirectoryFullPath.Length, "Target path should be longer")

                If TargetDirectoryFullPath.Chars(SourceDirectoryFullPath.Length) = IO.Path.DirectorySeparatorChar Then
                    Throw ExUtils.GetInvalidOperationException(SR.IO_CyclicOperation)
                End If
            End If

            ' NOTE: Decision to create target directory is different for Shell and Framework call.

            If showUI <> UIOptionInternal.NoUI AndAlso Environment.UserInteractive Then
                ShellCopyOrMove(operation, FileOrDirectory.Directory, SourceDirectoryFullPath, TargetDirectoryFullPath, showUI, onUserCancel)
            Else
                ' Otherwise, copy the directory using System.IO.
                FxCopyOrMoveDirectory(operation, SourceDirectoryFullPath, TargetDirectoryFullPath, overwrite)
            End If
        End Sub

        ''' <summary>
        ''' Copies or moves the directory using Framework.
        ''' </summary>
        ''' <param name="operation">Copy or Move.</param>
        ''' <param name="sourceDirectoryPath">Source path - must be full path.</param>
        ''' <param name="targetDirectoryPath">Target path - must be full path.</param>
        ''' <param name="Overwrite">True to overwrite the files. Otherwise, False.</param>
        ''' <exception cref="IO.IOException">Some files or directories cannot be copied or moved.</exception>
        Private Shared Sub FxCopyOrMoveDirectory(ByVal operation As CopyOrMove,
                                                 ByVal sourceDirectoryPath As String, ByVal targetDirectoryPath As String, ByVal overwrite As Boolean)

            Debug.Assert(System.Enum.IsDefined(GetType(CopyOrMove), operation), "Invalid Operation")
            Debug.Assert(sourceDirectoryPath <> "" And IO.Path.IsPathRooted(sourceDirectoryPath), "Invalid Source")
            Debug.Assert(targetDirectoryPath <> "" And IO.Path.IsPathRooted(targetDirectoryPath), "Invalid Target")

            ' Special case for moving: If target directory does not exist, AND both directories are on same drive,
            '   use IO.Directory.Move for performance gain (not copying).
            If operation = CopyOrMove.Move And Not IO.Directory.Exists(targetDirectoryPath) And
                IsOnSameDrive(sourceDirectoryPath, targetDirectoryPath) Then

                ' Create the target's parent. IO.Directory.CreateDirectory won't throw if it exists.
                IO.Directory.CreateDirectory(GetParentPath(targetDirectoryPath))

                Try
                    IO.Directory.Move(sourceDirectoryPath, targetDirectoryPath)
                    Exit Sub
                Catch ex As IO.IOException
                Catch ex As UnauthorizedAccessException
                    ' Ignore IO.Directory.Move specific exceptions here. Try to do as much as possible later.
                End Try
            End If

            ' Create the target, create the root node, and call the recursive function.
            System.IO.Directory.CreateDirectory(targetDirectoryPath)
            Debug.Assert(IO.Directory.Exists(targetDirectoryPath), "Should be able to create Target Directory")

            Dim SourceDirectoryNode As New DirectoryNode(sourceDirectoryPath, targetDirectoryPath)
            Dim Exceptions As New ListDictionary
            CopyOrMoveDirectoryNode(operation, SourceDirectoryNode, overwrite, Exceptions)

            ' Throw the final exception if there were exceptions during copy / move.
            If Exceptions.Count > 0 Then
                Dim IOException As New IO.IOException(SR.IO_CopyMoveRecursive)
                For Each Entry As DictionaryEntry In Exceptions
                    IOException.Data.Add(Entry.Key, Entry.Value)
                Next
                Throw IOException
            End If
        End Sub

        ''' <summary>
        ''' Given a directory node, copy or move that directory tree.
        ''' </summary>
        ''' <param name="Operation">Specify whether to move or copy the directories.</param>
        ''' <param name="SourceDirectoryNode">The source node. Only copy / move directories contained in the source node.</param>
        ''' <param name="Overwrite">True to overwrite sub-files. Otherwise False.</param>
        ''' <param name="Exceptions">The list of accumulated exceptions while doing the copy / move</param>
        Private Shared Sub CopyOrMoveDirectoryNode(ByVal Operation As CopyOrMove,
                                                   ByVal SourceDirectoryNode As DirectoryNode, ByVal Overwrite As Boolean, ByVal Exceptions As ListDictionary)

            Debug.Assert(System.Enum.IsDefined(GetType(CopyOrMove), Operation), "Invalid Operation")
            Debug.Assert(Exceptions IsNot Nothing, "Null exception list")
            Debug.Assert(SourceDirectoryNode IsNot Nothing, "Null source node")

            ' Create the target directory. If we encounter known exceptions, add the exception to the exception list and quit.
            Try
                If Not IO.Directory.Exists(SourceDirectoryNode.TargetPath) Then
                    IO.Directory.CreateDirectory(SourceDirectoryNode.TargetPath)
                End If
            Catch ex As Exception
                If (TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                    TypeOf ex Is IO.DirectoryNotFoundException OrElse TypeOf ex Is NotSupportedException OrElse
                    TypeOf ex Is SecurityException) Then
                    Exceptions.Add(SourceDirectoryNode.Path, ex.Message)
                    Exit Sub
                Else
                    Throw
                End If
            End Try
            Debug.Assert(IO.Directory.Exists(SourceDirectoryNode.TargetPath), "TargetPath should have existed or exception should be thrown")
            If Not IO.Directory.Exists(SourceDirectoryNode.TargetPath) Then
                Exceptions.Add(SourceDirectoryNode.TargetPath, ExUtils.GetDirectoryNotFoundException(SR.IO_DirectoryNotFound_Path, SourceDirectoryNode.TargetPath))
                Exit Sub
            End If

            ' Copy / move all the files under this directory to target directory.
            For Each SubFilePath As String In IO.Directory.GetFiles(SourceDirectoryNode.Path)
                Try
                    CopyOrMoveFile(Operation, SubFilePath, IO.Path.Combine(SourceDirectoryNode.TargetPath, IO.Path.GetFileName(SubFilePath)),
                        Overwrite, UIOptionInternal.NoUI, UICancelOption.ThrowException)
                Catch ex As Exception
                    If (TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                        TypeOf ex Is SecurityException OrElse TypeOf ex Is NotSupportedException) Then
                        Exceptions.Add(SubFilePath, ex.Message)
                    Else
                        Throw
                    End If
                End Try
            Next

            ' Copy / move all the sub directories under this directory to target directory.
            For Each SubDirectoryNode As DirectoryNode In SourceDirectoryNode.SubDirs
                CopyOrMoveDirectoryNode(Operation, SubDirectoryNode, Overwrite, Exceptions)
            Next

            ' If this is a move, try to delete the current directory.
            ' Using recursive:=False since we expect the content should be emptied by now.
            If Operation = CopyOrMove.Move Then
                Try
                    IO.Directory.Delete(SourceDirectoryNode.Path, recursive:=False)
                Catch ex As Exception
                    If (TypeOf ex Is IO.IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                        TypeOf ex Is SecurityException OrElse TypeOf ex Is IO.DirectoryNotFoundException) Then
                        Exceptions.Add(SourceDirectoryNode.Path, ex.Message)
                    Else
                        Throw
                    End If
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Copies or move files. This will be called from CopyFile and MoveFile.
        ''' </summary>
        ''' <param name="Operation">Copy or Move.</param>
        ''' <param name="sourceFileName">Path to source file.</param>
        ''' <param name="destinationFileName">Path to target file.</param>
        ''' <param name="Overwrite">True = Overwrite. This flag will be ignored if ShowUI.</param>
        ''' <param name="ShowUI">Hide or show the UIDialogs.</param>
        ''' <param name="OnUserCancel">Throw exception in case user cancel using UI or not.</param>
        ''' <exception cref="IO.Path.GetFullPath">
        '''   IO.Path.GetFullPath exceptions: If SourceFilePath or TargetFilePath is invalid.
        '''   ArgumentException: If Source or Target is device path (\\.\).
        '''   FileNotFoundException: If SourceFilePath does not exist (including pointing to an existing directory).
        '''   IOException: If TargetFilePath points to an existing directory.
        '''   ArgumentNullException: If NewName = "".
        '''   ArgumentException: If NewName contains path information.
        ''' </exception>
        Private Shared Sub CopyOrMoveFile(ByVal operation As CopyOrMove,
                                          ByVal sourceFileName As String, ByVal destinationFileName As String,
                                          ByVal overwrite As Boolean, ByVal showUI As UIOptionInternal, ByVal onUserCancel As UICancelOption
                                          )
            Debug.Assert(System.Enum.IsDefined(GetType(CopyOrMove), operation), "Invalid Operation")

            ' Verify enums.
            VerifyUICancelOption("onUserCancel", onUserCancel)

            ' Get the full path and remove any separator at the end. This will handle invalid path exceptions.
            ' IMPORTANT: sourceFileName and destinationFileName should be used for throwing user exceptions ONLY.
            Dim sourceFileFullPath As String = NormalizeFilePath(sourceFileName, "sourceFileName")
            Dim destinationFileFullPath As String = NormalizeFilePath(destinationFileName, "destinationFileName")

            ' Throw if device path.
            ThrowIfDevicePath(sourceFileFullPath)
            ThrowIfDevicePath(destinationFileFullPath)

            ' Throw exception if SourceFilePath does not exist.
            If Not IO.File.Exists(sourceFileFullPath) Then
                Throw ExUtils.GetFileNotFoundException(sourceFileName, SR.IO_FileNotFound_Path, sourceFileName)
            End If

            ' Throw exception if TargetFilePath is an existing directory.
            If IO.Directory.Exists(destinationFileFullPath) Then
                Throw ExUtils.GetIOException(SR.IO_DirectoryExists_Path, destinationFileName)
            End If

            ' Always create the target's parent directory(s).
            IO.Directory.CreateDirectory(GetParentPath(destinationFileFullPath))

            ' If ShowUI, attempt to call Shell function.
            If showUI <> UIOptionInternal.NoUI AndAlso System.Environment.UserInteractive Then
                ShellCopyOrMove(operation, FileOrDirectory.File, sourceFileFullPath, destinationFileFullPath, showUI, onUserCancel)
                Exit Sub
            End If

            ' Use Framework.
            If operation = CopyOrMove.Copy OrElse
                sourceFileFullPath.Equals(destinationFileFullPath, StringComparison.OrdinalIgnoreCase) Then
                ' Call IO.File.Copy if this is a copy operation.
                ' In addition, if sourceFileFullPath is the same as destinationFileFullPath,
                '   IO.File.Copy will throw, IO.File.Move will not.
                '   Whatever overwrite flag is passed in, IO.File.Move should throw exception,
                '   so call IO.File.Copy to get the exception as well.
                IO.File.Copy(sourceFileFullPath, destinationFileFullPath, overwrite)
            Else ' MoveFile with support for overwrite flag.
                If overwrite Then ' User wants to overwrite destination.
                    ' Why not checking for destination existence: user may not have read permission / ACL,
                    ' but have write permission / ACL thus cannot see but can delete / overwrite destination.

                    If Environment.OSVersion.Platform = PlatformID.Win32NT Then ' Platforms supporting MoveFileEx.
#If TARGET_WINDOWS Then
                        Try
                            Dim succeed As Boolean = NativeMethods.MoveFileEx(
                                    sourceFileFullPath, destinationFileFullPath, m_MOVEFILEEX_FLAGS)
                            ' GetLastWin32Error has to be close to PInvoke call. FxCop rule.
                            If Not succeed Then
                                ThrowWinIOError(System.Runtime.InteropServices.Marshal.GetLastWin32Error())
                            End If
                        Catch
                            Throw
                        End Try
#End If
                    Else ' Non Windows
                        ' IO.File.Delete will not throw if destinationFileFullPath does not exist
                        ' (user may not have permission to discover this, but have permission to overwrite),
                        ' so always delete the destination.
                        IO.File.Delete(destinationFileFullPath)

                        IO.File.Move(sourceFileFullPath, destinationFileFullPath)
                    End If
                Else ' Overwrite = False, call Framework.
                    IO.File.Move(sourceFileFullPath, destinationFileFullPath)
                End If ' Overwrite
            End If
        End Sub

        ''' <summary>
        ''' Delete the given directory, with options to recursively delete, show progress UI, send file to Recycle Bin, and whether to throw exception if user cancels.
        ''' </summary>
        ''' <param name="directory">The path to the directory.</param>
        ''' <param name="onDirectoryNotEmpty">DeleteAllContents to delete everything. ThrowIfDirectoryNonEmpty to throw exception if the directory is not empty.</param>
        ''' <param name="showUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="recycle">SendToRecycleBin to delete to Recycle Bin. Otherwise DeletePermanently.</param>
        ''' <param name="onUserCancel">Throw exception when user cancel the UI operation or not.</param>
        ''' <remarks>If user wants shell features, onDirectoryNotEmpty is ignored.</remarks>
        Private Shared Sub DeleteDirectoryInternal(ByVal directory As String, ByVal onDirectoryNotEmpty As DeleteDirectoryOption,
                                                   ByVal showUI As UIOptionInternal, ByVal recycle As RecycleOption, ByVal onUserCancel As UICancelOption)

            VerifyDeleteDirectoryOption("onDirectoryNotEmpty", onDirectoryNotEmpty)
            VerifyRecycleOption("recycle", recycle)
            VerifyUICancelOption("onUserCancel", onUserCancel)

            ' Get the full path. This will handle invalid paths exceptions.
            Dim directoryFullPath As String = IO.Path.GetFullPath(directory)

            ' Throw if device path.
            ThrowIfDevicePath(directoryFullPath)

            If Not IO.Directory.Exists(directoryFullPath) Then
                Throw ExUtils.GetDirectoryNotFoundException(SR.IO_DirectoryNotFound_Path, directory)
            End If

            If IsRoot(directoryFullPath) Then
                Throw ExUtils.GetIOException(SR.IO_DirectoryIsRoot_Path, directory)
            End If

            ' If user want shell features (Progress, Recycle Bin), call shell operation.
            ' We don't need to consider onDirectoryNotEmpty here.
            If (showUI <> UIOptionInternal.NoUI) AndAlso Environment.UserInteractive Then
                ShellDelete(directoryFullPath, showUI, recycle, onUserCancel, FileOrDirectory.Directory)
                Exit Sub
            End If

            ' Otherwise, call Framework's method.
            IO.Directory.Delete(directoryFullPath, onDirectoryNotEmpty = DeleteDirectoryOption.DeleteAllContents)
        End Sub

        ''' <summary>
        ''' Delete the given file, with options to show progress UI, send file to Recycle Bin, throw exception if user cancels.
        ''' </summary>
        ''' <param name="file">the path to the file</param>
        ''' <param name="showUI">AllDialogs, OnlyErrorDialogs, or NoUI</param>
        ''' <param name="recycle">DeletePermanently or SendToRecycleBin</param>
        ''' <param name="onUserCancel">DoNothing or ThrowException</param>
        ''' <remarks></remarks>
        Private Shared Sub DeleteFileInternal(ByVal file As String, ByVal showUI As UIOptionInternal, ByVal recycle As RecycleOption,
                                              ByVal onUserCancel As UICancelOption)
            ' Verify enums
            VerifyRecycleOption("recycle", recycle)
            VerifyUICancelOption("onUserCancel", onUserCancel)

            ' Get the full path. This will handle invalid path exceptions.
            Dim fileFullPath As String = NormalizeFilePath(file, "file")

            ' Throw if device path.
            ThrowIfDevicePath(fileFullPath)

            If Not IO.File.Exists(fileFullPath) Then
                Throw ExUtils.GetFileNotFoundException(file, SR.IO_FileNotFound_Path, file)
            End If

            ' If user want shell features (Progress, Recycle Bin), call shell operation.
            If (showUI <> UIOptionInternal.NoUI) AndAlso Environment.UserInteractive Then
                ShellDelete(fileFullPath, showUI, recycle, onUserCancel, FileOrDirectory.File)
                Exit Sub
            End If

            IO.File.Delete(fileFullPath)
        End Sub

        ''' <summary>
        ''' Verify that a path does not refer to an existing directory or file. Throw exception otherwise.
        ''' </summary>
        ''' <param name="Path">The path to verify.</param>
        ''' <remarks>This is used for RenameFile and RenameDirectory.</remarks>
        Private Shared Sub EnsurePathNotExist(ByVal Path As String)
            If IO.File.Exists(Path) Then
                Throw ExUtils.GetIOException(SR.IO_FileExists_Path, Path)
            End If

            If IO.Directory.Exists(Path) Then
                Throw ExUtils.GetIOException(SR.IO_DirectoryExists_Path, Path)
            End If
        End Sub

        ''' <summary>
        ''' Determines if the given file in the path contains the given text.
        ''' </summary>
        ''' <param name="FilePath">The file to check for.</param>
        ''' <param name="Text">The text to search for.</param>
        ''' <returns>True if the file contains the text. Otherwise False.</returns>
        Private Shared Function FileContainsText(ByVal FilePath As String, ByVal Text As String, ByVal IgnoreCase As Boolean) _
            As Boolean

            Debug.Assert(FilePath <> "" AndAlso IO.Path.IsPathRooted(FilePath), FilePath)
            Debug.Assert(Text <> "", "Empty text")

            ' To support different encoding (UTF-8, ASCII).
            ' Read the file in byte, then use Decoder classes to get a string from those bytes and compare.
            ' Decoder class maintains state between the conversion, allowing it to correctly decode
            ' byte sequences that span adjacent blocks. (sources\ndp\clr\src\BCL\System\Text\Decoder.cs).

            Dim DEFAULT_BUFFER_SIZE As Integer = 1024 ' default buffer size to read each time.
            Dim FileStream As IO.FileStream = Nothing

            Try
                ' Open the file with ReadWrite share, least possibility to fail.
                FileStream = New IO.FileStream(FilePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)

                ' Read in a byte buffer with default size, then open a StreamReader to detect the encoding of the file.
                Dim DetectedEncoding As System.Text.Encoding = System.Text.Encoding.Default ' Use Default encoding as the fall back.
                Dim ByteBuffer(DEFAULT_BUFFER_SIZE - 1) As Byte
                Dim ByteCount As Integer = 0
                ByteCount = FileStream.Read(ByteBuffer, 0, ByteBuffer.Length)
                If ByteCount > 0 Then
                    ' Only take the number of bytes returned to avoid false detection.
                    Dim MemoryStream As New IO.MemoryStream(ByteBuffer, 0, ByteCount)
                    Dim StreamReader As New IO.StreamReader(MemoryStream, DetectedEncoding, detectEncodingFromByteOrderMarks:=True)
                    StreamReader.ReadLine()
                    DetectedEncoding = StreamReader.CurrentEncoding
                End If

                ' Calculate the real buffer size to read in each time to ensure read in at least a character array
                ' as long as or longer than the given text.
                ' 1. Calculate the maximum number of bytes required to encode the given text in the detected encoding.
                ' 2. If it's larger than DEFAULT_BUFFER_SIZE, use it. Otherwise, use DEFAULT_BUFFER_SIZE.
                Dim MaxByteDetectedEncoding As Integer = DetectedEncoding.GetMaxByteCount(Text.Length)
                Dim BufferSize As Integer = Math.Max(MaxByteDetectedEncoding, DEFAULT_BUFFER_SIZE)

                ' Dim up the byte buffer and the search helpers (See TextSearchHelper).
                Dim SearchHelper As New TextSearchHelper(DetectedEncoding, Text, IgnoreCase)

                ' If the buffer size is larger than DEFAULT_BUFFER_SIZE, read more from the file stream
                ' to fill up the byte buffer.
                If BufferSize > DEFAULT_BUFFER_SIZE Then
                    ReDim Preserve ByteBuffer(BufferSize - 1)
                    ' Read maximum ByteBuffer.Length - ByteCount (from the initial read) bytes from the stream
                    ' into the ByteBuffer, starting at ByteCount position.
                    Dim AdditionalByteCount As Integer = FileStream.Read(ByteBuffer, ByteCount, ByteBuffer.Length - ByteCount)
                    ByteCount += AdditionalByteCount ' The total byte count now is ByteCount + AdditionalByteCount
                    Debug.Assert(ByteCount <= ByteBuffer.Length)
                End If

                ' Start the search and read until end of file.
                Do
                    If ByteCount > 0 Then
                        If SearchHelper.IsTextFound(ByteBuffer, ByteCount) Then
                            Return True
                        End If
                    End If
                    ByteCount = FileStream.Read(ByteBuffer, 0, ByteBuffer.Length)
                Loop While (ByteCount > 0)

                Return False
            Catch ex As Exception

                ' We don't expect the following types of exceptions, so we'll re-throw it together with Yukon's exceptions.
                Debug.Assert(Not (TypeOf ex Is ArgumentException Or TypeOf ex Is ArgumentOutOfRangeException Or
                    TypeOf ex Is ArgumentNullException Or TypeOf ex Is IO.DirectoryNotFoundException Or
                    TypeOf ex Is IO.FileNotFoundException Or TypeOf ex Is ObjectDisposedException Or
                    TypeOf ex Is RankException Or TypeOf ex Is ArrayTypeMismatchException Or
                    TypeOf ex Is InvalidCastException), "Unexpected exception: " & ex.ToString())

                ' These exceptions may happen and we'll return False here.
                If TypeOf ex Is IO.IOException Or
                    TypeOf ex Is NotSupportedException Or
                    TypeOf ex Is SecurityException Or
                    TypeOf ex Is UnauthorizedAccessException Then

                    Return False
                Else
                    ' Re-throw Yukon's exceptions, PathTooLong exception (linked directory) and others.
                    Throw
                End If
            Finally
                If FileStream IsNot Nothing Then
                    FileStream.Close()
                End If
            End Try
        End Function

        ''' <summary>
        ''' Find files or directories in a directory and return them in a string collection.
        ''' </summary>
        ''' <param name="FileOrDirectory">Specify to search for file or directory.</param>
        ''' <param name="directory">The directory path to start from.</param>
        ''' <param name="searchType">SearchAllSubDirectories to find recursively. Otherwise, SearchTopLevelOnly.</param>
        ''' <param name="wildcards">The search patterns to use for the file name ("*.*")</param>
        ''' <returns>A ReadOnlyCollection(Of String) containing the files that match the search condition.</returns>
        ''' <exception cref="System.ArgumentException">ArgumentNullException: If one of the pattern is Null, Empty or all-spaces string.</exception>
        Private Shared Function FindFilesOrDirectories(ByVal FileOrDirectory As FileOrDirectory, ByVal directory As String,
                                                       ByVal searchType As SearchOption, ByVal wildcards() As String) As ObjectModel.ReadOnlyCollection(Of String)

            Dim Results As New ObjectModel.Collection(Of String)
            FindFilesOrDirectories(FileOrDirectory, directory, searchType, wildcards, Results)

            Return New ObjectModel.ReadOnlyCollection(Of String)(Results)
        End Function

        ''' <summary>
        ''' Find files or directories in a directory and return them in a string collection.
        ''' </summary>
        ''' <param name="FileOrDirectory">Specify to search for file or directory.</param>
        ''' <param name="directory">The directory path to start from.</param>
        ''' <param name="searchType">SearchAllSubDirectories to find recursively. Otherwise, SearchTopLevelOnly.</param>
        ''' <param name="wildcards">The search patterns to use for the file name ("*.*")</param>
        ''' <param name="Results">A ReadOnlyCollection(Of String) containing the files that match the search condition.</param>
        Private Shared Sub FindFilesOrDirectories(ByVal FileOrDirectory As FileOrDirectory, ByVal directory As String,
                                                  ByVal searchType As SearchOption, ByVal wildcards() As String, ByVal Results As ObjectModel.Collection(Of String))
            Debug.Assert(Results IsNot Nothing, "Results is NULL")

            ' Verify enums.
            VerifySearchOption("searchType", searchType)

            directory = NormalizePath(directory)

            ' Verify wild cards. Only TrimEnd since empty space is allowed at the start of file / directory name.
            If wildcards IsNot Nothing Then
                For Each wildcard As String In wildcards
                    ' Throw if empty string or Nothing.
                    If wildcard.TrimEnd() = "" Then
                        Throw ExUtils.GetArgumentNullException("wildcards", SR.IO_GetFiles_NullPattern)
                    End If
                Next
            End If

            ' Search for files / directories directly under given directory (based on wildcards).
            If wildcards Is Nothing OrElse wildcards.Length = 0 Then
                AddToStringCollection(Results, FindPaths(FileOrDirectory, directory, Nothing))
            Else
                For Each wildcard As String In wildcards
                    AddToStringCollection(Results, FindPaths(FileOrDirectory, directory, wildcard))
                Next
            End If

            ' Search in sub directories if specified.
            If searchType = SearchOption.SearchAllSubDirectories Then
                For Each SubDirectoryPath As String In IO.Directory.GetDirectories(directory)
                    FindFilesOrDirectories(FileOrDirectory, SubDirectoryPath, searchType, wildcards, Results)
                Next
            End If
        End Sub

        ''' <summary>
        ''' Given a directory, a pattern, find the files or directories directly under the given directory that match the pattern.
        ''' </summary>
        ''' <param name="FileOrDirectory">Specify whether to find files or directories.</param>
        ''' <param name="directory">The directory to look under.</param>
        ''' <param name="wildCard">*.bmp, *.txt, ... Nothing to search for every thing.</param>
        ''' <returns>An array of String containing the paths found.</returns>
        Private Shared Function FindPaths(ByVal FileOrDirectory As FileOrDirectory, ByVal directory As String, ByVal wildCard As String) As String()
            If FileOrDirectory = FileSystem.FileOrDirectory.Directory Then
                If wildCard = "" Then
                    Return IO.Directory.GetDirectories(directory)
                Else
                    Return IO.Directory.GetDirectories(directory, wildCard)
                End If
            Else
                If wildCard = "" Then
                    Return IO.Directory.GetFiles(directory)
                Else
                    Return IO.Directory.GetFiles(directory, wildCard)
                End If
            End If
        End Function

        ''' <summary>
        ''' Returns the fullpath from a directory path and a new name. Throws exception if the new name contains path information.
        ''' </summary>
        ''' <param name="Path">The directory path.</param>
        ''' <param name="NewName">The new name to combine to the directory path.</param>
        ''' <param name="ArgumentName">The argument name to throw in the exception.</param>
        ''' <returns>A String contains the full path.</returns>
        ''' <remarks>This function is used for CopyFile, RenameFile and RenameDirectory.</remarks>
        Private Shared Function GetFullPathFromNewName(ByVal Path As String,
                                                       ByVal NewName As String, ByVal ArgumentName As String) As String
            Debug.Assert(Path <> "" AndAlso IO.Path.IsPathRooted(Path), Path)
            Debug.Assert(Path.Equals(IO.Path.GetFullPath(Path)), Path)
            Debug.Assert(NewName <> "", "Null NewName")
            Debug.Assert(ArgumentName <> "", "Null argument name")

            ' In copy file, rename file and rename directory, the new name must be a name only.
            ' Enforce that by combine the path, normalize it, then compare the new parent directory with the old parent directory.
            ' These two directories must be the same.

            ' Throw exception if NewName contains any separator characters.
            If NewName.IndexOfAny(m_SeparatorChars) >= 0 Then
                Throw ExUtils.GetArgumentExceptionWithArgName(ArgumentName, SR.IO_ArgumentIsPath_Name_Path, ArgumentName, NewName)
            End If

            ' Call GetFullPath again to catch invalid characters in NewName.
            Dim FullPath As String = RemoveEndingSeparator(IO.Path.GetFullPath(IO.Path.Combine(Path, NewName)))

            ' If the new parent directory path does not equal the parent directory passed in, throw exception.
            ' Use this to check for cases like "..", checking for separators will not block this case.
            If Not GetParentPath(FullPath).Equals(Path, StringComparison.OrdinalIgnoreCase) Then
                Throw ExUtils.GetArgumentExceptionWithArgName(ArgumentName, SR.IO_ArgumentIsPath_Name_Path, ArgumentName, NewName)
            End If

            Return FullPath
        End Function

        ''' <summary>
        '''  Returns the given path in long format (v.s 8.3 format) if the path exists.
        ''' </summary>
        ''' <param name="FullPath">The path to resolve to long format.</param>
        ''' <returns>The given path in long format if the path exists.</returns>
        ''' <remarks>
        '''  GetLongPathName is a PInvoke call and requires unmanaged code permission.
        '''  Use DirectoryInfo.GetFiles and GetDirectories (which call FindFirstFile) so that we always have permission.
        '''</remarks>
        Private Shared Function GetLongPath(ByVal FullPath As String) As String
            Debug.Assert(Not FullPath = "" AndAlso IO.Path.IsPathRooted(FullPath), "Must be full path")
            Try
                ' If root path, return itself. UNC path do not recognize 8.3 format in root path, so this is fine.
                If IsRoot(FullPath) Then
                    Return FullPath
                End If

                ' DirectoryInfo.GetFiles and GetDirectories call FindFirstFile which resolves 8.3 path.
                ' Get the DirectoryInfo (user must have code permission or access permission).
                Dim DInfo As New IO.DirectoryInfo(GetParentPath(FullPath))

                If IO.File.Exists(FullPath) Then
                    Debug.Assert(DInfo.GetFiles(IO.Path.GetFileName(FullPath)).Length = 1, "Must found exactly 1")
                    Return DInfo.GetFiles(IO.Path.GetFileName(FullPath))(0).FullName
                ElseIf IO.Directory.Exists(FullPath) Then
                    Debug.Assert(DInfo.GetDirectories(IO.Path.GetFileName(FullPath)).Length = 1,
                                 "Must found exactly 1")
                    Return DInfo.GetDirectories(IO.Path.GetFileName(FullPath))(0).FullName
                Else
                    Return FullPath ' Path does not exist, cannot resolve.
                End If
            Catch ex As Exception
                ' Ignore these type of exceptions and return FullPath. These type of exceptions should either be caught by calling functions
                ' or indicate that caller does not have enough permission and should get back the 8.3 path.
                If TypeOf ex Is ArgumentException OrElse
                    TypeOf ex Is ArgumentNullException OrElse
                    TypeOf ex Is IO.PathTooLongException OrElse
                    TypeOf ex Is NotSupportedException OrElse
                    TypeOf ex Is IO.DirectoryNotFoundException OrElse
                    TypeOf ex Is SecurityException OrElse
                    TypeOf ex Is UnauthorizedAccessException Then

                    Debug.Assert(Not (TypeOf ex Is ArgumentException OrElse
                        TypeOf ex Is ArgumentNullException OrElse
                        TypeOf ex Is IO.PathTooLongException OrElse
                        TypeOf ex Is NotSupportedException), "These exceptions should be caught above")

                    Return FullPath
                Else
                    Throw
                End If
            End Try
        End Function

        ''' <summary>
        ''' Checks to see if the two paths is on the same drive.
        ''' </summary>
        ''' <param name="Path1"></param>
        ''' <param name="Path2"></param>
        ''' <returns>True if the 2 paths is on the same drive. False otherwise.</returns>
        ''' <remarks>Just a string comparison.</remarks>
        Private Shared Function IsOnSameDrive(ByVal Path1 As String, ByVal Path2 As String) As Boolean
            ' Remove any separators at the end for the same reason in IsRoot.
            Path1 = Path1.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
            Path2 = Path2.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
            Return String.Equals(IO.Path.GetPathRoot(Path1), IO.Path.GetPathRoot(Path2), StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Checks if the full path is a root path.
        ''' </summary>
        ''' <param name="Path">The path to check.</param>
        ''' <returns>True if FullPath is a root path, False otherwise.</returns>
        ''' <remarks>
        '''   IO.Path.GetPathRoot: C: -> C:, C:\ -> C:\, \\machine\share -> \\machine\share,
        '''           BUT \\machine\share\ -> \\machine\share (No separator here).
        '''   Therefore, remove any separators at the end to have correct result.
        ''' </remarks>
        Private Shared Function IsRoot(ByVal Path As String) As Boolean
            ' This function accepts a relative path since GetParentPath will call this,
            ' and GetParentPath accept relative paths.
            If Not IO.Path.IsPathRooted(Path) Then
                Return False
            End If

            Path = Path.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
            Return String.Equals(Path, IO.Path.GetPathRoot(Path), StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Removes all directory separators at the end of a path.
        ''' </summary>
        ''' <param name="Path">a full or relative path.</param>
        ''' <returns>If Path is a root path, the same value. Otherwise, removes any directory separators at the end.</returns>
        ''' <remarks>We decided not to return path with separators at the end.</remarks>
        Private Shared Function RemoveEndingSeparator(ByVal Path As String) As String
            If IO.Path.IsPathRooted(Path) Then
                ' If the path is rooted, attempt to check if it is a root path.
                ' Note: IO.Path.GetPathRoot: C: -> C:, C:\ -> C:\, \\myshare\mydir -> \\myshare\mydir
                ' BUT \\myshare\mydir\ -> \\myshare\mydir!!! This function will remove the ending separator of
                ' \\myshare\mydir\ as well. Do not use IsRoot here.
                If Path.Equals(IO.Path.GetPathRoot(Path), StringComparison.OrdinalIgnoreCase) Then
                    Return Path
                End If
            End If

            ' Otherwise, remove all separators at the end.
            Return Path.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
        End Function

        ''' <summary>
        ''' Sets relevant flags on the SHFILEOPSTRUCT and calls SHFileOperation to copy move file / directory.
        ''' </summary>
        ''' <param name="Operation">Copy or move.</param>
        ''' <param name="TargetType">The target is a file or directory?</param>
        ''' <param name="FullSourcePath">Full path to source directory / file.</param>
        ''' <param name="FullTargetPath">Full path to target directory / file.</param>
        ''' <param name="ShowUI">Show all dialogs or just the error dialogs.</param>
        ''' <param name="OnUserCancel">Throw exception or ignore if user cancels the operation.</param>
        ''' <remarks>
        ''' Copy/MoveFile will call this directly. Copy/MoveDirectory will call ShellCopyOrMoveDirectory first
        ''' to change the path if needed.
        ''' </remarks>
        Private Shared Sub ShellCopyOrMove(ByVal Operation As CopyOrMove, ByVal TargetType As FileOrDirectory,
            ByVal FullSourcePath As String, ByVal FullTargetPath As String, ByVal ShowUI As UIOptionInternal, ByVal OnUserCancel As UICancelOption)
            Debug.Assert(System.Enum.IsDefined(GetType(CopyOrMove), Operation))
            Debug.Assert(System.Enum.IsDefined(GetType(FileOrDirectory), TargetType))
            Debug.Assert(FullSourcePath <> "" And IO.Path.IsPathRooted(FullSourcePath), "Invalid FullSourcePath")
            Debug.Assert(FullTargetPath <> "" And IO.Path.IsPathRooted(FullTargetPath), "Invalid FullTargetPath")
            Debug.Assert(ShowUI <> UIOptionInternal.NoUI, "Why call ShellDelete if ShowUI is NoUI???")

#If TARGET_WINDOWS Then
            ' Set operation type.
            Dim OperationType As SHFileOperationType
            If Operation = CopyOrMove.Copy Then
                OperationType = SHFileOperationType.FO_COPY
            Else
                OperationType = SHFileOperationType.FO_MOVE
            End If

            ' Set operation details.
            Dim OperationFlags As ShFileOperationFlags = GetOperationFlags(ShowUI)

            ' *** Special action for Directory only. ***
            Dim FinalSourcePath As String = FullSourcePath
            If TargetType = FileOrDirectory.Directory Then
                ' Shell behavior: If target does not exist, create target and copy / move source CONTENT into target.
                '                 If target exists, copy / move source into target.
                ' To have our behavior:
                '   If target does not exist, create target parent (or shell will throw) and call ShellCopyOrMove.
                '   If target exists, attach "\*" to FullSourcePath and call ShellCopyOrMove.
                ' In case of Move, since moving the directory, just create the target parent.
                If IO.Directory.Exists(FullTargetPath) Then
                    FinalSourcePath = IO.Path.Combine(FullSourcePath, "*")
                Else
                    IO.Directory.CreateDirectory(GetParentPath(FullTargetPath))
                End If
            End If

            ' Call into ShellFileOperation.
            ShellFileOperation(OperationType, OperationFlags, FinalSourcePath, FullTargetPath, OnUserCancel, TargetType)

            ' *** Special action for Directory only. ***
            ' In case target does exist, and it's a move, we actually move content and leave the source directory.
            ' Clean up here.
            If Operation = CopyOrMove.Move And TargetType = FileOrDirectory.Directory Then
                If IO.Directory.Exists(FullSourcePath) Then
                    If IO.Directory.GetDirectories(FullSourcePath).Length = 0 _
                        AndAlso IO.Directory.GetFiles(FullSourcePath).Length = 0 Then
                        IO.Directory.Delete(FullSourcePath, recursive:=False)
                    End If
                End If
            End If
#Else
            Throw New PlatformNotSupportedException(SR.NoShellCopyOrMove)
#End If

        End Sub

        ''' <summary>
        ''' Sets relevant flags on the SHFILEOPSTRUCT and calls into SHFileOperation to delete file / directory.
        ''' </summary>
        ''' <param name="FullPath">Full path to the file / directory.</param>
        ''' <param name="ShowUI">ShowDialogs to display progress and confirmation dialogs. Otherwise HideDialogs.</param>
        ''' <param name="recycle">SendToRecycleBin to delete to Recycle Bin. Otherwise DeletePermanently.</param>
        ''' <param name="OnUserCancel">Throw exception or not if the operation was canceled (by user or errors in the system).</param>
        ''' <remarks>
        ''' We don't need to consider Recursive flag here since we already verify that in DeleteDirectory.
        ''' </remarks>
        Private Shared Sub ShellDelete(ByVal FullPath As String,
            ByVal ShowUI As UIOptionInternal, ByVal recycle As RecycleOption, ByVal OnUserCancel As UICancelOption, ByVal FileOrDirectory As FileOrDirectory)

            Debug.Assert(FullPath <> "" And IO.Path.IsPathRooted(FullPath), "FullPath must be a full path")
            Debug.Assert(ShowUI <> UIOptionInternal.NoUI, "Why call ShellDelete if ShowUI is NoUI???")

#If TARGET_WINDOWS Then
            ' Set fFlags to control the operation details.
            Dim OperationFlags As ShFileOperationFlags = GetOperationFlags(ShowUI)
            If (recycle = RecycleOption.SendToRecycleBin) Then
                OperationFlags = OperationFlags Or ShFileOperationFlags.FOF_ALLOWUNDO
            End If

            ShellFileOperation(SHFileOperationType.FO_DELETE, OperationFlags, FullPath, Nothing, OnUserCancel, FileOrDirectory)
#Else
            Throw New PlatformNotSupportedException(SR.NoShellCopyOrMove)
#End If
        End Sub

#If TARGET_WINDOWS Then
        ''' <summary>
        ''' Calls NativeMethods.SHFileOperation with the given SHFILEOPSTRUCT, notifies the shell of change,
        ''' and throw exceptions if needed.
        ''' </summary>
        ''' <param name="OperationType">Value from SHFileOperationType, specifying Copy / Move / Delete</param>
        ''' <param name="OperationFlags">Value from ShFileOperationFlags, specifying overwrite, recycle bin, etc...</param>
        ''' <param name="FullSource">The full path to the source.</param>
        ''' <param name="FullTarget">The full path to the target. Nothing if this is a Delete operation.</param>
        ''' <param name="OnUserCancel">Value from UICancelOption, specifying to throw or not when user cancels the operation.</param>
        Private Shared Sub ShellFileOperation(ByVal OperationType As SHFileOperationType, ByVal OperationFlags As ShFileOperationFlags,
            ByVal FullSource As String, ByVal FullTarget As String, ByVal OnUserCancel As UICancelOption, ByVal FileOrDirectory As FileOrDirectory)

            Debug.Assert(System.Enum.IsDefined(GetType(SHFileOperationType), OperationType))
            Debug.Assert(OperationType <> SHFileOperationType.FO_RENAME, "Don't call Shell to rename")
            Debug.Assert(FullSource <> "" And IO.Path.IsPathRooted(FullSource), "Invalid FullSource path")
            Debug.Assert(OperationType = SHFileOperationType.FO_DELETE OrElse (FullTarget <> "" And IO.Path.IsPathRooted(FullTarget)), "Invalid FullTarget path")

            ' Get the SHFILEOPSTRUCT
            Dim OperationInfo As SHFILEOPSTRUCT = GetShellOperationInfo(OperationType, OperationFlags, FullSource, FullTarget)

            Dim Result As Integer
            Try
                Result = NativeMethods.SHFileOperation(OperationInfo)
                ' Notify the shell in case some changes happened.
                NativeMethods.SHChangeNotify(SHChangeEventTypes.SHCNE_DISKEVENTS,
                                             SHChangeEventParameterFlags.SHCNF_DWORD, IntPtr.Zero, IntPtr.Zero)
            Catch
                Throw
            Finally
            End Try

            ' If the operation was canceled, check OnUserCancel and throw OperationCanceledException if needed.
            ' Otherwise, check the result and throw the appropriate exception if there is an error code.
            If OperationInfo.fAnyOperationsAborted Then
                If OnUserCancel = UICancelOption.ThrowException Then
                    Throw New OperationCanceledException()
                End If
            ElseIf Result <> 0 Then
                ThrowWinIOError(Result)
            End If
        End Sub

        ''' <summary>
        ''' Returns an SHFILEOPSTRUCT used by SHFileOperation based on the given parameters.
        ''' </summary>
        ''' <param name="OperationType">One of the SHFileOperationType value: copy, move or delete.</param>
        ''' <param name="OperationFlags">Combination SHFileOperationFlags values: details of the operation.</param>
        ''' <param name="SourcePath">The source file / directory path.</param>
        ''' <param name="TargetPath">The target file / directory path. Nothing in case of delete.</param>
        ''' <returns>A fully initialized SHFILEOPSTRUCT.</returns>
        Private Shared Function GetShellOperationInfo(
                            ByVal OperationType As SHFileOperationType, ByVal OperationFlags As ShFileOperationFlags,
                            ByVal SourcePath As String, Optional ByVal TargetPath As String = Nothing) As SHFILEOPSTRUCT
            Debug.Assert(SourcePath <> "" And IO.Path.IsPathRooted(SourcePath), "Invalid SourcePath")

            Return GetShellOperationInfo(OperationType, OperationFlags, New String() {SourcePath}, TargetPath)
        End Function

        ''' <summary>
        ''' Returns an SHFILEOPSTRUCT used by SHFileOperation based on the given parameters.
        ''' </summary>
        ''' <param name="OperationType">One of the SHFileOperationType value: copy, move or delete.</param>
        ''' <param name="OperationFlags">Combination SHFileOperationFlags values: details of the operation.</param>
        ''' <param name="SourcePaths">A string array containing the paths of source files. Must not be empty.</param>
        ''' <param name="TargetPath">The target file / directory path. Nothing in case of delete.</param>
        ''' <returns>A fully initialized SHFILEOPSTRUCT.</returns>
        Private Shared Function GetShellOperationInfo(
                            ByVal OperationType As SHFileOperationType, ByVal OperationFlags As ShFileOperationFlags,
                            ByVal SourcePaths() As String, Optional ByVal TargetPath As String = Nothing) As SHFILEOPSTRUCT
            Debug.Assert(System.Enum.IsDefined(GetType(SHFileOperationType), OperationType), "Invalid OperationType")
            Debug.Assert(TargetPath = "" Or IO.Path.IsPathRooted(TargetPath), "Invalid TargetPath")
            Debug.Assert(SourcePaths IsNot Nothing AndAlso SourcePaths.Length > 0, "Invalid SourcePaths")

            Dim OperationInfo As SHFILEOPSTRUCT

            ' Set wFunc - the operation.
            OperationInfo.wFunc = CType(OperationType, UInteger)

            ' Set fFlags - the operation details.
            OperationInfo.fFlags = CType(OperationFlags, UShort)

            ' Set pFrom and pTo - the paths.
            OperationInfo.pFrom = GetShellPath(SourcePaths)
            If TargetPath Is Nothing Then
                OperationInfo.pTo = Nothing
            Else
                OperationInfo.pTo = GetShellPath(TargetPath)
            End If

            ' Set other fields.
            OperationInfo.hNameMappings = IntPtr.Zero
            ' Try to set hwnd to the process's MainWindowHandle. If exception occurs, use IntPtr.Zero, which is desktop.
            Try
                OperationInfo.hwnd = Process.GetCurrentProcess.MainWindowHandle
            Catch ex As Exception
                If TypeOf (ex) Is SecurityException OrElse
                    TypeOf (ex) Is InvalidOperationException OrElse
                    TypeOf (ex) Is NotSupportedException Then
                    ' GetCurrentProcess can throw SecurityException. MainWindowHandle can throw InvalidOperationException or NotSupportedException.
                    OperationInfo.hwnd = IntPtr.Zero
                Else
                    Throw
                End If
            End Try
            OperationInfo.lpszProgressTitle = String.Empty ' We don't set this since we don't have any FOF_SIMPLEPROGRESS.

            Return OperationInfo
        End Function

        ''' <summary>
        ''' Return the ShFileOperationFlags based on the ShowUI option.
        ''' </summary>
        ''' <param name="ShowUI">UIOptionInternal value.</param>
        Private Shared Function GetOperationFlags(ByVal ShowUI As UIOptionInternal) As ShFileOperationFlags
            Dim OperationFlags As ShFileOperationFlags = m_SHELL_OPERATION_FLAGS_BASE
            If (ShowUI = UIOptionInternal.OnlyErrorDialogs) Then
                OperationFlags = OperationFlags Or m_SHELL_OPERATION_FLAGS_HIDE_UI
            End If
            Return OperationFlags
        End Function

        ''' <summary>
        ''' Returns the special path format required for pFrom and pTo of SHFILEOPSTRUCT. See NativeMethod.
        ''' </summary>
        ''' <param name="FullPath">The full path to be converted.</param>
        ''' <returns>A string in the required format.</returns>
        Private Shared Function GetShellPath(ByVal FullPath As String) As String
            Debug.Assert(FullPath <> "" And IO.Path.IsPathRooted(FullPath), "Must be full path")

            Return GetShellPath(New String() {FullPath})
        End Function

        ''' <summary>
        ''' Returns the special path format required for pFrom and pTo of SHFILEOPSTRUCT. See NativeMethod.
        ''' </summary>
        ''' <param name="FullPaths">A string array containing the paths for the operation.</param>
        ''' <returns>A string in the required format.</returns>
        Private Shared Function GetShellPath(ByVal FullPaths() As String) As String
#If DEBUG Then
            Debug.Assert(FullPaths IsNot Nothing, "FullPaths is NULL")
            Debug.Assert(FullPaths.Length > 0, "FullPaths() is empty array")
            For Each FullPath As String In FullPaths
                Debug.Assert(FullPath <> "" And IO.Path.IsPathRooted(FullPath), FullPath)
            Next
#End If

            ' Each path will end with a Null character.
            Dim MultiString As New StringBuilder()
            For Each FullPath As String In FullPaths
                MultiString.Append(FullPath & ControlChars.NullChar)
            Next
            ' Don't need to append another Null character since String always end with Null character by default.
            Debug.Assert(MultiString.ToString.EndsWith(ControlChars.NullChar, StringComparison.Ordinal))

            Return MultiString.ToString()
        End Function
#End If

        ''' <summary>
        ''' Throw an argument exception if the given path starts with "\\.\" (device path).
        ''' </summary>
        ''' <param name="path">The path to check.</param>
        ''' <remarks>
        ''' FileStream already throws exception with device path, so our code only check for device path in Copy / Move / Delete / Rename.
        ''' </remarks>
        Private Shared Sub ThrowIfDevicePath(ByVal path As String)
            If path.StartsWith("\\.\", StringComparison.Ordinal) Then
                Throw ExceptionUtils.GetArgumentExceptionWithArgName("path", SR.IO_DevicePath)
            End If
        End Sub

#If TARGET_WINDOWS Then
        ''' <summary>
        ''' Given an error code from winerror.h, throw the appropriate exception.
        ''' </summary>
        ''' <param name="errorCode">An error code from winerror.h.</param>
        ''' <remarks>
        ''' - This method is based on sources\ndp\clr\src\BCL\System\IO\_Error.cs::WinIOError, except the following.
        ''' - Exception message does not contain the path since at this point it is normalized.
        ''' - Instead of using PInvoke of GetMessage and MakeHRFromErrorCode, use managed code.
        ''' </remarks>
        Private Shared Sub ThrowWinIOError(ByVal errorCode As Integer)
            Select Case errorCode
                Case NativeTypes.ERROR_FILE_NOT_FOUND
                    Throw New IO.FileNotFoundException()
                Case NativeTypes.ERROR_PATH_NOT_FOUND
                    Throw New IO.DirectoryNotFoundException()
                Case NativeTypes.ERROR_ACCESS_DENIED
                    Throw New UnauthorizedAccessException()
                Case NativeTypes.ERROR_FILENAME_EXCED_RANGE
                    Throw New IO.PathTooLongException()
                Case NativeTypes.ERROR_INVALID_DRIVE
                    Throw New IO.DriveNotFoundException()
                Case NativeTypes.ERROR_OPERATION_ABORTED, NativeTypes.ERROR_CANCELLED
                    Throw New OperationCanceledException()
                Case Else
                    ' Including these from _Error.cs::WinIOError.
                    'Case NativeTypes.ERROR_ALREADY_EXISTS
                    'Case NativeTypes.ERROR_INVALID_PARAMETER
                    'Case NativeTypes.ERROR_SHARING_VIOLATION
                    'Case NativeTypes.ERROR_FILE_EXISTS
                    Throw New IO.IOException((New Win32Exception(errorCode)).Message,
                        System.Runtime.InteropServices.Marshal.GetHRForLastWin32Error())
            End Select
        End Sub
#End If

        ''' <summary>
        ''' Convert UIOption to UIOptionInternal to use internally.
        ''' </summary>
        ''' <remarks>
        ''' Only accept valid UIOption values.
        ''' </remarks>
        Private Shared Function ToUIOptionInternal(ByVal showUI As UIOption) As UIOptionInternal
            Select Case showUI
                Case FileIO.UIOption.AllDialogs
                    Return UIOptionInternal.AllDialogs
                Case FileIO.UIOption.OnlyErrorDialogs
                    Return UIOptionInternal.OnlyErrorDialogs
                Case Else
                    Throw New System.ComponentModel.InvalidEnumArgumentException("showUI", showUI, GetType(UIOption))
            End Select
        End Function

        ''' <summary>
        ''' Verify that the given argument value is a valid DeleteDirectoryOption. If not, throw InvalidEnumArgumentException.
        ''' </summary>
        ''' <param name="argName">The argument name.</param>
        ''' <param name="argValue">The argument value.</param>
        ''' <remarks></remarks>
        Private Shared Sub VerifyDeleteDirectoryOption(ByVal argName As String, ByVal argValue As DeleteDirectoryOption)
            If argValue = FileIO.DeleteDirectoryOption.DeleteAllContents OrElse
                argValue = FileIO.DeleteDirectoryOption.ThrowIfDirectoryNonEmpty Then
                Exit Sub
            End If

            Throw New InvalidEnumArgumentException(argName, argValue, GetType(DeleteDirectoryOption))
        End Sub

        ''' <summary>
        ''' Verify that the given argument value is a valid RecycleOption. If not, throw InvalidEnumArgumentException.
        ''' </summary>
        ''' <param name="argName">The argument name.</param>
        ''' <param name="argValue">The argument value.</param>
        Private Shared Sub VerifyRecycleOption(ByVal argName As String, ByVal argValue As RecycleOption)
            If argValue = RecycleOption.DeletePermanently OrElse
                argValue = RecycleOption.SendToRecycleBin Then
                Exit Sub
            End If

            Throw New InvalidEnumArgumentException(argName, argValue, GetType(RecycleOption))
        End Sub

        ''' <summary>
        ''' Verify that the given argument value is a valid SearchOption. If not, throw InvalidEnumArgumentException.
        ''' </summary>
        ''' <param name="argName">The argument name.</param>
        ''' <param name="argValue">The argument value.</param>
        Private Shared Sub VerifySearchOption(ByVal argName As String, ByVal argValue As SearchOption)
            If argValue = SearchOption.SearchAllSubDirectories OrElse
                argValue = SearchOption.SearchTopLevelOnly Then
                Exit Sub
            End If

            Throw New InvalidEnumArgumentException(argName, argValue, GetType(SearchOption))
        End Sub

        ''' <summary>
        ''' Verify that the given argument value is a valid UICancelOption. If not, throw InvalidEnumArgumentException.
        ''' </summary>
        ''' <param name="argName">The argument name.</param>
        ''' <param name="argValue">The argument value.</param>
        Private Shared Sub VerifyUICancelOption(ByVal argName As String, ByVal argValue As UICancelOption)
            If argValue = UICancelOption.DoNothing OrElse
                argValue = UICancelOption.ThrowException Then
                Exit Sub
            End If

            Throw New InvalidEnumArgumentException(argName, argValue, GetType(UICancelOption))
        End Sub

#If TARGET_WINDOWS Then
        ' Base operation flags used in shell IO operation.
        ' - DON'T move connected files as a group.
        ' - DON'T confirm directory creation - our silent copy / move do not.
        Private Const m_SHELL_OPERATION_FLAGS_BASE As ShFileOperationFlags =
            ShFileOperationFlags.FOF_NO_CONNECTED_ELEMENTS Or
            ShFileOperationFlags.FOF_NOCONFIRMMKDIR

        ' Hide UI operation flags for Delete.
        ' - DON'T show progress bar.
        ' - DON'T confirm (answer yes to everything). NOTE: In exception cases (read-only file), shell still asks.
        Private Const m_SHELL_OPERATION_FLAGS_HIDE_UI As ShFileOperationFlags =
            ShFileOperationFlags.FOF_SILENT Or
            ShFileOperationFlags.FOF_NOCONFIRMATION

        ' When calling MoveFileEx, set the following flags:
        ' - Simulate CopyFile and DeleteFile if copied to a different volume.
        ' - Replace contents of existing target with the contents of source file.
        ' - Do not return until the file has actually been moved on the disk.
        Private Const m_MOVEFILEEX_FLAGS As Integer = CInt(
            MoveFileExFlags.MOVEFILE_COPY_ALLOWED Or
            MoveFileExFlags.MOVEFILE_REPLACE_EXISTING Or
            MoveFileExFlags.MOVEFILE_WRITE_THROUGH)
#End If

        ' Array containing all the path separator chars. Used to verify that input is a name, not a path.
        Private Shared ReadOnly m_SeparatorChars() As Char = {
            IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar, IO.Path.VolumeSeparatorChar}

        ''' <summary>
        ''' Private enumeration: The operation is a Copy or Move.
        ''' </summary>
        Private Enum CopyOrMove
            Copy
            Move
        End Enum

        ''' <summary>
        ''' Private enumeration: Target of the operation is a File or Directory.
        ''' </summary>
        ''' <remarks></remarks>
        Private Enum FileOrDirectory
            File
            Directory
        End Enum

        ''' <summary>
        ''' Private enumeration: Indicate the options of ShowUI to use internally.
        ''' This includes NoUI so that we can base the decision on 1 variable.
        ''' </summary>
        ''' <remarks></remarks>
        Private Enum UIOptionInternal
            OnlyErrorDialogs = UIOption.OnlyErrorDialogs
            AllDialogs = UIOption.AllDialogs
            NoUI
        End Enum

        ''' <summary>
        ''' A simple tree node to build up the directory structure used for a snapshot in Copy / Move Directory.
        ''' </summary>
        Private Class DirectoryNode
            ''' <summary>
            ''' Given a DirectoryPath, create the node and add the sub-directory nodes.
            ''' </summary>
            ''' <param name="DirectoryPath">Path to the directory. NOTE: must exist.</param>
            ''' <param name="TargetDirectoryPath">Path to the target directory of the move / copy. NOTE: must be a full path.</param>
            Friend Sub New(ByVal DirectoryPath As String, ByVal TargetDirectoryPath As String)
                Debug.Assert(IO.Directory.Exists(DirectoryPath), "Directory does not exist")
                Debug.Assert(TargetDirectoryPath <> "" And IO.Path.IsPathRooted(TargetDirectoryPath), "Invalid TargetPath")

                m_Path = DirectoryPath
                m_TargetPath = TargetDirectoryPath
                m_SubDirs = New ObjectModel.Collection(Of DirectoryNode)
                For Each SubDirPath As String In IO.Directory.GetDirectories(m_Path)
                    Dim SubTargetDirPath As String = IO.Path.Combine(m_TargetPath, IO.Path.GetFileName(SubDirPath))
                    m_SubDirs.Add(New DirectoryNode(SubDirPath, SubTargetDirPath))
                Next
            End Sub
            ''' <summary>
            ''' Return the Path of the current node.
            ''' </summary>
            ''' <value>A String containing the Path of the current node.</value>
            Friend ReadOnly Property Path() As String
                Get
                    Return m_Path
                End Get
            End Property

            ''' <summary>
            ''' Return the TargetPath for copy / move.
            ''' </summary>
            ''' <value>A String containing the copy / move target path of the current node.</value>
            Friend ReadOnly Property TargetPath() As String
                Get
                    Return m_TargetPath
                End Get
            End Property

            ''' <summary>
            ''' Return the sub directories of the current node.
            ''' </summary>
            ''' <value>A Collection(Of DirectoryNode) containing the sub-directory nodes.</value>
            Friend ReadOnly Property SubDirs() As ObjectModel.Collection(Of DirectoryNode)
                Get
                    Return m_SubDirs
                End Get
            End Property
            Private m_Path As String
            Private m_TargetPath As String
            Private m_SubDirs As ObjectModel.Collection(Of DirectoryNode)
        End Class 'Private Class DirectoryNode

        ''' <summary>
        ''' Helper class to search for text in an array of byte using a specific Decoder.
        ''' </summary>
        ''' <remarks>
        ''' To search for text that might exist in an encoding, construct this class with the text and Decoder.
        '''      Then call IsTextFound() and pass in byte arrays.
        ''' This class will take care of text spanning byte arrays by caching a part of the array and use it in
        '''      the next IsTextFound() call.
        ''' </remarks>
        Private Class TextSearchHelper
            ''' <summary>
            ''' Constructs a new helper with a given encoding and a text to search for.
            ''' </summary>
            ''' <param name="Encoding">The Encoding to use to convert byte to text.</param>
            ''' <param name="Text">The text to search for in subsequent byte array.</param>
            Friend Sub New(ByVal Encoding As Text.Encoding, ByVal Text As String, ByVal IgnoreCase As Boolean)
                Debug.Assert(Encoding IsNot Nothing, "Null Decoder")
                Debug.Assert(Text <> "", "Empty Text")

                m_Decoder = Encoding.GetDecoder
                m_Preamble = Encoding.GetPreamble
                m_IgnoreCase = IgnoreCase

                ' If use wants to ignore case, convert search text to lower case.
                If m_IgnoreCase Then
                    m_SearchText = Text.ToUpper(CultureInfo.CurrentCulture)
                Else
                    m_SearchText = Text
                End If
            End Sub

            ''' <summary>
            ''' Determines whether the text is found in the given byte array.
            ''' </summary>
            ''' <param name="ByteBuffer">The byte array to find the text in</param>
            ''' <param name="Count">The number of valid bytes in the byte array</param>
            ''' <returns>True if the text is found. Otherwise, False.</returns>
            Friend Function IsTextFound(ByVal ByteBuffer() As Byte, ByVal Count As Integer) As Boolean
                Debug.Assert(ByteBuffer IsNot Nothing, "Null ByteBuffer")
                Debug.Assert(Count > 0, Count.ToString(CultureInfo.InvariantCulture))
                Debug.Assert(m_Decoder IsNot Nothing, "Null Decoder")
                Debug.Assert(m_Preamble IsNot Nothing, "Null Preamble")

                Dim ByteBufferStartIndex As Integer = 0 ' If need to handle BOM, ByteBufferStartIndex will increase.

                ' Check for the preamble the first time IsTextFound is called. If find it, shrink ByteBuffer.
                If m_CheckPreamble Then
                    If BytesMatch(ByteBuffer, m_Preamble) Then
                        ByteBufferStartIndex = m_Preamble.Length
                        Count -= m_Preamble.Length ' Reduce the valid byte count if ByteBuffer was shrinked.
                    End If
                    m_CheckPreamble = False
                    ' In case of an empty file with BOM at the beginning return FALSE.
                    If Count <= 0 Then
                        Return False
                    End If
                End If

                ' Get the number of characters in the byte array.
                Dim ExpectedCharCount As Integer = m_Decoder.GetCharCount(ByteBuffer, ByteBufferStartIndex, Count)

                ' The character buffer used to search will be a combination of the cached buffer and the current one.
                Dim CharBuffer(m_PreviousCharBuffer.Length + ExpectedCharCount - 1) As Char
                ' Start the buffer with the cached buffer.
                Array.Copy(sourceArray:=m_PreviousCharBuffer, sourceIndex:=0,
                    destinationArray:=CharBuffer, destinationIndex:=0, length:=m_PreviousCharBuffer.Length)
                ' And fill the rest with the ones from byte array.
                Dim CharCount As Integer = m_Decoder.GetChars(
                    bytes:=ByteBuffer, byteIndex:=ByteBufferStartIndex, byteCount:=Count,
                    chars:=CharBuffer, charIndex:=m_PreviousCharBuffer.Length)
                Debug.Assert(CharCount = ExpectedCharCount, "Should read all characters")

                ' Refresh the cached buffer for the possible next search.
                If CharBuffer.Length > m_SearchText.Length Then
                    If m_PreviousCharBuffer.Length <> m_SearchText.Length Then
                        ReDim m_PreviousCharBuffer(m_SearchText.Length - 1)
                    End If
                    Array.Copy(sourceArray:=CharBuffer, sourceIndex:=(CharBuffer.Length - m_SearchText.Length),
                        destinationArray:=m_PreviousCharBuffer, destinationIndex:=0, length:=m_SearchText.Length)
                Else
                    m_PreviousCharBuffer = CharBuffer
                End If

                ' If user wants to ignore case, convert new string to lower case. m_SearchText was converted in constructor.
                If m_IgnoreCase Then
                    Return New String(CharBuffer).ToUpper(CultureInfo.CurrentCulture).Contains(m_SearchText)
                Else
                    Return New String(CharBuffer).Contains(m_SearchText)
                End If
            End Function

            ''' <summary>
            ''' No default constructor.
            ''' </summary>
            Private Sub New()
            End Sub

            ''' <summary>
            ''' Returns whether the big buffer starts with the small buffer.
            ''' </summary>
            ''' <param name="BigBuffer"></param>
            ''' <param name="SmallBuffer"></param>
            ''' <returns>True if BigBuffer starts with SmallBuffer.Otherwise, False.</returns>
            Private Shared Function BytesMatch(ByVal BigBuffer() As Byte, ByVal SmallBuffer() As Byte) As Boolean
                Debug.Assert(BigBuffer.Length > SmallBuffer.Length, "BigBuffer should be longer")
                If BigBuffer.Length < SmallBuffer.Length Or SmallBuffer.Length = 0 Then
                    Return False
                End If
                For i As Integer = 0 To SmallBuffer.Length - 1
                    If BigBuffer(i) <> SmallBuffer(i) Then
                        Return False
                    End If
                Next
                Return True
            End Function

            Private m_SearchText As String ' The text to search.
            Private m_IgnoreCase As Boolean ' Should we ignore case?
            Private m_Decoder As Text.Decoder ' The Decoder to use.
            Private m_PreviousCharBuffer() As Char = Array.Empty(Of Char)() ' The cached character array from previous call to IsTextExist.
            Private m_CheckPreamble As Boolean = True ' True to check for preamble. False otherwise.
            Private m_Preamble() As Byte ' The byte order mark we need to consider.
        End Class 'Private Class TextSearchHelper

    End Class 'Public Class FileSystem

    ''' <summary>
    ''' Specify the action to do when deleting a directory and it is not empty.
    ''' </summary>
    ''' <remarks>
    ''' Again, avoid Integer values that VB Compiler will convert Boolean to (0 and -1).
    ''' IMPORTANT: Change VerifyDeleteDirectoryOption if this enum is changed.
    ''' Also, values in DeleteDirectoryOption must be different from UIOption.
    ''' </remarks>
    Public Enum DeleteDirectoryOption As Integer
        ThrowIfDirectoryNonEmpty = 4
        DeleteAllContents = 5
    End Enum

    ''' <summary>
    ''' Specify whether to delete a file / directory to Recycle Bin or not.
    ''' </summary>
    Public Enum RecycleOption As Integer
        DeletePermanently = 2
        SendToRecycleBin = 3
    End Enum

    ''' <summary>
    ''' Specify whether to perform the search for files/directories recursively or not.
    ''' </summary>
    Public Enum SearchOption As Integer
        SearchTopLevelOnly = 2
        SearchAllSubDirectories = 3
    End Enum

    ''' <summary>
    ''' Defines option whether to throw exception when user cancels a UI operation or not.
    ''' </summary>
    Public Enum UICancelOption As Integer
        DoNothing = 2
        ThrowException = 3
    End Enum

    ''' <summary>
    ''' Specify which UI dialogs to show.
    ''' </summary>
    ''' <remarks>
    ''' To fix common issues; avoid Integer values that VB Compiler
    ''' will convert Boolean to (0 and -1).
    ''' </remarks>
    Public Enum UIOption As Integer
        OnlyErrorDialogs = 2
        AllDialogs = 3
    End Enum

End Namespace

' NOTE:
' - All path returned by us will NOT have the Directory Separator Character ('\') at the end.
' - All path accepted by us will NOT consider the meaning of Directory Separator Character ('\') at the end.
' - Parameter accepting path will accept both relative and absolute paths unless specified.
'       Relative paths will be resolved using the current working directory.
' - IO.Path.GetFullPath is used to normalized the path. It will only throw in case of not well-formed path.
' - Hidden Files and Directories will be moved / copied by Framework code.
'
' - On both Read and Write, we use the default Share mode that FX uses for the StreamReader/Writer, which is Share.Read.
'   Details on what share mode means:
'       When a call is made to open the file, the share mode not only means that the caller wants to restrict every call
'       afterwards, but also every call before as well, which means that the caller will fail if any calls before it
'       already obtained a conflict right.
'   For example: if this call succeeds,
'           Open(FileA, OpenMode.Write, ShareMode.Read)
'       Although it is sharing FileA for reading, if the 2nd call is
'           Open(FileA, OpenMode.Read, ShareMode.Read)
'       the 2nd call will fail since it wants to restrict everybody else to read only, but 1st caller has already obtained
'       write access.
'   So the default behavior is fine since novice Mort can't run into trouble using it.
'
' - All IO functions involving ShowUI have dependency on Windows Shell and sometimes have different behavior.
' - CopyDirectory will attempt to copy all the files in the directory. If there are files or sub-directories
'       that cause exception, CopyDirectory will not stop, since that will leave the result in unknown state.
'       Instead, an exception will be thrown at the end containing a list of exception files in Data property.
' - MoveDirectory behaves the same so MoveDirectory is not equal to calling CopyDirectory and DeleteDirectory.
' - Overwrite in directory case means overwrite sub files. Sub directories will always be merged.
'
' Shell behavior in exception cases:
'   - Copy / Move File
'       . Existing target:
'           Overwrite = True:   Overwrite target.
'           Overwrite = False:  Dialog  Yes:    Overwrite target.
'                                       No:     Error code 7. ERROR_ARENA_TRASHED
'       . Existing target and Read-Only (Framework will throw).
'           Always ask. No: Error code 7. ERROR_ARENA_TRASHED
'       . OS access denied: Error code 1223. ERROR_CANCELLED
'   - Copy / Move Directory Existing target:
'       . Has an existing file:
'               Overwrite = True:   Overwrite file.
'               Overwrite = False:  Dialog  Yes / Yes to all :      Overwrite target.
'                                           No:                     Leave and copy the rest.
'                                           Cancel:                 Error code 2. ERROR_FILE_NOT_FOUND.
'       . Has an existing file and Read-Only (Framework will throw).
'               Behave as when Overwrite = False.
'       . File in source same name with directory in target:
'           * Copy: Error code 1223 ERROR_CANCELLED.
'           * Move:     Overwrite = True:   Error code 183. ERROR_ALREADY_EXISTS.
'                       Overwrite = False:  Ask question    Yes:    Error code 183.
'                                                           Cancel: Error code 2.
'       . Directory in source same name with file in target:
'           Error code 183 in all cases.
'
' NOTE: Some different behavior when deleting files / directories.
' ShowUI        RecycleBin          Normal file.                Read-only file.
'   F               F                   Gone                        Exception.              *
'   T               F                   Question + UI + Gone        Question + UI + Gone
'   F               T                   Bin                         Question + Bin          *
'   T               T                   Question + UI + Bin         Question + UI + Bin
