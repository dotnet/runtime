// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class TestClassWithStructMember : ITestClass
    {
        public StructMember MyStructProperty { get; set; }

        public StructMember MyStructField;

        public ClassMember MyClassProperty { get; set; }

        public ClassMember MyClassField;

        public void Initialize()
        {
            MyStructProperty = new StructMember("StructProperty");
            MyStructField = new StructMember("StructField");
            MyClassProperty = new ClassMember("ClassProperty");
            MyClassField = new ClassMember("ClassField");
        }

        public void Verify()
        {
            Assert.Equal("StructProperty", MyStructProperty.Value);
            Assert.Equal("StructField", MyStructField.Value);
            Assert.Equal("ClassProperty", MyClassProperty.Value);
            Assert.Equal("ClassField", MyClassField.Value);
        }
    }

    public class TestClassWithNullableStructMember : ITestClass
    {
        public StructMember? MyStructProperty { get; set; }

        public StructMember? MyStructField;

        public ClassMember MyClassProperty { get; set; }

        public ClassMember MyClassField;

        public void Initialize()
        {
            MyStructProperty = new StructMember("StructProperty");
            MyStructField = new StructMember("StructField");
            MyClassProperty = new ClassMember("ClassProperty");
            MyClassField = new ClassMember("ClassField");
        }

        public void Verify()
        {
            Assert.Equal("StructProperty", MyStructProperty.Value.Value);
            Assert.Equal("StructField", MyStructField.Value.Value);
            Assert.Equal("ClassProperty", MyClassProperty.Value);
            Assert.Equal("ClassField", MyClassField.Value);
        }
    }

    public interface IMemberInterface
    {
        string Value { get; }
    }

    public struct StructMember : IMemberInterface
    {
        public string Value { get; }

        public StructMember(string value)
        {
            Value = value;
        }
    }

    public class ClassMember : IMemberInterface
    {
        public string Value { get; }

        public ClassMember(string value)
        {
            Value = value;
        }
    }
}
