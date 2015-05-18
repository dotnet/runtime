// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MethodInfoPropertyTests
    {
        //Verify ReturnType for Method GetMethod
        [Fact]
        public static void TestReturnType1()
        {
            VerifyReturnType("GetMethod", "MethodInfo");
        }

        //Verify ReturnType for Method DummyMethod1
        [Fact]
        public static void TestReturnType2()
        {
            VerifyReturnType("DummyMethod1", "void");
        }

        //Verify ReturnType for Method DummyMethod2
        [Fact]
        public static void TestReturnType3()
        {
            VerifyReturnType("DummyMethod2", "Int32");
        }

        //Verify ReturnType for Method DummyMethod3
        [Fact]
        public static void TestReturnType4()
        {
            VerifyReturnType("DummyMethod3", "string");
        }

        //Verify ReturnType for Method DummyMethod4
        [Fact]
        public static void TestReturnType5()
        {
            VerifyReturnType("DummyMethod4", "String[]");
        }


        //Verify ReturnType for Method DummyMethod5
        [Fact]
        public static void TestReturnType6()
        {
            VerifyReturnType("DummyMethod5", "Boolean");
        }


        //Verify ReturnParameter for Method DummyMethod5
        [Fact]
        public static void TestReturnParam1()
        {
            VerifyReturnParameter("DummyMethod5", "Boolean");
        }


        //Verify ReturnParameter for Method DummyMethod4
        [Fact]
        public static void TestReturnParam2()
        {
            VerifyReturnParameter("DummyMethod4", "System.String[]");
        }


        //Verify ReturnParameter for Method DummyMethod3
        [Fact]
        public static void TestReturnParam3()
        {
            VerifyReturnParameter("DummyMethod3", "System.String");
        }


        //Verify ReturnParameter for Method DummyMethod2
        [Fact]
        public static void TestReturnParam4()
        {
            VerifyReturnParameter("DummyMethod2", "Int32");
        }


        //Verify ReturnParameter for Method DummyMethod1
        [Fact]
        public static void TestReturnParam5()
        {
            VerifyReturnParameter("DummyMethod1", "Void");
        }


        //Verify ReturnParameter forclassA method method1
        [Fact]
        public static void TestReturnParam6()
        {
            MethodInfo mi = GetMethod(typeof(classA), "method1");
            ParameterInfo returnParam = mi.ReturnParameter;

            Assert.Equal(returnParam.Position, -1);
        }

        //Verify IsVirtual Property
        [Fact]
        public static void TestIsVirtual1()
        {
            MethodInfo mi = GetMethod("DummyMethod5");

            Assert.True(mi.IsVirtual);
        }


        //Verify IsVirtual Property
        [Fact]
        public static void TestIsVirtual2()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsVirtual);
        }


        //Verify IsPublic Property
        [Fact]
        public static void TestIsPublic()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.True(mi.IsPublic);
        }


        //Verify IsPrivate Property
        [Fact]
        public static void TestIsPrivate()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsPrivate);
        }


        //Verify IsStatic Property
        [Fact]
        public static void TestIsStatic1()
        {
            MethodInfo mi = GetMethod(typeof(classA), "method1");

            Assert.True(mi.IsStatic);
        }

        //Verify IsStatic Property
        [Fact]
        public static void TestIsStatic2()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsStatic);
        }


        //Verify IsGenericMethod Property
        [Fact]
        public static void TestIsGeneric1()
        {
            MethodInfo mi = GetMethod(typeof(classA), "GenericMethod");

            Assert.True(mi.IsGenericMethod);
        }

        //Verify IsGenericMethod Property
        [Fact]
        public static void TestIsGeneric2()
        {
            MethodInfo mi = GetMethod(typeof(classA), "method1");

            Assert.False(mi.IsGenericMethod);
        }


        //Verify IsGenericMethodDefinition Property
        [Fact]
        public static void TestIsGenericMethodDefinition1()
        {
            MethodInfo mi = GetMethod(typeof(classA), "method1");

            Assert.False(mi.IsGenericMethodDefinition);
        }


        //Verify IsGenericMethodDefinition Property
        [Fact]
        public static void TestIsGenericMethodDefinition2()
        {
            MethodInfo mi = GetMethod(typeof(classA), "GenericMethod");
            Type[] types = new Type[1];
            types[0] = typeof(string);

            MethodInfo miConstructed = mi.MakeGenericMethod(types);

            MethodInfo midef = miConstructed.GetGenericMethodDefinition();
            Assert.True(midef.IsGenericMethodDefinition);
        }


        //Verify IsFinal Property
        [Fact]
        public static void TestIsFinal1()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsFinal);
        }

        //Verify IsFinal Property
        [Fact]
        public static void TestIsFinal2()
        {
            MethodInfo mi = GetMethod(typeof(classC), "virtualMethod");

            Assert.True(mi.IsFinal);
        }


        //Verify IsConstructor Property
        [Fact]
        public static void TestIsConstructor1()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsConstructor);
        }


        //Verify IsConstructor Property
        [Fact]
        public static void TestIsConstructor2()
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

        //Verify IsAbstract Property
        [Fact]
        public static void TestIsAbstract1()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsAbstract);
        }

        //Verify IsAbstract Property
        [Fact]
        public static void TestIsAbstract2()
        {
            MethodInfo mi = GetMethod(typeof(classB), "abstractMethod");

            Assert.True(mi.IsAbstract);
        }


        //Verify IsAssembly Property
        [Fact]
        public static void TestIsAssembly()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsAssembly);
        }


        //Verify IsFamily Property
        [Fact]
        public static void TestIsFamily()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsFamily);
        }


        //Verify IsFamilyAndAssembly Property
        [Fact]
        public static void TestIsFamilyAndAssembly()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsFamilyAndAssembly);
        }


        //Verify IsFamilyOrAssembly Property
        [Fact]
        public static void TestIsFamilyOrAssembly()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsFamilyOrAssembly);
        }


        //Verify ContainsGenericParameters Property
        [Fact]
        public static void TestContainsGenericParameters1()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.ContainsGenericParameters);
        }


        //Verify ContainsGenericParameters Property
        [Fact]
        public static void TestContainsGenericParameters2()
        {
            MethodInfo mi = GetMethod(typeof(classA), "GenericMethod");

            Assert.True(mi.ContainsGenericParameters);
        }


        //Verify CallingConventions Property
        [Fact]
        public static void TestCallingConventions()
        {
            MethodInfo mi = GetMethod("DummyMethod1");
            CallingConventions myc = mi.CallingConvention;


            Assert.NotNull(myc);
        }


        //Verify IsSpecialName Property
        [Fact]
        public static void TestIsSpecialName()
        {
            MethodInfo mi = GetMethod("DummyMethod1");

            Assert.False(mi.IsSpecialName);
        }


        //Verify IsHidebySig Property
        [Fact]
        public static void TestIsHidebySig()
        {
            MethodInfo mi = GetMethod(typeof(classC), "abstractMethod");

            Assert.True(mi.IsHideBySig);
        }


        //Verify Attributes Property
        [Fact]
        public static void TestAttributes()
        {
            MethodInfo mi = GetMethod("DummyMethod1");
            MethodAttributes myattr = mi.Attributes;

            Assert.NotNull(myattr);
        }


        //Helper Method to Verify ReturnType
        public static void VerifyReturnType(string methodName, string returnType)
        {
            MethodInfo mi = GetMethod(methodName);

            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));
            Assert.True(mi.ReturnType.Name.Equals(returnType, StringComparison.CurrentCultureIgnoreCase));
        }


        //Helper Method to Verify ReturnParameter
        public static void VerifyReturnParameter(string methodName, string returnParam)
        {
            MethodInfo mi = GetMethod(methodName);

            Assert.NotNull(mi);
            Assert.True(mi.Name.Equals(methodName, StringComparison.CurrentCultureIgnoreCase));

            Assert.Equal(mi.ReturnType, mi.ReturnParameter.ParameterType);

            Assert.Null(mi.ReturnParameter.Name);
        }


        // Helper Method to get MethodInfo
        public static MethodInfo GetMethod(string method)
        {
            return GetMethod(typeof(MethodInfoPropertyTests), method);
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
