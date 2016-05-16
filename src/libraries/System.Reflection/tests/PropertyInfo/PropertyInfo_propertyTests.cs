// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoPropertyTests
    {
        [Theory]
        [InlineData("MyPropAA", true)]
        [InlineData("MyPropBB", true)]
        [InlineData("MyPropCC", false)]
        public static void TestCanRead(String propName, String message, bool boolean)
        {
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.Equal(boolean, pi.CanRead);
        }

        [Theory]
        [InlineData("MyPropAA", true)]
        [InlineData("MyPropBB", false)]
        [InlineData("MyPropCC", true)]
        public static void TestCanWrite(String propName, bool boolean)
        {
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.Equal(boolean, pi.CanWrite);

        }

        [Theory]
        [InlineData("DerivedPropertyProperty", typeof(DerivedProperty), "DerivedProperty")]
        [InlineData("BasePropertyProperty", typeof(BaseProperty), "BaseProperty")]
        public static void TestDeclaringType(String propName, Type type, String typeName)
        {
            PropertyInfo pi = GetProperty(type, propName);

            Assert.NotNull(pi);
            Assert.NotNull(pi.DeclaringType.Name);
            Assert.Equal(typeName, pi.DeclaringType.Name);
        }

        [Theory]
        [InlineData("DerivedPropertyProperty", typeof(DerivedProperty), "Int32")]
        [InlineData("BasePropertyProperty", typeof(BaseProperty), "Int32")]
        [InlineData("MyPropAA", typeof(SampleProperty), "Int16")]
        public static void TestPropertyType(String propName, Type type, String typeName)
        {
            PropertyInfo pi = GetProperty(type, propName);

            Assert.NotNull(pi);
            Assert.NotNull(pi.PropertyType);
            Assert.Equal(typeName, pi.PropertyType.Name);
        }

        [Theory]
        [InlineData("MyPropAA")]
        [InlineData("MyPropBB")]
        [InlineData("MyPropCC")]
        public static void TestName(String propName)
        {
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.Equal(propName, pi.Name);
        }

        //Verify IsSpecialName for PropertyInfo
        [Theory]
        [InlineData("MyPropAA")]
        [InlineData("MyPropBB")]
        [InlineData("MyPropCC")]
        public static void TestIsSpecialName(String propName)
        {
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.False(pi.IsSpecialName, "Failed: PropertyInfo IsSpecialName returned True for property: " + propName);
        }

        //Verify Attributes for Property
        [Theory]
        [InlineData("Description")]
        [InlineData("DerivedPropertyProperty")]
        public static void TestAttributes(String propName)
        {
            PropertyInfo pi = GetProperty(typeof(DerivedProperty), propName);

            Assert.NotNull(pi);

            PropertyAttributes pa = pi.Attributes;
            Assert.Equal((object)pa, (object)PropertyAttributes.None);
        }


        //Gets PropertyInfo object from a Type
        public static PropertyInfo GetProperty(Type t, string property)
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
    }

    //Reflection Metadata  
    public class SampleProperty
    {
        public short m_PropAA = -2;
        public int m_PropBB = -2;
        public long m_PropCC = -2;
        public int m_PropDD = -2;

        /// declare/define all the properties here
        // MyPropAA - ReadWrite public property
        public short MyPropAA
        {
            get { return m_PropAA; }
            set { m_PropAA = value; }
        }

        // MyPropBB - ReadOnly public property
        public int MyPropBB
        {
            get { return m_PropBB; }
        }

        // MyPropCC - WriteOnly public property
        public int MyPropCC
        {
            set { m_PropCC = value; }
        }

        //indexer Property
        public string[] mystrings = { "abc", "def", "ghi", "jkl" };

        public string this[int Index]
        {
            get
            {
                return mystrings[Index];
            }
            set
            {
                mystrings[Index] = value;
            }
        }
    }


    public class BaseProperty
    {
        private int _baseprop = 10;

        public int BasePropertyProperty
        {
            get { return _baseprop; }
            set { _baseprop = value; }
        }
    }

    public class DerivedProperty : BaseProperty
    {
        private int _derivedprop = 100;
        private string _description;

        public int DerivedPropertyProperty
        {
            get { return _derivedprop; }
            set { _derivedprop = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
    }
}
