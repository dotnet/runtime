// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices.ComTypes
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BIND_OPTS
    {
        public int cbStruct;
        public int grfFlags;
        public int grfMode;
        public int dwTickCountDeadline;
    }

    [Guid("0000000e-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IBindCtx
    {
        void RegisterObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);
        void RevokeObjectBound([MarshalAs(UnmanagedType.Interface)] object punk);
        void ReleaseBoundObjects();
        void SetBindOptions([In()] ref BIND_OPTS pbindopts);
        void GetBindOptions(ref BIND_OPTS pbindopts);
        void GetRunningObjectTable(out IRunningObjectTable? pprot);
        void RegisterObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] object punk);
        void GetObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey, [MarshalAs(UnmanagedType.Interface)] out object? ppunk);
        void EnumObjectParam(out IEnumString? ppenum);
        [PreserveSig]
        int RevokeObjectParam([MarshalAs(UnmanagedType.LPWStr)] string pszKey);
    }
}
