// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests.NETStandard
{
    public class NETStandardContextTests
    {
        /// <summary>
        /// Tests that we can serialize and deserialize a type defined in a NETStandard assembly.
        /// This tests an issue where we were emitting source-gen logic that caused the compiler
        /// to emit a reference to an internal definition of IsExternalInit that was missing
        /// on later versions of .NET (since it was defined by the framework).
        /// </summary>
        [Fact]
        public void RoundTripNETStandardDefinedSourceGenType()
        {
            MyPoco expected = new MyPoco() { Value = "Hello from NETStandard type."};

            string json = JsonSerializer.Serialize(expected, NETStandardSerializerContext.Default.MyPoco);
            MyPoco actual = JsonSerializer.Deserialize(json, NETStandardSerializerContext.Default.MyPoco);
            Assert.Equal(expected.Value, actual.Value);
        }

        [Fact]
        public void NETStandardReflectionAccessors_PreserveExceptions()
        {
            ClassWithPrivateConstructor result = JsonSerializer.Deserialize(
                """{"Value":42}""", NETStandardSerializerContext.Default.ClassWithPrivateConstructor);
            Assert.Equal(42, result.Value);

            ArgumentOutOfRangeException constructorException = Assert.Throws<ArgumentOutOfRangeException>(() =>
                JsonSerializer.Deserialize("""{"Value":-1}""", NETStandardSerializerContext.Default.ClassWithPrivateConstructor));
            Assert.Equal("value", constructorException.ParamName);

            InvalidOperationException getterException = Assert.Throws<InvalidOperationException>(() =>
                JsonSerializer.Serialize(default(StructWithThrowingPrivateGetter), NETStandardSerializerContext.Default.StructWithThrowingPrivateGetter));
            Assert.Equal("Getter failure.", getterException.Message);
        }
    }
}
