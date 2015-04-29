// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    // System.Reflection.FieldInfo.IsLiteral
    public class FieldInfoIsLiteral
    {
        // Positive Test 1:the class is public and the field is public const
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field1", BindingFlags.Public | BindingFlags.Static);
            Assert.True(fieldinfo.IsLiteral);
        }

        // Positive Test 2:the class is public and the field is public static
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field2", BindingFlags.Public | BindingFlags.Static);
            Assert.False(fieldinfo.IsLiteral);
        }

        // Positive Test 3:the class is public and the field is protected internal const
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field4", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.True(fieldinfo.IsLiteral);
        }

        // Positive Test 4:the class is public and the field is protected internal
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsLiteral);
        }

        // Positive Test 5:the class is public and the field is private
        [Fact]
        public void PosTest5()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("_field5", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsLiteral);
        }

        // this class will only ever be used for reflection so
        // build warnings about unused fields do not apply.
#pragma warning disable 0414
        public class TestClassPublic
        {
            public const string field1 = "Test";
            public static string field2 = "TestClassPublicField2";
            protected internal int field3 = new Random().Next(int.MinValue, int.MaxValue);
            protected internal const int field4 = 123;
            private string _field5 = "TestClassPublicPrivateField5";
        }
#pragma warning restore 0414
    }
}


