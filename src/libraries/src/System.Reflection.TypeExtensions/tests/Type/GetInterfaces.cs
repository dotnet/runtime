// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class GetInterfacesTest
    {
        public static void TryGetInterfaces(string AssemblyQualifiedNameOfTypeToGet, string[] interfacesExpected)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            Type[] interfacesReturned = typeToCheck.GetInterfaces();
            Assert.Equal(interfacesExpected.Length, interfacesReturned.Length);
            int foundIndex;
            Array.Sort(interfacesExpected);
            for (int i = 0; i < interfacesReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(interfacesExpected, interfacesReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected interface " + interfacesReturned[i].ToString() + " was returned");
            }
        }
        public static string ArrayToCommaList(Type[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0].ToString();
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i].ToString();
                }
            }
            return returnString;
        }

        public static string ArrayToCommaList(string[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0].ToString();
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i].ToString();
                }
            }
            return returnString;
        }

        [Fact]
        public void Test1()
        {
            TryGetInterfaces("System.Reflection.Tests.GenericClass`1[System.String]", new string[0]);
        }

        [Fact]
        public void Test2()
        {
            TryGetInterfaces("System.Reflection.Tests.IGenericInterface`1[System.String]", new string[0]);
        }

        [Fact]
        public void Test3()
        {
            TryGetInterfaces("System.Reflection.Tests.Cat`1[System.String]", new string[] { "System.Reflection.Tests.IConsume" });
        }

        [Fact]
        public void Test4()
        {
            TryGetInterfaces("System.Reflection.Tests.Cat`1", new string[] { "System.Reflection.Tests.IConsume" });
        }

        [Fact]
        public void Test5()
        {
            TryGetInterfaces("System.Reflection.Tests.PackOfCarnivores`1[[System.Reflection.Tests.Cat`1[System.Int32]]]", new string[0]);
        }

        [Fact]
        public void Test6()
        {
            TryGetInterfaces("System.Reflection.Tests.IGenericInterfaceInherits`2[System.Int32,System.String]", new string[] { "System.Reflection.Tests.IGenericInterface`1[System.Int32]", "System.Reflection.Tests.IGenericInterface2`2[System.String,System.Int32]" });
        }

        [Fact]
        public void Test7()
        {
            TryGetInterfaces("System.Reflection.Tests.GenericClassUsingNestedInterfaces`2[System.Int32,System.String]", new string[] { "System.Reflection.Tests.IGenericInterfaceInherits`2[System.Int32,System.String]", "System.Reflection.Tests.IGenericInterface`1[System.Int32]", "System.Reflection.Tests.IGenericInterface2`2[System.String,System.Int32]" });
        }

        [Fact]
        public void Test8()
        {
            TryGetInterfaces("System.Reflection.Tests.GenericClassWithInterface`1", new string[] { "System.Reflection.Tests.IGenericInterface`1[T]" });
        }

        [Fact]
        public void Test9()
        {
            TryGetInterfaces("System.Reflection.Tests.NonGenericClassWithGenericInterface", new string[] { "System.Reflection.Tests.IGenericInterface`1[System.Int32]" });
        }
    }
}
