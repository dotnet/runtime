// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    // System.Reflection.FieldInfo.IsInitOnly
    public class FieldInfoIsInitOnly
    {
        // Positive Test 1:the class is public and the field is public
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field1", BindingFlags.Public | BindingFlags.Instance);
            Assert.False(fieldinfo.IsInitOnly);
        }

        // Positive Test 2:the class is public and the field is public readonly
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field2", BindingFlags.Public | BindingFlags.Instance);
            Assert.True(fieldinfo.IsInitOnly);
        }

        // Positive Test 3:the class is public and the field is protected readonly
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(fieldinfo.IsInitOnly);
        }

        // Positive Test 4:the class is public and the field is protected
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("field4", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsInitOnly);
        }

        // Positive Test 5:the class is public and the field is private
        [Fact]
        public void PosTest5()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("_field5", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsInitOnly);
        }

        // Positive Test 6:the class is public and the field is private readonly
        [Fact]
        public void PosTest6()
        {
            Type tpA = typeof(TestClassPublic);
            FieldInfo fieldinfo = tpA.GetField("_field6", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(fieldinfo.IsInitOnly);
        }

        // Positive Test 7:the class is internal and the field is public
        [Fact]
        public void PosTest7()
        {
            Type tpA = typeof(TestClassInternal);
            FieldInfo fieldinfo = tpA.GetField("field1", BindingFlags.Public | BindingFlags.Instance);
            Assert.False(fieldinfo.IsInitOnly);
        }

        // Positive Test 8:the class is internal and the field is protected internal readonly
        [Fact]
        public void PosTest8()
        {
            Type tpA = typeof(TestClassInternal);
            FieldInfo fieldinfo = tpA.GetField("field0", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(fieldinfo.IsInitOnly);
        }

        // Positive Test 9:the class is internal and the field is public readonly
        [Fact]
        public void PosTest9()
        {
            Type tpA = typeof(TestClassInternal);
            FieldInfo fieldinfo = tpA.GetField("field2", BindingFlags.Public | BindingFlags.Instance);
            Assert.True(fieldinfo.IsInitOnly);
        }

        // Positive Test 10:the class is internal and the field is private
        [Fact]
        public void PosTest10()
        {
            Type tpA = typeof(TestClassInternal);
            FieldInfo fieldinfo = tpA.GetField("_field3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsInitOnly);
        }

        // Positive Test 11:the class is internal and the field is private readonly
        [Fact]
        public void PosTest11()
        {
            Type tpA = typeof(TestClassInternal);
            FieldInfo fieldinfo = tpA.GetField("_field4", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(fieldinfo.IsInitOnly);
        }

        #region ForTestObject
        public class TestClassPublic
        {
            public string field1 = System.IO.Path.GetRandomFileName() + "TestString";
            public readonly int field2 = new Random().Next(int.MinValue, int.MaxValue);
            protected readonly int field3 = new Random().Next(int.MinValue, int.MaxValue);
            protected string field4 = System.IO.Path.GetRandomFileName() + "TestString";
            private int _field5 = new Random().Next(int.MinValue, int.MaxValue);
            private readonly string _field6 = System.IO.Path.GetRandomFileName() + "TestString";
        }
        internal class TestClassInternal
        {
            protected internal readonly int field0 = new Random().Next(int.MinValue, int.MaxValue);
            public string field1 = System.IO.Path.GetRandomFileName() + "TestString";
            public readonly int field2 = new Random().Next(int.MinValue, int.MaxValue);
            private string _field3 = System.IO.Path.GetRandomFileName() + "TestString";
            private readonly int _field4 = new Random().Next(int.MinValue, int.MaxValue);
        }
        #endregion
    }
}