// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class BooleanIConvertibleToChar
{

    public static int Main()
    {
        BooleanIConvertibleToChar testCase = new BooleanIConvertibleToChar();

        TestLibrary.TestFramework.BeginTestCase("Boolean.IConvertible.ToChar");
        if (testCase.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;


        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        try
        {
            char v = (true as IConvertible).ToChar(null);
            TestLibrary.TestFramework.LogError("001", 
                String.Format("expected a InvalidCastException on (true as IConvertible).ToChar(null)) but got {0}", v));
            retVal = false;
        }
        catch(InvalidCastException)
        {
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        try
        {
            char v = (false as IConvertible).ToChar(null);
            TestLibrary.TestFramework.LogError("002",
                String.Format("expected a InvalidCastException on (false as IConvertible).ToChar(null)) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException)
        {
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal; 
    }
}
