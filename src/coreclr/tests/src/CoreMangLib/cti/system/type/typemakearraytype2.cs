// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// MakeArrayType(System.Int32)
/// </summary>
public class TypeMakeArrayType2
{
    #region Private Members
    private const int c_DEFAULT_MULTIPLE_ARRAY_DIMENSION = 4;
    #endregion

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
        retVal = PosTest6() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        int desiredDimension = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call MakeArrayType to make 1 dimension value type array");

        try
        {
            desiredDimension = 1;
            Type type = typeof(Int32);
            Type arrayType = type.MakeArrayType(desiredDimension);

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("001", "Call MakeArrayType for value type does not make a array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
                    retVal = false;
                    break;
                }

                int actualDimension = arrayType.GetArrayRank();
                if (actualDimension != desiredDimension)
                {
                    TestLibrary.TestFramework.LogError("002", "Call MakeArrayType for value type does not make a one dimension array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString() + "; actualDimension = " + actualDimension.ToString());
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        int desiredDimension = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call MakeArrayType to make multiple dimensions value type array");

        try
        {
            desiredDimension = c_DEFAULT_MULTIPLE_ARRAY_DIMENSION;
            Type type = typeof(Int32);
            Type arrayType = type.MakeArrayType(desiredDimension);

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("004", "Call MakeArrayType for value type does not make a array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
                    retVal = false;
                    break;
                }

                int actualDimension = arrayType.GetArrayRank();
                if (actualDimension != desiredDimension)
                {
                    TestLibrary.TestFramework.LogError("005", "Call MakeArrayType for value type does not make a one dimension array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString() + "; actualDimension = " + actualDimension.ToString());
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        int desiredDimension = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call MakeArrayType to make multiple dimensions reference type array");

        try
        {
            desiredDimension = c_DEFAULT_MULTIPLE_ARRAY_DIMENSION;
            Type type = typeof(String);
            Type arrayType = type.MakeArrayType(desiredDimension);

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("007", "Call MakeArrayType for reference type does not make a array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
                    retVal = false;
                    break;
                }

                int actualDimension = arrayType.GetArrayRank();
                if (actualDimension != desiredDimension)
                {
                    TestLibrary.TestFramework.LogError("008", "Call MakeArrayType for reference type does not make a multiple dimensions array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString() + "; actualDimension = " + actualDimension.ToString());
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        int desiredDimension = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call MakeArrayType to make multiple dimensions pointer type array");

        try
        {
            desiredDimension = c_DEFAULT_MULTIPLE_ARRAY_DIMENSION;
            Type type = typeof(char *);
            Type arrayType = type.MakeArrayType(desiredDimension);

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("010", "Call MakeArrayType for pointer type does not make a array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
                    retVal = false;
                    break;
                }

                int actualDimension = arrayType.GetArrayRank();
                if (actualDimension != desiredDimension)
                {
                    TestLibrary.TestFramework.LogError("011", "Call MakeArrayType for pointer type does not make a multiple dimensions array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString() + "; actualDimension = " + actualDimension.ToString());
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        int desiredDimension = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call MakeArrayType to make multiple dimensions reference array type array");

        try
        {
            desiredDimension = c_DEFAULT_MULTIPLE_ARRAY_DIMENSION;
            Type type = typeof(String[]);
            Type arrayType = type.MakeArrayType(desiredDimension);

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("013", "Call MakeArrayType for reference array type does not make a array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
                    retVal = false;
                    break;
                }

                int actualDimension = arrayType.GetArrayRank();
                if (actualDimension != desiredDimension)
                {
                    TestLibrary.TestFramework.LogError("014", "Call MakeArrayType for reference array type does not make a multiple dimensions array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString() + "; actualDimension = " + actualDimension.ToString());
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        int desiredDimension = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Call MakeArrayType to make multiple dimensions value array type array");

        try
        {
            desiredDimension = c_DEFAULT_MULTIPLE_ARRAY_DIMENSION;
            Type type = typeof(int[]);
            Type arrayType = type.MakeArrayType(desiredDimension);

            do
            {
                if (!arrayType.IsArray)
                {
                    TestLibrary.TestFramework.LogError("016", "Call MakeArrayType for value array type does not make a array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
                    retVal = false;
                    break;
                }

                int actualDimension = arrayType.GetArrayRank();
                if (actualDimension != desiredDimension)
                {
                    TestLibrary.TestFramework.LogError("017", "Call MakeArrayType for value array type does not make a multiple dimensions array type");
                    TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString() + "; actualDimension = " + actualDimension.ToString());
                    retVal = false;
                    break;
                }
            } while (false);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredDimension = " + desiredDimension.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        int rank = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest1: IndexOutOfRangeException will be thrown when rank is invalid");

        try
        {
            rank = TestLibrary.Generator.GetByte(-55);
            if (rank > 0)
                rank = 0 - rank;

            Type type = typeof(Object);
            Type arrayType = type.MakeArrayType(rank);

            TestLibrary.TestFramework.LogError("101", "IndexOutOfRangeException is not thrown when rank is invalid");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] rank = " + rank.ToString());
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] rank = " + rank.ToString());
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: IndexOutOfRangeException will be thrown when rank is 0");

        try
        {

            Type type = typeof(Object);
            Type arrayType = type.MakeArrayType(0);

            TestLibrary.TestFramework.LogError("103", "IndexOutOfRangeException is not thrown when rank is 0");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TypeMakeArrayType2 test = new TypeMakeArrayType2();

        TestLibrary.TestFramework.BeginTestCase("TypeMakeArrayType2");

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
