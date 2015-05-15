// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace MethodInfoTests
{
    public class Test
    {
        //Invoke a method using a matching MethodInfo from another/parent class. 
        [Fact]
        public static void TestInvokeMethod1()
        {
            MethodInfo mi = null;
            Co1333_b clsObj = new Co1333_b();
            int retVal = -1;
            int expectedVal = 0;

            mi = getMethod(typeof(Co1333_b), "ReturnTheIntPlus");
            retVal = (int)mi.Invoke(clsObj, (Object[])null);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }

        //Invoke a method using a matching MethodInfo from another/parent class. 
        [Fact]
        public static void TestInvokeMethod2()
        {
            MethodInfo mi = null;
            Co1333_b clsObj = new Co1333_b1();
            int retVal = -1;
            int expectedVal = 1;

            mi = getMethod(typeof(Co1333_b), "ReturnTheIntPlus");
            retVal = (int)mi.Invoke(clsObj, (Object[])null);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }



        // Invoke a method that requires Reflection to box a primitive integer for the invoked method's parm. // Bug 10829 
        [Fact]
        public static void TestInvokeMethod3()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            string retVal = "";
            string expectedVal = "42";
            Object[] varParams =
            {
                (int)42
            };

            mi = getMethod(typeof(Co1333Invoke), "ConvertI4ObjToString");
            retVal = (String)mi.Invoke(clsObj, varParams);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }


        //Invoke a vanilla method that takes no parms but returns an integer 
        [Fact]
        public static void TestInvokeMethod4()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            int retVal = 0;
            int expectedVal = 3;

            mi = getMethod(typeof(Co1333Invoke), "Int4ReturnThree");
            retVal = (int)mi.Invoke(clsObj, (Object[])null);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }



        //Invoke a vanilla method that takes no parms but returns an integer 
        [Fact]
        public static void TestInvokeMethod5()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            Int64 retVal = 0;
            Int64 expectedVal = Int64.MaxValue;

            mi = getMethod(typeof(Co1333Invoke), "ReturnLongMax");
            retVal = (Int64)mi.Invoke(clsObj, (Object[])null);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }


        //Invoke a method that has parameters of primative types
        [Fact]
        public static void TestInvokeMethod6()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            long retVal = 0;
            long expectedVal = 100200L;
            Object[] varParams =
            {
                ( 200 ),
                ( 100000 )
            };


            mi = getMethod(typeof(Co1333Invoke), "ReturnI8Sum");
            retVal = (long)mi.Invoke(clsObj, varParams);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }


        //Invoke a static method using null , that has parameters of primative types
        [Fact]
        public static void TestInvokeMethod7()
        {
            MethodInfo mi = null;
            int retVal = 0;
            int expectedVal = 110;
            Object[] varParams =
            {
                ( 10 ),
                ( 100 )
            };

            mi = getMethod(typeof(Co1333Invoke), "ReturnI4Sum");
            retVal = (int)mi.Invoke(null, varParams);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }



        //Invoke a static method using class object , that has parameters of primative types
        [Fact]
        public static void TestInvokeMethod8()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            int retVal = 0;
            int expectedVal = 110;
            Object[] varParams =
            {
                ( 10 ),
                ( 100 )
            };

            mi = getMethod(typeof(Co1333Invoke), "ReturnI4Sum");
            retVal = (int)mi.Invoke(clsObj, varParams);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }


        //Invoke inherited static method.
        [Fact]
        public static void TestInvokeMethod9()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            bool retVal = false;
            bool expectedVal = true;
            Object[] varParams =
            {
                ( 10 ),
            };

            mi = getMethod(typeof(Co1333_a), "IsEvenStatic");
            retVal = (bool)mi.Invoke(clsObj, varParams);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }


        //Enum reflection in method signature
        [Fact]
        public static void TestInvokeMethod10()
        {
            MethodInfo mi = null;
            Co1333Invoke clsObj = new Co1333Invoke();
            int retVal = 0;
            int expectedVal = (Int32)MyColorEnum.GREEN;
            Object[] vars = new Object[1];
            vars[0] = (MyColorEnum.RED);

            mi = getMethod(typeof(Co1333Invoke), "GetAndRetMyEnum");

            retVal = (int)mi.Invoke(clsObj, vars);

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }



        //Call Interface Method
        [Fact]
        public static void TestInvokeMethod11()
        {
            MethodInfo mi = null;
            MyClass clsObj = new MyClass();
            int retVal = 0;
            int expectedVal = 10;

            mi = getMethod(typeof(MyInterface), "IMethod");

            retVal = (int)mi.Invoke(clsObj, new object[] { });

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }



        //Call Interface Method marked as new in class
        [Fact]
        public static void TestInvokeMethod12()
        {
            MethodInfo mi = null;
            MyClass clsObj = new MyClass();
            int retVal = 0;
            int expectedVal = 20;

            mi = getMethod(typeof(MyInterface), "IMethodNew");

            retVal = (int)mi.Invoke(clsObj, new object[] { });

            Assert.True(retVal.Equals(expectedVal), String.Format("Failed! MethodInfo.Invoke did not return correct result. Expected {0} , Got {1}", expectedVal, retVal));
        }





        // Gets MethodInfo object from current class
        public static MethodInfo getMethod(string method)
        {
            return getMethod(typeof(Test), method);
        }


        //Gets MethodInfo object from a Type
        public static MethodInfo getMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    //found method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }
    }

    public class Co1333_a
    {
        public static bool IsEvenStatic(int int4a)
        { return (int4a % 2 == 0) ? true : false; }
    }


    public class Co1333_b
    {
        public virtual int ReturnTheIntPlus()
        { return (0); }
    }
    public class Co1333_b1 : Co1333_b
    {
        public override int ReturnTheIntPlus()
        { return (1); }
    }
    public class Co1333_b2 : Co1333_b
    {
        public override int ReturnTheIntPlus()
        { return (2); }
    }

    public enum MyColorEnum
    {
        RED = 1,
        YELLOW = 2,
        GREEN = 3
    }


    public class Co1333Invoke
        : Co1333_a
    {
        public MyColorEnum GetAndRetMyEnum(MyColorEnum myenum)
        {
            if (myenum == MyColorEnum.RED)
            {
                return MyColorEnum.GREEN;
            }
            else
                return MyColorEnum.RED;
        }

        public String ConvertI4ObjToString(Object p_obj)  // Bug 10829.  Expecting Integer4 type of obj.
        {
            return (p_obj.ToString());
        }

        public int Int4ReturnThree()
        {
            return (3);
        }

        public long ReturnLongMax()
        {
            return (Int64.MaxValue);
        }

        public long ReturnI8Sum(int iFoo, long lFoo)
        {
            return ((long)iFoo + lFoo);
        }

        static public int ReturnI4Sum(int i4Foo, int i4Bar)
        {
            return (i4Foo + i4Bar);
        }
    }

    public interface MyInterface
    {
        int IMethod();
        int IMethodNew();
    }


    public class MyBaseClass : MyInterface
    {
        public int IMethod() { return 10; }
        public int IMethodNew() { return 20; }
    }

    public class MyClass : MyBaseClass
    {
        public new int IMethodNew() { return 200; }
    }
}
