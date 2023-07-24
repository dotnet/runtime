// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

// Simple struct containing two integers (size 8).
struct MyStruct
{
    public MyStruct(int a, int b)
    {
        A = a;
        B = b;
    }

    public int A;
    public int B;
    public int C { get { return A + B; } }
}

public class Program
{

    static int Pass = 100;
    static int Fail = -1;
    [Fact]
    public static int TestEntryPoint()
    {
        // Entry point for our repro.
        // Pass in a bunch of integers.  The 5th parameter is a MyStruct, a value type of size 8.
        int result = Caller(0, 1, 2, 3, new MyStruct(4, 5));
        if (result != 9)
        {
            Console.WriteLine("Fail");
            return Fail;
        }
        else
        {
            Console.WriteLine("Pass");
            return Pass;
        }
    }

    // Caller method takes 5 parameters.  4 in register and 1 on the stack.  The important details here are that
    //  * Must take a value type as a parameter
    //  * The value type must be of size 1, 2, 4 or 8
    //  * That parameter must be passed on the stack.  Typically this is the 5th parameter and beyond for
    //    static methods, or 4th and beyond for instance methods.
    static int Caller(int regParam1, int regParam2, int regParam3, int regParam4, MyStruct stackParam1)
    {
        // Add random calls to block inlining of this call into the parent frame.
        Console.Write("Let's ");
        Console.Write("Discourage ");
        Console.Write("Inlining ");
        Console.Write("Of ");
        Console.Write("Caller ");
        Console.Write("Into ");
        Console.WriteLine("Main.");

        // Avoid touching stackParam1 except to pass it off to the callee.  Any non-trivial usage of
        // stackParam1 or other code within this method will likely eliminate the potential for tail
        // call optimization.
        // if (stackParam1.C == 9) Console.WriteLine("C == 9");

        // Now make our call.
        // The keys here are:
        //  * That the incoming value type stack parameter must be passed to the callee in register.
        //  * Tail call optimizations must be enabled
        //  * The tail call optimization must fire (see above for an example of what might block it).
        // The JIT will incorrectly load the outgoing parameter from the incorrect stack location
        // on the local frame.
        return Callee(stackParam1, regParam1, regParam2, regParam3, regParam4);
    }

    // Callee method takes 5 parameters.  4 in register and 1 on the stack.  The important details here are that
    //  * Must take a value type as a parameter
    //  * The value type must be of size 1, 2, 4 or 8
    //  * That parameter must be passed in register.  Typically this is the 4th parameter or before for
    //    static methods, or 3rd or before for instance methods.
    static int Callee(MyStruct regParam1, int regParam2, int regParam3, int regParam4, int stackParam1)
    {
        // If all conditions are met, Callee enters with an incorrect value for regParam1
        // This should print "9 0 1 2 3".  If the tail call is made incorrectly,
        // the result is (typically) "418858424 0 1 2 3".
        System.Console.WriteLine("Printing Outputs: {0} {1} {2} {3} {4}",
            regParam1.C,
            regParam2,
            regParam3,
            regParam4,
            stackParam1);

        return regParam1.C;
    }
}
