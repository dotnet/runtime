﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 618 // Must test deprecated features

namespace Server.Contract
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    [ComVisible(true)]
    [Guid("05655A94-A915-4926-815D-A9EA648BAAD9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INumericTesting
    {
        byte Add_Byte(byte a, byte b);
        short Add_Short(short a, short b);
        ushort Add_UShort(ushort a, ushort b);
        int Add_Int(int a, int b);
        uint Add_UInt(uint a, uint b);
        long Add_Long(long a, long b);
        ulong Add_ULong(ulong a, ulong b);
        float Add_Float(float a, float b);
        double Add_Double(double a, double b);

        void Add_Byte_Ref(byte a, byte b, ref byte c);
        void Add_Short_Ref(short a, short b, ref short c);
        void Add_UShort_Ref(ushort a, ushort b, ref ushort c);
        void Add_Int_Ref(int a, int b, ref int c);
        void Add_UInt_Ref(uint a, uint b, ref uint c);
        void Add_Long_Ref(long a, long b, ref long c);
        void Add_ULong_Ref(ulong a, ulong b, ref ulong c);
        void Add_Float_Ref(float a, float b, ref float c);
        void Add_Double_Ref(double a, double b, ref double c);

        void Add_Byte_Out(byte a, byte b, out byte c);
        void Add_Short_Out(short a, short b, out short c);
        void Add_UShort_Out(ushort a, ushort b, out ushort c);
        void Add_Int_Out(int a, int b, out int c);
        void Add_UInt_Out(uint a, uint b, out uint c);
        void Add_Long_Out(long a, long b, out long c);
        void Add_ULong_Out(ulong a, ulong b, out ulong c);
        void Add_Float_Out(float a, float b, out float c);
        void Add_Double_Out(double a, double b, out double c);
    }

    [ComVisible(true)]
    [Guid("7731CB31-E063-4CC8-BCD2-D151D6BC8F43")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IArrayTesting
    {
        double Mean_Byte_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] byte[] d);
        double Mean_Short_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] short[] d);
        double Mean_UShort_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] ushort[] d);
        double Mean_Int_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] int[] d);
        double Mean_UInt_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] uint[] d);
        double Mean_Long_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] long[] d);
        double Mean_ULong_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] ulong[] d);
        double Mean_Float_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] float[] d);
        double Mean_Double_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray)] double[] d);

        double Mean_Byte_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] byte[] d, int len);
        double Mean_Short_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] short[] d, int len);
        double Mean_UShort_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] ushort[] d, int len);
        double Mean_Int_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] int[] d, int len);
        double Mean_UInt_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] uint[] d, int len);
        double Mean_Long_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] long[] d, int len);
        double Mean_ULong_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] ulong[] d, int len);
        double Mean_Float_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] float[] d, int len);
        double Mean_Double_LP_PostLen([MarshalAs(UnmanagedType.LPArray)] double[] d, int len);

        double Mean_Byte_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] byte[] d, out int len);
        double Mean_Short_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] short[] d, out int len);
        double Mean_UShort_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] ushort[] d, out int len);
        double Mean_Int_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] int[] d, out int len);
        double Mean_UInt_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] uint[] d, out int len);
        double Mean_Long_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] long[] d, out int len);
        double Mean_ULong_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] ulong[] d, out int len);
        double Mean_Float_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] float[] d, out int len);
        double Mean_Double_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] double[] d, out int len);
    }

    [ComVisible(true)]
    [Guid("7044C5C0-C6C6-4713-9294-B4A4E86D58CC")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IStringTesting
    {
        [return: MarshalAs(UnmanagedType.LPStr)]
        string Add_LPStr(
            [MarshalAs(UnmanagedType.LPStr)] string a,
            [MarshalAs(UnmanagedType.LPStr)] string b);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string Add_LPWStr(
            [MarshalAs(UnmanagedType.LPWStr)] string a,
            [MarshalAs(UnmanagedType.LPWStr)] string b);

        [return: MarshalAs(UnmanagedType.BStr)]
        string Add_BStr(
            [MarshalAs(UnmanagedType.BStr)] string a,
            [MarshalAs(UnmanagedType.BStr)] string b);

        // LPStr

        [return: MarshalAs(UnmanagedType.LPStr)]
        string Reverse_LPStr([MarshalAs(UnmanagedType.LPStr)] string a);

        [return: MarshalAs(UnmanagedType.LPStr)]
        string Reverse_LPStr_Ref([MarshalAs(UnmanagedType.LPStr)] ref string a);

        [return: MarshalAs(UnmanagedType.LPStr)]
        string Reverse_LPStr_InRef([In][MarshalAs(UnmanagedType.LPStr)] ref string a);

        void Reverse_LPStr_Out([MarshalAs(UnmanagedType.LPStr)] string a, [MarshalAs(UnmanagedType.LPStr)] out string b);

        void Reverse_LPStr_OutAttr([MarshalAs(UnmanagedType.LPStr)] string a, [Out][MarshalAs(UnmanagedType.LPStr)] string b);

        [return: MarshalAs(UnmanagedType.LPStr)]
        StringBuilder Reverse_SB_LPStr([MarshalAs(UnmanagedType.LPStr)] StringBuilder a);

        [return: MarshalAs(UnmanagedType.LPStr)]
        StringBuilder Reverse_SB_LPStr_Ref([MarshalAs(UnmanagedType.LPStr)] ref StringBuilder a);

        [return: MarshalAs(UnmanagedType.LPStr)]
        StringBuilder Reverse_SB_LPStr_InRef([In][MarshalAs(UnmanagedType.LPStr)] ref StringBuilder a);

        void Reverse_SB_LPStr_Out([MarshalAs(UnmanagedType.LPStr)] StringBuilder a, [MarshalAs(UnmanagedType.LPStr)] out StringBuilder b);

        void Reverse_SB_LPStr_OutAttr([MarshalAs(UnmanagedType.LPStr)] StringBuilder a, [Out][MarshalAs(UnmanagedType.LPStr)] StringBuilder b);

        // LPWStr

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string Reverse_LPWStr([MarshalAs(UnmanagedType.LPWStr)] string a);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string Reverse_LPWStr_Ref([MarshalAs(UnmanagedType.LPWStr)] ref string a);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string Reverse_LPWStr_InRef([In][MarshalAs(UnmanagedType.LPWStr)] ref string a);

        void Reverse_LPWStr_Out([MarshalAs(UnmanagedType.LPWStr)] string a, [MarshalAs(UnmanagedType.LPWStr)] out string b);

        void Reverse_LPWStr_OutAttr([MarshalAs(UnmanagedType.LPWStr)] string a, [Out][MarshalAs(UnmanagedType.LPWStr)] string b);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder Reverse_SB_LPWStr([MarshalAs(UnmanagedType.LPWStr)] StringBuilder a);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder Reverse_SB_LPWStr_Ref([MarshalAs(UnmanagedType.LPWStr)] ref StringBuilder a);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder Reverse_SB_LPWStr_InRef([In][MarshalAs(UnmanagedType.LPWStr)] ref StringBuilder a);

        void Reverse_SB_LPWStr_Out([MarshalAs(UnmanagedType.LPWStr)] StringBuilder a, [MarshalAs(UnmanagedType.LPWStr)] out StringBuilder b);

        void Reverse_SB_LPWStr_OutAttr([MarshalAs(UnmanagedType.LPWStr)] StringBuilder a, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder b);

        // BSTR

        [return: MarshalAs(UnmanagedType.BStr)]
        string Reverse_BStr([MarshalAs(UnmanagedType.BStr)] string a);

        [return: MarshalAs(UnmanagedType.BStr)]
        string Reverse_BStr_Ref([MarshalAs(UnmanagedType.BStr)] ref string a);

        [return: MarshalAs(UnmanagedType.BStr)]
        string Reverse_BStr_InRef([In][MarshalAs(UnmanagedType.BStr)] ref string a);

        void Reverse_BStr_Out([MarshalAs(UnmanagedType.BStr)] string a, [MarshalAs(UnmanagedType.BStr)] out string b);

        void Reverse_BStr_OutAttr([MarshalAs(UnmanagedType.BStr)] string a, [Out][MarshalAs(UnmanagedType.BStr)] string b);
    }

    [ComVisible(true)]
    [Guid("592386A5-6837-444D-9DE3-250815D18556")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IErrorMarshalTesting
    {
        void Throw_HResult(int hresultToReturn);

        [PreserveSig]
        int Return_As_HResult(int hresultToReturn);
    }
}

#pragma warning restore 618 // Must test deprecated features
