// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using SafeHandlesTests;

#pragma warning disable 618
namespace SafeHandlesTests{
    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithSHFld
    {
        public SafeFileHandle hnd; //SH subclass field
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithBaseSHFld
    {
        public SafeHandle hnd; //SH field
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithChildSHFld
    {
        public ChildSafeFileHandle hnd; //SafeFileHandle subclass field
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructNestedParent
    {
        public StructNestedOneDeep snOneDeep;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructNestedOneDeep
    {
        public StructWithSHFld s;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithSHArrayFld
    {
        public SafeFileHandle[] sharr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithManySHFlds
    {
        public SafeHandle hnd1;
        public SafeFileHandle hnd2;
        public ChildSafeFileHandle hnd3;

        public SafeHandle hnd4;
        public SafeFileHandle hnd5;
        public ChildSafeFileHandle hnd6;

        public SafeHandle hnd7;
        public SafeFileHandle hnd8;
        public ChildSafeFileHandle hnd9;

        public SafeHandle hnd10;
        public SafeFileHandle hnd11;
        public ChildSafeFileHandle hnd12;

        public SafeHandle hnd13;
        public SafeFileHandle hnd14;
        public ChildSafeFileHandle hnd15;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructWithObjFld
    {
        //the MA attribute indicates that obj is to be marshaled as a VARIANT
        [MarshalAs(UnmanagedType.Struct)]
        public Object obj;
    }

    ////The following Structure definitions are for negative testing purposes
    ///
    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA1
    {
        [MarshalAs(UnmanagedType.AnsiBStr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA2
    {
        [MarshalAs(UnmanagedType.AsAny)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA3
    {
        [MarshalAs(UnmanagedType.Bool)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA4
    {
        [MarshalAs(UnmanagedType.BStr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA5
    {
        [MarshalAs(UnmanagedType.ByValArray)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA6
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA7
    {
        [MarshalAs(UnmanagedType.Currency)]
        public SafeFileHandle hnd;
    }

    //NOTE: Specified unmanaged type also needs MarshalType or MarshalTypeRef which indicates the custom marshaler
    //[StructLayout(LayoutKind.Sequential)]
    //public struct StructMA8
    //{
    //	[MarshalAs(UnmanagedType.CustomMarshaler)]
    //	public SafeFileHandle hnd;
    //}

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA9
    {
        [MarshalAs(UnmanagedType.Error)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA10
    {
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA11
    {
        [MarshalAs(UnmanagedType.I1)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA12
    {
        [MarshalAs(UnmanagedType.I2)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA13
    {
        [MarshalAs(UnmanagedType.I4)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA14
    {
        [MarshalAs(UnmanagedType.I8)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA15
    {
        [MarshalAs(UnmanagedType.IDispatch)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA16
    {
        [MarshalAs(UnmanagedType.Interface)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA17
    {
        [MarshalAs(UnmanagedType.IUnknown)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA18
    {
        [MarshalAs(UnmanagedType.LPArray)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA19
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA20
    {
        [MarshalAs(UnmanagedType.LPStruct)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA21
    {
        [MarshalAs(UnmanagedType.LPTStr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA22
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA23
    {
        [MarshalAs(UnmanagedType.R4)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA24
    {
        [MarshalAs(UnmanagedType.R8)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA25
    {
        [MarshalAs(UnmanagedType.SafeArray)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA26
    {
        [MarshalAs(UnmanagedType.Struct)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA27
    {
        [MarshalAs(UnmanagedType.SysInt)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA28
    {
        [MarshalAs(UnmanagedType.SysUInt)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA29
    {
        [MarshalAs(UnmanagedType.TBStr)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA30
    {
        [MarshalAs(UnmanagedType.U1)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA31
    {
        [MarshalAs(UnmanagedType.U2)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA32
    {
        [MarshalAs(UnmanagedType.U4)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA33
    {
        [MarshalAs(UnmanagedType.U8)]
        public SafeFileHandle hnd;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StructMA34
    {
        [MarshalAs(UnmanagedType.VariantBool)]
        public SafeFileHandle hnd;
    }

    //NOTE: This unmanagedtype is not valid for fields
    //[StructLayout(LayoutKind.Sequential)]
    //public struct StructMA35
    //{
    //	[MarshalAs(UnmanagedType.VBByRefStr)]
    //	public SafeFileHandle hnd;
    //}
}
#pragma warning restore 618



