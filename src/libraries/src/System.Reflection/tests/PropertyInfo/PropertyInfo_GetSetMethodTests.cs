// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoGetSetMethodTests
    {
        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod1()
        {
            VerifyGetSetMethod(typeof(ReferenceTypeHelper), "PropertyGetterSetter", true, true);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod2()
        {
            VerifyGetSetMethod(typeof(ReferenceTypeHelper), "PropertyGetter", true, false);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod3()
        {
            VerifyGetSetMethod(typeof(ReferenceTypeHelper), "PropertySetter", false, true);
        }


        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod4()
        {
            VerifyGetSetMethod(typeof(ReferenceTypeHelper), "Item", true, true);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod5()
        {
            VerifyGetSetMethod(typeof(ValueTypeHelper), "PropertyGetterSetter", true, true);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod6()
        {
            VerifyGetSetMethod(typeof(ValueTypeHelper), "PropertyGetter", true, false);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod7()
        {
            VerifyGetSetMethod(typeof(ValueTypeHelper), "PropertySetter", false, true);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod8()
        {
            VerifyGetSetMethod(typeof(ValueTypeHelper), "Item", true, false);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod9()
        {
            VerifyGetSetMethod(typeof(InterfaceHelper), "PropertyGetterSetter", true, true);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod10()
        {
            VerifyGetSetMethod(typeof(InterfaceHelper), "PropertyGetter", true, false);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod11()
        {
            VerifyGetSetMethod(typeof(InterfaceHelper), "PropertySetter", false, true);
        }

        //Verify GetMethod , SetMethod properties for PropertyInfo
        [Fact]
        public static void TestGetSetMethod12()
        {
            VerifyGetSetMethod(typeof(InterfaceHelper), "Item", false, true);
        }


        //Gets PropertyInfo object from a Type
        public static PropertyInfo getProperty(Type t, string property)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<PropertyInfo> allproperties = ti.DeclaredProperties.GetEnumerator();
            PropertyInfo pi = null;

            while (allproperties.MoveNext())
            {
                if (allproperties.Current.Name.Equals(property))
                {
                    //found property
                    pi = allproperties.Current;
                    break;
                }
            }
            return pi;
        }

        public static void VerifyGetSetMethod(Type type, String propertyName, Boolean getter, Boolean setter)
        {
            PropertyInfo pi = getProperty(type, propertyName);

            Assert.NotNull(pi);


            MethodInfo mi = pi.GetMethod;
            if (getter && mi == null)
            {
                Assert.False(true);
            }

            mi = pi.SetMethod;
            if (setter && mi == null)
            {
                Assert.False(true, String.Format("Error! Type: {0}, Property: {1} should contain Setter", type, propertyName));
            }
        }
    }

    //Reflection Metadata  


    public class ReferenceTypeHelper
    {
        public int PropertyGetterSetter { get { return 1; } set { } }
        public String PropertyGetter { get { return "Test"; } }
        public Char PropertySetter { set { } }
        public int this[int index] { get { return 2; } set { } }

        public int PropertyPrivateGetterSetter { private get { return 1; } set { } }
        public int PropertyProtectedGetterSetter { protected get { return 1; } set { } }
        public int PropertyInternalGetterSetter { internal get { return 1; } set { } }

        public int PropertyGetterPrivateSetter { get { return 1; } private set { } }
        public int PropertyGetterProtectedSetter { get { return 1; } protected set { } }
        public int PropertyGetterInternalSetter { get { return 1; } internal set { } }
    }

    public struct ValueTypeHelper
    {
        public int PropertyGetterSetter { get { return 1; } set { } }
        public String PropertyGetter { get { return "Test"; } }
        public Char PropertySetter { set { } }
        public String this[int index] { get { return "name"; } }
    }

    public interface InterfaceHelper
    {
        int PropertyGetterSetter { get; set; }
        String PropertyGetter { get; }
        Char PropertySetter { set; }
        Char this[int index] { set; }
    }
}
