// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices.ComTypes
{
    [Guid("00020411-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ITypeLib2 : ITypeLib
    {
        [PreserveSig]
        new int GetTypeInfoCount();
        new void GetTypeInfo(int index, out ITypeInfo ppTI);
        new void GetTypeInfoType(int index, out TYPEKIND pTKind);
        new void GetTypeInfoOfGuid(ref Guid guid, out ITypeInfo ppTInfo);
        new void GetLibAttr(out IntPtr ppTLibAttr);
        new void GetTypeComp(out ITypeComp ppTComp);
        new void GetDocumentation(int index, out string strName, out string strDocString, out int dwHelpContext, out string strHelpFile);
        [return: MarshalAs(UnmanagedType.Bool)]
        new bool IsName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal);
        new void FindName([MarshalAs(UnmanagedType.LPWStr)] string szNameBuf, int lHashVal, [MarshalAs(UnmanagedType.LPArray), Out] ITypeInfo[] ppTInfo, [MarshalAs(UnmanagedType.LPArray), Out] int[] rgMemId, ref short pcFound);
        [PreserveSig]
        new void ReleaseTLibAttr(IntPtr pTLibAttr);
        void GetCustData(ref Guid guid, out object pVarVal);
        [LCIDConversion(1)]
        void GetDocumentation2(int index, out string pbstrHelpString, out int pdwHelpStringContext, out string pbstrHelpStringDll);
        void GetLibStatistics(IntPtr pcUniqueNames, out int pcchUniqueNames);
        void GetAllCustData(IntPtr pCustData);
    }
}
