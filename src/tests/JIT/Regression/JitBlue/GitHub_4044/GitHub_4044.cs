// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class C
{
    [Fact]
    public static int TestEntryPoint()
    {
        int error = Test1();
        error += Test2();
        error += Test3();
        error += Test3b();
        error += Test4();
        error += Test5();
        error += Test6();
        Console.WriteLine(error == 0 ? "Pass" : "Fail");
        return 100 + error;
    }

    static int Test1()
    {
        try {
            return Test1Inner();
        }
        catch {
            return 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test1Inner()
    {
        int i = 3;
        do
        {
            try
            {
                throw new Exception();
            }
            // we should be decrementing i here, it does not happen
            catch (Exception) when (--i < 0)
            {
                Console.Write("e");
                break;
            }
            catch (Exception)
            {
                // just printing constant 3 here
                //000000b5  mov         ecx,3
                //000000ba  call        000000005B3CBAF0
                Print1(i);
            }
        } while (true);

        return 0;
    }

    static int limit = 10;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Print1(int i)
    {
        if (limit > 0) {
            Console.Write(i.ToString());
            --limit;
        }
        else {
            throw new Exception();
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test2()
    {
        int x = 1, y = 5;
        try {
            throw new Exception();
        } catch when (Print2(Print2(0, --x), ++x) == 1) {
        } catch {
            // Need a PHI here for x that includes the decremented
            // value; doesn't happen if we don't realize that exceptions
            // at the first call to Print2 can reach here.
            // Without it, we might const-prop a 1 here which is
            // incorrect when the first Print2 call throws.
            y = Print2(1, x);
        }

        return y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Print2(int i, int j)
    {
        if (i == 0)
            throw new Exception();

        Console.WriteLine(j.ToString());
        return j;
    }

    static int Test3()
    {
        return Test3(0, 50);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test3(int x, int y) // must pass 50 for y
    {
        try {
            throw new Exception();
        }
        // Need to make sure the increment to y is not
        // hoisted to before the call to Throw(x).  If
        // the importer doesn't realize that an exception
        // from Throw(x) can be caught in this method, it
        // won't separate Throw(x) out from the Ignore(..)
        // tree, but will still separate out ++y, creating
        // the bad reordering
        catch when (Ignore(Throw(x), ++y)) { }
        catch { Print3(y); }

        return y - 50; // Caller should pass '50'
    }

    static int Test3b()
    {
        return Test3b(new int[5], 50);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test3b(int[] x, int y)
    {
        try {
            throw new Exception();
        }
        // Same as Test3 except that the tree which raises
        // an exception is the array access x[100] instead
        // of a call.
        catch when (Ignore(x[100], ++y)) { }
        catch { Print3(y); }

        return y - 50; // Caller should pass '50'
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Ignore(int a, int b) { return false; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Throw(int n) { throw new Exception(); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Print3(int i)
    {
        Console.WriteLine(i.ToString());
        return i;
    }

    class BoxedBool
    {
        public bool Field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test4()
    {
        return Test4(new BoxedBool(), new Exception());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test4(BoxedBool box, Exception e)
    {
        bool b = box.Field;

        try {
            throw e;
        }
        // This filter side-effects the heap and then throws an
        // exception which is caught by the outer catch; make
        // sure we recognize the heap modification and don't think
        // we can value-number the load here the same as the load
        // on entry to the function.
        catch when ((box.Field = true) && ((BoxedBool)null).Field) {
        }
        catch {
            b = box.Field;
        }

        if (b) {
            Console.WriteLine("true");
        }
        else {
            Console.WriteLine("false");
        }

        return (b ? 0 : 1);
    }


    static int Test5()
    {
        int n = int.MaxValue - 1;
        int m = Test5(n);
        return (m == n ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test5(int n)
    {
        try {
            throw new Exception();
        }
        catch when (Filter5(ref n)) { }
        catch { }
        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool Filter5(ref int n)
    {
        // Codegen'ing this as
        //   add [addr_of_n], 2
        //   jo throw_ovf
        // would incorrectly modify n on the path where
        // the exception occurs.
        n = checked(n + 2);
        return (n != 0);
    }


    static int Test6()
    {
        int n = int.MaxValue - 3;
        int m = Test6(n);
        return (m == n + 2 ? 0 : 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test6(int n)
    {
        try {
            throw new Exception();
        }
        catch when (Filter6(ref n)) { }
        catch { }
        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool Filter6(ref int n)
    {
        // Codegen'ing this as
        //   add [addr_of_n], 4
        //   jo throw_ovf
        // would incorrectly increment n by 4 rather than 2
        // on the path where the exception occurs.
        checked {
            n += 2;
            n += 2;
        }
        return (n != 0);
    }
}
