// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// MarshalAsAttribute.ctor(UnmanagedType) [v-minch]
/// </summary>
public class MarshalAsAttributeCtor2
{
    public static int Main()
    {
        MarshalAsAttributeCtor2 test = new MarshalAsAttributeCtor2();
        TestLibrary.TestFramework.BeginTestCase("MarshalAsAttribute.ctor(UnmanagedType)");
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Initialize a instance of MarshalAsAttribute Class 1");
        try
        {
            for (int i = 2; i <= 12; i++)
            {
                UnmanagedType unmanagedType = (UnmanagedType)i;
                #region Switch
                switch (unmanagedType)
                {
                    case UnmanagedType.Bool:
                        MarshalAsAttribute myMarshalAsAttribute1 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute1 == null || myMarshalAsAttribute1.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.1", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute1.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.I1:
                        MarshalAsAttribute myMarshalAsAttribute2 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute2 == null || myMarshalAsAttribute2.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.2", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute2.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.U1:
                        MarshalAsAttribute myMarshalAsAttribute3 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute3 == null || myMarshalAsAttribute3.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.3", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute3.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.I2:
                        MarshalAsAttribute myMarshalAsAttribute4 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute4 == null || myMarshalAsAttribute4.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.4", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute4.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.U2:
                        MarshalAsAttribute myMarshalAsAttribute5 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute5 == null || myMarshalAsAttribute5.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.5", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute5.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.I4:
                        MarshalAsAttribute myMarshalAsAttribute6 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute6 == null || myMarshalAsAttribute6.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.6", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute6.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.U4:
                        MarshalAsAttribute myMarshalAsAttribute7 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute7 == null || myMarshalAsAttribute7.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.7", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute7.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.I8:
                        MarshalAsAttribute myMarshalAsAttribute8 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute8 == null || myMarshalAsAttribute8.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.8", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute8.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.U8:
                        MarshalAsAttribute myMarshalAsAttribute9 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute9 == null || myMarshalAsAttribute9.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.9", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute9.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.R4:
                        MarshalAsAttribute myMarshalAsAttribute10 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute10 == null || myMarshalAsAttribute10.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.10", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute10.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.R8:
                        MarshalAsAttribute myMarshalAsAttribute11 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute11 == null || myMarshalAsAttribute11.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("001.11", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute11.Value.ToString());
                            retVal = false;
                        }
                        break;
                }
                #endregion
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Initialize a instance of MarshalAsAttribute Class 2");
        try
        {
            for (int i = 19; i <= 23; i++)
            {
                UnmanagedType unmanagedType = (UnmanagedType)i;
                #region Switch
                switch (unmanagedType)
                {

                    case UnmanagedType.LPStr:
                        MarshalAsAttribute myMarshalAsAttribute2 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute2 == null || myMarshalAsAttribute2.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("003.2", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute2.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.LPWStr:
                        MarshalAsAttribute myMarshalAsAttribute3 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute3 == null || myMarshalAsAttribute3.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("003.3", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute3.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.LPTStr:
                        MarshalAsAttribute myMarshalAsAttribute4 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute4 == null || myMarshalAsAttribute4.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("003.4", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute4.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.ByValTStr:
                        MarshalAsAttribute myMarshalAsAttribute5 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute5 == null || myMarshalAsAttribute5.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("003.5", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute5.Value.ToString());
                            retVal = false;
                        }
                        break;
                }
                #endregion
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
         bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Initialize a instance of MarshalAsAttribute Class 3");
        try
        {
            for (int i = 25; i <= 38; i++)
            {
                UnmanagedType unmanagedType = (UnmanagedType)i;
                #region Switch
                switch (unmanagedType)
                {
                    case UnmanagedType.IUnknown:
                        MarshalAsAttribute myMarshalAsAttribute6 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute6 == null || myMarshalAsAttribute6.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("005.1", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute6.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.Struct:
                        MarshalAsAttribute myMarshalAsAttribute8 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute8 == null || myMarshalAsAttribute8.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("005.3", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute8.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.ByValArray:
                        MarshalAsAttribute myMarshalAsAttribute11 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute11 == null || myMarshalAsAttribute11.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("005.6", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute11.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.SysInt:
                        MarshalAsAttribute myMarshalAsAttribute12 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute12 == null || myMarshalAsAttribute12.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("005.7", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute12.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.SysUInt:
                        MarshalAsAttribute myMarshalAsAttribute13 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute13 == null || myMarshalAsAttribute13.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("005.8", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute13.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.FunctionPtr:
                        MarshalAsAttribute myMarshalAsAttribute18 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute18 == null || myMarshalAsAttribute18.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("005.13", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute18.Value.ToString());
                            retVal = false;
                        }
                        break;                    
                }
                #endregion
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Initialize a instance of MarshalAsAttribute Class 4");
        try
        {
            for (int i = 40; i <= 45; i++)
            {
                UnmanagedType unmanagedType = (UnmanagedType)i;
                #region Switch
                switch (unmanagedType)
                {
                    case UnmanagedType.AsAny:
                        MarshalAsAttribute myMarshalAsAttribute1 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute1 == null || myMarshalAsAttribute1.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("007.1", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute1.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.LPArray:
                        MarshalAsAttribute myMarshalAsAttribute2 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute2 == null || myMarshalAsAttribute2.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("007.2", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute2.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.LPStruct:
                        MarshalAsAttribute myMarshalAsAttribute3 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute3 == null || myMarshalAsAttribute3.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("007.3", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute3.Value.ToString());
                            retVal = false;
                        }
                        break;
                    case UnmanagedType.Error:
                        MarshalAsAttribute myMarshalAsAttribute5 = new MarshalAsAttribute(unmanagedType);
                        if (myMarshalAsAttribute5 == null || myMarshalAsAttribute5.Value != (UnmanagedType)unmanagedType)
                        {
                            TestLibrary.TestFramework.LogError("007.4", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute5.Value.ToString());
                            retVal = false;
                        }
                        break;
                }
                #endregion
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

}