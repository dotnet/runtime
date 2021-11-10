// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

            for (int i = 0; i < 1000; i++)
            {
                float x = r.NextSingle();
                Assert.True(x >= 0.0 && x < 1.0);
            }

            for (int i = 0; i < 1000; i++)
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Shared_IsSingleton()
        {
            Assert.NotNull(Random.Shared);
            Assert.Same(Random.Shared, Random.Shared);
            Assert.Same(Random.Shared, Task.Run(() => Random.Shared).Result);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

                Assert.Equal(11, randOuter.Next(42));
                Assert.Equal(1865324524, randOuter.Next(int.MaxValue));

                Assert.Equal(0, randOuter.Next(0, 0));
                Assert.Equal(1, randOuter.Next(1, 2));
                Assert.Equal(12, randOuter.Next(0, 42));
                Assert.Equal(7234, randOuter.Next(42, 12345));
                Assert.Equal(2147483642, randOuter.Next(int.MaxValue - 5, int.MaxValue));
                Assert.Equal(-1236260882, randOuter.Next(int.MinValue, int.MaxValue));

                Assert.Equal(3644728249650840822, randOuter.NextInt64());
                Assert.Equal(2809750975933744783, randOuter.NextInt64());

                Assert.Equal(0, randOuter.NextInt64(0));
                Assert.Equal(0, randOuter.NextInt64(1));
                Assert.Equal(35, randOuter.NextInt64(42));
                Assert.Equal(7986543274318426717, randOuter.NextInt64(long.MaxValue));

                Assert.Equal(0, randOuter.NextInt64(0, 0));
                Assert.Equal(1, randOuter.NextInt64(1, 2));
                Assert.Equal(15, randOuter.NextInt64(0, 42));
                Assert.Equal(4155, randOuter.NextInt64(42, 12345));
                Assert.Equal(9223372036854775803, randOuter.NextInt64(long.MaxValue - 5, long.MaxValue));
                Assert.Equal(375288451405801266, randOuter.NextInt64(long.MinValue, long.MaxValue));

                Assert.Equal(0.2885307561293763, randOuter.NextDouble());
                Assert.Equal(0.8319616593420064, randOuter.NextDouble());
                Assert.Equal(0.694751074593599, randOuter.NextDouble());

                Assert.Equal(0.7749006f, randOuter.NextSingle());
                Assert.Equal(0.13424736f, randOuter.NextSingle());
                Assert.Equal(0.05282557f, randOuter.NextSingle());
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
                Assert.Equal(1207874445, randOuter.Next(int.MaxValue));

                Assert.Equal(0, randOuter.Next(0, 0));
                Assert.Equal(1, randOuter.Next(1, 2));
                Assert.Equal(33, randOuter.Next(0, 42));
                Assert.Equal(2525, randOuter.Next(42, 12345));
                Assert.Equal(2147483646, randOuter.Next(int.MaxValue - 5, int.MaxValue));
                Assert.Equal(-1841045958, randOuter.Next(int.MinValue, int.MaxValue));

                Assert.Equal(364988307769675967, randOuter.NextInt64());
                Assert.Equal(4081751239945971648, randOuter.NextInt64());

                Assert.Equal(0, randOuter.NextInt64(0));
                Assert.Equal(0, randOuter.NextInt64(1));
                Assert.Equal(8, randOuter.NextInt64(42));
                Assert.Equal(3127675200855610302, randOuter.NextInt64(long.MaxValue));

                Assert.Equal(0, randOuter.NextInt64(0, 0));
                Assert.Equal(1, randOuter.NextInt64(1, 2));
                Assert.Equal(25, randOuter.NextInt64(0, 42));
                Assert.Equal(593, randOuter.NextInt64(42, 12345));
                Assert.Equal(9223372036854775805, randOuter.NextInt64(long.MaxValue - 5, long.MaxValue));
                Assert.Equal(-1415073976784572606, randOuter.NextInt64(long.MinValue, long.MaxValue));

                Assert.Equal(0.054582986776168796, randOuter.NextDouble());
                Assert.Equal(0.7599686772523376, randOuter.NextDouble());
                Assert.Equal(0.9113759792165226, randOuter.NextDouble());

                Assert.Equal(0.3010761f, randOuter.NextSingle());
                Assert.Equal(0.8162224f, randOuter.NextSingle());
                Assert.Equal(0.5866389f, randOuter.NextSingle());
            }
        }

        private static Random Create(bool derived, bool seeded, int seed = 42) =>
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
    }
}
