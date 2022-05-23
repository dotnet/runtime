// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct VT
{
    public ushort[,] ushort2darr;
    public ushort[, ,] ushort3darr;
    public ushort[,] ushort2darr_b;
    public ushort[, ,] ushort3darr_b;
}

public class CL
{
    public ushort[,] ushort2darr = { { 0, 1 }, { 0, 0 } };
    public ushort[, ,] ushort3darr = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    public ushort[,] ushort2darr_b = { { 0, 49 }, { 0, 0 } };
    public ushort[, ,] ushort3darr_b = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };
}

public class ushortMDArrTest
{

    static ushort[,] ushort2darr = { { 0, 1 }, { 0, 0 } };
    static ushort[, ,] ushort3darr = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    static ushort[,] ushort2darr_b = { { 0, 49 }, { 0, 0 } };
    static ushort[, ,] ushort3darr_b = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

    static ushort[][,] ja1 = new ushort[2][,];
    static ushort[][, ,] ja2 = new ushort[2][, ,];
    static ushort[][,] ja1_b = new ushort[2][,];
    static ushort[][, ,] ja2_b = new ushort[2][, ,];

    [Fact]
    public static int TestEntryPoint()
    {

        bool pass = true;

        VT vt1;
        vt1.ushort2darr = new ushort[,] { { 0, 1 }, { 0, 0 } };
        vt1.ushort3darr = new ushort[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        vt1.ushort2darr_b = new ushort[,] { { 0, 49 }, { 0, 0 } };
        vt1.ushort3darr_b = new ushort[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        CL cl1 = new CL();

        ja1[0] = new ushort[,] { { 0, 1 }, { 0, 0 } };
        ja2[1] = new ushort[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        ja1_b[0] = new ushort[,] { { 0, 49 }, { 0, 0 } };
        ja2_b[1] = new ushort[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        ushort result = 1;

        // 2D
        if (result != ushort2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.ushort2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.ushort2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (result != ushort3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.ushort3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.ushort3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToBool
        bool Bool_result = true;

        // 2D
        if (Bool_result != Convert.ToBoolean(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Bool_result != Convert.ToBoolean(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToByte tests
        byte Byte_result = 1;

        // 2D
        if (Byte_result != Convert.ToByte(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Byte_result != Convert.ToByte(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToChar
        char Char_result = '1';

        // 2D
        if (Char_result != Convert.ToChar(ushort2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.ushort2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.ushort2darr_b[0, 1] is: {0}", vt1.ushort2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.ushort2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.ushort2darr_b[0, 1] is: {0}", cl1.ushort2darr_b[0, 1]);
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
        if (Char_result != Convert.ToChar(ushort3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("ushort3darr_b[1,0,1] is: {0}", ushort3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.ushort3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.ushort3darr_b[1,0,1] is: {0}", vt1.ushort3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.ushort3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.ushort3darr_b[1,0,1] is: {0}", cl1.ushort3darr_b[1, 0, 1]);
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

        //UInt16ToDecimal
        decimal Decimal_result = 1;

        // 2D
        if (Decimal_result != Convert.ToDecimal(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Decimal_result != Convert.ToDecimal(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToDouble
        double Double_result = 1;

        // 2D
        if (Double_result != Convert.ToDouble(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Double_result != Convert.ToDouble(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToSingle
        float Single_result = 1;

        // 2D
        if (Single_result != Convert.ToSingle(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Single_result != Convert.ToSingle(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToInt32
        int Int32_result = 1;

        // 2D
        if (Int32_result != Convert.ToInt32(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Int32_result != Convert.ToInt32(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToInt64
        long Int64_result = 1;

        // 2D
        if (Int64_result != Convert.ToInt64(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Int64_result != Convert.ToInt64(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToSByte
        sbyte SByte_result = 1;

        // 2D
        if (SByte_result != Convert.ToSByte(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (SByte_result != Convert.ToSByte(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToInt16
        short Int16_result = 1;

        // 2D
        if (Int16_result != Convert.ToInt16(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (Int16_result != Convert.ToInt16(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToUInt32
        uint UInt32_result = 1;

        // 2D
        if (UInt32_result != Convert.ToUInt32(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
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
        if (UInt32_result != Convert.ToUInt32(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
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

        //UInt16ToUInt64 tests
        ulong UInt64_result = 1;

        // 2D
        if (UInt64_result != Convert.ToUInt64(ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.ushort2darr[0, 1] is: {0}", vt1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.ushort2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.ushort2darr[0, 1] is: {0}", cl1.ushort2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt64_result != Convert.ToUInt64(ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("ushort3darr[1,0,1] is: {0}", ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.ushort3darr[1,0,1] is: {0}", vt1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.ushort3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.ushort3darr[1,0,1] is: {0}", cl1.ushort3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
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
