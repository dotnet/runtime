// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct VT
{
    public byte[,] byte2darr;
    public byte[, ,] byte3darr;
    public byte[,] byte2darr_b;
    public byte[, ,] byte3darr_b;
}

public class CL
{
    public byte[,] byte2darr = { { 0, 1 }, { 0, 0 } };
    public byte[, ,] byte3darr = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    public byte[,] byte2darr_b = { { 0, 49 }, { 0, 0 } };
    public byte[, ,] byte3darr_b = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };
}

public class byteMDArrTest
{

    static byte[,] byte2darr = { { 0, 1 }, { 0, 0 } };
    static byte[, ,] byte3darr = { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
    static byte[,] byte2darr_b = { { 0, 49 }, { 0, 0 } };
    static byte[, ,] byte3darr_b = { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };


    static byte[][,] ja1 = new byte[2][,];
    static byte[][, ,] ja2 = new byte[2][, ,];
    static byte[][,] ja1_b = new byte[2][,];
    static byte[][, ,] ja2_b = new byte[2][, ,];

    [Fact]
    public static int TestEntryPoint()
    {

        bool pass = true;

        VT vt1;
        vt1.byte2darr = new byte[,] { { 0, 1 }, { 0, 0 } };
        vt1.byte3darr = new byte[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        vt1.byte2darr_b = new byte[,] { { 0, 49 }, { 0, 0 } };
        vt1.byte3darr_b = new byte[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        CL cl1 = new CL();

        ja1[0] = new byte[,] { { 0, 1 }, { 0, 0 } };
        ja2[1] = new byte[,,] { { { 0, 0 } }, { { 0, 1 } }, { { 0, 0 } } };
        ja1_b[0] = new byte[,] { { 0, 49 }, { 0, 0 } };
        ja2_b[1] = new byte[,,] { { { 0, 0 } }, { { 0, 49 } }, { { 0, 0 } } };

        byte result = 1;

        // 2D
        if (result != byte2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.byte2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.byte2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (result != byte3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.byte3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.byte3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToBool tests
        bool Bool_result = true;

        // 2D
        if (Bool_result != Convert.ToBoolean(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Bool_result != Convert.ToBoolean(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Bool_result != Convert.ToBoolean(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Bool_result is: {0}", Bool_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToDecimal tests
        decimal Decimal_result = 1;

        // 2D
        if (Decimal_result != Convert.ToDecimal(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Decimal_result != Convert.ToDecimal(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Decimal_result != Convert.ToDecimal(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Decimal_result is: {0}", Decimal_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToDouble
        double Double_result = 1;

        // 2D
        if (Double_result != Convert.ToDouble(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Double_result != Convert.ToDouble(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Double_result != Convert.ToDouble(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Double_result is: {0}", Double_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToSingle tests
        Single Single_result = 1;

        // 2D
        if (Single_result != Convert.ToSingle(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Single_result != Convert.ToSingle(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Single_result != Convert.ToSingle(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Single_result is: {0}", Single_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToInt32 tests
        int Int32_result = 1;

        // 2D
        if (Int32_result != Convert.ToInt32(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Int32_result != Convert.ToInt32(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToInt64 tests
        long Int64_result = 1;

        // 2D
        if (Int64_result != Convert.ToInt64(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Int64_result != Convert.ToInt64(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToSByte tests
        sbyte SByte_result = 1;

        // 2D
        if (SByte_result != Convert.ToSByte(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (SByte_result != Convert.ToSByte(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToInt16 tests
        short Int16_result = 1;

        // 2D
        if (Int16_result != Convert.ToInt16(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (Int16_result != Convert.ToInt16(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToUInt32
        uint UInt32_result = 1;

        // 2D
        if (UInt32_result != Convert.ToUInt32(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (UInt32_result != Convert.ToUInt32(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToUInt64
        ulong UInt64_result = 1;

        // 2D
        if (UInt64_result != Convert.ToUInt64(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (UInt64_result != Convert.ToUInt64(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToUInt16 tests
        short UInt16_result = 1;

        // 2D
        if (UInt16_result != Convert.ToUInt16(byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.byte2darr[0, 1] is: {0}", vt1.byte2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.byte2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.byte2darr[0, 1] is: {0}", cl1.byte2darr[0, 1]);
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
        if (UInt16_result != Convert.ToUInt16(byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("byte3darr[1,0,1] is: {0}", byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.byte3darr[1,0,1] is: {0}", vt1.byte3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.byte3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.byte3darr[1,0,1] is: {0}", cl1.byte3darr[1, 0, 1]);
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

        //ByteToChar tests
        char Char_result = '1';

        // 2D
        if (Char_result != Convert.ToChar(byte2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("2darr[0, 1] is: {0}", byte2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.byte2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.byte2darr_b[0, 1] is: {0}", vt1.byte2darr_b[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.byte2darr_b[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.byte2darr_b[0, 1] is: {0}", cl1.byte2darr_b[0, 1]);
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
        if (Char_result != Convert.ToChar(byte3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("byte3darr_b[1,0,1] is: {0}", byte3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(vt1.byte3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("vt1.byte3darr_b[1,0,1] is: {0}", vt1.byte3darr_b[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Char_result != Convert.ToChar(cl1.byte3darr_b[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Char_result is: {0}", Char_result);
            Console.WriteLine("cl1.byte3darr_b[1,0,1] is: {0}", cl1.byte3darr_b[1, 0, 1]);
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
