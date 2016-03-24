// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Collections.IEnumerator.MoveNext
/// </summary>
public class IEnumeratorMoveNext
{
    private const int minLength = 1;
    private const int maxLength = 10;

    public static int Main(string[] args)
    {
        IEnumeratorMoveNext moveNext = new IEnumeratorMoveNext();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Collections.IEnumerator.MoveNext()...");

        if (moveNext.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("Check MoveNext method return valid value...");

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();

            for (int i = 0; i < str.Length; i++)
            {
                if (!iEnum.MoveNext())
                {
                    TestLibrary.TestFramework.LogError("001", "MoveNext method does not return a valid value when index within range!");
                    retVal = false;
                }
            }

            if (iEnum.MoveNext())
            {
                TestLibrary.TestFramework.LogError("002", "MoveNext should return false when index is out of range!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check MoveNext method could pass the correct index...");
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();

            for (int i = 0; i < str.Length; i++)
            {
                iEnum.MoveNext();
                if (iEnum.Current.ToString() != strCopy[i].ToString())
                {
                    TestLibrary.TestFramework.LogError("004", "MoveNext method does not navigate to correct index!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Execute MoveNext method for several times after index to to max+1,verify it always return false...");

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();

            for (int i = 0; i < str.Length; i++)
            {
                if (!iEnum.MoveNext())
                {
                    TestLibrary.TestFramework.LogError("006", "MoveNext method does not return a valid value when index within range!");
                    retVal = false;
                }
            }

            for (int j = 0; j < str.Length; j++)
            {
                if (iEnum.MoveNext())
                {
                    TestLibrary.TestFramework.LogError("007", "MoveNext method does not return a valid value when index within range!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
