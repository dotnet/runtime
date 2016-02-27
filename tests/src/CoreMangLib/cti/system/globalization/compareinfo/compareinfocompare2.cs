// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using System.Globalization;
using TestLibrary;

public class CompareInfoCompare
{
    public static string[] InterestingStrings = new string[] { null, "", "a", "1", "-", "A", "!", "abc", "aBc", "a\u0400Bc", "I", "i", "\u0130", "\u0131", "A", "\uFF21", "\uFE57"};

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare many characters");

        try
        {
            for (int i = 0; i < 40; i++)  // Ok, 40 isn't that many... but this takes way too long
            {
                char c = Generator.GetChar(-55);
                if (Utilities.CurrentCulture.CompareInfo.Compare(new string(new char[] { c }), new string(new char[] { c }), CompareOptions.Ordinal) != 0)
                {
                    TestLibrary.TestFramework.LogError("002.1", "Character " + i.ToString() + " is not equal to itself ordinally!");
                    retVal = false;
                }

                for (int j = 0; j < (int)c; j++)
                {
                    int compareResult = Utilities.CurrentCulture.CompareInfo.Compare(new string(new char[] { c }), new string(new char[] { (char)j }), CompareOptions.Ordinal);
                    if (compareResult != 0) compareResult = compareResult / Math.Abs(compareResult);
                    if (compareResult != 1)
                    {
                        TestLibrary.TestFramework.LogError("002.2", "Character " + ((int)c).ToString() + " is not greater than character " + j.ToString() + ", Compare result: " + compareResult.ToString());
                        retVal = false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.4", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare many strings");

        try
        {
            for (int i = 0; i < 1000; i++) 
            {
                string str1 = Generator.GetString(-55, false, 5, 20);
                string str2 = Generator.GetString(-55, false, 5, 20);
                if (Utilities.CurrentCulture.CompareInfo.Compare(str1, str1, CompareOptions.Ordinal) != 0)
                {
                    TestLibrary.TestFramework.LogError("003.1", "Comparison not as expected! Acutal result: " + Utilities.CurrentCulture.CompareInfo.Compare(str1, str1, CompareOptions.Ordinal).ToString() + ", Expected result: 0");
                    TestLibrary.TestFramework.LogInformation("String 1: <" + str1 + "> : " + BytesFromString(str1) + "\nString 2: <" + str1 + "> : " + BytesFromString(str1));
                    retVal = false;
                }
                if (Utilities.CurrentCulture.CompareInfo.Compare(str2, str2, CompareOptions.Ordinal) != 0)
                {
                    TestLibrary.TestFramework.LogError("003.2", "Comparison not as expected! Acutal result: " + Utilities.CurrentCulture.CompareInfo.Compare(str2, str2, CompareOptions.Ordinal).ToString() + ", Expected result: 0");
                    TestLibrary.TestFramework.LogInformation("String 1: <" + str2 + "> : " + BytesFromString(str2) + "\nString 2: <" + str2 + "> : " + BytesFromString(str2));
                    retVal = false;
                }
                TestStrings(str1, str2);
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.4", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Specific regression cases");

        try
        {
            CultureInfo oldCi = Utilities.CurrentCulture;
            Utilities.CurrentCulture = new CultureInfo("hu-HU");
            retVal &= TestStrings("dzsdzs", "ddzs");
            Utilities.CurrentCulture = oldCi;

            retVal &= TestStrings("\u00C0nimal", "A\u0300nimal");

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
        CompareInfoCompare test = new CompareInfoCompare();

        TestLibrary.TestFramework.BeginTestCase("CompareInfoCompare");

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
        int actualValue = Utilities.CurrentCulture.CompareInfo.Compare(str1, str2, CompareOptions.Ordinal);
        if (expectValue != 0) expectValue = expectValue / Math.Abs(expectValue);
        if (actualValue != 0) actualValue = actualValue / Math.Abs(actualValue);

        if (actualValue != expectValue)
        {
            TestLibrary.TestFramework.LogError("001.1", "Comparison not as expected! Acutal result: " + actualValue + ", Expected result: " + expectValue);
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
        if (str2 == null) return 1;

        for (int i = 0; i < str1.Length; i++)
        {
            if (i >= str2.Length) return 1;
            if ((int)str1[i] > (int)str2[i]) return 1;
            if ((int)str1[i] < (int)str2[i]) return -1;
        }

        if (str2.Length > str1.Length) return -1;

        return 0;
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
