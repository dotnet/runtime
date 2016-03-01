// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class TypeInfo_GetEnum
    {
        [Fact]
        public static void GetEnumName()
        {
            Assert.Throws<ArgumentException>(() => typeof(MyCustomType).GetTypeInfo().GetEnumName(""));
            Assert.Throws<ArgumentNullException>(() => typeof(MyCustomEnum).GetTypeInfo().GetEnumName(null));
            Assert.Equal("Enum1", typeof(MyCustomEnum).GetTypeInfo().GetEnumName(1));
            Assert.Equal("Enum2", typeof(MyCustomEnum).GetTypeInfo().GetEnumName(2));
            Assert.Equal("Enum10", typeof(MyCustomEnum).GetTypeInfo().GetEnumName(10));
            Assert.Equal("Enum45", typeof(MyCustomEnum).GetTypeInfo().GetEnumName(45));
            Assert.Null(typeof(MyCustomEnum).GetTypeInfo().GetEnumName(8));
        }

        [Fact]
        public static void GetEnumNames()
        {
            Assert.Throws<ArgumentException>(() => typeof(MyCustomType).GetTypeInfo().GetEnumNames());

            string[] values = typeof(MyCustomEnum).GetTypeInfo().GetEnumNames();
            Assert.Equal(5, values.Length);
        }

        [Fact]
        public static void GetEnumUnderlyingType()
        {
            Type t = typeof(MyCustomEnum).GetTypeInfo().GetEnumUnderlyingType();
            Assert.Equal(typeof(int), t);

            t = typeof(MyUint32Enum).GetTypeInfo().GetEnumUnderlyingType();
            Assert.Equal(typeof(uint), t);
            Assert.Throws<ArgumentException>(() => typeof(MyCustomType).GetTypeInfo().GetEnumUnderlyingType());
        }

        [Fact]
        public static void GetEnumValues()
        {
            Array a = typeof(MyCustomEnum).GetTypeInfo().GetEnumNames();
            Assert.Equal(5, a.Length);
            a = typeof(MyUint32Enum).GetTypeInfo().GetEnumNames();
            Assert.Equal(2, a.Length);
            Assert.Throws<ArgumentException>(() => typeof(MyCustomType).GetTypeInfo().GetEnumUnderlyingType());
        }

        [Fact]
        public static void IsEnumDefined()
        {
            bool b;
            b = typeof(MyCustomEnum).GetTypeInfo().IsEnumDefined(10);
            b = typeof(MyCustomEnum).GetTypeInfo().IsEnumDefined(5);

            Assert.Throws<ArgumentException>(() => typeof(MyCustomType).GetTypeInfo().IsEnumDefined(10));
            Assert.Throws<ArgumentNullException>(() => typeof(MyCustomEnum).GetTypeInfo().IsEnumDefined(null));
            Assert.Throws<InvalidOperationException>(() => typeof(MyCustomEnum).GetTypeInfo().IsEnumDefined(new MyCustomType()));
        }

        public enum MyUint32Enum : uint
        {
            A = 1,
            B = 10
        }

        public enum MyCustomEnum
        {
            Enum1 = 1,
            Enum2 = 2,
            Enum10 = 10,
            Enum18 = 18,
            Enum45 = 45
        }

        public class MyCustomType { }
    }
}