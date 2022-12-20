// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct VT
{
    public int[,] int2darr;
    public int[, ,] int3darr;
    public int[,] int2darr_b;
    public int[, ,] int3darr_b;
    public int[,] int2darr_c;
    public int[, ,] int3darr_c;
}

public class CL
{
    public int[,] int2darr = { { 0, -1 }, { 0, 0 } };
    public int[, ,] int3darr = { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
    public int[,] int2darr_b = { { 0, 1 }, { 0, 0 } };
    public int[, ,] int3darr_b = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    public int[,] int2darr_c = { { 0, 49 }, { 0, 0 } };
    public int[, ,] int3darr_c = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };
}

public class intMDArrTest
{

    static int[,] int2darr = { { 0, -1 }, { 0, 0 } };
    static int[, ,] int3darr = { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
    static int[][,] ja1 = new int[2][,];
    static int[][, ,] ja2 = new int[2][, ,];
    static int[,] int2darr_b = { { 0, 1 }, { 0, 0 } };
    static int[, ,] int3darr_b = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    static int[][,] ja1_b = new int[2][,];
    static int[][, ,] ja2_b = new int[2][, ,];
    static int[,] int2darr_c = { { 0, 49 }, { 0, 0 } };
    static int[, ,] int3darr_c = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };
    static int[][,] ja1_c = new int[2][,];
    static int[][, ,] ja2_c = new int[2][, ,];



    [Fact]
    public static int TestEntryPoint()
    {

        bool pass = true;

        VT vt1;
        vt1.int2darr = new int[,] { { 0, -1 }, { 0, 0 } };
        vt1.int3darr = new int[,,] { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
        vt1.int2darr_b = new int[,] { { 0, 1 }, { 0, 0 } };
        vt1.int3darr_b = new int[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        vt1.int2darr_c = new int[,] { { 0, 49 }, { 0, 0 } };
        vt1.int3darr_c = new int[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        CL cl1 = new CL();

        ja1[0] = new int[,] { { 0, -1 }, { 0, 0 } };
        ja2[1] = new int[,,] { { { 0, 0 } }, { { 0, -1 } }, { { 0, 0 } } };
        ja1_b[0] = new int[,] { { 0, 1 }, { 0, 0 } };
        ja2_b[1] = new int[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        ja1_c[0] = new int[,] { { 0, 49 }, { 0, 0 } };
        ja2_c[1] = new int[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        int result = -1;

        // 2D
        if (result != int2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.int2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.int2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (result != int3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.int3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.int3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToBool tests
        bool Bool_result = true;

        // 2D
        if (Bool_result != Convert.ToBoolean(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (Bool_result != Convert.ToBoolean(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToByte tests
        byte Byte_result = 1;

        // 2D
        if (Byte_result != Convert.ToByte(int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.int2darr_b[0, 1] is: {0}", vt1.int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.int2darr_b[0, 1] is: {0}", cl1.int2darr_b[0, 1]);
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
        if (Byte_result != Convert.ToByte(int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("int3darr_b[1,0,1] is: {0}", int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.int3darr_b[1,0,1] is: {0}", vt1.int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.int3darr_b[1,0,1] is: {0}", cl1.int3darr_b[1, 0, 1]);
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

        //Int32ToDecimal tests
        decimal Decimal_result = -1;

        // 2D
        if (Decimal_result != Convert.ToDecimal(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (Decimal_result != Convert.ToDecimal(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToDouble
        double Double_result = -1;

        // 2D
        if (Double_result != Convert.ToDouble(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (Double_result != Convert.ToDouble(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToSingle
        float Single_result = -1;

        // 2D
        if (Single_result != Convert.ToSingle(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (Single_result != Convert.ToSingle(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToInt64 tests
        long Int64_result = -1;

        // 2D
        if (Int64_result != Convert.ToInt64(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (Int64_result != Convert.ToInt64(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToSByte tests
        sbyte SByte_result = -1;

        // 2D
        if (SByte_result != Convert.ToSByte(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
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
        if (SByte_result != Convert.ToSByte(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
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

        //Int32ToInt16 tests
        short Int16_result = -1;

        // 2D
        if (Int16_result != Convert.ToInt32(int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt32(vt1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.int2darr[0, 1] is: {0}", vt1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt32(cl1.int2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.int2darr[0, 1] is: {0}", cl1.int2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt32(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ja1[0][0, 1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int16_result != Convert.ToInt32(int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("int3darr[1,0,1] is: {0}", int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt32(vt1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.int3darr[1,0,1] is: {0}", vt1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt32(cl1.int3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.int3darr[1,0,1] is: {0}", cl1.int3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt32(ja2[1][1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ja2[1][1,0,1] is: {0}", ja2[1][1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        //Int32ToUInt32 tests
        uint UInt32_result = 1;

        // 2D
        if (UInt32_result != Convert.ToUInt32(int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.int2darr_b[0, 1] is: {0}", vt1.int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.int2darr_b[0, 1] is: {0}", cl1.int2darr_b[0, 1]);
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
        if (UInt32_result != Convert.ToUInt32(int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("int3darr_b[1,0,1] is: {0}", int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.int3darr_b[1,0,1] is: {0}", vt1.int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.int3darr_b[1,0,1] is: {0}", cl1.int3darr_b[1, 0, 1]);
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

        //Int32ToUInt64 tests
        ulong UInt64_result = 1;

        // 2D
        if (UInt64_result != Convert.ToUInt64(int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.int2darr_b[0, 1] is: {0}", vt1.int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.int2darr_b[0, 1] is: {0}", cl1.int2darr_b[0, 1]);
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
        if (UInt64_result != Convert.ToUInt64(int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("int3darr_b[1,0,1] is: {0}", int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.int3darr_b[1,0,1] is: {0}", vt1.int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.int3darr_b[1,0,1] is: {0}", cl1.int3darr_b[1, 0, 1]);
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

        //Int32ToUInt16 tests
        ushort UInt16_result = 1;

        // 2D
        if (UInt16_result != Convert.ToUInt16(int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.int2darr_b[0, 1] is: {0}", vt1.int2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.int2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.int2darr_b[0, 1] is: {0}", cl1.int2darr_b[0, 1]);
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
        if (UInt16_result != Convert.ToUInt16(int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("int3darr_b[1,0,1] is: {0}", int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.int3darr_b[1,0,1] is: {0}", vt1.int3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.int3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.int3darr_b[1,0,1] is: {0}", cl1.int3darr_b[1, 0, 1]);
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

        //Int32ToChar tests
        char Char_result = '1';

        // 2D
        if (Char_result != Convert.ToChar(int2darr_c[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("2darr[0, 1] is: {0}", int2darr_c[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.int2darr_c[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.int2darr_c[0, 1] is: {0}", vt1.int2darr_c[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.int2darr_c[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.int2darr_c[0, 1] is: {0}", cl1.int2darr_c[0, 1]);
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
        if (Char_result != Convert.ToChar(int3darr_c[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("int3darr_c[1,0,1] is: {0}", int3darr_c[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.int3darr_c[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.int3darr_c[1,0,1] is: {0}", vt1.int3darr_c[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.int3darr_c[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.int3darr_c[1,0,1] is: {0}", cl1.int3darr_c[1, 0, 1]);
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
