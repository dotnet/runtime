// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
//test case for delegate GetInvocationList method.
namespace DelegateTest
{
    delegate bool booldelegate();
    public class DelegateGetInvocationList
    {

        booldelegate starkWork;

        public static int Main()
        {
            DelegateGetInvocationList delegateGetInvocationList = new DelegateGetInvocationList();

            TestLibrary.TestFramework.BeginTestCase("DelegateGetInvocationList");

            if (delegateGetInvocationList.RunTests())
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

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetInvocationList against a delegate with one function");
            try
            {
                DelegateGetInvocationList delctor = new DelegateGetInvocationList();
                booldelegate dStartWork_Bool = new booldelegate(new TestClass().StartWork_Bool);
                delctor.starkWork = dStartWork_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length != 1)
                {
                    TestLibrary.TestFramework.LogError("001", "Call GetInvocationList against a delegate with one function returns wrong result: " + invocationList.Length);
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(dStartWork_Bool))
                {
                    TestLibrary.TestFramework.LogError("002", " GetInvocationList return error method  ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest2()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetInvocationList against a delegate with muti different functions ");
            try
            {
                DelegateGetInvocationList delctor = new DelegateGetInvocationList();
		booldelegate bStartWork_Bool = new booldelegate(new TestClass().StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(new TestClass().Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(new TestClass().Completed_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length != 3)
                {
                    TestLibrary.TestFramework.LogError("004", "Call GetInvocationList against a delegate with one function returns wrong result: " + invocationList.Length);
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[1].Equals(bWorking_Bool)
                    || !delctor.starkWork.GetInvocationList()[2].Equals(bCompleted_Bool))
                {
                    TestLibrary.TestFramework.LogError("005", " GetInvocationList return error method  ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest3()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest3: Call GetInvocationList against a delegate with muti functions ,some is null");
            try
            {
                DelegateGetInvocationList delctor = new DelegateGetInvocationList();
		booldelegate bStartWork_Bool = new booldelegate(new TestClass().StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(new TestClass().Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(new TestClass().Completed_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += null;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length != 3)
                {
                    TestLibrary.TestFramework.LogError("007", "Call GetInvocationList against a delegate with one function returns wrong result: " + invocationList.Length);
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[1].Equals(bWorking_Bool)
                    || !delctor.starkWork.GetInvocationList()[2].Equals(bCompleted_Bool))
                {
                    TestLibrary.TestFramework.LogError("008", " GetInvocationList return error method  ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest4()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest4: Call GetInvocationList against a delegate with muti functions ,some of these are the same");
            try
            {
                DelegateGetInvocationList delctor = new DelegateGetInvocationList();
		booldelegate bStartWork_Bool = new booldelegate(new TestClass().StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(new TestClass().Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(new TestClass().Completed_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length != 4)
                {
                    TestLibrary.TestFramework.LogError("010", "Call GetInvocationList against a delegate with one function returns wrong result: " + invocationList.Length);
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[1].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[2].Equals(bWorking_Bool)
                    || !delctor.starkWork.GetInvocationList()[3].Equals(bCompleted_Bool))
                {
                    TestLibrary.TestFramework.LogError("011", " GetInvocationList return error method  ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

    }
    //create testclass for providing test method and test target.
    class TestClass
    {
        public bool StartWork_Bool()
        {
            TestLibrary.TestFramework.LogInformation("StartWork_Bool method  is running .");
            return true;
        }
        public bool Working_Bool()
        {
            TestLibrary.TestFramework.LogInformation("Working_Bool method  is running .");
            return true;
        }
        public bool Completed_Bool()
        {
            TestLibrary.TestFramework.LogInformation("Completed_Bool method  is running .");
            return true;
        }
    }


}
