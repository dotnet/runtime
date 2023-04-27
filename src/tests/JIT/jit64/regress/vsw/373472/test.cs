// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// This test is used to try out very large decrementing loop strides.  The strides cannot be negated if the integer
// is too large.  For example, a stride of 0xA0000000 cannot be turned into a signed number.  For the most
// part, other things prevent us from getting to OSR with a condition like this but it's good to have
// coverage for large strides.

public class StrideTest
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;
        pass &= Test1();
        pass &= Test2();
        pass &= Test3();

        return (pass ? 100 : 1);
    }

    public static bool Test1()
    {
        try
        {
            uint[] array = new uint[0x8ffffff];
            for (uint i = 0x8fffffe; true; i -= 0xa0000001)
            {
                array[i] = 40;
            }
        }
        catch (IndexOutOfRangeException)
        {
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("test1 exception: {0}", e.ToString());
        }

        Console.WriteLine("Test1 failed");
        return false;
    }

    public static bool Test2()
    {
        try
        {
            uint[] array = new uint[0x8ffffff];
            for (uint i = 0; true; i -= 0xa0a0a0a0)
            {
                array[i] = i;
            }
        }
        catch (IndexOutOfRangeException)
        {
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("test2 exception: {0}", e.ToString());
        }

        Console.WriteLine("Test2 failed");
        return false;
    }

    public static bool Test3()
    {
        try
        {
            int[] array = new int[0x8ffffff];
            for (long i = 0x8ffffffL - 1; i > 0x8ffffffL - 0xa0a0a0a0L - 1000; i -= 0xa0a0a0a0L)
            {
                array[i] = (int)i;
            }
        }
        catch (IndexOutOfRangeException)
        {
            return true;
        }
        catch (OverflowException)
        {
            // This could potentially produce an overflow exception on x86 when calculating
            // address offset.
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("test3 exception: {0}", e.ToString());
        }

        Console.WriteLine("Test3 failed");
        return false;
    }

    public static bool Test4()
    {
        try
        {
            ulong[] array = new ulong[0xfffffff];
            ulong i = 0xa000000000000002;
            while (true)
            {
                i -= 0xa000000000000001;
                array[i] = i;
            }
        }
        catch (IndexOutOfRangeException)
        {
            return true;
        }
        catch (Exception) { }

        Console.WriteLine("Test4 failed");
        return false;
    }

    public static bool Test5()
    {
        try
        {
            ulong[] array = new ulong[0xfffffff];
            ulong i = 0xa000000000000010;
            while (true)
            {
                i -= 0xa000000000000001;
                while (i >= 0)
                {
                    array[i] = i;
                    i -= 1;
                }

                array[i] = i;
            }
        }
        catch (IndexOutOfRangeException)
        {
            return true;
        }
        catch (Exception) { }

        Console.WriteLine("Test5 failed");
        return false;
    }
}
