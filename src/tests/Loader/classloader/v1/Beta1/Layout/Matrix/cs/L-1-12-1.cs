// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////
// L-1-9-1.cs - Beta1 Layout Test - RDawson
//
// Tests layout of classes using 1-deep implementation in
// the same assembly and module
//
// See ReadMe.txt in the same project as this source for
// further details about these tests.
//

//@csharp C# doesn't allow much flexibility here in terms of negative testing on interfaces...
//THIS NEEDS MORE COVERAGE

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

interface A{

  //////////////////////////////
  // Instance Methods
int MethPubInst();
}

interface B{
int MethPubInst2();
}

class C : A, B
{
    public int MethPubInst()
    {
        Console.WriteLine("B::MethPubInst()");
        return 100;
    }
    
    public int MethPubInst2()
    {
        Console.WriteLine("C::MethPubInst2()");
        return 100;
    }
    
    public int Test()
    {
        int mi_RetCode = 100;
        
        /////////////////////////////////
        // Test instance method access  
        if(MethPubInst() != 100)
            mi_RetCode = 0;
        
        if(MethPubInst2() != 100)
            mi_RetCode = 0;
        
        return mi_RetCode;
    }
}
