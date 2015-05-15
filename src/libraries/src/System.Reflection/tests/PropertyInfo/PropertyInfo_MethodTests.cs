// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoMethodTests
    {
        //Verify Equals Method for two different properties
        [Fact]
        public static void TestEqualsMethod1()
        {
            string propName1 = "MyPropAA";
            PropertyInfo pi1 = getProperty(typeof(SampleMethod), propName1);

            string propName2 = "MyPropBB";
            PropertyInfo pi2 = getProperty(typeof(SampleMethod), propName2);


            Assert.NotNull(pi1);

            Assert.NotNull(pi2);

            Assert.False(pi1.Equals(pi2));
        }

        //Verify Equals Method for same properties
        [Fact]
        public static void TestEqualsMethod2()
        {
            string propName1 = "MyPropAA";
            PropertyInfo pi1 = getProperty(typeof(SampleMethod), propName1);

            string propName2 = "MyPropAA";
            PropertyInfo pi2 = getProperty(typeof(SampleMethod), propName2);


            Assert.NotNull(pi1);

            Assert.NotNull(pi2);

            Assert.True(pi1.Equals(pi2));
        }

        //Verify GetHashCode Method for propertyInfo object
        [Fact]
        public static void TestGetHashCode()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = getProperty(typeof(SampleMethod), propName);

            Assert.NotNull(pi);

            int hcode = pi.GetHashCode();

            Assert.NotEqual(hcode, 0);
        }

        //Verify GetIndexParameter Method for propertyInfo object
        [Fact]
        public static void TestGetIndexParameters1()
        {
            string propName = "Item";
            PropertyInfo pi = getProperty(typeof(SampleMethod), propName);

            Assert.NotNull(pi);

            ParameterInfo[] allparams = pi.GetIndexParameters();


            Assert.Equal(1, allparams.Length);

            Assert.Equal("Index", allparams[0].Name);
        }


        //Verify GetIndexParameter Method for propertyInfo object
        [Fact]
        public static void TestGetIndexParameters2()
        {
            string propName = "MyPropAA";
            PropertyInfo pi = getProperty(typeof(SampleMethod), propName);

            Assert.NotNull(pi);

            ParameterInfo[] allparams = pi.GetIndexParameters();

            Assert.Equal(0, allparams.Length);
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
