// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// I have no ideas about the different between IEnumerator.Current and IEnumerator.get_Current
/// </summary>
public class CharEnumeratorIEnumgetCurrent
{
    const int minLength = 1;
    const int maxLength = 10;

    public static int Main(string[] args)
    {
        CharEnumeratorIEnumgetCurrent ceIEumget_Current =
            new CharEnumeratorIEnumgetCurrent();

        TestLibrary.TestFramework.BeginScenario("System.CharEnumerator.IEnumerator.get_Current method...");

        if (ceIEumget_Current.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Check IENumerator.get_Current show the correct element when index is valid...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            System.Collections.Generic.IEnumerator<Char> charIEnum = (System.Collections.Generic.IEnumerator<Char>)((System.Collections.Generic.IEnumerable<Char>)str).GetEnumerator();
            charIEnum.Reset();
            
            for (int i = 0; i < str.Length; i++)
            {
                charIEnum.MoveNext();
                if (!charIEnum.Current.Equals(strCopy[i]))
                {
                    TestLibrary.TestFramework.LogError("001", "IENumerator.get_Current show the wrong element when index is valid!");
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

    public bool NegTest1()
    {
        bool retVal = true;
        String str = TestLibrary.Generator.GetString(-55, true, minLength, maxLength);
        TestLibrary.TestFramework.BeginScenario("Check the first index...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            System.Collections.Generic.IEnumerator<Char> charIEnum = (System.Collections.Generic.IEnumerator<Char>)((System.Collections.Generic.IEnumerable<Char>)str).GetEnumerator();
            charIEnum.Reset();
            char charTest = (char)charIEnum.Current;

            TestLibrary.TestFramework.LogError("003", "No exception occurs!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
            TestLibrary.TestFramework.LogInformation("InvalidOperationException is throw after fetch IEnumerator.get_Current with -1 index!");
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
        TestLibrary.TestFramework.BeginScenario("Check the last index...");
        char[] strCopy = new char[str.Length];
        str.CopyTo(0, strCopy, 0, str.Length);

        try
        {
            System.Collections.Generic.IEnumerator<Char> charIEnum = (System.Collections.Generic.IEnumerator<Char>)((System.Collections.Generic.IEnumerable<Char>)str).GetEnumerator();
            charIEnum.Reset();
            for (int i = 0; i < str.Length; i++)
            {
                charIEnum.MoveNext();
                if (!charIEnum.Current.Equals(strCopy[i]))
                {
                    TestLibrary.TestFramework.LogError("005", "IEnumerator.get_Current show the wrong element!");
                    retVal = false;
                }
            }
            charIEnum.MoveNext();
            char charTest = (char)charIEnum.Current;

            TestLibrary.TestFramework.LogError("006", "No exception occurs when fetch IEnumerator.Current out of range!");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
            TestLibrary.TestFramework.LogInformation("InvalidOperationException is thrown after fetch IEnumerator.Current out of range");
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception occurs: " + e);
            return retVal;
        }

        return retVal;
    }
}
