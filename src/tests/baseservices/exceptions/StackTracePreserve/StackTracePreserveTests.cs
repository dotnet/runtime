// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Runtime.ExceptionServices;
using System.IO;
using System.Security;
using Xunit;

public class InactiveForeignException
{
    private static ExceptionDispatchInfo s_EDI = null;
    private static int iPassed = 0, iFailed = 0;
    
    private static Exception GetInnerException()
    {
        try
        {
            throw new ArgumentNullException("InnerException");
        }
        catch (ArgumentNullException ex)
        {
            return ex;
        }
    }

    private static void ThrowEntryPointInner()
    {
        Console.Write("Throwing exception from spawned thread");
        Console.WriteLine("...");
        throw new Exception("E1");
    }
    
    private static void ThrowEntryPoint()
    {
        if (s_EDI == null)
        {
            try
            {
                ThrowEntryPointInner();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Caught exception with message: {0}", ex.Message);
                s_EDI = ExceptionDispatchInfo.Capture(ex);
            }
        }
        else
        {
            Console.WriteLine("s_Exception is not null!");
            s_EDI = null;
        }
    }
    
    
    private static bool Scenario1()
    {
        s_EDI = null;

        Console.WriteLine("\nScenario1");
        Thread t1 = new Thread(new ThreadStart(ThrowEntryPoint));
        t1.Start();
        t1.Join();
        
        bool fPassed = false;
        if (s_EDI == null)
        {
            Console.WriteLine("s_EDI shouldn't be null!");
            goto exit;
        }
        
        // ThrowAndCatch the exception
        try
        {
            s_EDI.Throw();
        }
        catch(Exception ex)
        {
            string stackTrace = ex.StackTrace;
            if (stackTrace.IndexOf("ThrowEntryPoint") == -1)
            {
                Console.WriteLine("FAILED - unable to find expected stackTrace");
            }
            else
            {
                Console.WriteLine("Caught: {0}", ex.ToString());
                Console.WriteLine("Passed");
                fPassed = true;
            }
        }
exit:   
        Console.WriteLine("");
        return fPassed;
    }
    
    private static void Scenario2Helper()
    {
            try
            {
                s_EDI.Throw();
            }
            catch(Exception)
            {
                Console.WriteLine("Rethrowing caught exception..");
                throw;
            }
    }
    
    
    private static bool Scenario2(bool fShouldLetGoUnhandled)
    {
        s_EDI = null;
        
        Console.WriteLine("\nScenario2");
        Thread t1 = new Thread(new ThreadStart(ThrowEntryPoint));
        t1.Start();
        t1.Join();
        
        bool fPassed = false;
        if (s_EDI == null)
        {
            Console.WriteLine("s_EDI shouldn't be null!");
            goto exit;
        }
        
        if (!fShouldLetGoUnhandled)
        {
            // ThrowRethrowAndCatch the exception
            try
            {
                Scenario2Helper();
            }
            catch(Exception ex)
            {
                string stackTrace = ex.StackTrace;
                if ((stackTrace.IndexOf("ThrowEntryPoint") == -1) || 
                    (stackTrace.IndexOf("Scenario2Helper") == -1))
                {
                    Console.WriteLine("FAILED - unable to find expected stackTrace");
                }
                else
                {
                    Console.WriteLine("Caught: {0}", ex.ToString());
                    Console.WriteLine("Passed");
                    fPassed = true;
                }
            }
        }
        else
        {
            // ThrowRethrowAndUnhandled exception
            Scenario2Helper();
        }
exit:        
        Console.WriteLine("");
        return fPassed;
    }

    private static void Scenario3Helper()
    {
        ExceptionDispatchInfo edi = null;

        try
        {
            Scenario2Helper();
        }
        catch (Exception exInner)
        {
            Console.WriteLine("Creating new ExceptionDispatchInfo in Scenario3Helper...");
            edi = ExceptionDispatchInfo.Capture(exInner);
        }

        edi.Throw();
    }

    private static bool Scenario3()
    {
        s_EDI = null;

        Console.WriteLine("\nScenario3");
        Thread t1 = new Thread(new ThreadStart(ThrowEntryPoint2));
        t1.Start();
        t1.Join();

        bool fPassed = false;
        if (s_EDI == null)
        {
            Console.WriteLine("s_EDI shouldn't be null!");
            goto exit;
        }

        // ThrowRethrowCatchCreateNewEDIAndThrow the exception - for multiple preservations
        try
        {
            Scenario3Helper();
        }
        catch (Exception ex)
        {
            string stackTrace = ex.StackTrace;
            if ((stackTrace.IndexOf("ThrowEntryPoint") == -1) ||
                (stackTrace.IndexOf("Scenario2Helper") == -1) ||
                (stackTrace.IndexOf("Scenario3Helper") == -1))
            {
                Console.WriteLine("FAILED - unable to find expected stackTrace");
            }
            else
            {
                Console.WriteLine("Caught: {0}", ex.ToString());
                Console.WriteLine("Passed");
                fPassed = true;
            }
        }
    exit:
        Console.WriteLine("");
        return fPassed;
    }

    private static void ThrowEntryPointNestedHelper()
    {
        if (s_EDI == null)
        {
            try
            {
                ThrowEntryPointInner();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception with message: {0}", ex.Message);
                s_EDI = ExceptionDispatchInfo.Capture(ex);
            }

            // This will preserve the original throw stacktrace and
            // commence a new one.
            s_EDI.Throw();
        }
        else
        {
            Console.WriteLine("s_Exception is not null!");
            s_EDI = null;
        }
    }

    private static void ThrowEntryPoint2()
    {
        if (s_EDI == null)
        {
            try
            {
                ThrowEntryPointNestedHelper();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught: {0}", ex.ToString());
            }
        }
        else
        {
            Console.WriteLine("s_Exception is not null!");
            s_EDI = null;
        }
    }


    private static bool Scenario4()
    {
        s_EDI = null;

        Console.WriteLine("\nScenario4");
        Thread t1 = new Thread(new ThreadStart(ThrowEntryPoint2));
        t1.Start();
        t1.Join();

        bool fPassed = false;
        if (s_EDI == null)
        {
            Console.WriteLine("s_EDI shouldn't be null!");
            goto exit;
        }

        // ThrowCatchCreateEDI_ThrowEDICatch the exception on T1
        // ThrowEDI on T2.
        //
        // We shouldn't see frames post ThrowEDI on T1 when we ThrowEDI on T2.
        try
        {
            s_EDI.Throw();
        }
        catch (Exception ex)
        {
            string stackTrace = ex.StackTrace;
            if ((stackTrace.IndexOf("ThrowEntryPointNestedHelper") == -1) ||
                (stackTrace.IndexOf("Scenario4") == -1) ||
                (stackTrace.IndexOf("ThrowEntryPoint2") != -1))

            {
                Console.WriteLine("FAILED - unable to find expected stackTrace");
            }
            else
            {
                Console.WriteLine("Caught: {0}", ex.ToString());
                Console.WriteLine("Passed");
                fPassed = true;
            }
        }
    exit:
        Console.WriteLine("");
        return fPassed;
    }


    // Use EDI to throw exception during EH dispatch on the same
    // thread for the same exception instance.
    private static bool Scenario5()
    {
        bool fPassed = false;
        ExceptionDispatchInfo edi = null;

        Console.WriteLine("\nScenario5");

        try
        {
            try
            {
                  ThrowEntryPointInner();
            }
            catch(Exception ex)
            {
                edi = ExceptionDispatchInfo.Capture(ex);
                edi.Throw();
            }
        }
        catch(Exception exOuter)
        {
            string stackTrace = exOuter.StackTrace;
            if ((stackTrace.IndexOf("ThrowEntryPointInner") == -1) ||
                (stackTrace.IndexOf("Scenario5") == -1))

            {
                Console.WriteLine("FAILED - unable to find expected stackTrace");
            }
            else
            {
                Console.WriteLine("Caught: {0}", exOuter.ToString());
                Console.WriteLine("Passed");
                fPassed = true;
            }
        }

        return fPassed;
    }

        // Use EDI to throw an unthrown exception.
    private static bool Scenario6()
    {
        bool fPassed = false;
        ExceptionDispatchInfo edi = null;
        
        Console.WriteLine("\nScenario6");

        try
        {
            edi = ExceptionDispatchInfo.Capture(new Exception("Unthrown exception"));
            edi.Throw();
        }
        catch(Exception exOuter)
        {
            string stackTrace = exOuter.StackTrace;
            if ((stackTrace.IndexOf("Scenario6") == -1))

            {
                Console.WriteLine("FAILED - unable to find expected stackTrace");
            }
            else
            {
                Console.WriteLine("Caught: {0}", exOuter.ToString());
                Console.WriteLine("Passed");
                fPassed = true;
            }
        }

        return fPassed;
    }
    
    // Scenario 7 - Attempt to create EDI using a null reference throws
    // ArgumentNullException.
    private static bool Scenario7()
    {
        bool fPassed = false;
        Console.WriteLine("\nScenario7");
        
        try{
            try{
                ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(null);
            }
            catch(ArgumentNullException)
            {
                fPassed = true;
            }
        }
        catch(Exception)
        {
        }
        
        Console.WriteLine("{0}", (fPassed)?"Passed":"Failed");
        
        return fPassed;
    }
    

    private static void Scenario9HelperInner()
    {
        try
        {
            Console.WriteLine("Throwing from Scenario9Helper...");
            s_EDI.Throw();
        }
        finally
        {
            ;
        }
    }

    private static void Scenario9Helper()
    {
        try
        {
            Scenario9HelperInner();
        }
        catch (Exception)
        {
            Console.WriteLine("Rethrowing...");
            throw;
        }
    }

    private static bool Scenario9()
    {
        s_EDI = null;

        Console.WriteLine("\nScenario9");
        Thread t1 = new Thread(new ThreadStart(ThrowEntryPoint));
        t1.Start();
        t1.Join();

        bool fPassed = false;
        if (s_EDI == null)
        {
            Console.WriteLine("s_EDI shouldn't be null!");
            goto exit;
        }

        string s1 = null, s2 = null;
        try
        {
            Scenario9Helper();
        }
        catch (Exception ex)
        {
            s1 = ex.ToString();
        }

        try
        {
            Console.WriteLine("Triggering 2nd Throw...");
            s_EDI.Throw();
        }
        catch (Exception ex)
        {
            s2 = ex.ToString();
        }

        // S1 should have Scenario9HelperInner, Scenario9Helper and Scenario9 frames, in addition to the original frames.
        // S2 should have Scenario9 frame, in addition to the original frames.
        if ((s1.IndexOf("Scenario9HelperInner") == -1) || (s1.IndexOf("Scenario9Helper") == -1) ||
            (s2.IndexOf("Scenario9HelperInner") != -1) || (s2.IndexOf("Scenario9Helper") != -1))
        {
            Console.WriteLine("S1: {0}\n", s1);
            Console.WriteLine("S2: {0}", s2);
            Console.WriteLine("FAILED");

        }
        else
        {
            Console.WriteLine("S1: {0}\n", s1);
            Console.WriteLine("S2: {0}", s2);
            Console.WriteLine("Passed");
            fPassed = true;
        }

    exit:
        Console.WriteLine("");
        return fPassed;
    }

    private static void ProcessStatus(bool fPassed)
    {
        if (fPassed)
            iPassed++;
        else
            iFailed++;   

    }

    
    [Fact]
    public static int TestEntryPoint()
    {
        iPassed = iFailed = 0;

        
        ProcessStatus(Scenario1());
        ProcessStatus(Scenario2(false));
        ProcessStatus(Scenario3());
        ProcessStatus(Scenario4());
        ProcessStatus(Scenario5());
        ProcessStatus(Scenario6());
        ProcessStatus(Scenario7());
        ProcessStatus(Scenario9());
        


        // This is the unhandled exception case
        //ProcessStatus(Scenario2(true));


        Console.WriteLine("\nPassed: {0}\nFailed: {1}", iPassed, iFailed);

        return ((iFailed == 0) && (iPassed > 0))?100:99;
        
    }
}
