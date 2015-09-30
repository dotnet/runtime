using System;
using System.Collections.Generic;
using System.Globalization;
using TestLibrary;


/// <summary>
/// System.Byte.IConvertible.ToString()
/// </summary>
public class ByteToString1
{
    public static int Main(string[] args)
    {
        ByteToString1 toString1 = new ByteToString1();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.IConvertible.ToString()...");

        if (toString1.RunTests())
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
        retVal = PosTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify convert byte is middle value...");

        try
        {
            Byte myByte = 128;
            string byteString = myByte.ToString();

            if (byteString != GlobLocHelper.OSByteToString(myByte))
            {
                TestLibrary.TestFramework.LogError("001", "The convert string is not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify convert byte is max value...");

        try
        {
            Byte myByte = 255;
            string byteString = myByte.ToString();

            if (byteString != GlobLocHelper.OSByteToString(myByte))
            {
                TestLibrary.TestFramework.LogError("003", "The convert string is not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify convert byte is min value...");

        try
        {
            Byte myByte = 0;
            string byteString = myByte.ToString();

            if (byteString != GlobLocHelper.OSByteToString(myByte))
            {
                TestLibrary.TestFramework.LogError("005", "The convert string is not correct!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify convert byte is max-1 value...");

        try
        {
            Byte myByte = 254;
            string byteString = myByte.ToString();

            if (byteString != GlobLocHelper.OSByteToString(myByte))
            {
                TestLibrary.TestFramework.LogError("007", "The convert string is not correct!");
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

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify convert byte is min+1 value...");

        try
        {
            Byte myByte = 1;
            string byteString = myByte.ToString();

            if (byteString != GlobLocHelper.OSByteToString(myByte))
            {
                TestLibrary.TestFramework.LogError("009", "The convert string is not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
