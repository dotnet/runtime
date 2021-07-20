// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class TestClassWithValueTypedMember : ITestClass
    {
        public ValueTypedMember MyValueTypedProperty { get; set; }

        public ValueTypedMember MyValueTypedField;

        public RefTypedMember MyRefTypedProperty { get; set; }

        public RefTypedMember MyRefTypedField;

        public void Initialize()
        {
            MyValueTypedProperty = new ValueTypedMember("ValueTypedProperty");
            MyValueTypedField = new ValueTypedMember("ValueTypedField");
            MyRefTypedProperty = new RefTypedMember("RefTypedProperty");
            MyRefTypedField = new RefTypedMember("RefTypedField");
        }

        public void Verify()
        {
            Assert.Equal("ValueTypedProperty", MyValueTypedProperty.Value);
            Assert.Equal("ValueTypedField", MyValueTypedField.Value);
            Assert.Equal("RefTypedProperty", MyRefTypedProperty.Value);
            Assert.Equal("RefTypedField", MyRefTypedField.Value);
        }
    }

    public class TestClassWithNullableValueTypedMember : ITestClass
    {
        public ValueTypedMember? MyValueTypedProperty { get; set; }

        public ValueTypedMember? MyValueTypedField;

        public RefTypedMember MyRefTypedProperty { get; set; }

        public RefTypedMember MyRefTypedField;

        public void Initialize()
        {
            MyValueTypedProperty = new ValueTypedMember("ValueTypedProperty");
            MyValueTypedField = new ValueTypedMember("ValueTypedField");
            MyRefTypedProperty = new RefTypedMember("RefTypedProperty");
            MyRefTypedField = new RefTypedMember("RefTypedField");
        }

        public void Verify()
        {
            Assert.Equal("ValueTypedProperty", MyValueTypedProperty.Value.Value);
            Assert.Equal("ValueTypedField", MyValueTypedField.Value.Value);
            Assert.Equal("RefTypedProperty", MyRefTypedProperty.Value);
            Assert.Equal("RefTypedField", MyRefTypedField.Value);
        }
    }

    public interface IMemberInterface
    {
        string Value { get; }
    }

    public struct ValueTypedMember : IMemberInterface
    {
        public string Value { get; }

        public ValueTypedMember(string value)
        {
            Value = value;
        }
    }

    public struct OtherVTMember : IMemberInterface
    {
        public string Value { get; }

        public OtherVTMember(string value)
        {
            Value = value;
        }
    }

    public class RefTypedMember : IMemberInterface
    {
        public string Value { get; }

        public RefTypedMember(string value)
        {
            Value = value;
        }
    }

    public class OtherRTMember : IMemberInterface
    {
        public string Value { get; }

        public OtherRTMember(string value)
        {
            Value = value;
        }
    }
}
