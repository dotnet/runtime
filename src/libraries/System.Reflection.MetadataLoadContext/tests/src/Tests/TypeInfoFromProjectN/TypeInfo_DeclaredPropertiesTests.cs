// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
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
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass).Project(), "Pubprop1");
        }

        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty2()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass).Project(), "SubPubprop1");
        }

        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty3()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass).Project(), "Pubprop2");
        }


        // Verify Declared Properties for Base class 
        [Fact]
        public static void TestBaseClassProperty4()
        {
            VerifyProperty(typeof(TypeInfoPropertiesBaseClass).Project(), "Pubprop3");
        }

        // Verify Declared Properties for Derived class 
        [Fact]
        public static void TestSubClassProperty1()
        {
            VerifyProperty(typeof(TypeInfoPropertiesSubClass).Project(), "Pubprop1");
        }

        // Verify Declared Properties for Derived class 
        [Fact]
        public static void TestSubClassProperty2()
        {
            VerifyProperty(typeof(TypeInfoPropertiesSubClass).Project(), "Pubprop2");
        }

        // Verify Declared Properties for Derived class 
        [Fact]
        public static void TestSubClassProperty3()
        {
            VerifyProperty(typeof(TypeInfoPropertiesSubClass).Project(), "Pubprop3");
        }

        //private helper methods
        private static void VerifyProperty(Type t, string name)
        {
            //Fix to initialize Reflection
            string str = typeof(object).Project().Name;

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
