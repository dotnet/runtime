// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using System.Globalization;
using TestLibrary;

public class StringIndexOf
{
    public static string[] InterestingStrings = new string[] { "", "a", "1", "-", "A", "!", "abc", "aBc", "a\u0400Bc", "I", "i", "\u0130", "\u0131", "A", "\uFF21", "\uFE57"};

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare interesting strings ordinally");

        try
        {
            foreach (string s in InterestingStrings)
            {
                foreach (string r in InterestingStrings)
                {
                    retVal &= TestStrings(s, r);
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
    
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare many strings");

        try
        {
            for (int i = 0; i < 1000; i++) 
            {
                string str1 = Generator.GetString(-55, false, 5, 20);
                string str2 = Generator.GetString(-55, false, 5, 20);
                if (str1.IndexOf(str1, StringComparison.Ordinal) != 0)
                {
                    TestLibrary.TestFramework.LogError("003.1", "Comparison not as expected! Actual result: " + str1.IndexOf(str1, StringComparison.Ordinal).ToString() + ", Expected result: 0");
                    TestLibrary.TestFramework.LogInformation("String 1: <" + str1 + "> : " + BytesFromString(str1) + "\nString 2: <" + str1 + "> : " + BytesFromString(str1));
                    retVal = false;
                }
                if (str2.IndexOf(str2, StringComparison.Ordinal) != 0)
                {
                    TestLibrary.TestFramework.LogError("003.2", "Comparison not as expected! Actual result: " + str2.IndexOf(str2, StringComparison.Ordinal).ToString() + ", Expected result: 0");
                    TestLibrary.TestFramework.LogInformation("String 1: <" + str2 + "> : " + BytesFromString(str2) + "\nString 2: <" + str2 + "> : " + BytesFromString(str2));
                    retVal = false;
                }
                TestStrings(str1, str2);
                TestStrings(str1 + str2, str2);
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.4", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Specific regression cases");

        try
        {
            CultureInfo oldCi = Utilities.CurrentCulture;
            Utilities.CurrentCulture = new CultureInfo("hu-HU");
            retVal &= TestStrings("Foodzsdzsbar", "ddzs");
            Utilities.CurrentCulture = oldCi;

            retVal &= TestStrings("\u00C0nimal", "A\u0300");

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public static int Main()
    {
        StringIndexOf test = new StringIndexOf();

        TestLibrary.TestFramework.BeginTestCase("StringIndexOf");

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

    private bool TestStrings(string str1, string str2)
    {
        bool retVal = true;

        int expectValue = PredictValue(str1, str2);
        int actualValue = str1.IndexOf(str2, StringComparison.Ordinal);

        if (actualValue != expectValue)
        {
            TestLibrary.TestFramework.LogError("001.1", "Comparison not as expected! Actual result: " + actualValue + ", Expected result: " + expectValue);
            TestLibrary.TestFramework.LogInformation("String 1: <" + str1 + "> : " + BytesFromString(str1) + "\nString 2: <" + str2 + "> : " + BytesFromString(str2));
            retVal = false;
        }

        return retVal;
    }

    int PredictValue(string str1, string str2)
    {
        if (str1 == null)
        {
            if (str2 == null) return 0;
            else return -1;
        }
        if (str2 == null) return -1;

        if (str2.Length > str1.Length) return -1;
        
        for (int i = 0; i <= str1.Length - str2.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < str2.Length; j++)
            {
                if ((int)str1[i + j] != (int)str2[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    private static string BytesFromString(string str)
    {
        if (str == null) return string.Empty;
        StringBuilder output = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            output.Append(Utilities.ByteArrayToString(BitConverter.GetBytes(str[i])));
            if (i != (str.Length - 1)) output.Append(", ");
        }
        return output.ToString();
    }
}
