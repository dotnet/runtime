// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static partial class RandomTests
    {
        [Fact]
        public static void Unseeded()
        {
            Random r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                int x = r.Next(20);
                Assert.True(x >= 0 && x < 20);
            }
            for (int i = 0; i < 1000; i++)
            {
                int x = r.Next(20, 30);
                Assert.True(x >= 20 && x < 30);
            }
            for (int i = 0; i < 1000; i++)
            {
                double x = r.NextDouble();
                Assert.True(x >= 0.0 && x < 1.0);
            }
        }

        [Fact]
        public static void Seeded()
        {
            int seed = Environment.TickCount;

            Random r1 = new Random(seed);
            Random r2 = new Random(seed);

            byte[] b1 = new byte[1000];
            r1.NextBytes(b1);
            byte[] b2 = new byte[1000];
            r2.NextBytes(b2);
            for (int i = 0; i < b1.Length; i++)
            {
                Assert.Equal(b1[i], b2[i]);
            }
            for (int i = 0; i < b1.Length; i++)
            {
                int x1 = r1.Next();
                int x2 = r2.Next();
                Assert.Equal(x1, x2);
            }
        }

        // Random has a predictable sequence of values it generates based on its seed.
        // So that we'll be made aware if a change to the implementation causes these
        // sequences to change, this test verifies the first few numbers for a few seeds.
        private static int[][] Values()
        {
            var expectedValues = new int[][]
            {
                new int[] {1559595546, 1755192844, 1649316166, 1198642031, 442452829, 1200195957, 1945678308, 949569752, 2099272109, 587775847},
                new int[] {534011718, 237820880, 1002897798, 1657007234, 1412011072, 929393559, 760389092, 2026928803, 217468053, 1379662799},
                new int[] {1655911537, 867932563, 356479430, 2115372437, 234085668, 658591161, 1722583523, 956804207, 483147644, 24066104},
                new int[] {630327709, 1498044246, 1857544709, 426253993, 1203643911, 387788763, 537294307, 2034163258, 748827235, 815953056},
                new int[] {1752227528, 2128155929, 1211126341, 884619196, 25718507, 116986365, 1499488738, 964038662, 1014506826, 1607840008},
                new int[] {726643700, 610783965, 564707973, 1342984399, 995276750, 1993667614, 314199522, 2041397713, 1280186417, 252243313},
                new int[] {1848543519, 1240895648, 2065773252, 1801349602, 1964834993, 1722865216, 1276393953, 971273117, 1545866008, 1044130265},
                new int[] {822959691, 1871007331, 1419354884, 112231158, 786909589, 1452062818, 91104737, 2048632168, 1811545599, 1836017217},
                new int[] {1944859510, 353635367, 772936516, 570596361, 1756467832, 1181260420, 1053299168, 978507572, 2077225190, 480420522},
                new int[] {919275682, 983747050, 126518148, 1028961564, 578542428, 910458022, 2015493599, 2055866623, 195421134, 1272307474},
                new int[] {2041175501, 1613858733, 1627583427, 1487326767, 1548100671, 639655624, 830204383, 985742027, 461100725, 2064194426},
                new int[] {1015591673, 96486769, 981165059, 1945691970, 370175267, 368853226, 1792398814, 2063101078, 726780316, 708597731},
                new int[] {2137491492, 726598452, 334746691, 256573526, 1339733510, 98050828, 607109598, 992976482, 992459907, 1500484683},
                new int[] {1111907664, 1356710135, 1835811970, 714938729, 161808106, 1974732077, 1569304029, 2070335533, 1258139498, 144887988},
                new int[] {86323836, 1986821818, 1189393602, 1173303932, 1131366349, 1703929679, 384014813, 1000210937, 1523819089, 936774940},
                new int[] {1208223655, 469449854, 542975234, 1631669135, 2100924592, 1433127281, 1346209244, 2077569988, 1789498680, 1728661892},
                new int[] {182639827, 1099561537, 2044040513, 2090034338, 922999188, 1162324883, 160920028, 1007445392, 2055178271, 373065197},
                new int[] {1304539646, 1729673220, 1397622145, 400915894, 1892557431, 891522485, 1123114459, 2084804443, 173374215, 1164952149},
                new int[] {278955818, 212301256, 751203777, 859281097, 714632027, 620720087, 2085308890, 1014679847, 439053806, 1956839101},
                new int[] {1400855637, 842412939, 104785409, 1317646300, 1684190270, 349917689, 900019674, 2092038898, 704733397, 601242406},
            };
            return (expectedValues);
        }

        [Fact]
        public static void ExpectedValues()
        {
            int[][] expectedValues = Values();
            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                var r = new Random(seed);
                for (int i = 0; i < expectedValues[seed].Length; i++)
                {
                    Assert.Equal(expectedValues[seed][i], r.Next());
                }
            }
        }

        private static byte[][] ByteValues()
        {
            var expectedValues = new byte[][]
            {
                new byte[] { 0x1A, 0xC, 0x46, 0x6F, 0x5D, 0x75, 0xE4, 0xD8, 0xAD, 0x67 },
                new byte[] { 0x46, 0xD0, 0x86, 0x82, 0x40, 0x97, 0xE4, 0xA3, 0x95, 0xCF },
                new byte[] { 0x71, 0x93, 0xC6, 0x95, 0x24, 0xB9, 0xE3, 0x6F, 0x7C, 0x38 },
                new byte[] { 0x9D, 0x56, 0x5, 0xA9, 0x7, 0xDB, 0xE3, 0x3A, 0x63, 0xA0 },
                new byte[] { 0xC8, 0x19, 0x45, 0xBC, 0xEB, 0xFD, 0xE2, 0x6, 0x4A, 0x8 },
                new byte[] { 0xF4, 0xDD, 0x85, 0xCF, 0xCE, 0x1E, 0xE2, 0xD1, 0x31, 0x71 },
                new byte[] { 0x1F, 0xA0, 0xC4, 0xE2, 0xB1, 0x40, 0xE1, 0x9D, 0x18, 0xD9 },
                new byte[] { 0x4B, 0x63, 0x4, 0xF6, 0x95, 0x62, 0xE1, 0x68, 0xFF, 0x41 },
                new byte[] { 0x76, 0x27, 0x44, 0x9, 0x78, 0x84, 0xE0, 0x34, 0xE6, 0xAA },
                new byte[] { 0xA2, 0xEA, 0x84, 0x1C, 0x5C, 0xA6, 0xDF, 0xFF, 0xCE, 0x12 },
                new byte[] { 0xCD, 0xAD, 0xC3, 0x2F, 0x3F, 0xC8, 0xDF, 0xCB, 0xB5, 0x7A },
                new byte[] { 0xF9, 0x71, 0x3, 0x42, 0x23, 0xEA, 0xDE, 0x96, 0x9C, 0xE3 },
                new byte[] { 0x24, 0x34, 0x43, 0x56, 0x6, 0xC, 0xDE, 0x62, 0x83, 0x4B },
                new byte[] { 0x50, 0xF7, 0x82, 0x69, 0xEA, 0x2D, 0xDD, 0x2D, 0x6A, 0xB4 },
                new byte[] { 0x7C, 0xBA, 0xC2, 0x7C, 0xCD, 0x4F, 0xDD, 0xF9, 0x51, 0x1C },
                new byte[] { 0xA7, 0x7E, 0x2, 0x8F, 0xB0, 0x71, 0xDC, 0xC4, 0x38, 0x84 },
                new byte[] { 0xD3, 0x41, 0x41, 0xA2, 0x94, 0x93, 0xDC, 0x90, 0x1F, 0xED },
                new byte[] { 0xFE, 0x4, 0x81, 0xB6, 0x77, 0xB5, 0xDB, 0x5B, 0x7, 0x55 },
                new byte[] { 0x2A, 0xC8, 0xC1, 0xC9, 0x5B, 0xD7, 0xDA, 0x27, 0xEE, 0xBD },
                new byte[] { 0x55, 0x8B, 0x1, 0xDC, 0x3E, 0xF9, 0xDA, 0xF2, 0xD5, 0x26 }
            };
            return (expectedValues);
        }

        [Fact]
        public static void ExpectedValues_NextBytesArray()
        {
            byte[][] expectedValues = ByteValues();
            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                byte[] actualValues = new byte[expectedValues[seed].Length];
                var r = new Random(seed);
                r.NextBytes(actualValues);
                Assert.Equal(expectedValues[seed], actualValues);
            }
        }

        [Fact]
        public static void Sample()
        {
            SubRandom r = new SubRandom();

            for (int i = 0; i < 1000; i++)
            {
                double d = r.ExposeSample();
                Assert.True(d >= 0.0 && d < 1.0);
            }
        }

        private class SubRandom : Random
        {
            public double ExposeSample()
            {
                return Sample();
            }
        }
    }
}
