// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc64Driver : Crc64DriverBase
    {
        internal override Crc64ParameterSet ParameterSet => Crc64ParameterSet.Crc64;

        internal override string EmptyOutput => "0000000000000000";
        internal override string Residue => "0000000000000000";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "42F0E1EBA9EA3693",
                "Zero" => "0000000000000000",
                "{ 0x00 }" => "0000000000000000",
                "{ 0x01, 0x00 }" => "AF052A6B538EDF09",
                "Self-test 123456789" => "6C40DF5F0B497347",
                "The quick brown fox jumps over the lazy dog" => "41E05242FFA9883B",
                "Lorem ipsum 256" => "DA70046E6B79DD83",
                "Lorem ipsum 272" => "A94F5E9C5557F65A",
                "Lorem ipsum 384" => "5768E3F2E9A63829",
                "Lorem ipsum 1001" => "3ECF3A363FC5BD59",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc64Tests_ParameterSet_Crc64 : Crc64Tests_Parameterized<Crc64Driver>
    {
        [Fact]
        public void StaticProperty_IsSingleton()
        {
            Crc64ParameterSet instance1 = Crc64ParameterSet.Crc64;
            Crc64ParameterSet instance2 = Crc64ParameterSet.Crc64;
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void StaticProperty_HasExpectedValues()
        {
            Crc64ParameterSet crc64 = Crc64ParameterSet.Crc64;
            Assert.Equal(0x42F0E1EBA9EA3693UL, crc64.Polynomial);
            Assert.Equal(0UL, crc64.InitialValue);
            Assert.Equal(0UL, crc64.FinalXorValue);
            Assert.False(crc64.ReflectValues);
        }
    }
}
