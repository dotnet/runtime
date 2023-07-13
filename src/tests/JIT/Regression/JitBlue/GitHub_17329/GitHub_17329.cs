// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Having an UnsafeValueTypeAttribute on a struct causes incoming arguments to be
//  spilled into shadow copies and the struct promotion optimization does not 
//  currently handle this properly.
// 

[UnsafeValueTypeAttribute]
struct DangerousBuffer
{
    public long a;
    public long b;
    public long c;
}

struct Point1
{
    long x;

    public Point1(long _x)
    {
        x = _x;
    }

    public void Increase(ref Point1 s, long amount)
    {
        x = s.x + amount;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public long Value()
    {
        return x;
    }
}

public class TestCase
{
    static public long[] arr;

    unsafe static long Test(int size, Point1 a, Point1 b, Point1 c)
    {

        // Mutate the values stored in a, b and c
        // So if these have a shadow copy we will notice
        // 
        a.Increase(ref a, arr[0]);
        b.Increase(ref b, arr[1]);
        c.Increase(ref c, arr[2]);

	DangerousBuffer db = new DangerousBuffer();
        db.a = -1;
        db.b = -2;
        db.c = -3;

        long* x1 = stackalloc long[size];
	
        long sum = 0;
        if (size >= 3)
        {
            x1[0] = a.Value();
            x1[1] = b.Value();
            x1[2] = c.Value();
            
            for (int i = 0; i < size; i++)
            {
                sum += x1[i];
            }
        }        
        return sum;
    }

    [Fact]
    public static int TestEntryPoint()
    { 
        long testResult = 0;
        int mainResult = 0;

        Point1 p1 = new Point1(1);
        Point1 p2 = new Point1(3);
        Point1 p3 = new Point1(5);

	arr = new long[3];
	arr[0] = 9;
	arr[1] = 10;
	arr[2] = 11;


        testResult = Test(3, p1, p2, p3);

        if (testResult != 39)
        {
            Console.WriteLine("FAILED!");
            mainResult = -1;
        }
        else
        {
            Console.WriteLine("passed");
            mainResult = 100;
        }

        return mainResult;
    } 
}
