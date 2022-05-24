// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct VT
{
    public long[,] long2darr;
    public long[, ,] long3darr;
    public long[,] long2darr_b;
    public long[, ,] long3darr_b;
    public long[,] long2darr_c;
    public long[, ,] long3darr_c;
}

public class CL
{
    public long[,] long2darr = { { 0, -1 }, { 0, 0 } };
    public long[, ,] long3darr = { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
    public long[,] long2darr_b = { { 0, 1 }, { 0, 0 } };
    public long[, ,] long3darr_b = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    public long[,] long2darr_c = { { 0, 49 }, { 0, 0 } };
    public long[, ,] long3darr_c = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };
}

public class longMDArrTest
{

    static long[,] long2darr = { { 0, -1 }, { 0, 0 } };
    static long[, ,] long3darr = { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
    static long[,] long2darr_b = { { 0, 1 }, { 0, 0 } };
    static long[, ,] long3darr_b = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    static long[,] long2darr_c = { { 0, 49 }, { 0, 0 } };
    static long[, ,] long3darr_c = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

    static long[][,] ja1 = new long[2][,];
    static long[][, ,] ja2 = new long[2][, ,];
    static long[][,] ja1_b = new long[2][,];
    static long[][, ,] ja2_b = new long[2][, ,];
    static long[][,] ja1_c = new long[2][,];
    static long[][, ,] ja2_c = new long[2][, ,];

    [Fact]
    public static int TestEntryPoint()
    {

        bool pass = true;

        VT vt1;
        vt1.long2darr = new long[,] { { 0, -1 }, { 0, 0 } };
        vt1.long3darr = new long[,,] { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
        vt1.long2darr_b = new long[,] { { 0, 1 }, { 0, 0 } };
        vt1.long3darr_b = new long[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        vt1.long2darr_c = new long[,] { { 0, 49 }, { 0, 0 } };
        vt1.long3darr_c = new long[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        CL cl1 = new CL();

        ja1[0] = new long[,] { { 0, -1 }, { 0, 0 } };
        ja2[1] = new long[,,] { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
        ja1_b[0] = new long[,] { { 0, 1 }, { 0, 0 } };
        ja2_b[1] = new long[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        ja1_c[0] = new long[,] { { 0, 49 }, { 0, 0 } };
        ja2_c[1] = new long[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        long result = -1;

        // 2D
        if (result != long2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.long2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.long2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (result != long3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.long3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.long3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToBool
        bool Bool_result = true;

        // 2D
        if (Bool_result != Convert.ToBoolean(long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.long2darr_b[0, 1] is: {0}", vt1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.long2darr_b[0, 1] is: {0}", cl1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(ja1_b[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("ja1_b[0][0, 1] is: {0}", ja1_b[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Bool_result != Convert.ToBoolean(long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("long3darr_b[1,0,1] is: {0}", long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.long3darr_b[1,0,1] is: {0}", vt1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.long3darr_b[1,0,1] is: {0}", cl1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(ja2_b[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("ja2_b[1][1,0,1] is: {0}", ja2_b[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //Int64ToByte tests
        byte Byte_result = 1;

        // 2D
        if (Byte_result != Convert.ToByte(long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.long2darr_b[0, 1] is: {0}", vt1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.long2darr_b[0, 1] is: {0}", cl1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(ja1_b[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ja1_b[0][0, 1] is: {0}", ja1_b[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Byte_result != Convert.ToByte(long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("long3darr_b[1,0,1] is: {0}", long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.long3darr_b[1,0,1] is: {0}", vt1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.long3darr_b[1,0,1] is: {0}", cl1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(ja2_b[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ja2_b[1][1,0,1] is: {0}", ja2_b[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //Int64ToChar tests
        char Char_result = '1';

        // 2D
        if (Char_result != Convert.ToChar(long2darr_c[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr_c[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.long2darr_c[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.long2darr_c[0, 1] is: {0}", vt1.long2darr_c[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.long2darr_c[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.long2darr_c[0, 1] is: {0}", cl1.long2darr_c[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(ja1_c[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("ja1_c[0][0, 1] is: {0}", ja1_c[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Char_result != Convert.ToChar(long3darr_c[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("long3darr_c[1,0,1] is: {0}", long3darr_c[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.long3darr_c[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.long3darr_c[1,0,1] is: {0}", vt1.long3darr_c[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.long3darr_c[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.long3darr_c[1,0,1] is: {0}", cl1.long3darr_c[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(ja2_c[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("ja2_c[1][1,0,1] is: {0}", ja2_c[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //Int64ToDecimal tests
        decimal Decimal_result = -1;

        // 2D
        if (Decimal_result != Convert.ToDecimal(long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (Decimal_result != Convert.ToDecimal(long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToDouble
        double Double_result = -1;

        // 2D
        if (Double_result != Convert.ToDouble(long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (Double_result != Convert.ToDouble(long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToSingle tests
        float Single_result = -1;

        // 2D
        if (Single_result != Convert.ToSingle(long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (Single_result != Convert.ToSingle(long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToInt32 tests
        int Int32_result = -1;

        // 2D
        if (Int32_result != Convert.ToInt32(long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (Int32_result != Convert.ToInt32(long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToSByte tests
        sbyte SByte_result = -1;

        // 2D
        if (SByte_result != Convert.ToSByte(long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (SByte_result != Convert.ToSByte(long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToInt16 tests
        short Int16_result = -1;

        // 2D
        if (Int16_result != Convert.ToInt16(long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.long2darr[0, 1] is: {0}", vt1.long2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.long2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.long2darr[0, 1] is: {0}", cl1.long2darr[0, 1]);
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
        if (Int16_result != Convert.ToInt16(long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("long3darr[1,0,1] is: {0}", long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.long3darr[1,0,1] is: {0}", vt1.long3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.long3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.long3darr[1,0,1] is: {0}", cl1.long3darr[1, 0, 1]);
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

        //Int64ToUInt32 tests
        uint UInt32_result = 1;

        // 2D
        if (UInt32_result != Convert.ToUInt32(long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.long2darr_b[0, 1] is: {0}", vt1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.long2darr_b[0, 1] is: {0}", cl1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(ja1_b[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ja1_b[0][0, 1] is: {0}", ja1_b[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt32_result != Convert.ToUInt32(long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("long3darr_b[1,0,1] is: {0}", long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.long3darr_b[1,0,1] is: {0}", vt1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.long3darr_b[1,0,1] is: {0}", cl1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(ja2_b[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ja2_b[1][1,0,1] is: {0}", ja2_b[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //Int64ToUInt64 tests
        ulong UInt64_result = 1;

        // 2D
        if (UInt64_result != Convert.ToUInt64(long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.long2darr_b[0, 1] is: {0}", vt1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.long2darr_b[0, 1] is: {0}", cl1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(ja1_b[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("ja1_b[0][0, 1] is: {0}", ja1_b[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt64_result != Convert.ToUInt64(long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("long3darr_b[1,0,1] is: {0}", long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.long3darr_b[1,0,1] is: {0}", vt1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.long3darr_b[1,0,1] is: {0}", cl1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(ja2_b[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("ja2_b[1][1,0,1] is: {0}", ja2_b[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //Int64ToUInt16 tests
        ushort UInt16_result = 1;

        // 2D
        if (UInt16_result != Convert.ToUInt16(long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.long2darr_b[0, 1] is: {0}", vt1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.long2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.long2darr_b[0, 1] is: {0}", cl1.long2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(ja1_b[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("ja1_b[0][0, 1] is: {0}", ja1_b[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt16_result != Convert.ToUInt16(long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("long3darr_b[1,0,1] is: {0}", long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.long3darr_b[1,0,1] is: {0}", vt1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.long3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.long3darr_b[1,0,1] is: {0}", cl1.long3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(ja2_b[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("ja2_b[1][1,0,1] is: {0}", ja2_b[1][1, 0, 1]);
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
