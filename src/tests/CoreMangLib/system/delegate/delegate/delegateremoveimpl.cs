// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
//test case for delegate RemoveImpl(System.Delegate) method.
namespace DelegateTest
{
    delegate bool booldelegate();
    public class DelegateRemoveImpl
    {

        booldelegate starkWork;

        public static int Main()
        {
            DelegateRemoveImpl delegateRemoveImpl = new DelegateRemoveImpl();

            TestLibrary.TestFramework.BeginTestCase("DelegateRemoveImpl");

            if (delegateRemoveImpl.RunTests())
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
            retVal = PosTest5() && retVal;//static method
            return retVal;
        }

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest1: Remove a function from the delegate which contains only 1 callback function");
            try
            {
                DelegateRemoveImpl delctor = new DelegateRemoveImpl();
                TestClass tcInstance = new TestClass();
                delctor.starkWork = new booldelegate(tcInstance.StartWork_Bool);
                delctor.starkWork -= new booldelegate(tcInstance.StartWork_Bool);
                if (null != delctor.starkWork)
                {
                    TestLibrary.TestFramework.LogError("001", "remove failure  " );
                    retVal = false;
                }

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest2()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest2: Remove a function which is in the InvocationList");
            try
            {
                DelegateRemoveImpl delctor = new DelegateRemoveImpl();
                TestClass tcInstance = new TestClass();
		booldelegate bStartWork_Bool = new booldelegate(tcInstance.StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(tcInstance.Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(tcInstance.Completed_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
                delctor.starkWork -= bWorking_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length != 2)
                {
                    TestLibrary.TestFramework.LogError("003", "remove failure or remove method is not in the InvocationList");
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[1].Equals(bCompleted_Bool))
                {
                    TestLibrary.TestFramework.LogError("004", " remove failure ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest3()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest3: Remove a function which is not in the InvocationList");
            try
            {
                DelegateRemoveImpl delctor = new DelegateRemoveImpl();
		booldelegate bStartWork_Bool = new booldelegate(new TestClass().StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(new TestClass().Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(new TestClass().Completed_Bool);
		booldelegate bOther_bool = new booldelegate(TestClass1.Other_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
		Delegate[] beforeList = delctor.starkWork.GetInvocationList();
                delctor.starkWork -= bOther_bool;
		Delegate[] afterList = delctor.starkWork.GetInvocationList();
		if (beforeList.Length != afterList.Length)
                 {
                    TestLibrary.TestFramework.LogError("006",
		String.Format("Remove changed invocation list length from {0} to {1}", beforeList.Length,
		afterList.Length));
                    retVal = false;
                } else {
		for (int i=0; i<beforeList.Length; i++)
			if (!beforeList[i].Equals(afterList[i]))
			{
			TestLibrary.TestFramework.LogError("006a", String.Format(
			 "Invocation lists differ at element {0}", i));
			retVal=false;
			}
		}
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest4()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest4: Remove a function which is in the InvocationList and not only one method");
            try
            {
                DelegateRemoveImpl delctor = new DelegateRemoveImpl();
                TestClass tcInstance = new TestClass();
		booldelegate bStartWork_Bool = new booldelegate(tcInstance.StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(tcInstance.Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(tcInstance.Completed_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
                delctor.starkWork -= bStartWork_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length !=3)
                {
                    TestLibrary.TestFramework.LogError("009", "remove failure: " + invocationList.Length);
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[1].Equals(bWorking_Bool)
                    || !delctor.starkWork.GetInvocationList()[2].Equals(bCompleted_Bool))
                {
                    TestLibrary.TestFramework.LogError("010", " remove failure ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("011", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest5()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest5: Remove a function which is in the InvocationList and not only one method ,method is static method");
            try
            {
                DelegateRemoveImpl delctor = new DelegateRemoveImpl();
		booldelegate bStartWork_Bool = new booldelegate(TestClass1.StartWork_Bool);
		booldelegate bWorking_Bool   = new booldelegate(TestClass1.Working_Bool);
		booldelegate bCompleted_Bool = new booldelegate(TestClass1.Completed_Bool);

                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bStartWork_Bool;
                delctor.starkWork += bWorking_Bool;
                delctor.starkWork += bCompleted_Bool;
                delctor.starkWork -= bStartWork_Bool;
                Delegate[] invocationList = delctor.starkWork.GetInvocationList();
                if (invocationList.Length != 3)
                {
                    TestLibrary.TestFramework.LogError("012", "remove failure: " + invocationList.Length);
                    retVal = false;
                }
                if (!delctor.starkWork.GetInvocationList()[0].Equals(bStartWork_Bool)
                    || !delctor.starkWork.GetInvocationList()[1].Equals(bWorking_Bool)
                    || !delctor.starkWork.GetInvocationList()[2].Equals(bCompleted_Bool))
                {
                    TestLibrary.TestFramework.LogError("013", " remove failure ");
                    retVal = false;
                }
                delctor.starkWork();
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
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
    class TestClass1
    {
        public static bool StartWork_Bool()
        {
            TestLibrary.TestFramework.LogInformation("StartWork_Bool method  is running .");
            return true;
        }
        public static bool Working_Bool()
        {
            TestLibrary.TestFramework.LogInformation("Working_Bool method  is running .");
            return true;
        }
        public static bool Completed_Bool()
        {
            TestLibrary.TestFramework.LogInformation("Completed_Bool method  is running .");
            return true;
        }
	public static bool Other_Bool()
	{
            TestLibrary.TestFramework.LogInformation("Other_Bool method  is running .");
	    return true;
	}
    }
}
