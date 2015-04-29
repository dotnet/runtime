// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetPropertyTests
    {
        public static void TryGetProperty(string AssemblyQualifiedNameOfTypeToGet, string propertyToFind, string expectedPropertyName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            PropertyInfo propertyReturned = typeToCheck.GetProperty(propertyToFind);
            Assert.NotNull(propertyReturned);
            Assert.Equal(expectedPropertyName, propertyReturned.ToString());
        }

        public static void TryGetProperty(string AssemblyQualifiedNameOfTypeToGet, string propertyToFind, BindingFlags bindingAttr, string expectedPropertyName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            PropertyInfo propertyReturned = typeToCheck.GetProperty(propertyToFind, bindingAttr);
            Assert.NotNull(propertyReturned);
            Assert.Equal(expectedPropertyName, propertyReturned.ToString());
        }

        public static void TryGetProperty(string AssemblyQualifiedNameOfTypeToGet, string propertyToFind, Type returnType, string expectedPropertyName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            PropertyInfo propertyReturned = typeToCheck.GetProperty(propertyToFind, returnType);
            Assert.NotNull(propertyReturned);
            Assert.Equal(expectedPropertyName, propertyReturned.ToString());
        }

        public static void TryGetProperty(string AssemblyQualifiedNameOfTypeToGet, string propertyToFind, Type[] types, string expectedPropertyName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            PropertyInfo propertyReturned = typeToCheck.GetProperty(propertyToFind, typeof(object), types);
            Assert.NotNull(propertyReturned);
            Assert.Equal(expectedPropertyName, propertyReturned.ToString());
        }

        public static void TryGetProperty(string AssemblyQualifiedNameOfTypeToGet, string propertyToFind, Type returnType, Type[] types, string expectedPropertyName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            PropertyInfo propertyReturned = typeToCheck.GetProperty(propertyToFind, returnType, types);
            Assert.NotNull(propertyReturned);
            Assert.Equal(expectedPropertyName, propertyReturned.ToString());
        }

        [Fact]
        public void Test1()
        {
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1[System.String]", "publicField", typeof(string), "System.String publicField");
        }

        [Fact]
        public void Test2()
        {
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", "StuffConsumed", "System.Object[] StuffConsumed");
        }

        [Fact]
        public void Test3()
        {
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1[System.Int32]", "publicField", typeof(int), "Int32 publicField");
        }

        [Fact]
        public void Test4()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1", "publicField", allBindingFlags, "T publicField");
        }

        [Fact]
        public void Test5()
        {
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1[System.Int32]", "publicField", "Int32 publicField");
        }

        [Fact]
        public void Test6()
        {
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1[System.Int32]", "publicField", typeof(int), new Type[] { }, "Int32 publicField");
        }

        [Fact]
        public void Test7()
        {
            TryGetProperty("System.Reflection.Compatibility.UnitTests.TypeTests.ClassWithVarArgMethod", "publicField", "Int32 publicField");
        }
    }
}
