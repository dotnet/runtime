// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 618 // Must test deprecated features

[ComVisible(true)]
[Guid(Server.Contract.Guids.ArrayTesting)]
public class ArrayTesting : Server.Contract.IArrayTesting
{
    private static double Mean(byte[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(short[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(ushort[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(int[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(uint[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(long[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(ulong[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(float[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }
    private static double Mean(double[] d)
    {
        double t = 0.0;
        foreach (var b in d)
        {
            t += b;
        }
        return (t / d.Length);
    }

    public double Mean_Byte_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] byte[] d)
    {
        return Mean(d);
    }

    public double Mean_Short_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] short[] d)
    {
        return Mean(d);
    }

    public double Mean_UShort_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] ushort[] d)
    {
        return Mean(d);
    }

    public double Mean_Int_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] int[] d)
    {
        return Mean(d);
    }

    public double Mean_UInt_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] uint[] d)
    {
        return Mean(d);
    }

    public double Mean_Long_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] long[] d)
    {
        return Mean(d);
    }

    public double Mean_ULong_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] ulong[] d)
    {
        return Mean(d);
    }

    public double Mean_Float_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] float[] d)
    {
        return Mean(d);
    }

    public double Mean_Double_LP_PreLen(int len, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] double[] d)
    {
        return Mean(d);
    }

    public double Mean_Byte_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] byte[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_Short_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] short[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_UShort_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] ushort[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_Int_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] int[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_UInt_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] uint[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_Long_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] long[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_ULong_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] ulong[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_Float_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] float[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_Double_LP_PostLen([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] double[] d, int len)
    {
        return Mean(d);
    }

    public double Mean_Byte_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] byte[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_Short_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] short[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_UShort_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] ushort[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_Int_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] int[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_UInt_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] uint[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_Long_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] long[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_ULong_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] ulong[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_Float_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] float[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }

    public double Mean_Double_SafeArray_OutLen([MarshalAs(UnmanagedType.SafeArray)] double[] d, out int len)
    {
        len = d.Length;
        return Mean(d);
    }
}

#pragma warning restore 618 // Must test deprecated features