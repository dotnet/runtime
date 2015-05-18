// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredGenericTypeArgumentsTests
    {
        //Interfaces
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments1()
        {
            VerifyGenericTypeArguments(typeof(Test_I), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments2()
        {
            VerifyGenericTypeArguments(typeof(Test_IG<>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments3()
        {
            VerifyGenericTypeArguments(typeof(Test_IG<Int32>), new String[] { "Int32" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments4()
        {
            VerifyGenericTypeArguments(typeof(Test_IG2<,>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments5()
        {
            VerifyGenericTypeArguments(typeof(Test_IG2<Int32, String>), new String[] { "Int32", "String" }, null);
        }

        // For Structs
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments6()
        {
            VerifyGenericTypeArguments(typeof(Test_S), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments7()
        {
            VerifyGenericTypeArguments(typeof(Test_SG<>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments8()
        {
            VerifyGenericTypeArguments(typeof(Test_SG<Int32>), new String[] { "Int32" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments9()
        {
            VerifyGenericTypeArguments(typeof(Test_SG2<,>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments10()
        {
            VerifyGenericTypeArguments(typeof(Test_SG2<Int32, String>), new String[] { "Int32", "String" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments11()
        {
            VerifyGenericTypeArguments(typeof(Test_SI), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments12()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG<>), new String[] { }, new String[] { "TS" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments13()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG<Int32>), new String[] { "Int32" }, new String[] { "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments14()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG2<,>), new String[] { }, new String[] { "TS", "VS" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments15()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG2<Int32, String>), new String[] { "Int32", "String" }, new String[] { "Int32", "String" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments16()
        {
            VerifyGenericTypeArguments(typeof(Test_SI_Int), new String[] { }, new String[] { "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments17()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG_Int<>), new String[] { }, new String[] { "TS", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments18()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG_Int<String>), new String[] { "String" }, new String[] { "String", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments19()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG_Int_Int), new String[] { }, new String[] { "Int32", "Int32" });
        }

        //For classes

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments20()
        {
            VerifyGenericTypeArguments(typeof(Test_C), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments21()
        {
            VerifyGenericTypeArguments(typeof(Test_CG<>), new String[] { }, null);
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments22()
        {
            VerifyGenericTypeArguments(typeof(Test_CG<Int32>), new String[] { "Int32" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments23()
        {
            VerifyGenericTypeArguments(typeof(Test_CG2<,>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments24()
        {
            VerifyGenericTypeArguments(typeof(Test_CG2<Int32, String>), new String[] { "Int32", "String" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments25()
        {
            VerifyGenericTypeArguments(typeof(Test_CI), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments26()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG<Int32>), new String[] { "Int32" }, new String[] { "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments27()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG2<,>), new String[] { }, new String[] { "T", "V" });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments28()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG2<Int32, String>), new String[] { "Int32", "String" }, new String[] { "Int32", "String" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments29()
        {
            VerifyGenericTypeArguments(typeof(Test_CI_Int), new String[] { }, new String[] { "Int32" });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments30()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG_Int<>), new String[] { }, new String[] { "T", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments31()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG_Int<String>), new String[] { "String" }, new String[] { "String", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments32()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG_Int_Int), new String[] { }, new String[] { "Int32", "Int32" });
        }

        //private helper methods

        private static void VerifyGenericTypeArguments(Type type, String[] expectedGTA, String[] expectedBaseGTA)
        {
            //Fix to initialize Reflection
            String str = typeof(Object).Name;

            TypeInfo typeInfo = type.GetTypeInfo();

            Type[] retGenericTypeArguments = typeInfo.GenericTypeArguments;

            Assert.Equal(expectedGTA.Length, retGenericTypeArguments.Length);


            for (int i = 0; i < retGenericTypeArguments.Length; i++)
            {
                Assert.Equal(expectedGTA[i], retGenericTypeArguments[i].Name);
            }


            Type baseType = typeInfo.BaseType;
            if (baseType == null)
                return;

            if (baseType == typeof(ValueType) || baseType == typeof(Object))
            {
                Type[] interfaces = getInterfaces(typeInfo);
                if (interfaces.Length == 0)
                    return;
                baseType = interfaces[0];
            }


            TypeInfo typeInfoBase = baseType.GetTypeInfo();
            retGenericTypeArguments = typeInfoBase.GenericTypeArguments;

            Assert.Equal(expectedBaseGTA.Length, retGenericTypeArguments.Length);


            for (int i = 0; i < retGenericTypeArguments.Length; i++)
            {
                Assert.Equal(expectedBaseGTA[i], retGenericTypeArguments[i].Name);
            }
        }


        private static Type[] getInterfaces(TypeInfo ti)
        {
            List<Type> list = new List<Type>();

            IEnumerator<Type> allinterfaces = ti.ImplementedInterfaces.GetEnumerator();

            while (allinterfaces.MoveNext())
            {
                list.Add(allinterfaces.Current);
            }
            return list.ToArray();
        }
    }

    //Metadata for Reflection
    public interface Test_I { }
    public interface Test_IG<TI> { }
    public interface Test_IG2<TI, VI> { }

    public struct Test_S { }
    public struct Test_SG<TS> { }
    public struct Test_SG2<TS, VS> { }

    public struct Test_SI : Test_I { }
    public struct Test_SIG<TS> : Test_IG<TS> { }
    public struct Test_SIG2<TS, VS> : Test_IG2<TS, VS> { }

    public struct Test_SI_Int : Test_IG<Int32> { }
    public struct Test_SIG_Int<TS> : Test_IG2<TS, Int32> { }
    public struct Test_SIG_Int_Int : Test_IG2<Int32, Int32> { }

    public class Test_C { }
    public class Test_CG<T> { }
    public class Test_CG2<T, V> { }

    public class Test_CI : Test_I { }
    public class Test_CIG<T> : Test_IG<T> { }
    public class Test_CIG2<T, V> : Test_IG2<T, V> { }

    public class Test_CI_Int : Test_IG<Int32> { }
    public class Test_CIG_Int<T> : Test_CG2<T, Int32> { }
    public class Test_CIG_Int_Int : Test_CG2<Int32, Int32> { }
}
