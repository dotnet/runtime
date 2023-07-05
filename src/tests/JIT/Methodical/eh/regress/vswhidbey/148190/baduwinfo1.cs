// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test_baduwinfo1
{
    private static TestUtil.TestLog testLog;

    static Test_baduwinfo1()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine("in the try block");
        expectedOut.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18}  {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30} {31} {32} ", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33);
        expectedOut.WriteLine("Expected Test Exception");
        expectedOut.WriteLine("in catch now");
        expectedOut.WriteLine("in finally now");

        // Create and initialize test log object
        testLog = new TestUtil.TestLog(expectedOut);
    }
    public static int bar(String i1, String i2, String i3, String i4, String i5, String i6, String i7, String i8, String i9, String i10, String i11, String i12, String i13, String i14, String i15, String i16, String i17, String i18, String i19, String i20, String i21, String i22, String i23, String i24, String i25, String i26, String i27, String i28, String i29, String i30, String i31, String i32, String i33)
    {
        Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18}  {19} {20} {21} {22} {23} {24} {25} {26} {27} {28} {29} {30} {31} {32} ", i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12, i13, i14, i15, i16, i17, i18, i19, i20, i21, i22, i23, i24, i25, i26, i27, i28, i29, i30, i31, i32, i33);
        if (i1 != "1") { Console.WriteLine("Test for i1 failed"); return -1; }

        if (i2 != "2") { Console.WriteLine("Test for i2 failed"); return -1; }

        if (i3 != "3") { Console.WriteLine("Test for i3 failed"); return -1; }

        if (i4 != "4") { Console.WriteLine("Test for i4 failed"); return -1; }

        if (i5 != "5") { Console.WriteLine("Test for i5 failed"); return -1; }

        if (i6 != "6") { Console.WriteLine("Test for i6 failed"); return -1; }

        if (i7 != "7") { Console.WriteLine("Test for i7 failed"); return -1; }

        if (i8 != "8") { Console.WriteLine("Test for i8 failed"); return -1; }

        if (i9 != "9") { Console.WriteLine("Test for i9 failed"); return -1; }

        if (i10 != "10") { Console.WriteLine("Test for i10 failed"); return -1; }

        if (i11 != "11") { Console.WriteLine("Test for i11 failed"); return -1; }

        if (i12 != "12") { Console.WriteLine("Test for i12 failed"); return -1; }

        if (i13 != "13") { Console.WriteLine("Test for i13 failed"); return -1; }

        if (i14 != "14") { Console.WriteLine("Test for i14 failed"); return -1; }

        if (i15 != "15") { Console.WriteLine("Test for i15 failed"); return -1; }

        if (i16 != "16") { Console.WriteLine("Test for i16 failed"); return -1; }

        if (i17 != "17") { Console.WriteLine("Test for i17 failed"); return -1; }

        if (i18 != "18") { Console.WriteLine("Test for i18 failed"); return -1; }

        if (i19 != "19") { Console.WriteLine("Test for i19 failed"); return -1; }

        if (i20 != "20") { Console.WriteLine("Test for i20 failed"); return -1; }

        if (i21 != "21") { Console.WriteLine("Test for i21 failed"); return -1; }

        if (i22 != "22") { Console.WriteLine("Test for i22 failed"); return -1; }

        if (i23 != "23") { Console.WriteLine("Test for i23 failed"); return -1; }

        if (i24 != "24") { Console.WriteLine("Test for i24 failed"); return -1; }

        if (i25 != "25") { Console.WriteLine("Test for i25 failed"); return -1; }

        if (i26 != "26") { Console.WriteLine("Test for i26 failed"); return -1; }

        if (i27 != "27") { Console.WriteLine("Test for i27 failed"); return -1; }

        if (i28 != "28") { Console.WriteLine("Test for i28 failed"); return -1; }

        if (i29 != "29") { Console.WriteLine("Test for i29 failed"); return -1; }

        if (i30 != "30") { Console.WriteLine("Test for i30 failed"); return -1; }

        if (i31 != "31") { Console.WriteLine("Test for i31 failed"); return -1; }

        if (i32 != "32") { Console.WriteLine("Test for i32 failed"); return -1; }

        if (i33 != "33") { Console.WriteLine("Test for i33 failed"); return -1; }


        throw new System.Exception("Expected Test Exception");
    }
    public static int foo()
    {
        int ret = 1;

        try
        {
            Console.WriteLine("in the try block");
            ret = bar("1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine("in catch now");
            ret = 100;
        }
        finally
        {
            Console.WriteLine("in finally now");
        }
        return ret;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        String s = "Test1";
        testLog.StartRecording();


        try
        {
            if (100 != foo()) Console.WriteLine("foo() Failed");
            if (s != "Test1") Console.WriteLine("s!=\"Test_baduwinfo1\" Failed");
        }
        catch
        {
            Console.WriteLine("in catch");
        }
        testLog.StopRecording();
        return testLog.VerifyOutput();
    }
}
