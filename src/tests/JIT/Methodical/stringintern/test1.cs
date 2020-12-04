// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ==, !=

using System;
using System.Runtime.CompilerServices;

class Test1
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

    public static bool TestDiffObjRef()
    {
        Console.WriteLine();
        Console.WriteLine("When NGEN'ed, two strings in different modules have different object reference");
        Console.WriteLine("When NGEN'ed, two strings in the same module have same object reference");
        Console.WriteLine("When JIT'ed, two strings always have same object reference");
        Console.WriteLine();
        Console.WriteLine("Testing DiffObjRef");

        bool passed = true;

        if ((object)teststr1 == (object)C.teststr1)
        {
            Console.WriteLine("(object)teststr1 == (object)C.teststr1 is not expected.  FAILED");
            passed = false;
        }

        if ((object)teststr2[0] == (object)C.teststr2[0])
        {
            Console.WriteLine("(object)teststr2[0] == (object)C.teststr2[0] is not expected.  FAILED");
            passed = false;
        }

        if (Object.ReferenceEquals((object)teststr3(), (object)C.teststr3()))
        {
            Console.WriteLine("(object)teststr3() == (object)C.teststr3() is not expected.  FAILED");
            passed = false;
        }

        // Special case for const literal teststr4
        // two consecutive LDSTR is emitted by C# compiler for the following statement
        // as a result, both are interned in the same module and object comparison returns true
        if ((object)teststr4 != (object)C.teststr4)
        {
            Console.WriteLine("(object)teststr4 != (object)C.teststr4 is not expected.  FAILED");
            passed = false;
        }

        // Special case for String.Empty
        // String.Empty is loaded using LDSFLD, rather than LDSTR in any module
        // as a result, it is always the same reference to [mscorlib]System.String::Empty,
        // and object comparison return true
        if ((object)teststr5 != (object)C.teststr5)
        {
            Console.WriteLine("(object)teststr5 != (object)C.teststr5 is not expected.  FAILED");
            passed = false;
        }

        if ((object)"GenC static \uC09C\u7B8B field" == (object)GenC<string>.teststr1)
        {
            Console.WriteLine("(object)\"GenC static \uC09C\u7B8B field\" == (object)GenC<string>.teststr1 is not expected.  FAILED");
            passed = false;
        }

        if ((object)"GenC \u3F2Aarray element 0" == (object)GenC<string>.teststr2[0])
        {
            Console.WriteLine("(object)\"GenC \u3F2Aarray element 0\" == (object)GenC<string>.teststr2[0] is not expected.  FAILED");
            passed = false;
        }

        if ((object)@"GenC method return\\" == (object)GenC<string>.teststr3<string>())
        {
            Console.WriteLine("(object)\"GenC method return\\\" == (object)GenC<string>.teststr3<string>() is not expected.  FAILED");
            passed = false;
        }

        // Special case for const literal teststr4
        // two consecutive LDSTR is emitted by C# compiler for the following statement
        // as a result, both are interned in the same module and object comparison returns true
        if (!Object.ReferenceEquals((object)"GenC const string\"", (object)GenC<string>.teststr4))
        {
            Console.WriteLine("(object)\"GenC const string\"\" != (object)GenC<string>.teststr4 is not expected.  FAILED");
            passed = false;
        }

        // Special case for String.Empty
        // String.Empty is loaded using LDSFLD, rather than LDSTR in any module
        // as a result, it is always the same reference to [mscorlib]System.String::Empty,
        // and object comparison return true
        if ((object)teststr5 != (object)GenC<string>.teststr5)
        {
            Console.WriteLine("(object)teststr5 != (object)GenC<string>.teststr5 is not expected.  FAILED");
            passed = false;
        }

        return passed;
    }

    public static int Main(string[] args)
    {
        bool passed = false;

        if ((args.Length < 1) || (args[0].ToUpper() == "SAMEOBJREF"))
            passed = TestSameObjRef();
        else if (args[0].ToUpper() == "DIFFOBJREF")
            passed = TestDiffObjRef();
        else
        {
            Console.WriteLine("Usage: Test1.exe [SameObjRef|DiffObjRef]");
            Console.WriteLine();
            Console.WriteLine("When NGEN'ed, two strings in different modules have different object reference");
            Console.WriteLine("When NGEN'ed, two strings in the same module have same object reference");
            Console.WriteLine("When JIT'ed, two strings always have same object reference");
            Console.WriteLine();
            return 9;
        }

        Console.WriteLine();
        if (passed)
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
