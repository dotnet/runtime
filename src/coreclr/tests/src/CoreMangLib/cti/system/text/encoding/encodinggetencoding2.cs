// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

public class EncodingGetEncoding2
{
    public static int Main()
    {
        EncodingGetEncoding2 enGetEncoding2 = new EncodingGetEncoding2();
        TestLibrary.TestFramework.BeginTestCase("EncodingGetEncoding2");
        if (enGetEncoding2.RunTests())
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
//        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest4() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }
    #region PositiveTest

/* no UTF32 in CoreCLR
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Get Encoding with the defined name 1");
        try
        {
            string name = "utf-32";
            Encoding myEncoding = Encoding.GetEncoding(name);
            if (myEncoding.BodyName != name)
            {
                TestLibrary.TestFramework.LogError("001","The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
*/
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Get Encoding with the defined name 2");
        try
        {
            string name = "utf-8";
            Encoding myEncoding = Encoding.GetEncoding(name);
            if (myEncoding.ToString() != Encoding.UTF8.ToString())
            {
                TestLibrary.TestFramework.LogError("003", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
  
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Get Encoding with the defined name 4");
        try
        {
            string name = "Unicode";
            Encoding myEncoding = Encoding.GetEncoding(name);
            if (myEncoding.ToString() != Encoding.Unicode.ToString())
            {
                TestLibrary.TestFramework.LogError("007", "The ExpectResult is not the ActualResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
   
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the name is not valid codepage name");
        try
        {
            string name = null;
            Encoding myEncoding = Encoding.GetEncoding(name);
            TestLibrary.TestFramework.LogError("N001", "the name is not valid codepage name but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: The platform do not support the named codepage");
        try
        {
            string name = "helloworld";
            Encoding myEncoding = Encoding.GetEncoding(name);
            TestLibrary.TestFramework.LogError("N003", "the name is not valid codepage name but not throw exception");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}