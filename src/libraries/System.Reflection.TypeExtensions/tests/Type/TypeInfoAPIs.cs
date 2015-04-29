// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable 169, 649, 164

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    // Compare the results of RuntimeType and TypeInfo APIs results
    public class TypeInfoAPIsTest
    {
        private static BindingFlags s_declaredOnly = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        private static Type s_runtimeType = typeof(PublicClass);
        private static TypeInfo s_typeInfo = s_runtimeType.GetTypeInfo();

        [Fact]
        public void Test1()
        {
            VerifyResults("Name", s_runtimeType.Name, s_typeInfo.Name);
        }

        [Fact]
        public void Test2()
        {
            VerifyResults("FullName", s_runtimeType.FullName, s_typeInfo.FullName);
        }
        [Fact]
        public void Test3()
        {
            VerifyResults("GetDeclaredMethods", s_runtimeType.GetMethod("ProtectedMethod", s_declaredOnly), s_typeInfo.GetDeclaredMethods("ProtectedMethod").First());
        }

        [Fact]
        public void Test4()
        {
            VerifyResults("GetDeclaredNestedType", s_runtimeType.GetNestedType("PublicNestedType", s_declaredOnly), s_typeInfo.GetDeclaredNestedType("PublicNestedType"));
        }

        [Fact]
        public void Test5()
        {
            VerifyResults("GetDeclaredProperty", s_runtimeType.GetProperty("PrivateProperty", s_declaredOnly), s_typeInfo.GetDeclaredProperty("PrivateProperty"));
        }

        [Fact]
        public void Test6()
        {
            VerifyResults("DeclaredFields", s_runtimeType.GetFields(s_declaredOnly), s_typeInfo.DeclaredFields);
        }

        [Fact]
        public void Test7()
        {
            VerifyResults("DeclaredMethods", s_runtimeType.GetMethods(s_declaredOnly), s_typeInfo.DeclaredMethods);
        }

        [Fact]
        public void Test8()
        {
            VerifyResults("DeclaredNestedTypes", s_runtimeType.GetNestedTypes(s_declaredOnly), s_typeInfo.DeclaredNestedTypes);
        }

        [Fact]
        public void Test9()
        {
            VerifyResults("DeclaredProperties", s_runtimeType.GetProperties(s_declaredOnly), s_typeInfo.DeclaredProperties);
        }

        [Fact]
        public void Test10()
        {
            VerifyResults("DeclaredEvents", s_runtimeType.GetEvents(s_declaredOnly), s_typeInfo.DeclaredEvents);
        }

        [Fact]
        public void Test11()
        {
            VerifyResults("DeclaredConstructors", s_runtimeType.GetConstructors(s_declaredOnly), s_typeInfo.DeclaredConstructors);
        }

        [Fact]
        public void Test12()
        {
            VerifyResults("GetEvents", s_runtimeType.GetEvents(), s_typeInfo.AsType().GetEvents());
        }

        [Fact]
        public void Test13()
        {
            VerifyResults("GetFields", s_runtimeType.GetFields(), s_typeInfo.AsType().GetFields());
        }

        [Fact]
        public void Test14()
        {
            VerifyResults("GetMethods", s_runtimeType.GetMethods(), s_typeInfo.AsType().GetMethods());
        }
        [Fact]
        public void Test15()
        {
            BindingFlags all = BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            VerifyResults("GetNestedTypes", s_runtimeType.GetNestedTypes(all), s_typeInfo.AsType().GetNestedTypes(all));
        }

        [Fact]
        public void Test16()
        {
            VerifyResults("GetProperties", s_runtimeType.GetProperties(), s_typeInfo.AsType().GetProperties());
        }

        [Fact]
        public void Test17()
        {
            VerifyResults("GetType", s_runtimeType.GetType(), s_typeInfo.GetType());
        }

        [Fact]
        public void Test18()
        {
            VerifyResults("IsAssignableFrom", true, s_runtimeType.IsAssignableFrom(s_typeInfo.AsType()));
        }

        [Fact]
        public void Test19()
        {
            VerifyResults("Equals", true, s_runtimeType.Equals(s_typeInfo));
        }

        [Fact]
        public void Test20()
        {
            VerifyResults("IsClass", s_runtimeType.GetTypeInfo().IsClass, s_typeInfo.IsClass);
        }

        [Fact]
        public void Test21()
        {
            VerifyResults("IsPublic", s_runtimeType.GetTypeInfo().IsPublic, s_typeInfo.IsPublic);
        }

        [Fact]
        public void Test22()
        {
            VerifyResults("IsGenericType", s_runtimeType.GetTypeInfo().IsGenericType, s_typeInfo.IsGenericType);
        }

        [Fact]
        public void Test23()
        {
            VerifyResults("IsImport", s_runtimeType.GetTypeInfo().IsImport, s_typeInfo.IsImport);
        }

        [Fact]
        public void Test24()
        {
            VerifyResults("IsEnum", s_runtimeType.GetTypeInfo().IsEnum, s_typeInfo.IsEnum);
        }

        [Fact]
        public void Test25()
        {
            VerifyResults("IsGenericTypeDefinition ", s_runtimeType.GetTypeInfo().IsGenericTypeDefinition, s_typeInfo.IsGenericTypeDefinition);
        }


        private void VerifyResults(String testName, Object[] expected, IEnumerable<Object> actual)
        {
            // if (expected.Length != expected.Length)
            foreach (Object exObj in expected)
            {
                Boolean found = false;
                foreach (Object acObj in actual)
                {
                    if (!exObj.ToString().Equals(acObj.ToString()))
                        continue;
                    found = true;
                    break;
                }
                Assert.True(found);
            }
        }

        private void VerifyResults(String testName, Object expected, Object actual)
        {
            Assert.Equal(expected, actual);
        }
    }

    public class PublicClass
    {
        #region fields
        public int PublicField;
        protected int ProtectedField;
        private int _privateField;
        internal int InternalField;

        public static int PublicStaticField;
        protected static int ProtectedStaticField;
        private static int s_privateStaticField;
        internal static int InternalStaticField;
        #endregion

        #region constructors
        public PublicClass() { }
        protected PublicClass(int i) { }
        private PublicClass(int i, int j) { }
        internal PublicClass(int i, int j, int k) { }
        #endregion

        #region methods
        public void PublicMethod() { }
        protected void ProtectedMethod() { }
        private void PrivateMethod() { }
        internal void InternalMethod() { }

        public static void PublicStaticMethod() { }
        protected static void ProtectedStaticMethod() { }
        private static void PrivateStaticMethod() { }
        internal static void InternalStaticMethod() { }
        #endregion

        #region nested types
        public class PublicNestedType { }
        protected class ProtectedNestedType { }
        private class PrivateNestedType { }
        internal class InternalNestedType { }
        #endregion

        #region properties
        public int PublicProperty { get { return default(int); } set { } }
        protected int ProtectedProperty { get { return default(int); } set { } }
        private int PrivateProperty { get { return default(int); } set { } }
        internal int InternalProperty { get { return default(int); } set { } }
        #endregion
    }
}