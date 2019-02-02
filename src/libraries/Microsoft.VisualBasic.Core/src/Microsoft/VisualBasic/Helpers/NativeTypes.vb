' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports System
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.Win32.SafeHandles

Namespace Microsoft.VisualBasic.CompilerServices

    <ComponentModel.EditorBrowsableAttribute(ComponentModel.EditorBrowsableState.Never)>
    Friend NotInheritable Class NativeTypes

        <StructLayout(LayoutKind.Sequential)>
        Friend NotInheritable Class SECURITY_ATTRIBUTES
            Implements IDisposable

            Friend Sub New()
                nLength = System.Runtime.InteropServices.Marshal.SizeOf(GetType(SECURITY_ATTRIBUTES))
            End Sub

            Public nLength As Integer
            Public lpSecurityDescriptor As IntPtr
            Public bInheritHandle As Boolean

            Public Overloads Sub Dispose() Implements IDisposable.Dispose
                If lpSecurityDescriptor <> IntPtr.Zero Then
                    UnsafeNativeMethods.LocalFree(lpSecurityDescriptor)
                    lpSecurityDescriptor = IntPtr.Zero
                End If
                GC.SuppressFinalize(Me)
            End Sub

            Protected Overrides Sub Finalize()
                Dispose()
                MyBase.Finalize()
            End Sub
        End Class

        ''' <summary>
        ''' Inherits SafeHandleZeroOrMinusOneIsInvalid, with additional InitialSetHandle method.
        ''' This is required because call to constructor of SafeHandle is not allowed in constrained region.
        ''' </summary>
        ''' <remarks>VSWhidbey 544308</remarks>
        Friend NotInheritable Class LateInitSafeHandleZeroOrMinusOneIsInvalid
            Inherits SafeHandleZeroOrMinusOneIsInvalid

            Friend Sub New()
                MyBase.New(True)
            End Sub

            Friend Sub InitialSetHandle(ByVal h As IntPtr)
                Debug.Assert(MyBase.IsInvalid, "Safe handle should only be set once.")
                MyBase.SetHandle(h)
            End Sub

            Protected Overrides Function ReleaseHandle() As Boolean
                Return NativeMethods.CloseHandle(Me.handle) <> 0
            End Function
        End Class

        ''' <summary>
        ''' Represent Win32 PROCESS_INFORMATION structure. IMPORTANT: Copy the handles to a SafeHandle before use them.
        ''' </summary>
        ''' <remarks>
        ''' The handles in PROCESS_INFORMATION are initialized in unmanaged function.
        ''' We can't use SafeHandle here because Interop doesn't support [out] SafeHandles in structure / classes yet.
        ''' This class makes no attempt to free the handles. To use the handle, first copy it to a SafeHandle class
        ''' (using LateInitSafeHandleZeroOrMinusOneIsInvalid.InitialSetHandle) to correctly use and dispose the handle.
        ''' </remarks>
        <StructLayout(LayoutKind.Sequential)>
        Friend NotInheritable Class PROCESS_INFORMATION
            Public hProcess As IntPtr = IntPtr.Zero
            Public hThread As IntPtr = IntPtr.Zero
            Public dwProcessId As Integer
            Public dwThreadId As Integer

            Friend Sub New()
            End Sub
        End Class

        ''' <summary>
        ''' Important!  This class should be used where the API being called has allocated the strings.  That is why lpReserved, etc. are declared as IntPtrs instead
        ''' of Strings - so that the marshaling layer won't release the memory.  This caused us problems in the shell() functions.  We would call GetStartupInfo()
        ''' which doesn't expect the memory for the strings to be freed.  But because the strings were previously defined as type String, the marshaller would
        ''' and we got memory corruption problems detectable while running AppVerifier.
        ''' If you use this structure with an API like CreateProcess() then you are supplying the strings so you'll need another version of this class that defines lpReserved, etc.
        ''' as String so that the memory will get cleaned up.
        ''' </summary>
        ''' <remarks></remarks>
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Friend NotInheritable Class STARTUPINFO
            Implements IDisposable

            Public cb As Integer
            Public lpReserved As IntPtr = IntPtr.Zero 'not string - see summary
            Public lpDesktop As IntPtr = IntPtr.Zero 'not string - see summary
            Public lpTitle As IntPtr = IntPtr.Zero 'not string - see summary
            Public dwX As Integer
            Public dwY As Integer
            Public dwXSize As Integer
            Public dwYSize As Integer
            Public dwXCountChars As Integer
            Public dwYCountChars As Integer
            Public dwFillAttribute As Integer
            Public dwFlags As Integer
            Public wShowWindow As Short
            Public cbReserved2 As Short
            Public lpReserved2 As IntPtr = IntPtr.Zero
            Public hStdInput As IntPtr = IntPtr.Zero
            Public hStdOutput As IntPtr = IntPtr.Zero
            Public hStdError As IntPtr = IntPtr.Zero

            Friend Sub New()
            End Sub

            Private m_HasBeenDisposed As Boolean ' To detect redundant calls. Default initialize = False.

            Protected Overrides Sub Finalize()
                Dispose(False)
            End Sub

            ' IDisposable
            Private Sub Dispose(ByVal disposing As Boolean)
                If Not m_HasBeenDisposed Then
                    If disposing Then
                        m_HasBeenDisposed = True

                        Const STARTF_USESTDHANDLES As Integer = 256 'Defined in windows.h
                        If (Me.dwFlags And STARTF_USESTDHANDLES) <> 0 Then
                            If hStdInput <> IntPtr.Zero AndAlso hStdInput <> NativeTypes.INVALID_HANDLE Then
                                NativeMethods.CloseHandle(hStdInput)
                                hStdInput = NativeTypes.INVALID_HANDLE
                            End If

                            If hStdOutput <> IntPtr.Zero AndAlso hStdOutput <> NativeTypes.INVALID_HANDLE Then
                                NativeMethods.CloseHandle(hStdOutput)
                                hStdOutput = NativeTypes.INVALID_HANDLE
                            End If

                            If hStdError <> IntPtr.Zero AndAlso hStdError <> NativeTypes.INVALID_HANDLE Then
                                NativeMethods.CloseHandle(hStdError)
                                hStdError = NativeTypes.INVALID_HANDLE
                            End If
                        End If 'Me.dwFlags and STARTF_USESTDHANDLES

                    End If
                End If
            End Sub

            ' This code correctly implements the disposable pattern.
            Friend Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
        End Class

        <StructLayout(LayoutKind.Sequential)> Friend NotInheritable Class SystemTime
            Public wYear As Short
            Public wMonth As Short
            Public wDayOfWeek As Short
            Public wDay As Short
            Public wHour As Short
            Public wMinute As Short
            Public wSecond As Short
            Public wMilliseconds As Short

            Friend Sub New()
            End Sub
        End Class

        ''' <summary>
        ''' Flags for MoveFileEx.
        ''' See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/fileio/fs/movefileex.asp
        ''' and public\sdk\inc\winbase.h.
        ''' </summary>
        <Flags()>
        Friend Enum MoveFileExFlags As Integer
            MOVEFILE_REPLACE_EXISTING = &H1
            MOVEFILE_COPY_ALLOWED = &H2
            MOVEFILE_DELAY_UNTIL_REBOOT = &H4
            MOVEFILE_WRITE_THROUGH = &H8
        End Enum

        ' Handle Values
        Friend Shared ReadOnly INVALID_HANDLE As IntPtr = New IntPtr(-1)

        ' GetWindow() Constants
        Friend Const GW_HWNDFIRST As Integer = 0
        Friend Const GW_HWNDLAST As Integer = 1
        Friend Const GW_HWNDNEXT As Integer = 2
        Friend Const GW_HWNDPREV As Integer = 3
        Friend Const GW_OWNER As Integer = 4
        Friend Const GW_CHILD As Integer = 5
        Friend Const GW_MAX As Integer = 5

        'Friend Const EVENTLOG_INFORMATION_TYPE As Integer = 0

        Friend Const STARTF_USESHOWWINDOW As Integer = 1

        Friend Const NORMAL_PRIORITY_CLASS As Integer = &H20

        Friend Const LCMAP_TRADITIONAL_CHINESE As Integer = &H4000000I
        Friend Const LCMAP_SIMPLIFIED_CHINESE As Integer = &H2000000I
        Friend Const LCMAP_UPPERCASE As Integer = &H200I
        Friend Const LCMAP_LOWERCASE As Integer = &H100I
        Friend Const LCMAP_FULLWIDTH As Integer = &H800000I
        Friend Const LCMAP_HALFWIDTH As Integer = &H400000I
        Friend Const LCMAP_KATAKANA As Integer = &H200000I
        Friend Const LCMAP_HIRAGANA As Integer = &H100000I

        ' Error code from public\sdk\inc\winerror.h
        Friend Const ERROR_FILE_NOT_FOUND As Integer = 2
        Friend Const ERROR_PATH_NOT_FOUND As Integer = 3
        Friend Const ERROR_ACCESS_DENIED As Integer = 5
        Friend Const ERROR_ALREADY_EXISTS As Integer = 183
        Friend Const ERROR_FILENAME_EXCED_RANGE As Integer = 206
        Friend Const ERROR_INVALID_DRIVE As Integer = 15
        Friend Const ERROR_INVALID_PARAMETER As Integer = 87
        Friend Const ERROR_SHARING_VIOLATION As Integer = 32
        Friend Const ERROR_FILE_EXISTS As Integer = 80
        Friend Const ERROR_OPERATION_ABORTED As Integer = 995
        Friend Const ERROR_CANCELLED As Integer = 1223

        ''' ;New
        ''' <summary>
        ''' FxCop violation: Avoid uninstantiated internal class.
        ''' Adding a private constructor to prevent the compiler from generating a default constructor.
        ''' </summary>
        Private Sub New()
        End Sub
    End Class

End Namespace
