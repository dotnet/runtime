// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;

public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    volatile int myField;
    volatile Object myObjectField;

    public BringUpTest()
    {
        myField = 1;
    }

    static BringUpTest g = null;

    static int finallyCounter = 0;

    public static int Main()
    {
        if (string.Empty.Length > 0)
        {
            // Just something to make sure we generate reflection metadata for the type
            new BringUpTest().ToString();
        }

        int counter = 0;
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;

        try
        {
            try
            {
                throw new Exception("My exception");
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("Unexpected exception caught");
                return Fail;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught!");
            if (e.Message != "My exception")
            {
                 Console.WriteLine("Unexpected exception message!");
                 return Fail;
            }

            string stackTrace = e.StackTrace;
            if (!stackTrace.Contains("BringUpTest.Main"))
            {
                Console.WriteLine("Unexpected stack trace: " + stackTrace);
                return Fail;
            }
            counter++;
        }

        try
        {
             g.myObjectField = new Object();
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Null reference exception in write barrier caught!");
            counter++;
        }

        try
        {
             try
             {
                 g.myField++;
             }
             finally
             {
                 counter++;
             }
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("Null reference exception caught!");
            counter++;
        }

        try
        {
            throw new Exception("Testing filter");
        }
        catch (Exception e) when (FilterWithStackTrace(e) && counter++ > 0)
        {
            Console.WriteLine("Exception caught via filter!");
            if (e.Message != "Testing filter")
            {
                 Console.WriteLine("Unexpected exception message!");
                 return Fail;
            }
            counter++;
        }

        // test interaction of filters and finally clauses with GC
        try
        {
            ThrowExcThroughMethodsWithFinalizers1("Main");
        }
        catch (Exception e) when (FilterWithGC() && counter++ > 0)
        {
            Console.WriteLine(e.Message);
            if (e.Message != "ThrowExcThroughMethodsWithFinalizers2")
            {
                Console.WriteLine("Unexpected exception message!");
                return Fail;
            }
            if (finallyCounter != 2)
            {
                Console.WriteLine("Finalizers didn't execute!");
                return Fail;
            }
            counter++;
        }

        try
        {
            try
            {
                throw new Exception("Hello");
            }
            catch
            {
                counter++;
                throw;
            }
        }
        catch (Exception ex)
        {
            if (ex.Message != "Hello")
                return Fail;
            counter++;
        }

        if (counter != 10)
        {
            Console.WriteLine("Unexpected counter value");
            return Fail;
        }

        throw new Exception("UnhandledException");

        return Fail;
    }

    static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine("Exception triggered UnhandledExceptionHandler");
        if (e.ExceptionObject is Exception ex && ex.Message == "UnhandledException")
        {
            Environment.Exit(Pass);
        }

        Console.WriteLine("Unexpected exception!");

        Environment.Exit(Fail);
    }

    static void CreateSomeGarbage()
    {
        for (int i = 0; i < 100; i++)
        {
            string s = new string('.', 100);
        }
    }

    static void ThrowExcThroughMethodsWithFinalizers1(string caller)
    {
        CreateSomeGarbage();
        string s = caller + " + ThrowExcThroughMethodsWithFinalizers1";
        CreateSomeGarbage();
        try
        {
            ThrowExcThroughMethodsWithFinalizers2(s);
        }
        finally
        {
            Console.WriteLine("Executing finally in {0}", s);
            finallyCounter++;
        }
    }

    static void ThrowExcThroughMethodsWithFinalizers2(string caller)
    {
        CreateSomeGarbage();
        string s = caller + " + ThrowExcThroughMethodsWithFinalizers2";
        CreateSomeGarbage();
        try
        {
            throw new Exception("ThrowExcThroughMethodsWithFinalizers2");
        }
        finally
        {
            Console.WriteLine("Executing finally in {0}", s);
            finallyCounter++;
        }
    }

    static bool FilterWithStackTrace(Exception e)
    {
        var stackTrace = new StackTrace(0, true);
        Console.WriteLine("Test Stacktrace with exception on stack:");
        Console.WriteLine(stackTrace);
        return e.Message == "Testing filter";
    }

    static bool FilterWithGC()
    {
        CreateSomeGarbage();
        GC.Collect();
        CreateSomeGarbage();
        return true;
    }
}

