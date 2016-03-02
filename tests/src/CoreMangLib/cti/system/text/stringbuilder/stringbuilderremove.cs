// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Text.StringBuilder.Remove
/// </summary>
public class StringBuilderRemove
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        StringBuilderRemove test = new StringBuilderRemove();

        TestLibrary.TestFramework.BeginTestCase("for Method:System.Text.StringBuilder.Remove(indexStart,length)");

        if (test.RunTests())
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;


        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
       
        return retVal;
    }

    
    public bool PosTest1()
    {
        bool    retVal      = true;

        string  oldString       = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int     startIndex      = TestLibrary.Generator.GetInt32(-55) % Math.Max(1,oldString.Length);
        int     removedLength   = TestLibrary.Generator.GetInt32(-55) % Math.Max(1,(oldString.Length-startIndex-1));
        string  newString        = oldString.Remove(startIndex, removedLength);

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Remove subString form a StringBuilder ...");

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);
        System.Text.StringBuilder removedStringBuilder = new System.Text.StringBuilder(newString);

        try
        {
           
            stringBuilder.Remove(startIndex,removedLength);

            int compareResult = string.CompareOrdinal(stringBuilder.ToString(), removedStringBuilder.ToString());

            if (compareResult != 0)
            {
                TestLibrary.TestFramework.LogError("001", "StringBuilder can't corrently remove");
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify StringBuilder Remove  itself ");

        string  oldString       = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int     startIndex      = 0;
        int     removedLength   = oldString.Length;

        try
        {
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);
            stringBuilder.Remove(startIndex, removedLength);
            if (stringBuilder.Length != 0)
            {
                TestLibrary.TestFramework.LogError("003", "StringBuilder can't corrently remove itself");
                retVal = false;
            }
        }
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        string  oldString       = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int     startIndex      = 0;
        int     removedLength   = TestLibrary.Generator.GetInt32(-55) % (oldString.Length - startIndex);
        string  newString       = oldString.Remove(startIndex, removedLength);


        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify StringBuilder Remove form posization of  0 index ...");

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);
        System.Text.StringBuilder removedStringBuilder = new System.Text.StringBuilder(newString);

        try
        {
            
            stringBuilder.Remove(startIndex, removedLength);

            int compareResult = string.CompareOrdinal(stringBuilder.ToString(), removedStringBuilder.ToString());

            if (compareResult != 0)
            {
                TestLibrary.TestFramework.LogError("005", "StringBuilder can't corrently remove from posization of 0 index");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string  oldString       = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        int     startIndex      = TestLibrary.Generator.GetInt32(-55) % oldString.Length;
        int     removedLength   = 0;
        string  newString       = oldString.Remove(startIndex, removedLength);

        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify StringBuilder Remove 0 length ...");

        try
        {
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);
            System.Text.StringBuilder removedStringBuilder = new System.Text.StringBuilder(newString);
            stringBuilder.Remove(startIndex, removedLength);

            if (stringBuilder.ToString() != removedStringBuilder.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "StringBuilder can't corrently Remove 0 length");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
   
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC    = "NegTest1: StingBuilder length is 0 and length of removed larger than 0";
        const string c_TEST_ID      = "N001";


        
        string oldString = TestLibrary.Generator.GetString(-55, false, 0, 0);
        int startIndex = 0;
        int removedLength = 0;

        while (removedLength == 0)
        {
            removedLength = TestLibrary.Generator.GetInt32(-55);
        }

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Remove(startIndex, removedLength);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." );
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: StingBuilder length is 0 and started index  larger than 0";
        const string c_TEST_ID = "N002";

        
        int startIndex      = 0;
        int removedLength   = 0;
        while (startIndex == 0)
        {
            startIndex = TestLibrary.Generator.GetInt32(-55);
        }
        string oldString = TestLibrary.Generator.GetString(-55, false, 0, 0);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Remove(startIndex, removedLength);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected.");
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: length of Removed is  larger than length of StringBuilder ";
        const string c_TEST_ID = "N003";


        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        
        int startIndex = TestLibrary.Generator.GetInt32(-55);
        int removedLength = TestLibrary.Generator.GetInt32(-55);


        while (startIndex <= oldString.Length )
        {
            startIndex = TestLibrary.Generator.GetInt32(-55);
        }

        
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);


        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Remove(startIndex, removedLength);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected.");
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest4: Sum of length of Removed and index of started is  larger than length of StringBuilder ";
        const string c_TEST_ID = "N004";


        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        int removedLength = TestLibrary.Generator.GetInt32(-55);
        int startIndex = TestLibrary.Generator.GetInt32(-55) % (oldString.Length-removedLength);

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Remove(startIndex, removedLength);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    
}

