// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Collections;
//create for delegate combine(delegate a,delegate b) testing
namespace DelegateTest
{
    delegate bool booldelegate();
    delegate void voiddelegate();
    delegate void delegatecombine(booldelegate delegate1, booldelegate delegate2);

    public class DelegateCombine1
    {
        const string c_StartWork = "Start";
        const string c_Working = "Working";
        enum identify_null
        {
            c_Start_null_true,
            c_Start_null_false,
            c_Working_null_true,
            c_Working_null_false

        }
        booldelegate starkWork;
        booldelegate working;
        voiddelegate completeWork;
        public static int Main()
        {
            DelegateCombine1 delegateCombine1 = new DelegateCombine1();

            TestLibrary.TestFramework.BeginTestCase("DelegateCombine1");



            if (delegateCombine1.RunTests())
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
            TestLibrary.TestFramework.LogInformation("[Negative]");
            retVal = NegTest1() && retVal;
            return retVal;
        }

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest1: combine two  delegates which are not null");

            try
            {
                if (GetInvocationListFlag(identify_null.c_Start_null_false, identify_null.c_Working_null_false ) != c_StartWork + c_Working)
                {
                    TestLibrary.TestFramework.LogError("001", "delegate combine is not successful ");
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

            TestLibrary.TestFramework.BeginScenario("PosTest2: combine two delegate ,first is null,second is not null");

            try
            {

                if (GetInvocationListFlag(identify_null.c_Start_null_true, identify_null.c_Working_null_false ) != c_Working)
                {
                    TestLibrary.TestFramework.LogError("003", "delegate combine is not successful ");
                    retVal = false;
                }


            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest3()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest3: combine two delegate ,first is not null,second is  null");

            try
            {

                if (GetInvocationListFlag( identify_null.c_Start_null_false, identify_null.c_Working_null_true ) != c_StartWork)
                {
                    TestLibrary.TestFramework.LogError("005", "delegate combine is not successful ");
                    retVal = false;
                }


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
        public bool PosTest4()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest4: combine two delegate ,first is  null and second is  null");

            try
            {
                if (GetInvocationListFlag( identify_null.c_Start_null_true , identify_null.c_Working_null_true) != string.Empty )
                {
                    TestLibrary.TestFramework.LogError("007", "delegate combine is not successful ");
                    retVal = false;
                }


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
        public bool NegTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("NegTest1:Both a and b are not a null reference , and a and b are not instances of the same delegate type.");

            try
            {
                DelegateCombine1 delctor = new DelegateCombine1();
                TestClass testinstance = new TestClass();
                delctor.starkWork = new booldelegate(testinstance.StartWork_Bool);
                delctor.completeWork = new voiddelegate(testinstance.CompleteWork_Void);

                object obj = Delegate.Combine(delctor.starkWork, delctor.completeWork);

                TestLibrary.TestFramework.LogError("009", "a ArgumentException should be throw ");
                retVal = false;

            }
            catch (ArgumentException)
            {

            }

            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        private string GetInvocationListFlag(identify_null start,identify_null working)
        {
            DelegateCombine1 delctor = new DelegateCombine1();
            TestClass testinstance = new TestClass();

            string sFlag = string.Empty;
            if (start == identify_null.c_Start_null_false)
            {
                delctor.starkWork = new booldelegate(testinstance.StartWork_Bool);
            }
            else
            {
                delctor.starkWork = null;
            }
            if (working == identify_null.c_Working_null_false)
            {
                delctor.working  = new booldelegate(testinstance.Working_Bool );
            }
            else
            {
                delctor.working = null;
            }
            booldelegate combine = (booldelegate)Delegate.Combine(delctor.starkWork, delctor.working);
            if (combine == null)
            {
                return string.Empty;
            }

            for (IEnumerator itr = combine.GetInvocationList().GetEnumerator(); itr.MoveNext(); )
            {
                booldelegate bd = (booldelegate)itr.Current;
                if (bd.Equals(delctor.starkWork))
                {
                    sFlag += c_StartWork;
                }
                if (bd.Equals(delctor.working))
                {
                    sFlag += c_Working;
                }
            }
            combine();
            return sFlag;
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
        public void CompleteWork_Void()
        {
            TestLibrary.TestFramework.LogInformation("CompleteWork_Void method  is running .");

        }
    }


}
