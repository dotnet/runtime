// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MethodInfoPropertyTests
    {
        public static IEnumerable<object> TestReturnTypeData()
        {
            yield return new object[] { "GetMethod", "MethodInfo" };
            yield return new object[] { "DummyMethod1", "void" };
            yield return new object[] { "DummyMethod2", "Int32" };
            yield return new object[] { "DummyMethod3", "string" };
            yield return new object[] { "DummyMethod4", "String[]" };
            yield return new object[] { "DummyMethod5", "Boolean" };
        }

        //Verify ReturnType for Methods
        [Theory]
        [MemberData(nameof(TestReturnTypeData))]

        public static void TestReturnType(string methodName, string returnType)
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoPropertyTests), methodName);

            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(mi.ReturnType.Name.Equals(returnType, StringComparison.CurrentCultureIgnoreCase));
        }

        public static IEnumerable<object> TestReturnParamData()
        {
            yield return new object[] { "DummyMethod5"};
            yield return new object[] { "DummyMethod4"};
            yield return new object[] { "DummyMethod3"};
            yield return new object[] { "DummyMethod2"};
            yield return new object[] { "DummyMethod1"};
        }

        //Verify ReturnParameter for Methods
        [Theory]
        [MemberData(nameof(TestReturnParamData))]

        public static void TestReturnParam(string methodName)
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoPropertyTests), methodName);

            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));

            Assert.Equal(mi.ReturnType, mi.ReturnParameter.ParameterType);

            Assert.Null(mi.ReturnParameter.Name);
        }

        //Verify ReturnParameter forclassA method method1
        [Fact]
        public static void TestReturnParam2()
        {
            MethodInfo mi = GetMethod(typeof(classA), "method1");
            ParameterInfo returnParam = mi.ReturnParameter;

            Assert.Equal(-1, returnParam.Position);
        }

        public static IEnumerable<object> TestPropertyData()
        {
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod5").IsVirtual, true};
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsVirtual, false};
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsPublic, true };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsPrivate, false };
            yield return new object[] { GetMethod(typeof(classA), "method1").IsStatic, true };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsStatic, false };
            yield return new object[] { GetMethod(typeof(classA), "GenericMethod").IsGenericMethod, true};
            yield return new object[] { GetMethod(typeof(classA), "method1").IsGenericMethod, false };
            yield return new object[] { GetMethod(typeof(classA), "method1").IsGenericMethodDefinition, false };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsFinal, false };
            yield return new object[] { GetMethod(typeof(classC), "virtualMethod").IsFinal, true };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsConstructor, false};
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsAbstract, false};
            yield return new object[] { GetMethod(typeof(classB), "abstractMethod").IsAbstract, true};
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsAssembly, false};
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsFamily, false};
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsFamilyAndAssembly, false };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsFamilyOrAssembly, false };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").ContainsGenericParameters, false};
            yield return new object[] { GetMethod(typeof(classA), "GenericMethod").ContainsGenericParameters, true };
            yield return new object[] { GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1").IsSpecialName, false};
            yield return new object[] { GetMethod(typeof(classC), "abstractMethod").IsHideBySig, true};
        }

        //Verify Properties
        [Theory]
        [MemberData(nameof(TestPropertyData))]
        public static void TestProperty(bool actual, bool expected)
        {
            Assert.Equal(expected, actual);
        }

        //Verify IsGenericMethodDefinition Property
        [Fact]
        public static void TestIsGenericMethodDefinition()
        {
            MethodInfo mi = GetMethod(typeof(classA), "GenericMethod");
            Type[] types = new Type[1];
            types[0] = typeof(string);

            MethodInfo miConstructed = mi.MakeGenericMethod(types);

            MethodInfo midef = miConstructed.GetGenericMethodDefinition();
            Assert.True(midef.IsGenericMethodDefinition);
        }

        //Verify IsConstructor Property
        [Fact]
        public static void TestIsConstructor()
        {
            ConstructorInfo ci = null;

            TypeInfo ti = typeof(classA).GetTypeInfo();
            IEnumerator<ConstructorInfo> allctors = ti.DeclaredConstructors.GetEnumerator();

            while (allctors.MoveNext())
            {
                if (allctors.Current != null)
                {
                    //found method
                    ci = allctors.Current;
                    break;
                }
            }

            Assert.True(ci.IsConstructor);
        }

        //Verify CallingConventions Property
        [Fact]
        public static void TestCallingConventions()
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1");
            CallingConventions myc = mi.CallingConvention;


            Assert.NotNull(myc);
        }

        //Verify Attributes Property
        [Fact]
        public static void TestAttributes()
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1");
            MethodAttributes myattr = mi.Attributes;

            Assert.NotNull(myattr);
        }

        // Helper Method to get MethodInfo
        public static MethodInfo GetMethod(Type t, string method)
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


        //Methods for Reflection Metadata

        public void DummyMethod1()
        {
        }

        public int DummyMethod2()
        {
            return 111;
        }

        public string DummyMethod3()
        {
            return "DummyMethod3";
        }

        public virtual String[] DummyMethod4()
        {
            System.String[] strarray = new System.String[2];
            return strarray;
        }


        public virtual bool DummyMethod5()
        {
            return true;
        }
    }

    //For Reflection metadata
    public class classA
    {
        public classA()
        {
        }
        public static int method1()
        {
            return 100;
        }
        public static void method2() { }

        public static void method3() { }

        public static void GenericMethod<T>(T toDisplay)
        {
        }
    }

    public abstract class classB
    {
        public abstract void abstractMethod();

        public virtual void virtualMethod() { }
    }


    public class classC : classB
    {
        public sealed override void virtualMethod()
        {
        }

        public override void abstractMethod()
        {
        }
    }
}
