// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 618 // Must test deprecated features

[ComVisible(true)]
[Guid(Server.Contract.Guids.NumericTesting)]
public class NumericTesting : Server.Contract.INumericTesting
{
    public byte Add_Byte(byte a, byte b)
    {
        return (byte)(a + b);
    }

    public short Add_Short(short a, short b)
    {
        return (short)(a + b);
    }

    public ushort Add_UShort(ushort a, ushort b)
    {
        return (ushort)(a + b);
    }

    public int Add_Int(int a, int b)
    {
        return a + b;
    }

    public uint Add_UInt(uint a, uint b)
    {
        return a + b;
    }

    public long Add_Long(long a, long b)
    {
        return a + b;
    }

    public ulong Add_ULong(ulong a, ulong b)
    {
        return a + b;
    }

    public float Add_Float(float a, float b)
    {
        return a + b;
    }

    public double Add_Double(double a, double b)
    {
        return a + b;
    }

    public void Add_Byte_Ref(byte a, byte b, ref byte c)
    {
        if (c != byte.MaxValue)
        {
            throw new Exception();
        }

        c = (byte)(a + b);
    }

    public void Add_Short_Ref(short a, short b, ref short c)
    {
        if (c != short.MaxValue)
        {
            throw new Exception();
        }

        c = (short)(a + b);
    }

    public void Add_UShort_Ref(ushort a, ushort b, ref ushort c)
    {
        if (c != ushort.MaxValue)
        {
            throw new Exception();
        }

        c = (ushort)(a + b);
    }

    public void Add_Int_Ref(int a, int b, ref int c)
    {
        if (c != int.MaxValue)
        {
            throw new Exception();
        }

        c = a + b;
    }

    public void Add_UInt_Ref(uint a, uint b, ref uint c)
    {
        if (c != uint.MaxValue)
        {
            throw new Exception();
        }

        c = a + b;
    }

    public void Add_Long_Ref(long a, long b, ref long c)
    {
        if (c != long.MaxValue)
        {
            throw new Exception();
        }

        c = a + b;
    }

    public void Add_ULong_Ref(ulong a, ulong b, ref ulong c)
    {
        if (c != ulong.MaxValue)
        {
            throw new Exception();
        }

        c = a + b;
    }

    public void Add_Float_Ref(float a, float b, ref float c)
    {
        if (c != float.MaxValue)
        {
            throw new Exception();
        }

        c = a + b;
    }

    public void Add_Double_Ref(double a, double b, ref double c)
    {
        if (c != double.MaxValue)
        {
            throw new Exception();
        }

        c = a + b;
    }

    public void Add_Byte_Out(byte a, byte b, out byte c)
    {
        c = (byte)(a + b);
    }

    public void Add_Short_Out(short a, short b, out short c)
    {
        c = (short)(a + b);
    }

    public void Add_UShort_Out(ushort a, ushort b, out ushort c)
    {
        c = (ushort)(a + b);
    }

    public void Add_Int_Out(int a, int b, out int c)
    {
        c = a + b;
    }

    public void Add_UInt_Out(uint a, uint b, out uint c)
    {
        c = a + b;
    }

    public void Add_Long_Out(long a, long b, out long c)
    {
        c = a + b;
    }

    public void Add_ULong_Out(ulong a, ulong b, out ulong c)
    {
        c = a + b;
    }

    public void Add_Float_Out(float a, float b, out float c)
    {
        c = a + b;
    }

    public void Add_Double_Out(double a, double b, out double c)
    {
        c = a + b;
    }

    public int Add_ManyInts11(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11)
    {
        return i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 + i11;
    }

    public int Add_ManyInts12(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, int i10, int i11, int i12)
    {
        return i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10 + i11 + i12;
    }
}
