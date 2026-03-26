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

    public class Crc64GoIsoDriver : Crc64DriverBase
    {
        internal override Crc64ParameterSet ParameterSet => Crc64ParameterSet.Create(
            polynomial: 0x000000000000001B,
            initialValue: 0xFFFFFFFFFFFFFFFF,
            finalXorValue: 0xFFFFFFFFFFFFFFFF,
            reflectValues: true);

        internal override string EmptyOutput => "0000000000000000";
        internal override string Residue => "FFFFFFFFFFFFFFAC";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "000000000000206E",
                "Zero" => "00000090FFFFFF6F",
                "{ 0x00 }" => "000000000000906F",
                "{ 0x01, 0x00 }" => "000000000020FE6F",
                "Self-test 123456789" => "0110A475C75609B9",
                "The quick brown fox jumps over the lazy dog" => "8EE2C6F4194EF14E",
                "Lorem ipsum 256" => "E20CF7B513137D74",
                "Lorem ipsum 272" => "86FB06344BEE503E",
                "Lorem ipsum 384" => "462B90C6A99B89B3",
                "Lorem ipsum 1001" => "4B26A7A1D402A294",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc64RedisDriver : Crc64DriverBase
    {
        internal override Crc64ParameterSet ParameterSet => Crc64ParameterSet.Create(
            polynomial: 0xAD93D23594C935A9,
            initialValue: 0x0000000000000000,
            finalXorValue: 0x0000000000000000,
            reflectValues: true);

        internal override string EmptyOutput => "0000000000000000";
        internal override string Residue => "0000000000000000";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "79893530C870D87A",
                "Zero" => "0000000000000000",
                "{ 0x00 }" => "0000000000000000",
                "{ 0x01, 0x00 }" => "69DFBD73FD9FE989",
                "Self-test 123456789" => "CAD9B8C414D9C6E9",
                "The quick brown fox jumps over the lazy dog" => "2B37AAC396E57EBF",
                "Lorem ipsum 256" => "8EE7FD261BFD76F6",
                "Lorem ipsum 272" => "3226A17CFAC99D01",
                "Lorem ipsum 384" => "60278B19CC527A08",
                "Lorem ipsum 1001" => "6B8431140B338242",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc64Tests_ParameterSet_Custom_WE : Crc64Tests_Parameterized<Crc64WEDriver>;
    public class Crc64Tests_ParameterSet_Custom_GoIso : Crc64Tests_Parameterized<Crc64GoIsoDriver>;
    public class Crc64Tests_ParameterSet_Custom_Redis : Crc64Tests_Parameterized<Crc64RedisDriver>;
}
