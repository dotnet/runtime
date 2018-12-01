// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredGenericTypeArgumentsTests
    {
        //Interfaces
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments1()
        {
            VerifyGenericTypeArguments(typeof(Test_I).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments2()
        {
            VerifyGenericTypeArguments(typeof(Test_IG<>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments3()
        {
            VerifyGenericTypeArguments(typeof(Test_IG<int>).Project(), new string[] { "Int32" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments4()
        {
            VerifyGenericTypeArguments(typeof(Test_IG2<,>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments5()
        {
            VerifyGenericTypeArguments(typeof(Test_IG2<int, string>).Project(), new string[] { "Int32", "String" }, null);
        }

        // For Structs
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments6()
        {
            VerifyGenericTypeArguments(typeof(Test_S).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments7()
        {
            VerifyGenericTypeArguments(typeof(Test_SG<>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments8()
        {
            VerifyGenericTypeArguments(typeof(Test_SG<int>).Project(), new string[] { "Int32" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments9()
        {
            VerifyGenericTypeArguments(typeof(Test_SG2<,>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments10()
        {
            VerifyGenericTypeArguments(typeof(Test_SG2<int, string>).Project(), new string[] { "Int32", "String" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments11()
        {
            VerifyGenericTypeArguments(typeof(Test_SI).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments12()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG<>).Project(), new string[] { }, new string[] { "TS" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments13()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG<Int32>).Project(), new string[] { "Int32" }, new string[] { "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments14()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG2<,>).Project(), new string[] { }, new string[] { "TS", "VS" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments15()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG2<Int32, string>).Project(), new string[] { "Int32", "String" }, new string[] { "Int32", "String" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments16()
        {
            VerifyGenericTypeArguments(typeof(Test_SI_Int).Project(), new string[] { }, new string[] { "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments17()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG_Int<>).Project(), new string[] { }, new string[] { "TS", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments18()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG_Int<string>).Project(), new string[] { "String" }, new string[] { "String", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments19()
        {
            VerifyGenericTypeArguments(typeof(Test_SIG_Int_Int).Project(), new string[] { }, new string[] { "Int32", "Int32" });
        }

        //For classes

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments20()
        {
            VerifyGenericTypeArguments(typeof(Test_C).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments21()
        {
            VerifyGenericTypeArguments(typeof(Test_CG<>).Project(), new string[] { }, null);
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments22()
        {
            VerifyGenericTypeArguments(typeof(Test_CG<int>).Project(), new string[] { "Int32" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments23()
        {
            VerifyGenericTypeArguments(typeof(Test_CG2<,>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments24()
        {
            VerifyGenericTypeArguments(typeof(Test_CG2<int, string>).Project(), new string[] { "Int32", "String" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments25()
        {
            VerifyGenericTypeArguments(typeof(Test_CI).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments26()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG<int>).Project(), new string[] { "Int32" }, new string[] { "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments27()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG2<,>).Project(), new string[] { }, new string[] { "T", "V" });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments28()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG2<int, string>).Project(), new string[] { "Int32", "String" }, new string[] { "Int32", "String" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments29()
        {
            VerifyGenericTypeArguments(typeof(Test_CI_Int).Project(), new string[] { }, new string[] { "Int32" });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments30()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG_Int<>).Project(), new string[] { }, new string[] { "T", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments31()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG_Int<string>).Project(), new string[] { "String" }, new string[] { "String", "Int32" });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericArguments32()
        {
            VerifyGenericTypeArguments(typeof(Test_CIG_Int_Int).Project(), new string[] { }, new string[] { "Int32", "Int32" });
        }

        //private helper methods

        private static void VerifyGenericTypeArguments(Type type, string[] expectedGTA, string[] expectedBaseGTA)
        {
            //Fix to initialize Reflection
            string str = typeof(object).Project().Name;

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

            if (baseType == typeof(ValueType).Project() || baseType == typeof(object).Project())
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

    public struct Test_SI_Int : Test_IG<int> { }
    public struct Test_SIG_Int<TS> : Test_IG2<TS, int> { }
    public struct Test_SIG_Int_Int : Test_IG2<int, int> { }

    public class Test_C { }
    public class Test_CG<T> { }
    public class Test_CG2<T, V> { }

    public class Test_CI : Test_I { }
    public class Test_CIG<T> : Test_IG<T> { }
    public class Test_CIG2<T, V> : Test_IG2<T, V> { }

    public class Test_CI_Int : Test_IG<int> { }
    public class Test_CIG_Int<T> : Test_CG2<T, int> { }
    public class Test_CIG_Int_Int : Test_CG2<int, int> { }
}
