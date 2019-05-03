' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualBasic.CompilerServices

    <ComVisible(False)>
    Friend NotInheritable Class UnsafeNativeMethods

        <PreserveSig()>
        Friend Declare Ansi Function LCMapStringA _
                Lib "kernel32" Alias "LCMapStringA" (ByVal Locale As Integer, ByVal dwMapFlags As Integer,
                    <MarshalAs(UnmanagedType.LPArray)> ByVal lpSrcStr As Byte(), ByVal cchSrc As Integer, <MarshalAs(UnmanagedType.LPArray)> ByVal lpDestStr As Byte(), ByVal cchDest As Integer) As Integer

        <PreserveSig()>
        Friend Declare Auto Function LCMapString _
                Lib "kernel32" (ByVal Locale As Integer, ByVal dwMapFlags As Integer,
                    ByVal lpSrcStr As String, ByVal cchSrc As Integer, ByVal lpDestStr As String, ByVal cchDest As Integer) As Integer

        <DllImport("oleaut32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="VarParseNumFromStr")>
        Friend Shared Function VarParseNumFromStr(
                <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal str As String,
                ByVal lcid As Integer,
                ByVal dwFlags As Integer,
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(),
                <MarshalAs(UnmanagedType.LPArray)> ByVal digits As Byte()) As Integer
        End Function

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VarNumFromParseNum")>
        Friend Shared Function VarNumFromParseNum(
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(),
                <MarshalAs(UnmanagedType.LPArray)> ByVal DigitArray As Byte(),
                ByVal dwVtBits As Int32) As Object
        End Function

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VariantChangeType")>
        Friend Shared Sub VariantChangeType(
            <Out()> ByRef dest As Object,
            <[In]()> ByRef Src As Object,
            ByVal wFlags As Int16,
            ByVal vt As Int16)
        End Sub

        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="SetLocalTime", SetLastError:=True)>
        Friend Shared Function SetLocalTime(ByVal systime As NativeTypes.SystemTime) As Integer
        End Function

        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Auto, EntryPoint:="MoveFile", BestFitMapping:=False, ThrowOnUnmappableChar:=True, SetLastError:=True)>
        Friend Shared Function MoveFile(<[In](), MarshalAs(UnmanagedType.LPTStr)> ByVal lpExistingFileName As String,
                <[In](), MarshalAs(UnmanagedType.LPTStr)> ByVal lpNewFileName As String) As Integer
        End Function

        <DllImport("kernel32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="GetLogicalDrives")>
        Friend Shared Function GetLogicalDrives() As Integer
        End Function

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
