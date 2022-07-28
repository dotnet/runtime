// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Copy of GCHandleType.Weak .. the object with GCHandleType Weak 
// will be collected. The handle and it's copy remain allocated even after the object is collected.
// Also tests the target of the handle.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class Test_HandleCopy
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
        public GCHandle handle, copy;

        public CreateObj()
        {
            obj = new Dummy();
            Console.WriteLine("Allocating a Weak handle to object..");
            handle = GCHandle.Alloc(obj, GCHandleType.Weak);

            // making a copy of the handle
            copy = handle;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DestroyObj()
        {
            obj = null;
        }    

        public bool RunTest()
        {
            DestroyObj();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            bool ans1 = handle.IsAllocated;
            bool ans2 = copy.IsAllocated;

            //Console.WriteLine("handle.IsAllocated = " + ans1);
            //Console.WriteLine("copy.IsAllocated = " + ans2);

            Dummy target1 = (Dummy)handle.Target;
            Dummy target2 = (Dummy)copy.Target;

            if (((ans1 == true) && (ans2 == true)) && ((target1 == null) && (target2 == null)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();
        if (temp.RunTest())
        {
            Console.WriteLine("Test for Copy of GCHandle passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for Copy of GCHandle failed!");
            return 1;
        }
    }
}
