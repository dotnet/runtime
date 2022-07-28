// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GCHandleType.Weak .. the object with GCHandleType Weak 
// will be collected.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class Test_Weak
{
    public class Dummy
    {
        public static int flag = 0;
        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            flag = 99;
        }
    }

    public class CreateObj
    {
        public Dummy obj;

        public CreateObj()
        {
            obj = new Dummy();
            Console.WriteLine("Allocating a Weak handle to object..");
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Weak);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void RunTest()
        {
            // ensuring that GC happens even with /debug mode
            obj = null;
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();
        temp.RunTest();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (Dummy.flag == 99)
        {
            Console.WriteLine("Test for GCHandleType.Weak passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for GCHandleType.Weak failed!");
            return 1;
        }
    }
}
