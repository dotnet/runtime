// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-2-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 2-deep inheritance in
// the separate assemblies and modules.
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
        
        /*  FldAsmInst = 100;
        if(FldAsmInst != 100)
        mi_RetCode = 0;
        */
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
        
        /*  FldAsmStat = 100;
        if(FldAsmStat != 100)
        mi_RetCode = 0;
        */
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
        
        /*  if(MethAsmInst() != 100)
        mi_RetCode = 0;
        */
        if(MethFoaInst() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test static method access
        if(MethPubStat() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private method access
        
        if(MethFamStat() != 100)
            mi_RetCode = 0;
        
        /*  if(MethAsmStat() != 100)
        mi_RetCode = 0;
        */
        if(MethFoaStat() != 100)
            mi_RetCode = 0;
        
        /////////////////////////////////
        // Test virtual method access
        if(MethPubVirt() != 100)
            mi_RetCode = 0;
        
        //@csharp - C# won't do private method access
        
        if(MethFamVirt() != 100)
            mi_RetCode = 0;
        
        /*  if(MethAsmVirt() != 100)
        mi_RetCode = 0;
        */
        if(MethFoaVirt() != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }
}
