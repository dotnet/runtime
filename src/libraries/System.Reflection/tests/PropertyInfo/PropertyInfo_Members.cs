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
    public class PropertyInfo_Members
    {

        [Theory]
        [InlineData("intPublicProperty")]
        [InlineData("strPublicProperty")]
        [InlineData("doublePublicProperty")]
        [InlineData("floatPublicProperty")]
        [InlineData("enumPublicProperty")]
        [InlineData("intPrivateProperty")]
        [InlineData("intPrivateSetterProperty")]
        public static void GetRequiredCustomModifiers(string propName)
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(pi);
            Type[] types = pi.GetRequiredCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);

            types = pi.GetOptionalCustomModifiers();
            Assert.NotNull(types);
            Assert.Equal(0, types.Length);
        }

        [Theory]
        [InlineData("intPublicProperty", 1, 1)]
        [InlineData("strPublicProperty", 2, 2)]
        [InlineData("doublePublicProperty", 1, 1)]
        [InlineData("floatPublicProperty", 1, 1)]
        [InlineData("enumPublicProperty", 2, 2)]
        [InlineData("intPrivateProperty", 0, 2)]
        [InlineData("intPrivateSetterProperty", 1, 2)]
        public static void GetAccessors(string propName, int accessorPublicCount, int accessorPublicAndNonPublicCount)
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(accessorPublicCount, pi.GetAccessors().Length);
            Assert.Equal(accessorPublicCount, pi.GetAccessors(false).Length);
            Assert.Equal(accessorPublicAndNonPublicCount, pi.GetAccessors(true).Length);
        }

        [Theory]
        [InlineData("intPublicProperty", true, true, false, false)]
        [InlineData("strPublicProperty", true, true, true, true)]
        [InlineData("doublePublicProperty", true, true, false, false)]
        [InlineData("floatPublicProperty", true, true, false, false)]
        [InlineData("enumPublicProperty", true, true, true, true)]
        [InlineData("intPrivateProperty", false, true, false, true)]
        [InlineData("intPrivateSetterProperty", true, true, false, true)]
        public static void GetGetMethod(string propName, bool publicget, bool nonpublicget, bool publicset, bool nonpublicset)
        {
            PropertyInfo pi = typeof(SamplePropertyInfo).GetTypeInfo().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(publicget ? pi.GetGetMethod().Name.Equals("get_" + propName) : pi.GetGetMethod() == null);
            Assert.True(publicget ? pi.GetGetMethod(false).Name.Equals("get_" + propName) : pi.GetGetMethod() == null);
            Assert.True(publicget ? pi.GetGetMethod(true).Name.Equals("get_" + propName) : pi.GetGetMethod() == null);
            Assert.True(nonpublicget ? pi.GetGetMethod(true).Name.Equals("get_" + propName) : pi.GetGetMethod() == null);
            Assert.True(nonpublicget ? pi.GetGetMethod(true).Name.Equals("get_" + propName) : pi.GetGetMethod(false) == null);

            Assert.True(publicset ? pi.GetSetMethod().Name.Equals("set_" + propName) : pi.GetSetMethod() == null);
            Assert.True(publicset ? pi.GetSetMethod(false).Name.Equals("set_" + propName) : pi.GetSetMethod() == null);
            Assert.True(publicset ? pi.GetSetMethod(true).Name.Equals("set_" + propName) : pi.GetSetMethod() == null);
            Assert.True(nonpublicset ? pi.GetSetMethod(true).Name.Equals("set_" + propName) : pi.GetSetMethod() == null);
            Assert.True(nonpublicset ? pi.GetSetMethod(true).Name.Equals("set_" + propName) : pi.GetSetMethod(false) == null);
        }
        public struct SamplePropertyInfo
        {
            public int intPublicProperty { get; }

            public string strPublicProperty { get; set; }

            public double doublePublicProperty { get; }

            public float floatPublicProperty { get; }

            public MyEnum enumPublicProperty { get; set; }

            private int intPrivateProperty { get; set; }
            public int intPrivateSetterProperty { get; private set; }
        }
    }
}
