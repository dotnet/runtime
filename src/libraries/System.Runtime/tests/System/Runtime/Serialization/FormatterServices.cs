// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Xunit;

namespace System.Tests
{
    public static class FormatterServicesTests
    {
    	private static Func<Type, Object> geo = FormatterServices.GetUninitializedObject;

        [Fact]
        public static void NoArgument()
        {
            Assert.Throws<ArgumentNullException>(() => geo(null));
        }

        [Theory]
        [InlineData(typeof(String))]
        [InlineData(typeof(int*))]
    	public static void Throws_ArgumentException(Type type)
    	{
            Assert.Throws<ArgumentException>(() => geo(type));
    	}

    	[Theory]
    	[InlineData(typeof(Array))]
        [InlineData(typeof(ICollection))]
        [InlineData(typeof(Stream))]
        public static void Throws_MemberAccessException(Type type)
    	{
    		Assert.Throws<MemberAccessException>(() => geo(type));
    	}

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(MyClass))]
        public static void Types(Type type)
        {
            Assert.Equal(type, geo(type).GetType());
        }

        [Fact]
        public static void Generic()
        {
            var type = typeof(List<int>);
            Assert.Equal(0, ((List<int>)geo(type)).Count);
            Assert.Equal(type, geo(type).GetType());
        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { typeof(Int32), 0 };
            yield return new object[] { typeof(short), 0 };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void Valid(Type type, Object value)
    	{
    		Assert.Equal(value.ToString(), geo(type).ToString());
    		Assert.Equal(type, geo(type).GetType());
    	}

        [Fact]
        public static void Nullable()
        {
            Assert.Equal(typeof(Int32), geo(typeof(Int32?)).GetType());
        }

        [Fact]
        public static void Mutable()
        {
            // Sanity check the object is actually useable
            MyClass mc = ((MyClass)geo(typeof(MyClass)));
            mc.MyMember = "foo";
            Assert.Equal("foo", mc.MyMember);
        }

        private class MyClass
        {
            public string MyMember { get; set; }
    	}
    }
}
