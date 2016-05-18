// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoMethodTests
    {
        [Fact]
        public static void EqualsMethod_False()
        {
            PropertyInfo pi1 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");
            PropertyInfo pi2 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropBB");

            Assert.False(pi1.Equals(pi2));
        }

        //Verify Equals Method for same properties
        [Fact]
        public static void EqualsMethod_True()
        {
            PropertyInfo pi1 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");
            PropertyInfo pi2 = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");

            Assert.True(pi1.Equals(pi2));
        }

        [Fact]
        public static void TestGetHashCode()
        {
            PropertyInfo pi = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");

            int hcode = pi.GetHashCode();

            Assert.NotEqual(hcode, 0);
        }

        [Fact]
        public static void GetIndexParameters_Item()
        {
            PropertyInfo pi = typeof(SampleMethod).GetTypeInfo().GetProperty("Item");

            ParameterInfo[] allparams = pi.GetIndexParameters();

            Assert.Equal(1, allparams.Length);

            Assert.Equal("Index", allparams[0].Name);
        }

        //Verify GetIndexParameter Method for propertyInfo object
        [Fact]
        public static void GetIndexParameters_MyPropAA()
        {
            PropertyInfo pi = typeof(SampleMethod).GetTypeInfo().GetProperty("MyPropAA");

            ParameterInfo[] allparams = pi.GetIndexParameters();

            Assert.Equal(0, allparams.Length);
        }
    }

    //Reflection Metadata  
    public class SampleMethod
    {
        public double m_PropBB = 1;
        public short m_PropAA = 2;
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

        // MyPropAA - ReadWrite property
        public String MyPropAA
        {
            get { return m_PropAA.ToString(); }
            set { m_PropAA = Int16.Parse(value); }
        }


        public double MyPropBB
        {
            get { return m_PropBB; }
            set { m_PropBB = value; }
        }
    }
}
