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
    }
}
