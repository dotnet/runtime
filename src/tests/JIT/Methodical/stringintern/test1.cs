// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ==, !=

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class
#if XASSEM
Test1_xassem
#else
Test1
#endif
{
    public static string teststr1 = "static \uC09C\u7B8B field";
    public static string[] teststr2 = new string[] { "\u3F2Aarray element 0", "array element 1\uCB53", "array \u47BBelement 2" };
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static string teststr3()
    {
        return @"method return\\";
    }
    public const string teststr4 = "const string\"";  // special case
    public static string teststr5 = String.Empty; // special case

    public static bool TestSameObjRef()
    {
        Console.WriteLine();
        Console.WriteLine("When NGEN'ed, two strings in different modules have different object reference");
        Console.WriteLine("When NGEN'ed, two strings in the same module have same object reference");
        Console.WriteLine("When JIT'ed, two strings always have same object reference");
        Console.WriteLine();
        Console.WriteLine("Testing SameObjRef");

        bool passed = true;

        if ((object)teststr1 != (object)C.teststr1)
        {
            Console.WriteLine("(object)teststr1 == (object)C.teststr1 is expected.  FAILED");
            passed = false;
        }

        if ((object)teststr2[0] != (object)C.teststr2[0])
        {
            Console.WriteLine("(object)teststr2[0] == (object)C.teststr2[0] is expected.  FAILED");
            passed = false;
        }

        if (!Object.ReferenceEquals((object)teststr3(), (object)C.teststr3()))
        {
            Console.WriteLine("Object.ReferenceEquals((object)teststr3(), (object)C.teststr3()) is expected.  FAILED");
            passed = false;
        }

        if ((object)teststr4 != (object)C.teststr4)
        {
            Console.WriteLine("(object)teststr4 == (object)C.teststr4 is expected.  FAILED");
            passed = false;
        }

        if ((object)teststr5 != (object)C.teststr5)
        {
            Console.WriteLine("(object)teststr5 == (object)C.teststr5 is expected.  FAILED");
            passed = false;
        }

        if ((object)teststr1 == (object)GenC<string>.teststr1)
        {
            Console.WriteLine("(object)teststr1 == (object)GenC<string>.teststr1 is not expected.  FAILED");
            passed = false;
        }

        if ((object)teststr2[0] == (object)GenC<string>.teststr2[0])
        {
            Console.WriteLine("(object)teststr2[0] == (object)GenC<string>.teststr2[0] is not expected.  FAILED");
            passed = false;
        }

        if ((object)teststr3() == (object)GenC<string>.teststr3<string>())
        {
            Console.WriteLine("(object)teststr3() == (object)GenC<string>.teststr3<string>() is not expected.  FAILED");
            passed = false;
        }

        if (Object.ReferenceEquals((object)teststr4, (object)GenC<string>.teststr4))
        {
            Console.WriteLine("Object.ReferenceEquals((object)teststr4, (object)GenC<string>.teststr4) is not expected.  FAILED");
            passed = false;
        }

        if ((object)teststr5 != (object)GenC<string>.teststr5)
        {
            Console.WriteLine("(object)teststr5 != (object)GenC<string>.teststr5 is not expected.  FAILED");
            passed = false;
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
