// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;


/// <summary>
/// System.Collections.IEnumerator.Reset
/// </summary>
public class IEnumeratorReset
{
    private const int minLength = 1;
    private const int maxLength = 10;
    public static int Main(string[] args)
    {
        IEnumeratorReset reset = new IEnumeratorReset();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Collections.IEnumerator.Reset interface...");

        if (reset.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("Check the index point to the right position after execute reset method...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();
            iEnum.Reset();
            for (int i = 0; i < str.Length; i++)
            {
                iEnum.MoveNext();
                if ((Char)iEnum.Current != strCopy[i])
                {
                    TestLibrary.TestFramework.LogError("001", "Reset method can't nevigate to the correct position!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("Check the index point to the right position when index is not at beginning and then execute reset method...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();

            for (int i = 0; i < str.Length; i++)
            {
                iEnum.MoveNext(); 
                if (iEnum.Current.ToString() != ((object)strCopy[i]).ToString())
                {
                    TestLibrary.TestFramework.LogError("008", "Reset method can't nevigate to the correct position!");
                    retVal = false;
                }
            }
            iEnum.Reset();
            for (int i = 0; i < str.Length; i++)
            {
                iEnum.MoveNext();
                if (iEnum.Current.ToString() != ((object)strCopy[i]).ToString())
                {
                    TestLibrary.TestFramework.LogError("009", "Reset method can't nevigate to the correct position!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unecpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("Check the first index after execute reset method...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();

            iEnum.Reset();
            char charTest = (char)iEnum.Current;

            TestLibrary.TestFramework.LogError("003", "No exception occurs!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("Check the last index after traverse the whole enumerator...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            IEnumerator iEnum = ((IEnumerable)str).GetEnumerator();

            iEnum.Reset();
            for (int i = 0; i < str.Length; i++)
            {
                iEnum.MoveNext();
                if ((Char)iEnum.Current != strCopy[i])
                {
                    TestLibrary.TestFramework.LogError("005", "Reset method can't nevigate to the correct position!");
                    retVal = false;
                }
            }
            iEnum.MoveNext();
            char charTest = (char)iEnum.Current;

            TestLibrary.TestFramework.LogError("006", "No exception occurs when fetch current out of range!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception occurs: " + e);
            return retVal;
        }

        return retVal;
    }
}
