// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct VT
{
    public ulong[,] ulong2darr;
    public ulong[, ,] ulong3darr;
    public ulong[,] ulong2darr_b;
    public ulong[, ,] ulong3darr_b;
}

public class CL
{
    public ulong[,] ulong2darr = { { 0, 1 }, { 0, 0 } };
    public ulong[, ,] ulong3darr = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    public ulong[,] ulong2darr_b = { { 0, 49 }, { 0, 0 } };
    public ulong[, ,] ulong3darr_b = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };
}

public class ulongMDArrTest
{

    static ulong[,] ulong2darr = { { 0, 1 }, { 0, 0 } };
    static ulong[, ,] ulong3darr = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    static ulong[,] ulong2darr_b = { { 0, 49 }, { 0, 0 } };
    static ulong[, ,] ulong3darr_b = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

    static ulong[][,] ja1 = new ulong[2][,];
    static ulong[][, ,] ja2 = new ulong[2][, ,];
    static ulong[][,] ja1_b = new ulong[2][,];
    static ulong[][, ,] ja2_b = new ulong[2][, ,];

    [Fact]
    public static int TestEntryPoint()
    {

        bool pass = true;

        VT vt1;
        vt1.ulong2darr = new ulong[,] { { 0, 1 }, { 0, 0 } };
        vt1.ulong3darr = new ulong[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        vt1.ulong2darr_b = new ulong[,] { { 0, 49 }, { 0, 0 } };
        vt1.ulong3darr_b = new ulong[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        CL cl1 = new CL();

        ja1[0] = new ulong[,] { { 0, 1 }, { 0, 0 } };
        ja2[1] = new ulong[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        ja1_b[0] = new ulong[,] { { 0, 49 }, { 0, 0 } };
        ja2_b[1] = new ulong[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        ulong result = 1;

        // 2D
        if (result != ulong2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.ulong2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.ulong2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != ja1[0][0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (result != ulong3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.ulong3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.ulong3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != ja2[1][1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToBool
        bool Bool_result = true;

        // 2D
        if (Bool_result != Convert.ToBoolean(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Bool_result != Convert.ToBoolean(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToByte
        byte Byte_result = 1;

        // 2D
        if (Byte_result != Convert.ToByte(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Byte_result != Convert.ToByte(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToChar
        char Char_result = '1';

        // 2D
        if (Char_result != Convert.ToChar(ulong2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.ulong2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.ulong2darr_b[0, 1] is: {0}", vt1.ulong2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.ulong2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.ulong2darr_b[0, 1] is: {0}", cl1.ulong2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(ja1_b[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("ja1_b[0][0, 1] is: {0}", ja1_b[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Char_result != Convert.ToChar(ulong3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("ulong3darr_b[1,0,1] is: {0}", ulong3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.ulong3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.ulong3darr_b[1,0,1] is: {0}", vt1.ulong3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.ulong3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.ulong3darr_b[1,0,1] is: {0}", cl1.ulong3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(ja2_b[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("ja2_b[1][1,0,1] is: {0}", ja2_b[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToDecimal
        decimal Decimal_result = 1;

        // 2D
        if (Decimal_result != Convert.ToDecimal(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Decimal_result != Convert.ToDecimal(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToDouble
        double Double_result = 1;

        // 2D
        if (Double_result != Convert.ToDouble(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Double_result != Convert.ToDouble(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToSingle
        float Single_result = 1;

        // 2D
        if (Single_result != Convert.ToSingle(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Single_result != Convert.ToSingle(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToInt32
        int Int32_result = 1;

        // 2D
        if (Int32_result != Convert.ToInt32(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int32_result != Convert.ToInt32(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToInt64
        long Int64_result = 1;

        // 2D
        if (Int64_result != Convert.ToInt64(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int64_result != Convert.ToInt64(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToSByte
        sbyte SByte_result = 1;

        // 2D
        if (SByte_result != Convert.ToSByte(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (SByte_result != Convert.ToSByte(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        short Int16_result = 1;

        // 2D
        if (Int16_result != Convert.ToInt16(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int16_result != Convert.ToInt16(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToUInt32
        uint UInt32_result = 1;

        // 2D
        if (UInt32_result != Convert.ToUInt32(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt32_result != Convert.ToUInt32(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //UInt64ToUInt16
        ushort UInt16_result = 1;

        // 2D
        if (UInt16_result != Convert.ToUInt16(ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.ulong2darr[0, 1] is: {0}", vt1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.ulong2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.ulong2darr[0, 1] is: {0}", cl1.ulong2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt16_result != Convert.ToUInt16(ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("ulong3darr[1,0,1] is: {0}", ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.ulong3darr[1,0,1] is: {0}", vt1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.ulong3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.ulong3darr[1,0,1] is: {0}", cl1.ulong3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (!pass)
        {
            Console.WriteLine("FAILED");
            return 1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }


    }

};
