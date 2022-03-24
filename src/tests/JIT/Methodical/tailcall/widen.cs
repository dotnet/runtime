using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program_widen
{
    // Random field we assign some bogus values to to trick the inliner below.
    // We cannot use NoInlining as the runtime disables tailcalls from such functions.
    private static int s_val;

    [Fact]
    public static int TestEntrypoint()
    {
        bool result = true;
        Console.Write("Test1U1S: ");
        if (Test1U1S(123) == (byte)Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1U2U: ");
        if (Test1U2U(123) == (byte)Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1U2S: ");
        if (Test1U2S(123) == (byte)Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1U4U: ");
        if (Test1U4U(123) == (byte)Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1U4S: ");
        if (Test1U4S(123) == (byte)Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1U8U: ");
        if (Test1U8U(123) == (byte)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1U8S: ");
        if (Test1U8S(123) == (byte)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S1U: ");
        if (Test1S1U(123) == (sbyte)Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S2U: ");
        if (Test1S2U(123) == (sbyte)Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S2S: ");
        if (Test1S2S(123) == (sbyte)Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S4U: ");
        if (Test1S4U(123) == (sbyte)Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S4S: ");
        if (Test1S4S(123) == (sbyte)Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S8U: ");
        if (Test1S8U(123) == (sbyte)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test1S8S: ");
        if (Test1S8S(123) == (sbyte)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U1U: ");
        if (Test2U1U(123) == Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U1S: ");
        if (Test2U1S(123) == (ushort)Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U2S: ");
        if (Test2U2S(123) == (ushort)Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U4U: ");
        if (Test2U4U(123) == (ushort)Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U4S: ");
        if (Test2U4S(123) == (ushort)Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U8U: ");
        if (Test2U8U(123) == (ushort)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2U8S: ");
        if (Test2U8S(123) == (ushort)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S1U: ");
        if (Test2S1U(123) == Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S1S: ");
        if (Test2S1S(123) == Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S2U: ");
        if (Test2S2U(123) == (short)Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S4U: ");
        if (Test2S4U(123) == (short)Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S4S: ");
        if (Test2S4S(123) == (short)Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S8U: ");
        if (Test2S8U(123) == (short)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test2S8S: ");
        if (Test2S8S(123) == (short)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U1U: ");
        if (Test4U1U(123) == Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U1S: ");
        if (Test4U1S(123) == (uint)Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U2U: ");
        if (Test4U2U(123) == Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U2S: ");
        if (Test4U2S(123) == (uint)Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U4S: ");
        if (Test4U4S(123) == (uint)Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U8U: ");
        if (Test4U8U(123) == (uint)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4U8S: ");
        if (Test4U8S(123) == (uint)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S1U: ");
        if (Test4S1U(123) == Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S1S: ");
        if (Test4S1S(123) == Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S2U: ");
        if (Test4S2U(123) == Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S2S: ");
        if (Test4S2S(123) == Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S4U: ");
        if (Test4S4U(123) == (int)Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S8U: ");
        if (Test4S8U(123) == (int)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test4S8S: ");
        if (Test4S8S(123) == (int)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U1U: ");
        if (Test8U1U(123) == (ulong)Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U1S: ");
        if (Test8U1S(123) == (ulong)Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U2U: ");
        if (Test8U2U(123) == Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U2S: ");
        if (Test8U2S(123) == (ulong)Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U4U: ");
        if (Test8U4U(123) == Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U4S: ");
        if (Test8U4S(123) == (ulong)Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8U8S: ");
        if (Test8U8S(123) == (ulong)Return8S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S1U: ");
        if (Test8S1U(123) == Return1U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S1S: ");
        if (Test8S1S(123) == Return1S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S2U: ");
        if (Test8S2U(123) == Return2U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S2S: ");
        if (Test8S2S(123) == Return2S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S4U: ");
        if (Test8S4U(123) == Return4U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S4S: ");
        if (Test8S4S(123) == Return4S(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }
        Console.Write("Test8S8U: ");
        if (Test8S8U(123) == (long)Return8U(123))
        {
            Console.WriteLine("Pass");
        }
        else
        {
            Console.WriteLine("Fail");
            result = false;
        }

        return result ? 100 : 101;
    }

    private static byte Return1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return 112;
    }

    private static sbyte Return1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return -11;
    }

    private static ushort Return2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return 11223;
    }

    private static short Return2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return -22334;
    }

    private static uint Return4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return 1122334455;
    }

    private static int Return4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return -554433221;
    }

    private static ulong Return8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return 11223344556677889911;
    }

    private static long Return8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        return -998877665544332211;
    }

    private static byte Test1U1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return1S(arg);
    }

    private static byte Test1U2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return2U(arg);
    }

    private static byte Test1U2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return2S(arg);
    }

    private static byte Test1U4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return4U(arg);
    }

    private static byte Test1U4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return4S(arg);
    }

    private static byte Test1U8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return8U(arg);
    }

    private static byte Test1U8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (byte)Return8S(arg);
    }

    private static sbyte Test1S1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return1U(arg);
    }

    private static sbyte Test1S2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return2U(arg);
    }

    private static sbyte Test1S2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return2S(arg);
    }

    private static sbyte Test1S4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return4U(arg);
    }

    private static sbyte Test1S4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return4S(arg);
    }

    private static sbyte Test1S8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return8U(arg);
    }

    private static sbyte Test1S8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (sbyte)Return8S(arg);
    }

    private static ushort Test2U1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1U(arg);
    }

    private static ushort Test2U1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ushort)Return1S(arg);
    }

    private static ushort Test2U2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ushort)Return2S(arg);
    }

    private static ushort Test2U4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ushort)Return4U(arg);
    }

    private static ushort Test2U4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ushort)Return4S(arg);
    }

    private static ushort Test2U8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ushort)Return8U(arg);
    }

    private static ushort Test2U8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ushort)Return8S(arg);
    }

    private static short Test2S1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1U(arg);
    }

    private static short Test2S1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1S(arg);
    }

    private static short Test2S2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (short)Return2U(arg);
    }

    private static short Test2S4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (short)Return4U(arg);
    }

    private static short Test2S4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (short)Return4S(arg);
    }

    private static short Test2S8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (short)Return8U(arg);
    }

    private static short Test2S8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (short)Return8S(arg);
    }

    private static uint Test4U1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1U(arg);
    }

    private static uint Test4U1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (uint)Return1S(arg);
    }

    private static uint Test4U2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return2U(arg);
    }

    private static uint Test4U2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (uint)Return2S(arg);
    }

    private static uint Test4U4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (uint)Return4S(arg);
    }

    private static uint Test4U8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (uint)Return8U(arg);
    }

    private static uint Test4U8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (uint)Return8S(arg);
    }

    private static int Test4S1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1U(arg);
    }

    private static int Test4S1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1S(arg);
    }

    private static int Test4S2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return2U(arg);
    }

    private static int Test4S2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return2S(arg);
    }

    private static int Test4S4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (int)Return4U(arg);
    }

    private static int Test4S8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (int)Return8U(arg);
    }

    private static int Test4S8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (int)Return8S(arg);
    }

    private static ulong Test8U1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1U(arg);
    }

    private static ulong Test8U1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ulong)Return1S(arg);
    }

    private static ulong Test8U2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return2U(arg);
    }

    private static ulong Test8U2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ulong)Return2S(arg);
    }

    private static ulong Test8U4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return4U(arg);
    }

    private static ulong Test8U4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ulong)Return4S(arg);
    }

    private static ulong Test8U8S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (ulong)Return8S(arg);
    }

    private static long Test8S1U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1U(arg);
    }

    private static long Test8S1S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return1S(arg);
    }

    private static long Test8S2U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return2U(arg);
    }

    private static long Test8S2S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return2S(arg);
    }

    private static long Test8S4U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return4U(arg);
    }

    private static long Test8S4S(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return Return4S(arg);
    }

    private static long Test8S8U(int arg)
    {
        if (arg == int.MaxValue) s_val = Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount + Environment.TickCount;

        ClobberReturnReg();
        return (long)Return8U(arg);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong ClobberReturnReg()
    {
        return 0xdeadbeefdeadbeef;
    }
}
