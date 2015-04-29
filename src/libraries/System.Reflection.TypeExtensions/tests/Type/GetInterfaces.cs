// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
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
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClass`1[System.String]", new string[0]);
        }

        [Fact]
        public void Test2()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1[System.String]", new string[0]);
        }

        [Fact]
        public void Test3()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.String]", new string[] { "System.Reflection.Compatibility.UnitTests.TypeTests.IConsume" });
        }

        [Fact]
        public void Test4()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1", new string[] { "System.Reflection.Compatibility.UnitTests.TypeTests.IConsume" });
        }

        [Fact]
        public void Test5()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.PackOfCarnivores`1[[System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]]]", new string[0]);
        }

        [Fact]
        public void Test6()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterfaceInherits`2[System.Int32,System.String]", new string[] { "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1[System.Int32]", "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface2`2[System.String,System.Int32]" });
        }

        [Fact]
        public void Test7()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassUsingNestedInterfaces`2[System.Int32,System.String]", new string[] { "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterfaceInherits`2[System.Int32,System.String]", "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1[System.Int32]", "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface2`2[System.String,System.Int32]" });
        }

        [Fact]
        public void Test8()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithInterface`1", new string[] { "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1[T]" });
        }

        [Fact]
        public void Test9()
        {
            TryGetInterfaces("System.Reflection.Compatibility.UnitTests.TypeTests.NonGenericClassWithGenericInterface", new string[] { "System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1[System.Int32]" });
        }
    }
}