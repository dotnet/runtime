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
        [Fact]
        public static void NoArgument_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("type", () => FormatterServices.GetUninitializedObject(null));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int*))]
        public static void StringsAndPointers_Throw_ArgumentException(Type type)
        {
            Assert.Throws<ArgumentException>(null /* really should be 'type' */, () => FormatterServices.GetUninitializedObject(type));
        }

        [Fact]
        public static void InstantiatedArrays_Throw_ArgumentException()
        {
            Assert.Throws<ArgumentException>(null /* really should be 'type' */, () => FormatterServices.GetUninitializedObject((new int[] { }).GetType()));
        }

        [Theory]
        [InlineData(typeof(Array))]
        [InlineData(typeof(ICollection))]
        [InlineData(typeof(Stream))]
        public static void InterfacesAndAbstractClasses_Throw_MemberAccessException(Type type)
        {
            Assert.Throws<MemberAccessException>(() => FormatterServices.GetUninitializedObject(type));
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(MyClass))]
        public static void PlainObjects_Success(Type type)
        {
            Assert.Equal(type, FormatterServices.GetUninitializedObject(type).GetType());
        }

        [Fact]
        public static void Generic_Success()
        {
            var type = typeof(List<int>);
            Assert.Equal(0, ((List<int>)FormatterServices.GetUninitializedObject(type)).Count);
            Assert.Equal(type, FormatterServices.GetUninitializedObject(type).GetType());
        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { typeof(int), 0 };
            yield return new object[] { typeof(short), 0 };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void PrimitiveTypes_Success(Type type, object value)
        {
            Assert.Equal(value.ToString(), FormatterServices.GetUninitializedObject(type).ToString());
            Assert.Equal(type, FormatterServices.GetUninitializedObject(type).GetType());
        }

        [Fact]
        public static void Nullable_BecomesNonNullable_Success()
        {
            Assert.Equal(typeof(int), FormatterServices.GetUninitializedObject(typeof(int?)).GetType());
        }

        [Fact]
        public static void Result_Is_Mutable()
        {
            // Sanity check the object is actually useable
            MyClass mc = ((MyClass)FormatterServices.GetUninitializedObject(typeof(MyClass)));
            mc.MyMember = "foo";
            Assert.Equal("foo", mc.MyMember);
        }

        private class MyClass
        {
            public string MyMember { get; set; }
        }
    }
}
