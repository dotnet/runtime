// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;

public class StringConcat3
{
    public static int Main()
    {
        StringConcat3 sc3 = new StringConcat3();

        TestLibrary.TestFramework.BeginTestCase("StringConcat3");

        if (sc3.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        object ObjB;
        object ObjC;

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat three objects of object ");
        try
        {
            ObjA = new object();
            ObjB = new object();
            ObjC = new object();
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjA.ToString() +ObjB.ToString() +ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "Concat three objects ExpectResult is" + ObjA.ToString() + ObjB.ToString() + ObjC.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat  three null objects");
        try
        {
            ObjA = null;
            ObjB = null;
            ObjC = null;
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("003", "Concat three null objects ExpectResult is" + string.Empty+ " ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat two null objects and a number of less than 0");
        try
        {
            ObjA = null;
            ObjB = null;
            ObjC = -12314124;
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("005", "Concat two null objects and a number of less than 0 ExpectResult is equel" + ObjC.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        object ObjA;
        object ObjB;
        object ObjC;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat three special strings");
        try
        {
            ObjA = new string('\t', 2);
            ObjB = "\n";
            ObjC = "\t";
            ActualResult = string.Concat(ObjA, ObjB, ObjC);
            if (ActualResult != ObjA.ToString() + ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "Concat three special strings ExpectResult is" + ObjB.ToString() + ObjA.ToString() + ObjC.ToString() + " ,ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat three numbers of less than 0");
        try
        {
            ObjA = -123;
            ObjB = -123;
            ObjC = -123;
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjA.ToString() + ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("009", "Concat three numbers of less than 0 ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + " ,ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat two nulls and an object of datetime");
        try
        {
            ObjA = null;
            ObjB = new DateTime();
            ObjC = null;
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("011", "Concat two nulls and an object of datetime ExpectResult is equel" + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat null and an object of datetime and one space");
        try
        {
            ObjA = null;
            ObjB = new DateTime();
            ObjC = " ";
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjB.ToString() +ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("013", "Concat null and an object of datetime and one space ExpectResult is equel"+ ObjB.ToString() + ObjC.ToString() +",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat null and an object of random class instance and bool object");
        try
        {
            ObjA = null;
            ObjB = new StringConcat3();
            ObjC = new bool();
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("015", "Concat null and an object of random class instance and bool object ExpectResult is equel" + ObjB.ToString() + " ,ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest9: Concat int and special symbol and an object of Guid");
        try
        {
            ObjA = 123;
            ObjB = "\n";
            ObjC = new Guid();
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjA.ToString() + ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("017", "Concat int and special symbol and an object of Guid ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ObjC.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest10:Concat two guids with a \n string in middle");
        try
        {
            ObjA = new Guid();
            ObjB = "\n";
            ObjC = new Guid();
            ActualResult = string.Concat(ObjA, ObjB,ObjC);
            if (ActualResult != ObjA.ToString() + ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("019", "Concat two guids with a \n string in middle ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ObjC.ToString() +  ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest11:Concat three guids");
        try
        {
            ObjA = new Guid();
            ObjB = new Guid();
            ObjC = new Guid();
            ActualResult = string.Concat(ObjA, ObjB, ObjC);
            if (ActualResult != ObjA.ToString() + ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("021", "Concat three guids ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ObjC.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        object ObjC;
        TestLibrary.TestFramework.BeginScenario("PosTest11:Concat guid,datetime and bool");
        try
        {
            ObjA = new Guid();
            ObjB = new bool();
            ObjC = new DateTime();
            ActualResult = string.Concat(ObjA, ObjB, ObjC);
            if (ActualResult != ObjA.ToString() + ObjB.ToString() + ObjC.ToString())
            {
                TestLibrary.TestFramework.LogError("023", "Concat guid,datetiem and bool ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ObjC.ToString() + ",ActualResult is (" + ActualResult + ")");
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



}

