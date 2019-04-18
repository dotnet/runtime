// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices.ComTypes
{
    [Guid("00020412-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ITypeInfo2 : ITypeInfo
    {
        new void GetTypeAttr(out IntPtr ppTypeAttr);
        new void GetTypeComp(out ITypeComp ppTComp);
        new void GetFuncDesc(int index, out IntPtr ppFuncDesc);
        new void GetVarDesc(int index, out IntPtr ppVarDesc);
        new void GetNames(int memid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] string[] rgBstrNames, int cMaxNames, out int pcNames);
        new void GetRefTypeOfImplType(int index, out int href);
        new void GetImplTypeFlags(int index, out IMPLTYPEFLAGS pImplTypeFlags);
        new void GetIDsOfNames([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1), In] string[] rgszNames, int cNames, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] int[] pMemId);
        new void Invoke([MarshalAs(UnmanagedType.IUnknown)] object pvInstance, int memid, short wFlags, ref DISPPARAMS pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, out int puArgErr);
        new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        new void GetDllEntry(int memid, INVOKEKIND invKind, IntPtr pBstrDllName, IntPtr pBstrName, IntPtr pwOrdinal);
        new void GetRefTypeInfo(int hRef, out ITypeInfo ppTI);
        new void AddressOfMember(int memid, INVOKEKIND invKind, out IntPtr ppv);
        new void CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object? pUnkOuter, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown), Out] out object ppvObj);
        new void GetMops(int memid, out string? pBstrMops);
        new void GetContainingTypeLib(out ITypeLib ppTLB, out int pIndex);
        [PreserveSig]
        new void ReleaseTypeAttr(IntPtr pTypeAttr);
        [PreserveSig]
        new void ReleaseFuncDesc(IntPtr pFuncDesc);
        [PreserveSig]
        new void ReleaseVarDesc(IntPtr pVarDesc);
        void GetTypeKind(out TYPEKIND pTypeKind);
        void GetTypeFlags(out int pTypeFlags);
        void GetFuncIndexOfMemId(int memid, INVOKEKIND invKind, out int pFuncIndex);
        void GetVarIndexOfMemId(int memid, out int pVarIndex);
        void GetCustData(ref Guid guid, out object pVarVal);
        void GetFuncCustData(int index, ref Guid guid, out object pVarVal);
        void GetParamCustData(int indexFunc, int indexParam, ref Guid guid, out object pVarVal);
        void GetVarCustData(int index, ref Guid guid, out object pVarVal);
        void GetImplTypeCustData(int index, ref Guid guid, out object pVarVal);
        [LCIDConversion(1)]
        void GetDocumentation2(int memid, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);
        void GetAllCustData(IntPtr pCustData);
        void GetAllFuncCustData(int index, IntPtr pCustData);
        void GetAllParamCustData(int indexFunc, int indexParam, IntPtr pCustData);
        void GetAllVarCustData(int index, IntPtr pCustData);
        void GetAllImplTypeCustData(int index, IntPtr pCustData);
    }
}
