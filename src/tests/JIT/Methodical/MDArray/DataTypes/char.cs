// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct VT
{
    public char[,] char2darr;
    public char[, ,] char3darr;
}

public class CL
{
    public char[,] char2darr = { { '0', '1' }, { '0', '0' } };
    public char[, ,] char3darr = { { { '0', '0' } }, { { '0', '1' } }, { { '0', '0' } } };
}

public class charMDArrTest
{

    static char[,] char2darr = { { '0', '1' }, { '0', '0' } };
    static char[, ,] char3darr = { { { '0', '0' } }, { { '0', '1' } }, { { '0', '0' } } };

    static char[][,] ja1 = new char[2][,];
    static char[][, ,] ja2 = new char[2][, ,];

    [Fact]
    public static int TestEntryPoint()
    {

        bool pass = true;

        VT vt1;
        vt1.char2darr = new char[,] { { '0', '1' }, { '0', '0' } };
        vt1.char3darr = new char[,,] { { { '0', '0' } }, { { '0', '1' } }, { { '0', '0' } } };

        CL cl1 = new CL();

        ja1[0] = new char[,] { { '0', '1' }, { '0', '0' } };
        ja2[1] = new char[,,] { { { '0', '0' } }, { { '0', '1' } }, { { '0', '0' } } };

        char result = '1';

        // 2D
        if (result != char2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.char2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.char2darr[0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != ja1[0][0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (result != char3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != vt1.char3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (result != cl1.char3darr[1, 0, 1])
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("result is: {0}", result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToByte tests
        byte Byte_result = 49;

        // 2D
        if (Byte_result != Convert.ToByte(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Byte_result != Convert.ToByte(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Byte_result != Convert.ToByte(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Byte_result is: {0}", Byte_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToInt32 tests
        int Int32_result = 49;

        // 2D
        if (Int32_result != Convert.ToInt32(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int32_result != Convert.ToInt32(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int32_result != Convert.ToInt32(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int32_result is: {0}", Int32_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToInt64 tests
        long Int64_result = 49;

        // 2D
        if (Int64_result != Convert.ToInt64(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int64_result != Convert.ToInt64(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int64_result != Convert.ToInt64(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int64_result is: {0}", Int64_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToSByte tests
        sbyte SByte_result = 49;

        // 2D
        if (SByte_result != Convert.ToSByte(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (SByte_result != Convert.ToSByte(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (SByte_result != Convert.ToSByte(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("SByte_result is: {0}", SByte_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToInt16 tests
        short Int16_result = 49;

        // 2D
        if (Int16_result != Convert.ToInt16(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (Int16_result != Convert.ToInt16(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (Int16_result != Convert.ToInt16(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("Int16_result is: {0}", Int16_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToUInt32
        uint UInt32_result = 49;

        // 2D
        if (UInt32_result != Convert.ToUInt32(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt32_result != Convert.ToUInt32(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt32_result != Convert.ToUInt32(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt32_result is: {0}", UInt32_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToUInt64
        ulong UInt64_result = 49;

        // 2D
        if (UInt64_result != Convert.ToUInt64(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt64_result != Convert.ToUInt64(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt64_result != Convert.ToUInt64(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt64_result is: {0}", UInt64_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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

        //CharToUInt16 tests
        ushort UInt16_result = 49;

        // 2D
        if (UInt16_result != Convert.ToUInt16(char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("2darr[0,1] is: {0}", char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.char2darr[0,1] is: {0}", vt1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.char2darr[0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.char2darr[0,1] is: {0}", cl1.char2darr[0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(ja1[0][0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("ja1[0][0,1] is: {0}", ja1[0][0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        // 3D
        if (UInt16_result != Convert.ToUInt16(char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("char3darr[1,0,1] is: {0}", char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(vt1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("vt1.char3darr[1,0,1] is: {0}", vt1.char3darr[1, 0, 1]);
            Console.WriteLine("and they are NOT equal !");
            Console.WriteLine();
            pass = false;
        }

        if (UInt16_result != Convert.ToUInt16(cl1.char3darr[1, 0, 1]))
        {
            Console.WriteLine("ERROR:");
            Console.WriteLine("UInt16_result is: {0}", UInt16_result);
            Console.WriteLine("cl1.char3darr[1,0,1] is: {0}", cl1.char3darr[1, 0, 1]);
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
