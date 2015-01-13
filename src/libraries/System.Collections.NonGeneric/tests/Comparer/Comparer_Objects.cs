// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class Comparer_Objects
{
    /////////////////////////////////////////////////////////////////////
    // Begin Testing run
    /////////////////////////////////////////////////////////////////////
    public Boolean runTest()
    {
        int iCountTestcases = 0;
        int iCountErrors = 0;
        Object obj1, obj2;
        int compRet;
        Comparer comp;

        //------------------- TEST 1

        //[]use Comparer to compare two different objects
        try
        {
            ++iCountTestcases;
            if (Comparer.Default.Compare(2, 1) <= 0)
            {
                ++iCountErrors;
                Console.WriteLine("Err_001a,  compare should have returned positive integer");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_001b,  Unexpected exception was thrown ex: " + ex.ToString());
        }

        //------------------- TEST 2

        //[]use Comparer to compare two different objects
        try
        {
            ++iCountTestcases;
            if (Comparer.Default.Compare(1, 2) >= 0)
            {
                ++iCountErrors;
                Console.WriteLine("Err_002a,  compare should have returned negative integer");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_002b,  Unexpected exception was thrown ex: " + ex.ToString());
        }


        //------------------- TEST 3

        //[]use Comparer to compare two equal objects
        try
        {
            ++iCountTestcases;
            if (Comparer.Default.Compare(1, 1) != 0)
            {
                ++iCountErrors;
                Console.WriteLine("Err_003a,  compare should have returned 0 for equal objects");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_003b,  Unexpected exception was thrown ex: " + ex.ToString());
        }

        //------------------- TEST 4

        //[]use Comparer to compare a null object to a non null object
        try
        {
            ++iCountTestcases;
            if (Comparer.Default.Compare(null, 1) >= 0)
            {
                ++iCountErrors;
                Console.WriteLine("Err_004a,  a null object should be always less than something else");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_004b,  Unexpected exception was thrown ex: " + ex.ToString());
        }

        //------------------- TEST 5

        //[]use Comparer to compare an object to a null object
        try
        {
            ++iCountTestcases;
            if (Comparer.Default.Compare(0, null) <= 0)
            {
                ++iCountErrors;
                Console.WriteLine("Err_005a,  a null object should be always less than something else");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_005b,  Unexpected exception was thrown ex: " + ex.ToString());
        }

        //------------------- TEST 6

        //[]for two null objects comparer returns equal
        try
        {
            ++iCountTestcases;
            if (Comparer.Default.Compare(null, null) != 0)
            {
                ++iCountErrors;
                Console.WriteLine("Err_006a,  two nulls should be equal");
            }
        }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_006b,  Unexpected exception was thrown ex: " + ex.ToString());
        }

        ///////////////////////////////////// NOW PASS IN THINGS THAT CANNOT BE COMPARED /////////////////

        //------------------- TEST 7

        //[]compare two objects that do not implement IComparable
        try
        {
            ++iCountTestcases;
            Comparer.Default.Compare(new Object(), new Object());

            ++iCountErrors;
            Console.WriteLine("Err_007a,  Expected exception ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        { }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_007b,  Unexpected exception was thrown ex: " + ex.ToString());
        }


        //------------------- TEST 8

        //[]compare two objects that are not of the same type
        try
        {
            ++iCountTestcases;
            Comparer.Default.Compare(1L, 1);

            ++iCountErrors;
            Console.WriteLine("Err_008a,  Expected exception ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        { }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_008b,  Unexpected exception was thrown ex: " + ex.ToString());
        }

        //------------------- TEST 9

        //[]compare two objects that are not of the same type
        try
        {
            ++iCountTestcases;
            Comparer.Default.Compare(1, 1L);

            ++iCountErrors;
            Console.WriteLine("Err_009a,  Expected exception ArgumentException was not thrown.");
        }
        catch (ArgumentException)
        { }
        catch (Exception ex)
        {
            ++iCountErrors;
            Console.WriteLine("Err_009b,  Unexpected exception was thrown ex: " + ex.ToString());
        }


        //[]Verify Compare where only one object implements IComparable
        //and exception is expected from incompatible types
        comp = Comparer.Default;

        obj1 = "Aa";
        obj2 = new Object();

        try
        {
            compRet = comp.Compare(obj1, obj2);
            iCountErrors++;
            Console.WriteLine("Err_37035ashz! Expected exception to be thrown");
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_3407pahs! Unexpected exception thrown {0}", e);
        }

        try
        {
            compRet = comp.Compare(obj2, obj1);
            iCountErrors++;
            Console.WriteLine("Err_0777paya! Expected exception to be thrown");
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_73543aha! Unexpected exception thrown {0}", e);
        }

        //[]Verify Compare where neither object implements IComparable
        comp = Comparer.Default;

        obj1 = new Object();
        obj2 = new Object();

        try
        {
            compRet = comp.Compare(obj1, obj2);
            iCountErrors++;
            Console.WriteLine("Err_22435asps! Expected exception to be thrown");
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_5649asdh! Unexpected exception thrown {0}", e);
        }

        try
        {
            compRet = comp.Compare(obj2, obj1);
            iCountErrors++;
            Console.WriteLine("Err_9879nmmj! Expected exception to be thrown");
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            iCountErrors++;
            Console.WriteLine("Err_9881qa! Unexpected exception thrown {0}", e);
        }

        //[]Verify Compare where only one object implements IComparable
        //and it will handle the conversion
        comp = Comparer.Default;

        obj1 = new Foo(5);
        obj2 = new Bar(5);

        if (0 != comp.Compare(obj1, obj2))
        {
            iCountErrors++;
            Console.WriteLine("Err_3073ahsk! Expected Compare to return 0 Compare(obj1, obj2)={0}, Compare(obj2, obj1)={1}",
                             comp.Compare(obj1, obj2), comp.Compare(obj2, obj1));
        }

        obj1 = new Foo(1);
        obj2 = new Bar(2);

        if (0 <= comp.Compare(obj1, obj2))
        {
            iCountErrors++;
            Console.WriteLine("Err_8922ayps! Expected Compare to return -1 Compare(obj1, obj2)={0}, Compare(obj2, obj1)={1}",
                             comp.Compare(obj1, obj2), comp.Compare(obj2, obj1));
        }


        if (iCountErrors == 0)
        {
            return true;
        }
        else
        {
            Console.WriteLine("Fail! iCountErrors=" + iCountErrors.ToString());
            return false;
        }
    }



    [Fact]
    public static void ExecuteComparer_Objects()
    {
        var runClass = new Comparer_Objects();
        Boolean bResult = runClass.runTest();

        Assert.Equal(true, bResult);
    }
}  


public class Foo : IComparable
{
    public int Data;

    public Foo(int i)
    {
        Data = i;
    }


    public int CompareTo(Object o)
    {
        if (o is Foo)
        {
            return Data.CompareTo(((Foo)o).Data);
        }
        else if (o is Bar)
        {
            return Data.CompareTo(((Bar)o).Data);
        }

        throw new ArgumentException("Object is not a Foo or a Bar");
    }
}

public class Bar
{
    public int Data;

    public Bar(int i)
    {
        Data = i;
    }
}
