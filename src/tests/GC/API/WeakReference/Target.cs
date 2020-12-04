// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests WeakReference.Target
// Retrieves or assigns the object an IsAlive status.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class Dummy
{
    public int val = 0;

    public Dummy(int val)
    {
        this.val = val;
    }
}

public class Test
{
    public static int[] array;
    public static Object[] obj;
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateArrays() 
    {
        array = new int[50];
        obj = new Object[25];
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static WeakReference CreateArrayWeakReference()
    {
        return new WeakReference(array);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroyArrays() 
    {
        array = null;
        obj = null;
    }
    
    public bool GetTargetTest()
    {
        CreateArrays();
        WeakReference weakarray = CreateArrayWeakReference(); // array has only weak reference

        // obj has both strong and weak ref and so should not get collected

        WeakReference weakobj = new WeakReference(obj);
        GCHandle objhandle = GCHandle.Alloc(obj, GCHandleType.Normal);
        
        DestroyArrays();
        GC.Collect();

        Object target1 = weakarray.Target; // should be null
        Object target2 = weakobj.Target;   // should be System.Object[]

        Console.WriteLine("{0},{1}", target1, target2);

        if ((target1 == null) && (target2 != null))
        {
            Console.WriteLine("Test for WeakReference.get_Target passed!");
            return true;
        }
        else
        {
            Console.WriteLine("Test for WeakReference.get_Target failed!");
            return false;
        }
    }

    public bool SetTargetTest()
    {
        Dummy d1 = new Dummy(99);
        Dummy d2 = new Dummy(66);

        WeakReference wr = new WeakReference(d1);   // array has only weak reference
        wr.Target = d2;                             // overwrite wr.Target with d2
        Dummy d3 = (Dummy)wr.Target;                // get wr.Target
        GC.KeepAlive(d2);                           // required so d2 doesn't get collected before setting d3

        if (d3.val == 66)
        {
            // make sure d3 == d2, not d1
            Console.WriteLine("Test for WeakReference.set_Target passed!");
            return true;
        }
        else
        {
            Console.WriteLine("Test for WeakReference.set_Target failed!");
            return false;
        }
    }

    public static int Main()
    {
        bool passed1, passed2;

        Test t = new Test();

        passed1 = t.GetTargetTest();
        passed2 = t.SetTargetTest();

        if (passed1 && passed2)
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }

        Console.WriteLine("Test Failed!");
        return 1;
    }
}
