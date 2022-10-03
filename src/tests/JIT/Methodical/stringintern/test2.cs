// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// switch, for

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class
#if XASSEM
Test2_xassem
#else
Test2
#endif
{
    public static string[] teststr2 = new string[] { "\u3F2Aarray element 0", "array element 1\uCB53", "array \u47BBelement 2" };

    public static bool TestSameObjRef()
    {
        Console.WriteLine();
        Console.WriteLine("When NGEN'ed, two strings in different modules have different object reference");
        Console.WriteLine("When NGEN'ed, two strings in the same module have same object reference");
        Console.WriteLine("When JIT'ed, two strings always have same object reference");
        Console.WriteLine();
        Console.WriteLine("Testing SameObjRef");

        bool passed = true;

        switch (C.teststr1)
        {
            case "static \uC09C\u7B8B field":
                switch (GenC<object>.teststr2[0])
                {
                    case "\u3F2Aarray element 0":
                        passed = false;
                        break;
                    default:
                        break;
                }
                break;

            default:
                passed = false;
                break;
        }

        switch (C.teststr3())
        {
            case @"method return\\":
                switch (GenC<string>.teststr5)
                {
                    case "":
                        switch (C.teststr3())
                        {
                            case @"method return\\":
                                break;
                            default:
                                passed = false;
                                break;
                        }
                        break;
                    default:
                        passed = false;
                        break;
                }
                break;
            default:
                passed = false;
                break;
        }

        int i;
        for (i = 1; (i < teststr2.Length) && (object)C.teststr2[i] == (object)teststr2[i]; i++)
            ;
        if (i != teststr2.Length)
        {
            Console.WriteLine("for, (object)C.teststr2[i]==(object)teststr2[i] is not expected, FAILED");
            passed = false;
        }

        switch (GenC<string>.teststr1)
        {
            case "static \uC09C\u7B8B field":
                passed = false;
                break;
            default:
                switch (GenC<string>.teststr2[0])
                {
                    case "GenC \u3F2Aarray element 0":
                        break;
                    default:
                        passed = false;
                        break;
                }
                break;
        }

        switch (GenC<int>.teststr3<int>())
        {
            case @"GenC method return\\":
                switch (GenC<string>.teststr4)
                {
                    case "GenC const string\"":
                        break;
                    default:
                        passed = false;
                        break;
                }
                break;
            default:
                passed = false;
                break;
        }

        for (i = 1; (i < teststr2.Length) && (object)GenC<object>.teststr2[i] != (object)C.teststr2[i]; i++)
            ;
        if (i != teststr2.Length)
        {
            Console.WriteLine("for, (object)GenC<object>.teststr2[i]!=(object)C.teststr2[i] is not expected, FAILED");
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
