// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;

public class StringConcat2
{
    public static int Main()
    {
        StringConcat2 sc2 = new StringConcat2();

        TestLibrary.TestFramework.BeginTestCase("StringConcat2");

        if (sc2.RunTests())
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
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        object ObjB;

        TestLibrary.TestFramework.BeginScenario("PostTest1:Concat two objects of object ");
        try
        {
            ObjA = new object();
            ObjB = new object();
            ActualResult = string.Concat(ObjA,ObjB);
            if (ActualResult != ObjA.ToString() + ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("001", "Concat a random object ExpectResult is" + ObjA.ToString() + ObjB.ToString()+ ",ActualResult is (" + ActualResult + ")");
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

        TestLibrary.TestFramework.BeginScenario("PostTest2:Concat null object and null object");
        try
        {
            ObjA = null;
            ObjB = null;
            ActualResult = string.Concat(ObjA,ObjB);
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("003", "Concat null object and null object ExpectResult is" + string.Empty + " ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Concat null object and empty");
        try
        {
            ObjA = null;
            ObjB = "";
            ActualResult = string.Concat(ObjA,ObjB);
            if (ActualResult != string.Empty)
            {
                TestLibrary.TestFramework.LogError("005", "Concat null object and empty ExpectResult is equel" + string.Empty + ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Concat null and two tabs");
        try
        {
            ObjB = null;
            ObjA = new string('\t', 2);
            ActualResult = string.Concat(ObjB,ObjA);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "Concat null and two tabs ExpectResult is" +ObjA.ToString() + " ,ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5:Concat null and an object of int");
        try
        {
            ObjA = null;
            ObjB = new int();
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("009", "Concat null and an object of int ExpectResult is equel" +ObjB.ToString() + " ,ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Concat null and an object of datetime");
        try
        {
            ObjA = null;
            ObjB = new DateTime();
            ActualResult = string.Concat(ObjA,ObjB);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("011", "Concat null and an object of datetime ExpectResult is equel" + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Concat null and an object of bool");
        try
        {
            ObjA = null;
            ObjB = new bool();
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("013", "Concat nulland an object of bool ExpectResult is equel" +ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest8:Concat null and an object of random class instance");
        try
        {
            ObjA = null;
            ObjB = new StringConcat2();
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("015", "Concat null and an object of random class instance ExpectResult is equel"+ ObjB.ToString() + " ,ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest9: Concat  null and an object of Guid");
        try
        {
            ObjA = null;
            ObjB = new Guid();
            ActualResult = string.Concat(ObjA,ObjB);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("017", "Concat null and an object of Guid ExpectResult is equel" + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest10:Concat null and an object of Random ");
        try
        {
            ObjA = null;
            ObjB = new Random(-55);
            ActualResult = string.Concat(ObjA,ObjB);
            if (ActualResult != ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("019", "Concat  null and an object of Random ExpectResult is equel" + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest11:Concat an object and null ");
        try
        {
            ObjA = new object();
            ObjB = null;
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("021", "Concat an object and null ExpectResult is equel" + ObjA.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        TestLibrary.TestFramework.BeginScenario("PosTest12:Concat an object of Guid and null ");
        try
        {
            ObjA = new Guid();
            ObjB = null;
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjA.ToString())
            {
                TestLibrary.TestFramework.LogError("023", "Concat an object of Guid and null ExpectResult is equel" + ObjA.ToString() + ",ActualResult is (" + ActualResult + ")");
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
        object ObjB;
        TestLibrary.TestFramework.BeginScenario("PosTest13:Concat an object of Guid and an object ");
        try
        {
            ObjA = new Guid();
            ObjB = new object();
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjA.ToString() + ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("025", "Concat an object of Guid and null ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
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

    public bool PosTest14()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        object ObjB;
        TestLibrary.TestFramework.BeginScenario("PosTest14:Concat an objec and an object of datetime");
        try
        {
            ObjA = new object();
            ObjB = new DateTime();
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjA.ToString() + ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("027", "Concat an object and an object of datetime ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest15()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        object ObjB;
        TestLibrary.TestFramework.BeginScenario("PosTest15:Concat an object of datetime and an object of bool");
        try
        {
            ObjA = new DateTime();
            ObjB = new bool();
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjA.ToString() + ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("029", "Concat an object of datetime and an object of bool ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest16()
    {
        bool retVal = true;
        string ActualResult;
        object ObjA;
        object ObjB;
        TestLibrary.TestFramework.BeginScenario("PosTest16:Concat object of two number of less than 0");
        try
        {
            ObjA = -123;
            ObjB = -132;
            ActualResult = string.Concat(ObjA, ObjB);
            if (ActualResult != ObjA.ToString() + ObjB.ToString())
            {
                TestLibrary.TestFramework.LogError("031", "Concat object of two number of less than 0 ExpectResult is equel" + ObjA.ToString() + ObjB.ToString() + ",ActualResult is (" + ActualResult + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception" + e);
            retVal = false;
        }
        return retVal;
    }
}

