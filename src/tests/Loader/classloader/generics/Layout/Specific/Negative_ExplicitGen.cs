// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test tests that we throw TypeLoadException when trying to load explicit generic class/struct, since we do not allow
// explicit generic types anymore.


using System;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Explicit)]
public class Gen1<T>
{
  // multiple fields, first generic
    [FieldOffset(0)] public T t;    
    [FieldOffset(16)]public int _int0 = 0;
}

[StructLayout(LayoutKind.Explicit)]
public class Gen2<T>
{
  // single field, generic
    [FieldOffset(0)] public T t;    
}

[StructLayout(LayoutKind.Explicit)]
public class Gen3<T>
{
  // single field, not generic
    [FieldOffset(0)] public int t;  
}

[StructLayout(LayoutKind.Explicit)]
public class Gen4<T>
{
  // multiple generic fields
  [FieldOffset(0)] public T t1;
  [FieldOffset(16)] public T t2;
  [FieldOffset(32)] public T t3;

}

[StructLayout(LayoutKind.Explicit)]
public struct Gen5<T>
{
  // multiple fields, generic is not first in a struct
  [FieldOffset(0)] public int t1;
  [FieldOffset(16)] public T t2;
}

[StructLayout(LayoutKind.Explicit)]
public struct Gen6<T>
{
  // single generic field in a struct
  [FieldOffset(0)] public T t1;
}

[StructLayout(LayoutKind.Sequential)]
public struct Gen8<T>
{
    // single generic field in a struct
    public T t1;
    public int i;
}

[StructLayout(LayoutKind.Explicit)]
public class Gen7<T>
{
  // nested sequential struct inside explicit struct
    [FieldOffset(0)]
    public Gen8<int> struct_Gen8;
    [FieldOffset(0)] public T t;    
}

public class Test
{
    public static void goGen1()
    {
        Gen1<int> gen1 = new Gen1<int>();
        gen1.t = 5;
        
        Console.WriteLine("Gen1: FAIL");
    }

    public static void goGen2()
    {
        Gen2<int> gen2 = new Gen2<int>();
        gen2.t = 5;
    
        Console.WriteLine("Gen2: FAIL");
    }
    
    public static void goGen3()
    {
        Gen3<int> gen3 = new Gen3<int>();
        gen3.t = 5;
    
        Console.WriteLine("Gen3: FAIL");
    }

    public static void goGen4()
    {
        Gen4<int> gen4 = new Gen4<int>();
        gen4.t1 = 5;
        
        Console.WriteLine("Gen4: FAIL");
    }

    public static void goGen5()
    {
        Gen5<int> gen5 = new Gen5<int>();
        gen5.t1 = 5;
        
        Console.WriteLine("Gen5: FAIL");
    }

    public static void goGen6()
    {
        Gen6<int> gen6 = new Gen6<int>();
        gen6.t1 = 5;
        Console.WriteLine("Gen6: FAIL");
    }

    public static void goGen7()
    {
        Gen7<int> gen7 = new Gen7<int>();
        gen7.t = 5;
        gen7.struct_Gen8 = new Gen8<int>();
        
        Console.WriteLine("Gen7: FAIL");
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;

        try
        {
            goGen1();
            pass = false;
        }
        catch(TypeLoadException)
        {
            Console.WriteLine("Gen1: PASS");
        }
        try
        {
            goGen2();
            pass = false;
        }
        catch(TypeLoadException)
        {
          Console.WriteLine("Gen2: PASS");
        }
        try
        {
            goGen3();
            pass = false;
        }
        catch(TypeLoadException)
        {
          Console.WriteLine("Gen3: PASS");
        }   
        try
        {
            goGen4();
            pass = false;
        }
        catch(TypeLoadException)
        {
          Console.WriteLine("Gen4: PASS");
        }
        try
        {
            goGen5();
            pass = false;
        }
        catch(TypeLoadException)
        {
          Console.WriteLine("Gen5: PASS");
        }
        try
        {
            goGen6();
            pass = false;
        }
        catch(TypeLoadException)
        {
            Console.WriteLine("Gen6: PASS");
        }

        try
        {
            goGen7();
            pass = false;
        }
        catch(TypeLoadException)
        {
            Console.WriteLine("Gen7: PASS");
        }

        if(pass)
        {
            Console.WriteLine("Test passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test failed");
            return 101;
        }
    }
}
