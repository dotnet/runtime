// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;

public class StringConcat4
{
    private int c_MAX_STRING_LENGTH = 256;
    private int c_MINI_STRING_LENGTH = 8;

    public static int Main()
    {
        StringConcat4 sc4 = new StringConcat4();

        TestLibrary.TestFramework.BeginTestCase("StringConcat4");

        if (sc4.RunTests())
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
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        Random rand = new Random(-55);
        string ActualResult;
        object[] ObjA = new object[rand.Next(c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH)];

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat object Array with all null member");
        try
        {
            for (int i = 0; i < ObjA.Length; i++)
            {
                ObjA[i] = null;
            }
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("001", "Concat object Array with all null member ExpectResult is" + MergeStrings(ObjA) + "ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        Random rand = new Random(-55);
        string ActualResult;
        object[] ObjA = new object[rand.Next(c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH)];

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat object Array with empty member");
        try
        {
            for (int i = 0; i < ObjA.Length; i++)
            {
                ObjA[i] = "";
            }
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("003", "Concat object Array with empty member ExpectResult is" + MergeStrings(ObjA) + " ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string ActualResult;
        object[] ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat object Array with null and empty member");
        try
        {
            ObjA = new object[]{ null, "", null, "", null, "" };
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("005", "Concat object Array with null and empty member ExpectResult is equel" + MergeStrings(ObjA) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string ActualResult;
        object[] ObjA;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat object Array with null,empty and space member");
        try
        {
            ObjA = new object[] {" ",null,""," "};
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("007", "Concat object Array with null,empty and space member ExpectResult is" + MergeStrings(ObjA) + " ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string ActualResult;
        object[] ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat object Array with int,datetime,bool and class instance member");
        try
        {
            object obj1 = -123;
            object obj2 = new DateTime();
            object obj3 = new bool();
            object obj4 = new StringConcat4();
            ObjA = new object[] { obj1, obj2, obj3, obj4 };
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("009", "Concat an object of int ExpectResult is equel" + MergeStrings(ObjA) + " ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string ActualResult;
        object[] ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat object Array with guid and some special symbols member");
        try
        {
            object obj1 = new Guid();
            object obj2 = "\n";
            object obj3 = "\t";
            object obj4 = "\u0041";
            ObjA = new object[] { obj1, obj2, obj3, obj4};
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("011", "Concat object Array with guid and some special symbols member ExpectResult is equel" + MergeStrings(ObjA) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        string ActualResult;
        object[] ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat object Array with some strings and symbols member");
        try
        {
            object obj1 = "hello";
            object obj2 = "\0";
            object obj3 = "World";
            object obj4 = "\u0020";
            object obj5 = "!";
            ObjA = new object[] { obj1, obj2, obj3, obj4, obj5 };
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("013", "Concat object Array with some strings and symbols member ExpectResult is equel" + MergeStrings(ObjA) + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        string ActualResult;
        object[] ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat object Array with bool and some strings member");
        try
        {
            object obj1 = true;
            object obj2 = "hello\0world";
            object obj3 = 12;
            object obj4 = "uff21";
            ObjA = new object[] { obj1, obj2, obj3, obj4 };
            ActualResult = string.Concat(ObjA);
            if (ActualResult != MergeStrings(ObjA))
            {
                TestLibrary.TestFramework.LogError("015", "Concat object Array with bool and some strings member ExpectResult is equel" + MergeStrings(ObjA) + " ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        Random rand = new Random(-55);

        TestLibrary.TestFramework.BeginScenario("NegTest1: Concat object Array is null");

        object[] ObjA = new object[rand.Next(c_MINI_STRING_LENGTH,c_MAX_STRING_LENGTH)];
        string ActualResult;
        try
        {
            ObjA = null;
            ActualResult = string.Concat(ObjA);
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        Random rand = new Random(-55);
        object[] ObjA = new object[50 * 1024 * 1024];
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Concat object Array with many strings");

        int TotalLength = 0;
        try
        {
            for (int i = 0; i < ObjA.Length; i++)
            {
                ObjA[i] = "HelloworldHelloworldHelloworldHelloworld!";
                TotalLength += ObjA[i].ToString().Length;
            }
            ActualResult = string.Concat(ObjA);
            retVal = false;
        }
        catch (OutOfMemoryException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #region Help method
    private string MergeStrings(object[] ObjS)
    {
        string ResultString ="";
        foreach (object obj in ObjS)
        {
            if (obj == null)
                ResultString += string.Empty;
            else
                ResultString += obj.ToString();
        }
        return ResultString;
    }

    #endregion

    #endregion

}

