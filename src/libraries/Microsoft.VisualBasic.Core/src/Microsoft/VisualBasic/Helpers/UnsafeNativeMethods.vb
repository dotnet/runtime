' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Runtime.InteropServices
Imports System.Runtime.Versioning

Namespace Microsoft.VisualBasic.CompilerServices

    <ComVisible(False)>
    Friend NotInheritable Class UnsafeNativeMethods

        <ResourceExposure(ResourceScope.Machine)>
        <PreserveSig()>
        Friend Declare Ansi Function LCMapStringA _
                Lib "kernel32" Alias "LCMapStringA" (ByVal Locale As Integer, ByVal dwMapFlags As Integer,
                    <MarshalAs(UnmanagedType.LPArray)> ByVal lpSrcStr As Byte(), ByVal cchSrc As Integer, <MarshalAs(UnmanagedType.LPArray)> ByVal lpDestStr As Byte(), ByVal cchDest As Integer) As Integer

        <ResourceExposure(ResourceScope.Machine)>
        <PreserveSig()>
        Friend Declare Auto Function LCMapString _
                Lib "kernel32" (ByVal Locale As Integer, ByVal dwMapFlags As Integer,
                    ByVal lpSrcStr As String, ByVal cchSrc As Integer, ByVal lpDestStr As String, ByVal cchDest As Integer) As Integer

        ''' <summary>
        ''' Frees memory allocated from the local heap. i.e. frees memory allocated
        ''' by LocalAlloc or LocalReAlloc.n
        ''' </summary>
        ''' <param name="LocalHandle"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <DllImport("kernel32", ExactSpelling:=True, SetLastError:=True)>
        Friend Shared Function LocalFree(ByVal LocalHandle As IntPtr) As IntPtr
        End Function
    End Class
End Namespace
