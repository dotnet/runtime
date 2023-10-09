// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-11-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes two unrelated classes in
// the same assembly and module
//
// See ReadMe.txt in the same project as this source for
// further details about these tests.
//

using System;
using Xunit;

public class Test
{
    [Fact]
    public static int TestEntryPoint()
    {
        int mi_RetCode = 100;
        A a = new A();
        B b = new B();
        
        //  if(a.Test(b) != 100)
        //    mi_RetCode = 0;
        
        if(b.Test(a) != 100)
        mi_RetCode = 0;
        
        if(mi_RetCode == 100)
            Console.WriteLine("Pass");
        else
            Console.WriteLine("FAIL");
        
        return mi_RetCode;
    }
}


struct B
{
    public int Test(A a)
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance field access
        a.FldPubInst = 100;
        if(a.FldPubInst != 100)
            mi_RetCode = 0;
        
        //@csharp - Note that C# will not compile an illegal access of a.FldPrivInst
        //So there is no negative test here, it should be covered elsewhere and
        //should throw a FieldAccessException within the runtime.  (IL sources is
        //the most logical, only?, choice)
        
        //@csharp - C# Won't compile illegal family access from non-family members
        
        /////////////////////////////////
        // Test static field access
        A.FldPubStat = 100;
        if(A.FldPubStat != 100)
            mi_RetCode = 0;
        
        //@csharp - Again, note C# won't do private field access
        
        //@csharp - C# Won't compile illegal family access from non-family members
        
        /////////////////////////////////
        // Test instance a.Method access
        if(a.MethPubInst() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private a.Method access
        
        //@csharp - C# Won't compile illegal family access from non-family members
        
        /////////////////////////////////
        // Test static a.Method access
        if(A.MethPubStat() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private a.Method access
        
        //@csharp - C# Won't compile illegal family access from non-family members
        
        return mi_RetCode;
    }


    //////////////////////////////
    // Instance Fields
    public int FldPubInst;
    private int FldPrivInst;
    internal int FldAsmInst;           //Translates to "assembly"

    //////////////////////////////
    // Static Fields
    public static int FldPubStat;
    private static int FldPrivStat;
    internal static int FldAsmStat;    //assembly

    //////////////////////////////
    // Instance Methods
    public int MethPubInst()
    {
        Console.WriteLine("B::MethPubInst()");
        return 100;
    }

    private int MethPrivInst()
    {
        Console.WriteLine("B::MethPrivInst()");
        return 100;
    }

    internal int MethAsmInst()
    {
        Console.WriteLine("B::MethAsmInst()");
        return 100;
    }

    //////////////////////////////
    // Static Methods
    public static int MethPubStat()
    {
        Console.WriteLine("B::MethPubStat()");
        return 100;
    }

    private static int MethPrivStat()
    {
        Console.WriteLine("B::MethPrivStat()");
        return 100;
    }

    internal static int MethAsmStat()
    {
        Console.WriteLine("B::MethAsmStat()");
        return 100;
    }
}
