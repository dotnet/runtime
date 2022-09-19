// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
//test case for delegate GetHashCode method.
namespace DelegateTest
{
    delegate bool booldelegate();
    delegate void voiddelegate();
    delegate bool booldelegate1();
    delegate bool booldelegate2(string str);
    public class DelegateGetHashCode
    {


        public static int Main()
        {
            DelegateGetHashCode DelegateGetHashCode = new DelegateGetHashCode();

            TestLibrary.TestFramework.BeginTestCase("DelegateGetHashCode");

            if (DelegateGetHashCode.RunTests())
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
            retVal = PosTest8() && retVal;
            return retVal;
        }

        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        // one delegate object  is booldelegate
        // the other is voiddelegate
        public bool PosTest1()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest1: hash code of two different delegate object is not equal,the two delegate callback different function. ");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate = new booldelegate(new TestClass(1).StartWork_Bool );
                voiddelegate workDelegate1 = new voiddelegate(new TestClass(1).StartWork_Void);
                if (workDelegate.GetHashCode() == workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("001", "HashCode is not excepted ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

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
        // one delegate object  is booldelegate
        // the other is booldelegate1
        public bool PosTest2()
        {
            bool retVal = true;
            //Type,target, method, and invocation list
            TestLibrary.TestFramework.BeginScenario("PosTest2: hash code of two different delegate object even though  they invoke the same function  is not equal ");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate = new booldelegate(new TestClass(1).StartWork_Bool);
                booldelegate1 workDelegate1 = new booldelegate1(new TestClass(1).StartWork_Bool);
                if (workDelegate.GetHashCode() == workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("003", "HashCode is not excepted ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

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
        // the same delegate object  is booldelegate
        public bool PosTest3()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest3: Use the same type's same  method to create two delegate which delegate object is the same,their hashcode is equal");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate = new booldelegate(TestClass.Working_Bool);
                booldelegate workDelegate1 = new booldelegate(TestClass.Working_Bool);
                if (workDelegate.GetHashCode() != workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("005", "HashCode is not excepted ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

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
        // the same delegate object  is booldelegate
        public bool PosTest4()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest4: Use the same type's different static method to create two delegate which delegate object is the same,their hashcode is equal ");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate1= new booldelegate(TestClass.Working_Bool);
                booldelegate workDelegate = new booldelegate(TestClass.Completed_Bool);
                if (workDelegate.GetHashCode() != workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("007", "HashCode is not excepted ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

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
        // the same delegate object  is booldelegate
        public bool PosTest6()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest6:  Use the different type's same static method to create two delegate ,which delegate object is the same,their hashcode is equal");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate = new booldelegate(TestClass.Completed_Bool);
                booldelegate workDelegate1 = new booldelegate(TestClass1.Completed_Bool);

                if (workDelegate.GetHashCode()!=workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("011", "HashCode is not excepted");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        // the same delegate object  is booldelegate
        public bool PosTest7()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest7:  Use the different instance's same instance method to create two delegate which delegate object is the same, their hashcode is different");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate = new booldelegate(new TestClass(1).StartWork_Bool);
                booldelegate workDelegate1 = new booldelegate(new TestClass1(2).StartWork_Bool );

                if (workDelegate.GetHashCode()==workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("013", "HashCode is not excepted ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1();

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        // one delegate object  is booldelegate
        // the other is booldelegate2
        public bool PosTest8()
        {
            bool retVal = true;
            //Type,target, method, and invocation list
            TestLibrary.TestFramework.BeginScenario("PosTest8: hash code of two delegate object is not equal,the two delegate callback different function. ");

            try
            {
                DelegateGetHashCode delctor = new DelegateGetHashCode();
                booldelegate workDelegate = new booldelegate(new TestClass(1).StartWork_Bool);
                booldelegate2 workDelegate1 = new booldelegate2(new TestClass(1).StartWork_Bool);
                if (workDelegate.GetHashCode() == workDelegate1.GetHashCode())
                {
                    TestLibrary.TestFramework.LogError("015", "HashCode is not excepted ");
                    retVal = false;
                }

                workDelegate();
                workDelegate1("hello");

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
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
        public bool StartWork_Bool(string str)
        {
            TestLibrary.TestFramework.LogInformation("TestClass's StartWork_Bool method  is running. id=" + this.id +" "+ "message=" + str);
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
        public void StartWork_Void()
        {
            TestLibrary.TestFramework.LogInformation("TestClass1's StartWork_Bool method  is running. id=" + this.id);

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
