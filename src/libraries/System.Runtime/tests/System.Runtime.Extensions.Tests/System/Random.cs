// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public class RandomTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void InvalidArguments_Throws(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            Assert.Throws<ArgumentNullException>(() => r.NextBytes(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Next(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Next(2, 1));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void SmallRanges_ReturnsExpectedValue(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            Assert.Equal(0, r.Next(0));
            Assert.Equal(0, r.Next(0, 0));
            Assert.Equal(1, r.Next(1, 1));

            Assert.Equal(0, r.Next(1));
            Assert.Equal(1, r.Next(1, 2));

            Assert.Equal(0, r.NextInt64(0));
            Assert.Equal(0, r.NextInt64(0, 0));
            Assert.Equal(1, r.NextInt64(1, 1));

            Assert.Equal(0, r.NextInt64(1));
            Assert.Equal(1, r.NextInt64(1, 2));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextInt_AllValuesAreWithinSpecifiedRange(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            for (int i = 0; i < 1000; i++)
            {
                Assert.InRange(r.Next(20), 0, 19);
                Assert.InRange(r.Next(20, 30), 20, 29);

                Assert.InRange(r.NextInt64(20), 0, 19);
                Assert.InRange(r.NextInt64(20, 30), 20, 29);
            }

            for (int i = 0; i < 5_000_000; i++)
            {
                float x = r.NextSingle();
                Assert.True(x >= 0.0 && x < 1.0);
            }

            for (int i = 0; i < 5_000_000; i++)
            {
                double x = r.NextDouble();
                Assert.True(x >= 0.0 && x < 1.0);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Next_Int_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            var hs = new HashSet<int>();
            for (int i = 0; i < 10_000; i++)
            {
                hs.Add(r.Next(4));
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Contains(i, hs);
            }

            Assert.DoesNotContain(-1, hs);
            Assert.DoesNotContain(4, hs);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Next_IntInt_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            var hs = new HashSet<int>();
            for (int i = 0; i < 10_000; i++)
            {
                hs.Add(r.Next(42, 44));
            }

            for (int i = 42; i < 44; i++)
            {
                Assert.Contains(i, hs);
            }

            Assert.DoesNotContain(41, hs);
            Assert.DoesNotContain(44, hs);
        }

        public static IEnumerable<object[]> Next_IntInt_Next_IntInt_AllValuesAreWithinRange_MemberData() =>
            from derived in new[] { false, true }
            from seeded in new[] { false, true }
            from (int min, int max) pair in new[]
            {
                (1, 2),
                (-10, -3),
                (0, int.MaxValue),
                (-1, int.MaxValue),
                (int.MinValue, 0),
                (int.MinValue, int.MaxValue),
            }
            select new object[] { derived, seeded, pair.min, pair.max };

        [Theory]
        [MemberData(nameof(Next_IntInt_Next_IntInt_AllValuesAreWithinRange_MemberData))]
        public void Next_IntInt_Next_IntInt_AllValuesAreWithinRange(bool derived, bool seeded, int min, int max)
        {
            Random r = Create(derived, seeded);
            for (int i = 0; i < 100; i++)
            {
                Assert.InRange(r.Next(min, max), min, max - 1);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Next_Long_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            var hs = new HashSet<long>();
            for (int i = 0; i < 10_000; i++)
            {
                hs.Add(r.NextInt64(4));
            }

            for (long i = 0; i < 4; i++)
            {
                Assert.Contains(i, hs);
            }

            Assert.DoesNotContain(-1L, hs);
            Assert.DoesNotContain(4L, hs);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Next_LongLong_AllValuesWithinSmallRangeHit(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            var hs = new HashSet<long>();
            for (int i = 0; i < 10_000; i++)
            {
                hs.Add(r.NextInt64(42, 44));
            }

            for (long i = 42; i < 44; i++)
            {
                Assert.Contains(i, hs);
            }

            Assert.DoesNotContain(41L, hs);
            Assert.DoesNotContain(44L, hs);
        }

        public static IEnumerable<object[]> Next_LongLong_Next_IntInt_AllValuesAreWithinRange_MemberData() =>
            from derived in new[] { false, true }
            from seeded in new[] { false, true }
            from (long min, long max) pair in new[]
            {
                (1L, 2L),
                (0L, long.MaxValue),
                (2147483648, 2147483658),
                (-1L, long.MaxValue),
                (long.MinValue, 0L),
                (long.MinValue, long.MaxValue),
            }
            select new object[] { derived, seeded, pair.min, pair.max };

        [Theory]
        [MemberData(nameof(Next_LongLong_Next_IntInt_AllValuesAreWithinRange_MemberData))]
        public void Next_LongLong_Next_IntInt_AllValuesAreWithinRange(bool derived, bool seeded, long min, long max)
        {
            Random r = Create(derived, seeded);
            for (int i = 0; i < 100; i++)
            {
                Assert.InRange(r.NextInt64(min, max), min, max - 1);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CtorWithSeed_SequenceIsRepeatable(bool derived)
        {
            Random r1 = Create(derived, seeded: true);
            Random r2 = Create(derived, seeded: true);

            for (int i = 0; i < 2; i++)
            {
                byte[] b1 = new byte[1000];
                byte[] b2 = new byte[1000];
                if (i == 0)
                {
                    r1.NextBytes(b1);
                    r2.NextBytes(b2);
                }
                else
                {
                    r1.NextBytes((Span<byte>)b1);
                    r2.NextBytes((Span<byte>)b2);
                }
                AssertExtensions.SequenceEqual(b1, b2);
            }

            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(r1.Next(), r2.Next());
            }
        }

        [Fact]
        public void ExpectedValues_DerivedSeededMatchesBaseSeeded()
        {
            for (int i = 0; i < 10; i++)
            {
                int seed = Random.Shared.Next();

                var baseSeeded = new Random(seed);
                var derivedSeeded = new SubRandom(seed);

                byte[] baseBuffer = new byte[42];
                byte[] derivedBuffer = new byte[42];

                Assert.Equal(baseSeeded.Next(), derivedSeeded.Next());
                Assert.Equal(baseSeeded.Next(42), derivedSeeded.Next(42));
                Assert.Equal(baseSeeded.Next(1, 42), derivedSeeded.Next(1, 42));

                Assert.Equal(baseSeeded.NextInt64(), derivedSeeded.NextInt64());
                Assert.Equal(baseSeeded.NextInt64(42), derivedSeeded.NextInt64(42));
                Assert.Equal(baseSeeded.NextInt64(int.MaxValue, long.MaxValue), derivedSeeded.NextInt64(int.MaxValue, long.MaxValue));

                Assert.Equal(baseSeeded.NextDouble(), derivedSeeded.NextDouble());
                Assert.Equal(baseSeeded.NextSingle(), derivedSeeded.NextSingle());

                baseSeeded.NextBytes(baseBuffer);
                derivedSeeded.NextBytes(derivedBuffer);
                AssertExtensions.SequenceEqual(baseBuffer, derivedBuffer);

                baseSeeded.NextBytes((Span<byte>)baseBuffer);
                derivedSeeded.NextBytes((Span<byte>)derivedBuffer);
                AssertExtensions.SequenceEqual(baseBuffer, derivedBuffer);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExpectedValues(bool derived)
        {
            // Random has a predictable sequence of values it generates based on its seed.
            // So that we'll be made aware if a change to the implementation causes these
            // sequences to change, this test verifies the first few numbers for a few seeds.
            int[][] expectedValues = new int[][]
            {
                new int[] { 1559595546, 1755192844, 1649316166, 1198642031, 442452829, 1200195957, 1945678308, 949569752, 2099272109, 587775847 },
                new int[] { 534011718, 237820880, 1002897798, 1657007234, 1412011072, 929393559, 760389092, 2026928803, 217468053, 1379662799 },
                new int[] { 1655911537, 867932563, 356479430, 2115372437, 234085668, 658591161, 1722583523, 956804207, 483147644, 24066104 },
                new int[] { 630327709, 1498044246, 1857544709, 426253993, 1203643911, 387788763, 537294307, 2034163258, 748827235, 815953056 },
                new int[] { 1752227528, 2128155929, 1211126341, 884619196, 25718507, 116986365, 1499488738, 964038662, 1014506826, 1607840008 },
                new int[] { 726643700, 610783965, 564707973, 1342984399, 995276750, 1993667614, 314199522, 2041397713, 1280186417, 252243313 },
                new int[] { 1848543519, 1240895648, 2065773252, 1801349602, 1964834993, 1722865216, 1276393953, 971273117, 1545866008, 1044130265 },
                new int[] { 822959691, 1871007331, 1419354884, 112231158, 786909589, 1452062818, 91104737, 2048632168, 1811545599, 1836017217 },
                new int[] { 1944859510, 353635367, 772936516, 570596361, 1756467832, 1181260420, 1053299168, 978507572, 2077225190, 480420522 },
                new int[] { 919275682, 983747050, 126518148, 1028961564, 578542428, 910458022, 2015493599, 2055866623, 195421134, 1272307474 },
                new int[] { 2041175501, 1613858733, 1627583427, 1487326767, 1548100671, 639655624, 830204383, 985742027, 461100725, 2064194426 },
                new int[] { 1015591673, 96486769, 981165059, 1945691970, 370175267, 368853226, 1792398814, 2063101078, 726780316, 708597731 },
                new int[] { 2137491492, 726598452, 334746691, 256573526, 1339733510, 98050828, 607109598, 992976482, 992459907, 1500484683 },
                new int[] { 1111907664, 1356710135, 1835811970, 714938729, 161808106, 1974732077, 1569304029, 2070335533, 1258139498, 144887988 },
                new int[] { 86323836, 1986821818, 1189393602, 1173303932, 1131366349, 1703929679, 384014813, 1000210937, 1523819089, 936774940 },
                new int[] { 1208223655, 469449854, 542975234, 1631669135, 2100924592, 1433127281, 1346209244, 2077569988, 1789498680, 1728661892 },
                new int[] { 182639827, 1099561537, 2044040513, 2090034338, 922999188, 1162324883, 160920028, 1007445392, 2055178271, 373065197 },
                new int[] { 1304539646, 1729673220, 1397622145, 400915894, 1892557431, 891522485, 1123114459, 2084804443, 173374215, 1164952149 },
                new int[] { 278955818, 212301256, 751203777, 859281097, 714632027, 620720087, 2085308890, 1014679847, 439053806, 1956839101 },
                new int[] { 1400855637, 842412939, 104785409, 1317646300, 1684190270, 349917689, 900019674, 2092038898, 704733397, 601242406 },
            };

            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                Random r = derived ? new SubRandom(seed) : new Random(seed);
                for (int i = 0; i < expectedValues[seed].Length; i++)
                {
                    Assert.Equal(expectedValues[seed][i], r.Next());
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExpectedValues_Next64(bool derived)
        {
            long[][] expectedValues = new long[][]
            {
                new long[] { 7083764782846131554, 5154802594370149492, 9016307100457696812, 4310211293818176047, 9058748489721462462, 9180463484486351044, 7534648089071954807, 300923067154768701, 8614801378171577836, 748103725286293756, },
                new long[] { 4307412542716114199, 3991716777541808379, 934021439863608487, 2287661736829161214, 6291475812869357143, 5675567160283690199, 8760158359188310330, 3523056573073259785, 7321359148939577545, 8176239586367362256, },
                new long[] { 1531069098683313357, 2828622164618347905, 2075099020033370914, 265112179838049230, 3524203136017251824, 2170679632171954410, 762296592449890045, 6745207671181989590, 6027925715800599462, 6380994614502729892, },
                new long[] { 7978097691503191170, 1665536347787909639, 3216194192387080605, 7465934659703810205, 756930459165146506, 7889164140914994429, 1987815658659267775, 743986732433846434, 4734483486568599171, 4585758438729022584, },
                new long[] { 5201754247470390327, 502450530957471373, 4357271772556843032, 5443376306619676012, 7213038615260839203, 4384276612803258641, 3213334724868645506, 3966129034447456878, 3441041257334501728, 2790513466864390220, },
                new long[] { 2425402007342470124, 8562736750981808916, 5498366944908455571, 3420835545721586235, 4445757142315711677, 879397880784545060, 4438844994985001029, 7188280132554089531, 2147607824197620797, 995277291092780064, },
                new long[] { 8872430600164445090, 7399650934153467802, 6639444525078217998, 1398277192639549194, 1678493261558725718, 6597873593434562871, 5664364061194378760, 1187050397712924167, 854156798872598297, 8423404356080826356, },
                new long[] { 6096087156129547095, 6236565117320932384, 7780530901338905481, 8599090876412287961, 8134592621559299055, 3092986065322827082, 6889874331310734282, 4409192699726534611, 8784086606495373814, 6628168180309216200, },
                new long[] { 3319734916003724044, 5073479300492591271, 8921617277601690116, 6576541319421175976, 5367319944707193737, 8811470574065867102, 8115384601427089805, 7631343797835264416, 7490653173356395731, 4832923208444583836, },
                new long[] { 543391471966728898, 3910384687569130797, 839331617009698943, 4553991762430063991, 2600047267857185570, 5306583045956228465, 117531630781691728, 1630122859085024108, 6197210944124395440, 3037678236577854320, },
                new long[] { 6990420064788703863, 2747298870740789683, 1980409197177364218, 2531442205441049158, 9056155423952878267, 1801695517842395524, 1343041900898047250, 4852273957193753912, 4903777510985417357, 1242442060806244164, },
                new long[] { 4214067824660783660, 1584213053910351417, 3121504369533171061, 508883852356914965, 6288873951005653589, 7520180026587532695, 2568560967107424981, 8074416259207364357, 3610335281753417066, 8670577921889409817, },
                new long[] { 1437724380627982818, 421127237079913152, 4262581949700836336, 7709706332222675941, 3521610070248667630, 4015301294568819115, 3794080033314705560, 2073186524366198993, 2316893052521416775, 6875332950022680301, },
                new long[] { 7884752973447860631, 8481404661011228486, 5403668325963620971, 5687156775231563956, 754337393396562311, 510404970364061118, 5019590303431061083, 5295337622472831645, 1023459619382438692, 5080087978158047937, },
                new long[] { 5108409529412962636, 7318318844180790220, 6544754702222211302, 3664607218240451971, 7210445549492255009, 6228889479107101137, 6245109369640438813, 8517479924486442090, 8953389427003117057, 3284851802384340629, },
                new long[] { 2332057289287139585, 6155241823445471314, 7685841078484995937, 1642048865158414930, 4443164076547127482, 2724001950995365348, 7470610843663772128, 2516258985740396086, 7659947197771116765, 1489615626614827625, },
                new long[] { 8779085882107017399, 4992147210522010841, 8826927454745683420, 8842862548929056545, 1675900195788044372, 8442486459738405368, 8696129909873149859, 5738410083847028739, 6366513764632138682, 8917742691602873917, },
                new long[] { 6002742438074216556, 3829061393691572575, 744641794153692247, 6820321788033063920, 8131999555790714861, 4937598931626669579, 698276939227751781, 8960552385858542031, 5073062739307116183, 7122497719736144401, },
                new long[] { 3226390197944199201, 2665966780768112101, 1885719374323454674, 4797763434951026879, 5364726878938609542, 1432711403514933790, 1923787209344107304, 2959322651019473819, 3779620510077213044, 5327261543966631397, },
                new long[] { 450046753911398359, 1502889760032793195, 3026814546677164365, 2775213877957817742, 2597454202086504224, 7151195912260070961, 3149306275553485035, 6181473749126106472, 2486187076938234961, 3532016572099901881, },
            };

            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                Random r = derived ? new SubRandom(seed) : new Random(seed);
                for (int i = 0; i < expectedValues[seed].Length; i++)
                {
                    Assert.Equal(expectedValues[seed][i], r.NextInt64());
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExpectedValues_NextBytes(bool derived)
        {
            byte[][] expectedValues = new byte[][]
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

            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                byte[] actualValues = new byte[expectedValues[seed].Length];
                Random r = derived ? new SubRandom(seed) : new Random(seed);

                r.NextBytes(actualValues);
                AssertExtensions.SequenceEqual(expectedValues[seed], actualValues);
            }

            for (int seed = 0; seed < expectedValues.Length; seed++)
            {
                byte[] actualValues = new byte[expectedValues[seed].Length];
                Random r = derived ? new SubRandom(seed) : new Random(seed);

                r.NextBytes((Span<byte>)actualValues);
                AssertExtensions.SequenceEqual(expectedValues[seed], actualValues);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Sample(bool seeded)
        {
            SubRandom r = seeded ? new SubRandom(42) : new SubRandom();
            for (int i = 0; i < 1000; i++)
            {
                double d = r.ExposeSample();
                Assert.True(d >= 0.0 && d < 1.0);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SampleOrNext_DerivedOverrideCalledWhereExpected(bool seeded)
        {
            // Validate our test Called state starts as false
            SubRandom r = seeded ? new SubRandom(42) : new SubRandom();
            Assert.False(r.SampleCalled);
            Assert.False(r.NextCalled);

            // Validate the right Called is true where expected

            foreach (int maxValue in new[] { 0, 1, 42 })
            {
                r = seeded ? new SubRandom(42) : new SubRandom();
                r.Next(maxValue);
                Assert.True(r.SampleCalled);
            }

            foreach ((int minValue, int maxValue) in new[] { (0, 0), (0, 1), (42, 47) })
            {
                r = seeded ? new SubRandom(42) : new SubRandom();
                r.Next(minValue, maxValue);
                Assert.True(r.SampleCalled);
            }

            foreach (long maxValue in new[] { 42L, (long)int.MaxValue + 1 })
            {
                r = seeded ? new SubRandom(42) : new SubRandom();
                r.NextInt64(maxValue);
                Assert.True(r.SampleCalled);
            }

            foreach ((long minValue, long maxValue) in new[] { (42L, 47L), ((long)int.MaxValue + 1, long.MaxValue) })
            {
                r = seeded ? new SubRandom(42) : new SubRandom();
                r.NextInt64(minValue, maxValue);
                Assert.True(r.SampleCalled);
            }

            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextSingle();
            Assert.True(r.SampleCalled);

            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextDouble();
            Assert.True(r.SampleCalled);

            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextBytes((Span<byte>)new byte[1]);
            Assert.True(r.NextCalled);

            // Next was changed to not call Sample in .NET Framework 2.0.
            // NextBytes((Span<byte>)) just uses Next().
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.Next();
            r.NextBytes((Span<byte>)new byte[1]);
            Assert.False(r.SampleCalled);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Empty_Success(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            r.NextBytes(new byte[0]);
            r.NextBytes(Span<byte>.Empty);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void Shared_IsSingleton()
        {
            Assert.NotNull(Random.Shared);
            Assert.Same(Random.Shared, Random.Shared);
            Assert.Same(Random.Shared, Task.Run(() => Random.Shared).Result);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void Shared_ParallelUsage()
        {
            using var barrier = new Barrier(2);
            Parallel.For(0, 2, _ =>
            {
                byte[] buffer = new byte[1000];

                barrier.SignalAndWait();
                for (int i = 0; i < 1_000; i++)
                {
                    Assert.InRange(Random.Shared.Next(), 0, int.MaxValue - 1);
                    Assert.InRange(Random.Shared.Next(5), 0, 4);
                    Assert.InRange(Random.Shared.Next(42, 50), 42, 49);

                    Assert.InRange(Random.Shared.NextInt64(), 0, long.MaxValue - 1);
                    Assert.InRange(Random.Shared.NextInt64(5), 0L, 5L);
                    Assert.InRange(Random.Shared.NextInt64(42L, 50L), 42L, 49L);

                    Assert.InRange(Random.Shared.NextSingle(), 0.0f, 1.0f);
                    Assert.InRange(Random.Shared.NextDouble(), 0.0, 1.0);

                    Array.Clear(buffer);
                    Random.Shared.NextBytes(buffer);
                    Assert.Contains(buffer, b => b != 0);

                    Array.Clear(buffer);
                    Random.Shared.NextBytes((Span<byte>)buffer);
                    Assert.Contains(buffer, b => b != 0);
                }
            });
        }

        [ConditionalFact(typeof(BitConverter), nameof(BitConverter.IsLittleEndian))] // test makes little-endian assumptions
        public void Xoshiro_AlgorithmBehavesAsExpected()
        {
            // This test is validating implementation detail.  If the algorithm used by `new Random()` is ever
            // updated, this test will need to be updated as well.

            // One and only one of Xoshiro128StarStar and Xoshiro256StarStar should be in a given build.
            Type implType = typeof(Random)
                .GetNestedTypes(BindingFlags.NonPublic)
                .Single(t => t.Name.StartsWith("Xoshiro", StringComparison.Ordinal));
            Assert.NotNull(implType);

            var randOuter = new Random();
            object randInner = randOuter.GetType().GetField("_impl", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(randOuter);
            Assert.NotNull(randInner);

            Type t = randInner.GetType();
            FieldInfo s0 = t.GetField("_s0", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo s1 = t.GetField("_s1", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo s2 = t.GetField("_s2", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo s3 = t.GetField("_s3", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(s0);
            Assert.NotNull(s1);
            Assert.NotNull(s2);
            Assert.NotNull(s3);

            if (IntPtr.Size == 8)
            {
                // Example seeds from https://www.pcg-random.org/posts/a-quick-look-at-xoshiro256.html
                s0.SetValue(randInner, 0x01d353e5f3993bb0ul);
                s1.SetValue(randInner, 0x7b9c0df6cb193b20ul);
                s2.SetValue(randInner, 0xfdfcaa91110765b6ul);
                s3.SetValue(randInner, 0xd2db341f10bb232eul);

                var buffer = new byte[256];
                randOuter.NextBytes(buffer);

                Assert.Contains("xoshiro256** by Blackman & Vigna", Encoding.ASCII.GetString(buffer));
                AssertExtensions.SequenceEqual(new byte[]
                {
                    0xdd, 0x51, 0xb2, 0xb7, 0xd9, 0x30, 0x3a, 0x37, 0xeb, 0xd9, 0x63, 0x66, 0xa6, 0x70, 0xfd, 0x50,
                    0x26, 0xe7, 0x29, 0x1f, 0x21, 0x21, 0xc0, 0x35, 0x36, 0xc1, 0x2d, 0x03, 0x77, 0xb1, 0x41, 0xd3,
                    0x43, 0x33, 0x2f, 0x77, 0xf7, 0xfe, 0x97, 0x01, 0x1e, 0x93, 0xc3, 0xce, 0xe4, 0xdf, 0xfc, 0xc4,
                    0xdb, 0x6c, 0x06, 0x54, 0x08, 0x25, 0x6f, 0x5a, 0x0e, 0x86, 0x82, 0x4d, 0x1c, 0x72, 0xc9, 0x50,
                    0x20, 0xae, 0xca, 0x84, 0xd9, 0x24, 0x87, 0xb9, 0x51, 0x96, 0x93, 0xae, 0xae, 0xd2, 0x8f, 0xce,
                    0x57, 0x37, 0xc1, 0x5c, 0xf4, 0xcc, 0x5c, 0xd6, 0x2a, 0x29, 0x72, 0xcb, 0xf0, 0xc5, 0xf8, 0xf8,
                    0x46, 0x1e, 0x33, 0xa2, 0x5d, 0xb1, 0x66, 0xb4, 0x15, 0x6f, 0x3b, 0xed, 0x93, 0xe4, 0x70, 0xba,
                    0x11, 0xbe, 0x24, 0xb0, 0x20, 0x64, 0x13, 0x86, 0x71, 0x72, 0x92, 0x31, 0xd8, 0xbe, 0x03, 0xa9,
                    0x78, 0x6f, 0x73, 0x68, 0x69, 0x72, 0x6f, 0x32, 0x35, 0x36, 0x2a, 0x2a, 0x20, 0x62, 0x79, 0x20,
                    0x42, 0x6c, 0x61, 0x63, 0x6b, 0x6d, 0x61, 0x6e, 0x20, 0x26, 0x20, 0x56, 0x69, 0x67, 0x6e, 0x61,
                    0xbd, 0x9a, 0xf9, 0xbd, 0x3a, 0x79, 0x52, 0xd3, 0x76, 0x50, 0x5e, 0x1e, 0x55, 0x6a, 0x36, 0x48,
                    0x9f, 0xc0, 0x39, 0xc2, 0x5c, 0xdb, 0x99, 0xa3, 0x5c, 0xd5, 0x4b, 0xa2, 0x15, 0x35, 0x53, 0x9c,
                    0xda, 0xdd, 0xc6, 0x0b, 0xbf, 0x33, 0xef, 0xa7, 0x82, 0xeb, 0x06, 0x52, 0x6d, 0x6d, 0x31, 0x2b,
                    0x24, 0x7a, 0x0c, 0x3f, 0x70, 0x43, 0xd1, 0x6f, 0xaa, 0xc6, 0x88, 0x7e, 0xf9, 0x30, 0xee, 0xff,
                    0x22, 0x31, 0xaf, 0xc6, 0x1f, 0xe5, 0x68, 0x22, 0xe9, 0x6e, 0x30, 0x06, 0xf6, 0x7f, 0x9a, 0x6e,
                    0xbe, 0x19, 0x0c, 0xf7, 0xae, 0xe2, 0xfa, 0xec, 0x8e, 0xc6, 0x22, 0xe1, 0x78, 0xb6, 0x39, 0xd1,
                }, buffer);

                Assert.Equal(50101881, randOuter.Next());
                Assert.Equal(1272175254, randOuter.Next());
                Assert.Equal(0, randOuter.Next(0));
                Assert.Equal(0, randOuter.Next(1));

                Assert.Equal(36, randOuter.Next(42));
                Assert.Equal(414373255, randOuter.Next(int.MaxValue));

                Assert.Equal(0, randOuter.Next(0, 0));
                Assert.Equal(1, randOuter.Next(1, 2));
                Assert.Equal(8, randOuter.Next(0, 42));
                Assert.Equal(4903, randOuter.Next(42, 12345));
                Assert.Equal(2147483643, randOuter.Next(int.MaxValue - 5, int.MaxValue));
                Assert.Equal(241160533, randOuter.Next(int.MinValue, int.MaxValue));

                Assert.Equal(7986543274318426717, randOuter.NextInt64());
                Assert.Equal(2184762751940478242, randOuter.NextInt64());

                Assert.Equal(0, randOuter.NextInt64(0));
                Assert.Equal(0, randOuter.NextInt64(1));
                Assert.Equal(8, randOuter.NextInt64(42));
                Assert.Equal(4799330244130288536, randOuter.NextInt64(long.MaxValue));

                Assert.Equal(0, randOuter.NextInt64(0, 0));
                Assert.Equal(1, randOuter.NextInt64(1, 2));
                Assert.Equal(29, randOuter.NextInt64(0, 42));
                Assert.Equal(9575, randOuter.NextInt64(42, 12345));
                Assert.Equal(9223372036854775802, randOuter.NextInt64(long.MaxValue - 5, long.MaxValue));
                Assert.Equal(-8248911992647668710, randOuter.NextInt64(long.MinValue, long.MaxValue));

                Assert.Equal(0.4319359955262648, randOuter.NextDouble());
                Assert.Equal(0.00939284326802925, randOuter.NextDouble());
                Assert.Equal(0.4631264615107299, randOuter.NextDouble());

                Assert.Equal(0.33326554f, randOuter.NextSingle());
                Assert.Equal(0.85681933f, randOuter.NextSingle());
                Assert.Equal(0.6594592f, randOuter.NextSingle());
            }
            else
            {
                s0.SetValue(randInner, 0x01d353e5u);
                s1.SetValue(randInner, 0x7b9c0df6u);
                s2.SetValue(randInner, 0xfdfcaa91u);
                s3.SetValue(randInner, 0xd2db341fu);

                var buffer = new byte[128];
                randOuter.NextBytes(buffer);
                AssertExtensions.SequenceEqual(new byte[]
                {
                    0xDD, 0x20, 0x3A, 0x37, 0xEB, 0x6F, 0xFD, 0x50, 0xA3, 0x7B, 0xCD, 0x37, 0xA8, 0xAA, 0x19, 0xA8,
                    0x22, 0xD6, 0x21, 0x57, 0x55, 0xF3, 0xA2, 0x56, 0x73, 0x30, 0x61, 0xDE, 0x62, 0xD8, 0x02, 0xB9,
                    0x5C, 0xAE, 0x3E, 0x2D, 0xC8, 0xD6, 0xBF, 0x7D, 0x6D, 0x86, 0xCE, 0x95, 0x3F, 0x7C, 0xF0, 0x86,
                    0x36, 0x26, 0xB8, 0xA7, 0x5C, 0x80, 0xC8, 0xA7, 0xAC, 0x2C, 0xE6, 0x0E, 0x25, 0x6F, 0xEB, 0x04,
                    0x22, 0xDE, 0xB4, 0xB6, 0x48, 0xB2, 0x07, 0x79, 0x09, 0xA8, 0xF6, 0x42, 0xA8, 0x5C, 0x3F, 0xCE,
                    0x11, 0xE9, 0x91, 0x8B, 0x17, 0x48, 0x0B, 0xE1, 0xEB, 0x0A, 0x89, 0xC1, 0x64, 0x3B, 0x58, 0x76,
                    0x30, 0x53, 0x67, 0x13, 0x68, 0xAC, 0xF3, 0x5D, 0x1B, 0x84, 0xF5, 0x88, 0x42, 0xC7, 0x45, 0x74,
                    0x65, 0xB5, 0x11, 0xF2, 0x0D, 0x3F, 0x62, 0xC8, 0x5C, 0x7C, 0x1C, 0x35, 0x34, 0x2D, 0xBC, 0x9E,
                }, buffer);

                Assert.Equal(1539844677, randOuter.Next());
                Assert.Equal(1451010027, randOuter.Next());
                Assert.Equal(0, randOuter.Next(0));
                Assert.Equal(0, randOuter.Next(1));

                Assert.Equal(23, randOuter.Next(42));
                Assert.Equal(1109044164, randOuter.Next(int.MaxValue));

                Assert.Equal(0, randOuter.Next(0, 0));
                Assert.Equal(1, randOuter.Next(1, 2));
                Assert.Equal(2, randOuter.Next(0, 42));
                Assert.Equal(528, randOuter.Next(42, 12345));
                Assert.Equal(2147483643, randOuter.Next(int.MaxValue - 5, int.MaxValue));
                Assert.Equal(-246770113, randOuter.Next(int.MinValue, int.MaxValue));

                Assert.Equal(7961633792735929777, randOuter.NextInt64());
                Assert.Equal(1188783949680720902, randOuter.NextInt64());

                Assert.Equal(0, randOuter.NextInt64(0));
                Assert.Equal(0, randOuter.NextInt64(1));
                Assert.Equal(1, randOuter.NextInt64(42));
                Assert.Equal(3659990215800279771, randOuter.NextInt64(long.MaxValue));

                Assert.Equal(0, randOuter.NextInt64(0, 0));
                Assert.Equal(1, randOuter.NextInt64(1, 2));
                Assert.Equal(5, randOuter.NextInt64(0, 42));
                Assert.Equal(9391, randOuter.NextInt64(42, 12345));
                Assert.Equal(9223372036854775805, randOuter.NextInt64(long.MaxValue - 5, long.MaxValue));
                Assert.Equal(7588547406678852723, randOuter.NextInt64(long.MinValue, long.MaxValue));

                Assert.Equal(0.3010761548802774, randOuter.NextDouble());
                Assert.Equal(0.5866389350236931, randOuter.NextDouble());
                Assert.Equal(0.4726054469222304, randOuter.NextDouble());

                Assert.Equal(0.35996222f, randOuter.NextSingle());
                Assert.Equal(0.929421f, randOuter.NextSingle());
                Assert.Equal(0.5790618f, randOuter.NextSingle());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Shuffle_Array_Seeded(bool emptyShuffle)
        {
            Random random = new Random(0x70636A61);
            int[] items = new int[] { 1, 2, 3, 4 };
            random.Shuffle(items);
            AssertExtensions.SequenceEqual(stackalloc int[] { 4, 2, 1, 3 }, items.AsSpan());
            random.Shuffle(items);
            AssertExtensions.SequenceEqual(stackalloc int[] { 2, 3, 4, 1 }, items.AsSpan());

            if (emptyShuffle)
            {
                // Empty shuffle should have no observable effect.
                random.Shuffle(Span<int>.Empty);
            }

            random.Shuffle(items);
            AssertExtensions.SequenceEqual(stackalloc int[] { 1, 4, 3, 2 }, items.AsSpan());
        }

        [Fact]
        public static void Shuffle_Array_Covariance()
        {
            Random random = new Random(0x70636A61);
			string[] items = ["", ""];
			object[] array = items;
            random.Shuffle(array);
            AssertExtensions.SequenceEqual((ReadOnlySpan<string>)["", ""], items);
        }

        [Fact]
        public static void Shuffle_Array_ArgValidation()
        {
            Random random = new Random(0x70636A61);
            AssertExtensions.Throws<ArgumentNullException>("values", () => random.Shuffle((int[])null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Shuffle_Span_Seeded(bool emptyShuffle)
        {
            Random random = new Random(0x70636A61);
            Span<int> items = new int[] { 1, 2, 3, 4 };
            random.Shuffle(items);
            AssertExtensions.SequenceEqual(stackalloc int[] { 4, 2, 1, 3 }, items);
            random.Shuffle(items);
            AssertExtensions.SequenceEqual(stackalloc int[] { 2, 3, 4, 1 }, items);

            if (emptyShuffle)
            {
                // Empty shuffle should have no observable effect.
                random.Shuffle(Array.Empty<int>());
            }

            random.Shuffle(items);
            AssertExtensions.SequenceEqual(stackalloc int[] { 1, 4, 3, 2 }, items);
        }

        [Fact]
        public static void GetItems_Span_ArgValidation()
        {
            Random random = new Random(0x70636A61);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => random.GetItems<int>(stackalloc int[1], length: -1));
            AssertExtensions.Throws<ArgumentException>("choices", () => random.GetItems<int>(ReadOnlySpan<int>.Empty, length: 1));
        }

        [Fact]
        public static void GetItems_Array_Allocating_ArgValidation()
        {
            Random random = new Random(0x70636A61);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => random.GetItems(new int[1], length: -1));
            AssertExtensions.Throws<ArgumentNullException>("choices", () => random.GetItems((int[])null, length: 1));
            AssertExtensions.Throws<ArgumentException>("choices", () => random.GetItems<int>(Array.Empty<int>(), length: 1));
        }

        [Fact]
        public static void GetItems_Buffer_ArgValidation()
        {
            Random random = new Random(0x70636A61);
            int[] destination = new int[1];
            AssertExtensions.Throws<ArgumentException>("choices", () => random.GetItems<int>(ReadOnlySpan<int>.Empty, destination));
        }

        [Fact]
        public static void GetItems_Allocating_Array_Seeded_NonPower2()
        {
            Random random = new Random(0x70636A61);
            byte[] items = new byte[] { 1, 2, 3 };

            byte[] result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 3, 1, 3, 2, 3, 3, 3 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 2, 1, 2, 1, 2, 3, 1 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 1, 1, 3, 1, 3, 2, 2 }, result);
        }

        [Fact]
        public static void GetItems_Allocating_Span_Seeded_NonPower2()
        {
            Random random = new Random(0x70636A61);
            ReadOnlySpan<byte> items = new byte[] { 1, 2, 3 };

            byte[] result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 3, 1, 3, 2, 3, 3, 3 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 2, 1, 2, 1, 2, 3, 1 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 1, 1, 3, 1, 3, 2, 2 }, result);
        }

        [Fact]
        public static void GetItems_Buffer_Seeded_NonPower2()
        {
            Random random = new Random(0x70636A61);
            ReadOnlySpan<byte> items = new byte[] { 1, 2, 3 };

            Span<byte> buffer = stackalloc byte[7];
            random.GetItems(items, buffer);
            AssertExtensions.SequenceEqual(new byte[] { 3, 1, 3, 2, 3, 3, 3 }.AsSpan(), buffer);

            random.GetItems(items, buffer);
            AssertExtensions.SequenceEqual(new byte[] { 2, 1, 2, 1, 2, 3, 1 }.AsSpan(), buffer);

            random.GetItems(items, buffer);
            AssertExtensions.SequenceEqual(new byte[] { 1, 1, 3, 1, 3, 2, 2 }.AsSpan(), buffer);
        }

        [Fact]
        public static void GetItems_Allocating_Array_Seeded_Power2()
        {
            Random random = new Random(0x70636A61);
            byte[] items = new byte[] { 1, 2, 3, 4 };

            byte[] result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 4, 1, 4, 2, 4, 4, 4 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 2, 2, 3, 1, 3, 3, 1 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 2, 1, 4, 2, 4, 2, 2 }, result);
        }

        [Fact]
        public static void GetItems_Allocating_Span_Seeded_Power2()
        {
            Random random = new Random(0x70636A61);
            ReadOnlySpan<byte> items = new byte[] { 1, 2, 3, 4 };

            byte[] result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 4, 1, 4, 2, 4, 4, 4 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 2, 2, 3, 1, 3, 3, 1 }, result);

            result = random.GetItems(items, length: 7);
            Assert.Equal(new byte[] { 2, 1, 4, 2, 4, 2, 2 }, result);
        }

        [Fact]
        public static void GetItems_Buffer_Seeded_Power2()
        {
            Random random = new Random(0x70636A61);
            ReadOnlySpan<byte> items = new byte[] { 1, 2, 3, 4 };

            Span<byte> buffer = stackalloc byte[7];
            random.GetItems(items, buffer);
            AssertExtensions.SequenceEqual(new byte[] { 4, 1, 4, 2, 4, 4, 4 }.AsSpan(), buffer);

            random.GetItems(items, buffer);
            AssertExtensions.SequenceEqual(new byte[] { 2, 2, 3, 1, 3, 3, 1 }.AsSpan(), buffer);

            random.GetItems(items, buffer);
            AssertExtensions.SequenceEqual(new byte[] { 2, 1, 4, 2, 4, 2, 2 }.AsSpan(), buffer);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public static void GetItems_AllValuesInRange(int mode)
        {
            Random random = mode switch
            {
                0 => new Random(),
                1 => new Random(42),
                2 => new SubRandom(),
                3 => new SubRandom(42),
                _ => Random.Shared,
            };

            foreach (int numItems in Enumerable.Range(1, 8).Append(300))
            {
                int[] items = Enumerable.Range(42, numItems).ToArray();
                for (int length = 1; length <= 16; length++)
                {
                    int[] result = random.GetItems(items, length: length);
                    Assert.All(result, b => Assert.InRange(b, 42, 42 + numItems - 1));

                    result = random.GetItems((ReadOnlySpan<int>)items, length: length);
                    Assert.All(result, b => Assert.InRange(b, 42, 42 + numItems - 1));

                    Array.Clear(result);
                    random.GetItems(items, (Span<int>)result);
                    Assert.All(result, b => Assert.InRange(b, 42, 42 + numItems - 1));
                }
            }
        }

        [Fact]
        public static void GetString_ArgValidation()
        {
            Random random = new();
            AssertExtensions.Throws<ArgumentException>("choices", () => random.GetString([], 42));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => random.GetString(['a'], -1));
        }

        [Fact]
        public static void GetHexString_Array_ArgValidation()
        {
            Random random = new();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => random.GetHexString(-1, true));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => random.GetHexString(-2, false));
        }

        [Fact]
        public static void GetString_ProducesExpectedStrings()
        {
            Random random = new Random(42);
            Assert.Equal("", random.GetString("abcdefghijklmnopqrstuvwxyz", 0));
            Assert.Equal("c", random.GetString("abcd", 1));
            Assert.Equal("aaca", random.GetString("abcde", 4));
            Assert.Equal("gsnetggnijgnavpkdcsvobsdxsnebi", random.GetString("abcdefghijklmnopqrstuvwxyz", 30));
        }

        [Fact]
        public static void GetHexString_Array_ProducesExpectedStrings()
        {
            Random random = new Random(42);

            Assert.Equal("", random.GetHexString(0));
            Assert.Equal("A", random.GetHexString(1));
            Assert.Equal("2282", random.GetHexString(4));
            Assert.Equal("4B82C34856480D9621BD80B2EB8204", random.GetHexString(30));

            Assert.Equal("", random.GetHexString(0, false));
            Assert.Equal("9", random.GetHexString(1, false));
            Assert.Equal("D08C", random.GetHexString(4, false));
            Assert.Equal("B6200CA13C4A209806BA30541B170C", random.GetHexString(30, false));

            Assert.Equal("", random.GetHexString(0, true));
            Assert.Equal("7", random.GetHexString(1, true));
            Assert.Equal("870d", random.GetHexString(4, true));
            Assert.Equal("001a0ee7690526c864e9b0ef5b2175", random.GetHexString(30, true));
        }

        [Fact]
        public static void GetHexString_Span_ProducesExpectedItems()
        {
            Random random = new Random(42);

            char[] dest;

            dest = [];
            random.GetHexString(dest);

            var tests = new (int Length, string Expected)[]
            {
                (0, ""),
                (1, "A"),
                (4, "2282"),
                (30, "4B82C34856480D9621BD80B2EB8204"),
            };

            foreach (var test in tests)
            {
                dest = new char[test.Length];
                random.GetHexString(dest);
                Assert.Equal(test.Expected, new string(dest));
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_InvalidArguments_Throws(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            // Negative maxValue throws for all signed types.
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<sbyte>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<short>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<int>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<long>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<nint>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<Int128>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<BigInteger>(-1));

            // minValue > maxValue throws.
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<int>(2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<long>(2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<byte>((byte)5, (byte)3));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<uint>(10u, 5u));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<Int128>((Int128)10, (Int128)5));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInteger<BigInteger>(10, 5));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_ZeroMaxValue_ReturnsZero(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTZeroMaxValue<byte>(r);
            AssertNextIntegerTZeroMaxValue<sbyte>(r);
            AssertNextIntegerTZeroMaxValue<short>(r);
            AssertNextIntegerTZeroMaxValue<ushort>(r);
            AssertNextIntegerTZeroMaxValue<char>(r);
            AssertNextIntegerTZeroMaxValue<int>(r);
            AssertNextIntegerTZeroMaxValue<uint>(r);
            AssertNextIntegerTZeroMaxValue<long>(r);
            AssertNextIntegerTZeroMaxValue<ulong>(r);
            AssertNextIntegerTZeroMaxValue<nint>(r);
            AssertNextIntegerTZeroMaxValue<nuint>(r);
            AssertNextIntegerTZeroMaxValue<Int128>(r);
            AssertNextIntegerTZeroMaxValue<UInt128>(r);

            static void AssertNextIntegerTZeroMaxValue<T>(Random r) where T : IBinaryInteger<T>
            {
                Assert.Equal(T.Zero, r.NextInteger<T>(T.Zero));
                Assert.Equal(T.Zero, r.NextInteger<T>(T.Zero, T.Zero));
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_EqualMinMax_ReturnsMinValue(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTEqualMinMax<byte>(r, (byte)42);
            AssertNextIntegerTEqualMinMax<sbyte>(r, (sbyte)-10);
            AssertNextIntegerTEqualMinMax<short>(r, (short)1000);
            AssertNextIntegerTEqualMinMax<ushort>(r, (ushort)500);
            AssertNextIntegerTEqualMinMax<int>(r, 12345);
            AssertNextIntegerTEqualMinMax<uint>(r, 99u);
            AssertNextIntegerTEqualMinMax<long>(r, -42L);
            AssertNextIntegerTEqualMinMax<ulong>(r, 100UL);
            AssertNextIntegerTEqualMinMax<nint>(r, (nint)7);
            AssertNextIntegerTEqualMinMax<nuint>(r, (nuint)7);
            AssertNextIntegerTEqualMinMax<Int128>(r, (Int128)(-77));
            AssertNextIntegerTEqualMinMax<UInt128>(r, (UInt128)200);

            static void AssertNextIntegerTEqualMinMax<T>(Random r, T value) where T : IBinaryInteger<T>
            {
                Assert.Equal(value, r.NextInteger<T>(value, value));
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_SingleElementRange_ReturnsMinValue(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTSingleElement<byte>(r, (byte)5, (byte)6);
            AssertNextIntegerTSingleElement<sbyte>(r, (sbyte)-3, (sbyte)-2);
            AssertNextIntegerTSingleElement<short>(r, (short)100, (short)101);
            AssertNextIntegerTSingleElement<ushort>(r, (ushort)200, (ushort)201);
            AssertNextIntegerTSingleElement<int>(r, 42, 43);
            AssertNextIntegerTSingleElement<uint>(r, 42u, 43u);
            AssertNextIntegerTSingleElement<long>(r, -1L, 0L);
            AssertNextIntegerTSingleElement<ulong>(r, 99UL, 100UL);
            AssertNextIntegerTSingleElement<nint>(r, (nint)10, (nint)11);
            AssertNextIntegerTSingleElement<nuint>(r, (nuint)10, (nuint)11);
            AssertNextIntegerTSingleElement<Int128>(r, (Int128)(-1), (Int128)0);
            AssertNextIntegerTSingleElement<UInt128>(r, (UInt128)50, (UInt128)51);

            static void AssertNextIntegerTSingleElement<T>(Random r, T min, T max) where T : IBinaryInteger<T>
            {
                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(min, r.NextInteger<T>(min, max));
                }
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_AllBuiltInTypes_MaxValueInRange(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTMaxValueInRange<byte>(r, (byte)100);
            AssertNextIntegerTMaxValueInRange<sbyte>(r, (sbyte)50);
            AssertNextIntegerTMaxValueInRange<short>(r, (short)500);
            AssertNextIntegerTMaxValueInRange<ushort>(r, (ushort)500);
            AssertNextIntegerTMaxValueInRange<char>(r, (char)100);
            AssertNextIntegerTMaxValueInRange<int>(r, 1000);
            AssertNextIntegerTMaxValueInRange<uint>(r, 1000u);
            AssertNextIntegerTMaxValueInRange<long>(r, 1000L);
            AssertNextIntegerTMaxValueInRange<ulong>(r, 1000UL);
            AssertNextIntegerTMaxValueInRange<nint>(r, (nint)1000);
            AssertNextIntegerTMaxValueInRange<nuint>(r, (nuint)1000);
            AssertNextIntegerTMaxValueInRange<Int128>(r, (Int128)1000);
            AssertNextIntegerTMaxValueInRange<UInt128>(r, (UInt128)1000);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_AllBuiltInTypes_MinMaxInRange(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTMinMaxInRange<byte>(r, (byte)10, (byte)200);
            AssertNextIntegerTMinMaxInRange<sbyte>(r, (sbyte)-50, (sbyte)50);
            AssertNextIntegerTMinMaxInRange<short>(r, (short)-500, (short)500);
            AssertNextIntegerTMinMaxInRange<ushort>(r, (ushort)100, (ushort)1000);
            AssertNextIntegerTMinMaxInRange<int>(r, -1000, 1000);
            AssertNextIntegerTMinMaxInRange<uint>(r, 50u, 500u);
            AssertNextIntegerTMinMaxInRange<long>(r, -100_000L, 100_000L);
            AssertNextIntegerTMinMaxInRange<ulong>(r, 100UL, 1000UL);
            AssertNextIntegerTMinMaxInRange<nint>(r, (nint)(-100), (nint)100);
            AssertNextIntegerTMinMaxInRange<nuint>(r, (nuint)10, (nuint)500);
            AssertNextIntegerTMinMaxInRange<Int128>(r, (Int128)(-1000), (Int128)1000);
            AssertNextIntegerTMinMaxInRange<UInt128>(r, (UInt128)50, (UInt128)500);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_AllValuesInSmallRangeHit(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            const int rangeSize = 5;
            AssertAllValuesHit<byte>(r, (byte)rangeSize);
            AssertAllValuesHit<sbyte>(r, (sbyte)rangeSize);
            AssertAllValuesHit<short>(r, (short)rangeSize);
            AssertAllValuesHit<ushort>(r, (ushort)rangeSize);
            AssertAllValuesHit<int>(r, rangeSize);
            AssertAllValuesHit<uint>(r, (uint)rangeSize);
            AssertAllValuesHit<long>(r, (long)rangeSize);
            AssertAllValuesHit<ulong>(r, (ulong)rangeSize);
            AssertAllValuesHit<nint>(r, (nint)rangeSize);
            AssertAllValuesHit<nuint>(r, (nuint)rangeSize);
            AssertAllValuesHit<Int128>(r, (Int128)rangeSize);
            AssertAllValuesHit<UInt128>(r, (UInt128)rangeSize);

            static void AssertAllValuesHit<T>(Random r, T maxExclusive) where T : IBinaryInteger<T>
            {
                HashSet<T> seen = [];
                for (int i = 0; i < 10_000; i++)
                {
                    seen.Add(r.NextInteger<T>(maxExclusive));
                }

                for (T v = T.Zero; v < maxExclusive; v++)
                {
                    Assert.Contains(v, seen);
                }

                Assert.DoesNotContain(maxExclusive, seen);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_Parameterless_AllTypes(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTParameterless<byte>(r);
            AssertNextIntegerTParameterless<sbyte>(r);
            AssertNextIntegerTParameterless<short>(r);
            AssertNextIntegerTParameterless<ushort>(r);
            AssertNextIntegerTParameterless<char>(r);
            AssertNextIntegerTParameterless<int>(r);
            AssertNextIntegerTParameterless<uint>(r);
            AssertNextIntegerTParameterless<long>(r);
            AssertNextIntegerTParameterless<ulong>(r);
            AssertNextIntegerTParameterless<nint>(r);
            AssertNextIntegerTParameterless<nuint>(r);
            AssertNextIntegerTParameterless<Int128>(r);
            AssertNextIntegerTParameterless<UInt128>(r);

            static void AssertNextIntegerTParameterless<T>(Random r) where T : IBinaryInteger<T>, IMinMaxValue<T>
            {
                for (int i = 0; i < 100; i++)
                {
                    T value = r.NextInteger<T>();
                    Assert.True(value >= T.Zero, $"NextInteger<{typeof(T).Name}>() returned negative value: {value}");
                    Assert.True(value <= T.MaxValue, $"NextInteger<{typeof(T).Name}>() returned a value greater than MaxValue: {value}");
                }
            }
        }

        [Fact]
        public void NextIntegerT_Parameterless_CanReturnMaxValue()
        {
            Random r = new MaxValueRandom();
            Assert.Equal(byte.MaxValue, r.NextInteger<byte>());
            Assert.Equal(sbyte.MaxValue, r.NextInteger<sbyte>());
            Assert.Equal(short.MaxValue, r.NextInteger<short>());
            Assert.Equal(ushort.MaxValue, r.NextInteger<ushort>());
            Assert.Equal(char.MaxValue, r.NextInteger<char>());
            Assert.Equal(int.MaxValue, r.NextInteger<int>());
            Assert.Equal(uint.MaxValue, r.NextInteger<uint>());
            Assert.Equal(long.MaxValue, r.NextInteger<long>());
            Assert.Equal(ulong.MaxValue, r.NextInteger<ulong>());
            Assert.Equal(nint.MaxValue, r.NextInteger<nint>());
            Assert.Equal(nuint.MaxValue, r.NextInteger<nuint>());
            Assert.Equal(Int128.MaxValue, r.NextInteger<Int128>());
            Assert.Equal(UInt128.MaxValue, r.NextInteger<UInt128>());
        }

        public static IEnumerable<object[]> NextIntegerT_SignedOverflowRange_MemberData() =>
            from derived in new[] { false, true }
            from seeded in new[] { false, true }
            select new object[] { derived, seeded };

        [Theory]
        [MemberData(nameof(NextIntegerT_SignedOverflowRange_MemberData))]
        public void NextIntegerT_SignedOverflow_FullRange(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            // These ranges exceed T.MaxValue, triggering the NextBinaryIntegerFullRange path.
            AssertNextIntegerTMinMaxInRange<sbyte>(r, sbyte.MinValue, sbyte.MaxValue);
            AssertNextIntegerTMinMaxInRange<short>(r, short.MinValue, short.MaxValue);
            AssertNextIntegerTMinMaxInRange<int>(r, int.MinValue, int.MaxValue);
            AssertNextIntegerTMinMaxInRange<long>(r, long.MinValue, long.MaxValue);
            AssertNextIntegerTMinMaxInRange<nint>(r, nint.MinValue, nint.MaxValue);
            AssertNextIntegerTMinMaxInRange<Int128>(r, Int128.MinValue, Int128.MaxValue);

            // Ranges that cross zero with large span.
            AssertNextIntegerTMinMaxInRange<int>(r, int.MinValue, 0);
            AssertNextIntegerTMinMaxInRange<int>(r, -1, int.MaxValue);
            AssertNextIntegerTMinMaxInRange<long>(r, long.MinValue, 0L);
            AssertNextIntegerTMinMaxInRange<long>(r, -1L, long.MaxValue);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_LargeUnsignedValues(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            // ulong values beyond long.MaxValue.
            for (int i = 0; i < 100; i++)
            {
                ulong value = r.NextInteger<ulong>(ulong.MaxValue);
                Assert.True(value < ulong.MaxValue);
            }

            // Full uint range.
            AssertNextIntegerTMaxValueInRange<uint>(r, uint.MaxValue);

            // UInt128 large range.
            AssertNextIntegerTMinMaxInRange<UInt128>(r, (UInt128)0, UInt128.MaxValue);

            // nuint on the current platform.
            AssertNextIntegerTMaxValueInRange<nuint>(r, nuint.MaxValue);

            // Int128/UInt128 values that exceed ulong.MaxValue - these must bypass the
            // ulong fast path and use rejection sampling.
            UInt128 largeUInt128Max = ((UInt128)ulong.MaxValue << 1) + 5;
            AssertNextIntegerTMaxValueInRange<UInt128>(r, largeUInt128Max);

            Int128 largeInt128Max = (Int128)ulong.MaxValue + 100;
            AssertNextIntegerTMaxValueInRange<Int128>(r, largeInt128Max);

            // UInt128 maxExclusive = 2^64 (exactly one more than ulong.MaxValue)
            // This previously truncated to 0 via ulong.CreateTruncating.
            UInt128 twoTo64 = (UInt128)ulong.MaxValue + 1;
            for (int i = 0; i < 100; i++)
            {
                UInt128 value = r.NextInteger<UInt128>(twoTo64);
                Assert.True(value < twoTo64, $"NextInteger<UInt128>({twoTo64}) returned {value}");
            }

            // Int128 maxExclusive that truncates to a small value when cast to ulong
            Int128 tricky = ((Int128)1 << 64) + 5;
            for (int i = 0; i < 100; i++)
            {
                Int128 value = r.NextInteger<Int128>(tricky);
                Assert.True(value >= Int128.Zero && value < tricky, $"NextInteger<Int128>({tricky}) returned {value}");
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_BigInteger_InRange(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            AssertNextIntegerTMaxValueInRange<BigInteger>(r, new BigInteger(1_000));
            AssertNextIntegerTMinMaxInRange<BigInteger>(r, new BigInteger(-1_000), new BigInteger(1_000));

            BigInteger largeMax = BigInteger.One << 3_000;
            AssertNextIntegerTMaxValueInRange<BigInteger>(r, largeMax, iterations: 10);
            AssertNextIntegerTMinMaxInRange<BigInteger>(r, -largeMax, largeMax, iterations: 10);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_NegativeRanges(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextIntegerTMinMaxInRange<sbyte>(r, (sbyte)-100, (sbyte)-10);
            AssertNextIntegerTMinMaxInRange<short>(r, (short)-1000, (short)-1);
            AssertNextIntegerTMinMaxInRange<int>(r, -1_000_000, -1);
            AssertNextIntegerTMinMaxInRange<long>(r, -1_000_000_000L, -1L);
            AssertNextIntegerTMinMaxInRange<Int128>(r, (Int128)(-1000), (Int128)(-1));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextBinaryFloatT_AllTypes_InRange(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextBinaryFloatInRange<Half>(r);
            AssertNextBinaryFloatInRange<BFloat16>(r);
            AssertNextBinaryFloatInRange<float>(r);
            AssertNextBinaryFloatInRange<double>(r);
            AssertNextBinaryFloatInRange<NFloat>(r);

            static void AssertNextBinaryFloatInRange<T>(Random r) where T : IBinaryFloatingPointIeee754<T>
            {
                for (int i = 0; i < 1000; i++)
                {
                    T value = r.NextBinaryFloat<T>();
                    Assert.True(value >= T.Zero, $"NextBinaryFloat<{typeof(T).Name}>() returned {value}, expected >= 0");
                    Assert.True(value < T.One, $"NextBinaryFloat<{typeof(T).Name}>() returned {value}, expected < 1");
                }
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextBinaryFloatT_ProducesVariedValues(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            AssertNextBinaryFloatVaried<Half>(r);
            AssertNextBinaryFloatVaried<BFloat16>(r);
            AssertNextBinaryFloatVaried<float>(r);
            AssertNextBinaryFloatVaried<double>(r);
            AssertNextBinaryFloatVaried<NFloat>(r);

            static void AssertNextBinaryFloatVaried<T>(Random r) where T : IBinaryFloatingPointIeee754<T>
            {
                HashSet<T> seen = [];
                for (int i = 0; i < 100; i++)
                {
                    seen.Add(r.NextBinaryFloat<T>());
                }

                Assert.True(seen.Count > 50, $"NextBinaryFloat<{typeof(T).Name}>() produced only {seen.Count} distinct values in 100 calls");
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextIntegerT_CustomReferenceType(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);

            AssertNextIntegerTMaxValueInRange<BinaryIntegerReference<int>>(r, 10);
            AssertNextIntegerTMinMaxInRange<BinaryIntegerReference<int>>(r, -5, 5);
        }

        [Fact]
        public void NextIntegerT_CustomReferenceType_ParameterlessCanReturnMaxValue()
        {
            Random r = new MaxValueRandom();

            Assert.Equal(BinaryIntegerReference<int>.MaxValue, r.NextInteger<BinaryIntegerReference<int>>());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NextBinaryFloatT_CustomReferenceType(bool derived, bool seeded)
        {
            Random r = Create(derived, seeded);
            HashSet<BinaryFloatingPointIeee754Reference<float>> seen = [];
            string typeName = nameof(BinaryFloatingPointIeee754Reference<float>);

            for (int i = 0; i < 100; i++)
            {
                BinaryFloatingPointIeee754Reference<float> value = r.NextBinaryFloat<BinaryFloatingPointIeee754Reference<float>>();
                Assert.True(value >= BinaryFloatingPointIeee754Reference<float>.Zero, $"NextBinaryFloat<{typeName}>() returned {value}, expected >= 0");
                Assert.True(value < BinaryFloatingPointIeee754Reference<float>.One, $"NextBinaryFloat<{typeName}>() returned {value}, expected < 1");
                seen.Add(value);
            }

            Assert.True(seen.Count > 50, $"NextBinaryFloat<{typeName}>() produced only {seen.Count} distinct values in 100 calls");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NextIntegerT_DerivedType_DispatchesThroughVirtuals(bool seeded)
        {
            // NextInteger<T>() routes through Next(int), NextInt64(long), or NextBytes(Span<byte>),
            // all of which are virtual. For a derived Random, these must reach the derived
            // type's overrides so that subclass behavior (e.g. custom RNG) is preserved.
            // SubRandom.Next() sets NextCalled; the compat path routes Next(int)/NextInt64/
            // NextSingle/NextDouble through Sample(), which sets SampleCalled.

            SubRandom r;

            // Integer types that hit Next(int) -> Sample()
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextInteger<int>(42);
            Assert.True(r.SampleCalled, "NextInteger<int> should dispatch through Sample on derived type");

            // Integer types that hit NextInt64(long) -> Sample()
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextInteger<long>(42L);
            Assert.True(r.SampleCalled, "NextInteger<long> should dispatch through Sample on derived type");

            // Large types that hit NextBytes -> Next() -> NextCalled
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextInteger<UInt128>(UInt128.MaxValue);
            Assert.True(r.NextCalled, "NextInteger<UInt128> should dispatch through Next on derived type");

            // NextBinaryFloat<float> -> NextSingle() -> Sample()
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextBinaryFloat<float>();
            Assert.True(r.SampleCalled, "NextBinaryFloat<float> should dispatch through Sample on derived type");

            // NextBinaryFloat<double> -> NextDouble() -> Sample()
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextBinaryFloat<double>();
            Assert.True(r.SampleCalled, "NextBinaryFloat<double> should dispatch through Sample on derived type");

            // NextBinaryFloat<Half> -> NextInt64() -> Sample()
            r = seeded ? new SubRandom(42) : new SubRandom();
            r.NextBinaryFloat<Half>();
            Assert.True(r.SampleCalled, "NextBinaryFloat<Half> should dispatch through Sample on derived type");
        }

        private static void AssertNextIntegerTMaxValueInRange<T>(Random r, T maxExclusive, int iterations = 100) where T : IBinaryInteger<T>
        {
            for (int i = 0; i < iterations; i++)
            {
                T value = r.NextInteger<T>(maxExclusive);
                Assert.True(!T.IsNegative(value), $"NextInteger<{typeof(T).Name}>({maxExclusive}) returned negative: {value}");
                Assert.True(value < maxExclusive, $"NextInteger<{typeof(T).Name}>({maxExclusive}) returned {value}, expected < {maxExclusive}");
            }
        }

        private static void AssertNextIntegerTMinMaxInRange<T>(Random r, T min, T max, int iterations = 100) where T : IBinaryInteger<T>
        {
            for (int i = 0; i < iterations; i++)
            {
                T value = r.NextInteger<T>(min, max);
                Assert.True(value >= min, $"NextInteger<{typeof(T).Name}>({min}, {max}) returned {value}, expected >= {min}");
                Assert.True(value < max, $"NextInteger<{typeof(T).Name}>({min}, {max}) returned {value}, expected < {max}");
            }
        }

        private static Random Create(bool derived, bool seeded) =>
            (derived, seeded) switch
            {
                (false, false) => new Random(),
                (false, true) => new Random(42),
                (true, false) => new SubRandom(),
                (true, true) => new SubRandom(42)
            };

        private class SubRandom : Random
        {
            public bool SampleCalled, NextCalled;

            public SubRandom() { }
            public SubRandom(int Seed) : base(Seed) { }

            public double ExposeSample() => Sample();

            protected override double Sample()
            {
                SampleCalled = true;
                return base.Sample();
            }

            public override int Next()
            {
                NextCalled = true;
                return base.Next();
            }
        }

        private sealed class BinaryIntegerReference<T> : IBinaryInteger<BinaryIntegerReference<T>>, IMinMaxValue<BinaryIntegerReference<T>>
            where T : IBinaryInteger<T>, IMinMaxValue<T>
        {
            public BinaryIntegerReference(T value) => Value = value;

            public T Value { get; }

            public static implicit operator BinaryIntegerReference<T>(T value) => new(value);
            public static implicit operator T(BinaryIntegerReference<T> value) => value.Value;

            public static BinaryIntegerReference<T> AdditiveIdentity => T.AdditiveIdentity;
            public static BinaryIntegerReference<T> MaxValue => T.MaxValue;
            public static BinaryIntegerReference<T> MinValue => T.MinValue;
            public static BinaryIntegerReference<T> MultiplicativeIdentity => T.MultiplicativeIdentity;
            public static BinaryIntegerReference<T> One => T.One;
            public static int Radix => T.Radix;
            public static BinaryIntegerReference<T> Zero => T.Zero;

            public static BinaryIntegerReference<T> Abs(BinaryIntegerReference<T> value) => T.Abs(value);
            public static bool IsCanonical(BinaryIntegerReference<T> value) => T.IsCanonical(value);
            public static bool IsComplexNumber(BinaryIntegerReference<T> value) => T.IsComplexNumber(value);
            public static bool IsEvenInteger(BinaryIntegerReference<T> value) => T.IsEvenInteger(value);
            public static bool IsFinite(BinaryIntegerReference<T> value) => T.IsFinite(value);
            public static bool IsImaginaryNumber(BinaryIntegerReference<T> value) => T.IsImaginaryNumber(value);
            public static bool IsInfinity(BinaryIntegerReference<T> value) => T.IsInfinity(value);
            public static bool IsInteger(BinaryIntegerReference<T> value) => T.IsInteger(value);
            public static bool IsNaN(BinaryIntegerReference<T> value) => T.IsNaN(value);
            public static bool IsNegative(BinaryIntegerReference<T> value) => T.IsNegative(value);
            public static bool IsNegativeInfinity(BinaryIntegerReference<T> value) => T.IsNegativeInfinity(value);
            public static bool IsNormal(BinaryIntegerReference<T> value) => T.IsNormal(value);
            public static bool IsOddInteger(BinaryIntegerReference<T> value) => T.IsOddInteger(value);
            public static bool IsPositive(BinaryIntegerReference<T> value) => T.IsPositive(value);
            public static bool IsPositiveInfinity(BinaryIntegerReference<T> value) => T.IsPositiveInfinity(value);
            public static bool IsPow2(BinaryIntegerReference<T> value) => T.IsPow2(value);
            public static bool IsRealNumber(BinaryIntegerReference<T> value) => T.IsRealNumber(value);
            public static bool IsSubnormal(BinaryIntegerReference<T> value) => T.IsSubnormal(value);
            public static bool IsZero(BinaryIntegerReference<T> value) => T.IsZero(value);
            public static BinaryIntegerReference<T> Log2(BinaryIntegerReference<T> value) => T.Log2(value);
            public static BinaryIntegerReference<T> MaxMagnitude(BinaryIntegerReference<T> x, BinaryIntegerReference<T> y) => T.MaxMagnitude(x, y);
            public static BinaryIntegerReference<T> MaxMagnitudeNumber(BinaryIntegerReference<T> x, BinaryIntegerReference<T> y) => T.MaxMagnitudeNumber(x, y);
            public static BinaryIntegerReference<T> MinMagnitude(BinaryIntegerReference<T> x, BinaryIntegerReference<T> y) => T.MinMagnitude(x, y);
            public static BinaryIntegerReference<T> MinMagnitudeNumber(BinaryIntegerReference<T> x, BinaryIntegerReference<T> y) => T.MinMagnitudeNumber(x, y);
            public static BinaryIntegerReference<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryIntegerReference<T> Parse(string s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryIntegerReference<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryIntegerReference<T> Parse(string s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryIntegerReference<T> PopCount(BinaryIntegerReference<T> value) => T.PopCount(value);
            public static BinaryIntegerReference<T> TrailingZeroCount(BinaryIntegerReference<T> value) => T.TrailingZeroCount(value);

            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerReference<T> result)
            {
                bool succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerReference<T> result)
            {
                bool succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerReference<T> result)
            {
                bool succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerReference<T> result)
            {
                bool succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BinaryIntegerReference<T> value)
            {
                bool succeeded = T.TryReadBigEndian(source, isUnsigned, out T actualValue);
                value = actualValue;
                return succeeded;
            }

            public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BinaryIntegerReference<T> value)
            {
                bool succeeded = T.TryReadLittleEndian(source, isUnsigned, out T actualValue);
                value = actualValue;
                return succeeded;
            }

            public int CompareTo(object? obj)
            {
                if (obj is not BinaryIntegerReference<T> other)
                {
                    return obj is null ? 1 : throw new ArgumentException();
                }

                return CompareTo(other);
            }

            public int CompareTo(BinaryIntegerReference<T>? other) => other is null ? 1 : Value.CompareTo(other.Value);
            public override bool Equals([NotNullWhen(true)] object? obj) => obj is BinaryIntegerReference<T> other && Equals(other);
            public bool Equals(BinaryIntegerReference<T>? other) => other is not null && Value.Equals(other.Value);
            public int GetByteCount() => Value.GetByteCount();
            public override int GetHashCode() => Value.GetHashCode();
            public int GetShortestBitLength() => Value.GetShortestBitLength();
            public override string ToString() => Value.ToString()!;
            public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);
            public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteBigEndian(destination, out bytesWritten);
            public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteLittleEndian(destination, out bytesWritten);

            static bool INumberBase<BinaryIntegerReference<T>>.TryConvertFromChecked<TOther>(TOther value, out BinaryIntegerReference<T> result)
            {
                if (typeof(TOther) == typeof(T))
                {
                    result = (T)(object)value;
                    return true;
                }

                bool succeeded = T.TryConvertFromChecked(value, out T actualResult);

                if (!succeeded)
                {
                    succeeded = TOther.TryConvertToChecked(value, out actualResult);
                }

                result = actualResult;
                return succeeded;
            }

            static bool INumberBase<BinaryIntegerReference<T>>.TryConvertFromSaturating<TOther>(TOther value, out BinaryIntegerReference<T> result)
            {
                if (typeof(TOther) == typeof(T))
                {
                    result = (T)(object)value;
                    return true;
                }

                bool succeeded = T.TryConvertFromSaturating(value, out T actualResult);

                if (!succeeded)
                {
                    succeeded = TOther.TryConvertToSaturating(value, out actualResult);
                }

                result = actualResult;
                return succeeded;
            }

            static bool INumberBase<BinaryIntegerReference<T>>.TryConvertFromTruncating<TOther>(TOther value, out BinaryIntegerReference<T> result)
            {
                if (typeof(TOther) == typeof(T))
                {
                    result = (T)(object)value;
                    return true;
                }

                bool succeeded = T.TryConvertFromTruncating(value, out T actualResult);

                if (!succeeded)
                {
                    succeeded = TOther.TryConvertToTruncating(value, out actualResult);
                }

                result = actualResult;
                return succeeded;
            }

            static bool INumberBase<BinaryIntegerReference<T>>.TryConvertToChecked<TOther>(BinaryIntegerReference<T> value, out TOther result) => T.TryConvertToChecked(value.Value, out result);
            static bool INumberBase<BinaryIntegerReference<T>>.TryConvertToSaturating<TOther>(BinaryIntegerReference<T> value, out TOther result) => T.TryConvertToSaturating(value.Value, out result);
            static bool INumberBase<BinaryIntegerReference<T>>.TryConvertToTruncating<TOther>(BinaryIntegerReference<T> value, out TOther result) => T.TryConvertToTruncating(value.Value, out result);

            public static BinaryIntegerReference<T> operator +(BinaryIntegerReference<T> value) => +value.Value;
            public static BinaryIntegerReference<T> operator +(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value + right.Value;
            public static BinaryIntegerReference<T> operator -(BinaryIntegerReference<T> value) => -value.Value;
            public static BinaryIntegerReference<T> operator -(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value - right.Value;
            public static BinaryIntegerReference<T> operator ~(BinaryIntegerReference<T> value) => ~value.Value;
            public static BinaryIntegerReference<T> operator ++(BinaryIntegerReference<T> value) => value.Value + T.One;
            public static BinaryIntegerReference<T> operator --(BinaryIntegerReference<T> value) => value.Value - T.One;
            public static BinaryIntegerReference<T> operator *(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value * right.Value;
            public static BinaryIntegerReference<T> operator /(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value / right.Value;
            public static BinaryIntegerReference<T> operator %(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value % right.Value;
            public static BinaryIntegerReference<T> operator &(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value & right.Value;
            public static BinaryIntegerReference<T> operator |(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value | right.Value;
            public static BinaryIntegerReference<T> operator ^(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value ^ right.Value;
            public static BinaryIntegerReference<T> operator <<(BinaryIntegerReference<T> value, int shiftAmount) => value.Value << shiftAmount;
            public static BinaryIntegerReference<T> operator >>(BinaryIntegerReference<T> value, int shiftAmount) => value.Value >> shiftAmount;
            public static bool operator ==(BinaryIntegerReference<T>? left, BinaryIntegerReference<T>? right) => left is null ? right is null : left.Equals(right);
            public static bool operator !=(BinaryIntegerReference<T>? left, BinaryIntegerReference<T>? right) => !(left == right);
            public static bool operator <(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value < right.Value;
            public static bool operator >(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value > right.Value;
            public static bool operator <=(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value <= right.Value;
            public static bool operator >=(BinaryIntegerReference<T> left, BinaryIntegerReference<T> right) => left.Value >= right.Value;
            public static BinaryIntegerReference<T> operator >>>(BinaryIntegerReference<T> value, int shiftAmount) => value.Value >>> shiftAmount;
        }

        private sealed class BinaryFloatingPointIeee754Reference<T> : IBinaryFloatingPointIeee754<BinaryFloatingPointIeee754Reference<T>>
            where T : IBinaryFloatingPointIeee754<T>
        {
            public BinaryFloatingPointIeee754Reference(T value) => Value = value;

            public T Value { get; }

            public static implicit operator BinaryFloatingPointIeee754Reference<T>(T value) => new(value);
            public static implicit operator T(BinaryFloatingPointIeee754Reference<T> value) => value.Value;

            public static BinaryFloatingPointIeee754Reference<T> AdditiveIdentity => T.AdditiveIdentity;
            public static BinaryFloatingPointIeee754Reference<T> E => T.E;
            public static BinaryFloatingPointIeee754Reference<T> Epsilon => T.Epsilon;
            public static BinaryFloatingPointIeee754Reference<T> MultiplicativeIdentity => T.MultiplicativeIdentity;
            public static BinaryFloatingPointIeee754Reference<T> NaN => T.NaN;
            public static BinaryFloatingPointIeee754Reference<T> NegativeInfinity => T.NegativeInfinity;
            public static BinaryFloatingPointIeee754Reference<T> NegativeOne => T.NegativeOne;
            public static BinaryFloatingPointIeee754Reference<T> NegativeZero => T.NegativeZero;
            public static BinaryFloatingPointIeee754Reference<T> One => T.One;
            public static BinaryFloatingPointIeee754Reference<T> Pi => T.Pi;
            public static BinaryFloatingPointIeee754Reference<T> PositiveInfinity => T.PositiveInfinity;
            public static int Radix => T.Radix;
            public static BinaryFloatingPointIeee754Reference<T> Tau => T.Tau;
            public static BinaryFloatingPointIeee754Reference<T> Zero => T.Zero;

            public static BinaryFloatingPointIeee754Reference<T> Abs(BinaryFloatingPointIeee754Reference<T> value) => T.Abs(value);
            public static BinaryFloatingPointIeee754Reference<T> Acos(BinaryFloatingPointIeee754Reference<T> x) => T.Acos(x);
            public static BinaryFloatingPointIeee754Reference<T> Acosh(BinaryFloatingPointIeee754Reference<T> x) => T.Acosh(x);
            public static BinaryFloatingPointIeee754Reference<T> AcosPi(BinaryFloatingPointIeee754Reference<T> x) => T.AcosPi(x);
            public static BinaryFloatingPointIeee754Reference<T> Asin(BinaryFloatingPointIeee754Reference<T> x) => T.Asin(x);
            public static BinaryFloatingPointIeee754Reference<T> Asinh(BinaryFloatingPointIeee754Reference<T> x) => T.Asinh(x);
            public static BinaryFloatingPointIeee754Reference<T> AsinPi(BinaryFloatingPointIeee754Reference<T> x) => T.AsinPi(x);
            public static BinaryFloatingPointIeee754Reference<T> Atan(BinaryFloatingPointIeee754Reference<T> x) => T.Atan(x);
            public static BinaryFloatingPointIeee754Reference<T> Atan2(BinaryFloatingPointIeee754Reference<T> y, BinaryFloatingPointIeee754Reference<T> x) => T.Atan2(y, x);
            public static BinaryFloatingPointIeee754Reference<T> Atan2Pi(BinaryFloatingPointIeee754Reference<T> y, BinaryFloatingPointIeee754Reference<T> x) => T.Atan2Pi(y, x);
            public static BinaryFloatingPointIeee754Reference<T> Atanh(BinaryFloatingPointIeee754Reference<T> x) => T.Atanh(x);
            public static BinaryFloatingPointIeee754Reference<T> AtanPi(BinaryFloatingPointIeee754Reference<T> x) => T.AtanPi(x);
            public static BinaryFloatingPointIeee754Reference<T> BitDecrement(BinaryFloatingPointIeee754Reference<T> x) => T.BitDecrement(x);
            public static BinaryFloatingPointIeee754Reference<T> BitIncrement(BinaryFloatingPointIeee754Reference<T> x) => T.BitIncrement(x);
            public static BinaryFloatingPointIeee754Reference<T> Cbrt(BinaryFloatingPointIeee754Reference<T> x) => T.Cbrt(x);
            public static BinaryFloatingPointIeee754Reference<T> Cos(BinaryFloatingPointIeee754Reference<T> x) => T.Cos(x);
            public static BinaryFloatingPointIeee754Reference<T> Cosh(BinaryFloatingPointIeee754Reference<T> x) => T.Cosh(x);
            public static BinaryFloatingPointIeee754Reference<T> CosPi(BinaryFloatingPointIeee754Reference<T> x) => T.CosPi(x);
            public static BinaryFloatingPointIeee754Reference<T> Exp(BinaryFloatingPointIeee754Reference<T> x) => T.Exp(x);
            public static BinaryFloatingPointIeee754Reference<T> Exp2(BinaryFloatingPointIeee754Reference<T> x) => T.Exp2(x);
            public static BinaryFloatingPointIeee754Reference<T> Exp10(BinaryFloatingPointIeee754Reference<T> x) => T.Exp10(x);
            public static BinaryFloatingPointIeee754Reference<T> FusedMultiplyAdd(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right, BinaryFloatingPointIeee754Reference<T> addend) => T.FusedMultiplyAdd(left, right, addend);
            public static BinaryFloatingPointIeee754Reference<T> Hypot(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> y) => T.Hypot(x, y);
            public static BinaryFloatingPointIeee754Reference<T> Ieee754Remainder(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => T.Ieee754Remainder(left, right);
            public static int ILogB(BinaryFloatingPointIeee754Reference<T> x) => T.ILogB(x);
            public static bool IsCanonical(BinaryFloatingPointIeee754Reference<T> value) => T.IsCanonical(value);
            public static bool IsComplexNumber(BinaryFloatingPointIeee754Reference<T> value) => T.IsComplexNumber(value);
            public static bool IsEvenInteger(BinaryFloatingPointIeee754Reference<T> value) => T.IsEvenInteger(value);
            public static bool IsFinite(BinaryFloatingPointIeee754Reference<T> value) => T.IsFinite(value);
            public static bool IsImaginaryNumber(BinaryFloatingPointIeee754Reference<T> value) => T.IsImaginaryNumber(value);
            public static bool IsInfinity(BinaryFloatingPointIeee754Reference<T> value) => T.IsInfinity(value);
            public static bool IsInteger(BinaryFloatingPointIeee754Reference<T> value) => T.IsInteger(value);
            public static bool IsNaN(BinaryFloatingPointIeee754Reference<T> value) => T.IsNaN(value);
            public static bool IsNegative(BinaryFloatingPointIeee754Reference<T> value) => T.IsNegative(value);
            public static bool IsNegativeInfinity(BinaryFloatingPointIeee754Reference<T> value) => T.IsNegativeInfinity(value);
            public static bool IsNormal(BinaryFloatingPointIeee754Reference<T> value) => T.IsNormal(value);
            public static bool IsOddInteger(BinaryFloatingPointIeee754Reference<T> value) => T.IsOddInteger(value);
            public static bool IsPositive(BinaryFloatingPointIeee754Reference<T> value) => T.IsPositive(value);
            public static bool IsPositiveInfinity(BinaryFloatingPointIeee754Reference<T> value) => T.IsPositiveInfinity(value);
            public static bool IsPow2(BinaryFloatingPointIeee754Reference<T> value) => T.IsPow2(value);
            public static bool IsRealNumber(BinaryFloatingPointIeee754Reference<T> value) => T.IsRealNumber(value);
            public static bool IsSubnormal(BinaryFloatingPointIeee754Reference<T> value) => T.IsSubnormal(value);
            public static bool IsZero(BinaryFloatingPointIeee754Reference<T> value) => T.IsZero(value);
            public static BinaryFloatingPointIeee754Reference<T> Log(BinaryFloatingPointIeee754Reference<T> x) => T.Log(x);
            public static BinaryFloatingPointIeee754Reference<T> Log(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> newBase) => T.Log(x, newBase);
            public static BinaryFloatingPointIeee754Reference<T> Log2(BinaryFloatingPointIeee754Reference<T> x) => BinaryLog2(x.Value);
            public static BinaryFloatingPointIeee754Reference<T> Log10(BinaryFloatingPointIeee754Reference<T> x) => T.Log10(x);
            public static BinaryFloatingPointIeee754Reference<T> MaxMagnitude(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> y) => T.MaxMagnitude(x, y);
            public static BinaryFloatingPointIeee754Reference<T> MaxMagnitudeNumber(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> y) => T.MaxMagnitudeNumber(x, y);
            public static BinaryFloatingPointIeee754Reference<T> MinMagnitude(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> y) => T.MinMagnitude(x, y);
            public static BinaryFloatingPointIeee754Reference<T> MinMagnitudeNumber(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> y) => T.MinMagnitudeNumber(x, y);
            public static BinaryFloatingPointIeee754Reference<T> Pow(BinaryFloatingPointIeee754Reference<T> x, BinaryFloatingPointIeee754Reference<T> y) => T.Pow(x, y);
            public static BinaryFloatingPointIeee754Reference<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryFloatingPointIeee754Reference<T> Parse(string s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryFloatingPointIeee754Reference<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryFloatingPointIeee754Reference<T> Parse(string s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryFloatingPointIeee754Reference<T> RootN(BinaryFloatingPointIeee754Reference<T> x, int n) => T.RootN(x, n);
            public static BinaryFloatingPointIeee754Reference<T> Round(BinaryFloatingPointIeee754Reference<T> x, int digits, MidpointRounding mode) => T.Round(x, digits, mode);
            public static BinaryFloatingPointIeee754Reference<T> ScaleB(BinaryFloatingPointIeee754Reference<T> x, int n) => T.ScaleB(x, n);
            public static BinaryFloatingPointIeee754Reference<T> Sin(BinaryFloatingPointIeee754Reference<T> x) => T.Sin(x);
            public static (BinaryFloatingPointIeee754Reference<T> Sin, BinaryFloatingPointIeee754Reference<T> Cos) SinCos(BinaryFloatingPointIeee754Reference<T> x) => T.SinCos(x);
            public static (BinaryFloatingPointIeee754Reference<T> SinPi, BinaryFloatingPointIeee754Reference<T> CosPi) SinCosPi(BinaryFloatingPointIeee754Reference<T> x) => T.SinCosPi(x);
            public static BinaryFloatingPointIeee754Reference<T> Sinh(BinaryFloatingPointIeee754Reference<T> x) => T.Sinh(x);
            public static BinaryFloatingPointIeee754Reference<T> SinPi(BinaryFloatingPointIeee754Reference<T> x) => T.SinPi(x);
            public static BinaryFloatingPointIeee754Reference<T> Sqrt(BinaryFloatingPointIeee754Reference<T> x) => T.Sqrt(x);
            public static BinaryFloatingPointIeee754Reference<T> Tan(BinaryFloatingPointIeee754Reference<T> x) => T.Tan(x);
            public static BinaryFloatingPointIeee754Reference<T> Tanh(BinaryFloatingPointIeee754Reference<T> x) => T.Tanh(x);
            public static BinaryFloatingPointIeee754Reference<T> TanPi(BinaryFloatingPointIeee754Reference<T> x) => T.TanPi(x);

            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Reference<T> result)
            {
                bool succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Reference<T> result)
            {
                bool succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Reference<T> result)
            {
                bool succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Reference<T> result)
            {
                bool succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }

            public int CompareTo(object? obj)
            {
                if (obj is not BinaryFloatingPointIeee754Reference<T> other)
                {
                    return obj is null ? 1 : throw new ArgumentException();
                }

                return CompareTo(other);
            }

            public int CompareTo(BinaryFloatingPointIeee754Reference<T>? other) => other is null ? 1 : Value.CompareTo(other.Value);
            public override bool Equals([NotNullWhen(true)] object? obj) => obj is BinaryFloatingPointIeee754Reference<T> other && Equals(other);
            public bool Equals(BinaryFloatingPointIeee754Reference<T>? other) => other is not null && Value.Equals(other.Value);
            public int GetExponentByteCount() => Value.GetExponentByteCount();
            public int GetExponentShortestBitLength() => Value.GetExponentShortestBitLength();
            public override int GetHashCode() => Value.GetHashCode();
            public int GetSignificandBitLength() => Value.GetSignificandBitLength();
            public int GetSignificandByteCount() => Value.GetSignificandByteCount();
            public override string ToString() => Value.ToString()!;
            public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);
            public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteExponentBigEndian(destination, out bytesWritten);
            public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteExponentLittleEndian(destination, out bytesWritten);
            public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteSignificandBigEndian(destination, out bytesWritten);
            public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteSignificandLittleEndian(destination, out bytesWritten);

            static bool INumberBase<BinaryFloatingPointIeee754Reference<T>>.TryConvertFromChecked<TOther>(TOther value, out BinaryFloatingPointIeee754Reference<T> result)
            {
                if (typeof(TOther) == typeof(T))
                {
                    result = (T)(object)value;
                    return true;
                }

                bool succeeded = T.TryConvertFromChecked(value, out T actualResult);

                if (!succeeded)
                {
                    succeeded = TOther.TryConvertToChecked(value, out actualResult);
                }

                result = actualResult;
                return succeeded;
            }

            static bool INumberBase<BinaryFloatingPointIeee754Reference<T>>.TryConvertFromSaturating<TOther>(TOther value, out BinaryFloatingPointIeee754Reference<T> result)
            {
                if (typeof(TOther) == typeof(T))
                {
                    result = (T)(object)value;
                    return true;
                }

                bool succeeded = T.TryConvertFromSaturating(value, out T actualResult);

                if (!succeeded)
                {
                    succeeded = TOther.TryConvertToSaturating(value, out actualResult);
                }

                result = actualResult;
                return succeeded;
            }

            static bool INumberBase<BinaryFloatingPointIeee754Reference<T>>.TryConvertFromTruncating<TOther>(TOther value, out BinaryFloatingPointIeee754Reference<T> result)
            {
                if (typeof(TOther) == typeof(T))
                {
                    result = (T)(object)value;
                    return true;
                }

                bool succeeded = T.TryConvertFromTruncating(value, out T actualResult);

                if (!succeeded)
                {
                    succeeded = TOther.TryConvertToTruncating(value, out actualResult);
                }

                result = actualResult;
                return succeeded;
            }

            static bool INumberBase<BinaryFloatingPointIeee754Reference<T>>.TryConvertToChecked<TOther>(BinaryFloatingPointIeee754Reference<T> value, out TOther result) => T.TryConvertToChecked(value.Value, out result);
            static bool INumberBase<BinaryFloatingPointIeee754Reference<T>>.TryConvertToSaturating<TOther>(BinaryFloatingPointIeee754Reference<T> value, out TOther result) => T.TryConvertToSaturating(value.Value, out result);
            static bool INumberBase<BinaryFloatingPointIeee754Reference<T>>.TryConvertToTruncating<TOther>(BinaryFloatingPointIeee754Reference<T> value, out TOther result) => T.TryConvertToTruncating(value.Value, out result);

            public static BinaryFloatingPointIeee754Reference<T> operator +(BinaryFloatingPointIeee754Reference<T> value) => +value.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator +(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value + right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator -(BinaryFloatingPointIeee754Reference<T> value) => -value.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator -(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value - right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator ~(BinaryFloatingPointIeee754Reference<T> value) => ~value.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator ++(BinaryFloatingPointIeee754Reference<T> value) => value.Value + T.One;
            public static BinaryFloatingPointIeee754Reference<T> operator --(BinaryFloatingPointIeee754Reference<T> value) => value.Value - T.One;
            public static BinaryFloatingPointIeee754Reference<T> operator *(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value * right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator /(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value / right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator %(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value % right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator &(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value & right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator |(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value | right.Value;
            public static BinaryFloatingPointIeee754Reference<T> operator ^(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value ^ right.Value;
            public static bool operator ==(BinaryFloatingPointIeee754Reference<T>? left, BinaryFloatingPointIeee754Reference<T>? right) => left is null ? right is null : left.Equals(right);
            public static bool operator !=(BinaryFloatingPointIeee754Reference<T>? left, BinaryFloatingPointIeee754Reference<T>? right) => !(left == right);
            public static bool operator <(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value < right.Value;
            public static bool operator >(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value > right.Value;
            public static bool operator <=(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value <= right.Value;
            public static bool operator >=(BinaryFloatingPointIeee754Reference<T> left, BinaryFloatingPointIeee754Reference<T> right) => left.Value >= right.Value;

            private static TNumber BinaryLog2<TNumber>(TNumber value) where TNumber : IBinaryNumber<TNumber> => TNumber.Log2(value);
        }

        private sealed class MaxValueRandom : Random
        {
            public override void NextBytes(Span<byte> buffer)
            {
                buffer.Fill(byte.MaxValue);
            }
        }
    }
}
