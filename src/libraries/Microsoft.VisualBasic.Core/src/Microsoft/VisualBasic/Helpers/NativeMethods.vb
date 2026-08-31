' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
Option Strict On
Option Explicit On

Imports System
Imports System.Runtime.InteropServices
Imports System.Text

Namespace Microsoft.VisualBasic.CompilerServices

    <ComVisible(False)>
    Friend NotInheritable Class NativeMethods
        <PreserveSig()>
        Friend Declare Function _
            CloseHandle _
                Lib "kernel32" (ByVal hObject As IntPtr) As Integer

#Disable Warning CA1838 ' Avoid 'StringBuilder' parameters for P/Invokes
        <DllImport(
             "kernel32",
             CharSet:=CharSet.Auto,
             PreserveSig:=True,
             BestFitMapping:=False,
             ThrowOnUnmappableChar:=True)>
        Friend Shared Function GetVolumeInformation(
             <MarshalAs(UnmanagedType.LPTStr)> ByVal lpRootPathName As String,
             ByVal lpVolumeNameBuffer As StringBuilder,
             ByVal nVolumeNameSize As Integer,
             ByRef lpVolumeSerialNumber As Integer,
             ByRef lpMaximumComponentLength As Integer,
             ByRef lpFileSystemFlags As Integer,
             ByVal lpFileSystemNameBuffer As IntPtr,
             ByVal nFileSystemNameSize As Integer) As Integer
        End Function
#Enable Warning CA1838

        ''' <summary>
        ''' Given a 32-bit SHFILEOPSTRUCT, call the appropriate SHFileOperation function
        ''' to perform shell file operation.
        ''' </summary>
        ''' <param name="lpFileOp">32-bit SHFILEOPSTRUCT</param>
        ''' <returns>0 if successful, non-zero otherwise.</returns>
        Friend Shared Function SHFileOperation(ByRef lpFileOp As SHFILEOPSTRUCT) As Int32
            If (IntPtr.Size = 4) Then ' 32-bit platforms
                Return SHFileOperation32(lpFileOp)
            Else ' 64-bit platforms

                ' Create a new SHFILEOPSTRUCT64. The only difference is the packing, so copy all fields.
                Dim lpFileOp64 As New SHFILEOPSTRUCT64
                lpFileOp64.hwnd = lpFileOp.hwnd
                lpFileOp64.wFunc = lpFileOp.wFunc
                lpFileOp64.pFrom = lpFileOp.pFrom
                lpFileOp64.pTo = lpFileOp.pTo
                lpFileOp64.fFlags = lpFileOp.fFlags
                lpFileOp64.fAnyOperationsAborted = lpFileOp.fAnyOperationsAborted
                lpFileOp64.hNameMappings = lpFileOp.hNameMappings
                lpFileOp64.lpszProgressTitle = lpFileOp.lpszProgressTitle

                ' P/Invoke SHFileOperation with the 64 bit structure.
                Dim result As Int32 = SHFileOperation64(lpFileOp64)

                ' Only need to check if any operations were aborted.
                lpFileOp.fAnyOperationsAborted = lpFileOp64.fAnyOperationsAborted

                Return result
            End If
        End Function

        ''' <summary>
        ''' Copies, moves, renames or deletes a file system object on 32-bit platforms.
        ''' </summary>
        ''' <param name="lpFileOp">Pointer to an SHFILEOPSTRUCT structure that contains information this function needs
        '''       to carry out the specified operation. This parameter must contain a valid value that is not NULL.
        '''       You are responsible for validating the value. If you do not, you will experience unexpected result.</param>
        ''' <returns>Returns zero if successful, non zero otherwise.</returns>
        ''' <remarks>
        ''' You should use fully-qualified path names with this function. Using it with relative path names is not thread safe.
        ''' You cannot use SHFileOperation to move special folders My Documents and My Pictures from a local drive to a remote computer.
        ''' File deletion is recursive unless you set the FOF_NORECURSION flag.
        ''' </remarks>
        <DllImport("shell32.dll", CharSet:=CharSet.Auto, EntryPoint:="SHFileOperation", SetLastError:=True, ThrowOnUnmappableChar:=True)>
        Private Shared Function SHFileOperation32(ByRef lpFileOp As SHFILEOPSTRUCT) As Int32
        End Function

        ''' <summary>
        ''' Contains information that the SHFileOperation function uses to perform file operations on 32-bit platforms.
        ''' </summary>
        ''' <remarks>
        ''' * For detail documentation: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/shellcc/platform/shell/reference/structures/shfileopstruct.asp.
        ''' Members:
        '''   hwnd: Window handle to the dialog box to display information about the status of the operation.
        '''   wFunc: Value indicates which operation (copy, move, rename, delete) to perform.
        '''   pFrom: Buffer for 1 or more source file names. Each name ends with a NULL separator + additional NULL at the end.
        '''   pTo: Buffer for destination name(s). Same rule as pFrom.
        '''   fFlags: Flags that control details of the operation.
        '''   fAnyOperationsAborted: Out param. TRUE if user aborted any file operations. Otherwise, FALSE.
        '''   hNameMappings: Handle to name mapping object containing old and new names of renamed files (not used).
        '''   lpszProgressTitle: Address of a string to use as title of progress dialog box. (not used).
        ''' typedef struct _SHFILEOPSTRUCT {
        '''    HWND hwnd;
        '''    UINT wFunc;
        '''    LPCTSTR pFrom;
        '''    LPCTSTR pTo;
        '''    FILEOP_FLAGS fFlags; (WORD)
        '''    BOOL fAnyOperationsAborted;
        '''    LPVOID hNameMappings;
        '''    LPCTSTR lpszProgressTitle;
        ''' } SHFILEOPSTRUCT, *LPSHFILEOPSTRUCT;
        '''   If no steps are taken, the last 3 variables will not be passed correctly. Hence the Pack:=1.
        ''' </remarks>
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Auto)>
        Friend Structure SHFILEOPSTRUCT
            Friend hwnd As IntPtr
            Friend wFunc As UInt32
            <MarshalAs(UnmanagedType.LPTStr)> Friend pFrom As String
            <MarshalAs(UnmanagedType.LPTStr)> Friend pTo As String
            Friend fFlags As UInt16
            Friend fAnyOperationsAborted As Boolean
            Friend hNameMappings As IntPtr
            <MarshalAs(UnmanagedType.LPTStr)> Friend lpszProgressTitle As String
        End Structure

        ''' <summary>
        ''' Copies, moves, renames or deletes a file system object on 64-bit platforms.
        ''' </summary>
        <DllImport("shell32.dll", CharSet:=CharSet.Auto, EntryPoint:="SHFileOperation", SetLastError:=True, ThrowOnUnmappableChar:=True)>
        Private Shared Function SHFileOperation64(ByRef lpFileOp As SHFILEOPSTRUCT64) As Int32
        End Function

        ''' <summary>
        ''' Contains information that the SHFileOperation function uses to perform file operations
        ''' on 64-bit platforms, where the structure is unpacked.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Private Structure SHFILEOPSTRUCT64
            Friend hwnd As IntPtr
            Friend wFunc As UInt32
            <MarshalAs(UnmanagedType.LPTStr)> Friend pFrom As String
            <MarshalAs(UnmanagedType.LPTStr)> Friend pTo As String
            Friend fFlags As UInt16
            Friend fAnyOperationsAborted As Boolean
            Friend hNameMappings As IntPtr
            <MarshalAs(UnmanagedType.LPTStr)> Friend lpszProgressTitle As String
        End Structure

        ''' <summary>
        ''' Values that indicate which file operation to perform. Used in SHFILEOPSTRUCT
        ''' </summary>
        Friend Enum SHFileOperationType As UInt32
            FO_MOVE = &H1
            FO_COPY = &H2
            FO_DELETE = &H3
            FO_RENAME = &H4
        End Enum

        ''' <summary>
        ''' Flags that control the file operation. Used in SHFILEOPSTRUCT.
        ''' </summary>
        <Flags>
        Friend Enum ShFileOperationFlags As UInt16
            ' The pTo member specifies multiple destination files (one for each source file)
            ' rather than one directory where all source files are to be deposited.
            FOF_MULTIDESTFILES = &H1
            ' Not currently used.
            FOF_CONFIRMMOUSE = &H2
            ' Do not display a progress dialog box.
            FOF_SILENT = &H4
            ' Give the file being operated on a new name in a move, copy, or rename operation
            ' if a file with the target name already exists.
            FOF_RENAMEONCOLLISION = &H8
            ' Respond with "Yes to All" for any dialog box that is displayed.
            FOF_NOCONFIRMATION = &H10
            ' If FOF_RENAMEONCOLLISION is specified and any files were renamed,
            ' assign a name mapping object containing their old and new names to the hNameMappings member.
            FOF_WANTMAPPINGHANDLE = &H20
            ' Preserve Undo information, if possible. Undone can only be done from the same process.
            ' If pFrom does not contain fully qualified path and file names, this flag is ignored.
            ' NOTE: Not setting this flag will let the file be deleted permanently, unlike the doc says.
            FOF_ALLOWUNDO = &H40
            ' Perform the operation on files only if a wildcard file name (*.*) is specified.
            FOF_FILESONLY = &H80
            ' Display a progress dialog box but do not show the file names.
            FOF_SIMPLEPROGRESS = &H100
            ' Do not confirm the creation of a new directory if the operation requires one to be created.
            FOF_NOCONFIRMMKDIR = &H200
            ' Do not display a user interface if an error occurs.
            FOF_NOERRORUI = &H400
            ' Do not copy the security attributes of the file.
            FOF_NOCOPYSECURITYATTRIBS = &H800
            ' Only operate in the local directory. Don't operate recursively into subdirectories.
            FOF_NORECURSION = &H1000
            ' Do not move connected files as a group. Only move the specified files.
            FOF_NO_CONNECTED_ELEMENTS = &H2000
            ' Send a warning if a file is being destroyed during a delete operation rather than recycled.
            ' This flag partially overrides FOF_NOCONFIRMATION.
            FOF_WANTNUKEWARNING = &H4000
            ' Treat reparse points as objects, not containers.
            FOF_NORECURSEREPARSE = &H8000
        End Enum

        ''' <summary>
        ''' Notifies the system of an event that an application has performed.
        ''' An application should use this function if it performs an action that may affect the shell.
        ''' </summary>
        ''' <param name="wEventId">Describes the event that has occurred. Typically, only one event is specified at a time.
        '''       If more than one event is specified, the values contained in dwItem1 and dwItem2 must be the same,
        '''       respectively, for all specified events. See ShellChangeNotificationEvents.</param>
        ''' <param name="uFlags">Flags that indicate the meaning of the dwItem1 and dwItem2 parameter. See ShellChangeNotificationFlags.</param>
        ''' <param name="dwItem1">First event-dependent value.</param>
        ''' <param name="dwItem2">Second event-dependent value.</param>
        <DllImport("shell32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Friend Shared Sub SHChangeNotify(ByVal wEventId As UInt32, ByVal uFlags As UInt32,
                ByVal dwItem1 As IntPtr, ByVal dwItem2 As IntPtr)
        End Sub

        ''' <summary>
        ''' Describes the event that has occurred. Used in SHChangeNotify.
        ''' There are more values in shellapi.h. Only include the relevant ones.
        ''' </summary>
        Friend Enum SHChangeEventTypes As UInt32
            ' Specifies a combination of all of the disk event identifiers.
            SHCNE_DISKEVENTS = &H2381F
            ' All events have occurred.
            SHCNE_ALLEVENTS = &H7FFFFFFF
        End Enum

        ''' <summary>
        ''' Indicates the meaning of dwItem1 and dwItem2 parameters in SHChangeNotify method.
        ''' There are more values in shellapi.h. Only include the relevant one.
        ''' </summary>
        Friend Enum SHChangeEventParameterFlags As UInt32
            ' The dwItem1 and dwItem2 parameters are DWORD values.
            SHCNF_DWORD = &H3
        End Enum

        ''' <summary>
        ''' The MoveFileEx function moves an existing file or directory.
        ''' http://msdn.microsoft.com/library/default.asp?url=/library/en-us/fileio/fs/movefileex.asp
        ''' </summary>
        <DllImport("kernel32",
             PreserveSig:=True,
             CharSet:=CharSet.Auto,
             EntryPoint:="MoveFileEx",
             BestFitMapping:=False,
             ThrowOnUnmappableChar:=True,
             SetLastError:=True)>
        Friend Shared Function MoveFileEx(
             ByVal lpExistingFileName As String,
             ByVal lpNewFileName As String,
             ByVal dwFlags As Integer) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        ''' <summary>
        ''' FxCop violation: Avoid uninstantiated internal class.
        ''' Adding a private constructor to prevent the compiler from generating a default constructor.
        ''' </summary>
        Private Sub New()
        End Sub

    End Class

End Namespace
