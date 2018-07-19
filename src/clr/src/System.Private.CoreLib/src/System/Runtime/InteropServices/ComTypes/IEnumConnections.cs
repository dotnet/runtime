// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.ComTypes
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CONNECTDATA
    {
        [MarshalAs(UnmanagedType.Interface)]
        public object pUnk;
        public int dwCookie;
    }

    [Guid("B196B287-BAB4-101A-B69C-00AA00341D07")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IEnumConnections
    {
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] CONNECTDATA[] rgelt, IntPtr pceltFetched);
        [PreserveSig]
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumConnections ppenum);
    }
}
