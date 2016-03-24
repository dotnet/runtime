// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToBase64CharArray
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;

    public static int Main()
    {
        ConvertToBase64CharArray ac = new ConvertToBase64CharArray();

        TestLibrary.TestFramework.BeginTestCase("ToBase64CharArray(byte[], int, int, char[], int)");

        if (ac.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;
        retVal = NegTest8() && retVal;

        return retVal;
    }

                                          //id inArray        outArray        length offsetIn offsetOut exception
    public bool NegTest1() { return NegTest(1, null,          null,            0,     0,       0,       typeof(System.ArgumentNullException)); }
    public bool NegTest2() { return NegTest(2, new byte[0],   null,            0,     0,       0,       typeof(System.ArgumentNullException)); }
    public bool NegTest3() { return NegTest(3, new byte[0],   new char[0],    -1,     0,       0,       typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest4() { return NegTest(4, new byte[0],   new char[0],     0,    -1,       0,       typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest5() { return NegTest(5, new byte[0],   new char[0],     0,     0,      -1,       typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest6() { return NegTest(6, new byte[0],   new char[0],     0,    10,       0,       typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest7() { return NegTest(7, new byte[1],   new char[1],    10,     0,      10,       typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest8() { return NegTest(8, new byte[1],   new char[1],     1,     0,      10,       typeof(System.ArgumentOutOfRangeException)); }

    public bool PosTest1()
    {
        bool   retVal = true;
        byte[] array;
        char[] outArray;
        int    numBytes;

        TestLibrary.TestFramework.BeginScenario("PosTest1: ToBase64CharArray(byte[], int, int, char[], int)");

        try
        {
            array    = new byte[ (TestLibrary.Generator.GetInt32(-55) % c_MAX_SIZE) + c_MIN_SIZE ];
            outArray = new char[ array.Length*3 ];

            // fill the array
            TestLibrary.Generator.GetBytes(-55, array);
            
            numBytes = Convert.ToBase64CharArray(array, 0, array.Length, outArray, 0);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("000", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool   retVal = true;
        byte[] array;
        char[] outArray;
        int    numBytes;

        TestLibrary.TestFramework.BeginScenario("PosTest2: ToBase64CharArray(byte[], int, int, char[], int) zero length");

        try
        {
            array    = new byte[ 0 ];
            outArray = new char[ array.Length ];

            numBytes = Convert.ToBase64CharArray(array, 0, array.Length, outArray, 0);

            if (numBytes != array.Length)
            {
                TestLibrary.TestFramework.LogError("001", "In array length mismatch: Expected(" + array.Length + ") Actual(" + numBytes + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest(int id, byte[] inArray, char[] outArray, int length, int offsetIn, int offsetOut, Type exception)
    {
        bool            retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": ToBase64CharArray(byte[], int, int, char[], int) (len:"+length+" in:"+offsetIn+" out:"+offsetOut+" ex:"+exception+")");

        try
        {
            Convert.ToBase64CharArray(inArray, offsetIn, length, outArray, offsetOut);

            TestLibrary.TestFramework.LogError("003", "Exception expected");
            retVal = false;
        }
        catch (Exception e)
        {
            if (exception != e.GetType())
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }
        }

        return retVal;
    }
}
