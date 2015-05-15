// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

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
            IEnumerator<PropertyInfo> allprops = ti.DeclaredProperties.GetEnumerator();
            bool found = false;

            while (allprops.MoveNext())
            {
                if (allprops.Current.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                Assert.False(true, String.Format("Property {0} not found in Type {1}", name, t.Name));
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
