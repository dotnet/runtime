// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandle.Target

using System;
using System.Runtime.InteropServices;

public class Test_Target
{
    public class Dummy
    {
        public int flag;

        public Dummy(int i)
        {
            flag = i;
        }

        public int getFlag()
        {
            return flag;
        }
    }
    public static int Main()
    {
        Dummy obj = new Dummy(99);
        bool passed = true;

        Console.WriteLine("Allocating a handle to object..");
        GCHandle handle = GCHandle.Alloc(obj);

        Dummy target = (Dummy)handle.Target;

        if (target.getFlag() == 99)
        {
            Console.WriteLine("Test for GCHandle.get_Target passed!");
        }
        else
        {
            Console.WriteLine("Test for GCHandle.get_Target failed!");
            passed = false;
        }

        Dummy obj2 = new Dummy(66);
        handle.Target = obj2;
        Dummy target2 = (Dummy)handle.Target;

        if (target2.getFlag() == 66)
        {
            Console.WriteLine("Test for GCHandle.set_Target passed!");
        }
        else
        {
            Console.WriteLine("Test for GCHandle.set_Target failed!");
            passed = false;
        }

        handle.Free();

        if (passed)
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }
        Console.WriteLine("Test Failed!");
        return 1;
    }
}
