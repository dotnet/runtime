// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
//test case for delegate Equals method.
namespace DelegateTest
{
    delegate bool booldelegate();
    public class DelegateEquals
    {

        object starkWork;

        public static int Main()
        {
            DelegateEquals DelegateEquals = new DelegateEquals();

            TestLibrary.TestFramework.BeginTestCase("DelegateEquals");

            if (DelegateEquals.RunTests())
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
            retVal = PosTest6() && retVal;
            retVal = PosTest7() && retVal;
            return retVal;
        }

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest1()
        {
            bool retVal = true;
            //Type,target, method, and invocation list
            TestLibrary.TestFramework.BeginScenario("PosTest1: Use one delegate object to instance the other delegate object,then use equals method to compare");

            try
            {
                DelegateEquals delctor = new DelegateEquals();
                delctor.starkWork = new booldelegate(new TestClass(1).StartWork_Bool);
                booldelegate workDelegate = (booldelegate)delctor.starkWork;
                if(GetCompareResult(workDelegate ,(booldelegate)delctor.starkWork))
                {
                    if (!workDelegate.Equals((booldelegate)delctor.starkWork))
                    {
                        TestLibrary.TestFramework.LogError("001", "Equals method return error ");
                        retVal = false;
                    }
                }
                else
                {
                    TestLibrary.TestFramework.LogError("002", "compare condition is error  ");
                    retVal = false;
                }
                workDelegate();
                ((booldelegate)delctor.starkWork)();

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

            TestLibrary.TestFramework.BeginScenario("PosTest2: Use the same instance's same instance method to create two different delegate ,then use equals method to compare");

            try
            {
                DelegateEquals delctor = new DelegateEquals();
                TestClass tcInstance = new TestClass(2);
                delctor.starkWork = new booldelegate(tcInstance.StartWork_Bool);
                booldelegate workDelegate = new booldelegate(tcInstance.StartWork_Bool);

                if (GetCompareResult(workDelegate, (booldelegate)delctor.starkWork))
                {
                    if (!workDelegate.Equals((booldelegate)delctor.starkWork))
                    {
                        TestLibrary.TestFramework.LogError("004", "Equals method return error ");
                        retVal = false;
                    }
                }
                else
                {
                    TestLibrary.TestFramework.LogError("005", "compare condition is error  ");
                    retVal = false;
                }
                workDelegate();
                ((booldelegate)delctor.starkWork)();

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

            TestLibrary.TestFramework.BeginScenario("PosTest3: Use the same type's same static method to create two delegate ,then use equals method to compare");

            try
            {
                DelegateEquals delctor = new DelegateEquals();
                delctor.starkWork = new booldelegate(TestClass.Working_Bool);
                booldelegate workDelegate = new booldelegate(TestClass.Working_Bool);
                if (GetCompareResult(workDelegate, (booldelegate)delctor.starkWork))
                {
                    if (!workDelegate.Equals((booldelegate)delctor.starkWork))
                    {
                        TestLibrary.TestFramework.LogError("007", "Equals method return error ");
                        retVal = false;
                    }
                }
                else
                {
                    TestLibrary.TestFramework.LogError("008", "compare condition is error  ");
                    retVal = false;
                }
                workDelegate();
                ((booldelegate)delctor.starkWork)();

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

            TestLibrary.TestFramework.BeginScenario("PosTest4: Use the same type's different static method to create two delegate ,then use equals method to compare");

            try
            {
                DelegateEquals delctor = new DelegateEquals();
                delctor.starkWork = new booldelegate(TestClass.Working_Bool);
                booldelegate workDelegate = new booldelegate(TestClass.Completed_Bool);
                if (workDelegate.Equals((booldelegate)delctor.starkWork))
                {
                    TestLibrary.TestFramework.LogError("010", "Equals method return error ");
                    retVal = false;
                }

                workDelegate();
                ((booldelegate)delctor.starkWork)();

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
        public bool PosTest6()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest6:  Use the different type's same static method to create two delegate ,then use equals method to compare");

            try
            {
                DelegateEquals delctor = new DelegateEquals();
                booldelegate workDelegate = new booldelegate(TestClass.Completed_Bool);
                booldelegate workDelegate1 = new booldelegate(TestClass1.Completed_Bool);

                if (workDelegate.Equals(workDelegate1))
                {
                    TestLibrary.TestFramework.LogError("014", "Equals method return error ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest7()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest7:  Use the different instance's same instance method to create two delegate ,then use equals method to compare");

            try
            {
                DelegateEquals delctor = new DelegateEquals();
                booldelegate workDelegate = new booldelegate(new TestClass(1).StartWork_Bool);
                booldelegate workDelegate1 = new booldelegate(new TestClass1(2).StartWork_Bool );

                if (workDelegate.Equals(workDelegate1))
                {
                    TestLibrary.TestFramework.LogError("016", "Equals method return error ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("017", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        //compare delegate's Type,target, method, and invocation list
        //two delegates have common  Type,target, method, and invocation list
        //return true.otherwise return false
        private bool GetCompareResult(booldelegate del1, booldelegate del2)
        {

            if (!del1.GetType().Equals(del2.GetType()))
            {
                return false;
            }
            if (!del1.Equals(del2))
            {
                return false;
            }

            return true;
        }


    }
    //create testclass for providing test method and test target.
    class TestClass
    {
        private int id;
        public TestClass(int id) { this.id = id; }
        public bool StartWork_Bool()
        {
            TestLibrary.TestFramework.LogInformation("TestClass's StartWork_Bool method  is running. id="+this.id);
            return true;
        }
        public static  bool Working_Bool()
        {
            TestLibrary.TestFramework.LogInformation("TestClass's Working_Bool method  is running .");
            return true;
        }
        public static bool Completed_Bool()
        {
            TestLibrary.TestFramework.LogInformation("TestClass's Completed_Bool method  is running .");
            return true;
        }
    }
    class TestClass1
    {
        private int id;
        public TestClass1(int id) { this.id = id; }
        public bool StartWork_Bool()
        {
            TestLibrary.TestFramework.LogInformation("TestClass1's StartWork_Bool method  is running. id="+ this.id  );
            return true;
        }
        public static bool Working_Bool()
        {
            TestLibrary.TestFramework.LogInformation("TestClass1's Working_Bool method  is running .");
            return true;
        }
        public static bool Completed_Bool()
        {
            TestLibrary.TestFramework.LogInformation("TestClass1's Completed_Bool method  is running .");
            return true;
        }
    }


}
