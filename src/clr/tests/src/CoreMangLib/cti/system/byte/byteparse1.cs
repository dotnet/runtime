// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;


/// <summary>
/// System.Byte.Parse(System.String)
/// </summary>
public class ByteParse1
{
    public static int Main(string[] args)
    {
        ByteParse1 parse1 = new ByteParse1();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.Parse(System.String)...");

        if (parse1.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString is between Byte.MinValue and Byte.MaxValue...");

        try
        {
            string byteString = "99";
            Byte myByte = Byte.Parse(byteString);

            if (myByte != 99)
            {
                TestLibrary.TestFramework.LogError("001","The value should be equal to byteString.ToInt...");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString contains +...");

        try
        {
            string byteString = "+99";
            Byte myByte = Byte.Parse(byteString);

            if (myByte != 99)
            {
                TestLibrary.TestFramework.LogError("003","The value should be equal to byteString.ToInt...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexcepted exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString is equal to Byte.MaxValue...");

        try
        {
            string byteString = "255";
            Byte myByte = Byte.Parse(byteString);

            if (myByte != 255)
            {
                TestLibrary.TestFramework.LogError("005", "byteString should be equal to Byte.MaxValue!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString is equal to Byte.MinValue...");

        try
        {
            string byteString = "0";
            Byte myByte = Byte.Parse(byteString);

            if (myByte != 0)
            {
                TestLibrary.TestFramework.LogError("007", "byteString should be equal to Byte.MinValue!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify OverFlowException is thrown when byteString is greater than Byte.MaxValue...");

        try
        {
            string byteString = "256";
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("009","No exception occurs!");
            retVal = false;
        }
        catch (OverflowException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify OverFlowException is thrown when  byteString is less than Byte.MinValue...");

        try
        {
            string byteString = "-1";
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("011","No exception occurs!");
            retVal = false;
        }
        catch (OverflowException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify ArgumentNullException occurs when byteString is null...");

        try
        {
            string byteString = null;
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("013", "No exception occurs!");
            retVal = false;
        }
        catch (ArgumentNullException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString is between Byte.MaxValue and Byte.MinValue and contains plus...");

        try
        {
            string byteString = "plus222";
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("015", "No exception occurs!");
            retVal = false;
        }
        catch (FormatException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString contains underline...");

        try
        {
            string byteString = "1_2_3";
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("017", "No exception occurs!");
            retVal = false;
        }
        catch (FormatException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString contains only characters...");

        try
        {
            string byteString = "hello";
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("017", "No exception occurs!");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byteString is an empty string...");

        try
        {
            string byteString = "";
            Byte myByte = Byte.Parse(byteString);

            TestLibrary.TestFramework.LogError("017", "No exception occurs!");
            retVal = false;
        }
        catch (FormatException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }


}
