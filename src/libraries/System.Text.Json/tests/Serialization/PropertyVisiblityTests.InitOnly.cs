// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class PropertyVisibilityTests
    {
        [Theory]
        [InlineData(typeof(ClassWithInitOnlyProperty))]
        [InlineData(typeof(StructWithInitOnlyProperty))]
        public static void InitOnlyProperties_Work(Type type)
        {
            // Init-only property included by default.
            object obj = JsonSerializer.Deserialize(@"{""MyInt"":1}", type);
            Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj));
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        public static void NonPublicInitOnlySetter_Without_JsonInclude_Fails(Type type)
        {
            // Non-public init-only property setter ignored.
            object obj = JsonSerializer.Deserialize(@"{""MyInt"":1}", type);
            Assert.Equal(0, (int)type.GetProperty("MyInt").GetValue(obj));

            // Public getter can be used for serialization.
            Assert.Equal(@"{""MyInt"":0}", JsonSerializer.Serialize(obj));
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        public static void NonPublicInitOnlySetter_With_JsonInclude_Works(Type type)
        {
            // Non-public init-only property setter included with [JsonInclude].
            object obj = JsonSerializer.Deserialize(@"{""MyInt"":1}", type);
            Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""MyInt"":1}", JsonSerializer.Serialize(obj));
        }

        public class ClassWithInitOnlyProperty
        {
            public int MyInt { get; init; }
        }

        public struct StructWithInitOnlyProperty
        {
            public int MyInt { get; init; }
        }

        public class Class_PropertyWith_PrivateInitOnlySetter
        {
            public int MyInt { get; private init; }
        }

        public class Class_PropertyWith_InternalInitOnlySetter
        {
            public int MyInt { get; internal init; }
        }

        public class Class_PropertyWith_ProtectedInitOnlySetter
        {
            public int MyInt { get; protected init; }
        }

        public class Class_PropertyWith_PrivateInitOnlySetter_WithAttribute
        {
            [JsonInclude]
            public int MyInt { get; private init; }
        }

        public class Class_PropertyWith_InternalInitOnlySetter_WithAttribute
        {
            [JsonInclude]
            public int MyInt { get; internal init; }
        }

        public class Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute
        {
            [JsonInclude]
            public int MyInt { get; protected init; }
        }
    }
}
