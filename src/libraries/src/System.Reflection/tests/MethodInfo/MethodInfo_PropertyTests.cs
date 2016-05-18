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

        //Verify ReturnType for Methods
        [Theory]
        [InlineData("GetMethod", "MethodInfo")]
        [InlineData("DummyMethod1", "void")]
        [InlineData("DummyMethod2", "Int32")]
        [InlineData("DummyMethod3", "string")]
        [InlineData("DummyMethod4", "String[]")]
        [InlineData("DummyMethod5", "Boolean")]

        public static void TestReturnType(string methodName, string returnType)
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoPropertyTests), methodName);

            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase), "methodName");
            Assert.True(mi.ReturnType.Name.Equals(returnType, StringComparison.CurrentCultureIgnoreCase), "returnType");
        }


        //Verify ReturnParameter for Methods
        [Theory]
        [InlineData("DummyMethod5")]
        [InlineData("DummyMethod4")]
        [InlineData("DummyMethod3")]
        [InlineData("DummyMethod2")]
        [InlineData("DummyMethod1")]

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

        //Verify DummyMethod1 properties
        [Fact]
        public static void TestProperty()
        {
            
            MethodInfo dummyMethodInfo1 = GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod1");

            Assert.False(dummyMethodInfo1.IsVirtual, "IsVirtual");
            Assert.True(dummyMethodInfo1.IsPublic, "IsPublic");
            Assert.False(dummyMethodInfo1.IsPrivate, "IsPrivate");
            Assert.False(dummyMethodInfo1.IsStatic, "IsStatic");
            Assert.False(dummyMethodInfo1.IsFinal, "IsFinal");
            Assert.False(dummyMethodInfo1.IsConstructor, "IsConstructor");
            Assert.False(dummyMethodInfo1.IsAbstract, "IsAbstract");
            Assert.False(dummyMethodInfo1.IsAssembly, "IsAssembly");
            Assert.False(dummyMethodInfo1.IsFamily, "IsFamily");
            Assert.False(dummyMethodInfo1.IsFamilyAndAssembly, "IsFamilyAndAssembly");
            Assert.False(dummyMethodInfo1.IsFamilyOrAssembly, "IsFamilyOrAssembly");
            Assert.False(dummyMethodInfo1.ContainsGenericParameters, "ContainsGenericParameters");
            Assert.False(dummyMethodInfo1.IsSpecialName, "IsSpecialName");
        }

        public static void TestProperty2()
        {
            MethodInfo dummyMethodInfo5 = GetMethod(typeof(MethodInfoPropertyTests), "DummyMethod5");
            MethodInfo methodInfo1 = GetMethod(typeof(classA), "method1");
            MethodInfo genericMethodInfo = GetMethod(typeof(classA), "GenericMethod");
            MethodInfo abstractMethodInfoB = GetMethod(typeof(classB), "abstractMethod");
            MethodInfo virtualMethodInfo = GetMethod(typeof(classC), "virtualMethod");
            MethodInfo abstractMethodInfoC = GetMethod(typeof(classC), "abstractMethod");

            //Verify method1 properties
            Assert.True(methodInfo1.IsStatic, "IsStatic");
            Assert.False(methodInfo1.IsGenericMethod, "IsGenericMethod");
            Assert.False(methodInfo1.IsGenericMethodDefinition, "IsGenericMethodDefinition");

            //Verify DummyMethod5 properties
            Assert.True(dummyMethodInfo5.IsVirtual, "IsVirtual");

            //Verify genericMethod properties
            Assert.True(genericMethodInfo.IsGenericMethodDefinition, "IsGenericMethodDefinition");
            Assert.True(genericMethodInfo.ContainsGenericParameters, "ContainsGenericParameters");

            //Verify abstractMethod Properties for classB and classC
            Assert.True(abstractMethodInfoB.IsAbstract, "IsAbstract");
            Assert.True(abstractMethodInfoC.IsHideBySig, "IsHideBySig");

            //Verify virtualMethod properties
            Assert.True(virtualMethodInfo.IsFinal, "IsFinal");
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
