// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoGetConstantValueTests
    {
        [Fact]
        public static void GetConstantValue_int_ThrowsInvalidOperationException()
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty("intProperty");

            Assert.Throws<InvalidOperationException>(() => (int)pi.GetConstantValue());

            Assert.Throws<InvalidOperationException>(() => (int)pi.GetRawConstantValue());
        }

        [Fact]
        public static void GetConstantValue_string_ThrowsInvalidOperationException()
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty("strProperty");

            Assert.Throws<InvalidOperationException>(() => (string)pi.GetConstantValue());

            Assert.Throws<InvalidOperationException>(() => (string)pi.GetRawConstantValue());
        }

        [Fact]
        public static void GetConstantValue_double_ThrowsInvalidOperationException()
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty("doubleProperty");

            Assert.Throws<InvalidOperationException>(() => (double)pi.GetConstantValue());

            Assert.Throws<InvalidOperationException>(() => (double)pi.GetRawConstantValue());
        }

        [Fact]
        public static void GetConstantValue_float_ThrowsInvalidOperationException()
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty("floatProperty");

            Assert.Throws<InvalidOperationException>(() => (float)pi.GetConstantValue());

            Assert.Throws<InvalidOperationException>(() => (float)pi.GetRawConstantValue());
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
