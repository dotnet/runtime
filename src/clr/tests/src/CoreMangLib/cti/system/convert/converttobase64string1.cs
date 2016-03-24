// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToBase64String1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;

    public static int Main()
    {
        ConvertToBase64String1 ac = new ConvertToBase64String1();

        TestLibrary.TestFramework.BeginTestCase("ToBase64String(byte[], int, int)");

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

        return retVal;
    }
                                          //id inArray        length offsetIn exception
    public bool NegTest1() { return NegTest(1, null,           0,     0,      typeof(System.ArgumentNullException)); }
    public bool NegTest2() { return NegTest(2, new byte[0],   -1,     0,      typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest3() { return NegTest(3, new byte[0],    0,    -1,      typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest4() { return NegTest(4, new byte[0],    0,     1,      typeof(System.ArgumentOutOfRangeException)); }
    public bool NegTest5() { return NegTest(5, new byte[0],    1,     0,      typeof(System.ArgumentOutOfRangeException)); }

    public bool PosTest1()
    {
        bool   retVal = true;
        byte[] array;
        string str;

        TestLibrary.TestFramework.BeginScenario("PosTest1: ToBase64String(byte[], int, int)");

        try
        {
            array    = new byte[ (TestLibrary.Generator.GetInt32(-55) % c_MAX_SIZE) + c_MIN_SIZE ];

            // fill the array
            TestLibrary.Generator.GetBytes(-55, array);
            
            str = Convert.ToBase64String(array, 0, array.Length);
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
        string str;

        TestLibrary.TestFramework.BeginScenario("PosTest2: ToBase64String(byte[], int, int) zero length");

        try
        {
            array = new byte[ 0 ];
            str   = Convert.ToBase64String(array, 0, array.Length);

            if (String.Empty != str)
            {
                TestLibrary.TestFramework.LogError("001", "In array length mismatch: Expected(String.Empty) Actual(" + str + ")");
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

    public bool NegTest(int id, byte[] inArray, int length, int offsetIn, Type exception)
    {
        bool            retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": ToBase64String(byte[], int, int) (len:"+length+" in:"+offsetIn+" ex:"+exception+")");

        try
        {
            Convert.ToBase64String(inArray, offsetIn, length);

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
