// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredGenericTypeParameterTests
    {
        //Interfaces
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters1()
        {
            VerifyGenericTypeParameters(typeof(Test_I1), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters2()
        {
            VerifyGenericTypeParameters(typeof(Test_IG1<>), new String[] { "TI" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters3()
        {
            VerifyGenericTypeParameters(typeof(Test_IG1<int>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters4()
        {
            VerifyGenericTypeParameters(typeof(Test_IG21<,>), new String[] { "TI", "VI" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters5()
        {
            VerifyGenericTypeParameters(typeof(Test_IG21<int, String>), new String[] { }, null);
        }

        // For Structs
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters6()
        {
            VerifyGenericTypeParameters(typeof(Test_S1), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters7()
        {
            VerifyGenericTypeParameters(typeof(Test_SG1<>), new String[] { "TS" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters8()
        {
            VerifyGenericTypeParameters(typeof(Test_SG1<int>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters9()
        {
            VerifyGenericTypeParameters(typeof(Test_SG21<,>), new String[] { "TS", "VS" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters10()
        {
            VerifyGenericTypeParameters(typeof(Test_SG21<int, String>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters11()
        {
            VerifyGenericTypeParameters(typeof(Test_SI1), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters12()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG1<>), new String[] { "TS" }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters13()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG1<int>), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters14()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG21<,>), new String[] { "TS", "VS" }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters15()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG21<int, String>), new String[] { }, new String[] { });
        }



        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters16()
        {
            VerifyGenericTypeParameters(typeof(Test_SI_Int1), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters17()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG_Int1<>), new String[] { "TS" }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters18()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG_Int1<String>), new String[] { }, new String[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters19()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG_Int_Int1), new String[] { }, new String[] { });
        }

        //For classes

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters20()
        {
            VerifyGenericTypeParameters(typeof(Test_C1), new String[] { }, null);
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters21()
        {
            VerifyGenericTypeParameters(typeof(Test_CG1<>), new String[] { "T" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters22()
        {
            VerifyGenericTypeParameters(typeof(Test_CG1<int>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters23()
        {
            VerifyGenericTypeParameters(typeof(Test_CG21<,>), new String[] { "T", "V" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters24()
        {
            VerifyGenericTypeParameters(typeof(Test_CG21<int, String>), new String[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters25()
        {
            VerifyGenericTypeParameters(typeof(Test_CI1), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters26()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG1<>), new String[] { "T" }, new String[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters27()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG1<int>), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters28()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG21<,>), new String[] { "T", "V" }, new String[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters29()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG21<int, String>), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters30()
        {
            VerifyGenericTypeParameters(typeof(Test_CI_Int1), new String[] { }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters31()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG_Int1<>), new String[] { "T" }, new String[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters32()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG_Int1<String>), new String[] { }, new String[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters33()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG_Int_Int1), new String[] { }, new String[] { });
        }

        //private helper methods

        private static void VerifyGenericTypeParameters(Type type, String[] expectedGTP, String[] expectedBaseGTP)
        {
            //Fix to initialize Reflection
            String str = typeof(Object).Name;

            TypeInfo typeInfo = type.GetTypeInfo();

            Type[] retGenericTypeParameters = typeInfo.GenericTypeParameters;

            Assert.Equal(expectedGTP.Length, retGenericTypeParameters.Length);


            for (int i = 0; i < retGenericTypeParameters.Length; i++)
            {
                Assert.Equal(expectedGTP[i], retGenericTypeParameters[i].Name);
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
            retGenericTypeParameters = typeInfoBase.GenericTypeParameters;

            Assert.Equal(expectedBaseGTP.Length, retGenericTypeParameters.Length);


            for (int i = 0; i < retGenericTypeParameters.Length; i++)
            {
                Assert.Equal(expectedBaseGTP[i], retGenericTypeParameters[i].Name);
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
    public interface Test_I1 { }
    public interface Test_IG1<TI> { }
    public interface Test_IG21<TI, VI> { }

    public struct Test_S1 { }
    public struct Test_SG1<TS> { }
    public struct Test_SG21<TS, VS> { }

    public struct Test_SI1 : Test_I1 { }
    public struct Test_SIG1<TS> : Test_IG1<TS> { }
    public struct Test_SIG21<TS, VS> : Test_IG21<TS, VS> { }

    public struct Test_SI_Int1 : Test_IG1<Int32> { }
    public struct Test_SIG_Int1<TS> : Test_IG21<TS, Int32> { }
    public struct Test_SIG_Int_Int1 : Test_IG21<Int32, Int32> { }

    public class Test_C1 { }
    public class Test_CG1<T> { }
    public class Test_CG21<T, V> { }

    public class Test_CI1 : Test_I1 { }
    public class Test_CIG1<T> : Test_IG1<T> { }
    public class Test_CIG21<T, V> : Test_IG21<T, V> { }

    public class Test_CI_Int1 : Test_IG1<Int32> { }
    public class Test_CIG_Int1<T> : Test_CG21<T, Int32> { }
    public class Test_CIG_Int_Int1 : Test_CG21<Int32, Int32> { }
}
