// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Collections;
using Xunit;
//create for delegate combine(delegate a,delegate b) testing
namespace DelegateTest
{
    delegate bool booldelegate();
    delegate void voiddelegate();

    public class DelegateCombineImpl
    {
        const string c_StartWork = "Start";
        const string c_Working = "Working";
        enum identify_null
        {
            c_Start_null_true,
            c_Start_null_false,
            c_Working_null_true,
            c_Working_null_false,
            c_Start_null_false_duplicate

        }
        booldelegate starkWork;
        booldelegate working;
        [Fact]
        public static int TestEntryPoint()
        {
            DelegateCombineImpl delegateCombineImpl = new DelegateCombineImpl();

            TestLibrary.TestFramework.BeginTestCase("DelegateCombineImpl");



            if (delegateCombineImpl.RunTests())
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
                booldelegate delegate1 = new booldelegate(new TestClass().Working_Bool);
                if (!CombineImpl(delegate1,identify_null.c_Start_null_false))
                {
                    TestLibrary.TestFramework.LogError("001", "delegate combineimpl is not successful ");
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

                booldelegate delegate1 = null;
                if (!CombineImpl(delegate1, identify_null.c_Working_null_false))
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

                booldelegate delegate1 = new booldelegate(new TestClass().StartWork_Bool);
                if (!CombineImpl(delegate1, identify_null.c_Working_null_true   ))
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
                booldelegate delegate1 = null;
                if (!CombineImpl(delegate1, identify_null.c_Working_null_true))
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
        public bool PosTest5()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest5: combine three delegate ,first is  not null and the two others  entry that refer to the same method on the same object");

            try
            {
                booldelegate delegate1 = new booldelegate(new TestClass().Working_Bool);
                if (!CombineImpl(delegate1, identify_null.c_Start_null_false_duplicate ))
                {
                    TestLibrary.TestFramework.LogError("009", "delegate combine is not successful ");
                    retVal = false;
                }

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        private bool CombineImpl(booldelegate delegatesrc,identify_null start)
        {
            DelegateCombineImpl delctor = new DelegateCombineImpl();
            TestClass testinstance = new TestClass();

            string sFlag = string.Empty;
            string sFlagAdd=string.Empty ;
            booldelegate combineImpl = delegatesrc;
            if (start == identify_null.c_Start_null_false)
            {
                delctor.starkWork = new booldelegate(testinstance.StartWork_Bool);
                combineImpl += (booldelegate)delctor.starkWork;
                sFlagAdd = c_StartWork;

            }
            else if (start == identify_null.c_Start_null_false_duplicate )
            {
                delctor.starkWork = new booldelegate(testinstance.StartWork_Bool);
                combineImpl += (booldelegate)delctor.starkWork;
                sFlagAdd = c_StartWork;
                //The invocation list can contain duplicate entries; that is, entries that refer to the same method on the same object.
                combineImpl += (booldelegate)delctor.starkWork;
                sFlagAdd += sFlagAdd;
            }
            else if(start==identify_null.c_Start_null_true )
            {
                delctor.starkWork = null;
                combineImpl += (booldelegate)delctor.starkWork;
            }
            else if (start == identify_null.c_Working_null_false)
            {
                delctor.working  = new booldelegate(testinstance.Working_Bool );
                combineImpl += (booldelegate)delctor.working;
                 sFlagAdd=c_Working  ;
            }
            else
            {
                delctor.working = null;
                combineImpl += (booldelegate)delctor.working;
            }

            if (combineImpl == null)
            {
                return true;
            }

            for (IEnumerator itr = combineImpl.GetInvocationList().GetEnumerator(); itr.MoveNext(); )
            {
                booldelegate bd = (booldelegate)itr.Current;
                //the filter is to get the delegate which is appended through equals method.
                if (bd.Equals(delctor.starkWork))
                {
                    sFlag += c_StartWork;
                }
                if (bd.Equals(delctor.working))
                {
                    sFlag += c_Working;
                }
            }
            combineImpl();
            //judge delegate is appended  to the end of the invocation list of the current
            if (sFlag == sFlagAdd)
                return true;
            else
                return false;
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
