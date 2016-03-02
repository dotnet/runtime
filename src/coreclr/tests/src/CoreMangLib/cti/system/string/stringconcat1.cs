// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;


public class StringConcat1
{
    public static int Main()
    {
        StringConcat1 sc1 = new StringConcat1();

        TestLibrary.TestFramework.BeginTestCase("StringConcat1");

        if (sc1.RunTests())
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
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat a random object");
        try
        {
            ObjA = new object();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "Concat a random object ExpectResult is" +ObjA.ToString()+ ",ActualResult is (" + ActualResult + ")");
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
        string ActualResult;
        object ObjA;

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat an object of null");
        try
        {
            ObjA = null;
            ActualResult = string.Concat(ObjA);
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("003", "Concat an object of null ExpectResult is" +string.Empty+" ActualResult is (" + ActualResult + ")");
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
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat an object of empty");
        try
        {
            ObjA = "";
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "Concat an object of empty ExpectResult is equel" +ObjA.ToString()+ ",ActualResult is (" + ActualResult + ")");
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
        string ObjA;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat an object of two tab");
        try
        {
            ObjA = new string('\t', 2);
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "Concat an object of two tab ExpectResult is" +ObjA.ToString()+" ,ActualResult is (" + ActualResult + ")");
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
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat an object of int");
        try
        {
            ObjA = new int();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("009", "Concat an object of int ExpectResult is equel" +ObjA.ToString()+" ,ActualResult is (" + ActualResult + ")");
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
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat an object of datetime");
        try
        {
            ObjA = new DateTime();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("011", "  ExpectResult is equel" +ObjA.ToString()+ ",ActualResult is (" + ActualResult + ")");
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
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat an object of bool");
        try
        {
            ObjA = new bool();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("013", "Concat an object of bool ExpectResult is equel" +ObjA.ToString()+",ActualResult is (" + ActualResult + ")");
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
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat an object of random class instance  ");
        try
        {
            ObjA = new StringConcat1();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("015", "Concat an object of random class instance ExpectResult is equel" +ObjA.ToString()+" ,ActualResult is (" + ActualResult + ")");
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

    public bool PosTest9()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Concat an object of Guid");
        try
        {
            ObjA = new Guid();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("017", "Concat an object of Guid ExpectResult is equel"+ ObjA.ToString()+",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest10()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest10:Concat an object of Random ");
        try
        {
            ObjA = new Random(-55);
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("019", "Concat an object of Random ExpectResult is equel" + ObjA.ToString() +",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest11()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest11: Concat an object of float");
        try
        {
            ObjA = new float();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("021", "Concat an object of float ExpectResult is equel" +ObjA.ToString()+" ,ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest12:Concat an object of SByte");
        try
        {
            ObjA = new SByte();
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("023", "Concat an object of SByte ExpectResult is equel" + ObjA.ToString()+ ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        TestLibrary.TestFramework.BeginScenario("PosTest13:Concat an object of number of less than 0");
        try
        {
            ObjA = -123;
            ActualResult = string.Concat(ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("025", "Concat an object of number of less than 0 ExpectResult is equel" + ObjA.ToString() + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }



}

