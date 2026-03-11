// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc32Driver : Crc32DriverBase
    {
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Crc32;

        internal override string EmptyOutput => "00000000";
        internal override string Residue => "1CDF4421";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "1BDF05A5",
                "Zero" => "1CDF4421",
                "Self-test 123456789" => "2639F4CB",
                "The quick brown fox jumps over the lazy dog" => "39A34F41",
                "Lorem ipsum 128" => "931A6737",
                "Lorem ipsum 144" => "2B719549",
                "Lorem ipsum 1001" => "0464ED5F",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class CustomCrc32Driver : Crc32Driver
    {
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Create(
            polynomial: 0x04C11DB7,
            initialValue: 0xFFFFFFFF,
            finalXorValue: 0xFFFFFFFF,
            reflectValues: true);
    }

    public sealed class Crc32Tests_ParameterSet_Crc32 : Crc32Tests_Parameterized<Crc32Driver>
    {
        [Fact]
        public void StaticProperty_IsSingleton()
        {
            Crc32ParameterSet instance1 = Crc32ParameterSet.Crc32;
            Crc32ParameterSet instance2 = Crc32ParameterSet.Crc32;
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void StaticProperty_HasExpectedValues()
        {
            Crc32ParameterSet crc32 = Crc32ParameterSet.Crc32;
            Assert.Equal(0x04C11DB7u, crc32.Polynomial);
            Assert.Equal(0xFFFFFFFFu, crc32.InitialValue);
            Assert.Equal(0xFFFFFFFFu, crc32.FinalXorValue);
            Assert.True(crc32.ReflectValues);
        }
    }

    public sealed class Crc32Tests_ParameterSet_Custom_Crc32 : Crc32Tests_Parameterized<CustomCrc32Driver>
    {
        [Fact]
        public void CreateIsNotSingleton()
        {
            Assert.NotSame(s_parameterSet, Crc32ParameterSet.Crc32);
        }

        [Fact]
        public void StaticProperty_HasExpectedValues()
        {
            Crc32ParameterSet crc32 = Crc32ParameterSet.Crc32;
            Assert.Equal(0x04C11DB7u, crc32.Polynomial);
            Assert.Equal(0xFFFFFFFFu, crc32.InitialValue);
            Assert.Equal(0xFFFFFFFFu, crc32.FinalXorValue);
            Assert.True(crc32.ReflectValues);
        }
    }
}
