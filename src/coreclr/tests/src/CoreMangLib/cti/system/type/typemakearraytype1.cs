// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MakeArrayType()
/// </summary>
public class TypeMakeArrayType1
{
    #region Public Methods
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

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call MakeArrayType for reference type");

        try
        {
            Type type = typeof(String);
            Type arrayType = type.MakeArrayType();

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("001", "Call MakeArrayType for reference type does not make a array type");
                    retVal = false;
                    break;
                }

                if (arrayType.GetArrayRank() != 1)
                {
                    TestLibrary.TestFramework.LogError("002", "Call MakeArrayType for reference type does not make a one dimension array type");
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call MakeArrayType for value type");

        try
        {
            Type type = typeof(Int32);
            Type arrayType = type.MakeArrayType();

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("004", "Call MakeArrayType for value type does not make a array type");
                    retVal = false;
                    break;
                }

                if (arrayType.GetArrayRank() != 1)
                {
                    TestLibrary.TestFramework.LogError("005", "Call MakeArrayType for value type does not make a one dimension array type");
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call MakeArrayType for pointer type");

        try
        {
            Type type = typeof(char *);
            Type arrayType = type.MakeArrayType();

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("007", "Call MakeArrayType for pointer type does not make a array type");
                    retVal = false;
                    break;
                }

                if (arrayType.GetArrayRank() != 1)
                {
                    TestLibrary.TestFramework.LogError("008", "Call MakeArrayType for pointer type does not make a one dimension array type");
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call MakeArrayType for value array type");

        try
        {
            Type type = typeof(int[]);
            Type arrayType = type.MakeArrayType();

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("010", "Call MakeArrayType for value array type does not make a array type");
                    retVal = false;
                    break;
                }

                if (arrayType.GetArrayRank() != 1)
                {
                    TestLibrary.TestFramework.LogError("011", "Call MakeArrayType for value array type does not make a one dimension array type");
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call MakeArrayType for reference array type");

        try
        {
            Type type = typeof(String[]);
            Type arrayType = type.MakeArrayType();

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("013", "Call MakeArrayType for reference array type does not make a array type");
                    retVal = false;
                    break;
                }

                if (arrayType.GetArrayRank() != 1)
                {
                    TestLibrary.TestFramework.LogError("014", "Call MakeArrayType for reference array type does not make a one dimension array type");
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TypeMakeArrayType1 test = new TypeMakeArrayType1();

        TestLibrary.TestFramework.BeginTestCase("TypeMakeArrayType1");

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
}
