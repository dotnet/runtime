// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//Test is checking the ReserveSlot function
//   If someone screws up the function we will end up
//   setting values in the wrong slots and the totals will be wrong


using System;
using System.Threading;
using Xunit;

public class Value0
{

    [ThreadStatic]
    private static object One= 1;
    [ThreadStatic]
    private static object Two= 2;
    [ThreadStatic]
    private static object Three= 3;
    [ThreadStatic]
    private static object Four= 4;
    [ThreadStatic]
    private static object Five= 5;
    [ThreadStatic]
    private static object Six= 6;
    [ThreadStatic]
    private static object Seven= 7;
    [ThreadStatic]
    private static object Eight= 8;
    [ThreadStatic]
    private static object Nine= 9;
    [ThreadStatic]
    private static object Ten= 10;
    [ThreadStatic]
    private static object Eleven= 11;
    [ThreadStatic]
    private static object Twelve= 12;
    [ThreadStatic]
    private static object Thirteen= 13;
    [ThreadStatic]
    private static object Fourteen= 14;
    [ThreadStatic]
    private static object Fifteen= 15;
    [ThreadStatic]
    private static object Sixteen= 16;
    [ThreadStatic]
    private static object Seventeen= 17;
    [ThreadStatic]
    private static object Eightteen= 18;
    [ThreadStatic]
    private static object Nineteen= 19;
    [ThreadStatic]
    private static object Twenty= 20;
    [ThreadStatic]
    private static object TwentyOne= 21;
    [ThreadStatic]
    private static object TwentyTwo= 22;
    [ThreadStatic]
    private static object TwentyThree= 23;
    [ThreadStatic]
    private static object TwentyFour= 24;
    [ThreadStatic]
    private static object TwentyFive= 25;
    [ThreadStatic]
    private static object TwentySix= 26;
    [ThreadStatic]
    private static object TwentySeven= 27;
    [ThreadStatic]
    private static object TwentyEight= 28;
    [ThreadStatic]
    private static object TwentyNine= 29;
    [ThreadStatic]
    private static object Thirty= 30;
    [ThreadStatic]
    private static object ThirtyOne= 31;
    [ThreadStatic]
    private static object ThirtyTwo= 32;

    public bool CheckValues()
    {
        if((int)ThirtyTwo != 32)
        {
            Console.WriteLine("ThirtySecond spot was incorrect!!!");
            return false;
        }
        
        int value = 0;
        value = (int)One
            + (int)Two
            + (int)Three
            + (int)Four
            + (int)Five
            + (int)Six
            + (int)Seven
            + (int)Eight
            + (int)Nine
            + (int)Ten
            + (int)Eleven
            + (int)Twelve
            + (int)Thirteen
            + (int)Fourteen
            + (int)Fifteen
            + (int)Sixteen
            + (int)Seventeen
            + (int)Eightteen
            + (int)Nineteen
            + (int)Twenty
            + (int)TwentyOne
            + (int)TwentyTwo
            + (int)TwentyThree
            + (int)TwentyFour
            + (int)TwentyFive
            + (int)TwentySix
            + (int)TwentySeven
            + (int)TwentyEight
            + (int)TwentyNine
            + (int)Thirty
            + (int)ThirtyOne
            + (int)ThirtyTwo;

        if(value != 528)
        {
           Console.WriteLine("Value0 - Wrong values in ThreadStatics!!! {0}",value);
            return false;
        }
        return true;
    }
    
}

public class Value1
{

    [ThreadStatic]
    private static object One= 1;
    [ThreadStatic]
    private static object Two= 2;
    [ThreadStatic]
    private static object Three= 3;
    [ThreadStatic]
    private static object Four= 4;
    [ThreadStatic]
    private static object Five= 5;
    [ThreadStatic]
    private static object Six= 6;
    [ThreadStatic]
    private static object Seven= 7;
    [ThreadStatic]
    private static object Eight= 8;
    [ThreadStatic]
    private static object Nine= 9;
    [ThreadStatic]
    private static object Ten= 10;
    [ThreadStatic]
    private static object Eleven= 11;
    [ThreadStatic]
    private static object Twelve= 12;
    [ThreadStatic]
    private static object Thirteen= 13;
    [ThreadStatic]
    private static object Fourteen= 14;
    [ThreadStatic]
    private static object Fifteen= 15;
    [ThreadStatic]
    private static object Sixteen= 16;
    [ThreadStatic]
    private static object Seventeen= 17;
    [ThreadStatic]
    private static object Eightteen= 18;
    [ThreadStatic]
    private static object Nineteen= 19;
    [ThreadStatic]
    private static object Twenty= 20;
    [ThreadStatic]
    private static object TwentyOne= 21;
    [ThreadStatic]
    private static object TwentyTwo= 22;
    [ThreadStatic]
    private static object TwentyThree= 23;
    [ThreadStatic]
    private static object TwentyFour= 24;
    [ThreadStatic]
    private static object TwentyFive= 25;
    [ThreadStatic]
    private static object TwentySix= 26;
    [ThreadStatic]
    private static object TwentySeven= 27;
    [ThreadStatic]
    private static object TwentyEight= 28;
    [ThreadStatic]
    private static object TwentyNine= 29;
    [ThreadStatic]
    private static object Thirty= 30;
    [ThreadStatic]
    private static object ThirtyOne= 31;
    [ThreadStatic]
    private static object ThirtyTwo= 32;

    public bool CheckValues()
    {
        if((int)ThirtyTwo != 32)
        {
            Console.WriteLine("ThirtySecond spot was incorrect!!!");
            return false;
        }
        
        int value = 0;
        value = (int)One
            + (int)Two
            + (int)Three
            + (int)Four
            + (int)Five
            + (int)Six
            + (int)Seven
            + (int)Eight
            + (int)Nine
            + (int)Ten
            + (int)Eleven
            + (int)Twelve
            + (int)Thirteen
            + (int)Fourteen
            + (int)Fifteen
            + (int)Sixteen
            + (int)Seventeen
            + (int)Eightteen
            + (int)Nineteen
            + (int)Twenty
            + (int)TwentyOne
            + (int)TwentyTwo
            + (int)TwentyThree
            + (int)TwentyFour
            + (int)TwentyFive
            + (int)TwentySix
            + (int)TwentySeven
            + (int)TwentyEight
            + (int)TwentyNine
            + (int)Thirty
            + (int)ThirtyOne
            + (int)ThirtyTwo;

        if(value != 528)
        {
            Console.WriteLine("Value1 - Wrong values in ThreadStatics!!! {0}",value);
            return false;
        }
        return true;
    }
    
}

public class Value2
{

    [ThreadStatic]
    private static object One= 1;
    [ThreadStatic]
    private static object Two= 2;
    [ThreadStatic]
    private static object Three= 3;
    [ThreadStatic]
    private static object Four= 4;
    [ThreadStatic]
    private static object Five= 5;
    [ThreadStatic]
    private static object Six= 6;
    [ThreadStatic]
    private static object Seven= 7;
    [ThreadStatic]
    private static object Eight= 8;
    [ThreadStatic]
    private static object Nine= 9;
    [ThreadStatic]
    private static object Ten= 10;
    [ThreadStatic]
    private static object Eleven= 11;
    [ThreadStatic]
    private static object Twelve= 12;
    [ThreadStatic]
    private static object Thirteen= 13;
    [ThreadStatic]
    private static object Fourteen= 14;
    [ThreadStatic]
    private static object Fifteen= 15;
    [ThreadStatic]
    private static object Sixteen= 16;
    [ThreadStatic]
    private static object Seventeen= 17;
    [ThreadStatic]
    private static object Eightteen= 18;
    [ThreadStatic]
    private static object Nineteen= 19;
    [ThreadStatic]
    private static object Twenty= 20;
    [ThreadStatic]
    private static object TwentyOne= 21;
    [ThreadStatic]
    private static object TwentyTwo= 22;
    [ThreadStatic]
    private static object TwentyThree= 23;
    [ThreadStatic]
    private static object TwentyFour= 24;
    [ThreadStatic]
    private static object TwentyFive= 25;
    [ThreadStatic]
    private static object TwentySix= 26;
    [ThreadStatic]
    private static object TwentySeven= 27;
    [ThreadStatic]
    private static object TwentyEight= 28;
    [ThreadStatic]
    private static object TwentyNine= 29;
    [ThreadStatic]
    private static object Thirty= 30;
    [ThreadStatic]
    private static object ThirtyOne= 31;
    [ThreadStatic]
    private static object ThirtyTwo= 32;

    public bool CheckValues()
    {
        if((int)ThirtyTwo != 32)
        {
            Console.WriteLine("Value2 - ThirtySecond spot was incorrect!!!");
            return false;
        }
        
        int value = 0;
        value = (int)One
            + (int)Two
            + (int)Three
            + (int)Four
            + (int)Five
            + (int)Six
            + (int)Seven
            + (int)Eight
            + (int)Nine
            + (int)Ten
            + (int)Eleven
            + (int)Twelve
            + (int)Thirteen
            + (int)Fourteen
            + (int)Fifteen
            + (int)Sixteen
            + (int)Seventeen
            + (int)Eightteen
            + (int)Nineteen
            + (int)Twenty
            + (int)TwentyOne
            + (int)TwentyTwo
            + (int)TwentyThree
            + (int)TwentyFour
            + (int)TwentyFive
            + (int)TwentySix
            + (int)TwentySeven
            + (int)TwentyEight
            + (int)TwentyNine
            + (int)Thirty
            + (int)ThirtyOne
            + (int)ThirtyTwo;

        if(value != 528)
        {
            Console.WriteLine("Wrong values in ThreadStatics!!! {0}",value);
            return false;
        }
        return true;
    }
}

public class Value3
{

    [ThreadStatic]
    private static object One= 1;
    [ThreadStatic]
    private static object Two= 2;
    [ThreadStatic]
    private static object Three= 3;
    [ThreadStatic]
    private static object Four= 4;
    [ThreadStatic]
    private static object Five= 5;
    [ThreadStatic]
    private static object Six= 6;
    [ThreadStatic]
    private static object Seven= 7;
    [ThreadStatic]
    private static object Eight= 8;
    [ThreadStatic]
    private static object Nine= 9;
    [ThreadStatic]
    private static object Ten= 10;
    [ThreadStatic]
    private static object Eleven= 11;
    [ThreadStatic]
    private static object Twelve= 12;
    [ThreadStatic]
    private static object Thirteen= 13;
    [ThreadStatic]
    private static object Fourteen= 14;
    [ThreadStatic]
    private static object Fifteen= 15;
    [ThreadStatic]
    private static object Sixteen= 16;
    [ThreadStatic]
    private static object Seventeen= 17;
    [ThreadStatic]
    private static object Eightteen= 18;
    [ThreadStatic]
    private static object Nineteen= 19;
    [ThreadStatic]
    private static object Twenty= 20;
    [ThreadStatic]
    private static object TwentyOne= 21;
    [ThreadStatic]
    private static object TwentyTwo= 22;
    [ThreadStatic]
    private static object TwentyThree= 23;
    [ThreadStatic]
    private static object TwentyFour= 24;
    [ThreadStatic]
    private static object TwentyFive= 25;
    [ThreadStatic]
    private static object TwentySix= 26;
    [ThreadStatic]
    private static object TwentySeven= 27;
    [ThreadStatic]
    private static object TwentyEight= 28;
    [ThreadStatic]
    private static object TwentyNine= 29;
    [ThreadStatic]
    private static object Thirty= 30;
    [ThreadStatic]
    private static object ThirtyOne= 31;
    [ThreadStatic]
    private static object ThirtyTwo= 32;

    public bool CheckValues()
    {
        if((int)ThirtyTwo != 32)
        {
            Console.WriteLine("Value2 - ThirtySecond spot was incorrect!!!");
            return false;
        }
        
        int value = 0;
        value = (int)One
            + (int)Two
            + (int)Three
            + (int)Four
            + (int)Five
            + (int)Six
            + (int)Seven
            + (int)Eight
            + (int)Nine
            + (int)Ten
            + (int)Eleven
            + (int)Twelve
            + (int)Thirteen
            + (int)Fourteen
            + (int)Fifteen
            + (int)Sixteen
            + (int)Seventeen
            + (int)Eightteen
            + (int)Nineteen
            + (int)Twenty
            + (int)TwentyOne
            + (int)TwentyTwo
            + (int)TwentyThree
            + (int)TwentyFour
            + (int)TwentyFive
            + (int)TwentySix
            + (int)TwentySeven
            + (int)TwentyEight
            + (int)TwentyNine
            + (int)Thirty
            + (int)ThirtyOne
            + (int)ThirtyTwo;

        if(value != 528)
        {
            Console.WriteLine("Wrong values in ThreadStatics!!! {0}",value);
            return false;
        }
        return true;
    }
}

public class Value4
{

    [ThreadStatic]
    private static object One= 1;
    [ThreadStatic]
    private static object Two= 2;
    [ThreadStatic]
    private static object Three= 3;
    [ThreadStatic]
    private static object Four= 4;
    [ThreadStatic]
    private static object Five= 5;
    [ThreadStatic]
    private static object Six= 6;
    [ThreadStatic]
    private static object Seven= 7;
    [ThreadStatic]
    private static object Eight= 8;
    [ThreadStatic]
    private static object Nine= 9;
    [ThreadStatic]
    private static object Ten= 10;
    [ThreadStatic]
    private static object Eleven= 11;
    [ThreadStatic]
    private static object Twelve= 12;
    [ThreadStatic]
    private static object Thirteen= 13;
    [ThreadStatic]
    private static object Fourteen= 14;
    [ThreadStatic]
    private static object Fifteen= 15;
    [ThreadStatic]
    private static object Sixteen= 16;
    [ThreadStatic]
    private static object Seventeen= 17;
    [ThreadStatic]
    private static object Eightteen= 18;
    [ThreadStatic]
    private static object Nineteen= 19;
    [ThreadStatic]
    private static object Twenty= 20;
    [ThreadStatic]
    private static object TwentyOne= 21;
    [ThreadStatic]
    private static object TwentyTwo= 22;
    [ThreadStatic]
    private static object TwentyThree= 23;
    [ThreadStatic]
    private static object TwentyFour= 24;
    [ThreadStatic]
    private static object TwentyFive= 25;
    [ThreadStatic]
    private static object TwentySix= 26;
    [ThreadStatic]
    private static object TwentySeven= 27;
    [ThreadStatic]
    private static object TwentyEight= 28;
    [ThreadStatic]
    private static object TwentyNine= 29;
    [ThreadStatic]
    private static object Thirty= 30;
    [ThreadStatic]
    private static object ThirtyOne= 31;
    [ThreadStatic]
    private static object ThirtyTwo= 32;

    public bool CheckValues()
    {
        if((int)ThirtyTwo != 32)
        {
            Console.WriteLine("Value2 - ThirtySecond spot was incorrect!!!");
            return false;
        }
        
        int value = 0;
        value = (int)One
            + (int)Two
            + (int)Three
            + (int)Four
            + (int)Five
            + (int)Six
            + (int)Seven
            + (int)Eight
            + (int)Nine
            + (int)Ten
            + (int)Eleven
            + (int)Twelve
            + (int)Thirteen
            + (int)Fourteen
            + (int)Fifteen
            + (int)Sixteen
            + (int)Seventeen
            + (int)Eightteen
            + (int)Nineteen
            + (int)Twenty
            + (int)TwentyOne
            + (int)TwentyTwo
            + (int)TwentyThree
            + (int)TwentyFour
            + (int)TwentyFive
            + (int)TwentySix
            + (int)TwentySeven
            + (int)TwentyEight
            + (int)TwentyNine
            + (int)Thirty
            + (int)ThirtyOne
            + (int)ThirtyTwo;

        if(value != 528)
        {
            Console.WriteLine("Wrong values in ThreadStatics!!! {0}",value);
            return false;
        }
        return true;
    }
}

public class Value5
{

    [ThreadStatic]
    private static object One= 1;
    [ThreadStatic]
    private static object Two= 2;
    [ThreadStatic]
    private static object Three= 3;
    [ThreadStatic]
    private static object Four= 4;
    [ThreadStatic]
    private static object Five= 5;
    [ThreadStatic]
    private static object Six= 6;
    [ThreadStatic]
    private static object Seven= 7;
    [ThreadStatic]
    private static object Eight= 8;
    [ThreadStatic]
    private static object Nine= 9;
    [ThreadStatic]
    private static object Ten= 10;
    [ThreadStatic]
    private static object Eleven= 11;
    [ThreadStatic]
    private static object Twelve= 12;
    [ThreadStatic]
    private static object Thirteen= 13;
    [ThreadStatic]
    private static object Fourteen= 14;
    [ThreadStatic]
    private static object Fifteen= 15;
    [ThreadStatic]
    private static object Sixteen= 16;
    [ThreadStatic]
    private static object Seventeen= 17;
    [ThreadStatic]
    private static object Eightteen= 18;
    [ThreadStatic]
    private static object Nineteen= 19;
    [ThreadStatic]
    private static object Twenty= 20;
    [ThreadStatic]
    private static object TwentyOne= 21;
    [ThreadStatic]
    private static object TwentyTwo= 22;
    [ThreadStatic]
    private static object TwentyThree= 23;
    [ThreadStatic]
    private static object TwentyFour= 24;
    [ThreadStatic]
    private static object TwentyFive= 25;
    [ThreadStatic]
    private static object TwentySix= 26;
    [ThreadStatic]
    private static object TwentySeven= 27;
    [ThreadStatic]
    private static object TwentyEight= 28;
    [ThreadStatic]
    private static object TwentyNine= 29;
    [ThreadStatic]
    private static object Thirty= 30;
    [ThreadStatic]
    private static object ThirtyOne= 31;
    [ThreadStatic]
    private static object ThirtyTwo= 32;

    public bool CheckValues()
    {
        if((int)ThirtyTwo != 32)
        {
            Console.WriteLine("Value2 - ThirtySecond spot was incorrect!!!");
            return false;
        }
        
        int value = 0;
        value = (int)One
            + (int)Two
            + (int)Three
            + (int)Four
            + (int)Five
            + (int)Six
            + (int)Seven
            + (int)Eight
            + (int)Nine
            + (int)Ten
            + (int)Eleven
            + (int)Twelve
            + (int)Thirteen
            + (int)Fourteen
            + (int)Fifteen
            + (int)Sixteen
            + (int)Seventeen
            + (int)Eightteen
            + (int)Nineteen
            + (int)Twenty
            + (int)TwentyOne
            + (int)TwentyTwo
            + (int)TwentyThree
            + (int)TwentyFour
            + (int)TwentyFive
            + (int)TwentySix
            + (int)TwentySeven
            + (int)TwentyEight
            + (int)TwentyNine
            + (int)Thirty
            + (int)ThirtyOne
            + (int)ThirtyTwo;

        if(value != 528)
        {
            Console.WriteLine("Wrong values in ThreadStatics!!! {0}",value);
            return false;
        }
        return true;
    }
}

public class MyData
{
    public AutoResetEvent autoEvent;

    [ThreadStatic]
    private static Value0 v0;
    [ThreadStatic]
    private static Value1 v1;
    [ThreadStatic]
    private static Value2 v2;

    [ThreadStatic]
    private static Value3 v3;
    [ThreadStatic]
    private static Value4 v4;
    [ThreadStatic]
    private static Value5 v5;

    public bool pass = false;

    public void ThreadTarget()
    {
        autoEvent.WaitOne();
        v0 = new Value0();
        v1 = new Value1();
        v2 = new Value2();
        v3 = new Value3();
        v4 = new Value4();
        v5 = new Value5();
        pass = v0.CheckValues()
                && v1.CheckValues()
                && v2.CheckValues()
                && v3.CheckValues()
                && v4.CheckValues()
                && v5.CheckValues();
    }
}

public class Test_threadstatic01
{

    private int retVal = 0;

    [Fact]
    public static int TestEntryPoint()
    {
        Test_threadstatic01 staticsTest = new Test_threadstatic01();
        staticsTest.RunTest();
        Console.WriteLine(100 == staticsTest.retVal ? "Test Passed":"Test Failed");
        return staticsTest.retVal;
    }

    public void RunTest()
    {
        MyData data = new MyData();
        data.autoEvent = new AutoResetEvent(false);
        
        Thread t = new Thread(data.ThreadTarget);
        t.Start();
        if(!t.IsAlive)
        {
            Console.WriteLine("Thread was not set to Alive after starting");
            retVal = 50;
            return;
        }
        data.autoEvent.Set();            
        t.Join();
        if(data.pass)
            retVal = 100;
    }

}

