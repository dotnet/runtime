// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// try/catch, try/finally

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class
#if XASSEM
Test4_xassem
#else
Test4
#endif
{
    public static string teststr1 = null;
    public static string[] teststr2 = new string[3];
    public static string teststr3 = null;
    public const string teststr4 = "const string\"";	// special case for DiffObjRef
    public const string testgenstr4 = "GenC const string\"";  // special case for DiffObjRef
    public static string teststr5 = null;  // special case for DiffObjRef

    public static bool TestSameObjRef()
    {
        Console.WriteLine();
        Console.WriteLine("When NGEN'ed, two strings in different modules have different object reference");
        Console.WriteLine("When NGEN'ed, two strings in the same module have same object reference");
        Console.WriteLine("When JIT'ed, two strings always have same object reference");
        Console.WriteLine();
        Console.WriteLine("Testing SameObjRef");

        bool passed = true;

        string b = null;

        try
        {
            teststr1 = "static \uC09C\u7B8B field";
            b = C.teststr1;
            throw new Exception();
        }
        catch (System.Exception)
        {
        }

        if ((object)teststr1 != (object)b)
        {
            passed = false;
            Console.WriteLine("FAILED, (object) teststr1 == (object) b is expected");
        }

        try
        {
            teststr2[0] = "\u3F2Aarray element 0";
            b = C.teststr2[0];
            throw new Exception();
        }
        catch (System.Exception)
        {
            if ((object)teststr2[0] != (object)C.teststr2[0])
            {
                passed = false;
                Console.WriteLine("FAILED, (object) teststr2[0] == (object)C.teststr2[0] is expected");
            }
        }

        try
        {
            throw new Exception();
        }
        catch (System.Exception)
        {
            teststr2[1] = "array element 1\uCB53";
            b = C.teststr2[1];
        }

        if ((object)teststr2[1] != (object)b)
        {
            passed = false;
            Console.WriteLine("FAILED, (object) teststr2[1] == (object) b is expected");
        }

        try
        {
            throw new Exception();
        }
        catch (System.Exception)
        {
        }
        finally
        {
            teststr2[2] = "array \u47BBelement 2";
        }

        if ((object)teststr2[2] != (object)C.teststr2[2])
        {
            passed = false;
            Console.WriteLine("FAILED, (object)teststr2[2] == (object)C.teststr2[2] is expected");
        }

        try
        {
            teststr3 = @"method return\\";
            throw new Exception();
        }
        catch (System.Exception)
        {
            if ((object)teststr3 != (object)C.teststr3())
            {
                passed = false;
                Console.WriteLine("FAILED, (object) teststr3 == (object)C.teststr3() is expected");
            }
            try
            {
            }
            finally
            {
                if ((object)teststr4 != (object)C.teststr4)
                {
                    passed = false;
                    Console.WriteLine("FAILED, (object)teststr4 != (object)C.teststr4  is expected");
                }
                try
                {
                    throw new Exception();
                }
                catch
                {
                }
                finally
                {
                    teststr5 = String.Empty;
                    if ((object)teststr5 != (object)C.teststr5)
                    {
                        passed = false;
                        Console.WriteLine("FAILED, (object) teststr5 != (object)C.teststr5  is expected");
                    }
                }
            }
        }

        // Generic Class
        try
        {
            teststr1 = "GenC static \uC09C\u7B8B field";
            b = GenC<string>.teststr1;
            throw new Exception();
        }
        catch (System.Exception)
        {
        }

        if ((object)teststr1 != (object)b)
        {
            passed = false;
            Console.WriteLine("FAILED, (object)teststr1 == (object)GenC<string>.teststr1 is expected");
        }

        try
        {
            teststr2[0] = "GenC \u3F2Aarray element 0";
            throw new Exception();
        }
        catch (System.Exception)
        {
            if ((object)teststr2[0] != (object)GenC<string>.teststr2[0])
            {
                passed = false;
                Console.WriteLine("FAILED, (object) teststr2[0] == (object)GenC<string>.teststr2[0] is expected");
            }
        }

        try
        {
            throw new Exception();
        }
        catch (System.Exception)
        {
            teststr2[1] = "GenC array element 1\uCB53";
            b = GenC<string>.teststr2[1];
        }

        if ((object)teststr2[1] != (object)b)
        {
            passed = false;
            Console.WriteLine("FAILED, (object) teststr2[1] == (object)GenC<string>.teststr2[1] is expected");
        }

        try
        {
            throw new Exception();
        }
        catch (System.Exception)
        {
        }
        finally
        {
            teststr2[2] = "GenC array \u47BBelement 2";
        }

        if ((object)teststr2[2] != (object)GenC<string>.teststr2[2])
        {
            passed = false;
            Console.WriteLine("FAILED, (object)teststr2[2] == (object)GenC<string>.teststr2[2] is expected");
        }

        try
        {
            teststr3 = @"GenC method return\\";
            throw new Exception();
        }
        catch (System.Exception)
        {
            if ((object)teststr3 != (object)GenC<string>.teststr3<int>())
            {
                passed = false;
                Console.WriteLine("FAILED, (object) teststr3 == (object)GenC<string>.teststr3<int>() is expected");
            }
            try
            {
            }
            finally
            {
                if ((object)testgenstr4 != (object)GenC<string>.teststr4)
                {
                    passed = false;
                    Console.WriteLine("FAILED, (object)testgenstr4 != (object)GenC<string>.teststr4  is expected");
                }
                try
                {
                    throw new Exception();
                }
                catch
                {
                }
                finally
                {
                    teststr5 = String.Empty;
                    if ((object)teststr5 != (object)GenC<string>.teststr5)
                    {
                        passed = false;
                        Console.WriteLine("FAILED, (object) teststr5 != (object)GenC<string>.teststr5  is expected");
                    }
                }
            }
        }

        return passed;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (TestSameObjRef())
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }

    }
}
