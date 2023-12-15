// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* 

This is regression test for VSW 577403 
We had a breaking change between 1.1 and 2.0 when a class implements an
  interface and a base type but doesn't override interface's method.

When invoking ITest.Test() on an instance of Level3 we got "Level1::Test" printed out instead of "Level2::Test".
On v1.1 we get correctly "Level2::Test"

After the fix, the Whidbey behavior is correct as well.


*/

using System; 
using Xunit;


public class Program 
{ 
    [Fact]
    public static int TestEntryPoint() 
    { 
        ITest test = new Level3();
        ITest gen_test = new GenericLevel4();

        int ret1 = test.Test;
        int ret2 = test.Test2;

        int gen_ret1 = gen_test.Test;
        int gen_ret2 = gen_test.Test2;
        
        if (ret1 != 21 || ret2 != 32)
        { 
            Console.WriteLine("FAIL");
            Console.WriteLine("EXPECTED: '21' and '32' when invoking test.Test and test.Test2 on an instance of Level3"); 
            Console.WriteLine("ACTUAL: '" + ret1 + "' and '" + ret2 + "'");
            return 101;
        }

        if (gen_ret1 != 21 || gen_ret2 != 32)
        {
            Console.WriteLine("FAIL");
            Console.WriteLine("EXPECTED: '21' and '32' when invoking gen_test.Test and gen_test.Test2 on an instance of GenericLevel4");
            Console.WriteLine("ACTUAL: '" + gen_ret1 + "' and '" + gen_ret2 + "'");
            return 102;
        }
        
        Console.WriteLine("PASS");
        return 100;

        
    } 
} 

interface ITest 
{ 
    int Test { get; } 
       int Test2 { get; } 
} 

class Level1 : ITest 
{ 
       public int Test { get { return 11; } } 
    public int Test2 { get { return 12; } } 
} 

class Level2 : Level1, ITest 
{ 
       int ITest.Test { get { return 21; } } 
    int ITest.Test2 { get { return 22; } } 
} 

class Level3 : Level2, ITest 
{ 
    int ITest.Test2 { get { return 32; } } 
}

//Generics
class GenericLevel2<T> : Level1, ITest
{
    int ITest.Test { get { return 21; } }
    int ITest.Test2 { get { return 22; } }
}

class GenericLevel3 : GenericLevel2<int>
{
}

class GenericLevel4 : GenericLevel3, ITest
{
    int ITest.Test2 { get { return 32; } }
}
