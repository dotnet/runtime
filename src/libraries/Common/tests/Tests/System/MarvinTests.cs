// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Test.Cryptography;

using Xunit;

namespace Tests.System
{
    public class MarvinTests
    {
        private const ulong Seed1 = 0x4FB61A001BDBCCLU;
        private const ulong Seed2 = 0x804FB61A001BDBCCLU;
        private const ulong Seed3 = 0x804FB61A801BDBCCLU;

        private const string TestDataString0Byte = "";
        private const string TestDataString1Byte = "af";
        private const string TestDataString2Byte = "e70f";
        private const string TestDataString3Byte = "37f495";
        private const string TestDataString4Byte = "8642dc59";
        private const string TestDataString5Byte = "153fb79826";
        private const string TestDataString6Byte = "0932e6246c47";
        private const string TestDataString7Byte = "ab427ea8d10fc7";

        [Theory]
        [MemberData(nameof(TestDataAndExpectedHashes))]
        public void ComputeHash_Success(ulong seed, string testDataString, ulong expectedHash)
        {
            var testDataSpan = new Span<byte>(testDataString.HexToByteArray());
            long hash = Marvin.ComputeHash(testDataSpan, seed);
            Assert.Equal((long)expectedHash, hash);
        }

        public static object[][] TestDataAndExpectedHashes =
        {
            new object[] { Seed1, TestDataString0Byte, 0x30ED35C100CD3C7DLU },
            new object[] { Seed1, TestDataString1Byte, 0x48E73FC77D75DDC1LU },
            new object[] { Seed1, TestDataString2Byte, 0xB5F6E1FC485DBFF8LU },
            new object[] { Seed1, TestDataString3Byte, 0xF0B07C789B8CF7E8LU },
            new object[] { Seed1, TestDataString4Byte, 0x7008F2E87E9CF556LU },
            new object[] { Seed1, TestDataString5Byte, 0xE6C08C6DA2AFA997LU },
            new object[] { Seed1, TestDataString6Byte, 0x6F04BF1A5EA24060LU },
            new object[] { Seed1, TestDataString7Byte, 0xE11847E4F0678C41LU },

            new object[] { Seed2, TestDataString0Byte, 0x10A9D5D3996FD65DLU },
            new object[] { Seed2, TestDataString1Byte, 0x68201F91960EBF91LU },
            new object[] { Seed2, TestDataString2Byte, 0x64B581631F6AB378LU },
            new object[] { Seed2, TestDataString3Byte, 0xE1F2DFA6E5131408LU },
            new object[] { Seed2, TestDataString4Byte, 0x36289D9654FB49F6LU },
            new object[] { Seed2, TestDataString5Byte, 0xA06114B13464DBDLU },
            new object[] { Seed2, TestDataString6Byte, 0xD6DD5E40AD1BC2EDLU },
            new object[] { Seed2, TestDataString7Byte, 0xE203987DBA252FB3LU },

            new object[] { Seed3, "00", 0xA37FB0DA2ECAE06CLU },
            new object[] { Seed3, "FF", 0xFECEF370701AE054LU },
            new object[] { Seed3, "00FF", 0xA638E75700048880LU },
            new object[] { Seed3, "FF00", 0xBDFB46D969730E2ALU },
            new object[] { Seed3, "FF00FF", 0x9D8577C0FE0D30BFLU },
            new object[] { Seed3, "00FF00", 0x4F9FBDDE15099497LU },
            new object[] { Seed3, "00FF00FF", 0x24EAA279D9A529CALU },
            new object[] { Seed3, "FF00FF00", 0xD3BEC7726B057943LU },
            new object[] { Seed3, "FF00FF00FF", 0x920B62BBCA3E0B72LU },
            new object[] { Seed3, "00FF00FF00", 0x1D7DDF9DFDF3C1BFLU },
            new object[] { Seed3, "00FF00FF00FF", 0xEC21276A17E821A5LU },
            new object[] { Seed3, "FF00FF00FF00", 0x6911A53CA8C12254LU },
            new object[] { Seed3, "FF00FF00FF00FF", 0xFDFD187B1D3CE784LU },
            new object[] { Seed3, "00FF00FF00FF00", 0x71876F2EFB1B0EE8LU },
        };
    }
}
