// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredGenericTypeParameterTests
    {
        //Interfaces
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters1()
        {
            VerifyGenericTypeParameters(typeof(Test_I1).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters2()
        {
            VerifyGenericTypeParameters(typeof(Test_IG1<>).Project(), new string[] { "TI" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters3()
        {
            VerifyGenericTypeParameters(typeof(Test_IG1<int>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters4()
        {
            VerifyGenericTypeParameters(typeof(Test_IG21<,>).Project(), new string[] { "TI", "VI" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters5()
        {
            VerifyGenericTypeParameters(typeof(Test_IG21<int, string>).Project(), new string[] { }, null);
        }

        // For Structs
        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters6()
        {
            VerifyGenericTypeParameters(typeof(Test_S1).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters7()
        {
            VerifyGenericTypeParameters(typeof(Test_SG1<>).Project(), new string[] { "TS" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters8()
        {
            VerifyGenericTypeParameters(typeof(Test_SG1<int>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters9()
        {
            VerifyGenericTypeParameters(typeof(Test_SG21<,>).Project(), new string[] { "TS", "VS" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters10()
        {
            VerifyGenericTypeParameters(typeof(Test_SG21<int, string>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters11()
        {
            VerifyGenericTypeParameters(typeof(Test_SI1).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters12()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG1<>).Project(), new string[] { "TS" }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters13()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG1<int>).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters14()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG21<,>).Project(), new string[] { "TS", "VS" }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters15()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG21<int, string>).Project(), new string[] { }, new string[] { });
        }



        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters16()
        {
            VerifyGenericTypeParameters(typeof(Test_SI_Int1).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters17()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG_Int1<>).Project(), new string[] { "TS" }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters18()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG_Int1<string>).Project(), new string[] { }, new string[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters19()
        {
            VerifyGenericTypeParameters(typeof(Test_SIG_Int_Int1).Project(), new string[] { }, new string[] { });
        }

        //For classes

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters20()
        {
            VerifyGenericTypeParameters(typeof(Test_C1).Project(), new string[] { }, null);
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters21()
        {
            VerifyGenericTypeParameters(typeof(Test_CG1<>).Project(), new string[] { "T" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters22()
        {
            VerifyGenericTypeParameters(typeof(Test_CG1<int>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters23()
        {
            VerifyGenericTypeParameters(typeof(Test_CG21<,>).Project(), new string[] { "T", "V" }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters24()
        {
            VerifyGenericTypeParameters(typeof(Test_CG21<int, string>).Project(), new string[] { }, null);
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters25()
        {
            VerifyGenericTypeParameters(typeof(Test_CI1).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters26()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG1<>).Project(), new string[] { "T" }, new string[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters27()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG1<int>).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters28()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG21<,>).Project(), new string[] { "T", "V" }, new string[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters29()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG21<int, string>).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters30()
        {
            VerifyGenericTypeParameters(typeof(Test_CI_Int1).Project(), new string[] { }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters31()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG_Int1<>).Project(), new string[] { "T" }, new string[] { });
        }

        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters32()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG_Int1<string>).Project(), new string[] { }, new string[] { });
        }


        // Verify Generic Arguments 
        [Fact]
        public static void TestGenericParameters33()
        {
            VerifyGenericTypeParameters(typeof(Test_CIG_Int_Int1).Project(), new string[] { }, new string[] { });
        }

        //private helper methods

        private static void VerifyGenericTypeParameters(Type type, string[] expectedGTP, string[] expectedBaseGTP)
        {
            //Fix to initialize Reflection
            string str = typeof(object).Project().Name;

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

            if (baseType == typeof(ValueType).Project() || baseType == typeof(object).Project())
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

    public struct Test_SI_Int1 : Test_IG1<int> { }
    public struct Test_SIG_Int1<TS> : Test_IG21<TS, int> { }
    public struct Test_SIG_Int_Int1 : Test_IG21<int, int> { }

    public class Test_C1 { }
    public class Test_CG1<T> { }
    public class Test_CG21<T, V> { }

    public class Test_CI1 : Test_I1 { }
    public class Test_CIG1<T> : Test_IG1<T> { }
    public class Test_CIG21<T, V> : Test_IG21<T, V> { }

    public class Test_CI_Int1 : Test_IG1<int> { }
    public class Test_CIG_Int1<T> : Test_CG21<T, int> { }
    public class Test_CIG_Int_Int1 : Test_CG21<int, int> { }
}
