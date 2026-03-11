// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Hashing.Tests
{
    public class Crc32CksumDriver : Crc32DriverBase
    {
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Create(
            polynomial: 0x04C11DB7,
            initialValue: 0x00000000,
            finalXorValue: 0xFFFFFFFF,
            reflectValues: false);

        internal override string EmptyOutput => "FFFFFFFF";
        internal override string Residue => "38FB2284";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "FB3EE248",
                "Zero" => "FFFFFFFF",
                "Self-test 123456789" => "765E7680",
                "The quick brown fox jumps over the lazy dog" => "36B78081",
                "Lorem ipsum 128" => "CD8EF435",
                "Lorem ipsum 144" => "04BD8AF7",
                "Lorem ipsum 1001" => "CD98BE63",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc32MefDriver : Crc32DriverBase
    {
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Create(
            polynomial: 0x741b8cd7,
            initialValue: 0xFFFFFFFF,
            finalXorValue: 0x00000000,
            reflectValues: true);

        internal override string EmptyOutput => "FFFFFFFF";
        internal override string Residue => "00000000";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "47173856",
                "Zero" => "3B324308",
                "Self-test 123456789" => "512FC2D2",
                "The quick brown fox jumps over the lazy dog" => "6F24DE1F",
                "Lorem ipsum 128" => "F4D0B046",
                "Lorem ipsum 144" => "14416454",
                "Lorem ipsum 1001" => "152A4D10",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc32CDRomEdcDriver : Crc32DriverBase
    {
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Create(
            polynomial: 0x8001801B,
            initialValue: 0x00000000,
            finalXorValue: 0x00000000,
            reflectValues: true);

        internal override string EmptyOutput => "00000000";
        internal override string Residue => "00000000";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "01019190",
                "Zero" => "00000000",
                "Self-test 123456789" => "C4EDC26E",
                "The quick brown fox jumps over the lazy dog" => "7E1EF9D9",
                "Lorem ipsum 128" => "896BC2A4",
                "Lorem ipsum 144" => "E204176B",
                "Lorem ipsum 1001" => "AC86A81C",
                _ => throw new ArgumentOutOfRangeException(nameof(testCaseName), testCaseName, "Unmapped Value"),
            };
    }

    public class Crc32HD16ForwardDriver : Crc32DriverBase
    {
        // Koopman's HD-13 CRC-32 polynomial with an arbitrary non-zero initial and final value.
        // This value is really just a polynomial with a lot of high bits set to ensure the vector code
        // isn't depending on the common 0x04... 
        internal override Crc32ParameterSet ParameterSet => Crc32ParameterSet.Create(
            polynomial: 0xE89061DB,
            initialValue: 0x00000001,
            finalXorValue: 0x00000003,
            reflectValues: false);

        internal override string EmptyOutput => "00000002";
        internal override string Residue => "D120C3B5";

        internal override string? GetExpectedOutput(string testCaseName) =>
            testCaseName switch
            {
                "One" => "E89060D8",
                "Zero" => "E89061D8",
                "Self-test 123456789" => "00FA61B6",
                "The quick brown fox jumps over the lazy dog" => "A81AC12F",
                "Lorem ipsum 128" => "7B773166",
                "Lorem ipsum 144" => "D3A67D09",
                "Lorem ipsum 1001" => "2E99E5F2",
                _ => null,
            };
    }

    public class Crc32Tests_ParameterSet_Custom_Cksum : Crc32Tests_Parameterized<Crc32CksumDriver>;
    public class Crc32Tests_ParameterSet_Custom_CDRomEdc : Crc32Tests_Parameterized<Crc32CDRomEdcDriver>;
    public class Crc32Tests_ParameterSet_Custom_Mef : Crc32Tests_Parameterized<Crc32MefDriver>;
    public class Crc32Tests_ParameterSet_Custom_HD16Forward : Crc32Tests_Parameterized<Crc32HD16ForwardDriver>;
}
