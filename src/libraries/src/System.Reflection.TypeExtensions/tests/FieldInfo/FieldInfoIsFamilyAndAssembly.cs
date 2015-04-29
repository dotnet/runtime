// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    // System.Reflection.FieldInfo.IsFamilyAndAssembly
    public class FieldInfoIsFamilyAndAssembly
    {
        // Positive Test 1:the class is public and the field is public
        [Fact]
        public void PosTest1()
        {
            Type tpA = typeof(TestPublicClass);
            FieldInfo fieldinfo = tpA.GetField("field1", BindingFlags.Public | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 2:the class is public and the field is protected
        [Fact]
        public void PosTest2()
        {
            Type tpA = typeof(TestPublicClass);
            FieldInfo fieldinfo = tpA.GetField("field2", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 3:the class is public and the field is protected internal
        [Fact]
        public void PosTest3()
        {
            Type tpA = typeof(TestPublicClass);
            FieldInfo fieldinfo = tpA.GetField("field3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 4:the class is public and the field is private
        [Fact]
        public void PosTest4()
        {
            Type tpA = typeof(TestPublicClass);
            FieldInfo fieldinfo = tpA.GetField("_field4", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 5:the class is public and the field is internal
        [Fact]
        public void PosTest5()
        {
            Type tpA = typeof(TestPublicClass);
            FieldInfo fieldinfo = tpA.GetField("field5", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 6:the class is internal and the field is public
        [Fact]
        public void PosTest6()
        {
            Type tpA = typeof(TestinternalClass);
            FieldInfo fieldinfo = tpA.GetField("field1", BindingFlags.Public | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 7:the class is internal and the field is protected
        [Fact]
        public void PosTest7()
        {
            Type tpA = typeof(TestinternalClass);
            FieldInfo fieldinfo = tpA.GetField("field2", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 8:the class is internal and the field is protected internal
        [Fact]
        public void PosTest8()
        {
            Type tpA = typeof(TestinternalClass);
            FieldInfo fieldinfo = tpA.GetField("field3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 9:the class is internal and the field is private
        [Fact]
        public void PosTest9()
        {
            Type tpA = typeof(TestinternalClass);
            FieldInfo fieldinfo = tpA.GetField("_field4", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        // Positive Test 10:the class is internal and the field is internal
        [Fact]
        public void PosTest10()
        {
            Type tpA = typeof(TestinternalClass);
            FieldInfo fieldinfo = tpA.GetField("field5", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.False(fieldinfo.IsFamilyAndAssembly, "IsFamilyAndAssembly was true for FieldInfo " + fieldinfo);
        }

        public class TestPublicClass
        {
            public string field1 = System.IO.Path.GetRandomFileName() + "TestString";
            protected string field2 = System.IO.Path.GetRandomFileName() + "TestString";
            protected internal string field3 = System.IO.Path.GetRandomFileName() + "TestString";
            private string _field4 = System.IO.Path.GetRandomFileName() + "TestString";
            internal int field5 = new Random().Next(int.MinValue, int.MaxValue);
        }
        internal class TestinternalClass
        {
            public string field1 = System.IO.Path.GetRandomFileName() + "TestString";
            protected string field2 = System.IO.Path.GetRandomFileName() + "TestString";
            protected internal string field3 = System.IO.Path.GetRandomFileName() + "TestString";
            private string _field4 = System.IO.Path.GetRandomFileName() + "TestString";
            internal int field5 = new Random().Next(int.MinValue, int.MaxValue);
        }
    }
}
