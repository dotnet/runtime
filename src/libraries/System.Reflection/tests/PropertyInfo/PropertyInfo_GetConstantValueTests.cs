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
    public class PropertyInfoGetConstantValueTests
    {
        //Verify GetConstantValue for int property
        [Fact]
        public static void TestGetConstantValue1()
        {
            string propName = "intProperty";
            PropertyInfo pi = GetProperty(typeof(SamplePropertyInfo), propName);

            Assert.NotNull(pi);

            Assert.Throws<InvalidOperationException>(() =>
            {
                int value = (int)pi.GetConstantValue();
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                int value = (int)pi.GetRawConstantValue();
            });
        }



        //Verify GetConstantValue for string property
        [Fact]
        public static void TestGetConstantValue2()
        {
            string propName = "strProperty";
            PropertyInfo pi = GetProperty(typeof(SamplePropertyInfo), propName);

            Assert.NotNull(pi);

            Assert.Throws<InvalidOperationException>(() =>
            {
                string value = (string)pi.GetConstantValue();
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                string value = (string)pi.GetRawConstantValue();
            });

        }


        //Verify GetConstantValue for double  property
        [Fact]
        public static void TestGetConstantValue3()
        {
            string propName = "doubleProperty";
            PropertyInfo pi = GetProperty(typeof(SamplePropertyInfo), propName);

            Assert.NotNull(pi);

            Assert.Throws<InvalidOperationException>(() =>
            {
                double value = (double)pi.GetConstantValue();
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                double value = (double)pi.GetRawConstantValue();
            });
        }


        //Verify GetConstantValue for float  property
        [Fact]
        public static void TestGetConstantValue4()
        {
            string propName = "floatProperty";
            PropertyInfo pi = GetProperty(typeof(SamplePropertyInfo), propName);

            Assert.NotNull(pi);

            Assert.Throws<InvalidOperationException>(() =>
            {
                float value = (float)pi.GetConstantValue();
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                float value = (float)pi.GetRawConstantValue();
            });
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

    public enum MyEnum { FIRST = 1, SECOND = 2, THIRD = 3, FOURTH = 4 };
    public struct SamplePropertyInfo
    {
        private const int _intProperty = 100;
        private const string _strProperty = "hello";
        private const double _doubleProperty = 22.314;
        private const float _floatProperty = 99.99F;
        private const MyEnum _enumProperty = MyEnum.FIRST;

        public int intProperty
        {
            get { return _intProperty; }
        }

        public string strProperty
        {
            get { return _strProperty; }
        }

        public double doubleProperty
        {
            get { return _doubleProperty; }
        }

        public float floatProperty
        {
            get { return _floatProperty; }
        }

        public MyEnum enumProperty
        {
            get { return _enumProperty; }
        }
    }
}
