// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc32CDriver : Crc32DriverBase
    {
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Crc32C;

        internal override string EmptyOutput => "00000000";
        internal override string Residue => "C74B6748";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "52D016A0",
                "Zero" => "C74B6748",
                "Self-test 123456789" => "839206E3",
                "The quick brown fox jumps over the lazy dog" => "04046222",
                "Lorem ipsum 128" => "189C3883",
                "Lorem ipsum 144" => "E7A2AA7A",
                "Lorem ipsum 1001" => "104CDF35",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc32Tests_ParameterSet_Crc32C : Crc32Tests_Parameterized<Crc32CDriver>
    {
        [Fact]
        public void StaticProperty_IsSingleton()
        {
            Crc32ParameterSet instance1 = Crc32ParameterSet.Crc32C;
            Crc32ParameterSet instance2 = Crc32ParameterSet.Crc32C;
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void StaticProperty_HasExpectedValues()
        {
            Crc32ParameterSet crc32 = Crc32ParameterSet.Crc32C;
            Assert.Equal(0x1EDC6F41u, crc32.Polynomial);
            Assert.Equal(0xFFFFFFFFu, crc32.InitialValue);
            Assert.Equal(0xFFFFFFFFu, crc32.FinalXorValue);
            Assert.True(crc32.ReflectValues);
        }
    }
}
