// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoPropertyTests
    {
        [Theory]
        [InlineData("MyPropAA", true)]
        [InlineData("MyPropBB", true)]
        [InlineData("MyPropCC", false)]
        public static void CanRead(String propName, bool expected)
        {
            PropertyInfo pi = typeof(SampleProperty).GetTypeInfo().GetProperty(propName);
            Assert.Equal(expected, pi.CanRead);
        }

        [Theory]
        [InlineData("MyPropAA", true)]
        [InlineData("MyPropBB", false)]
        [InlineData("MyPropCC", true)]
        public static void CanWrite(String propName, bool expected)
        {
            PropertyInfo pi = typeof(SampleProperty).GetTypeInfo().GetProperty(propName);
            Assert.Equal(expected, pi.CanWrite);

        }

        [Theory]
        [InlineData("DerivedPropertyProperty", typeof(DerivedProperty), typeof(DerivedProperty))]
        [InlineData("BasePropertyProperty", typeof(BaseProperty), typeof(BaseProperty))]
        public static void DeclaringType(String propName, Type type, Type expectedType)
        {
            PropertyInfo pi = type.GetTypeInfo().GetProperty(propName);
            Assert.Equal(expectedType, pi.DeclaringType);
        }

        [Theory]
        [InlineData("DerivedPropertyProperty", typeof(DerivedProperty), typeof(int))]
        [InlineData("BasePropertyProperty", typeof(BaseProperty), typeof(int))]
        [InlineData("MyPropAA", typeof(SampleProperty), typeof(short))]
        public static void PropertyType(String propName, Type type, Type expectedType)
        {
            PropertyInfo pi = type.GetTypeInfo().GetProperty(propName);
            Assert.Equal(expectedType, pi.PropertyType);
        }

        [Theory]
        [InlineData("MyPropAA")]
        [InlineData("MyPropBB")]
        [InlineData("MyPropCC")]
        public static void Name(String propName)
        {
            PropertyInfo pi = typeof(SampleProperty).GetTypeInfo().GetProperty(propName);
            Assert.Equal(propName, pi.Name);
        }

        //Verify IsSpecialName for PropertyInfo
        [Theory]
        [InlineData("MyPropAA")]
        [InlineData("MyPropBB")]
        [InlineData("MyPropCC")]
        public static void IsSpecialName(String propName)
        {
            PropertyInfo pi = typeof(SampleProperty).GetTypeInfo().GetProperty(propName);
            Assert.False(pi.IsSpecialName, "Failed: PropertyInfo IsSpecialName returned True for property: " + propName);
        }

        //Verify Attributes for Property
        [Theory]
        [InlineData("Description")]
        [InlineData("DerivedPropertyProperty")]
        public static void Attributes(String propName)
        {
            PropertyInfo pi = typeof(DerivedProperty).GetTypeInfo().GetProperty(propName);

            PropertyAttributes pa = pi.Attributes;
            Assert.Equal((object)pa, (object)PropertyAttributes.None);
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
