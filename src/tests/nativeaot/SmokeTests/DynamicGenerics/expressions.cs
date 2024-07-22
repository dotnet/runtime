// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using RuntimeLibrariesTest;
using TypeOfRepo;


namespace Expressions
{
    #region Test Types
    public class SomeClass
    {
        public int IntField = 15;

        public static int StaticIntField = 1985;

        public string StringField = "StringFieldHere";

        public int IntMethod()
        {
            return 16;
        }

        public static string StaticStringMethod()
        {
            return "StaticStringMethod";
        }
    }

    public class SomeGenericClass<T, U>
    {
        public T GenericField1;
        public U GenericField2;

        public static T GenericStaticField1;
        public static U GenericStaticField2;

        public int IntField = 666;

        public static string StaticStringField = "StaticStringFieldHere";

        public string MethodWithParameter(T parameter)
        {
            return typeof(T) + "::" + parameter.ToString();
        }
        public string MethodWithParameter(U parameter)
        {
            return typeof(U) + "::" + parameter.ToString();
        }
        public string MethodWithParameter<M, N>(M m_parameter, N n_parameter)
        {
            return typeof(M) + "::" + m_parameter.ToString() + "~" + typeof(N) + "::" + n_parameter.ToString();
        }

        public void Method() { }

        public string CheckMethodExpression(T value)
        {
            Expression<Func<T, string>> expr = x => this.MethodWithParameter(x);
            return expr.Compile()(value);
        }
        public string CheckMethodExpression(U value)
        {
            Expression<Func<U, string>> expr = x => this.MethodWithParameter(x);
            return expr.Compile()(value);
        }

        public T CheckFieldExpression(T value)
        {
            GenericField1 = value;
            Expression<Func<T>> expr = () => this.GenericField1;
            return expr.Compile()();
        }
        public U CheckFieldExpression(U value)
        {
            GenericField2 = value;
            Expression<Func<U>> expr = () => this.GenericField2;
            return expr.Compile()();
        }
    }

    public class SomeGenericClass2<T, U>
    {
        public static T One;
        public static T Two;
        public static U Three;
        public static U Four;

        public static string CheckInternalGenericExpression(T one, T two)
        {
            One = one;
            Two = two;
            Expression<Func<string>> e = () => ConcatList1<List<T>>(new List<T> { One, Two });
            return e.Compile()();
        }
        public static string CheckInternalGenericExpression(U three, U four)
        {
            Three = three;
            Four = four;
            Expression<Func<string>> e = () => ConcatList2<List<U>>(new List<U> { Three, Four });
            return e.Compile()();
        }

        public static string ConcatList1<M>(M m)
        {
            List<T> x = m as List<T>;
            if (x != null)
            {
                return x[0].ToString() + x[1].ToString();
            }
            return null;
        }
        public static string ConcatList2<M>(M m)
        {
            List<U> x = m as List<U>;
            if (x != null)
            {
                return x[0].ToString() + x[1].ToString();
            }
            return null;
        }
    }

    public class SomeDerivedGenericClass<T, U> : SomeGenericClass<T, U>
    {
    }

    public struct SomeGenericStruct<T, U>
    {
        public void Method() { }
    }

    public class MyType1
    {
        private string _n;
        public MyType1(string s) { _n = s; }
        public override string ToString() { return "MyType1::" + _n; }
    }
    public class MyType2
    {
        private int _n;
        public MyType2(int s) { _n = s; }
        public override string ToString() { return "MyType2::" + _n; }
    }

    public struct MyStructType1
    {
        private string _n;
        public MyStructType1(string s) { _n = s; }
        public override string ToString() { return "MyStructType1::" + _n; }
    }
    public struct MyStructType2
    {
        private int _n;
        public MyStructType2(int s) { _n = s; }
        public override string ToString() { return "MyStructType2::" + _n; }
    }
    #endregion


    public class TestRunner<T>
    {
        public void RunTest<U>(T tval1, T tval2, U uval1, U uval2)
        {
            // FIELDS
            {
                Expression<Func<SomeClass, int>> expr = x => x.IntField;
                Assert.AreEqual(expr.Compile()(new SomeClass()), 15);
            }

            {
                Expression<Func<SomeClass, string>> expr = x => x.StringField;
                Assert.AreEqual(expr.Compile()(new SomeClass()), "StringFieldHere");
            }

            {
                Expression<Func<int>> expr = () => SomeClass.StaticIntField;
                Assert.AreEqual(expr.Compile()(), 1985);
            }

            {
                Expression<Func<SomeGenericClass<T, U>, int>> expr = x => x.IntField;
                Assert.AreEqual(expr.Compile()(new SomeGenericClass<T, U>()), 666);
            }

            {
                Expression<Func<string>> expr = () => SomeGenericClass<U, T>.StaticStringField;
                Assert.AreEqual(expr.Compile()(), "StaticStringFieldHere");
            }

            {
                SomeGenericClass<T, U> sgc = new SomeGenericClass<T, U>();
                sgc.GenericField1 = tval1;
                sgc.GenericField2 = uval1;
                Expression<Func<SomeGenericClass<T, U>, T>> expr1 = x => x.GenericField1;
                Assert.AreEqual(expr1.Compile()(sgc), tval1);
                Expression<Func<SomeGenericClass<T, U>, U>> expr2 = x => x.GenericField2;
                Assert.AreEqual(expr2.Compile()(sgc), uval1);
            }

            {
                SomeGenericClass<U, T> sgc = new SomeGenericClass<U, T>();
                sgc.GenericField1 = uval2;
                sgc.GenericField2 = tval2;
                Expression<Func<SomeGenericClass<U, T>, U>> expr1 = x => x.GenericField1;
                Assert.AreEqual(expr1.Compile()(sgc), uval2);
                Expression<Func<SomeGenericClass<U, T>, T>> expr2 = x => x.GenericField2;
                Assert.AreEqual(expr2.Compile()(sgc), tval2);
            }

            {
                SomeGenericClass<T, U>.GenericStaticField1 = tval1;
                SomeGenericClass<T, U>.GenericStaticField2 = uval2;
                Expression<Func<T>> expr1 = () => SomeGenericClass<T, U>.GenericStaticField1;
                Assert.AreEqual(expr1.Compile()(), tval1);
                Expression<Func<U>> expr2 = () => SomeGenericClass<T, U>.GenericStaticField2;
                Assert.AreEqual(expr2.Compile()(), uval2);
            }

            {
                SomeGenericClass<T, U> sgc = new SomeGenericClass<T, U>();
                Assert.AreEqual(sgc.CheckFieldExpression(tval2), tval2);
                Assert.AreEqual(sgc.CheckFieldExpression(uval1), uval1);
            }


            // METHODS
            {
                Expression<Func<SomeGenericClass<T, U>, T, string>> expr1 = (x, y) => x.MethodWithParameter(y);
                Expression<Func<SomeGenericClass<T, U>, U, string>> expr2 = (x, y) => x.MethodWithParameter(y);
                Assert.AreEqual(expr1.Compile()(new SomeGenericClass<T, U>(), tval2), typeof(T) + "::" + tval2.ToString());
                Assert.AreEqual(expr2.Compile()(new SomeGenericClass<T, U>(), uval2), typeof(U) + "::" + uval2.ToString());
            }

            {
                SomeGenericClass<U, T> sgc = new SomeGenericClass<U, T>();
                Assert.AreEqual(sgc.CheckMethodExpression(tval1), tval1.GetType() + "::" + tval1.ToString());
                Assert.AreEqual(sgc.CheckMethodExpression(uval1), uval1.GetType() + "::" + uval1.ToString());
            }

            {
                Expression<Func<SomeGenericClass<T, U>, T, U, string>> expr1 = (x, y, z) => x.MethodWithParameter<T, U>(y, z);
                Assert.AreEqual(expr1.Compile()(new SomeGenericClass<T, U>(), tval1, uval1), typeof(T) + "::" + tval1 + "~" + typeof(U) + "::" + uval1);

                Expression<Func<SomeGenericClass<T, U>, U, T, string>> expr2 = (x, y, z) => x.MethodWithParameter<U, T>(y, z);
                Assert.AreEqual(expr2.Compile()(new SomeGenericClass<T, U>(), uval2, tval2), typeof(U) + "::" + uval2 + "~" + typeof(T) + "::" + tval2);

                Expression<Func<SomeGenericClass<T, T>, U, U, string>> expr3 = (x, y, z) => x.MethodWithParameter<U, U>(y, z);
                Assert.AreEqual(expr3.Compile()(new SomeGenericClass<T, T>(), uval1, uval2), typeof(U) + "::" + uval1 + "~" + typeof(U) + "::" + uval2);
            }

            {
                Assert.AreEqual(SomeGenericClass2<T, U>.CheckInternalGenericExpression(uval1, uval2), uval1.ToString() + uval2.ToString());
                Assert.AreEqual(SomeGenericClass2<U, T>.CheckInternalGenericExpression(uval1, uval2), uval1.ToString() + uval2.ToString());
                Assert.AreEqual(SomeGenericClass2<T, U>.CheckInternalGenericExpression(tval2, tval1), tval2.ToString() + tval1.ToString());
                Assert.AreEqual(SomeGenericClass2<U, T>.CheckInternalGenericExpression(tval2, tval1), tval2.ToString() + tval1.ToString());
            }

#if UNIVERSAL_GENERICS
            {
                Expression<Func<U>> expr1 = ()=>default(U);
                Assert.AreEqual(expr1.Compile()(), default(U));
            }
            RunTestDefaultExpression<U>();
#endif
#if false
            // BUG 971950
            // MethodInfo Tests
            {
                // Test an instance method on one instantiation of a generic type
                SomeGenericClass<T, U> test1 = new SomeGenericClass<T, U>();
                Action del1 = test1.Method;
                MethodInfo mi1Linq = GetMethodInfo(() => test1.Method());
                MethodInfo mi1Reflection = typeof(SomeGenericClass<T, U>).GetTypeInfo().GetDeclaredMethod("Method");
                MethodInfo mi1Delegate = del1.GetMethodInfo();
            }
#endif
        }
        public int ARG;
        public void RunTestDefaultExpression<U>()
        {
            Expression<Func<U>> expr1 = ()=>ARG == 0 ? default(U) : (U)(object)null;

            if (ARG != 0)
            {
                ARG = 0;
            }
            Assert.AreEqual(expr1.Compile()(), default(U));
        }
    }

    public class ExpressionsTesting
    {
        [TestMethod]
        public static void TestLdTokenResults()
        {
            var dynamicType = TypeOf.E_TestRunner.MakeGenericType(TypeOf.E_MyType1);
            var instance = Activator.CreateInstance(dynamicType);

            var testMethod = dynamicType.GetTypeInfo().GetDeclaredMethod("RunTest");
            testMethod = testMethod.MakeGenericMethod(TypeOf.E_MyType2);

            testMethod.Invoke(instance, new object[] { new MyType1("Dynamic1"), new MyType1("Dynamic2"), new MyType2(123), new MyType2(456) });
        }

        [TestMethod]
        public static void TestLdTokenResultsWithStructTypes()
        {
#if UNIVERSAL_GENERICS
            var dynamicType = TypeOf.E_TestRunner.MakeGenericType(typeof(MyStructType1));
            var instance = Activator.CreateInstance(dynamicType);

            var testMethod = dynamicType.GetTypeInfo().GetDeclaredMethod("RunTest");
            testMethod = testMethod.MakeGenericMethod(typeof(MyStructType2));

            testMethod.Invoke(instance, new object[] { new MyStructType1("Dynamic1"), new MyStructType1("Dynamic2"), new MyStructType2(123), new MyStructType2(456) });
#endif
        }
    }
}
