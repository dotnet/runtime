// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Hashing.Tests
{
    public class Crc64WEDriver : Crc64DriverBase
    {
        internal override Crc64ParameterSet ParameterSet => Crc64ParameterSet.Create(
            polynomial: 0x42f0e1eba9ea3693,
            initialValue: 0xFFFFFFFFFFFFFFFF,
            finalXorValue: 0xFFFFFFFFFFFFFFFF,
            reflectValues: false);

        internal override string EmptyOutput => "0000000000000000";
        internal override string Residue => "03534142A6CE566D";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "D80C07CD676F836B",
                "Zero" => "D2F9D878AC61A52F",
                "{ 0x00 }" => "9AFCE626CE85B5F8",
                "{ 0x01, 0x00 }" => "4E5F78E1BA3CD74B",
                "Self-test 123456789" => "62EC59E3F1A4F00A",
                "The quick brown fox jumps over the lazy dog" => "BCD8BB366D256116",
                "Lorem ipsum 256" => "E103EC29594D7688",
                "Lorem ipsum 272" => "10D41FA7ED684849",
                "Lorem ipsum 384" => "225F96A9DD5ED822",
                "Lorem ipsum 1001" => "033B46C6C3BC5254",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc64Tests_ParameterSet_Custom_WE : Crc64Tests_Parameterized<Crc64WEDriver>;
}
