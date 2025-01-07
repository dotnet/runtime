// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-2-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 2-deep inheritance in
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
        int mi_RetCode;
        C c = new C();
        mi_RetCode = c.Test();
        
        if(mi_RetCode == 100)
            Console.WriteLine("Pass");
        else
            Console.WriteLine("FAIL");
        
        return mi_RetCode;
    }
}

class A
{
    //////////////////////////////
    // Instance Fields
    public int FldPubInst;
    private int FldPrivInst;
    protected int FldFamInst;          //Translates to "family"
    internal int FldAsmInst;           //Translates to "assembly"
    protected internal int FldFoaInst; //Translates to "famorassem"
    
    //////////////////////////////
    // Static Fields
    public static int FldPubStat;
    private static int FldPrivStat;
    protected static int FldFamStat;   //family
    internal static int FldAsmStat;    //assembly
    protected internal int FldFoaStat; //famorassem
    
    //////////////////////////////
    // Instance Methods
    public int MethPubInst()
    {
        Console.WriteLine("A::MethPubInst()");
        return 100;
    }
    
    private int MethPrivInst()
    {
        Console.WriteLine("A::MethPrivInst()");
        return 100;
    }
    
    protected int MethFamInst()
    {
        Console.WriteLine("A::MethFamInst()");
        return 100;
    }
    
    internal int MethAsmInst()
    {
        Console.WriteLine("A::MethAsmInst()");
        return 100;
    }
    
    protected internal int MethFoaInst()
    {
        Console.WriteLine("A::MethFoaInst()");
        return 100;
    }
    
      //////////////////////////////
      // Static Methods
    public static int MethPubStat()
    {
        Console.WriteLine("A::MethPubStat()");
        return 100;
    }
    
    private static int MethPrivStat()
    {
        Console.WriteLine("A::MethPrivStat()");
        return 100;
    }
    
    protected static int MethFamStat()
    {
        Console.WriteLine("A::MethFamStat()");
        return 100;
    }
    
    internal static int MethAsmStat()
    {
        Console.WriteLine("A::MethAsmStat()");
        return 100;
    }
    
    protected internal static int MethFoaStat()
    {
        Console.WriteLine("A::MethFoaStat()");
        return 100;
    }
    
      //////////////////////////////
      // Virtual Instance Methods
    public virtual int MethPubVirt()
    {
        Console.WriteLine("A::MethPubVirt()");
        return 100;
    }
    
    //@csharp - Note that C# won't compile an illegal private virtual function
    //So there is no negative testing MethPrivVirt() here.
    
    protected virtual int MethFamVirt()
    {
        Console.WriteLine("A::MethFamVirt()");
        return 100;
    }
    
    internal virtual int MethAsmVirt()
    {
        Console.WriteLine("A::MethAsmVirt()");
        return 100;
    }
    
    protected internal virtual int MethFoaVirt()
    {
        Console.WriteLine("A::MethFoaVirt()");
        return 100;
    }
}

class B : A
{
      //@todo - Class B is currently a simple placeholder to force N-Deep inheritance...
      //However, a non-trivial class B that might hide some members of A as a visiblity
      //test is a test that we need to think about and develop.  That is not currently the
      //focus of this test (maybe in the near future), but for now we're happy forcing
      //a N-Deep inheritance.  Such instances have, in the past, proven worthy of
      //investigation.
    
    public int placeholder;
}
    
class C : B
{
    public int Test()
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance field access
        FldPubInst = 100;
        if(FldPubInst != 100)
            mi_RetCode = 0;
        
        //@csharp - Note that C# will not compile an illegal access of FldPrivInst
        //So there is no negative test here, it should be covered elsewhere and
        //should throw a FieldAccessException within the runtime.  (IL sources is
        //the most logical, only?, choice)
        
        FldFamInst = 100;
        if(FldFamInst != 100)
            mi_RetCode = 0;
        
        FldAsmInst = 100;
        if(FldAsmInst != 100)
            mi_RetCode = 0;
        
        FldFoaInst = 100;
        if(FldFoaInst != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static field access
        FldPubStat = 100;
        if(FldPubStat != 100)
            mi_RetCode = 0;
        
        //@csharp - Again, note C# won't do private field access
        
        FldFamStat = 100;
        if(FldFamStat != 100)
            mi_RetCode = 0;
        
        FldAsmStat = 100;
        if(FldAsmStat != 100)
            mi_RetCode = 0;
        
        FldFoaStat = 100;
        if(FldFoaStat != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test instance method access
        if(MethPubInst() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private method access
        
        if(MethFamInst() != 100)
            mi_RetCode = 0;
        
        if(MethAsmInst() != 100)
            mi_RetCode = 0;
        
        if(MethFoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static method access
        if(MethPubStat() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private method access
        
        if(MethFamStat() != 100)
            mi_RetCode = 0;
        
        if(MethAsmStat() != 100)
            mi_RetCode = 0;
        
        if(MethFoaStat() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test virtual method access
        if(MethPubVirt() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private method access
        
        if(MethFamVirt() != 100)
            mi_RetCode = 0;
        
        if(MethAsmVirt() != 100)
            mi_RetCode = 0;
        
        if(MethFoaVirt() != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }
}
