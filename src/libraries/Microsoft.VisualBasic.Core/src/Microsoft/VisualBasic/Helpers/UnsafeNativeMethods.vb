' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Diagnostics.CodeAnalysis
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
        <RequiresUnreferencedCode("Marshalling COM Objects is not trim safe.")>
        <UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:COMMarshalling",
            Justification:="RequiresUnreferencedCode attribute currently doesn't suppress IL2050. This should be removed once it does. https://github.com/mono/linker/issues/1989")>
        Friend Shared Function VarNumFromParseNum(
                <MarshalAs(UnmanagedType.LPArray)> ByVal numprsPtr As Byte(),
                <MarshalAs(UnmanagedType.LPArray)> ByVal DigitArray As Byte(),
                ByVal dwVtBits As Int32) As Object
        End Function

        <DllImport("oleaut32", PreserveSig:=False, CharSet:=CharSet.Unicode, EntryPoint:="VariantChangeType")>
        <RequiresUnreferencedCode("Marshalling COM Objects is not trim safe.")>
        <UnconditionalSuppressMessage("ReflectionAnalysis", "IL2050:COMMarshalling",
            Justification:="RequiresUnreferencedCode attribute currently doesn't suppress IL2050. This should be removed once it does. https://github.com/mono/linker/issues/1989")>
        Friend Shared Sub VariantChangeType(
            <Out()> ByRef dest As Object,
            <[In]()> ByRef Src As Object,
            ByVal wFlags As Int16,
            ByVal vt As Int16)
        End Sub

        <DllImport("user32", PreserveSig:=True, CharSet:=CharSet.Unicode, EntryPoint:="MessageBeep")>
        Friend Shared Function MessageBeep(ByVal uType As Integer) As Integer
        End Function

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

        Public Const LCID_US_ENGLISH As Integer = &H409

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Public Enum tagSYSKIND
            SYS_WIN16 = 0
            SYS_MAC = 2
        End Enum

        ' REVIEW :  - c# version was class, does it make a difference?
        '    [StructLayout(LayoutKind.Sequential)]
        '    Public class  tagTLIBATTR {
        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Public Structure tagTLIBATTR
            Public guid As Guid
            Public lcid As Integer
            Public syskind As tagSYSKIND
            <MarshalAs(UnmanagedType.U2)> Public wMajorVerNum As Short
            <MarshalAs(UnmanagedType.U2)> Public wMinorVerNum As Short
            <MarshalAs(UnmanagedType.U2)> Public wLibFlags As Short
        End Structure

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),
         ComImport(),
         Guid("00020403-0000-0000-C000-000000000046"),
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)>
        Public Interface ITypeComp

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub RemoteBind(
                   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szName As String,
                   <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer,
                   <[In](), MarshalAs(UnmanagedType.U2)> ByVal wFlags As Short,
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo(),
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDescKind As ComTypes.DESCKIND(),
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppFuncDesc As ComTypes.FUNCDESC(),
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppVarDesc As ComTypes.VARDESC(),
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTypeComp As ITypeComp(),
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDummy As Integer())

            Sub RemoteBindType(
                   <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szName As String,
                   <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer,
                   <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())
        End Interface

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),
         ComImport(),
         Guid("00020400-0000-0000-C000-000000000046"),
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)>
        Public Interface IDispatch

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function GetTypeInfoCount() As Integer

            <PreserveSig()>
            Function GetTypeInfo(
                    <[In]()> ByVal index As Integer,
                    <[In]()> ByVal lcid As Integer,
                    <[Out](), MarshalAs(UnmanagedType.Interface)> ByRef pTypeInfo As ITypeInfo) As Integer

            ' WARNING :  - This api NOT COMPLETELY DEFINED, DO NOT CALL!
            <PreserveSig()>
            Function GetIDsOfNames() As Integer

            ' WARNING :  - This api NOT COMPLETELY DEFINED, DO NOT CALL!
            <PreserveSig()>
            Function Invoke() As Integer
        End Interface

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),
         ComImport(),
         Guid("00020401-0000-0000-C000-000000000046"),
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)>
        Public Interface ITypeInfo
            <PreserveSig()>
            Function GetTypeAttr(
                    <Out()> ByRef pTypeAttr As IntPtr) As Integer

            <PreserveSig()>
            Function GetTypeComp(
                    <Out()> ByRef pTComp As ITypeComp) As Integer


            <PreserveSig()>
            Function GetFuncDesc(
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer,
                    <Out()> ByRef pFuncDesc As IntPtr) As Integer

            <PreserveSig()>
            Function GetVarDesc(
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer,
                    <Out()> ByRef pVarDesc As IntPtr) As Integer

            <PreserveSig()>
            Function GetNames(
                    <[In]()> ByVal memid As Integer,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal rgBstrNames As String(),
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal cMaxNames As Integer,
                    <Out(), MarshalAs(UnmanagedType.U4)> ByRef cNames As Integer) As Integer

            <Obsolete("Bad signature, second param type should be Byref. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function GetRefTypeOfImplType(
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer,
                    <Out()> ByRef pRefType As Integer) As Integer

            <Obsolete("Bad signature, second param type should be Byref. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function GetImplTypeFlags(
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer,
                    <Out()> ByVal pImplTypeFlags As Integer) As Integer

            <PreserveSig()>
            Function GetIDsOfNames(
                    <[In]()> ByVal rgszNames As IntPtr,
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal cNames As Integer,
                    <Out()> ByRef pMemId As IntPtr) As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function Invoke() As Integer

            <PreserveSig()>
            Function GetDocumentation(
                     <[In]()> ByVal memid As Integer,
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrName As String,
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrDocString As String,
                     <Out(), MarshalAs(UnmanagedType.U4)> ByRef pdwHelpContext As Integer,
                     <Out(), MarshalAs(UnmanagedType.BStr)> ByRef pBstrHelpFile As String) As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function GetDllEntry(
                    <[In]()> ByVal memid As Integer,
                    <[In]()> ByVal invkind As ComTypes.INVOKEKIND,
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrDllName As String,
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrName As String,
                    <Out(), MarshalAs(UnmanagedType.U2)> ByVal pwOrdinal As Short) As Integer

            <PreserveSig()>
            Function GetRefTypeInfo(
                     <[In]()> ByVal hreftype As IntPtr,
                     <Out()> ByRef pTypeInfo As ITypeInfo) As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function AddressOfMember() As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function CreateInstance(
                    <[In]()> ByRef pUnkOuter As IntPtr,
                    <[In]()> ByRef riid As Guid,
                    <Out(), MarshalAs(UnmanagedType.IUnknown)> ByVal ppvObj As Object) As Integer

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            <PreserveSig()>
            Function GetMops(
                    <[In]()> ByVal memid As Integer,
                    <Out(), MarshalAs(UnmanagedType.BStr)> ByVal pBstrMops As String) As Integer

            <PreserveSig()>
            Function GetContainingTypeLib(
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTLib As ITypeLib(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pIndex As Integer()) As Integer

            <PreserveSig()>
            Sub ReleaseTypeAttr(ByVal typeAttr As IntPtr)

            <PreserveSig()>
            Sub ReleaseFuncDesc(ByVal funcDesc As IntPtr)

            <PreserveSig()>
            Sub ReleaseVarDesc(ByVal varDesc As IntPtr)
        End Interface

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),
         ComImport(),
         Guid("B196B283-BAB4-101A-B69C-00AA00341D07"),
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)>
        Public Interface IProvideClassInfo
            Function GetClassInfo() As <MarshalAs(UnmanagedType.Interface)> ITypeInfo
        End Interface

        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never),
         ComImport(),
         Guid("00020402-0000-0000-C000-000000000046"),
         InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)>
        Public Interface ITypeLib
            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub RemoteGetTypeInfoCount(
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pcTInfo As Integer())

            Sub GetTypeInfo(
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())

            Sub GetTypeInfoType(
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal index As Integer,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pTKind As ComTypes.TYPEKIND())

            Sub GetTypeInfoOfGuid(
                    <[In]()> ByRef guid As Guid,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub RemoteGetLibAttr(
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTLibAttr As tagTLIBATTR(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pDummy As Integer())

            Sub GetTypeComp(
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTComp As ITypeComp())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub RemoteGetDocumentation(
            ByVal index As Integer,
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal refPtrFlags As Integer,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrName As String(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrDocString As String(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pdwHelpContext As Integer(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrHelpFile As String())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub RemoteIsName(
                    <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szNameBuf As String,
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pfName As IntPtr(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrLibName As String())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub RemoteFindName(
                    <[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szNameBuf As String,
                    <[In](), MarshalAs(UnmanagedType.U4)> ByVal lHashVal As Integer,
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal ppTInfo As ITypeInfo(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal rgMemId As Integer(),
                    <[In](), Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pcFound As Short(),
                    <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal pBstrLibName As String())

            <Obsolete("Bad signature. Fix and verify signature before use.", True)>
            Sub LocalReleaseTLibAttr()
        End Interface

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
