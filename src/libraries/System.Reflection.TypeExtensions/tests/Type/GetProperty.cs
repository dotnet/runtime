// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
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
            TryGetProperty("System.Reflection.Tests.GenericClassWithVarArgMethod`1[System.String]", "publicField", typeof(string), "System.String publicField");
        }

        [Fact]
        public void Test2()
        {
            TryGetProperty("System.Reflection.Tests.Cat`1[System.Int32]", "StuffConsumed", "System.Object[] StuffConsumed");
        }

        [Fact]
        public void Test3()
        {
            TryGetProperty("System.Reflection.Tests.GenericClassWithVarArgMethod`1[System.Int32]", "publicField", typeof(int), "Int32 publicField");
        }

        [Fact]
        public void Test4()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetProperty("System.Reflection.Tests.GenericClassWithVarArgMethod`1", "publicField", allBindingFlags, "T publicField");
        }

        [Fact]
        public void Test5()
        {
            TryGetProperty("System.Reflection.Tests.GenericClassWithVarArgMethod`1[System.Int32]", "publicField", "Int32 publicField");
        }

        [Fact]
        public void Test6()
        {
            TryGetProperty("System.Reflection.Tests.GenericClassWithVarArgMethod`1[System.Int32]", "publicField", typeof(int), new Type[] { }, "Int32 publicField");
        }

        [Fact]
        public void Test7()
        {
            TryGetProperty("System.Reflection.Tests.ClassWithVarArgMethod", "publicField", "Int32 publicField");
        }
    }
}
