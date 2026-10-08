// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc64NvmeDriver : Crc64DriverBase
    {
        internal override Crc64ParameterSet ParameterSet => Crc64ParameterSet.Nvme;

        internal override string EmptyOutput => "0000000000000000";
        internal override string Residue => "BD9190D4C4CFEF0C";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "510ED9DF8FA0B4AA",
                "Zero" => "2450DAA1E511B86D",
                "{ 0x00 }" => "2887ECEF4750DAD5",
                "{ 0x01, 0x00 }" => "B195D6A2A931A405",
                "Self-test 123456789" => "8898790A86148BAE",
                "The quick brown fox jumps over the lazy dog" => "43C1544905546CD7",
                "Lorem ipsum 256" => "89E7B0A9DD9C2926",
                "Lorem ipsum 272" => "F62B865740AB6502",
                "Lorem ipsum 384" => "2759B3D6521D1E41",
                "Lorem ipsum 1001" => "12B2C874E65876D4",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc64Tests_ParameterSet_Nvme : Crc64Tests_Parameterized<Crc64NvmeDriver>
    {
        [Fact]
        public void StaticProperty_IsSingleton()
        {
            Crc64ParameterSet instance1 = Crc64ParameterSet.Nvme;
            Crc64ParameterSet instance2 = Crc64ParameterSet.Nvme;
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void StaticProperty_HasExpectedValues()
        {
            Crc64ParameterSet nvme = Crc64ParameterSet.Nvme;
            Assert.Equal(0xAD93D23594C93659UL, nvme.Polynomial);
            Assert.Equal(0xFFFFFFFFFFFFFFFFUL, nvme.InitialValue);
            Assert.Equal(0xFFFFFFFFFFFFFFFFUL, nvme.FinalXorValue);
            Assert.True(nvme.ReflectValues);
        }
    }
}
