// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 0414
#pragma warning disable 0067
#pragma warning disable 3026

namespace System.Reflection.Tests
{
    public class TypeInfoDeclaredPropertiesTests
    {
        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty1()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass), "Pubprop1");
        }

        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty2()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass), "SubPubprop1");
        }

        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty3()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass), "Pubprop2");
        }


        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty4()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass), "Pubprop3");
        }

        // Verify Declared Properties for Derived class 
        [Fact]
        public static void TestSubClassProperty1()
        {
            VerifyProperty(typeof(TypeInfoPropertiesSubClass), "Pubprop1");
        }

        // Verify Declared Properties for Derived class 
        [Fact]
        public static void TestSubClassProperty2()
        {
            VerifyProperty(typeof(TypeInfoPropertiesSubClass), "Pubprop2");
        }

        // Verify Declared Properties for Derived class 
        [Fact]
        public static void TestSubClassProperty3()
        {
            VerifyProperty(typeof(TypeInfoPropertiesSubClass), "Pubprop3");
        }

        //private helper methods
        private static void VerifyProperty(Type t, String name)
        {
            //Fix to initialize Reflection
            String str = typeof(Object).Name;

            TypeInfo ti = t.GetTypeInfo();

            Assert.True(ti.DeclaredProperties.Any(item => item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)));
        }
    } //end class

    //Metadata for Reflection
    public class TypeInfoPropertiesBaseClass
    {
        public string Pubprop1 { get { return ""; } set { } }
        public string SubPubprop1 { get { return ""; } set { } }
        public virtual string Pubprop2 { get { return ""; } set { } }
        public static string Pubprop3 { get { return ""; } set { } }
    }

    public class TypeInfoPropertiesSubClass : TypeInfoPropertiesBaseClass
    {
        public new string Pubprop1 { get { return ""; } set { } }
        public new virtual string Pubprop2 { get { return ""; } set { } }
        public new static string Pubprop3 { get { return ""; } set { } }
    }
}
