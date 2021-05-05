' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
Option Strict On
Option Explicit On

Imports System
Imports System.Diagnostics.CodeAnalysis
Imports System.Environment
Imports System.Reflection
Imports ExUtils = Microsoft.VisualBasic.CompilerServices.ExceptionUtils

Namespace Microsoft.VisualBasic.FileIO

    ''' <summary>
    ''' This class contains properties that will return the Special Directories
    ''' specific to the current user (My Documents, My Music ...) and those specific
    ''' to the current Application that a developer expects to be able to find quickly.
    ''' </summary>
    Public Class SpecialDirectories
        ''' <summary>
        ''' Return the directory that serves as a common repository for user's personal documents.
        ''' </summary>
        ''' <value>A String containing the path to the user's personal documents.</value>
        ''' <exception cref="IO.DirectoryNotFoundException">If the system does not have the notion of My Documents directory.</exception>
        ''' <remarks>This directory is usually: C:\Documents and Settings\[UserName]\My Documents.</remarks>
        Public Shared ReadOnly Property MyDocuments() As String
            Get
                Return GetDirectoryPath(Environment.GetFolderPath(SpecialFolder.Personal), SR.IO_SpecialDirectory_MyDocuments)
            End Get
        End Property

        ''' <summary>
        ''' Return the "My Music" directory.
        ''' </summary>
        ''' <value>A String containing the path to the user's "My Music" directory.</value>
        ''' <exception cref="IO.DirectoryNotFoundException">If the system does not have the notion of My Music directory.</exception>
        ''' <remarks>This directory is C:\Documents and Settings\[UserName]\My Music</remarks>
        Public Shared ReadOnly Property MyMusic() As String
            Get
                Return GetDirectoryPath(Environment.GetFolderPath(SpecialFolder.MyMusic), SR.IO_SpecialDirectory_MyMusic)
            End Get
        End Property

        ''' <summary>
        ''' Return the "My Pictures" directory.
        ''' </summary>
        ''' <value>A String containing the path to the user's "My Pictures" directory.</value>
        ''' <exception cref="IO.DirectoryNotFoundException">If the system does not have the notion of My Pictures directory.</exception>
        ''' <remarks>This directory is C:\Documents and Settings\[UserName]\My Pictures.</remarks>
        Public Shared ReadOnly Property MyPictures() As String
            Get
                Return GetDirectoryPath(Environment.GetFolderPath(SpecialFolder.MyPictures), SR.IO_SpecialDirectory_MyPictures)
            End Get
        End Property

        ''' <summary>
        ''' Return the current user's Desktop directory.
        ''' </summary>
        ''' <value>A String containing the path to the current user's Desktop directory.</value>
        ''' <remarks>This directory is C:\Document and Settings\[UserName]\Desktop.</remarks>
        Public Shared ReadOnly Property Desktop() As String
            Get
                Return GetDirectoryPath(Environment.GetFolderPath(SpecialFolder.Desktop), SR.IO_SpecialDirectory_Desktop)
            End Get
        End Property

        ''' <summary>
        ''' Returns the directory used to store program shortcuts from Start Menu for current user.
        ''' </summary>
        ''' <value>A String containing the path to the Start Menu \ Programs directory.</value>
        ''' <remarks>This directory is C:\Document and Settings\[UserName]\Start Menu\Programs.</remarks>
        Public Shared ReadOnly Property Programs() As String
            Get
                Return GetDirectoryPath(Environment.GetFolderPath(SpecialFolder.Programs), SR.IO_SpecialDirectory_Programs)
            End Get
        End Property

        ''' <summary>
        ''' Return the program files directory.
        ''' </summary>
        ''' <value>A String containing the path to the default program directories.</value>
        ''' <remarks>This directory is C:\Program Files.</remarks>
        Public Shared ReadOnly Property ProgramFiles() As String
            Get
                Return GetDirectoryPath(Environment.GetFolderPath(SpecialFolder.ProgramFiles), SR.IO_SpecialDirectory_ProgramFiles)
            End Get
        End Property

        ''' <summary>
        ''' Return the directory that contain temporary files for the current user.
        ''' </summary>
        ''' <value>A String containing the path to the temporary directory for the current user.</value>
        ''' <remarks>
        ''' According to Win32 API document, GetTempPath should always return a value even if TEMP and TMP = "".
        ''' Also, this is not updated if TEMP or TMP is changed in Windows. The reason is
        '''     each process has its own copy of the environment variables and this copy is not updated.
        ''' </remarks>
        Public Shared ReadOnly Property Temp() As String
            Get
                Return GetDirectoryPath(IO.Path.GetTempPath(), SR.IO_SpecialDirectory_Temp)
            End Get
        End Property

        ''' <summary>
        ''' Returns the directory that serves as a common repository for data files
        ''' from your application used only by the current user.
        ''' </summary>
        ''' <value>A String containing the path to the directory your application can use to store data for the current user.</value>
        ''' <remarks>
        ''' If a path does not exist, one is created in the following format
        ''' C:\Documents and Settings\[UserName]\Application Data\[CompanyName]\[ProductName]\[ProductVersion]
        '''
        ''' We choose to use System.Windows.Forms.Application.* instead of System.Environment.GetFolderPath(*)
        ''' since the second function will only return the C:\Documents and Settings\[UserName]\Application Data.\
        ''' The first function separates applications by CompanyName, ProductName, ProductVersion.
        ''' The only catch is that CompanyName, ProductName has to be specified in the AssemblyInfo.vb file,
        ''' otherwise the name of the assembly will be used instead (which still has a level of separation).
        '''
        ''' Also, we chose to use UserAppDataPath instead of LocalUserAppDataPath since this directory
        ''' will work with Roaming User as well.
        ''' </remarks>
        Public Shared ReadOnly Property CurrentUserApplicationData() As String
            <UnconditionalSuppressMessage("ReflectionAnalsys", "IL2026:RequiresUnreferencedCode",
                Justification:="Trimmer warns because it can't see System.Windows.Forms.Application. If the assembly is there, the trimmer will be able to tell to preserve the method specified.")>
            Get
                Return GetDirectoryPath(GetWindowsFormsDirectory("System.Windows.Forms.Application", "UserAppDataPath"), SR.IO_SpecialDirectory_UserAppData)
            End Get
        End Property

        ''' <summary>
        ''' Returns the directory that serves as a common repository for data files
        ''' from your application used by all users.
        ''' </summary>
        ''' <value>A String containing the path to the directory your application can use to store data for all users.</value>
        ''' <remarks>
        ''' If a path does not exist, one is created in the following format
        ''' C:\Documents and Settings\All Users\Application Data\[CompanyName]\[ProductName]\[ProductVersion]
        '''
        ''' See above for reason why we don't use System.Environment.GetFolderPath(*).
        ''' </remarks>
        Public Shared ReadOnly Property AllUsersApplicationData() As String
            <UnconditionalSuppressMessage("ReflectionAnalsys", "IL2026:RequiresUnreferencedCode",
                Justification:="Trimmer warns because it can't see System.Windows.Forms.Application. If the assembly is there, the trimmer will be able to tell to preserve the method specified.")>
            Get
                Return GetDirectoryPath(GetWindowsFormsDirectory("System.Windows.Forms.Application", "CommonAppDataPath"), SR.IO_SpecialDirectory_AllUserAppData)
            End Get
        End Property

        ''' <summary>
        ''' Return a normalized from a directory path and throw exception if directory path is "".
        ''' </summary>
        ''' <param name="Directory">The special directory's path got back from FX. "" if it does not exist.</param>
        ''' <param name="DirectoryNameResID">The resource ID of the special directory's localized name.</param>
        ''' <returns>A String containing the path to the special directory if success.</returns>
        Private Shared Function GetDirectoryPath(ByVal Directory As String, ByVal DirectoryNameResID As String) As String
            ' Only need to worry about Directory being "" since it comes from Framework.
            If Directory = "" Then
                Throw ExUtils.GetDirectoryNotFoundException(SR.IO_SpecialDirectoryNotExist, DirectoryNameResID)
            End If
            Return FileSystem.NormalizePath(Directory)
        End Function

        <RequiresUnreferencedCode("Cannot statically analyze the passed in type.")>
        Private Shared Function GetWindowsFormsDirectory(typeName As String, propertyName As String) As String
            Dim type As Type = Type.GetType($"{typeName}, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError:=False)
            Dim [property] As PropertyInfo = type?.GetProperty(propertyName)
            If [property] Is Nothing Then
                Return ""
            End If
            Return DirectCast([property].GetValue(Nothing, BindingFlags.DoNotWrapExceptions, Nothing, Nothing, Nothing), String)
        End Function

    End Class
End Namespace
