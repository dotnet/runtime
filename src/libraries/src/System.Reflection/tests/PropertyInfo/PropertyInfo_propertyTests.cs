// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoPropertyTests
    {
        //Verify CanRead for read write PropertyInfo
        [Fact]
        public static void TestCanRead1()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);


            Assert.NotNull(pi);
            Assert.True(pi.CanRead, "Failed!  CanRead Failed for read write property. Expected True , returned False");
        }

        //Verify CanRead for readonly PropertyInfo
        [Fact]
        public static void TestCanRead2()
        {
            string propName = "MyPropBB";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.True(pi.CanRead, "Failed!  CanRead Failed for read only property. Expected True , returned False");
        }


        //Verify CanRead for writeonly PropertyInfo
        [Fact]
        public static void TestCanRead3()
        {
            string propName = "MyPropCC";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.False(pi.CanRead, "Failed!  CanRead Failed for write only property. Expected False , returned True");
        }

        //Verify CanWrite for read write PropertyInfo
        [Fact]
        public static void TestCanWrite1()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);


            Assert.NotNull(pi);
            Assert.True(pi.CanWrite, "Failed!  CanWrite Failed for read write property. Expected True , returned False");
        }

        //Verify CanWrite for readonly PropertyInfo
        [Fact]
        public static void TestCanWrite2()
        {
            string propName = "MyPropBB";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.False(pi.CanWrite, "Failed!  CanWrite Failed for read only property. Expected False , returned True");
        }


        //Verify CanWrite for writeonly PropertyInfo
        [Fact]
        public static void TestCanWrite3()
        {
            string propName = "MyPropCC";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.True(pi.CanWrite, "Failed!  CanWrite Failed for write only property. Expected True , returned False");
        }

        //Verify DeclaringType for PropertyInfo
        [Fact]
        public static void TestDeclaringType1()
        {
            string propName = "DerivedProeprtyProperty";
            PropertyInfo pi = GetProperty(typeof(DerivedProeprty), propName);

            Assert.NotNull(pi);
            Assert.NotNull(pi.DeclaringType.Name);
            Assert.Equal("DerivedProeprty", pi.DeclaringType.Name);
        }

        //Verify DeclaringType for PropertyInfo
        [Fact]
        public static void TestDeclaringType2()
        {
            string propName = "BasePropertyProperty";
            PropertyInfo pi = GetProperty(typeof(BaseProperty), propName);

            Assert.NotNull(pi);
            Assert.NotNull(pi.DeclaringType.Name);
            Assert.Equal("BaseProperty", pi.DeclaringType.Name);
        }


        //Verify PropertyType for PropertyInfo
        [Fact]
        public static void TestPropertyType1()
        {
            string propName = "DerivedProeprtyProperty";
            PropertyInfo pi = GetProperty(typeof(DerivedProeprty), propName);

            Assert.NotNull(pi);
            Assert.NotNull(pi.PropertyType);
            Assert.Equal("Int32", pi.PropertyType.Name);
        }

        //Verify PropertyType for PropertyInfo
        [Fact]
        public static void TestPropertyType2()
        {
            string propName = "BasePropertyProperty";
            PropertyInfo pi = GetProperty(typeof(BaseProperty), propName);

            Assert.NotNull(pi);
            Assert.NotNull(pi.PropertyType);
            Assert.Equal("Int32", pi.PropertyType.Name);
        }

        //Verify PropertyType for PropertyInfo
        [Fact]
        public static void TestPropertyType3()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);


            Assert.NotNull(pi);
            Assert.NotNull(pi.PropertyType);
            Assert.Equal("Int16", pi.PropertyType.Name);
        }


        //Verify Name for PropertyInfo
        [Fact]
        public static void TestName1()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);


            Assert.NotNull(pi);
            Assert.Equal(propName, pi.Name);
        }

        //Verify Name for PropertyInfo
        [Fact]
        public static void TestName2()
        {
            string propName = "MyPropBB";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.Equal(propName, pi.Name);
        }


        //Verify Name for PropertyInfo
        [Fact]
        public static void TestName3()
        {
            string propName = "MyPropCC";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.Equal(propName, pi.Name);
        }


        //Verify IsSpecialName for PropertyInfo
        [Fact]
        public static void TestIsSpecialName1()
        {
            string propName = "MyPropCC";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.False(pi.IsSpecialName, "Failed: PropertyInfo IsSpecialName returned True for property: " + propName);
        }

        //Verify IsSpecialName for PropertyInfo
        [Fact]
        public static void TestIsSpecialName2()
        {
            string propName = "MyPropBB";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.False(pi.IsSpecialName, "Failed: PropertyInfo IsSpecialName returned True for property: " + propName);
        }


        //Verify IsSpecialName for PropertyInfo
        [Fact]
        public static void TestIsSpecialName3()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = GetProperty(typeof(SampleProperty), propName);

            Assert.NotNull(pi);
            Assert.False(pi.IsSpecialName, "Failed: PropertyInfo IsSpecialName returned True for property: " + propName);
        }


        //Verify Attributes for Property
        [Fact]
        public static void TestAttributes1()
        {
            string propName = "Description";
            PropertyInfo pi = GetProperty(typeof(DerivedProeprty), propName);

            Assert.NotNull(pi);

            PropertyAttributes pa = pi.Attributes;
            Assert.Equal((object)pa, (object)PropertyAttributes.None);
        }


        //Verify Attributes for Property
        [Fact]
        public static void TestAttributes2()
        {
            string propName = "DerivedProeprtyProperty";
            PropertyInfo pi = GetProperty(typeof(DerivedProeprty), propName);

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

    public class DerivedProeprty : BaseProperty
    {
        private int _derivedprop = 100;
        private string _description;

        public int DerivedProeprtyProperty
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
