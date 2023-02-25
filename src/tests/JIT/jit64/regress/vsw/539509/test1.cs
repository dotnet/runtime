// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

////////////////////////////////////////////////////////////////
//
// Description 
// ____________
// On IA64 if the only use of a parameter is assigning to it 
// inside and EH clause and the parameter is a GC pointer, then 
// the JIT reports a stack slot as live for the duration of the method,
// but never initializes it.  Sort of the inverse of a GC hole.  
// Thus the runtime sees random stack garbage as a GC pointer and 
// bad things happen.  Workarounds include using the parameter, 
// assinging to a different local, removing the assignment (since 
// it has no subsequent uses).
//
//
// Right Behavior
// ________________
// No Assertion
//
// Wrong Behavior
// ________________
// Assertion
//
// Commands to issue
// __________________
// > test1.exe
//
// External files 
// _______________
// None
////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

class ApplicationException : Exception { }

#pragma warning disable 1917,1918
public enum TestEnum
{
    red = 1,
    green = 2,
    blue = 4,
}

public class AA<TA, TB, TC, TD, TE, TF>
     where TA : IComparable where TB : IComparable where TC : IComparable where TD : IComparable where TE : IComparable

{
    public TC m_aguiGeneric1;
    public short[][][] Method1(uint[,,,] param1, ref TestEnum param2)
    {
        uint local1 = ((uint)(((ulong)(17.0f))));
        double local2 = ((double)(((ulong)(113.0f))));
        String[] local3 = new String[] { "113", "92", "26", "24" };
        while (Convert.ToBoolean(((short)(local1))))
        {
            local3[23] = "69";
            do
            {
                char[][] local4 = new char[][]{(new char[48u]), new char[]{'\x3f', '\x00',
                    '\x47' }, new char[]{'\x58', '\x39', '\x70', '\x31' }, (new char[48u]),
                    new char[]{'\x62', '\x6b', '\x19', '\x30', '\x17' } };
                local3 = ((String[])(((Array)(null))));
                while ((((short)(local2)) == ((short)(local2))))
                {
                    for (App.m_byFwd1 = App.m_byFwd1; ((bool)(((object)(new BB())))); local1--)
                    {
                        do
                        {
                            while (Convert.ToBoolean((local1 >> 100)))
                            {
                                local2 = local2;
                            }
                            while ((new AA<TA, TB, TC, TD, TE, TF>() == new
                                AA<TA, TB, TC, TD, TE, TF>()))
                            {
                                if (((bool)(((object)(new AA<TA, TB, TC, TD, TE, TF>())))))
                                    param1 = (new uint[local1, 107u, 22u, local1]);
                                if (((bool)(((object)(param2)))))
                                    continue;
                                if (App.m_bFwd2)
                                    continue;
                                if ((/*2 REFS*/((byte)(local1)) != /*2 REFS*/((byte)(local1))))
                                {
                                    throw new ApplicationException();
                                }
                            }
                            local1 -= 88u;
                            while (((bool)(((object)(local2)))))
                            {
                            }
                            if (Convert.ToBoolean(((int)(local2))))
                                do
                                {
                                }
                                while (App.m_bFwd2);
                            else
                            {
                            }
                        }
                        while ((null != new AA<TA, TB, TC, TD, TE, TF>()));
                        local4 = (local4 = (local4 = new char[][]{(new char[local1]), (new char[
                            local1]), (new char[113u]) }));
                        do
                        {
                        }
                        while (Convert.ToBoolean(local2));
                        for (App.m_byFwd1 = ((byte)(local1)); ((bool)(((object)(local1)))); local2
                            -= (local2 + local2))
                        {
                        }
                        while (Convert.ToBoolean(((short)(local1))))
                        {
                        }
                    }
                    if (((bool)(((object)(new BB())))))
                    {
                    }
                    else
                        for (App.m_iFwd3 -= 33; ((bool)(((object)(local2)))); App.m_bFwd2 = App.
                            m_bFwd2)
                        {
                        }
                }
                for (App.m_iFwd3 /= (Convert.ToByte(33.0) ^ ((byte)(local1))); App.m_bFwd2;
                    App.m_shFwd4 = ((short)(((sbyte)(local2)))))
                {
                }
                while (App.m_bFwd2)
                {
                }
                break;
            }
            while ((/*2 REFS*/((object)(new BB())) != ((AA<TA, TB, TC, TD, TE, TF>)(
                /*2 REFS*/((object)(new BB()))))));
            for (App.m_iFwd3 = 60; ((bool)(((object)(new BB())))); local2 = local2)
            {
            }
            local3 = ((String[])(((object)(local2))));
        }
        local3[((int)(((byte)(65))))] = "47";
        try
        {
        }
        catch (IndexOutOfRangeException)
        {
        }
        return new short[][][]{/*2 REFS*/(new short[36u][]), new short[][]{ },
            /*2 REFS*/
                      (new short[36u][]) };
    }
    public static ulong Static1(TF param1)
    {
        byte local5 = ((byte)(((long)(69.0))));
        float[,][,] local6 = (new float[9u, 6u][,]);
        TestEnum local7 = TestEnum.blue;
        do
        {
            bool[,,,,][,] local8 = (new bool[81u, 98u, ((uint)(58.0f)), ((uint)(36.0f)),
                74u*4u][,]);
            while ((((uint)(local5)) != 4u))
            {
                if (Convert.ToBoolean((local5 + local5)))
                    local6 = (new float[((uint)(116.0)), 94u][,]);
                else
                    for (App.m_iFwd3 -= 97; Convert.ToBoolean(((ushort)(local5))); App.m_ushFwd5
                        = Math.Max(((ushort)(26)), ((ushort)(43))))
                    {
                        local7 = local7;
                    }
            }
            local8[69, 1, 61, 62, 122][24, 40] = true;
            local8[97, (((short)(115)) >> ((ushort)(local5))), 29, 29, ((int)(((ulong)(
                local5))))][((int)(((long)(119u)))), 52] = false;
            try
            {
                param1 = param1;
                param1 = param1;
                while ((/*2 REFS*/((sbyte)(local5)) == /*2 REFS*/((sbyte)(local5))))
                {
                    try
                    {
                        throw new IndexOutOfRangeException();
                    }
                    catch (InvalidOperationException)
                    {
                        try
                        {
                            while (((bool)(((object)(local7)))))
                            {
                                return ((ulong)(((int)(7u))));
                            }
                            while ((new AA<TA, TB, TC, TD, TE, TF>() == new
                                AA<TA, TB, TC, TD, TE, TF>()))
                            {
                                local7 = local7;
                                local5 = (local5 += local5);
                            }
                            while (((bool)(((object)(local5)))))
                            {
                            }
                            goto label1;
                        }
                        catch (InvalidOperationException)
                        {
                        }
                        do
                        {
                        }
                        while ((new AA<TA, TB, TC, TD, TE, TF>() == new AA<TA, TB, TC, TD, TE, TF>(
                            )));
                    label1:
                        try
                        {
                        }
                        catch (Exception)
                        {
                        }
                    }
                    for (App.m_fFwd6 = App.m_fFwd6; ((bool)(((object)(new BB())))); App.m_dblFwd7
                        /= 94.0)
                    {
                    }
                    for (App.m_shFwd4--; App.m_bFwd2; App.m_ulFwd8 = ((ulong)(((ushort)(local5))
                        )))
                    {
                    }
                    local7 = local7;
                    local8[((int)(Convert.ToUInt64(26.0))), 60, ((int)(((long)(local5)))), ((
                        int)(local5)), 96] = (new bool[((uint)(48.0)), 97u]);
                }
                param1 = (param1 = param1);
            }
            finally
            {
            }
            local8 = local8;
        }
        while (((bool)(((object)(local7)))));
        if ((local5 == (local5 -= local5)))
            while ((((Array)(null)) != ((object)(local7))))
            {
            }
        else
        {
        }
        for (App.m_dblFwd7++; App.m_bFwd2; App.m_chFwd9 += '\x69')
        {
        }
        return ((ulong)(105));
    }
    public static char[] Static2(ulong param1, short param2, ref uint param3, ref
        TA param4)
    {
        long[,,,,][,,][][,,,] local9 = (new long[((uint)(5.0)), 24u, 65u, 9u, 29u]
            [,,][][,,,]);
        char local10 = ((char)(97));
        double local11 = 102.0;
        sbyte[,][,,,][] local12 = (new sbyte[41u, 15u][,,,][]);
        try
        {
            local12[26, 65] = ((sbyte[,,,][])(((object)(new AA<TA, TB, TC, TD, TE, TF>()
                ))));
            try
            {
                do
                {
                    do
                    {
                        do
                        {
                            try
                            {
                                do
                                {
                                    try
                                    {
                                        local11 *= 27.0;
                                        try
                                        {
                                            if (Convert.ToBoolean(((ushort)(param1))))
                                                for (App.m_ushFwd5 /= ((ushort)(17.0f)); (new
                                                    AA<TA, TB, TC, TD, TE, TF>() != new AA<TA, TB, TC, TD, TE, TF>());
                                                    App.m_ushFwd5 *= ((ushort)(((sbyte)(param1)))))
                                                {
                                                }
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                        }
                                        do
                                        {
                                        }
                                        while (((bool)(((object)(param1)))));
                                    }
                                    catch (InvalidOperationException)
                                    {
                                    }
                                }
                                while (("95" == Convert.ToString(local10)));
                                local11 -= ((double)(30));
                                while (((bool)(((object)(local10)))))
                                {
                                }
                            }
                            catch (NullReferenceException)
                            {
                            }
                            try
                            {
                            }
                            catch (InvalidOperationException)
                            {
                            }
                            param3 /= ((param3 /= param3) / param3);
                        }
                        while ((((long)(param2)) != (55 | param3)));
                        local10 = ((char)(((object)(local10))));
                        param1 *= ((ulong)(((ushort)(54u))));
                        try
                        {
                        }
                        catch (ApplicationException)
                        {
                        }
                        param4 = (param4 = param4);
                    }
                    while ((param2 == param2));
                    do
                    {
                    }
                    while (((bool)(((object)(new AA<TA, TB, TC, TD, TE, TF>())))));
                    throw new DivideByZeroException();
                }
                while ((param3 == (65u / param3)));
                do
                {
                }
                while ((((sbyte)(local11)) == ((sbyte)(local11))));
                local12[116, ((int)((param2 *= param2)))] = (new sbyte[((uint)(param2)), (
                    param3 += param3), 67u, 116u][]);
                try
                {
                }
                finally
                {
                }
            }
            finally
            {
            }
            for (App.m_lFwd10 = (60 * param3); ((bool)(((object)(local10)))); local11--)
            {
            }
            local12 = (local12 = (local12 = local12));
        }
        catch (IndexOutOfRangeException)
        {
        }
        local9 = local9;
        param1 *= (param1 >> ((ushort)(30)));
        return new char[] { (local10 = local10), local10, (local10 = local10), '\x7e' };
    }
    public static sbyte[][][,,,,][][,,] Static3(TestEnum param1, short param2)
    {
        param1 = param1;
        do
        {
            sbyte local13 = ((sbyte)(89.0));
            double local14 = 103.0;
            uint[,][][,,][,] local15 = (new uint[92u, 102u][][,,][,]);
            short[][,,,][,][] local16 = (new short[32u][,,,][,][]);
            local15[((int)(((float)(69.0)))), 9][((int)(((ushort)(75.0f))))][((int)(66u))
                , (((byte)(local13)) ^ ((byte)(param2))), ((local13 << local13) << ((ushort
                )(local13)))][((int)(63u)), ((int)(((char)(8))))] *= 82u;
            param1 = (param1 = param1);
        }
        while (((bool)(((object)(param1)))));
        param1 = param1;
        param2 = (param2 /= (param2 = param2));
        return (new sbyte[36u][][,,,,][][,,]);
    }
    public static long[][,,] Static4(char param1)
    {
        sbyte[][] local17 = ((sbyte[][])(((Array)(null))));
        ulong[,,] local18 = ((ulong[,,])(((Array)(null))));
        sbyte[][] local19 = new sbyte[][] { (new sbyte[16u]), (new sbyte[126u]) };
        byte local20 = ((byte)(((sbyte)(90u))));
        return (new long[15u][,,]);
    }
    public static int Static5(ref TE param1, ref char[][,,,] param2, Array param3,
        ref ulong[,,,,] param4, ref long[,,,][][][][,] param5)
    {
        BB[] local21 = ((BB[])(((Array)(null))));
        sbyte local22 = ((sbyte)(121));
        bool local23 = (new AA<TA, TB, TC, TD, TE, TF>() == new
            AA<TA, TB, TC, TD, TE, TF>());
        object[][,,][][,,][,] local24 = (new object[115u][,,][][,,][,]);
        while (local23)
        {
            param1 = param1;
            while (local23)
            {
                try
                {
                    local23 = false;
                }
                catch (ApplicationException)
                {
                    param2[1] = (new char[57u, ((uint)(68)), 104u, ((uint)(local22))]);
                    try
                    {
                        local22 = local22;
                        do
                        {
                            do
                            {
                                local21[((int)(((long)(102u))))].m_achField1[((int)(local22))] = ((
                                    char[,])(((object)(new BB()))));
                                param3 = ((Array)(null));
                                throw new IndexOutOfRangeException();
                            }
                            while (local23);
                            param3 = ((Array)(null));
                            local22 = local22;
                            local22 = (local22 *= local22);
                            while ((local23 && (null != new AA<TA, TB, TC, TD, TE, TF>())))
                            {
                                for (local22 = local22; local23; App.m_abyFwd11 = App.m_abyFwd11)
                                {
                                    while (local23)
                                    {
                                    }
                                }
                                local22 = local22;
                            }
                        }
                        while ((/*3 REFS*/((uint)(local22)) != (local23 ?/*3 REFS*/((uint)(local22))
                            :/*3 REFS*/((uint)(local22)))));
                        local21[38].m_achField1 = new char[][,]{((char[,])(param3)), (new char[
                            102u, 36u]) };
                    }
                    catch (DivideByZeroException)
                    {
                    }
                    local21 = local21;
                }
                try
                {
                }
                catch (Exception)
                {
                }
                throw new InvalidOperationException();
            }
            try
            {
            }
            catch (ApplicationException)
            {
            }
            for (App.m_uFwd12--; local23; App.m_lFwd10 /= ((long)(((short)(28u)))))
            {
            }
        }
        param5 = (new long[108u, 115u, 20u, 126u][][][][,]);
        local21[(((ushort)(local22)) << ((int)(local22)))].m_achField1[101] = (new
            char[21u, 43u]);
        for (App.m_shFwd4 = ((short)(76.0f)); ((bool)(((object)(local23)))); App.
            m_chFwd9 *= '\x67')
        {
        }
        if (local23)
            try
            {
            }
            catch (InvalidOperationException)
            {
            }
        else
            while (local23)
            {
            }
        return 83;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct BB
{
    public char[][,] m_achField1;
    public void Method1(ref uint[][][,] param1, ref String[][] param2, ref char[,]
        param3, AA<sbyte, byte, uint, uint, long, bool> param4, ref
        AA<sbyte, byte, uint, uint, long, bool> param5, int param6)
    {
        do
        {
            ushort[] local25 = (new ushort[62u]);
            do
            {
                BB local26 = ((BB)(((object)(new AA<sbyte, byte, uint, uint, long, bool>()))
                    ));
                param4.m_aguiGeneric1 = new AA<sbyte, byte, uint, uint, long, bool>().
                    m_aguiGeneric1;
                try
                {
                    ulong[,,][] local27 = ((ulong[,,][])(((Array)(null))));
                    ushort[,] local28 = (new ushort[8u, 8u]);
                    if ((/*2 REFS*/((short)(param6)) == /*2 REFS*/((short)(param6))))
                        while (App.m_bFwd2)
                        {
                            for (App.m_ushFwd5--; App.m_bFwd2; App.m_ulFwd8--)
                            {
                                param1 = param1;
                            }
                            AA<sbyte, byte, uint, uint, long, bool>.Static3(
                                TestEnum.blue,
                                App.m_shFwd4);
                            param1[(5 ^ param6)][param6] = (new uint[2u, ((uint)(param6))]);
                        }
                    else
                        local28[param6, (((ushort)(param6)) << ((sbyte)(47)))] += ((ushort)(((
                            ulong)(25u))));
                    while (((bool)(((object)(param4)))))
                    {
                        AA<sbyte, byte, uint, uint, long, bool>.Static2(
                            ((ulong)(114.0)),
                            ((short)(((long)(49.0f)))),
                            ref App.m_uFwd12,
                            ref App.m_gsbFwd13);
                        try
                        {
                            if ((null == new AA<sbyte, byte, uint, uint, long, bool>()))
                                if ((((char)(25)) != ((char)(param6))))
                                    if (App.m_bFwd2)
                                        try
                                        {
                                            param6 /= param6;
                                            while ((((long)(44u)) != ((long)(param6))))
                                            {
                                                try
                                                {
                                                }
                                                catch (InvalidOperationException)
                                                {
                                                }
                                                do
                                                {
                                                }
                                                while (App.m_bFwd2);
                                                local25 = local25;
                                                for (App.m_shFwd4 -= App.m_shFwd4; Convert.ToBoolean(param6); App.
                                                    m_byFwd1 *= Math.Max(((byte)(9u)), ((byte)(40u))))
                                                {
                                                }
                                            }
                                            local25[12] = App.m_ushFwd5;
                                            local28 = (new ushort[111u, 80u]);
                                            for (App.m_dblFwd7 = App.m_dblFwd7; (param6 == ((int)(101.0))); param6
                                                *= param6)
                                            {
                                            }
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                        }
                            param1 = param1;
                            param2[param6] = ((String[])(((Array)(null))));
                            try
                            {
                            }
                            catch (ApplicationException)
                            {
                            }
                        }
                        finally
                        {
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            while (App.m_bFwd2);
            for (App.m_xFwd14 = App.m_xFwd14; ((param6 - (0.0f)) == 86.0f); App.m_fFwd6 += (
                108u - ((float)(param6))))
            {
            }
            if ((((object)(new AA<sbyte, byte, uint, uint, long, bool>())) == "32"))
                param5.m_aguiGeneric1 = new AA<sbyte, byte, uint, uint, long, bool>().
                    m_aguiGeneric1;
            else
                do
                {
                }
                while (((bool)(((object)(new AA<sbyte, byte, uint, uint, long, bool>())))));
            if (App.m_bFwd2)
            {
            }
        }
        while (Convert.ToBoolean(param6));
        param5.m_aguiGeneric1 = (param4 = param4).m_aguiGeneric1;
        do
        {
        }
        while (Convert.ToBoolean(param6));
        ;
    }
}

public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing AA::Method1");
            ((AA<sbyte, byte, uint, uint, long, bool>)(((object)(new BB())))).Method1(
                (new uint[12u, 115u, 95u, 13u]),
                ref App.m_xFwd15);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static1");
            AA<sbyte, byte, uint, uint, long, bool>.Static1(App.m_agboFwd16);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static2");
            AA<sbyte, byte, uint, uint, long, bool>.Static2(
                ((ulong)(((ushort)(10.0)))),
                ((short)(70.0)),
                ref App.m_uFwd12,
                ref App.m_gsbFwd13);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static3");
            AA<sbyte, byte, uint, uint, long, bool>.Static3(
                TestEnum.green,
                ((short)(((sbyte)(69.0)))));
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static4");
            AA<sbyte, byte, uint, uint, long, bool>.Static4('\x02');
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing AA::Static5");
            AA<sbyte, byte, uint, uint, long, bool>.Static5(
                ref App.m_aglFwd17,
                ref App.m_achFwd18,
                ((Array)(null)),
                ref App.m_aulFwd19,
                ref App.m_alFwd20);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        try
        {
            Console.WriteLine("Testing BB::Method1");
            new BB().Method1(
                ref App.m_auFwd21,
                ref App.m_axFwd22,
                ref App.m_achFwd23,
                new AA<sbyte, byte, uint, uint, long, bool>(),
                ref App.m_axFwd24,
                87);
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
    public static byte m_byFwd1;
    public static bool m_bFwd2;
    public static int m_iFwd3;
    public static short m_shFwd4;
    public static ushort m_ushFwd5;
    public static float m_fFwd6;
    public static double m_dblFwd7;
    public static ulong m_ulFwd8;
    public static char m_chFwd9;
    public static long m_lFwd10;
    public static byte[] m_abyFwd11;
    public static uint m_uFwd12;
    public static sbyte m_gsbFwd13;
    public static Array m_xFwd14;
    public static TestEnum m_xFwd15;
    public static bool m_agboFwd16;
    public static long m_aglFwd17;
    public static char[][,,,] m_achFwd18;
    public static ulong[,,,,] m_aulFwd19;
    public static long[,,,][][][][,] m_alFwd20;
    public static uint[][][,] m_auFwd21;
    public static String[][] m_axFwd22;
    public static char[,] m_achFwd23;
    public static AA<sbyte, byte, uint, uint, long, bool> m_axFwd24;
}
