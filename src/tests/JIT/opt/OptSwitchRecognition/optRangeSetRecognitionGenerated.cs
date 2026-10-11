// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is generated (seed 20240601, 400 methods) by a randomized generator of constant set membership
// tests. Every method checks membership of a random set of values/ranges using one of several code shapes
// (is-patterns, ||-chains, subtractions, decision trees, switches, negations, ...) and the result is compared with
// a trivial reference implementation for the interesting part of the value domain.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace optRangeSetRecognitionGenerated;

public class CRangeSetRecognitionGeneratedTest
{
    private static bool InSet((long Lo, long Hi)[] set, long value)
    {
        foreach (var (lo, hi) in set)
        {
            if (value >= lo && value <= hi)
            {
                return true;
            }
        }
        return false;
    }

    // The values that are tested: the neighborhood of all range boundaries, the extremes of the type, some random values
    // and the whole domain for small types.
    private static long[] Probe((long Lo, long Hi)[] set, long min, long max, bool fullDomain, int seed)
    {
        var values = new HashSet<long>();
        if (fullDomain)
        {
            for (long v = min; v <= max; v++)
            {
                values.Add(v);
            }
        }
        else
        {
            foreach (var (lo, hi) in set)
            {
                for (long d = -70; d <= 70; d++)
                {
                    Add(values, lo + d, min, max);
                    Add(values, hi + d, min, max);
                }
            }
            for (long d = 0; d < 70; d++)
            {
                Add(values, min + d, min, max);
                Add(values, max - d, min, max);
                Add(values, d - 35, min, max);
            }
            var random = new Random(seed);
            for (int i = 0; i < 300; i++)
            {
                Add(values, random.NextInt64(min, max), min, max);
            }
        }
        long[] result = new long[values.Count];
        values.CopyTo(result);
        return result;

        static void Add(HashSet<long> values, long v, long min, long max)
        {
            if (v >= min && v <= max)
            {
                values.Add(v);
            }
        }
    }

    private static readonly (long Lo, long Hi)[] s_set0 = [(12437L, 12437L), (12371L, 12371L), (12440L, 12440L), (12424L, 12424L), (12352L, 12359L), (12381L, 12381L), (12403L, 12403L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_IsPattern_0(char x) => x is '\u3095' or '\u3053' or '\u3098' or '\u3088' or >= '\u3040' and <= '\u3047' or '\u305D' or '\u3073';

    private static void Check0()
    {
        foreach (long v in Probe(s_set0, 0L, 65535L, true, 0))
        {
            Assert.True(char_IsPattern_0((char)v) == InSet(s_set0, v), $"char_IsPattern_0({v}) set=[12437..12437] [12371..12371] [12440..12440] [12424..12424] [12352..12359] [12381..12381] [12403..12403]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set1 = [(233L, 233L), (233L, 233L), (201L, 208L), (206L, 206L), (238L, 238L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_OrChain_1(byte x) => x == 233 || x == 233 || (x >= 201 && x <= 208) || x == 206 || x == 238;

    private static void Check1()
    {
        foreach (long v in Probe(s_set1, 0L, 255L, true, 1))
        {
            Assert.True(byte_OrChain_1((byte)v) == InSet(s_set1, v), $"byte_OrChain_1({v}) set=[233..233] [233..233] [201..208] [206..206] [238..238]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set2 = [(-128L, -117L), (-108L, -99L), (-71L, -69L), (-117L, -108L), (-32L, -31L), (-124L, -124L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_2(sbyte x) => (x < (-108) ? ((x >= (-128) && x <= (-117)) || x == (-124) || (x >= (-117) && x <= (-108))) : (x < (-71) ? ((x >= (-117) && x <= (-108)) || (x >= (-108) && x <= (-99))) : (x < (-32) ? (x >= (-71) && x <= (-69)) : (x >= (-32) && x <= (-31)))));

    private static void Check2()
    {
        foreach (long v in Probe(s_set2, (-128L), 127L, true, 2))
        {
            Assert.True(sbyte_Tree_2((sbyte)v) == InSet(s_set2, v), $"sbyte_Tree_2({v}) set=[-128..-117] [-108..-99] [-71..-69] [-117..-108] [-32..-31] [-124..-124]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set3 = [(167L, 178L), (134L, 134L), (92L, 96L), (85L, 85L), (31L, 32L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_3(short x) => !(x is >= 167 and <= 178 or 134 or >= 92 and <= 96 or 85 or >= 31 and <= 32);

    private static void Check3()
    {
        foreach (long v in Probe(s_set3, (-32768L), 32767L, true, 3))
        {
            Assert.True(short_Negated_3((short)v) == !InSet(s_set3, v), $"short_Negated_3({v}) set=[167..178] [134..134] [92..96] [85..85] [31..32]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set4 = [(224L, 233L), (261L, 263L), (257L, 259L), (293L, 300L), (205L, 208L), (270L, 277L), (238L, 247L), (269L, 292L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Negated_4(ushort x) => !(x is >= 224 and <= 233 or >= 261 and <= 263 or >= 257 and <= 259 or >= 293 and <= 300 or >= 205 and <= 208 or >= 270 and <= 277 or >= 238 and <= 247 or >= 269 and <= 292);

    private static void Check4()
    {
        foreach (long v in Probe(s_set4, 0L, 65535L, true, 4))
        {
            Assert.True(ushort_Negated_4((ushort)v) == !InSet(s_set4, v), $"ushort_Negated_4({v}) set=[224..233] [261..263] [257..259] [293..300] [205..208] [270..277] [238..247] [269..292]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set5 = [(1000L, 1000L), (1016L, 1025L), (1000L, 1001L), (1068L, 1092L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_5(int x)
    {
        switch (x)
        {
            case 1000:
            case 1001:
            case 1016:
            case 1017:
            case 1018:
            case 1019:
            case 1020:
            case 1021:
            case 1022:
            case 1023:
            case 1024:
            case 1025:
            case >= 1068 and <= 1092:
                return true;
            default:
                return false;
        }
    }

    private static void Check5()
    {
        foreach (long v in Probe(s_set5, (-2147483648L), 2147483647L, false, 5))
        {
            Assert.True(int_SwitchStmt_5((int)v) == InSet(s_set5, v), $"int_SwitchStmt_5({v}) set=[1000..1000] [1016..1025] [1000..1001] [1068..1092]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set6 = [(1101L, 1106L), (1158L, 1158L), (1197L, 1197L), (1160L, 1160L), (1123L, 1126L), (1140L, 1149L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Negated_6(uint x) => !(x is >= 1101u and <= 1106u or 1158u or 1197u or 1160u or >= 1123u and <= 1126u or >= 1140u and <= 1149u);

    private static void Check6()
    {
        foreach (long v in Probe(s_set6, 0L, 4294967295L, false, 6))
        {
            Assert.True(uint_Negated_6((uint)v) == !InSet(s_set6, v), $"uint_Negated_6({v}) set=[1101..1106] [1158..1158] [1197..1197] [1160..1160] [1123..1126] [1140..1149]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set7 = [(-9223372036854775743L, -9223372036854775742L), (9223372036854775798L, 9223372036854775807L), (-9223372036854775789L, -9223372036854775789L), (-9223372036854775779L, -9223372036854775767L), (-9223372036854775762L, -9223372036854775762L), (-9223372036854775734L, -9223372036854775732L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_7(long x) => (x >= (-9223372036854775743L) && x <= (-9223372036854775742L)) || (x >= 9223372036854775798L && x <= 9223372036854775807L) || x == (-9223372036854775789L) || (x >= (-9223372036854775779L) && x <= (-9223372036854775767L)) || x == (-9223372036854775762L) || (x >= (-9223372036854775734L) && x <= (-9223372036854775732L));

    private static void Check7()
    {
        foreach (long v in Probe(s_set7, long.MinValue, 9223372036854775807L, false, 7))
        {
            Assert.True(long_OrChain_7((long)v) == InSet(s_set7, v), $"long_OrChain_7({v}) set=[-9223372036854775743..-9223372036854775742] [9223372036854775798..9223372036854775807] [-9223372036854775789..-9223372036854775789] [-9223372036854775779..-9223372036854775767] [-9223372036854775762..-9223372036854775762] [-9223372036854775734..-9223372036854775732]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set8 = [(7L, 7L), (11L, 23L), (8L, 19L), (16L, 21L), (13L, 20L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_IsPattern_8(char x) => x is '\u0007' or >= '\u000B' and <= '\u0017' or >= '\u0008' and <= '\u0013' or >= '\u0010' and <= '\u0015' or >= '\u000D' and <= '\u0014';

    private static void Check8()
    {
        foreach (long v in Probe(s_set8, 0L, 65535L, true, 8))
        {
            Assert.True(char_IsPattern_8((char)v) == InSet(s_set8, v), $"char_IsPattern_8({v}) set=[7..7] [11..23] [8..19] [16..21] [13..20]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set9 = [(111L, 113L), (193L, 198L), (179L, 188L), (94L, 101L), (67L, 109L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchStmt_9(byte x)
    {
        switch (x)
        {
            case >= 67 and <= 109:
            case 111:
            case 112:
            case 113:
            case 179:
            case 180:
            case 181:
            case 182:
            case 183:
            case 184:
            case 185:
            case 186:
            case 187:
            case 188:
            case 193:
            case 194:
            case 195:
            case 196:
            case 197:
            case 198:
                return true;
            default:
                return false;
        }
    }

    private static void Check9()
    {
        foreach (long v in Probe(s_set9, 0L, 255L, true, 9))
        {
            Assert.True(byte_SwitchStmt_9((byte)v) == InSet(s_set9, v), $"byte_SwitchStmt_9({v}) set=[111..113] [193..198] [179..188] [94..101] [67..109]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set10 = [(84L, 84L), (98L, 101L), (82L, 84L), (66L, 76L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_10(sbyte x) => (x < 84 ? (x < 82 ? (x >= 66 && x <= 76) : (x >= 82 && x <= 84)) : ((x >= 82 && x <= 84) || x == 84 || (x >= 98 && x <= 101)));

    private static void Check10()
    {
        foreach (long v in Probe(s_set10, (-128L), 127L, true, 10))
        {
            Assert.True(sbyte_Tree_10((sbyte)v) == InSet(s_set10, v), $"sbyte_Tree_10({v}) set=[84..84] [98..101] [82..84] [66..76]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set11 = [(32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_11(short x) => !(x is 32767 or 32767 or 32767 or 32767 or 32767 or 32767);

    private static void Check11()
    {
        foreach (long v in Probe(s_set11, (-32768L), 32767L, true, 11))
        {
            Assert.True(short_Negated_11((short)v) == !InSet(s_set11, v), $"short_Negated_11({v}) set=[32767..32767] [32767..32767] [32767..32767] [32767..32767] [32767..32767] [32767..32767]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set12 = [(1194L, 1196L), (1113L, 1113L), (1099L, 1107L), (1120L, 1120L), (1024L, 1025L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchStmt_12(ushort x)
    {
        switch (x)
        {
            case 1024:
            case 1025:
            case 1099:
            case 1100:
            case 1101:
            case 1102:
            case 1103:
            case 1104:
            case 1105:
            case 1106:
            case 1107:
            case 1113:
            case 1120:
            case 1194:
            case 1195:
            case 1196:
                return true;
            default:
                return false;
        }
    }

    private static void Check12()
    {
        foreach (long v in Probe(s_set12, 0L, 65535L, true, 12))
        {
            Assert.True(ushort_SwitchStmt_12((ushort)v) == InSet(s_set12, v), $"ushort_SwitchStmt_12({v}) set=[1194..1196] [1113..1113] [1099..1107] [1120..1120] [1024..1025]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set13 = [(1048757L, 1048757L), (1048707L, 1048707L), (1048673L, 1048681L), (1048704L, 1048704L), (1048634L, 1048634L), (1048640L, 1048648L), (1048622L, 1048622L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_13(int x) => x is 1048757 or 1048707 or >= 1048673 and <= 1048681 or 1048704 or 1048634 or >= 1048640 and <= 1048648 or 1048622;

    private static void Check13()
    {
        foreach (long v in Probe(s_set13, (-2147483648L), 2147483647L, false, 13))
        {
            Assert.True(int_IsPattern_13((int)v) == InSet(s_set13, v), $"int_IsPattern_13({v}) set=[1048757..1048757] [1048707..1048707] [1048673..1048681] [1048704..1048704] [1048634..1048634] [1048640..1048648] [1048622..1048622]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set14 = [(104L, 113L), (164L, 167L), (89L, 89L), (77L, 86L), (86L, 92L), (125L, 130L), (130L, 130L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Negated_14(uint x) => !(x is >= 104u and <= 113u or >= 164u and <= 167u or 89u or >= 77u and <= 86u or >= 86u and <= 92u or >= 125u and <= 130u or 130u);

    private static void Check14()
    {
        foreach (long v in Probe(s_set14, 0L, 4294967295L, false, 14))
        {
            Assert.True(uint_Negated_14((uint)v) == !InSet(s_set14, v), $"uint_Negated_14({v}) set=[104..113] [164..167] [89..89] [77..86] [86..92] [125..130] [130..130]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set15 = [(85L, 88L), (-9223372036854775808L, -9223372036854775797L), (68L, 68L), (125L, 132L), (100L, 105L), (-9223372036854775808L, -9223372036854775808L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Flipped_15(long x) => (x > 84L && x < 89L) || (x >= long.MinValue && x < (-9223372036854775796L)) || x == 68L || (x > 124L && x < 133L) || (100L <= x && 105L >= x) || x == long.MinValue;

    private static void Check15()
    {
        foreach (long v in Probe(s_set15, long.MinValue, 9223372036854775807L, false, 15))
        {
            Assert.True(long_Flipped_15((long)v) == InSet(s_set15, v), $"long_Flipped_15({v}) set=[85..88] [-9223372036854775808..-9223372036854775797] [68..68] [125..132] [100..105] [-9223372036854775808..-9223372036854775808]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set16 = [(1027L, 1034L), (1044L, 1046L), (1045L, 1045L), (1023L, 1023L), (1059L, 1068L), (1040L, 1045L), (1068L, 1083L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_16(char x) => (x >= '\u0403' && x <= '\u040A') || (x >= '\u0414' && x <= '\u0416') || x == '\u0415' || x == '\u03FF' || (x >= '\u0423' && x <= '\u042C') || (x >= '\u0410' && x <= '\u0415') || (x >= '\u042C' && x <= '\u043B');

    private static void Check16()
    {
        foreach (long v in Probe(s_set16, 0L, 65535L, true, 16))
        {
            Assert.True(char_OrChain_16((char)v) == InSet(s_set16, v), $"char_OrChain_16({v}) set=[1027..1034] [1044..1046] [1045..1045] [1023..1023] [1059..1068] [1040..1045] [1068..1083]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set17 = [(99L, 110L), (133L, 144L), (223L, 223L), (221L, 227L), (57L, 63L), (163L, 165L), (103L, 121L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_17(byte x) => (x >= 99 && x <= 110) ? true : (x >= 133 && x <= 144) ? true : x == 223 ? true : (x >= 221 && x <= 227) ? true : (x >= 57 && x <= 63) ? true : (x >= 163 && x <= 165) ? true : (x >= 103 && x <= 121) ? true : false;

    private static void Check17()
    {
        foreach (long v in Probe(s_set17, 0L, 255L, true, 17))
        {
            Assert.True(byte_Ternary_17((byte)v) == InSet(s_set17, v), $"byte_Ternary_17({v}) set=[99..110] [133..144] [223..223] [221..227] [57..63] [163..165] [103..121]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set18 = [(127L, 127L), (121L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_18(sbyte x) => (x == 127 || (x >= 121 && x <= 127) || x == 127);

    private static void Check18()
    {
        foreach (long v in Probe(s_set18, (-128L), 127L, true, 18))
        {
            Assert.True(sbyte_Tree_18((sbyte)v) == InSet(s_set18, v), $"sbyte_Tree_18({v}) set=[127..127] [121..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set19 = [(-5L, 2L), (3L, 3L), (-17L, -17L), (33L, 35L), (8L, 18L), (-49L, -46L), (32L, 34L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Subtractions_19(short x) => (uint)(x - (-5)) <= 7u || x == 3 || x == (-17) || (uint)(x - 33) <= 2u || (uint)(x - 8) <= 10u || (uint)(x - (-49)) <= 3u || (uint)(x - 32) <= 2u;

    private static void Check19()
    {
        foreach (long v in Probe(s_set19, (-32768L), 32767L, true, 19))
        {
            Assert.True(short_Subtractions_19((short)v) == InSet(s_set19, v), $"short_Subtractions_19({v}) set=[-5..2] [3..3] [-17..-17] [33..35] [8..18] [-49..-46] [32..34]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set20 = [(69L, 69L), (68L, 73L), (71L, 82L), (69L, 76L), (90L, 96L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchExpr_20(ushort x) => x switch
    {
        >= 68 and <= 82 => true,
        >= 90 and <= 96 => true,
        _ => false,
    };

    private static void Check20()
    {
        foreach (long v in Probe(s_set20, 0L, 65535L, true, 20))
        {
            Assert.True(ushort_SwitchExpr_20((ushort)v) == InSet(s_set20, v), $"ushort_SwitchExpr_20({v}) set=[69..69] [68..73] [71..82] [69..76] [90..96]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set21 = [(174L, 184L), (271L, 271L), (243L, 249L), (195L, 195L), (282L, 282L), (180L, 180L), (274L, 285L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_21(int x) => x switch
    {
        >= 174 and <= 184 => true,
        195 => true,
        >= 243 and <= 249 => true,
        271 => true,
        >= 274 and <= 285 => true,
        _ => false,
    };

    private static void Check21()
    {
        foreach (long v in Probe(s_set21, (-2147483648L), 2147483647L, false, 21))
        {
            Assert.True(int_SwitchExpr_21((int)v) == InSet(s_set21, v), $"int_SwitchExpr_21({v}) set=[174..184] [271..271] [243..249] [195..195] [282..282] [180..180] [274..285]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set22 = [(6L, 9L), (9L, 20L), (65L, 90L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_IsPattern_22(uint x) => x is >= 6u and <= 9u or >= 9u and <= 20u or >= 65u and <= 90u;

    private static void Check22()
    {
        foreach (long v in Probe(s_set22, 0L, 4294967295L, false, 22))
        {
            Assert.True(uint_IsPattern_22((uint)v) == InSet(s_set22, v), $"uint_IsPattern_22({v}) set=[6..9] [9..20] [65..90]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set23 = [(14L, 14L), (-18L, -13L), (-11L, 1L), (9223372036854775798L, 9223372036854775807L), (-17L, -8L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Subtractions_23(long x) => x == 14L || (ulong)(x - (-18L)) <= 5UL || (ulong)(x - (-11L)) <= 12UL || (ulong)(x - 9223372036854775798L) <= 9UL || (ulong)(x - (-17L)) <= 9UL;

    private static void Check23()
    {
        foreach (long v in Probe(s_set23, long.MinValue, 9223372036854775807L, false, 23))
        {
            Assert.True(long_Subtractions_23((long)v) == InSet(s_set23, v), $"long_Subtractions_23({v}) set=[14..14] [-18..-13] [-11..1] [9223372036854775798..9223372036854775807] [-17..-8]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set24 = [(44L, 56L), (198L, 202L), (96L, 105L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Tree_24(char x) => (x < '\u0060' ? (x >= '\u002C' && x <= '\u0038') : (x < '\u00C6' ? (x >= '\u0060' && x <= '\u0069') : (x >= '\u00C6' && x <= '\u00CA')));

    private static void Check24()
    {
        foreach (long v in Probe(s_set24, 0L, 65535L, true, 24))
        {
            Assert.True(char_Tree_24((char)v) == InSet(s_set24, v), $"char_Tree_24({v}) set=[44..56] [198..202] [96..105]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set25 = [(255L, 255L), (254L, 254L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchExpr_25(byte x) => x switch
    {
        254 => true,
        255 => true,
        _ => false,
    };

    private static void Check25()
    {
        foreach (long v in Probe(s_set25, 0L, 255L, true, 25))
        {
            Assert.True(byte_SwitchExpr_25((byte)v) == InSet(s_set25, v), $"byte_SwitchExpr_25({v}) set=[255..255] [254..254] [255..255] [255..255] [255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set26 = [(68L, 69L), (103L, 110L), (127L, 127L), (-128L, -123L), (114L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_26(sbyte x) => x switch
    {
        >= (-128) and <= (-123) => true,
        >= 68 and <= 69 => true,
        >= 103 and <= 110 => true,
        >= 114 and <= 127 => true,
        _ => false,
    };

    private static void Check26()
    {
        foreach (long v in Probe(s_set26, (-128L), 127L, true, 26))
        {
            Assert.True(sbyte_SwitchExpr_26((sbyte)v) == InSet(s_set26, v), $"sbyte_SwitchExpr_26({v}) set=[68..69] [103..110] [127..127] [-128..-123] [114..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set27 = [(50L, 54L), (35L, 42L), (42L, 42L), (101L, 139L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Ternary_27(short x) => (x >= 50 && x <= 54) ? true : (x >= 35 && x <= 42) ? true : x == 42 ? true : (x >= 101 && x <= 139) ? true : false;

    private static void Check27()
    {
        foreach (long v in Probe(s_set27, (-32768L), 32767L, true, 27))
        {
            Assert.True(short_Ternary_27((short)v) == InSet(s_set27, v), $"short_Ternary_27({v}) set=[50..54] [35..42] [42..42] [101..139]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set28 = [(1041L, 1041L), (1003L, 1003L), (1020L, 1020L), (1043L, 1043L), (1060L, 1061L), (1066L, 1108L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_28(ushort x) => (x < 1043 ? (x < 1020 ? x == 1003 : (x < 1041 ? x == 1020 : x == 1041)) : (x < 1060 ? x == 1043 : (x < 1066 ? (x >= 1060 && x <= 1061) : (x >= 1066 && x <= 1108))));

    private static void Check28()
    {
        foreach (long v in Probe(s_set28, 0L, 65535L, true, 28))
        {
            Assert.True(ushort_Tree_28((ushort)v) == InSet(s_set28, v), $"ushort_Tree_28({v}) set=[1041..1041] [1003..1003] [1020..1020] [1043..1043] [1060..1061] [1066..1108]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set29 = [(286L, 286L), (169L, 169L), (221L, 233L), (148L, 149L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Ternary_29(int x) => x == 286 ? true : x == 169 ? true : (x >= 221 && x <= 233) ? true : (x >= 148 && x <= 149) ? true : false;

    private static void Check29()
    {
        foreach (long v in Probe(s_set29, (-2147483648L), 2147483647L, false, 29))
        {
            Assert.True(int_Ternary_29((int)v) == InSet(s_set29, v), $"int_Ternary_29({v}) set=[286..286] [169..169] [221..233] [148..149]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set30 = [(0L, 1L), (4294967230L, 4294967230L), (4294967269L, 4294967269L), (4294967250L, 4294967250L), (4294967264L, 4294967264L), (4294967241L, 4294967241L), (4294967200L, 4294967200L), (4294967257L, 4294967288L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Tree_30(uint x) => (x < 4294967250u ? (x < 4294967230u ? (x < 4294967200u ? (x >= 0u && x <= 1u) : x == 4294967200u) : (x < 4294967241u ? x == 4294967230u : x == 4294967241u)) : (x < 4294967264u ? (x < 4294967257u ? x == 4294967250u : (x >= 4294967257u && x <= 4294967288u)) : ((x >= 4294967257u && x <= 4294967288u) || x == 4294967264u || x == 4294967269u)));

    private static void Check30()
    {
        foreach (long v in Probe(s_set30, 0L, 4294967295L, false, 30))
        {
            Assert.True(uint_Tree_30((uint)v) == InSet(s_set30, v), $"uint_Tree_30({v}) set=[0..1] [4294967230..4294967230] [4294967269..4294967269] [4294967250..4294967250] [4294967264..4294967264] [4294967241..4294967241] [4294967200..4294967200] [4294967257..4294967288]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set31 = [(227L, 227L), (150L, 156L), (112L, 115L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_31(long x) => !(x is 227L or >= 150L and <= 156L or >= 112L and <= 115L);

    private static void Check31()
    {
        foreach (long v in Probe(s_set31, long.MinValue, 9223372036854775807L, false, 31))
        {
            Assert.True(long_Negated_31((long)v) == !InSet(s_set31, v), $"long_Negated_31({v}) set=[227..227] [150..156] [112..115]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set32 = [(133L, 145L), (131L, 132L), (217L, 217L), (133L, 166L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchStmt_32(char x)
    {
        switch (x)
        {
            case '\u0083':
            case '\u0084':
            case >= '\u0085' and <= '\u00A6':
            case '\u00D9':
                return true;
            default:
                return false;
        }
    }

    private static void Check32()
    {
        foreach (long v in Probe(s_set32, 0L, 65535L, true, 32))
        {
            Assert.True(char_SwitchStmt_32((char)v) == InSet(s_set32, v), $"char_SwitchStmt_32({v}) set=[133..145] [131..132] [217..217] [133..166]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set33 = [(73L, 76L), (116L, 127L), (146L, 149L), (169L, 172L), (251L, 251L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_IsPattern_33(byte x) => x is >= 73 and <= 76 or >= 116 and <= 127 or >= 146 and <= 149 or >= 169 and <= 172 or 251;

    private static void Check33()
    {
        foreach (long v in Probe(s_set33, 0L, 255L, true, 33))
        {
            Assert.True(byte_IsPattern_33((byte)v) == InSet(s_set33, v), $"byte_IsPattern_33({v}) set=[73..76] [116..127] [146..149] [169..172] [251..251]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set34 = [(-116L, -107L), (-94L, -94L), (-103L, -103L), (-119L, -119L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_34(sbyte x) => x switch
    {
        (-119) => true,
        >= (-116) and <= (-107) => true,
        (-103) => true,
        (-94) => true,
        _ => false,
    };

    private static void Check34()
    {
        foreach (long v in Probe(s_set34, (-128L), 127L, true, 34))
        {
            Assert.True(sbyte_SwitchExpr_34((sbyte)v) == InSet(s_set34, v), $"sbyte_SwitchExpr_34({v}) set=[-116..-107] [-94..-94] [-103..-103] [-119..-119]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set35 = [(48L, 48L), (164L, 165L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchExpr_35(short x) => x switch
    {
        48 => true,
        >= 164 and <= 165 => true,
        _ => false,
    };

    private static void Check35()
    {
        foreach (long v in Probe(s_set35, (-32768L), 32767L, true, 35))
        {
            Assert.True(short_SwitchExpr_35((short)v) == InSet(s_set35, v), $"short_SwitchExpr_35({v}) set=[48..48] [164..165]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set36 = [(225L, 229L), (136L, 136L), (146L, 146L), (210L, 211L), (205L, 216L), (188L, 195L), (168L, 179L), (165L, 196L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchStmt_36(ushort x)
    {
        switch (x)
        {
            case 136:
            case 146:
            case >= 165 and <= 196:
            case 205:
            case 206:
            case 207:
            case 208:
            case 209:
            case 210:
            case 211:
            case 212:
            case 213:
            case 214:
            case 215:
            case 216:
            case 225:
            case 226:
            case 227:
            case 228:
            case 229:
                return true;
            default:
                return false;
        }
    }

    private static void Check36()
    {
        foreach (long v in Probe(s_set36, 0L, 65535L, true, 36))
        {
            Assert.True(ushort_SwitchStmt_36((ushort)v) == InSet(s_set36, v), $"ushort_SwitchStmt_36({v}) set=[225..229] [136..136] [146..146] [210..211] [205..216] [188..195] [168..179] [165..196]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set37 = [(2147483639L, 2147483647L), (12370L, 12370L), (12374L, 12385L), (12357L, 12366L), (12415L, 12447L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_OrChain_37(int x) => (x >= 2147483639 && x <= 2147483647) || x == 12370 || (x >= 12374 && x <= 12385) || (x >= 12357 && x <= 12366) || (x >= 12415 && x <= 12447);

    private static void Check37()
    {
        foreach (long v in Probe(s_set37, (-2147483648L), 2147483647L, false, 37))
        {
            Assert.True(int_OrChain_37((int)v) == InSet(s_set37, v), $"int_OrChain_37({v}) set=[2147483639..2147483647] [12370..12370] [12374..12385] [12357..12366] [12415..12447]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set38 = [(80L, 87L), (76L, 76L), (75L, 80L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Branches_38(uint x)
    {
        if ((x >= 80u && x <= 87u))
        {
            return true;
        }

        if (x == 76u)
        {
            return true;
        }

        if ((x >= 75u && x <= 80u))
        {
            return true;
        }

        return false;
    }

    private static void Check38()
    {
        foreach (long v in Probe(s_set38, 0L, 4294967295L, false, 38))
        {
            Assert.True(uint_Branches_38((uint)v) == InSet(s_set38, v), $"uint_Branches_38({v}) set=[80..87] [76..76] [75..80]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set39 = [(94L, 94L), (128L, 134L), (107L, 107L), (69L, 73L), (121L, 130L), (87L, 89L), (58L, 60L), (113L, 136L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Subtractions_39(long x) => x == 94L || (ulong)(x - 128L) <= 6UL || x == 107L || (ulong)(x - 69L) <= 4UL || (ulong)(x - 121L) <= 9UL || (ulong)(x - 87L) <= 2UL || (ulong)(x - 58L) <= 2UL || (ulong)(x - 113L) <= 23UL;

    private static void Check39()
    {
        foreach (long v in Probe(s_set39, long.MinValue, 9223372036854775807L, false, 39))
        {
            Assert.True(long_Subtractions_39((long)v) == InSet(s_set39, v), $"long_Subtractions_39({v}) set=[94..94] [128..134] [107..107] [69..73] [121..130] [87..89] [58..60] [113..136]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set40 = [(139L, 139L), (147L, 147L), (146L, 146L), (65532L, 65535L), (165L, 185L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_40(char x) => x == '\u008B' || x == '\u0093' || x == '\u0092' || (x >= '\uFFFC' && x <= '\uFFFF') || (x >= '\u00A5' && x <= '\u00B9');

    private static void Check40()
    {
        foreach (long v in Probe(s_set40, 0L, 65535L, true, 40))
        {
            Assert.True(char_OrChain_40((char)v) == InSet(s_set40, v), $"char_OrChain_40({v}) set=[139..139] [147..147] [146..146] [65532..65535] [165..185]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set41 = [(255L, 255L), (72L, 72L), (79L, 79L), (90L, 92L), (63L, 63L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchStmt_41(byte x)
    {
        switch (x)
        {
            case 63:
            case 72:
            case 79:
            case 90:
            case 91:
            case 92:
            case 255:
                return true;
            default:
                return false;
        }
    }

    private static void Check41()
    {
        foreach (long v in Probe(s_set41, 0L, 255L, true, 41))
        {
            Assert.True(byte_SwitchStmt_41((byte)v) == InSet(s_set41, v), $"byte_SwitchStmt_41({v}) set=[255..255] [72..72] [79..79] [90..92] [63..63]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set42 = [(71L, 71L), (53L, 54L), (50L, 61L), (55L, 66L), (85L, 86L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Negated_42(sbyte x) => !(x is 71 or >= 53 and <= 54 or >= 50 and <= 61 or >= 55 and <= 66 or >= 85 and <= 86);

    private static void Check42()
    {
        foreach (long v in Probe(s_set42, (-128L), 127L, true, 42))
        {
            Assert.True(sbyte_Negated_42((sbyte)v) == !InSet(s_set42, v), $"sbyte_Negated_42({v}) set=[71..71] [53..54] [50..61] [55..66] [85..86]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set43 = [(155L, 155L), (27L, 29L), (156L, 168L), (139L, 139L), (110L, 110L), (46L, 51L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Flipped_43(short x) => x == 155 || (27 <= x && 29 >= x) || (156 <= x && x < 169) || 139 == x || x == 110 || (x > 45 && 51 >= x);

    private static void Check43()
    {
        foreach (long v in Probe(s_set43, (-32768L), 32767L, true, 43))
        {
            Assert.True(short_Flipped_43((short)v) == InSet(s_set43, v), $"short_Flipped_43({v}) set=[155..155] [27..29] [156..168] [139..139] [110..110] [46..51]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set44 = [(120L, 120L), (124L, 124L), (125L, 125L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Ternary_44(ushort x) => x == 120 ? true : x == 124 ? true : x == 125 ? true : false;

    private static void Check44()
    {
        foreach (long v in Probe(s_set44, 0L, 65535L, true, 44))
        {
            Assert.True(ushort_Ternary_44((ushort)v) == InSet(s_set44, v), $"ushort_Ternary_44({v}) set=[120..120] [124..124] [125..125]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set45 = [(-5L, -5L), (-1L, 4L), (2147483647L, 2147483647L), (-7L, -7L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Ternary_45(int x) => x == (-5) ? true : (x >= (-1) && x <= 4) ? true : x == 2147483647 ? true : x == (-7) ? true : false;

    private static void Check45()
    {
        foreach (long v in Probe(s_set45, (-2147483648L), 2147483647L, false, 45))
        {
            Assert.True(int_Ternary_45((int)v) == InSet(s_set45, v), $"int_Ternary_45({v}) set=[-5..-5] [-1..4] [2147483647..2147483647] [-7..-7]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set46 = [(131L, 141L), (115L, 115L), (112L, 120L), (103L, 105L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Tree_46(uint x) => (x < 115u ? (x < 112u ? (x >= 103u && x <= 105u) : (x >= 112u && x <= 120u)) : ((x >= 112u && x <= 120u) || x == 115u || (x >= 131u && x <= 141u)));

    private static void Check46()
    {
        foreach (long v in Probe(s_set46, 0L, 4294967295L, false, 46))
        {
            Assert.True(uint_Tree_46((uint)v) == InSet(s_set46, v), $"uint_Tree_46({v}) set=[131..141] [115..115] [112..120] [103..105]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set47 = [(272L, 277L), (149L, 156L), (114L, 114L), (119L, 131L), (159L, 167L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchExpr_47(long x) => x switch
    {
        114L => true,
        >= 119L and <= 131L => true,
        >= 149L and <= 156L => true,
        >= 159L and <= 167L => true,
        >= 272L and <= 277L => true,
        _ => false,
    };

    private static void Check47()
    {
        foreach (long v in Probe(s_set47, long.MinValue, 9223372036854775807L, false, 47))
        {
            Assert.True(long_SwitchExpr_47((long)v) == InSet(s_set47, v), $"long_SwitchExpr_47({v}) set=[272..277] [149..156] [114..114] [119..131] [159..167]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set48 = [(65535L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_48(char x)
    {
        if (x == '\uFFFF')
        {
            return true;
        }

        if (x == '\uFFFF')
        {
            return true;
        }

        if (x == '\uFFFF')
        {
            return true;
        }

        if (x == '\uFFFF')
        {
            return true;
        }

        if (x == '\uFFFF')
        {
            return true;
        }

        return false;
    }

    private static void Check48()
    {
        foreach (long v in Probe(s_set48, 0L, 65535L, true, 48))
        {
            Assert.True(char_Branches_48((char)v) == InSet(s_set48, v), $"char_Branches_48({v}) set=[65535..65535] [65535..65535] [65535..65535] [65535..65535] [65535..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set49 = [(44L, 56L), (58L, 70L), (9L, 12L), (20L, 20L), (71L, 79L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Negated_49(byte x) => !(x is >= 44 and <= 56 or >= 58 and <= 70 or >= 9 and <= 12 or 20 or >= 71 and <= 79);

    private static void Check49()
    {
        foreach (long v in Probe(s_set49, 0L, 255L, true, 49))
        {
            Assert.True(byte_Negated_49((byte)v) == !InSet(s_set49, v), $"byte_Negated_49({v}) set=[44..56] [58..70] [9..12] [20..20] [71..79]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set50 = [(-85L, -84L), (-100L, -90L), (-64L, -52L), (-86L, -86L), (-111L, -111L), (-113L, -113L), (-66L, -66L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Ternary_50(sbyte x) => (x >= (-85) && x <= (-84)) ? true : (x >= (-100) && x <= (-90)) ? true : (x >= (-64) && x <= (-52)) ? true : x == (-86) ? true : x == (-111) ? true : x == (-113) ? true : x == (-66) ? true : false;

    private static void Check50()
    {
        foreach (long v in Probe(s_set50, (-128L), 127L, true, 50))
        {
            Assert.True(sbyte_Ternary_50((sbyte)v) == InSet(s_set50, v), $"sbyte_Ternary_50({v}) set=[-85..-84] [-100..-90] [-64..-52] [-86..-86] [-111..-111] [-113..-113] [-66..-66]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set51 = [(1035L, 1044L), (1131L, 1133L), (1064L, 1078L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Branches_51(short x)
    {
        if ((x >= 1035 && x <= 1044))
        {
            return true;
        }

        if ((x >= 1131 && x <= 1133))
        {
            return true;
        }

        if ((x >= 1064 && x <= 1078))
        {
            return true;
        }

        return false;
    }

    private static void Check51()
    {
        foreach (long v in Probe(s_set51, (-32768L), 32767L, true, 51))
        {
            Assert.True(short_Branches_51((short)v) == InSet(s_set51, v), $"short_Branches_51({v}) set=[1035..1044] [1131..1133] [1064..1078]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set52 = [(83L, 83L), (68L, 78L), (59L, 65L), (65525L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_52(ushort x) => (x < 83 ? (x < 68 ? (x >= 59 && x <= 65) : (x >= 68 && x <= 78)) : (x < 65525 ? x == 83 : (x >= 65525 && x <= 65535)));

    private static void Check52()
    {
        foreach (long v in Probe(s_set52, 0L, 65535L, true, 52))
        {
            Assert.True(ushort_Tree_52((ushort)v) == InSet(s_set52, v), $"ushort_Tree_52({v}) set=[83..83] [68..78] [59..65] [65525..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set53 = [(-38L, -28L), (-22L, -22L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Ternary_53(int x) => (x >= (-38) && x <= (-28)) ? true : x == (-22) ? true : false;

    private static void Check53()
    {
        foreach (long v in Probe(s_set53, (-2147483648L), 2147483647L, false, 53))
        {
            Assert.True(int_Ternary_53((int)v) == InSet(s_set53, v), $"int_Ternary_53({v}) set=[-38..-28] [-22..-22]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set54 = [(101L, 101L), (85L, 90L), (4294967295L, 4294967295L), (92L, 97L), (69L, 78L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Flipped_54(uint x) => x == 101u || (x >= 85u && 90u >= x) || x == 4294967295u || (x > 91u && x <= 97u) || (x >= 69u && x <= 78u);

    private static void Check54()
    {
        foreach (long v in Probe(s_set54, 0L, 4294967295L, false, 54))
        {
            Assert.True(uint_Flipped_54((uint)v) == InSet(s_set54, v), $"uint_Flipped_54({v}) set=[101..101] [85..90] [4294967295..4294967295] [92..97] [69..78]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set55 = [(9223372036854775719L, 9223372036854775719L), (9223372036854775713L, 9223372036854775713L), (9223372036854775713L, 9223372036854775714L), (9223372036854775708L, 9223372036854775708L), (9223372036854775707L, 9223372036854775707L), (9223372036854775745L, 9223372036854775747L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Branches_55(long x)
    {
        if (x == 9223372036854775719L)
        {
            return true;
        }

        if (x == 9223372036854775713L)
        {
            return true;
        }

        if ((x >= 9223372036854775713L && x <= 9223372036854775714L))
        {
            return true;
        }

        if (x == 9223372036854775708L)
        {
            return true;
        }

        if (x == 9223372036854775707L)
        {
            return true;
        }

        if ((x >= 9223372036854775745L && x <= 9223372036854775747L))
        {
            return true;
        }

        return false;
    }

    private static void Check55()
    {
        foreach (long v in Probe(s_set55, long.MinValue, 9223372036854775807L, false, 55))
        {
            Assert.True(long_Branches_55((long)v) == InSet(s_set55, v), $"long_Branches_55({v}) set=[9223372036854775719..9223372036854775719] [9223372036854775713..9223372036854775713] [9223372036854775713..9223372036854775714] [9223372036854775708..9223372036854775708] [9223372036854775707..9223372036854775707] [9223372036854775745..9223372036854775747]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set56 = [(100L, 100L), (72L, 72L), (90L, 93L), (92L, 92L), (95L, 102L), (69L, 81L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_56(char x) => !(x is '\u0064' or '\u0048' or >= '\u005A' and <= '\u005D' or '\u005C' or >= '\u005F' and <= '\u0066' or >= '\u0045' and <= '\u0051');

    private static void Check56()
    {
        foreach (long v in Probe(s_set56, 0L, 65535L, true, 56))
        {
            Assert.True(char_Negated_56((char)v) == !InSet(s_set56, v), $"char_Negated_56({v}) set=[100..100] [72..72] [90..93] [92..92] [95..102] [69..81]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set57 = [(255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Flipped_57(byte x) => x == 255 || x == 255 || x == 255 || 255 == x || 255 == x;

    private static void Check57()
    {
        foreach (long v in Probe(s_set57, 0L, 255L, true, 57))
        {
            Assert.True(byte_Flipped_57((byte)v) == InSet(s_set57, v), $"byte_Flipped_57({v}) set=[255..255] [255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set58 = [(127L, 127L), (-128L, -128L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Subtractions_58(sbyte x) => x == 127 || x == (-128);

    private static void Check58()
    {
        foreach (long v in Probe(s_set58, (-128L), 127L, true, 58))
        {
            Assert.True(sbyte_Subtractions_58((sbyte)v) == InSet(s_set58, v), $"sbyte_Subtractions_58({v}) set=[127..127] [-128..-128]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set59 = [(225L, 232L), (124L, 132L), (211L, 213L), (282L, 282L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_OrChain_59(short x) => (x >= 225 && x <= 232) || (x >= 124 && x <= 132) || (x >= 211 && x <= 213) || x == 282;

    private static void Check59()
    {
        foreach (long v in Probe(s_set59, (-32768L), 32767L, true, 59))
        {
            Assert.True(short_OrChain_59((short)v) == InSet(s_set59, v), $"short_OrChain_59({v}) set=[225..232] [124..132] [211..213] [282..282]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set60 = [(65451L, 65458L), (65435L, 65444L), (65442L, 65442L), (65435L, 65439L), (65439L, 65444L), (65441L, 65442L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Subtractions_60(ushort x) => (uint)(x - 65451) <= 7u || (uint)(x - 65435) <= 9u || x == 65442 || (uint)(x - 65435) <= 4u || (uint)(x - 65439) <= 5u || (uint)(x - 65441) <= 1u;

    private static void Check60()
    {
        foreach (long v in Probe(s_set60, 0L, 65535L, true, 60))
        {
            Assert.True(ushort_Subtractions_60((ushort)v) == InSet(s_set60, v), $"ushort_Subtractions_60({v}) set=[65451..65458] [65435..65444] [65442..65442] [65435..65439] [65439..65444] [65441..65442]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set61 = [(4L, 10L), (22L, 22L), (60L, 63L), (40L, 40L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Branches_61(int x)
    {
        if ((x >= 4 && x <= 10))
        {
            return true;
        }

        if (x == 22)
        {
            return true;
        }

        if ((x >= 60 && x <= 63))
        {
            return true;
        }

        if (x == 40)
        {
            return true;
        }

        return false;
    }

    private static void Check61()
    {
        foreach (long v in Probe(s_set61, (-2147483648L), 2147483647L, false, 61))
        {
            Assert.True(int_Branches_61((int)v) == InSet(s_set61, v), $"int_Branches_61({v}) set=[4..10] [22..22] [60..63] [40..40]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set62 = [(12L, 12L), (17L, 25L), (2L, 12L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Negated_62(uint x) => !(x is 12u or >= 17u and <= 25u or >= 2u and <= 12u);

    private static void Check62()
    {
        foreach (long v in Probe(s_set62, 0L, 4294967295L, false, 62))
        {
            Assert.True(uint_Negated_62((uint)v) == !InSet(s_set62, v), $"uint_Negated_62({v}) set=[12..12] [17..25] [2..12]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set63 = [(147L, 152L), (161L, 164L), (111L, 117L), (116L, 123L), (102L, 102L), (162L, 162L), (123L, 130L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchExpr_63(long x) => x switch
    {
        102L => true,
        >= 111L and <= 130L => true,
        >= 147L and <= 152L => true,
        >= 161L and <= 164L => true,
        _ => false,
    };

    private static void Check63()
    {
        foreach (long v in Probe(s_set63, long.MinValue, 9223372036854775807L, false, 63))
        {
            Assert.True(long_SwitchExpr_63((long)v) == InSet(s_set63, v), $"long_SwitchExpr_63({v}) set=[147..152] [161..164] [111..117] [116..123] [102..102] [162..162] [123..130]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set64 = [(60L, 60L), (61L, 72L), (33L, 44L), (47L, 47L), (52L, 56L), (66L, 70L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_64(char x) => x == '\u003C' || (x >= '\u003D' && x <= '\u0048') || (x >= '\u0021' && x <= '\u002C') || x == '\u002F' || (x >= '\u0034' && x <= '\u0038') || (x >= '\u0042' && x <= '\u0046');

    private static void Check64()
    {
        foreach (long v in Probe(s_set64, 0L, 65535L, true, 64))
        {
            Assert.True(char_OrChain_64((char)v) == InSet(s_set64, v), $"char_OrChain_64({v}) set=[60..60] [61..72] [33..44] [47..47] [52..56] [66..70]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set65 = [(255L, 255L), (254L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_65(byte x) => x == 255 ? true : (x >= 254 && x <= 255) ? true : false;

    private static void Check65()
    {
        foreach (long v in Probe(s_set65, 0L, 255L, true, 65))
        {
            Assert.True(byte_Ternary_65((byte)v) == InSet(s_set65, v), $"byte_Ternary_65({v}) set=[255..255] [254..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set66 = [(65L, 65L), (-128L, -128L), (60L, 61L), (84L, 96L), (119L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Ternary_66(sbyte x) => x == 65 ? true : x == (-128) ? true : (x >= 60 && x <= 61) ? true : (x >= 84 && x <= 96) ? true : (x >= 119 && x <= 127) ? true : false;

    private static void Check66()
    {
        foreach (long v in Probe(s_set66, (-128L), 127L, true, 66))
        {
            Assert.True(sbyte_Ternary_66((sbyte)v) == InSet(s_set66, v), $"sbyte_Ternary_66({v}) set=[65..65] [-128..-128] [60..61] [84..96] [119..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set67 = [(217L, 220L), (131L, 132L), (-32768L, -32761L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Subtractions_67(short x) => (uint)(x - 217) <= 3u || (uint)(x - 131) <= 1u || (uint)(x - (-32768)) <= 7u;

    private static void Check67()
    {
        foreach (long v in Probe(s_set67, (-32768L), 32767L, true, 67))
        {
            Assert.True(short_Subtractions_67((short)v) == InSet(s_set67, v), $"short_Subtractions_67({v}) set=[217..220] [131..132] [-32768..-32761]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set68 = [(47L, 50L), (0L, 9L), (141L, 141L), (68L, 73L), (83L, 83L), (146L, 146L), (94L, 118L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Flipped_68(ushort x) => (x > 46 && x < 51) || (x >= 0 && 9 >= x) || 141 == x || (x >= 68 && x < 74) || 83 == x || 146 == x || (x >= 94 && x < 119);

    private static void Check68()
    {
        foreach (long v in Probe(s_set68, 0L, 65535L, true, 68))
        {
            Assert.True(ushort_Flipped_68((ushort)v) == InSet(s_set68, v), $"ushort_Flipped_68({v}) set=[47..50] [0..9] [141..141] [68..73] [83..83] [146..146] [94..118]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set69 = [(-2147483648L, -2147483648L), (96L, 99L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_69(int x) => x switch
    {
        int.MinValue => true,
        >= 96 and <= 99 => true,
        _ => false,
    };

    private static void Check69()
    {
        foreach (long v in Probe(s_set69, (-2147483648L), 2147483647L, false, 69))
        {
            Assert.True(int_SwitchExpr_69((int)v) == InSet(s_set69, v), $"int_SwitchExpr_69({v}) set=[-2147483648..-2147483648] [96..99]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set70 = [(297L, 297L), (231L, 231L), (264L, 264L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_70(uint x) => x switch
    {
        231u => true,
        264u => true,
        297u => true,
        _ => false,
    };

    private static void Check70()
    {
        foreach (long v in Probe(s_set70, 0L, 4294967295L, false, 70))
        {
            Assert.True(uint_SwitchExpr_70((uint)v) == InSet(s_set70, v), $"uint_SwitchExpr_70({v}) set=[297..297] [231..231] [264..264]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set71 = [(86L, 88L), (63L, 66L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchExpr_71(long x) => x switch
    {
        >= 63L and <= 66L => true,
        >= 86L and <= 88L => true,
        _ => false,
    };

    private static void Check71()
    {
        foreach (long v in Probe(s_set71, long.MinValue, 9223372036854775807L, false, 71))
        {
            Assert.True(long_SwitchExpr_71((long)v) == InSet(s_set71, v), $"long_SwitchExpr_71({v}) set=[86..88] [63..66]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set72 = [(50L, 56L), (50L, 50L), (60L, 62L), (95L, 100L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_72(char x)
    {
        if ((x >= '\u0032' && x <= '\u0038'))
        {
            return true;
        }

        if (x == '\u0032')
        {
            return true;
        }

        if ((x >= '\u003C' && x <= '\u003E'))
        {
            return true;
        }

        if ((x >= '\u005F' && x <= '\u0064'))
        {
            return true;
        }

        return false;
    }

    private static void Check72()
    {
        foreach (long v in Probe(s_set72, 0L, 65535L, true, 72))
        {
            Assert.True(char_Branches_72((char)v) == InSet(s_set72, v), $"char_Branches_72({v}) set=[50..56] [50..50] [60..62] [95..100]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set73 = [(101L, 101L), (104L, 111L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_73(byte x) => x == 101 ? true : (x >= 104 && x <= 111) ? true : false;

    private static void Check73()
    {
        foreach (long v in Probe(s_set73, 0L, 255L, true, 73))
        {
            Assert.True(byte_Ternary_73((byte)v) == InSet(s_set73, v), $"byte_Ternary_73({v}) set=[101..101] [104..111]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set74 = [(-39L, -38L), (35L, 47L), (-34L, -33L), (-42L, -42L), (15L, 46L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Subtractions_74(sbyte x) => (uint)(x - (-39)) <= 1u || (uint)(x - 35) <= 12u || (uint)(x - (-34)) <= 1u || x == (-42) || (uint)(x - 15) <= 31u;

    private static void Check74()
    {
        foreach (long v in Probe(s_set74, (-128L), 127L, true, 74))
        {
            Assert.True(sbyte_Subtractions_74((sbyte)v) == InSet(s_set74, v), $"sbyte_Subtractions_74({v}) set=[-39..-38] [35..47] [-34..-33] [-42..-42] [15..46]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set75 = [(99L, 99L), (8L, 19L), (-32768L, -32765L), (5L, 5L), (37L, 37L), (10L, 13L), (63L, 78L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_75(short x) => !(x is 99 or >= 8 and <= 19 or >= (-32768) and <= (-32765) or 5 or 37 or >= 10 and <= 13 or >= 63 and <= 78);

    private static void Check75()
    {
        foreach (long v in Probe(s_set75, (-32768L), 32767L, true, 75))
        {
            Assert.True(short_Negated_75((short)v) == !InSet(s_set75, v), $"short_Negated_75({v}) set=[99..99] [8..19] [-32768..-32765] [5..5] [37..37] [10..13] [63..78]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set76 = [(211L, 211L), (255L, 255L), (279L, 279L), (275L, 275L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_76(ushort x) => (x < 275 ? (x < 255 ? x == 211 : x == 255) : (x < 279 ? x == 275 : x == 279));

    private static void Check76()
    {
        foreach (long v in Probe(s_set76, 0L, 65535L, true, 76))
        {
            Assert.True(ushort_Tree_76((ushort)v) == InSet(s_set76, v), $"ushort_Tree_76({v}) set=[211..211] [255..255] [279..279] [275..275]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set77 = [(-2147483637L, -2147483637L), (-2147483611L, -2147483610L), (-2147483612L, -2147483603L), (-2147483633L, -2147483629L), (-2147483609L, -2147483609L), (-2147483640L, -2147483640L), (-2147483586L, -2147483572L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_77(int x) => x is (-2147483637) or >= (-2147483611) and <= (-2147483610) or >= (-2147483612) and <= (-2147483603) or >= (-2147483633) and <= (-2147483629) or (-2147483609) or (-2147483640) or >= (-2147483586) and <= (-2147483572);

    private static void Check77()
    {
        foreach (long v in Probe(s_set77, (-2147483648L), 2147483647L, false, 77))
        {
            Assert.True(int_IsPattern_77((int)v) == InSet(s_set77, v), $"int_IsPattern_77({v}) set=[-2147483637..-2147483637] [-2147483611..-2147483610] [-2147483612..-2147483603] [-2147483633..-2147483629] [-2147483609..-2147483609] [-2147483640..-2147483640] [-2147483586..-2147483572]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set78 = [(17L, 20L), (31L, 34L), (34L, 36L), (12L, 15L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_OrChain_78(uint x) => (x >= 17u && x <= 20u) || (x >= 31u && x <= 34u) || (x >= 34u && x <= 36u) || (x >= 12u && x <= 15u);

    private static void Check78()
    {
        foreach (long v in Probe(s_set78, 0L, 4294967295L, false, 78))
        {
            Assert.True(uint_OrChain_78((uint)v) == InSet(s_set78, v), $"uint_OrChain_78({v}) set=[17..20] [31..34] [34..36] [12..15]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set79 = [(12L, 12L), (34L, 34L), (9L, 19L), (-19L, -12L), (-19L, -12L), (-9223372036854775808L, -9223372036854775808L), (57L, 57L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Flipped_79(long x) => x == 12L || x == 34L || (9L <= x && x < 20L) || (x > (-20L) && (-12L) >= x) || (x > (-20L) && (-12L) >= x) || x == long.MinValue || 57L == x;

    private static void Check79()
    {
        foreach (long v in Probe(s_set79, long.MinValue, 9223372036854775807L, false, 79))
        {
            Assert.True(long_Flipped_79((long)v) == InSet(s_set79, v), $"long_Flipped_79({v}) set=[12..12] [34..34] [9..19] [-19..-12] [-19..-12] [-9223372036854775808..-9223372036854775808] [57..57]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set80 = [(45L, 45L), (47L, 47L), (42L, 45L), (35L, 38L), (35L, 35L), (44L, 46L), (34L, 46L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_80(char x) => x == '\u002D' || x == '\u002F' || (x >= '\u002A' && x <= '\u002D') || (x >= '\u0023' && x <= '\u0026') || x == '\u0023' || (x >= '\u002C' && x <= '\u002E') || (x >= '\u0022' && x <= '\u002E');

    private static void Check80()
    {
        foreach (long v in Probe(s_set80, 0L, 65535L, true, 80))
        {
            Assert.True(char_OrChain_80((char)v) == InSet(s_set80, v), $"char_OrChain_80({v}) set=[45..45] [47..47] [42..45] [35..38] [35..35] [44..46] [34..46]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set81 = [(42L, 42L), (36L, 36L), (94L, 106L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Negated_81(byte x) => !(x is 42 or 36 or >= 94 and <= 106);

    private static void Check81()
    {
        foreach (long v in Probe(s_set81, 0L, 255L, true, 81))
        {
            Assert.True(byte_Negated_81((byte)v) == !InSet(s_set81, v), $"byte_Negated_81({v}) set=[42..42] [36..36] [94..106]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set82 = [(127L, 127L), (127L, 127L), (116L, 122L), (68L, 78L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_82(sbyte x) => (x < 127 ? (x < 116 ? (x >= 68 && x <= 78) : (x >= 116 && x <= 122)) : (x == 127 || x == 127));

    private static void Check82()
    {
        foreach (long v in Probe(s_set82, (-128L), 127L, true, 82))
        {
            Assert.True(sbyte_Tree_82((sbyte)v) == InSet(s_set82, v), $"sbyte_Tree_82({v}) set=[127..127] [127..127] [116..122] [68..78]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set83 = [(83L, 93L), (58L, 59L), (30L, 34L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Tree_83(short x) => (x < 58 ? (x >= 30 && x <= 34) : (x < 83 ? (x >= 58 && x <= 59) : (x >= 83 && x <= 93)));

    private static void Check83()
    {
        foreach (long v in Probe(s_set83, (-32768L), 32767L, true, 83))
        {
            Assert.True(short_Tree_83((short)v) == InSet(s_set83, v), $"short_Tree_83({v}) set=[83..93] [58..59] [30..34]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set84 = [(151L, 154L), (107L, 107L), (110L, 119L), (126L, 133L), (124L, 124L), (140L, 140L), (110L, 121L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Ternary_84(ushort x) => (x >= 151 && x <= 154) ? true : x == 107 ? true : (x >= 110 && x <= 119) ? true : (x >= 126 && x <= 133) ? true : x == 124 ? true : x == 140 ? true : (x >= 110 && x <= 121) ? true : false;

    private static void Check84()
    {
        foreach (long v in Probe(s_set84, 0L, 65535L, true, 84))
        {
            Assert.True(ushort_Ternary_84((ushort)v) == InSet(s_set84, v), $"ushort_Ternary_84({v}) set=[151..154] [107..107] [110..119] [126..133] [124..124] [140..140] [110..121]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set85 = [(2147483647L, 2147483647L), (48L, 53L), (45L, 45L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Branches_85(int x)
    {
        if (x == 2147483647)
        {
            return true;
        }

        if ((x >= 48 && x <= 53))
        {
            return true;
        }

        if (x == 45)
        {
            return true;
        }

        return false;
    }

    private static void Check85()
    {
        foreach (long v in Probe(s_set85, (-2147483648L), 2147483647L, false, 85))
        {
            Assert.True(int_Branches_85((int)v) == InSet(s_set85, v), $"int_Branches_85({v}) set=[2147483647..2147483647] [48..53] [45..45]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set86 = [(12385L, 12392L), (12367L, 12367L), (12391L, 12399L), (12383L, 12386L), (12421L, 12429L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Ternary_86(uint x) => (x >= 12385u && x <= 12392u) ? true : x == 12367u ? true : (x >= 12391u && x <= 12399u) ? true : (x >= 12383u && x <= 12386u) ? true : (x >= 12421u && x <= 12429u) ? true : false;

    private static void Check86()
    {
        foreach (long v in Probe(s_set86, 0L, 4294967295L, false, 86))
        {
            Assert.True(uint_Ternary_86((uint)v) == InSet(s_set86, v), $"uint_Ternary_86({v}) set=[12385..12392] [12367..12367] [12391..12399] [12383..12386] [12421..12429]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set87 = [(147L, 148L), (86L, 86L), (9223372036854775807L, 9223372036854775807L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_87(long x) => !(x is >= 147L and <= 148L or 86L or 9223372036854775807L);

    private static void Check87()
    {
        foreach (long v in Probe(s_set87, long.MinValue, 9223372036854775807L, false, 87))
        {
            Assert.True(long_Negated_87((long)v) == !InSet(s_set87, v), $"long_Negated_87({v}) set=[147..148] [86..86] [9223372036854775807..9223372036854775807]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set88 = [(43L, 45L), (141L, 152L), (140L, 143L), (102L, 120L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchStmt_88(char x)
    {
        switch (x)
        {
            case '\u002B':
            case '\u002C':
            case '\u002D':
            case >= '\u0066' and <= '\u0078':
            case '\u008C':
            case '\u008D':
            case '\u008E':
            case '\u008F':
            case '\u0090':
            case '\u0091':
            case '\u0092':
            case '\u0093':
            case '\u0094':
            case '\u0095':
            case '\u0096':
            case '\u0097':
            case '\u0098':
                return true;
            default:
                return false;
        }
    }

    private static void Check88()
    {
        foreach (long v in Probe(s_set88, 0L, 65535L, true, 88))
        {
            Assert.True(char_SwitchStmt_88((char)v) == InSet(s_set88, v), $"char_SwitchStmt_88({v}) set=[43..45] [141..152] [140..143] [102..120]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set89 = [(0L, 0L), (54L, 64L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_OrChain_89(byte x) => x == 0 || (x >= 54 && x <= 64);

    private static void Check89()
    {
        foreach (long v in Probe(s_set89, 0L, 255L, true, 89))
        {
            Assert.True(byte_OrChain_89((byte)v) == InSet(s_set89, v), $"byte_OrChain_89({v}) set=[0..0] [54..64]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set90 = [(115L, 115L), (115L, 115L), (112L, 123L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_90(sbyte x) => x switch
    {
        >= 112 and <= 123 => true,
        _ => false,
    };

    private static void Check90()
    {
        foreach (long v in Probe(s_set90, (-128L), 127L, true, 90))
        {
            Assert.True(sbyte_SwitchExpr_90((sbyte)v) == InSet(s_set90, v), $"sbyte_SwitchExpr_90({v}) set=[115..115] [115..115] [112..123]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set91 = [(-32710L, -32705L), (-32698L, -32698L), (-32585L, -32585L), (-32654L, -32645L), (-32732L, -32728L), (-32611L, -32608L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Branches_91(short x)
    {
        if ((x >= (-32710) && x <= (-32705)))
        {
            return true;
        }

        if (x == (-32698))
        {
            return true;
        }

        if (x == (-32585))
        {
            return true;
        }

        if ((x >= (-32654) && x <= (-32645)))
        {
            return true;
        }

        if ((x >= (-32732) && x <= (-32728)))
        {
            return true;
        }

        if ((x >= (-32611) && x <= (-32608)))
        {
            return true;
        }

        return false;
    }

    private static void Check91()
    {
        foreach (long v in Probe(s_set91, (-32768L), 32767L, true, 91))
        {
            Assert.True(short_Branches_91((short)v) == InSet(s_set91, v), $"short_Branches_91({v}) set=[-32710..-32705] [-32698..-32698] [-32585..-32585] [-32654..-32645] [-32732..-32728] [-32611..-32608]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set92 = [(99L, 103L), (107L, 108L), (131L, 131L), (100L, 100L), (101L, 114L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_IsPattern_92(ushort x) => x is >= 99 and <= 103 or >= 107 and <= 108 or 131 or 100 or >= 101 and <= 114;

    private static void Check92()
    {
        foreach (long v in Probe(s_set92, 0L, 65535L, true, 92))
        {
            Assert.True(ushort_IsPattern_92((ushort)v) == InSet(s_set92, v), $"ushort_IsPattern_92({v}) set=[99..103] [107..108] [131..131] [100..100] [101..114]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set93 = [(198L, 204L), (66L, 66L), (126L, 126L), (244L, 256L), (89L, 100L), (82L, 92L), (193L, 193L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Tree_93(int x) => (x < 126 ? (x < 82 ? x == 66 : ((x >= 82 && x <= 92) || (x >= 89 && x <= 100))) : (x < 198 ? (x < 193 ? x == 126 : x == 193) : (x < 244 ? (x >= 198 && x <= 204) : (x >= 244 && x <= 256))));

    private static void Check93()
    {
        foreach (long v in Probe(s_set93, (-2147483648L), 2147483647L, false, 93))
        {
            Assert.True(int_Tree_93((int)v) == InSet(s_set93, v), $"int_Tree_93({v}) set=[198..204] [66..66] [126..126] [244..256] [89..100] [82..92] [193..193]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set94 = [(76L, 86L), (65L, 65L), (67L, 72L), (0L, 12L), (0L, 2L), (79L, 82L), (74L, 76L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_OrChain_94(uint x) => (x >= 76u && x <= 86u) || x == 65u || (x >= 67u && x <= 72u) || (x >= 0u && x <= 12u) || (x >= 0u && x <= 2u) || (x >= 79u && x <= 82u) || (x >= 74u && x <= 76u);

    private static void Check94()
    {
        foreach (long v in Probe(s_set94, 0L, 4294967295L, false, 94))
        {
            Assert.True(uint_OrChain_94((uint)v) == InSet(s_set94, v), $"uint_OrChain_94({v}) set=[76..86] [65..65] [67..72] [0..12] [0..2] [79..82] [74..76]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set95 = [(12359L, 12359L), (9223372036854775796L, 9223372036854775807L), (12346L, 12350L), (12350L, 12355L), (12350L, 12350L), (12348L, 12359L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_95(long x) => x == 12359L || (x >= 9223372036854775796L && x <= 9223372036854775807L) || (x >= 12346L && x <= 12350L) || (x >= 12350L && x <= 12355L) || x == 12350L || (x >= 12348L && x <= 12359L);

    private static void Check95()
    {
        foreach (long v in Probe(s_set95, long.MinValue, 9223372036854775807L, false, 95))
        {
            Assert.True(long_OrChain_95((long)v) == InSet(s_set95, v), $"long_OrChain_95({v}) set=[12359..12359] [9223372036854775796..9223372036854775807] [12346..12350] [12350..12355] [12350..12350] [12348..12359]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set96 = [(231L, 231L), (226L, 229L), (224L, 224L), (212L, 212L), (224L, 227L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_96(char x) => !(x is '\u00E7' or >= '\u00E2' and <= '\u00E5' or '\u00E0' or '\u00D4' or >= '\u00E0' and <= '\u00E3');

    private static void Check96()
    {
        foreach (long v in Probe(s_set96, 0L, 65535L, true, 96))
        {
            Assert.True(char_Negated_96((char)v) == !InSet(s_set96, v), $"char_Negated_96({v}) set=[231..231] [226..229] [224..224] [212..212] [224..227]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set97 = [(78L, 89L), (184L, 194L), (177L, 177L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Subtractions_97(byte x) => (uint)(x - 78) <= 11u || (uint)(x - 184) <= 10u || x == 177;

    private static void Check97()
    {
        foreach (long v in Probe(s_set97, 0L, 255L, true, 97))
        {
            Assert.True(byte_Subtractions_97((byte)v) == InSet(s_set97, v), $"byte_Subtractions_97({v}) set=[78..89] [184..194] [177..177]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set98 = [(127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Ternary_98(sbyte x) => x == 127 ? true : x == 127 ? true : x == 127 ? true : x == 127 ? true : x == 127 ? true : x == 127 ? true : false;

    private static void Check98()
    {
        foreach (long v in Probe(s_set98, (-128L), 127L, true, 98))
        {
            Assert.True(sbyte_Ternary_98((sbyte)v) == InSet(s_set98, v), $"sbyte_Ternary_98({v}) set=[127..127] [127..127] [127..127] [127..127] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set99 = [(-32768L, -32765L), (-32747L, -32736L), (-32759L, -32756L), (-32765L, -32765L), (-32703L, -32669L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchExpr_99(short x) => x switch
    {
        >= (-32768) and <= (-32765) => true,
        >= (-32759) and <= (-32756) => true,
        >= (-32747) and <= (-32736) => true,
        >= (-32703) and <= (-32669) => true,
        _ => false,
    };

    private static void Check99()
    {
        foreach (long v in Probe(s_set99, (-32768L), 32767L, true, 99))
        {
            Assert.True(short_SwitchExpr_99((short)v) == InSet(s_set99, v), $"short_SwitchExpr_99({v}) set=[-32768..-32765] [-32747..-32736] [-32759..-32756] [-32765..-32765] [-32703..-32669]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set100 = [(231L, 237L), (214L, 216L), (237L, 249L), (235L, 243L), (226L, 231L), (217L, 217L), (268L, 286L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_100(ushort x)
    {
        if ((x >= 231 && x <= 237))
        {
            return true;
        }

        if ((x >= 214 && x <= 216))
        {
            return true;
        }

        if ((x >= 237 && x <= 249))
        {
            return true;
        }

        if ((x >= 235 && x <= 243))
        {
            return true;
        }

        if ((x >= 226 && x <= 231))
        {
            return true;
        }

        if (x == 217)
        {
            return true;
        }

        if ((x >= 268 && x <= 286))
        {
            return true;
        }

        return false;
    }

    private static void Check100()
    {
        foreach (long v in Probe(s_set100, 0L, 65535L, true, 100))
        {
            Assert.True(ushort_Branches_100((ushort)v) == InSet(s_set100, v), $"ushort_Branches_100({v}) set=[231..237] [214..216] [237..249] [235..243] [226..231] [217..217] [268..286]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set101 = [(2147483548L, 2147483555L), (2147483588L, 2147483592L), (2147483563L, 2147483563L), (2147483618L, 2147483645L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Flipped_101(int x) => (2147483548 <= x && x < 2147483556) || (x > 2147483587 && x < 2147483593) || 2147483563 == x || (x > 2147483617 && 2147483645 >= x);

    private static void Check101()
    {
        foreach (long v in Probe(s_set101, (-2147483648L), 2147483647L, false, 101))
        {
            Assert.True(int_Flipped_101((int)v) == InSet(s_set101, v), $"int_Flipped_101({v}) set=[2147483548..2147483555] [2147483588..2147483592] [2147483563..2147483563] [2147483618..2147483645]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set102 = [(223L, 235L), (276L, 277L), (265L, 286L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_102(uint x) => x switch
    {
        >= 223u and <= 235u => true,
        >= 265u and <= 286u => true,
        _ => false,
    };

    private static void Check102()
    {
        foreach (long v in Probe(s_set102, 0L, 4294967295L, false, 102))
        {
            Assert.True(uint_SwitchExpr_102((uint)v) == InSet(s_set102, v), $"uint_SwitchExpr_102({v}) set=[223..235] [276..277] [265..286]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set103 = [(12391L, 12391L), (12360L, 12360L), (12378L, 12379L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_IsPattern_103(long x) => x is 12391L or 12360L or >= 12378L and <= 12379L;

    private static void Check103()
    {
        foreach (long v in Probe(s_set103, long.MinValue, 9223372036854775807L, false, 103))
        {
            Assert.True(long_IsPattern_103((long)v) == InSet(s_set103, v), $"long_IsPattern_103({v}) set=[12391..12391] [12360..12360] [12378..12379]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set104 = [(100L, 100L), (99L, 99L), (75L, 75L), (95L, 96L), (98L, 104L), (99L, 99L), (132L, 175L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Ternary_104(char x) => x == '\u0064' ? true : x == '\u0063' ? true : x == '\u004B' ? true : (x >= '\u005F' && x <= '\u0060') ? true : (x >= '\u0062' && x <= '\u0068') ? true : x == '\u0063' ? true : (x >= '\u0084' && x <= '\u00AF') ? true : false;

    private static void Check104()
    {
        foreach (long v in Probe(s_set104, 0L, 65535L, true, 104))
        {
            Assert.True(char_Ternary_104((char)v) == InSet(s_set104, v), $"char_Ternary_104({v}) set=[100..100] [99..99] [75..75] [95..96] [98..104] [99..99] [132..175]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set105 = [(255L, 255L), (0L, 0L), (162L, 178L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchExpr_105(byte x) => x switch
    {
        0 => true,
        >= 162 and <= 178 => true,
        255 => true,
        _ => false,
    };

    private static void Check105()
    {
        foreach (long v in Probe(s_set105, 0L, 255L, true, 105))
        {
            Assert.True(byte_SwitchExpr_105((byte)v) == InSet(s_set105, v), $"byte_SwitchExpr_105({v}) set=[255..255] [0..0] [162..178]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set106 = [(4L, 4L), (10L, 18L), (18L, 19L), (2L, 2L), (7L, 7L), (2L, 6L), (63L, 92L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_106(sbyte x) => (x < 7 ? (x == 2 || (x >= 2 && x <= 6) || x == 4) : (x < 18 ? (x < 10 ? x == 7 : (x >= 10 && x <= 18)) : ((x >= 10 && x <= 18) || (x >= 18 && x <= 19) || (x >= 63 && x <= 92))));

    private static void Check106()
    {
        foreach (long v in Probe(s_set106, (-128L), 127L, true, 106))
        {
            Assert.True(sbyte_Tree_106((sbyte)v) == InSet(s_set106, v), $"sbyte_Tree_106({v}) set=[4..4] [10..18] [18..19] [2..2] [7..7] [2..6] [63..92]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set107 = [(-32752L, -32748L), (-32767L, -32767L), (-32745L, -32738L), (-32736L, -32729L), (-32748L, -32746L), (-32699L, -32664L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchStmt_107(short x)
    {
        switch (x)
        {
            case (-32767):
            case (-32752):
            case (-32751):
            case (-32750):
            case (-32749):
            case (-32748):
            case (-32747):
            case (-32746):
            case (-32745):
            case (-32744):
            case (-32743):
            case (-32742):
            case (-32741):
            case (-32740):
            case (-32739):
            case (-32738):
            case (-32736):
            case (-32735):
            case (-32734):
            case (-32733):
            case (-32732):
            case (-32731):
            case (-32730):
            case (-32729):
            case >= (-32699) and <= (-32664):
                return true;
            default:
                return false;
        }
    }

    private static void Check107()
    {
        foreach (long v in Probe(s_set107, (-32768L), 32767L, true, 107))
        {
            Assert.True(short_SwitchStmt_107((short)v) == InSet(s_set107, v), $"short_SwitchStmt_107({v}) set=[-32752..-32748] [-32767..-32767] [-32745..-32738] [-32736..-32729] [-32748..-32746] [-32699..-32664]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set108 = [(65535L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_108(ushort x) => (x == 65535 || x == 65535 || x == 65535 || x == 65535);

    private static void Check108()
    {
        foreach (long v in Probe(s_set108, 0L, 65535L, true, 108))
        {
            Assert.True(ushort_Tree_108((ushort)v) == InSet(s_set108, v), $"ushort_Tree_108({v}) set=[65535..65535] [65535..65535] [65535..65535] [65535..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set109 = [(1048590L, 1048590L), (1048578L, 1048588L), (1048591L, 1048591L), (1048589L, 1048589L), (1048581L, 1048581L), (1048580L, 1048580L), (1048592L, 1048599L), (1048638L, 1048670L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_109(int x)
    {
        switch (x)
        {
            case 1048578:
            case 1048579:
            case 1048580:
            case 1048581:
            case 1048582:
            case 1048583:
            case 1048584:
            case 1048585:
            case 1048586:
            case 1048587:
            case 1048588:
            case 1048589:
            case 1048590:
            case 1048591:
            case 1048592:
            case 1048593:
            case 1048594:
            case 1048595:
            case 1048596:
            case 1048597:
            case 1048598:
            case 1048599:
            case >= 1048638 and <= 1048670:
                return true;
            default:
                return false;
        }
    }

    private static void Check109()
    {
        foreach (long v in Probe(s_set109, (-2147483648L), 2147483647L, false, 109))
        {
            Assert.True(int_SwitchStmt_109((int)v) == InSet(s_set109, v), $"int_SwitchStmt_109({v}) set=[1048590..1048590] [1048578..1048588] [1048591..1048591] [1048589..1048589] [1048581..1048581] [1048580..1048580] [1048592..1048599] [1048638..1048670]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set110 = [(128L, 133L), (4294967293L, 4294967295L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Ternary_110(uint x) => (x >= 128u && x <= 133u) ? true : (x >= 4294967293u && x <= 4294967295u) ? true : false;

    private static void Check110()
    {
        foreach (long v in Probe(s_set110, 0L, 4294967295L, false, 110))
        {
            Assert.True(uint_Ternary_110((uint)v) == InSet(s_set110, v), $"uint_Ternary_110({v}) set=[128..133] [4294967293..4294967295]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set111 = [(64L, 76L), (66L, 66L), (55L, 60L), (51L, 53L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_IsPattern_111(long x) => x is >= 64L and <= 76L or 66L or >= 55L and <= 60L or >= 51L and <= 53L;

    private static void Check111()
    {
        foreach (long v in Probe(s_set111, long.MinValue, 9223372036854775807L, false, 111))
        {
            Assert.True(long_IsPattern_111((long)v) == InSet(s_set111, v), $"long_IsPattern_111({v}) set=[64..76] [66..66] [55..60] [51..53]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set112 = [(2L, 14L), (14L, 22L), (17L, 17L), (9L, 9L), (4L, 4L), (63L, 79L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Flipped_112(char x) => (x > '\u0001' && x <= '\u000E') || ('\u000E' <= x && x < '\u0017') || x == '\u0011' || x == '\u0009' || x == '\u0004' || (x >= '\u003F' && '\u004F' >= x);

    private static void Check112()
    {
        foreach (long v in Probe(s_set112, 0L, 65535L, true, 112))
        {
            Assert.True(char_Flipped_112((char)v) == InSet(s_set112, v), $"char_Flipped_112({v}) set=[2..14] [14..22] [17..17] [9..9] [4..4] [63..79]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set113 = [(255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_OrChain_113(byte x) => x == 255 || x == 255;

    private static void Check113()
    {
        foreach (long v in Probe(s_set113, 0L, 255L, true, 113))
        {
            Assert.True(byte_OrChain_113((byte)v) == InSet(s_set113, v), $"byte_OrChain_113({v}) set=[255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set114 = [(127L, 127L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_114(sbyte x) => 127 == x || 127 == x || 127 == x;

    private static void Check114()
    {
        foreach (long v in Probe(s_set114, (-128L), 127L, true, 114))
        {
            Assert.True(sbyte_Flipped_114((sbyte)v) == InSet(s_set114, v), $"sbyte_Flipped_114({v}) set=[127..127] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set115 = [(105L, 107L), (129L, 139L), (165L, 203L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Subtractions_115(short x) => (uint)(x - 105) <= 2u || (uint)(x - 129) <= 10u || (uint)(x - 165) <= 38u;

    private static void Check115()
    {
        foreach (long v in Probe(s_set115, (-32768L), 32767L, true, 115))
        {
            Assert.True(short_Subtractions_115((short)v) == InSet(s_set115, v), $"short_Subtractions_115({v}) set=[105..107] [129..139] [165..203]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set116 = [(65535L, 65535L), (57L, 61L), (0L, 9L), (50L, 55L), (0L, 10L), (117L, 126L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_116(ushort x) => (x < 57 ? ((x >= 0 && x <= 9) || (x >= 0 && x <= 10) || (x >= 50 && x <= 55)) : (x < 117 ? (x >= 57 && x <= 61) : (x < 65535 ? (x >= 117 && x <= 126) : x == 65535)));

    private static void Check116()
    {
        foreach (long v in Probe(s_set116, 0L, 65535L, true, 116))
        {
            Assert.True(ushort_Tree_116((ushort)v) == InSet(s_set116, v), $"ushort_Tree_116({v}) set=[65535..65535] [57..61] [0..9] [50..55] [0..10] [117..126]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set117 = [(-10L, -9L), (-17L, -17L), (0L, 10L), (62L, 68L), (48L, 68L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Flipped_117(int x) => (x >= (-10) && (-9) >= x) || x == (-17) || (0 <= x && x < 11) || (x > 61 && x < 69) || (x > 47 && x < 69);

    private static void Check117()
    {
        foreach (long v in Probe(s_set117, (-2147483648L), 2147483647L, false, 117))
        {
            Assert.True(int_Flipped_117((int)v) == InSet(s_set117, v), $"int_Flipped_117({v}) set=[-10..-9] [-17..-17] [0..10] [62..68] [48..68]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set118 = [(89L, 97L), (63L, 63L), (102L, 136L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Tree_118(uint x) => (x < 89u ? x == 63u : (x < 102u ? (x >= 89u && x <= 97u) : (x >= 102u && x <= 136u)));

    private static void Check118()
    {
        foreach (long v in Probe(s_set118, 0L, 4294967295L, false, 118))
        {
            Assert.True(uint_Tree_118((uint)v) == InSet(s_set118, v), $"uint_Tree_118({v}) set=[89..97] [63..63] [102..136]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set119 = [(1007L, 1014L), (1009L, 1017L), (1015L, 1015L), (1016L, 1027L), (1003L, 1003L), (1002L, 1013L), (1007L, 1007L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Ternary_119(long x) => (x >= 1007L && x <= 1014L) ? true : (x >= 1009L && x <= 1017L) ? true : x == 1015L ? true : (x >= 1016L && x <= 1027L) ? true : x == 1003L ? true : (x >= 1002L && x <= 1013L) ? true : x == 1007L ? true : false;

    private static void Check119()
    {
        foreach (long v in Probe(s_set119, long.MinValue, 9223372036854775807L, false, 119))
        {
            Assert.True(long_Ternary_119((long)v) == InSet(s_set119, v), $"long_Ternary_119({v}) set=[1007..1014] [1009..1017] [1015..1015] [1016..1027] [1003..1003] [1002..1013] [1007..1007]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set120 = [(105L, 105L), (79L, 87L), (54L, 63L), (102L, 102L), (55L, 57L), (65L, 72L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_120(char x) => x == '\u0069' || (x >= '\u004F' && x <= '\u0057') || (x >= '\u0036' && x <= '\u003F') || x == '\u0066' || (x >= '\u0037' && x <= '\u0039') || (x >= '\u0041' && x <= '\u0048');

    private static void Check120()
    {
        foreach (long v in Probe(s_set120, 0L, 65535L, true, 120))
        {
            Assert.True(char_OrChain_120((char)v) == InSet(s_set120, v), $"char_OrChain_120({v}) set=[105..105] [79..87] [54..63] [102..102] [55..57] [65..72]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set121 = [(85L, 85L), (118L, 121L), (85L, 95L), (75L, 75L), (72L, 72L), (77L, 77L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Negated_121(byte x) => !(x is 85 or >= 118 and <= 121 or >= 85 and <= 95 or 75 or 72 or 77);

    private static void Check121()
    {
        foreach (long v in Probe(s_set121, 0L, 255L, true, 121))
        {
            Assert.True(byte_Negated_121((byte)v) == !InSet(s_set121, v), $"byte_Negated_121({v}) set=[85..85] [118..121] [85..95] [75..75] [72..72] [77..77]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set122 = [(56L, 65L), (56L, 65L), (5L, 14L), (124L, 127L), (-30L, -30L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_IsPattern_122(sbyte x) => x is >= 56 and <= 65 or >= 56 and <= 65 or >= 5 and <= 14 or >= 124 and <= 127 or (-30);

    private static void Check122()
    {
        foreach (long v in Probe(s_set122, (-128L), 127L, true, 122))
        {
            Assert.True(sbyte_IsPattern_122((sbyte)v) == InSet(s_set122, v), $"sbyte_IsPattern_122({v}) set=[56..65] [56..65] [5..14] [124..127] [-30..-30]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set123 = [(163L, 163L), (149L, 154L), (147L, 151L), (123L, 131L), (32756L, 32767L), (125L, 125L), (124L, 124L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Branches_123(short x)
    {
        if (x == 163)
        {
            return true;
        }

        if ((x >= 149 && x <= 154))
        {
            return true;
        }

        if ((x >= 147 && x <= 151))
        {
            return true;
        }

        if ((x >= 123 && x <= 131))
        {
            return true;
        }

        if ((x >= 32756 && x <= 32767))
        {
            return true;
        }

        if (x == 125)
        {
            return true;
        }

        if (x == 124)
        {
            return true;
        }

        return false;
    }

    private static void Check123()
    {
        foreach (long v in Probe(s_set123, (-32768L), 32767L, true, 123))
        {
            Assert.True(short_Branches_123((short)v) == InSet(s_set123, v), $"short_Branches_123({v}) set=[163..163] [149..154] [147..151] [123..131] [32756..32767] [125..125] [124..124]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set124 = [(11L, 15L), (2L, 14L), (18L, 18L), (8L, 15L), (13L, 23L), (19L, 31L), (0L, 10L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Ternary_124(ushort x) => (x >= 11 && x <= 15) ? true : (x >= 2 && x <= 14) ? true : x == 18 ? true : (x >= 8 && x <= 15) ? true : (x >= 13 && x <= 23) ? true : (x >= 19 && x <= 31) ? true : (x >= 0 && x <= 10) ? true : false;

    private static void Check124()
    {
        foreach (long v in Probe(s_set124, 0L, 65535L, true, 124))
        {
            Assert.True(ushort_Ternary_124((ushort)v) == InSet(s_set124, v), $"ushort_Ternary_124({v}) set=[11..15] [2..14] [18..18] [8..15] [13..23] [19..31] [0..10]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set125 = [(-2147483628L, -2147483628L), (-2147483610L, -2147483599L), (-2147483635L, -2147483634L), (2147483639L, 2147483647L), (-2147483579L, -2147483562L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_125(int x) => x is (-2147483628) or >= (-2147483610) and <= (-2147483599) or >= (-2147483635) and <= (-2147483634) or >= 2147483639 and <= 2147483647 or >= (-2147483579) and <= (-2147483562);

    private static void Check125()
    {
        foreach (long v in Probe(s_set125, (-2147483648L), 2147483647L, false, 125))
        {
            Assert.True(int_IsPattern_125((int)v) == InSet(s_set125, v), $"int_IsPattern_125({v}) set=[-2147483628..-2147483628] [-2147483610..-2147483599] [-2147483635..-2147483634] [2147483639..2147483647] [-2147483579..-2147483562]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set126 = [(122L, 126L), (110L, 116L), (115L, 117L), (167L, 184L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_OrChain_126(uint x) => (x >= 122u && x <= 126u) || (x >= 110u && x <= 116u) || (x >= 115u && x <= 117u) || (x >= 167u && x <= 184u);

    private static void Check126()
    {
        foreach (long v in Probe(s_set126, 0L, 4294967295L, false, 126))
        {
            Assert.True(uint_OrChain_126((uint)v) == InSet(s_set126, v), $"uint_OrChain_126({v}) set=[122..126] [110..116] [115..117] [167..184]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set127 = [(109L, 119L), (137L, 137L), (158L, 158L), (108L, 108L), (118L, 119L), (110L, 110L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_127(long x) => (x >= 109L && x <= 119L) || x == 137L || x == 158L || x == 108L || (x >= 118L && x <= 119L) || x == 110L;

    private static void Check127()
    {
        foreach (long v in Probe(s_set127, long.MinValue, 9223372036854775807L, false, 127))
        {
            Assert.True(long_OrChain_127((long)v) == InSet(s_set127, v), $"long_OrChain_127({v}) set=[109..119] [137..137] [158..158] [108..108] [118..119] [110..110]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set128 = [(126L, 135L), (171L, 179L), (179L, 179L), (179L, 188L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_128(char x) => !(x is >= '\u007E' and <= '\u0087' or >= '\u00AB' and <= '\u00B3' or '\u00B3' or >= '\u00B3' and <= '\u00BC');

    private static void Check128()
    {
        foreach (long v in Probe(s_set128, 0L, 65535L, true, 128))
        {
            Assert.True(char_Negated_128((char)v) == !InSet(s_set128, v), $"char_Negated_128({v}) set=[126..135] [171..179] [179..179] [179..188]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set129 = [(114L, 122L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_129(byte x) => (x >= 114 && x <= 122) ? true : x == 255 ? true : false;

    private static void Check129()
    {
        foreach (long v in Probe(s_set129, 0L, 255L, true, 129))
        {
            Assert.True(byte_Ternary_129((byte)v) == InSet(s_set129, v), $"byte_Ternary_129({v}) set=[114..122] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set130 = [(63L, 63L), (49L, 57L), (57L, 68L), (65L, 65L), (56L, 62L), (51L, 56L), (63L, 63L), (116L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Ternary_130(sbyte x) => x == 63 ? true : (x >= 49 && x <= 57) ? true : (x >= 57 && x <= 68) ? true : x == 65 ? true : (x >= 56 && x <= 62) ? true : (x >= 51 && x <= 56) ? true : x == 63 ? true : (x >= 116 && x <= 127) ? true : false;

    private static void Check130()
    {
        foreach (long v in Probe(s_set130, (-128L), 127L, true, 130))
        {
            Assert.True(sbyte_Ternary_130((sbyte)v) == InSet(s_set130, v), $"sbyte_Ternary_130({v}) set=[63..63] [49..57] [57..68] [65..65] [56..62] [51..56] [63..63] [116..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set131 = [(202L, 211L), (54L, 55L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Ternary_131(short x) => (x >= 202 && x <= 211) ? true : (x >= 54 && x <= 55) ? true : false;

    private static void Check131()
    {
        foreach (long v in Probe(s_set131, (-32768L), 32767L, true, 131))
        {
            Assert.True(short_Ternary_131((short)v) == InSet(s_set131, v), $"short_Ternary_131({v}) set=[202..211] [54..55]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set132 = [(150L, 155L), (118L, 118L), (161L, 161L), (164L, 184L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Flipped_132(ushort x) => (x >= 150 && x < 156) || 118 == x || 161 == x || (164 <= x && 184 >= x);

    private static void Check132()
    {
        foreach (long v in Probe(s_set132, 0L, 65535L, true, 132))
        {
            Assert.True(ushort_Flipped_132((ushort)v) == InSet(s_set132, v), $"ushort_Flipped_132({v}) set=[150..155] [118..118] [161..161] [164..184]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set133 = [(119L, 129L), (109L, 111L), (2147483644L, 2147483647L), (117L, 117L), (109L, 117L), (112L, 115L), (113L, 113L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_133(int x) => x switch
    {
        >= 109 and <= 117 => true,
        >= 119 and <= 129 => true,
        >= 2147483644 and <= 2147483647 => true,
        _ => false,
    };

    private static void Check133()
    {
        foreach (long v in Probe(s_set133, (-2147483648L), 2147483647L, false, 133))
        {
            Assert.True(int_SwitchExpr_133((int)v) == InSet(s_set133, v), $"int_SwitchExpr_133({v}) set=[119..129] [109..111] [2147483644..2147483647] [117..117] [109..117] [112..115] [113..113]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set134 = [(208L, 211L), (275L, 275L), (214L, 214L), (296L, 296L), (166L, 197L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Branches_134(uint x)
    {
        if ((x >= 208u && x <= 211u))
        {
            return true;
        }

        if (x == 275u)
        {
            return true;
        }

        if (x == 214u)
        {
            return true;
        }

        if (x == 296u)
        {
            return true;
        }

        if ((x >= 166u && x <= 197u))
        {
            return true;
        }

        return false;
    }

    private static void Check134()
    {
        foreach (long v in Probe(s_set134, 0L, 4294967295L, false, 134))
        {
            Assert.True(uint_Branches_134((uint)v) == InSet(s_set134, v), $"uint_Branches_134({v}) set=[208..211] [275..275] [214..214] [296..296] [166..197]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set135 = [(9223372036854775739L, 9223372036854775739L), (9223372036854775739L, 9223372036854775739L), (9223372036854775752L, 9223372036854775752L), (9223372036854775762L, 9223372036854775762L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchExpr_135(long x) => x switch
    {
        9223372036854775739L => true,
        9223372036854775752L => true,
        9223372036854775762L => true,
        _ => false,
    };

    private static void Check135()
    {
        foreach (long v in Probe(s_set135, long.MinValue, 9223372036854775807L, false, 135))
        {
            Assert.True(long_SwitchExpr_135((long)v) == InSet(s_set135, v), $"long_SwitchExpr_135({v}) set=[9223372036854775739..9223372036854775739] [9223372036854775739..9223372036854775739] [9223372036854775752..9223372036854775752] [9223372036854775762..9223372036854775762]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set136 = [(1050L, 1054L), (1012L, 1023L), (1172L, 1179L), (1194L, 1195L), (1063L, 1082L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchStmt_136(char x)
    {
        switch (x)
        {
            case '\u03F4':
            case '\u03F5':
            case '\u03F6':
            case '\u03F7':
            case '\u03F8':
            case '\u03F9':
            case '\u03FA':
            case '\u03FB':
            case '\u03FC':
            case '\u03FD':
            case '\u03FE':
            case '\u03FF':
            case '\u041A':
            case '\u041B':
            case '\u041C':
            case '\u041D':
            case '\u041E':
            case >= '\u0427' and <= '\u043A':
            case '\u0494':
            case '\u0495':
            case '\u0496':
            case '\u0497':
            case '\u0498':
            case '\u0499':
            case '\u049A':
            case '\u049B':
            case '\u04AA':
            case '\u04AB':
                return true;
            default:
                return false;
        }
    }

    private static void Check136()
    {
        foreach (long v in Probe(s_set136, 0L, 65535L, true, 136))
        {
            Assert.True(char_SwitchStmt_136((char)v) == InSet(s_set136, v), $"char_SwitchStmt_136({v}) set=[1050..1054] [1012..1023] [1172..1179] [1194..1195] [1063..1082]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set137 = [(240L, 240L), (255L, 255L), (117L, 126L), (184L, 188L), (151L, 163L), (174L, 185L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_137(byte x) => x == 240 ? true : x == 255 ? true : (x >= 117 && x <= 126) ? true : (x >= 184 && x <= 188) ? true : (x >= 151 && x <= 163) ? true : (x >= 174 && x <= 185) ? true : x == 255 ? true : false;

    private static void Check137()
    {
        foreach (long v in Probe(s_set137, 0L, 255L, true, 137))
        {
            Assert.True(byte_Ternary_137((byte)v) == InSet(s_set137, v), $"byte_Ternary_137({v}) set=[240..240] [255..255] [117..126] [184..188] [151..163] [174..185] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set138 = [(106L, 106L), (67L, 67L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_138(sbyte x) => x switch
    {
        67 => true,
        106 => true,
        _ => false,
    };

    private static void Check138()
    {
        foreach (long v in Probe(s_set138, (-128L), 127L, true, 138))
        {
            Assert.True(sbyte_SwitchExpr_138((sbyte)v) == InSet(s_set138, v), $"sbyte_SwitchExpr_138({v}) set=[106..106] [67..67]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set139 = [(32727L, 32727L), (32686L, 32694L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_IsPattern_139(short x) => x is 32727 or >= 32686 and <= 32694;

    private static void Check139()
    {
        foreach (long v in Probe(s_set139, (-32768L), 32767L, true, 139))
        {
            Assert.True(short_IsPattern_139((short)v) == InSet(s_set139, v), $"short_IsPattern_139({v}) set=[32727..32727] [32686..32694]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set140 = [(1051L, 1062L), (1012L, 1012L), (1026L, 1035L), (1067L, 1098L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_IsPattern_140(ushort x) => x is >= 1051 and <= 1062 or 1012 or >= 1026 and <= 1035 or >= 1067 and <= 1098;

    private static void Check140()
    {
        foreach (long v in Probe(s_set140, 0L, 65535L, true, 140))
        {
            Assert.True(ushort_IsPattern_140((ushort)v) == InSet(s_set140, v), $"ushort_IsPattern_140({v}) set=[1051..1062] [1012..1012] [1026..1035] [1067..1098]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set141 = [(-38L, -38L), (-31L, -20L), (-31L, -31L), (-34L, -34L), (-37L, -31L), (-41L, -36L), (-31L, -30L), (20L, 43L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Subtractions_141(int x) => x == (-38) || (uint)(x - (-31)) <= 11u || x == (-31) || x == (-34) || (uint)(x - (-37)) <= 6u || (uint)(x - (-41)) <= 5u || (uint)(x - (-31)) <= 1u || (uint)(x - 20) <= 23u;

    private static void Check141()
    {
        foreach (long v in Probe(s_set141, (-2147483648L), 2147483647L, false, 141))
        {
            Assert.True(int_Subtractions_141((int)v) == InSet(s_set141, v), $"int_Subtractions_141({v}) set=[-38..-38] [-31..-20] [-31..-31] [-34..-34] [-37..-31] [-41..-36] [-31..-30] [20..43]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set142 = [(1048616L, 1048623L), (1048594L, 1048597L), (1048582L, 1048582L), (1048600L, 1048604L), (1048604L, 1048604L), (1048630L, 1048636L), (1048644L, 1048661L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchStmt_142(uint x)
    {
        switch (x)
        {
            case 1048582u:
            case 1048594u:
            case 1048595u:
            case 1048596u:
            case 1048597u:
            case 1048600u:
            case 1048601u:
            case 1048602u:
            case 1048603u:
            case 1048604u:
            case 1048616u:
            case 1048617u:
            case 1048618u:
            case 1048619u:
            case 1048620u:
            case 1048621u:
            case 1048622u:
            case 1048623u:
            case 1048630u:
            case 1048631u:
            case 1048632u:
            case 1048633u:
            case 1048634u:
            case 1048635u:
            case 1048636u:
            case >= 1048644u and <= 1048661u:
                return true;
            default:
                return false;
        }
    }

    private static void Check142()
    {
        foreach (long v in Probe(s_set142, 0L, 4294967295L, false, 142))
        {
            Assert.True(uint_SwitchStmt_142((uint)v) == InSet(s_set142, v), $"uint_SwitchStmt_142({v}) set=[1048616..1048623] [1048594..1048597] [1048582..1048582] [1048600..1048604] [1048604..1048604] [1048630..1048636] [1048644..1048661]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set143 = [(1048601L, 1048601L), (1048593L, 1048602L), (-9223372036854775808L, -9223372036854775805L), (-9223372036854775808L, -9223372036854775808L), (1048596L, 1048597L), (1048593L, 1048593L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_143(long x) => !(x is 1048601L or >= 1048593L and <= 1048602L or >= long.MinValue and <= (-9223372036854775805L) or long.MinValue or >= 1048596L and <= 1048597L or 1048593L);

    private static void Check143()
    {
        foreach (long v in Probe(s_set143, long.MinValue, 9223372036854775807L, false, 143))
        {
            Assert.True(long_Negated_143((long)v) == !InSet(s_set143, v), $"long_Negated_143({v}) set=[1048601..1048601] [1048593..1048602] [-9223372036854775808..-9223372036854775805] [-9223372036854775808..-9223372036854775808] [1048596..1048597] [1048593..1048593]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set144 = [(280L, 291L), (275L, 275L), (107L, 119L), (203L, 208L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchExpr_144(char x) => x switch
    {
        >= '\u006B' and <= '\u0077' => true,
        >= '\u00CB' and <= '\u00D0' => true,
        '\u0113' => true,
        >= '\u0118' and <= '\u0123' => true,
        _ => false,
    };

    private static void Check144()
    {
        foreach (long v in Probe(s_set144, 0L, 65535L, true, 144))
        {
            Assert.True(char_SwitchExpr_144((char)v) == InSet(s_set144, v), $"char_SwitchExpr_144({v}) set=[280..291] [275..275] [107..119] [203..208]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set145 = [(110L, 120L), (190L, 197L), (164L, 164L), (155L, 167L), (222L, 222L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Flipped_145(byte x) => (x > 109 && x <= 120) || (x > 189 && x <= 197) || x == 164 || (155 <= x && x <= 167) || 222 == x;

    private static void Check145()
    {
        foreach (long v in Probe(s_set145, 0L, 255L, true, 145))
        {
            Assert.True(byte_Flipped_145((byte)v) == InSet(s_set145, v), $"byte_Flipped_145({v}) set=[110..120] [190..197] [164..164] [155..167] [222..222]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set146 = [(61L, 61L), (82L, 82L), (51L, 55L), (55L, 61L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_OrChain_146(sbyte x) => x == 61 || x == 82 || (x >= 51 && x <= 55) || (x >= 55 && x <= 61);

    private static void Check146()
    {
        foreach (long v in Probe(s_set146, (-128L), 127L, true, 146))
        {
            Assert.True(sbyte_OrChain_146((sbyte)v) == InSet(s_set146, v), $"sbyte_OrChain_146({v}) set=[61..61] [82..82] [51..55] [55..61]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set147 = [(-41L, -41L), (-45L, -45L), (-32L, -28L), (-46L, -34L), (-34L, -25L), (32767L, 32767L), (-43L, -43L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Tree_147(short x) => (x < (-41) ? ((x >= (-46) && x <= (-34)) || x == (-45) || x == (-43)) : (x < (-34) ? ((x >= (-46) && x <= (-34)) || x == (-41)) : (x < (-32) ? ((x >= (-46) && x <= (-34)) || (x >= (-34) && x <= (-25))) : ((x >= (-34) && x <= (-25)) || (x >= (-32) && x <= (-28)) || x == 32767))));

    private static void Check147()
    {
        foreach (long v in Probe(s_set147, (-32768L), 32767L, true, 147))
        {
            Assert.True(short_Tree_147((short)v) == InSet(s_set147, v), $"short_Tree_147({v}) set=[-41..-41] [-45..-45] [-32..-28] [-46..-34] [-34..-25] [32767..32767] [-43..-43]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set148 = [(113L, 113L), (66L, 66L), (75L, 84L), (82L, 82L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchStmt_148(ushort x)
    {
        switch (x)
        {
            case 66:
            case 75:
            case 76:
            case 77:
            case 78:
            case 79:
            case 80:
            case 81:
            case 82:
            case 83:
            case 84:
            case 113:
                return true;
            default:
                return false;
        }
    }

    private static void Check148()
    {
        foreach (long v in Probe(s_set148, 0L, 65535L, true, 148))
        {
            Assert.True(ushort_SwitchStmt_148((ushort)v) == InSet(s_set148, v), $"ushort_SwitchStmt_148({v}) set=[113..113] [66..66] [75..84] [82..82]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set149 = [(1026L, 1026L), (1008L, 1018L), (1033L, 1035L), (1029L, 1029L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_149(int x) => x is 1026 or >= 1008 and <= 1018 or >= 1033 and <= 1035 or 1029;

    private static void Check149()
    {
        foreach (long v in Probe(s_set149, (-2147483648L), 2147483647L, false, 149))
        {
            Assert.True(int_IsPattern_149((int)v) == InSet(s_set149, v), $"int_IsPattern_149({v}) set=[1026..1026] [1008..1018] [1033..1035] [1029..1029]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set150 = [(1019L, 1031L), (1018L, 1030L), (4294967288L, 4294967295L), (1004L, 1004L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_IsPattern_150(uint x) => x is >= 1019u and <= 1031u or >= 1018u and <= 1030u or >= 4294967288u and <= 4294967295u or 1004u;

    private static void Check150()
    {
        foreach (long v in Probe(s_set150, 0L, 4294967295L, false, 150))
        {
            Assert.True(uint_IsPattern_150((uint)v) == InSet(s_set150, v), $"uint_IsPattern_150({v}) set=[1019..1031] [1018..1030] [4294967288..4294967295] [1004..1004]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set151 = [(81L, 81L), (9223372036854775795L, 9223372036854775807L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_151(long x) => !(x is 81L or >= 9223372036854775795L and <= 9223372036854775807L);

    private static void Check151()
    {
        foreach (long v in Probe(s_set151, long.MinValue, 9223372036854775807L, false, 151))
        {
            Assert.True(long_Negated_151((long)v) == !InSet(s_set151, v), $"long_Negated_151({v}) set=[81..81] [9223372036854775795..9223372036854775807]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set152 = [(12351L, 12360L), (12345L, 12353L), (12355L, 12355L), (12347L, 12354L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Flipped_152(char x) => (x >= '\u303F' && x < '\u3049') || (x >= '\u3039' && x < '\u3042') || x == '\u3043' || (x >= '\u303B' && x <= '\u3042');

    private static void Check152()
    {
        foreach (long v in Probe(s_set152, 0L, 65535L, true, 152))
        {
            Assert.True(char_Flipped_152((char)v) == InSet(s_set152, v), $"char_Flipped_152({v}) set=[12351..12360] [12345..12353] [12355..12355] [12347..12354]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set153 = [(255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_153(byte x) => x == 255 ? true : x == 255 ? true : x == 255 ? true : x == 255 ? true : x == 255 ? true : false;

    private static void Check153()
    {
        foreach (long v in Probe(s_set153, 0L, 255L, true, 153))
        {
            Assert.True(byte_Ternary_153((byte)v) == InSet(s_set153, v), $"byte_Ternary_153({v}) set=[255..255] [255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set154 = [(19L, 19L), (4L, 11L), (35L, 46L), (4L, 4L), (31L, 32L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_IsPattern_154(sbyte x) => x is 19 or >= 4 and <= 11 or >= 35 and <= 46 or 4 or >= 31 and <= 32;

    private static void Check154()
    {
        foreach (long v in Probe(s_set154, (-128L), 127L, true, 154))
        {
            Assert.True(sbyte_IsPattern_154((sbyte)v) == InSet(s_set154, v), $"sbyte_IsPattern_154({v}) set=[19..19] [4..11] [35..46] [4..4] [31..32]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set155 = [(32710L, 32710L), (32684L, 32687L), (32758L, 32767L), (32753L, 32753L), (32711L, 32715L), (32687L, 32698L), (32699L, 32703L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Subtractions_155(short x) => x == 32710 || (uint)(x - 32684) <= 3u || (uint)(x - 32758) <= 9u || x == 32753 || (uint)(x - 32711) <= 4u || (uint)(x - 32687) <= 11u || (uint)(x - 32699) <= 4u;

    private static void Check155()
    {
        foreach (long v in Probe(s_set155, (-32768L), 32767L, true, 155))
        {
            Assert.True(short_Subtractions_155((short)v) == InSet(s_set155, v), $"short_Subtractions_155({v}) set=[32710..32710] [32684..32687] [32758..32767] [32753..32753] [32711..32715] [32687..32698] [32699..32703]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set156 = [(174L, 180L), (59L, 59L), (125L, 129L), (89L, 89L), (0L, 10L), (190L, 190L), (24L, 27L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_156(ushort x) => (x < 89 ? (x < 24 ? (x >= 0 && x <= 10) : (x < 59 ? (x >= 24 && x <= 27) : x == 59)) : (x < 174 ? (x < 125 ? x == 89 : (x >= 125 && x <= 129)) : (x < 190 ? (x >= 174 && x <= 180) : x == 190)));

    private static void Check156()
    {
        foreach (long v in Probe(s_set156, 0L, 65535L, true, 156))
        {
            Assert.True(ushort_Tree_156((ushort)v) == InSet(s_set156, v), $"ushort_Tree_156({v}) set=[174..180] [59..59] [125..129] [89..89] [0..10] [190..190] [24..27]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set157 = [(91L, 92L), (60L, 66L), (47L, 47L), (80L, 87L), (103L, 120L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_157(int x) => x switch
    {
        47 => true,
        >= 60 and <= 66 => true,
        >= 80 and <= 87 => true,
        >= 91 and <= 92 => true,
        >= 103 and <= 120 => true,
        _ => false,
    };

    private static void Check157()
    {
        foreach (long v in Probe(s_set157, (-2147483648L), 2147483647L, false, 157))
        {
            Assert.True(int_SwitchExpr_157((int)v) == InSet(s_set157, v), $"int_SwitchExpr_157({v}) set=[91..92] [60..66] [47..47] [80..87] [103..120]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set158 = [(12347L, 12353L), (12403L, 12406L), (12379L, 12379L), (12357L, 12357L), (12359L, 12365L), (12367L, 12378L), (12374L, 12383L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Subtractions_158(uint x) => (x - 12347u) <= 6u || (x - 12403u) <= 3u || x == 12379u || x == 12357u || (x - 12359u) <= 6u || (x - 12367u) <= 11u || (x - 12374u) <= 9u;

    private static void Check158()
    {
        foreach (long v in Probe(s_set158, 0L, 4294967295L, false, 158))
        {
            Assert.True(uint_Subtractions_158((uint)v) == InSet(s_set158, v), $"uint_Subtractions_158({v}) set=[12347..12353] [12403..12406] [12379..12379] [12357..12357] [12359..12365] [12367..12378] [12374..12383]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set159 = [(75L, 78L), (62L, 62L), (73L, 84L), (71L, 82L), (55L, 66L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Ternary_159(long x) => (x >= 75L && x <= 78L) ? true : x == 62L ? true : (x >= 73L && x <= 84L) ? true : (x >= 71L && x <= 82L) ? true : (x >= 55L && x <= 66L) ? true : false;

    private static void Check159()
    {
        foreach (long v in Probe(s_set159, long.MinValue, 9223372036854775807L, false, 159))
        {
            Assert.True(long_Ternary_159((long)v) == InSet(s_set159, v), $"long_Ternary_159({v}) set=[75..78] [62..62] [73..84] [71..82] [55..66]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set160 = [(65449L, 65449L), (65474L, 65475L), (0L, 0L), (65470L, 65470L), (65490L, 65494L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_160(char x) => x == '\uFFA9' || (x >= '\uFFC2' && x <= '\uFFC3') || x == '\u0000' || x == '\uFFBE' || (x >= '\uFFD2' && x <= '\uFFD6');

    private static void Check160()
    {
        foreach (long v in Probe(s_set160, 0L, 65535L, true, 160))
        {
            Assert.True(char_OrChain_160((char)v) == InSet(s_set160, v), $"char_OrChain_160({v}) set=[65449..65449] [65474..65475] [0..0] [65470..65470] [65490..65494]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set161 = [(255L, 255L), (0L, 5L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_161(byte x) => x == 255 ? true : (x >= 0 && x <= 5) ? true : x == 255 ? true : x == 255 ? true : false;

    private static void Check161()
    {
        foreach (long v in Probe(s_set161, 0L, 255L, true, 161))
        {
            Assert.True(byte_Ternary_161((byte)v) == InSet(s_set161, v), $"byte_Ternary_161({v}) set=[255..255] [0..5] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set162 = [(125L, 127L), (127L, 127L), (119L, 120L), (-128L, -122L), (102L, 104L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_162(sbyte x) => (125 <= x && 127 >= x) || x == 127 || (x >= 119 && x < 121) || (x >= (-128) && x <= (-122)) || (x > 101 && x < 105);

    private static void Check162()
    {
        foreach (long v in Probe(s_set162, (-128L), 127L, true, 162))
        {
            Assert.True(sbyte_Flipped_162((sbyte)v) == InSet(s_set162, v), $"sbyte_Flipped_162({v}) set=[125..127] [127..127] [119..120] [-128..-122] [102..104]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set163 = [(12361L, 12363L), (12361L, 12361L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Ternary_163(short x) => (x >= 12361 && x <= 12363) ? true : x == 12361 ? true : false;

    private static void Check163()
    {
        foreach (long v in Probe(s_set163, (-32768L), 32767L, true, 163))
        {
            Assert.True(short_Ternary_163((short)v) == InSet(s_set163, v), $"short_Ternary_163({v}) set=[12361..12363] [12361..12361]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set164 = [(98L, 102L), (121L, 130L), (88L, 88L), (0L, 10L), (128L, 133L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Ternary_164(ushort x) => (x >= 98 && x <= 102) ? true : (x >= 121 && x <= 130) ? true : x == 88 ? true : (x >= 0 && x <= 10) ? true : (x >= 128 && x <= 133) ? true : false;

    private static void Check164()
    {
        foreach (long v in Probe(s_set164, 0L, 65535L, true, 164))
        {
            Assert.True(ushort_Ternary_164((ushort)v) == InSet(s_set164, v), $"ushort_Ternary_164({v}) set=[98..102] [121..130] [88..88] [0..10] [128..133]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set165 = [(117L, 117L), (67L, 79L), (124L, 124L), (77L, 79L), (119L, 125L), (94L, 103L), (114L, 115L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Branches_165(int x)
    {
        if (x == 117)
        {
            return true;
        }

        if ((x >= 67 && x <= 79))
        {
            return true;
        }

        if (x == 124)
        {
            return true;
        }

        if ((x >= 77 && x <= 79))
        {
            return true;
        }

        if ((x >= 119 && x <= 125))
        {
            return true;
        }

        if ((x >= 94 && x <= 103))
        {
            return true;
        }

        if ((x >= 114 && x <= 115))
        {
            return true;
        }

        return false;
    }

    private static void Check165()
    {
        foreach (long v in Probe(s_set165, (-2147483648L), 2147483647L, false, 165))
        {
            Assert.True(int_Branches_165((int)v) == InSet(s_set165, v), $"int_Branches_165({v}) set=[117..117] [67..79] [124..124] [77..79] [119..125] [94..103] [114..115]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set166 = [(61L, 65L), (64L, 64L), (51L, 55L), (81L, 81L), (62L, 63L), (67L, 70L), (115L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Negated_166(uint x) => !(x is >= 61u and <= 65u or 64u or >= 51u and <= 55u or 81u or >= 62u and <= 63u or >= 67u and <= 70u or >= 115u and <= 127u);

    private static void Check166()
    {
        foreach (long v in Probe(s_set166, 0L, 4294967295L, false, 166))
        {
            Assert.True(uint_Negated_166((uint)v) == !InSet(s_set166, v), $"uint_Negated_166({v}) set=[61..65] [64..64] [51..55] [81..81] [62..63] [67..70] [115..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set167 = [(103L, 105L), (112L, 119L), (-9223372036854775808L, -9223372036854775804L), (112L, 112L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_167(long x) => !(x is >= 103L and <= 105L or >= 112L and <= 119L or >= long.MinValue and <= (-9223372036854775804L) or 112L);

    private static void Check167()
    {
        foreach (long v in Probe(s_set167, long.MinValue, 9223372036854775807L, false, 167))
        {
            Assert.True(long_Negated_167((long)v) == !InSet(s_set167, v), $"long_Negated_167({v}) set=[103..105] [112..119] [-9223372036854775808..-9223372036854775804] [112..112]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set168 = [(153L, 157L), (101L, 112L), (118L, 118L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_168(char x)
    {
        if ((x >= '\u0099' && x <= '\u009D'))
        {
            return true;
        }

        if ((x >= '\u0065' && x <= '\u0070'))
        {
            return true;
        }

        if (x == '\u0076')
        {
            return true;
        }

        return false;
    }

    private static void Check168()
    {
        foreach (long v in Probe(s_set168, 0L, 65535L, true, 168))
        {
            Assert.True(char_Branches_168((char)v) == InSet(s_set168, v), $"char_Branches_168({v}) set=[153..157] [101..112] [118..118]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set169 = [(94L, 102L), (0L, 0L), (17L, 18L), (83L, 83L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchStmt_169(byte x)
    {
        switch (x)
        {
            case 0:
            case 17:
            case 18:
            case 83:
            case 94:
            case 95:
            case 96:
            case 97:
            case 98:
            case 99:
            case 100:
            case 101:
            case 102:
                return true;
            default:
                return false;
        }
    }

    private static void Check169()
    {
        foreach (long v in Probe(s_set169, 0L, 255L, true, 169))
        {
            Assert.True(byte_SwitchStmt_169((byte)v) == InSet(s_set169, v), $"byte_SwitchStmt_169({v}) set=[94..102] [0..0] [17..18] [83..83]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set170 = [(127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_OrChain_170(sbyte x) => x == 127 || x == 127 || x == 127 || x == 127;

    private static void Check170()
    {
        foreach (long v in Probe(s_set170, (-128L), 127L, true, 170))
        {
            Assert.True(sbyte_OrChain_170((sbyte)v) == InSet(s_set170, v), $"sbyte_OrChain_170({v}) set=[127..127] [127..127] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set171 = [(32709L, 32710L), (32687L, 32687L), (32668L, 32678L), (32749L, 32759L), (32674L, 32674L), (32753L, 32754L), (32712L, 32722L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Flipped_171(short x) => (x > 32708 && x < 32711) || x == 32687 || (x >= 32668 && x < 32679) || (32749 <= x && x < 32760) || x == 32674 || (x >= 32753 && x <= 32754) || (x >= 32712 && 32722 >= x);

    private static void Check171()
    {
        foreach (long v in Probe(s_set171, (-32768L), 32767L, true, 171))
        {
            Assert.True(short_Flipped_171((short)v) == InSet(s_set171, v), $"short_Flipped_171({v}) set=[32709..32710] [32687..32687] [32668..32678] [32749..32759] [32674..32674] [32753..32754] [32712..32722]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set172 = [(12382L, 12382L), (12345L, 12345L), (12405L, 12410L), (12377L, 12386L), (12350L, 12350L), (12387L, 12391L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_172(ushort x) => (x < 12382 ? (x < 12350 ? x == 12345 : (x < 12377 ? x == 12350 : (x >= 12377 && x <= 12386))) : (x < 12387 ? ((x >= 12377 && x <= 12386) || x == 12382) : (x < 12405 ? (x >= 12387 && x <= 12391) : (x >= 12405 && x <= 12410))));

    private static void Check172()
    {
        foreach (long v in Probe(s_set172, 0L, 65535L, true, 172))
        {
            Assert.True(ushort_Tree_172((ushort)v) == InSet(s_set172, v), $"ushort_Tree_172({v}) set=[12382..12382] [12345..12345] [12405..12410] [12377..12386] [12350..12350] [12387..12391]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set173 = [(1007L, 1007L), (1010L, 1010L), (1018L, 1019L), (1013L, 1016L), (1017L, 1017L), (1007L, 1010L), (1001L, 1012L), (1064L, 1081L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Tree_173(int x) => (x < 1013 ? ((x >= 1001 && x <= 1012) || x == 1007 || (x >= 1007 && x <= 1010) || x == 1010) : (x < 1018 ? (x < 1017 ? (x >= 1013 && x <= 1016) : x == 1017) : (x < 1064 ? (x >= 1018 && x <= 1019) : (x >= 1064 && x <= 1081))));

    private static void Check173()
    {
        foreach (long v in Probe(s_set173, (-2147483648L), 2147483647L, false, 173))
        {
            Assert.True(int_Tree_173((int)v) == InSet(s_set173, v), $"int_Tree_173({v}) set=[1007..1007] [1010..1010] [1018..1019] [1013..1016] [1017..1017] [1007..1010] [1001..1012] [1064..1081]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set174 = [(113L, 119L), (77L, 78L), (86L, 86L), (107L, 110L), (103L, 115L), (45L, 45L), (76L, 76L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Branches_174(uint x)
    {
        if ((x >= 113u && x <= 119u))
        {
            return true;
        }

        if ((x >= 77u && x <= 78u))
        {
            return true;
        }

        if (x == 86u)
        {
            return true;
        }

        if ((x >= 107u && x <= 110u))
        {
            return true;
        }

        if ((x >= 103u && x <= 115u))
        {
            return true;
        }

        if (x == 45u)
        {
            return true;
        }

        if (x == 76u)
        {
            return true;
        }

        return false;
    }

    private static void Check174()
    {
        foreach (long v in Probe(s_set174, 0L, 4294967295L, false, 174))
        {
            Assert.True(uint_Branches_174((uint)v) == InSet(s_set174, v), $"uint_Branches_174({v}) set=[113..119] [77..78] [86..86] [107..110] [103..115] [45..45] [76..76]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set175 = [(15L, 15L), (88L, 88L), (47L, 88L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchExpr_175(long x) => x switch
    {
        15L => true,
        >= 47L and <= 88L => true,
        _ => false,
    };

    private static void Check175()
    {
        foreach (long v in Probe(s_set175, long.MinValue, 9223372036854775807L, false, 175))
        {
            Assert.True(long_SwitchExpr_175((long)v) == InSet(s_set175, v), $"long_SwitchExpr_175({v}) set=[15..15] [88..88] [47..88]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set176 = [(1003L, 1012L), (1038L, 1045L), (1032L, 1040L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_176(char x) => !(x is >= '\u03EB' and <= '\u03F4' or >= '\u040E' and <= '\u0415' or >= '\u0408' and <= '\u0410');

    private static void Check176()
    {
        foreach (long v in Probe(s_set176, 0L, 65535L, true, 176))
        {
            Assert.True(char_Negated_176((char)v) == !InSet(s_set176, v), $"char_Negated_176({v}) set=[1003..1012] [1038..1045] [1032..1040]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set177 = [(255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_IsPattern_177(byte x) => x is 255 or 255 or 255;

    private static void Check177()
    {
        foreach (long v in Probe(s_set177, 0L, 255L, true, 177))
        {
            Assert.True(byte_IsPattern_177((byte)v) == InSet(s_set177, v), $"byte_IsPattern_177({v}) set=[255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set178 = [(26L, 26L), (39L, 39L), (-64L, -43L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_178(sbyte x) => x == 26 || x == 39 || (x > (-65) && x <= (-43));

    private static void Check178()
    {
        foreach (long v in Probe(s_set178, (-128L), 127L, true, 178))
        {
            Assert.True(sbyte_Flipped_178((sbyte)v) == InSet(s_set178, v), $"sbyte_Flipped_178({v}) set=[26..26] [39..39] [-64..-43]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set179 = [(32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_179(short x) => !(x is 32767 or 32767 or 32767 or 32767 or 32767);

    private static void Check179()
    {
        foreach (long v in Probe(s_set179, (-32768L), 32767L, true, 179))
        {
            Assert.True(short_Negated_179((short)v) == !InSet(s_set179, v), $"short_Negated_179({v}) set=[32767..32767] [32767..32767] [32767..32767] [32767..32767] [32767..32767]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set180 = [(102L, 106L), (45L, 45L), (104L, 107L), (121L, 127L), (92L, 92L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Negated_180(ushort x) => !(x is >= 102 and <= 106 or 45 or >= 104 and <= 107 or >= 121 and <= 127 or 92);

    private static void Check180()
    {
        foreach (long v in Probe(s_set180, 0L, 65535L, true, 180))
        {
            Assert.True(ushort_Negated_180((ushort)v) == !InSet(s_set180, v), $"ushort_Negated_180({v}) set=[102..106] [45..45] [104..107] [121..127] [92..92]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set181 = [(157L, 157L), (103L, 113L), (181L, 187L), (164L, 169L), (174L, 174L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_181(int x) => x switch
    {
        >= 103 and <= 113 => true,
        157 => true,
        >= 164 and <= 169 => true,
        174 => true,
        >= 181 and <= 187 => true,
        _ => false,
    };

    private static void Check181()
    {
        foreach (long v in Probe(s_set181, (-2147483648L), 2147483647L, false, 181))
        {
            Assert.True(int_SwitchExpr_181((int)v) == InSet(s_set181, v), $"int_SwitchExpr_181({v}) set=[157..157] [103..113] [181..187] [164..169] [174..174]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set182 = [(239L, 239L), (4294967290L, 4294967295L), (224L, 229L), (205L, 209L), (235L, 240L), (216L, 226L), (207L, 209L), (269L, 272L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchStmt_182(uint x)
    {
        switch (x)
        {
            case 205u:
            case 206u:
            case 207u:
            case 208u:
            case 209u:
            case >= 216u and <= 229u:
            case 235u:
            case 236u:
            case 237u:
            case 238u:
            case 239u:
            case 240u:
            case 269u:
            case 270u:
            case 271u:
            case 272u:
            case 4294967290u:
            case 4294967291u:
            case 4294967292u:
            case 4294967293u:
            case 4294967294u:
            case 4294967295u:
                return true;
            default:
                return false;
        }
    }

    private static void Check182()
    {
        foreach (long v in Probe(s_set182, 0L, 4294967295L, false, 182))
        {
            Assert.True(uint_SwitchStmt_182((uint)v) == InSet(s_set182, v), $"uint_SwitchStmt_182({v}) set=[239..239] [4294967290..4294967295] [224..229] [205..209] [235..240] [216..226] [207..209] [269..272]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set183 = [(1044L, 1055L), (1029L, 1035L), (1044L, 1044L), (1043L, 1043L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Branches_183(long x)
    {
        if ((x >= 1044L && x <= 1055L))
        {
            return true;
        }

        if ((x >= 1029L && x <= 1035L))
        {
            return true;
        }

        if (x == 1044L)
        {
            return true;
        }

        if (x == 1043L)
        {
            return true;
        }

        return false;
    }

    private static void Check183()
    {
        foreach (long v in Probe(s_set183, long.MinValue, 9223372036854775807L, false, 183))
        {
            Assert.True(long_Branches_183((long)v) == InSet(s_set183, v), $"long_Branches_183({v}) set=[1044..1055] [1029..1035] [1044..1044] [1043..1043]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set184 = [(12357L, 12357L), (12398L, 12410L), (12390L, 12401L), (12404L, 12404L), (12399L, 12407L), (12358L, 12370L), (12372L, 12372L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Ternary_184(char x) => x == '\u3045' ? true : (x >= '\u306E' && x <= '\u307A') ? true : (x >= '\u3066' && x <= '\u3071') ? true : x == '\u3074' ? true : (x >= '\u306F' && x <= '\u3077') ? true : (x >= '\u3046' && x <= '\u3052') ? true : x == '\u3054' ? true : false;

    private static void Check184()
    {
        foreach (long v in Probe(s_set184, 0L, 65535L, true, 184))
        {
            Assert.True(char_Ternary_184((char)v) == InSet(s_set184, v), $"char_Ternary_184({v}) set=[12357..12357] [12398..12410] [12390..12401] [12404..12404] [12399..12407] [12358..12370] [12372..12372]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set185 = [(151L, 151L), (138L, 138L), (105L, 105L), (141L, 141L), (149L, 158L), (127L, 127L), (120L, 120L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchStmt_185(byte x)
    {
        switch (x)
        {
            case 105:
            case 120:
            case 127:
            case 138:
            case 141:
            case 149:
            case 150:
            case 151:
            case 152:
            case 153:
            case 154:
            case 155:
            case 156:
            case 157:
            case 158:
                return true;
            default:
                return false;
        }
    }

    private static void Check185()
    {
        foreach (long v in Probe(s_set185, 0L, 255L, true, 185))
        {
            Assert.True(byte_SwitchStmt_185((byte)v) == InSet(s_set185, v), $"byte_SwitchStmt_185({v}) set=[151..151] [138..138] [105..105] [141..141] [149..158] [127..127] [120..120]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set186 = [(127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_186(sbyte x) => (x == 127 || x == 127 || x == 127 || x == 127 || x == 127 || x == 127);

    private static void Check186()
    {
        foreach (long v in Probe(s_set186, (-128L), 127L, true, 186))
        {
            Assert.True(sbyte_Tree_186((sbyte)v) == InSet(s_set186, v), $"sbyte_Tree_186({v}) set=[127..127] [127..127] [127..127] [127..127] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set187 = [(12353L, 12353L), (12362L, 12367L), (12346L, 12353L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_187(short x) => !(x is 12353 or >= 12362 and <= 12367 or >= 12346 and <= 12353);

    private static void Check187()
    {
        foreach (long v in Probe(s_set187, (-32768L), 32767L, true, 187))
        {
            Assert.True(short_Negated_187((short)v) == !InSet(s_set187, v), $"short_Negated_187({v}) set=[12353..12353] [12362..12367] [12346..12353]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set188 = [(227L, 232L), (253L, 253L), (234L, 234L), (255L, 257L), (212L, 212L), (230L, 233L), (213L, 221L), (268L, 310L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_188(ushort x)
    {
        if ((x >= 227 && x <= 232))
        {
            return true;
        }

        if (x == 253)
        {
            return true;
        }

        if (x == 234)
        {
            return true;
        }

        if ((x >= 255 && x <= 257))
        {
            return true;
        }

        if (x == 212)
        {
            return true;
        }

        if ((x >= 230 && x <= 233))
        {
            return true;
        }

        if ((x >= 213 && x <= 221))
        {
            return true;
        }

        if ((x >= 268 && x <= 310))
        {
            return true;
        }

        return false;
    }

    private static void Check188()
    {
        foreach (long v in Probe(s_set188, 0L, 65535L, true, 188))
        {
            Assert.True(ushort_Branches_188((ushort)v) == InSet(s_set188, v), $"ushort_Branches_188({v}) set=[227..232] [253..253] [234..234] [255..257] [212..212] [230..233] [213..221] [268..310]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set189 = [(-2147483631L, -2147483624L), (-2147483631L, -2147483631L), (-2147483640L, -2147483640L), (-2147483634L, -2147483634L), (-2147483644L, -2147483632L), (-2147483633L, -2147483632L), (-2147483644L, -2147483641L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_189(int x)
    {
        switch (x)
        {
            case (-2147483644):
            case (-2147483643):
            case (-2147483642):
            case (-2147483641):
            case (-2147483640):
            case (-2147483639):
            case (-2147483638):
            case (-2147483637):
            case (-2147483636):
            case (-2147483635):
            case (-2147483634):
            case (-2147483633):
            case (-2147483632):
            case (-2147483631):
            case (-2147483630):
            case (-2147483629):
            case (-2147483628):
            case (-2147483627):
            case (-2147483626):
            case (-2147483625):
            case (-2147483624):
                return true;
            default:
                return false;
        }
    }

    private static void Check189()
    {
        foreach (long v in Probe(s_set189, (-2147483648L), 2147483647L, false, 189))
        {
            Assert.True(int_SwitchStmt_189((int)v) == InSet(s_set189, v), $"int_SwitchStmt_189({v}) set=[-2147483631..-2147483624] [-2147483631..-2147483631] [-2147483640..-2147483640] [-2147483634..-2147483634] [-2147483644..-2147483632] [-2147483633..-2147483632] [-2147483644..-2147483641]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set190 = [(0L, 0L), (4294967239L, 4294967243L), (4294967204L, 4294967212L), (4294967251L, 4294967257L), (4294967201L, 4294967204L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Flipped_190(uint x) => x == 0u || (x > 4294967238u && x < 4294967244u) || (x >= 4294967204u && 4294967212u >= x) || (4294967251u <= x && x < 4294967258u) || (x > 4294967200u && 4294967204u >= x);

    private static void Check190()
    {
        foreach (long v in Probe(s_set190, 0L, 4294967295L, false, 190))
        {
            Assert.True(uint_Flipped_190((uint)v) == InSet(s_set190, v), $"uint_Flipped_190({v}) set=[0..0] [4294967239..4294967243] [4294967204..4294967212] [4294967251..4294967257] [4294967201..4294967204]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set191 = [(-30L, -30L), (-26L, -16L), (-34L, -34L), (-22L, -22L), (16L, 47L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_IsPattern_191(long x) => x is (-30L) or >= (-26L) and <= (-16L) or (-34L) or (-22L) or >= 16L and <= 47L;

    private static void Check191()
    {
        foreach (long v in Probe(s_set191, long.MinValue, 9223372036854775807L, false, 191))
        {
            Assert.True(long_IsPattern_191((long)v) == InSet(s_set191, v), $"long_IsPattern_191({v}) set=[-30..-30] [-26..-16] [-34..-34] [-22..-22] [16..47]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set192 = [(65535L, 65535L), (65534L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Subtractions_192(char x) => x == '\uFFFF' || (uint)(x - '\uFFFE') <= 1u || x == '\uFFFF' || x == '\uFFFF' || x == '\uFFFF' || x == '\uFFFF' || x == '\uFFFF';

    private static void Check192()
    {
        foreach (long v in Probe(s_set192, 0L, 65535L, true, 192))
        {
            Assert.True(char_Subtractions_192((char)v) == InSet(s_set192, v), $"char_Subtractions_192({v}) set=[65535..65535] [65534..65535] [65535..65535] [65535..65535] [65535..65535] [65535..65535] [65535..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set193 = [(255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchExpr_193(byte x) => x switch
    {
        255 => true,
        _ => false,
    };

    private static void Check193()
    {
        foreach (long v in Probe(s_set193, 0L, 255L, true, 193))
        {
            Assert.True(byte_SwitchExpr_193((byte)v) == InSet(s_set193, v), $"byte_SwitchExpr_193({v}) set=[255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set194 = [(-19L, -12L), (10L, 21L), (43L, 53L), (-8L, -6L), (1L, 1L), (64L, 68L), (4L, 6L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_194(sbyte x) => (x >= (-19) && x < (-11)) || (x >= 10 && x < 22) || (x > 42 && x < 54) || (x > (-9) && x <= (-6)) || x == 1 || (x > 63 && x < 69) || (x >= 4 && 6 >= x);

    private static void Check194()
    {
        foreach (long v in Probe(s_set194, (-128L), 127L, true, 194))
        {
            Assert.True(sbyte_Flipped_194((sbyte)v) == InSet(s_set194, v), $"sbyte_Flipped_194({v}) set=[-19..-12] [10..21] [43..53] [-8..-6] [1..1] [64..68] [4..6]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set195 = [(124L, 128L), (101L, 104L), (161L, 200L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchStmt_195(short x)
    {
        switch (x)
        {
            case 101:
            case 102:
            case 103:
            case 104:
            case 124:
            case 125:
            case 126:
            case 127:
            case 128:
            case >= 161 and <= 200:
                return true;
            default:
                return false;
        }
    }

    private static void Check195()
    {
        foreach (long v in Probe(s_set195, (-32768L), 32767L, true, 195))
        {
            Assert.True(short_SwitchStmt_195((short)v) == InSet(s_set195, v), $"short_SwitchStmt_195({v}) set=[124..128] [101..104] [161..200]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set196 = [(65534L, 65535L), (107L, 113L), (108L, 108L), (103L, 107L), (111L, 115L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Flipped_196(ushort x) => (65534 <= x && x <= 65535) || (107 <= x && x <= 113) || x == 108 || (x >= 103 && x <= 107) || (x > 110 && x <= 115);

    private static void Check196()
    {
        foreach (long v in Probe(s_set196, 0L, 65535L, true, 196))
        {
            Assert.True(ushort_Flipped_196((ushort)v) == InSet(s_set196, v), $"ushort_Flipped_196({v}) set=[65534..65535] [107..113] [108..108] [103..107] [111..115]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set197 = [(1048593L, 1048593L), (2147483645L, 2147483647L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_197(int x) => x switch
    {
        1048593 => true,
        >= 2147483645 and <= 2147483647 => true,
        _ => false,
    };

    private static void Check197()
    {
        foreach (long v in Probe(s_set197, (-2147483648L), 2147483647L, false, 197))
        {
            Assert.True(int_SwitchExpr_197((int)v) == InSet(s_set197, v), $"int_SwitchExpr_197({v}) set=[1048593..1048593] [2147483645..2147483647]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set198 = [(50L, 52L), (50L, 50L), (57L, 68L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Flipped_198(uint x) => (x >= 50u && x < 53u) || 50u == x || (x >= 57u && 68u >= x);

    private static void Check198()
    {
        foreach (long v in Probe(s_set198, 0L, 4294967295L, false, 198))
        {
            Assert.True(uint_Flipped_198((uint)v) == InSet(s_set198, v), $"uint_Flipped_198({v}) set=[50..52] [50..50] [57..68]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set199 = [(62L, 62L), (4L, 4L), (57L, 62L), (36L, 36L), (16L, 26L), (34L, 34L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_199(long x) => !(x is 62L or 4L or >= 57L and <= 62L or 36L or >= 16L and <= 26L or 34L);

    private static void Check199()
    {
        foreach (long v in Probe(s_set199, long.MinValue, 9223372036854775807L, false, 199))
        {
            Assert.True(long_Negated_199((long)v) == !InSet(s_set199, v), $"long_Negated_199({v}) set=[62..62] [4..4] [57..62] [36..36] [16..26] [34..34]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set200 = [(65451L, 65451L), (65465L, 65465L), (65449L, 65449L), (65455L, 65464L), (65450L, 65450L), (65471L, 65478L), (65467L, 65470L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_200(char x)
    {
        if (x == '\uFFAB')
        {
            return true;
        }

        if (x == '\uFFB9')
        {
            return true;
        }

        if (x == '\uFFA9')
        {
            return true;
        }

        if ((x >= '\uFFAF' && x <= '\uFFB8'))
        {
            return true;
        }

        if (x == '\uFFAA')
        {
            return true;
        }

        if ((x >= '\uFFBF' && x <= '\uFFC6'))
        {
            return true;
        }

        if ((x >= '\uFFBB' && x <= '\uFFBE'))
        {
            return true;
        }

        return false;
    }

    private static void Check200()
    {
        foreach (long v in Probe(s_set200, 0L, 65535L, true, 200))
        {
            Assert.True(char_Branches_200((char)v) == InSet(s_set200, v), $"char_Branches_200({v}) set=[65451..65451] [65465..65465] [65449..65449] [65455..65464] [65450..65450] [65471..65478] [65467..65470]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set201 = [(142L, 142L), (117L, 117L), (132L, 141L), (117L, 117L), (119L, 127L), (100L, 100L), (168L, 210L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Flipped_201(byte x) => 142 == x || x == 117 || (x > 131 && x <= 141) || 117 == x || (x > 118 && x < 128) || 100 == x || (x >= 168 && 210 >= x);

    private static void Check201()
    {
        foreach (long v in Probe(s_set201, 0L, 255L, true, 201))
        {
            Assert.True(byte_Flipped_201((byte)v) == InSet(s_set201, v), $"byte_Flipped_201({v}) set=[142..142] [117..117] [132..141] [117..117] [119..127] [100..100] [168..210]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set202 = [(127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_202(sbyte x) => x switch
    {
        127 => true,
        _ => false,
    };

    private static void Check202()
    {
        foreach (long v in Probe(s_set202, (-128L), 127L, true, 202))
        {
            Assert.True(sbyte_SwitchExpr_202((sbyte)v) == InSet(s_set202, v), $"sbyte_SwitchExpr_202({v}) set=[127..127] [127..127] [127..127] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set203 = [(-32746L, -32738L), (-32763L, -32763L), (-32747L, -32735L), (-32759L, -32752L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Ternary_203(short x) => (x >= (-32746) && x <= (-32738)) ? true : x == (-32763) ? true : (x >= (-32747) && x <= (-32735)) ? true : (x >= (-32759) && x <= (-32752)) ? true : false;

    private static void Check203()
    {
        foreach (long v in Probe(s_set203, (-32768L), 32767L, true, 203))
        {
            Assert.True(short_Ternary_203((short)v) == InSet(s_set203, v), $"short_Ternary_203({v}) set=[-32746..-32738] [-32763..-32763] [-32747..-32735] [-32759..-32752]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set204 = [(163L, 170L), (128L, 135L), (168L, 200L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Flipped_204(ushort x) => (x >= 163 && 170 >= x) || (x >= 128 && 135 >= x) || (168 <= x && x <= 200);

    private static void Check204()
    {
        foreach (long v in Probe(s_set204, 0L, 65535L, true, 204))
        {
            Assert.True(ushort_Flipped_204((ushort)v) == InSet(s_set204, v), $"ushort_Flipped_204({v}) set=[163..170] [128..135] [168..200]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set205 = [(129L, 130L), (138L, 148L), (142L, 146L), (104L, 105L), (56L, 56L), (118L, 125L), (59L, 71L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_205(int x) => x switch
    {
        56 => true,
        >= 59 and <= 71 => true,
        >= 104 and <= 105 => true,
        >= 118 and <= 125 => true,
        >= 129 and <= 130 => true,
        >= 138 and <= 148 => true,
        _ => false,
    };

    private static void Check205()
    {
        foreach (long v in Probe(s_set205, (-2147483648L), 2147483647L, false, 205))
        {
            Assert.True(int_SwitchExpr_205((int)v) == InSet(s_set205, v), $"int_SwitchExpr_205({v}) set=[129..130] [138..148] [142..146] [104..105] [56..56] [118..125] [59..71]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set206 = [(232L, 232L), (294L, 295L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Ternary_206(uint x) => x == 232u ? true : (x >= 294u && x <= 295u) ? true : false;

    private static void Check206()
    {
        foreach (long v in Probe(s_set206, 0L, 4294967295L, false, 206))
        {
            Assert.True(uint_Ternary_206((uint)v) == InSet(s_set206, v), $"uint_Ternary_206({v}) set=[232..232] [294..295]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set207 = [(6L, 13L), (1L, 1L), (7L, 7L), (13L, 13L), (19L, 31L), (9L, 9L), (70L, 84L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchStmt_207(long x)
    {
        switch (x)
        {
            case 1L:
            case 6L:
            case 7L:
            case 8L:
            case 9L:
            case 10L:
            case 11L:
            case 12L:
            case 13L:
            case 19L:
            case 20L:
            case 21L:
            case 22L:
            case 23L:
            case 24L:
            case 25L:
            case 26L:
            case 27L:
            case 28L:
            case 29L:
            case 30L:
            case 31L:
            case >= 70L and <= 84L:
                return true;
            default:
                return false;
        }
    }

    private static void Check207()
    {
        foreach (long v in Probe(s_set207, long.MinValue, 9223372036854775807L, false, 207))
        {
            Assert.True(long_SwitchStmt_207((long)v) == InSet(s_set207, v), $"long_SwitchStmt_207({v}) set=[6..13] [1..1] [7..7] [13..13] [19..31] [9..9] [70..84]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set208 = [(100L, 105L), (115L, 124L), (165L, 205L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_208(char x) => (x >= '\u0064' && x <= '\u0069') || (x >= '\u0073' && x <= '\u007C') || (x >= '\u00A5' && x <= '\u00CD');

    private static void Check208()
    {
        foreach (long v in Probe(s_set208, 0L, 65535L, true, 208))
        {
            Assert.True(char_OrChain_208((char)v) == InSet(s_set208, v), $"char_OrChain_208({v}) set=[100..105] [115..124] [165..205]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set209 = [(116L, 125L), (112L, 117L), (145L, 145L), (108L, 115L), (255L, 255L), (131L, 141L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Subtractions_209(byte x) => (uint)(x - 116) <= 9u || (uint)(x - 112) <= 5u || x == 145 || (uint)(x - 108) <= 7u || x == 255 || (uint)(x - 131) <= 10u;

    private static void Check209()
    {
        foreach (long v in Probe(s_set209, 0L, 255L, true, 209))
        {
            Assert.True(byte_Subtractions_209((byte)v) == InSet(s_set209, v), $"byte_Subtractions_209({v}) set=[116..125] [112..117] [145..145] [108..115] [255..255] [131..141]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set210 = [(127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_210(sbyte x) => x == 127 || x == 127;

    private static void Check210()
    {
        foreach (long v in Probe(s_set210, (-128L), 127L, true, 210))
        {
            Assert.True(sbyte_Flipped_210((sbyte)v) == InSet(s_set210, v), $"sbyte_Flipped_210({v}) set=[127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set211 = [(1072L, 1072L), (1068L, 1068L), (1096L, 1099L), (1098L, 1104L), (1078L, 1079L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Tree_211(short x) => (x < 1078 ? (x < 1072 ? x == 1068 : x == 1072) : (x < 1096 ? (x >= 1078 && x <= 1079) : ((x >= 1096 && x <= 1099) || (x >= 1098 && x <= 1104))));

    private static void Check211()
    {
        foreach (long v in Probe(s_set211, (-32768L), 32767L, true, 211))
        {
            Assert.True(short_Tree_211((short)v) == InSet(s_set211, v), $"short_Tree_211({v}) set=[1072..1072] [1068..1068] [1096..1099] [1098..1104] [1078..1079]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set212 = [(13L, 25L), (18L, 21L), (16L, 16L), (4L, 12L), (18L, 18L), (18L, 22L), (18L, 18L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_OrChain_212(ushort x) => (x >= 13 && x <= 25) || (x >= 18 && x <= 21) || x == 16 || (x >= 4 && x <= 12) || x == 18 || (x >= 18 && x <= 22) || x == 18;

    private static void Check212()
    {
        foreach (long v in Probe(s_set212, 0L, 65535L, true, 212))
        {
            Assert.True(ushort_OrChain_212((ushort)v) == InSet(s_set212, v), $"ushort_OrChain_212({v}) set=[13..25] [18..21] [16..16] [4..12] [18..18] [18..22] [18..18]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set213 = [(2147483590L, 2147483598L), (2147483610L, 2147483610L), (2147483575L, 2147483575L), (2147483548L, 2147483548L), (2147483572L, 2147483572L), (2147483559L, 2147483567L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_213(int x) => x is >= 2147483590 and <= 2147483598 or 2147483610 or 2147483575 or 2147483548 or 2147483572 or >= 2147483559 and <= 2147483567;

    private static void Check213()
    {
        foreach (long v in Probe(s_set213, (-2147483648L), 2147483647L, false, 213))
        {
            Assert.True(int_IsPattern_213((int)v) == InSet(s_set213, v), $"int_IsPattern_213({v}) set=[2147483590..2147483598] [2147483610..2147483610] [2147483575..2147483575] [2147483548..2147483548] [2147483572..2147483572] [2147483559..2147483567]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set214 = [(12348L, 12359L), (12349L, 12355L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Tree_214(uint x) => ((x >= 12348u && x <= 12359u) || (x >= 12349u && x <= 12355u));

    private static void Check214()
    {
        foreach (long v in Probe(s_set214, 0L, 4294967295L, false, 214))
        {
            Assert.True(uint_Tree_214((uint)v) == InSet(s_set214, v), $"uint_Tree_214({v}) set=[12348..12359] [12349..12355]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set215 = [(11L, 11L), (9L, 16L), (-31L, -31L), (1L, 1L), (13L, 53L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Ternary_215(long x) => x == 11L ? true : (x >= 9L && x <= 16L) ? true : x == (-31L) ? true : x == 1L ? true : (x >= 13L && x <= 53L) ? true : false;

    private static void Check215()
    {
        foreach (long v in Probe(s_set215, long.MinValue, 9223372036854775807L, false, 215))
        {
            Assert.True(long_Ternary_215((long)v) == InSet(s_set215, v), $"long_Ternary_215({v}) set=[11..11] [9..16] [-31..-31] [1..1] [13..53]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set216 = [(137L, 145L), (181L, 193L), (164L, 164L), (185L, 185L), (100L, 102L), (188L, 191L), (183L, 185L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_216(char x) => !(x is >= '\u0089' and <= '\u0091' or >= '\u00B5' and <= '\u00C1' or '\u00A4' or '\u00B9' or >= '\u0064' and <= '\u0066' or >= '\u00BC' and <= '\u00BF' or >= '\u00B7' and <= '\u00B9');

    private static void Check216()
    {
        foreach (long v in Probe(s_set216, 0L, 65535L, true, 216))
        {
            Assert.True(char_Negated_216((char)v) == !InSet(s_set216, v), $"char_Negated_216({v}) set=[137..145] [181..193] [164..164] [185..185] [100..102] [188..191] [183..185]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set217 = [(255L, 255L), (231L, 236L), (250L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Negated_217(byte x) => !(x is 255 or >= 231 and <= 236 or >= 250 and <= 255 or 255);

    private static void Check217()
    {
        foreach (long v in Probe(s_set217, 0L, 255L, true, 217))
        {
            Assert.True(byte_Negated_217((byte)v) == !InSet(s_set217, v), $"byte_Negated_217({v}) set=[255..255] [231..236] [250..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set218 = [(127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (126L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Ternary_218(sbyte x) => x == 127 ? true : x == 127 ? true : x == 127 ? true : x == 127 ? true : x == 127 ? true : (x >= 126 && x <= 127) ? true : x == 127 ? true : false;

    private static void Check218()
    {
        foreach (long v in Probe(s_set218, (-128L), 127L, true, 218))
        {
            Assert.True(sbyte_Ternary_218((sbyte)v) == InSet(s_set218, v), $"sbyte_Ternary_218({v}) set=[127..127] [127..127] [127..127] [127..127] [127..127] [126..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set219 = [(103L, 106L), (122L, 134L), (118L, 118L), (147L, 147L), (109L, 111L), (90L, 101L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Flipped_219(short x) => (x >= 103 && 106 >= x) || (122 <= x && 134 >= x) || x == 118 || 147 == x || (x > 108 && 111 >= x) || (x > 89 && 101 >= x);

    private static void Check219()
    {
        foreach (long v in Probe(s_set219, (-32768L), 32767L, true, 219))
        {
            Assert.True(short_Flipped_219((short)v) == InSet(s_set219, v), $"short_Flipped_219({v}) set=[103..106] [122..134] [118..118] [147..147] [109..111] [90..101]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set220 = [(80L, 90L), (111L, 112L), (85L, 85L), (85L, 88L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Flipped_220(ushort x) => (x >= 80 && 90 >= x) || (x > 110 && x < 113) || x == 85 || (x >= 85 && 88 >= x);

    private static void Check220()
    {
        foreach (long v in Probe(s_set220, 0L, 65535L, true, 220))
        {
            Assert.True(ushort_Flipped_220((ushort)v) == InSet(s_set220, v), $"ushort_Flipped_220({v}) set=[80..90] [111..112] [85..85] [85..88]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set221 = [(-10L, -10L), (-16L, -13L), (-10L, -8L), (-5L, -1L), (44L, 79L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Branches_221(int x)
    {
        if (x == (-10))
        {
            return true;
        }

        if ((x >= (-16) && x <= (-13)))
        {
            return true;
        }

        if ((x >= (-10) && x <= (-8)))
        {
            return true;
        }

        if ((x >= (-5) && x <= (-1)))
        {
            return true;
        }

        if ((x >= 44 && x <= 79))
        {
            return true;
        }

        return false;
    }

    private static void Check221()
    {
        foreach (long v in Probe(s_set221, (-2147483648L), 2147483647L, false, 221))
        {
            Assert.True(int_Branches_221((int)v) == InSet(s_set221, v), $"int_Branches_221({v}) set=[-10..-10] [-16..-13] [-10..-8] [-5..-1] [44..79]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set222 = [(104L, 104L), (122L, 131L), (111L, 115L), (0L, 0L), (134L, 139L), (164L, 175L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_IsPattern_222(uint x) => x is 104u or >= 122u and <= 131u or >= 111u and <= 115u or 0u or >= 134u and <= 139u or >= 164u and <= 175u;

    private static void Check222()
    {
        foreach (long v in Probe(s_set222, 0L, 4294967295L, false, 222))
        {
            Assert.True(uint_IsPattern_222((uint)v) == InSet(s_set222, v), $"uint_IsPattern_222({v}) set=[104..104] [122..131] [111..115] [0..0] [134..139] [164..175]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set223 = [(1013L, 1023L), (1025L, 1029L), (1008L, 1008L), (1018L, 1024L), (1067L, 1091L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Branches_223(long x)
    {
        if ((x >= 1013L && x <= 1023L))
        {
            return true;
        }

        if ((x >= 1025L && x <= 1029L))
        {
            return true;
        }

        if (x == 1008L)
        {
            return true;
        }

        if ((x >= 1018L && x <= 1024L))
        {
            return true;
        }

        if ((x >= 1067L && x <= 1091L))
        {
            return true;
        }

        return false;
    }

    private static void Check223()
    {
        foreach (long v in Probe(s_set223, long.MinValue, 9223372036854775807L, false, 223))
        {
            Assert.True(long_Branches_223((long)v) == InSet(s_set223, v), $"long_Branches_223({v}) set=[1013..1023] [1025..1029] [1008..1008] [1018..1024] [1067..1091]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set224 = [(65535L, 65535L), (12345L, 12345L), (12364L, 12373L), (12347L, 12358L), (12364L, 12373L), (12347L, 12357L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Tree_224(char x) => (x < '\u304C' ? (x < '\u303B' ? x == '\u3039' : ((x >= '\u303B' && x <= '\u3046') || (x >= '\u303B' && x <= '\u3045'))) : ((x >= '\u304C' && x <= '\u3055') || (x >= '\u304C' && x <= '\u3055') || x == '\uFFFF'));

    private static void Check224()
    {
        foreach (long v in Probe(s_set224, 0L, 65535L, true, 224))
        {
            Assert.True(char_Tree_224((char)v) == InSet(s_set224, v), $"char_Tree_224({v}) set=[65535..65535] [12345..12345] [12364..12373] [12347..12358] [12364..12373] [12347..12357]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set225 = [(127L, 138L), (119L, 131L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_225(byte x) => (x >= 127 && x <= 138) ? true : (x >= 119 && x <= 131) ? true : false;

    private static void Check225()
    {
        foreach (long v in Probe(s_set225, 0L, 255L, true, 225))
        {
            Assert.True(byte_Ternary_225((byte)v) == InSet(s_set225, v), $"byte_Ternary_225({v}) set=[127..138] [119..131]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set226 = [(51L, 57L), (54L, 66L), (58L, 68L), (48L, 51L), (-128L, -117L), (118L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchStmt_226(sbyte x)
    {
        switch (x)
        {
            case (-128):
            case (-127):
            case (-126):
            case (-125):
            case (-124):
            case (-123):
            case (-122):
            case (-121):
            case (-120):
            case (-119):
            case (-118):
            case (-117):
            case >= 48 and <= 68:
            case 118:
            case 119:
            case 120:
            case 121:
            case 122:
            case 123:
            case 124:
            case 125:
            case 126:
            case 127:
                return true;
            default:
                return false;
        }
    }

    private static void Check226()
    {
        foreach (long v in Probe(s_set226, (-128L), 127L, true, 226))
        {
            Assert.True(sbyte_SwitchStmt_226((sbyte)v) == InSet(s_set226, v), $"sbyte_SwitchStmt_226({v}) set=[51..57] [54..66] [58..68] [48..51] [-128..-117] [118..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set227 = [(32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchExpr_227(short x) => x switch
    {
        32767 => true,
        _ => false,
    };

    private static void Check227()
    {
        foreach (long v in Probe(s_set227, (-32768L), 32767L, true, 227))
        {
            Assert.True(short_SwitchExpr_227((short)v) == InSet(s_set227, v), $"short_SwitchExpr_227({v}) set=[32767..32767] [32767..32767] [32767..32767] [32767..32767] [32767..32767] [32767..32767]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set228 = [(10L, 11L), (171L, 179L), (134L, 146L), (32L, 40L), (0L, 0L), (50L, 50L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Tree_228(ushort x) => (x < 50 ? (x < 10 ? x == 0 : (x < 32 ? (x >= 10 && x <= 11) : (x >= 32 && x <= 40))) : (x < 134 ? x == 50 : (x < 171 ? (x >= 134 && x <= 146) : (x >= 171 && x <= 179))));

    private static void Check228()
    {
        foreach (long v in Probe(s_set228, 0L, 65535L, true, 228))
        {
            Assert.True(ushort_Tree_228((ushort)v) == InSet(s_set228, v), $"ushort_Tree_228({v}) set=[10..11] [171..179] [134..146] [32..40] [0..0] [50..50]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set229 = [(-2147483625L, -2147483625L), (-2147483636L, -2147483636L), (-2147483580L, -2147483562L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Tree_229(int x) => (x < (-2147483625) ? x == (-2147483636) : (x < (-2147483580) ? x == (-2147483625) : (x >= (-2147483580) && x <= (-2147483562))));

    private static void Check229()
    {
        foreach (long v in Probe(s_set229, (-2147483648L), 2147483647L, false, 229))
        {
            Assert.True(int_Tree_229((int)v) == InSet(s_set229, v), $"int_Tree_229({v}) set=[-2147483625..-2147483625] [-2147483636..-2147483636] [-2147483580..-2147483562]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set230 = [(12379L, 12382L), (12363L, 12363L), (12361L, 12361L), (12357L, 12366L), (12384L, 12389L), (12374L, 12374L), (12349L, 12353L), (12415L, 12434L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Subtractions_230(uint x) => (x - 12379u) <= 3u || x == 12363u || x == 12361u || (x - 12357u) <= 9u || (x - 12384u) <= 5u || x == 12374u || (x - 12349u) <= 4u || (x - 12415u) <= 19u;

    private static void Check230()
    {
        foreach (long v in Probe(s_set230, 0L, 4294967295L, false, 230))
        {
            Assert.True(uint_Subtractions_230((uint)v) == InSet(s_set230, v), $"uint_Subtractions_230({v}) set=[12379..12382] [12363..12363] [12361..12361] [12357..12366] [12384..12389] [12374..12374] [12349..12353] [12415..12434]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set231 = [(243L, 248L), (-9223372036854775808L, -9223372036854775808L), (252L, 254L), (150L, 156L), (115L, 116L), (159L, 202L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Ternary_231(long x) => (x >= 243L && x <= 248L) ? true : x == long.MinValue ? true : (x >= 252L && x <= 254L) ? true : (x >= 150L && x <= 156L) ? true : (x >= 115L && x <= 116L) ? true : (x >= 159L && x <= 202L) ? true : false;

    private static void Check231()
    {
        foreach (long v in Probe(s_set231, long.MinValue, 9223372036854775807L, false, 231))
        {
            Assert.True(long_Ternary_231((long)v) == InSet(s_set231, v), $"long_Ternary_231({v}) set=[243..248] [-9223372036854775808..-9223372036854775808] [252..254] [150..156] [115..116] [159..202]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set232 = [(237L, 237L), (285L, 292L), (391L, 391L), (0L, 0L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchStmt_232(char x)
    {
        switch (x)
        {
            case '\u0000':
            case '\u00ED':
            case '\u011D':
            case '\u011E':
            case '\u011F':
            case '\u0120':
            case '\u0121':
            case '\u0122':
            case '\u0123':
            case '\u0124':
            case '\u0187':
                return true;
            default:
                return false;
        }
    }

    private static void Check232()
    {
        foreach (long v in Probe(s_set232, 0L, 65535L, true, 232))
        {
            Assert.True(char_SwitchStmt_232((char)v) == InSet(s_set232, v), $"char_SwitchStmt_232({v}) set=[237..237] [285..292] [391..391] [0..0]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set233 = [(254L, 254L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Branches_233(byte x)
    {
        if (x == 254)
        {
            return true;
        }

        if (x == 255)
        {
            return true;
        }

        if (x == 255)
        {
            return true;
        }

        if (x == 255)
        {
            return true;
        }

        return false;
    }

    private static void Check233()
    {
        foreach (long v in Probe(s_set233, 0L, 255L, true, 233))
        {
            Assert.True(byte_Branches_233((byte)v) == InSet(s_set233, v), $"byte_Branches_233({v}) set=[254..254] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set234 = [(127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_234(sbyte x) => x == 127 || 127 == x || x == 127 || 127 == x || 127 == x || x == 127 || 127 == x;

    private static void Check234()
    {
        foreach (long v in Probe(s_set234, (-128L), 127L, true, 234))
        {
            Assert.True(sbyte_Flipped_234((sbyte)v) == InSet(s_set234, v), $"sbyte_Flipped_234({v}) set=[127..127] [127..127] [127..127] [127..127] [127..127] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set235 = [(12389L, 12390L), (12381L, 12383L), (12345L, 12356L), (-32768L, -32762L), (12391L, 12391L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_235(short x) => !(x is >= 12389 and <= 12390 or >= 12381 and <= 12383 or >= 12345 and <= 12356 or >= (-32768) and <= (-32762) or 12391);

    private static void Check235()
    {
        foreach (long v in Probe(s_set235, (-32768L), 32767L, true, 235))
        {
            Assert.True(short_Negated_235((short)v) == !InSet(s_set235, v), $"short_Negated_235({v}) set=[12389..12390] [12381..12383] [12345..12356] [-32768..-32762] [12391..12391]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set236 = [(65498L, 65503L), (65470L, 65470L), (65445L, 65445L), (65488L, 65489L), (65470L, 65472L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Subtractions_236(ushort x) => (uint)(x - 65498) <= 5u || x == 65470 || x == 65445 || (uint)(x - 65488) <= 1u || (uint)(x - 65470) <= 2u;

    private static void Check236()
    {
        foreach (long v in Probe(s_set236, 0L, 65535L, true, 236))
        {
            Assert.True(ushort_Subtractions_236((ushort)v) == InSet(s_set236, v), $"ushort_Subtractions_236({v}) set=[65498..65503] [65470..65470] [65445..65445] [65488..65489] [65470..65472]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set237 = [(-2147483472L, -2147483461L), (-2147483597L, -2147483594L), (-2147483458L, -2147483454L), (-2147483485L, -2147483475L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Flipped_237(int x) => (x >= (-2147483472) && x <= (-2147483461)) || ((-2147483597) <= x && x < (-2147483593)) || (x > (-2147483459) && (-2147483454) >= x) || (x > (-2147483486) && x <= (-2147483475));

    private static void Check237()
    {
        foreach (long v in Probe(s_set237, (-2147483648L), 2147483647L, false, 237))
        {
            Assert.True(int_Flipped_237((int)v) == InSet(s_set237, v), $"int_Flipped_237({v}) set=[-2147483472..-2147483461] [-2147483597..-2147483594] [-2147483458..-2147483454] [-2147483485..-2147483475]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set238 = [(4294967205L, 4294967205L), (4294967208L, 4294967211L), (4294967198L, 4294967202L), (4294967197L, 4294967207L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_238(uint x) => x switch
    {
        >= 4294967197u and <= 4294967207u => true,
        >= 4294967208u and <= 4294967211u => true,
        _ => false,
    };

    private static void Check238()
    {
        foreach (long v in Probe(s_set238, 0L, 4294967295L, false, 238))
        {
            Assert.True(uint_SwitchExpr_238((uint)v) == InSet(s_set238, v), $"uint_SwitchExpr_238({v}) set=[4294967205..4294967205] [4294967208..4294967211] [4294967198..4294967202] [4294967197..4294967207]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set239 = [(68L, 68L), (125L, 131L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_239(long x) => x == 68L || (x >= 125L && x <= 131L);

    private static void Check239()
    {
        foreach (long v in Probe(s_set239, long.MinValue, 9223372036854775807L, false, 239))
        {
            Assert.True(long_OrChain_239((long)v) == InSet(s_set239, v), $"long_OrChain_239({v}) set=[68..68] [125..131]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set240 = [(12429L, 12432L), (12495L, 12506L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Tree_240(char x) => (x < '\u30CF' ? (x >= '\u308D' && x <= '\u3090') : (x >= '\u30CF' && x <= '\u30DA'));

    private static void Check240()
    {
        foreach (long v in Probe(s_set240, 0L, 65535L, true, 240))
        {
            Assert.True(char_Tree_240((char)v) == InSet(s_set240, v), $"char_Tree_240({v}) set=[12429..12432] [12495..12506]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set241 = [(255L, 255L), (121L, 121L), (197L, 208L), (158L, 160L), (182L, 182L), (229L, 233L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchExpr_241(byte x) => x switch
    {
        121 => true,
        >= 158 and <= 160 => true,
        182 => true,
        >= 197 and <= 208 => true,
        >= 229 and <= 233 => true,
        255 => true,
        _ => false,
    };

    private static void Check241()
    {
        foreach (long v in Probe(s_set241, 0L, 255L, true, 241))
        {
            Assert.True(byte_SwitchExpr_241((byte)v) == InSet(s_set241, v), $"byte_SwitchExpr_241({v}) set=[255..255] [121..121] [197..208] [158..160] [182..182] [229..233]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set242 = [(60L, 60L), (89L, 95L), (42L, 48L), (100L, 107L), (126L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_242(sbyte x) => x == 60 || (89 <= x && x < 96) || (x >= 42 && x < 49) || (x >= 100 && x < 108) || (x > 125 && x <= 127);

    private static void Check242()
    {
        foreach (long v in Probe(s_set242, (-128L), 127L, true, 242))
        {
            Assert.True(sbyte_Flipped_242((sbyte)v) == InSet(s_set242, v), $"sbyte_Flipped_242({v}) set=[60..60] [89..95] [42..48] [100..107] [126..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set243 = [(79L, 79L), (80L, 80L), (65L, 74L), (56L, 56L), (48L, 48L), (75L, 81L), (81L, 82L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_243(short x) => !(x is 79 or 80 or >= 65 and <= 74 or 56 or 48 or >= 75 and <= 81 or >= 81 and <= 82);

    private static void Check243()
    {
        foreach (long v in Probe(s_set243, (-32768L), 32767L, true, 243))
        {
            Assert.True(short_Negated_243((short)v) == !InSet(s_set243, v), $"short_Negated_243({v}) set=[79..79] [80..80] [65..74] [56..56] [48..48] [75..81] [81..82]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set244 = [(104L, 104L), (62L, 62L), (60L, 61L), (90L, 90L), (84L, 84L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_OrChain_244(ushort x) => x == 104 || x == 62 || (x >= 60 && x <= 61) || x == 90 || x == 84;

    private static void Check244()
    {
        foreach (long v in Probe(s_set244, 0L, 65535L, true, 244))
        {
            Assert.True(ushort_OrChain_244((ushort)v) == InSet(s_set244, v), $"ushort_OrChain_244({v}) set=[104..104] [62..62] [60..61] [90..90] [84..84]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set245 = [(-50L, -50L), (-34L, -34L), (-45L, -45L), (-32L, -20L), (-34L, -34L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_245(int x)
    {
        switch (x)
        {
            case (-50):
            case (-45):
            case (-34):
            case (-32):
            case (-31):
            case (-30):
            case (-29):
            case (-28):
            case (-27):
            case (-26):
            case (-25):
            case (-24):
            case (-23):
            case (-22):
            case (-21):
            case (-20):
                return true;
            default:
                return false;
        }
    }

    private static void Check245()
    {
        foreach (long v in Probe(s_set245, (-2147483648L), 2147483647L, false, 245))
        {
            Assert.True(int_SwitchStmt_245((int)v) == InSet(s_set245, v), $"int_SwitchStmt_245({v}) set=[-50..-50] [-34..-34] [-45..-45] [-32..-20] [-34..-34]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set246 = [(150L, 150L), (55L, 58L), (160L, 165L), (116L, 127L), (213L, 213L), (68L, 75L), (200L, 200L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Branches_246(uint x)
    {
        if (x == 150u)
        {
            return true;
        }

        if ((x >= 55u && x <= 58u))
        {
            return true;
        }

        if ((x >= 160u && x <= 165u))
        {
            return true;
        }

        if ((x >= 116u && x <= 127u))
        {
            return true;
        }

        if (x == 213u)
        {
            return true;
        }

        if ((x >= 68u && x <= 75u))
        {
            return true;
        }

        if (x == 200u)
        {
            return true;
        }

        return false;
    }

    private static void Check246()
    {
        foreach (long v in Probe(s_set246, 0L, 4294967295L, false, 246))
        {
            Assert.True(uint_Branches_246((uint)v) == InSet(s_set246, v), $"uint_Branches_246({v}) set=[150..150] [55..58] [160..165] [116..127] [213..213] [68..75] [200..200]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set247 = [(9223372036854775759L, 9223372036854775760L), (9223372036854775796L, 9223372036854775796L), (9223372036854775807L, 9223372036854775807L), (9223372036854775787L, 9223372036854775794L), (9223372036854775807L, 9223372036854775807L), (9223372036854775735L, 9223372036854775743L), (9223372036854775807L, 9223372036854775807L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchStmt_247(long x)
    {
        switch (x)
        {
            case 9223372036854775735L:
            case 9223372036854775736L:
            case 9223372036854775737L:
            case 9223372036854775738L:
            case 9223372036854775739L:
            case 9223372036854775740L:
            case 9223372036854775741L:
            case 9223372036854775742L:
            case 9223372036854775743L:
            case 9223372036854775759L:
            case 9223372036854775760L:
            case 9223372036854775787L:
            case 9223372036854775788L:
            case 9223372036854775789L:
            case 9223372036854775790L:
            case 9223372036854775791L:
            case 9223372036854775792L:
            case 9223372036854775793L:
            case 9223372036854775794L:
            case 9223372036854775796L:
            case 9223372036854775807L:
                return true;
            default:
                return false;
        }
    }

    private static void Check247()
    {
        foreach (long v in Probe(s_set247, long.MinValue, 9223372036854775807L, false, 247))
        {
            Assert.True(long_SwitchStmt_247((long)v) == InSet(s_set247, v), $"long_SwitchStmt_247({v}) set=[9223372036854775759..9223372036854775760] [9223372036854775796..9223372036854775796] [9223372036854775807..9223372036854775807] [9223372036854775787..9223372036854775794] [9223372036854775807..9223372036854775807] [9223372036854775735..9223372036854775743] [9223372036854775807..9223372036854775807]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set248 = [(12391L, 12399L), (12356L, 12361L), (12383L, 12385L), (12441L, 12449L), (12432L, 12432L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_248(char x) => !(x is >= '\u3067' and <= '\u306F' or >= '\u3044' and <= '\u3049' or >= '\u305F' and <= '\u3061' or >= '\u3099' and <= '\u30A1' or '\u3090');

    private static void Check248()
    {
        foreach (long v in Probe(s_set248, 0L, 65535L, true, 248))
        {
            Assert.True(char_Negated_248((char)v) == !InSet(s_set248, v), $"char_Negated_248({v}) set=[12391..12399] [12356..12361] [12383..12385] [12441..12449] [12432..12432]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set249 = [(90L, 102L), (179L, 190L), (226L, 226L), (102L, 104L), (213L, 213L), (152L, 152L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Tree_249(byte x) => (x < 179 ? ((x >= 90 && x <= 102) || (x >= 102 && x <= 104) || x == 152) : (x < 213 ? (x >= 179 && x <= 190) : (x < 226 ? x == 213 : x == 226)));

    private static void Check249()
    {
        foreach (long v in Probe(s_set249, 0L, 255L, true, 249))
        {
            Assert.True(byte_Tree_249((byte)v) == InSet(s_set249, v), $"byte_Tree_249({v}) set=[90..102] [179..190] [226..226] [102..104] [213..213] [152..152]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set250 = [(101L, 101L), (127L, 127L), (107L, 112L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_250(sbyte x) => x switch
    {
        101 => true,
        >= 107 and <= 112 => true,
        127 => true,
        _ => false,
    };

    private static void Check250()
    {
        foreach (long v in Probe(s_set250, (-128L), 127L, true, 250))
        {
            Assert.True(sbyte_SwitchExpr_250((sbyte)v) == InSet(s_set250, v), $"sbyte_SwitchExpr_250({v}) set=[101..101] [127..127] [107..112]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set251 = [(32767L, 32767L), (32713L, 32713L), (32767L, 32767L), (32767L, 32767L), (32754L, 32754L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Branches_251(short x)
    {
        if (x == 32767)
        {
            return true;
        }

        if (x == 32713)
        {
            return true;
        }

        if (x == 32767)
        {
            return true;
        }

        if (x == 32767)
        {
            return true;
        }

        if (x == 32754)
        {
            return true;
        }

        return false;
    }

    private static void Check251()
    {
        foreach (long v in Probe(s_set251, (-32768L), 32767L, true, 251))
        {
            Assert.True(short_Branches_251((short)v) == InSet(s_set251, v), $"short_Branches_251({v}) set=[32767..32767] [32713..32713] [32767..32767] [32767..32767] [32754..32754]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set252 = [(12381L, 12381L), (12383L, 12383L), (12390L, 12390L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_252(ushort x)
    {
        if (x == 12381)
        {
            return true;
        }

        if (x == 12383)
        {
            return true;
        }

        if (x == 12390)
        {
            return true;
        }

        return false;
    }

    private static void Check252()
    {
        foreach (long v in Probe(s_set252, 0L, 65535L, true, 252))
        {
            Assert.True(ushort_Branches_252((ushort)v) == InSet(s_set252, v), $"ushort_Branches_252({v}) set=[12381..12381] [12383..12383] [12390..12390]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set253 = [(12362L, 12364L), (12349L, 12349L), (12385L, 12385L), (12379L, 12380L), (12367L, 12379L), (12366L, 12366L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_253(int x) => x is >= 12362 and <= 12364 or 12349 or 12385 or >= 12379 and <= 12380 or >= 12367 and <= 12379 or 12366;

    private static void Check253()
    {
        foreach (long v in Probe(s_set253, (-2147483648L), 2147483647L, false, 253))
        {
            Assert.True(int_IsPattern_253((int)v) == InSet(s_set253, v), $"int_IsPattern_253({v}) set=[12362..12364] [12349..12349] [12385..12385] [12379..12380] [12367..12379] [12366..12366]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set254 = [(33L, 40L), (47L, 59L), (33L, 33L), (36L, 36L), (44L, 53L), (99L, 109L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_254(uint x) => x switch
    {
        >= 33u and <= 40u => true,
        >= 44u and <= 59u => true,
        >= 99u and <= 109u => true,
        _ => false,
    };

    private static void Check254()
    {
        foreach (long v in Probe(s_set254, 0L, 4294967295L, false, 254))
        {
            Assert.True(uint_SwitchExpr_254((uint)v) == InSet(s_set254, v), $"uint_SwitchExpr_254({v}) set=[33..40] [47..59] [33..33] [36..36] [44..53] [99..109]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set255 = [(9223372036854775781L, 9223372036854775786L), (9223372036854775768L, 9223372036854775768L), (9223372036854775732L, 9223372036854775735L), (9223372036854775713L, 9223372036854775719L), (9223372036854775775L, 9223372036854775775L), (9223372036854775721L, 9223372036854775721L), (9223372036854775769L, 9223372036854775792L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchExpr_255(long x) => x switch
    {
        >= 9223372036854775713L and <= 9223372036854775719L => true,
        9223372036854775721L => true,
        >= 9223372036854775732L and <= 9223372036854775735L => true,
        9223372036854775768L => true,
        >= 9223372036854775769L and <= 9223372036854775792L => true,
        _ => false,
    };

    private static void Check255()
    {
        foreach (long v in Probe(s_set255, long.MinValue, 9223372036854775807L, false, 255))
        {
            Assert.True(long_SwitchExpr_255((long)v) == InSet(s_set255, v), $"long_SwitchExpr_255({v}) set=[9223372036854775781..9223372036854775786] [9223372036854775768..9223372036854775768] [9223372036854775732..9223372036854775735] [9223372036854775713..9223372036854775719] [9223372036854775775..9223372036854775775] [9223372036854775721..9223372036854775721] [9223372036854775769..9223372036854775792]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set256 = [(183L, 189L), (271L, 272L), (214L, 214L), (295L, 307L), (107L, 107L), (126L, 126L), (273L, 281L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_256(char x)
    {
        if ((x >= '\u00B7' && x <= '\u00BD'))
        {
            return true;
        }

        if ((x >= '\u010F' && x <= '\u0110'))
        {
            return true;
        }

        if (x == '\u00D6')
        {
            return true;
        }

        if ((x >= '\u0127' && x <= '\u0133'))
        {
            return true;
        }

        if (x == '\u006B')
        {
            return true;
        }

        if (x == '\u007E')
        {
            return true;
        }

        if ((x >= '\u0111' && x <= '\u0119'))
        {
            return true;
        }

        return false;
    }

    private static void Check256()
    {
        foreach (long v in Probe(s_set256, 0L, 65535L, true, 256))
        {
            Assert.True(char_Branches_256((char)v) == InSet(s_set256, v), $"char_Branches_256({v}) set=[183..189] [271..272] [214..214] [295..307] [107..107] [126..126] [273..281]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set257 = [(192L, 197L), (187L, 197L), (162L, 168L), (158L, 158L), (161L, 161L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_OrChain_257(byte x) => (x >= 192 && x <= 197) || (x >= 187 && x <= 197) || (x >= 162 && x <= 168) || x == 158 || x == 161;

    private static void Check257()
    {
        foreach (long v in Probe(s_set257, 0L, 255L, true, 257))
        {
            Assert.True(byte_OrChain_257((byte)v) == InSet(s_set257, v), $"byte_OrChain_257({v}) set=[192..197] [187..197] [162..168] [158..158] [161..161]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set258 = [(-95L, -88L), (-80L, -75L), (-101L, -92L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_IsPattern_258(sbyte x) => x is >= (-95) and <= (-88) or >= (-80) and <= (-75) or >= (-101) and <= (-92);

    private static void Check258()
    {
        foreach (long v in Probe(s_set258, (-128L), 127L, true, 258))
        {
            Assert.True(sbyte_IsPattern_258((sbyte)v) == InSet(s_set258, v), $"sbyte_IsPattern_258({v}) set=[-95..-88] [-80..-75] [-101..-92]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set259 = [(102L, 102L), (108L, 118L), (111L, 111L), (99L, 109L), (109L, 109L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchExpr_259(short x) => x switch
    {
        >= 99 and <= 118 => true,
        _ => false,
    };

    private static void Check259()
    {
        foreach (long v in Probe(s_set259, (-32768L), 32767L, true, 259))
        {
            Assert.True(short_SwitchExpr_259((short)v) == InSet(s_set259, v), $"short_SwitchExpr_259({v}) set=[102..102] [108..118] [111..111] [99..109] [109..109]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set260 = [(57L, 67L), (50L, 58L), (51L, 58L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchExpr_260(ushort x) => x switch
    {
        >= 50 and <= 67 => true,
        _ => false,
    };

    private static void Check260()
    {
        foreach (long v in Probe(s_set260, 0L, 65535L, true, 260))
        {
            Assert.True(ushort_SwitchExpr_260((ushort)v) == InSet(s_set260, v), $"ushort_SwitchExpr_260({v}) set=[57..67] [50..58] [51..58]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set261 = [(1030L, 1036L), (1010L, 1010L), (-2147483648L, -2147483648L), (1000L, 1012L), (1021L, 1021L), (1036L, 1047L), (1015L, 1015L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Subtractions_261(int x) => (uint)(x - 1030) <= 6u || x == 1010 || x == int.MinValue || (uint)(x - 1000) <= 12u || x == 1021 || (uint)(x - 1036) <= 11u || x == 1015;

    private static void Check261()
    {
        foreach (long v in Probe(s_set261, (-2147483648L), 2147483647L, false, 261))
        {
            Assert.True(int_Subtractions_261((int)v) == InSet(s_set261, v), $"int_Subtractions_261({v}) set=[1030..1036] [1010..1010] [-2147483648..-2147483648] [1000..1012] [1021..1021] [1036..1047] [1015..1015]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set262 = [(216L, 223L), (237L, 242L), (212L, 218L), (206L, 206L), (226L, 235L), (238L, 248L), (220L, 227L), (263L, 282L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Branches_262(uint x)
    {
        if ((x >= 216u && x <= 223u))
        {
            return true;
        }

        if ((x >= 237u && x <= 242u))
        {
            return true;
        }

        if ((x >= 212u && x <= 218u))
        {
            return true;
        }

        if (x == 206u)
        {
            return true;
        }

        if ((x >= 226u && x <= 235u))
        {
            return true;
        }

        if ((x >= 238u && x <= 248u))
        {
            return true;
        }

        if ((x >= 220u && x <= 227u))
        {
            return true;
        }

        if ((x >= 263u && x <= 282u))
        {
            return true;
        }

        return false;
    }

    private static void Check262()
    {
        foreach (long v in Probe(s_set262, 0L, 4294967295L, false, 262))
        {
            Assert.True(uint_Branches_262((uint)v) == InSet(s_set262, v), $"uint_Branches_262({v}) set=[216..223] [237..242] [212..218] [206..206] [226..235] [238..248] [220..227] [263..282]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set263 = [(-9223372036854775783L, -9223372036854775777L), (-9223372036854775807L, -9223372036854775807L), (-9223372036854775775L, -9223372036854775775L), (-9223372036854775808L, -9223372036854775806L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Flipped_263(long x) => (x > (-9223372036854775784L) && (-9223372036854775777L) >= x) || (-9223372036854775807L) == x || (-9223372036854775775L) == x || (x >= long.MinValue && (-9223372036854775806L) >= x);

    private static void Check263()
    {
        foreach (long v in Probe(s_set263, long.MinValue, 9223372036854775807L, false, 263))
        {
            Assert.True(long_Flipped_263((long)v) == InSet(s_set263, v), $"long_Flipped_263({v}) set=[-9223372036854775783..-9223372036854775777] [-9223372036854775807..-9223372036854775807] [-9223372036854775775..-9223372036854775775] [-9223372036854775808..-9223372036854775806]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set264 = [(0L, 8L), (7L, 10L), (2L, 7L), (13L, 20L), (66L, 83L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Tree_264(char x) => ((x >= '\u0000' && x <= '\u0008') || (x >= '\u0007' && x <= '\u000A') || (x >= '\u0002' && x <= '\u0007') || (x >= '\u000D' && x <= '\u0014') || (x >= '\u0042' && x <= '\u0053'));

    private static void Check264()
    {
        foreach (long v in Probe(s_set264, 0L, 65535L, true, 264))
        {
            Assert.True(char_Tree_264((char)v) == InSet(s_set264, v), $"char_Tree_264({v}) set=[0..8] [7..10] [2..7] [13..20] [66..83]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set265 = [(59L, 59L), (87L, 92L), (72L, 72L), (70L, 70L), (115L, 147L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_265(byte x) => x == 59 ? true : (x >= 87 && x <= 92) ? true : x == 72 ? true : x == 70 ? true : (x >= 115 && x <= 147) ? true : false;

    private static void Check265()
    {
        foreach (long v in Probe(s_set265, 0L, 255L, true, 265))
        {
            Assert.True(byte_Ternary_265((byte)v) == InSet(s_set265, v), $"byte_Ternary_265({v}) set=[59..59] [87..92] [72..72] [70..70] [115..147]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set266 = [(0L, 7L), (1L, 1L), (22L, 22L), (71L, 102L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Negated_266(sbyte x) => !(x is >= 0 and <= 7 or 1 or 22 or >= 71 and <= 102);

    private static void Check266()
    {
        foreach (long v in Probe(s_set266, (-128L), 127L, true, 266))
        {
            Assert.True(sbyte_Negated_266((sbyte)v) == !InSet(s_set266, v), $"sbyte_Negated_266({v}) set=[0..7] [1..1] [22..22] [71..102]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set267 = [(1145L, 1145L), (1008L, 1008L), (1114L, 1114L), (1142L, 1142L), (1142L, 1142L), (1157L, 1157L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Branches_267(short x)
    {
        if (x == 1145)
        {
            return true;
        }

        if (x == 1008)
        {
            return true;
        }

        if (x == 1114)
        {
            return true;
        }

        if (x == 1142)
        {
            return true;
        }

        if (x == 1142)
        {
            return true;
        }

        if (x == 1157)
        {
            return true;
        }

        return false;
    }

    private static void Check267()
    {
        foreach (long v in Probe(s_set267, (-32768L), 32767L, true, 267))
        {
            Assert.True(short_Branches_267((short)v) == InSet(s_set267, v), $"short_Branches_267({v}) set=[1145..1145] [1008..1008] [1114..1114] [1142..1142] [1142..1142] [1157..1157]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set268 = [(65535L, 65535L), (65456L, 65463L), (65534L, 65535L), (65535L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_268(ushort x)
    {
        if (x == 65535)
        {
            return true;
        }

        if ((x >= 65456 && x <= 65463))
        {
            return true;
        }

        if ((x >= 65534 && x <= 65535))
        {
            return true;
        }

        if (x == 65535)
        {
            return true;
        }

        return false;
    }

    private static void Check268()
    {
        foreach (long v in Probe(s_set268, 0L, 65535L, true, 268))
        {
            Assert.True(ushort_Branches_268((ushort)v) == InSet(s_set268, v), $"ushort_Branches_268({v}) set=[65535..65535] [65456..65463] [65534..65535] [65535..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set269 = [(177L, 181L), (137L, 137L), (120L, 120L), (166L, 187L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_269(int x) => x is >= 177 and <= 181 or 137 or 120 or >= 166 and <= 187;

    private static void Check269()
    {
        foreach (long v in Probe(s_set269, (-2147483648L), 2147483647L, false, 269))
        {
            Assert.True(int_IsPattern_269((int)v) == InSet(s_set269, v), $"int_IsPattern_269({v}) set=[177..181] [137..137] [120..120] [166..187]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set270 = [(1029L, 1040L), (1032L, 1032L), (1027L, 1028L), (1070L, 1091L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Subtractions_270(uint x) => (x - 1029u) <= 11u || x == 1032u || (x - 1027u) <= 1u || (x - 1070u) <= 21u;

    private static void Check270()
    {
        foreach (long v in Probe(s_set270, 0L, 4294967295L, false, 270))
        {
            Assert.True(uint_Subtractions_270((uint)v) == InSet(s_set270, v), $"uint_Subtractions_270({v}) set=[1029..1040] [1032..1032] [1027..1028] [1070..1091]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set271 = [(-9223372036854775808L, -9223372036854775808L), (-9223372036854775763L, -9223372036854775761L), (-9223372036854775793L, -9223372036854775793L), (-9223372036854775756L, -9223372036854775749L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_271(long x) => x == long.MinValue || (x >= (-9223372036854775763L) && x <= (-9223372036854775761L)) || x == (-9223372036854775793L) || (x >= (-9223372036854775756L) && x <= (-9223372036854775749L));

    private static void Check271()
    {
        foreach (long v in Probe(s_set271, long.MinValue, 9223372036854775807L, false, 271))
        {
            Assert.True(long_OrChain_271((long)v) == InSet(s_set271, v), $"long_OrChain_271({v}) set=[-9223372036854775808..-9223372036854775808] [-9223372036854775763..-9223372036854775761] [-9223372036854775793..-9223372036854775793] [-9223372036854775756..-9223372036854775749]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set272 = [(12348L, 12348L), (12411L, 12411L), (12355L, 12355L), (0L, 0L), (12436L, 12440L), (12414L, 12457L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Ternary_272(char x) => x == '\u303C' ? true : x == '\u307B' ? true : x == '\u3043' ? true : x == '\u0000' ? true : (x >= '\u3094' && x <= '\u3098') ? true : (x >= '\u307E' && x <= '\u30A9') ? true : false;

    private static void Check272()
    {
        foreach (long v in Probe(s_set272, 0L, 65535L, true, 272))
        {
            Assert.True(char_Ternary_272((char)v) == InSet(s_set272, v), $"char_Ternary_272({v}) set=[12348..12348] [12411..12411] [12355..12355] [0..0] [12436..12440] [12414..12457]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set273 = [(29L, 32L), (22L, 33L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchStmt_273(byte x)
    {
        switch (x)
        {
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:
            case 33:
                return true;
            default:
                return false;
        }
    }

    private static void Check273()
    {
        foreach (long v in Probe(s_set273, 0L, 255L, true, 273))
        {
            Assert.True(byte_SwitchStmt_273((byte)v) == InSet(s_set273, v), $"byte_SwitchStmt_273({v}) set=[29..32] [22..33]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set274 = [(-128L, -128L), (127L, 127L), (127L, 127L), (106L, 106L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_274(sbyte x) => (x < 127 ? (x < 106 ? x == (-128) : x == 106) : (x == 127 || x == 127));

    private static void Check274()
    {
        foreach (long v in Probe(s_set274, (-128L), 127L, true, 274))
        {
            Assert.True(sbyte_Tree_274((sbyte)v) == InSet(s_set274, v), $"sbyte_Tree_274({v}) set=[-128..-128] [127..127] [127..127] [106..106]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set275 = [(32667L, 32679L), (32677L, 32685L), (32679L, 32679L), (32683L, 32695L), (32676L, 32685L), (32684L, 32690L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchStmt_275(short x)
    {
        switch (x)
        {
            case >= 32667 and <= 32695:
                return true;
            default:
                return false;
        }
    }

    private static void Check275()
    {
        foreach (long v in Probe(s_set275, (-32768L), 32767L, true, 275))
        {
            Assert.True(short_SwitchStmt_275((short)v) == InSet(s_set275, v), $"short_SwitchStmt_275({v}) set=[32667..32679] [32677..32685] [32679..32679] [32683..32695] [32676..32685] [32684..32690]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set276 = [(65L, 67L), (71L, 77L), (77L, 84L), (58L, 61L), (56L, 59L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Subtractions_276(ushort x) => (uint)(x - 65) <= 2u || (uint)(x - 71) <= 6u || (uint)(x - 77) <= 7u || (uint)(x - 58) <= 3u || (uint)(x - 56) <= 3u;

    private static void Check276()
    {
        foreach (long v in Probe(s_set276, 0L, 65535L, true, 276))
        {
            Assert.True(ushort_Subtractions_276((ushort)v) == InSet(s_set276, v), $"ushort_Subtractions_276({v}) set=[65..67] [71..77] [77..84] [58..61] [56..59]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set277 = [(67L, 74L), (65L, 77L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Flipped_277(int x) => (x >= 67 && x < 75) || (x > 64 && x < 78);

    private static void Check277()
    {
        foreach (long v in Probe(s_set277, (-2147483648L), 2147483647L, false, 277))
        {
            Assert.True(int_Flipped_277((int)v) == InSet(s_set277, v), $"int_Flipped_277({v}) set=[67..74] [65..77]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set278 = [(12350L, 12350L), (12357L, 12357L), (12354L, 12362L), (12347L, 12355L), (12410L, 12432L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Flipped_278(uint x) => x == 12350u || x == 12357u || (x > 12353u && 12362u >= x) || (12347u <= x && 12355u >= x) || (x >= 12410u && x < 12433u);

    private static void Check278()
    {
        foreach (long v in Probe(s_set278, 0L, 4294967295L, false, 278))
        {
            Assert.True(uint_Flipped_278((uint)v) == InSet(s_set278, v), $"uint_Flipped_278({v}) set=[12350..12350] [12357..12357] [12354..12362] [12347..12355] [12410..12432]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set279 = [(54L, 65L), (98L, 102L), (77L, 84L), (110L, 110L), (116L, 152L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_279(long x) => !(x is >= 54L and <= 65L or >= 98L and <= 102L or >= 77L and <= 84L or 110L or >= 116L and <= 152L);

    private static void Check279()
    {
        foreach (long v in Probe(s_set279, long.MinValue, 9223372036854775807L, false, 279))
        {
            Assert.True(long_Negated_279((long)v) == !InSet(s_set279, v), $"long_Negated_279({v}) set=[54..65] [98..102] [77..84] [110..110] [116..152]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set280 = [(48L, 53L), (176L, 188L), (61L, 66L), (111L, 111L), (182L, 192L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_280(char x) => (x >= '\u0030' && x <= '\u0035') || (x >= '\u00B0' && x <= '\u00BC') || (x >= '\u003D' && x <= '\u0042') || x == '\u006F' || (x >= '\u00B6' && x <= '\u00C0');

    private static void Check280()
    {
        foreach (long v in Probe(s_set280, 0L, 65535L, true, 280))
        {
            Assert.True(char_OrChain_280((char)v) == InSet(s_set280, v), $"char_OrChain_280({v}) set=[48..53] [176..188] [61..66] [111..111] [182..192]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set281 = [(52L, 62L), (59L, 62L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Branches_281(byte x)
    {
        if ((x >= 52 && x <= 62))
        {
            return true;
        }

        if ((x >= 59 && x <= 62))
        {
            return true;
        }

        return false;
    }

    private static void Check281()
    {
        foreach (long v in Probe(s_set281, 0L, 255L, true, 281))
        {
            Assert.True(byte_Branches_281((byte)v) == InSet(s_set281, v), $"byte_Branches_281({v}) set=[52..62] [59..62]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set282 = [(60L, 60L), (71L, 79L), (64L, 64L), (57L, 66L), (46L, 46L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_IsPattern_282(sbyte x) => x is 60 or >= 71 and <= 79 or 64 or >= 57 and <= 66 or 46;

    private static void Check282()
    {
        foreach (long v in Probe(s_set282, (-128L), 127L, true, 282))
        {
            Assert.True(sbyte_IsPattern_282((sbyte)v) == InSet(s_set282, v), $"sbyte_IsPattern_282({v}) set=[60..60] [71..79] [64..64] [57..66] [46..46]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set283 = [(-3L, 2L), (-5L, -1L), (-10L, 2L), (-15L, -13L), (-8L, -2L), (45L, 54L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_OrChain_283(short x) => (x >= (-3) && x <= 2) || (x >= (-5) && x <= (-1)) || (x >= (-10) && x <= 2) || (x >= (-15) && x <= (-13)) || (x >= (-8) && x <= (-2)) || (x >= 45 && x <= 54);

    private static void Check283()
    {
        foreach (long v in Probe(s_set283, (-32768L), 32767L, true, 283))
        {
            Assert.True(short_OrChain_283((short)v) == InSet(s_set283, v), $"short_OrChain_283({v}) set=[-3..2] [-5..-1] [-10..2] [-15..-13] [-8..-2] [45..54]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set284 = [(65440L, 65446L), (65495L, 65497L), (65485L, 65496L), (65514L, 65525L), (65446L, 65457L), (65495L, 65507L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchExpr_284(ushort x) => x switch
    {
        >= 65440 and <= 65457 => true,
        >= 65485 and <= 65507 => true,
        >= 65514 and <= 65525 => true,
        _ => false,
    };

    private static void Check284()
    {
        foreach (long v in Probe(s_set284, 0L, 65535L, true, 284))
        {
            Assert.True(ushort_SwitchExpr_284((ushort)v) == InSet(s_set284, v), $"ushort_SwitchExpr_284({v}) set=[65440..65446] [65495..65497] [65485..65496] [65514..65525] [65446..65457] [65495..65507]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set285 = [(2L, 4L), (9L, 9L), (-44L, -44L), (4L, 4L), (-11L, -1L), (-8L, -4L), (20L, 61L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_OrChain_285(int x) => (x >= 2 && x <= 4) || x == 9 || x == (-44) || x == 4 || (x >= (-11) && x <= (-1)) || (x >= (-8) && x <= (-4)) || (x >= 20 && x <= 61);

    private static void Check285()
    {
        foreach (long v in Probe(s_set285, (-2147483648L), 2147483647L, false, 285))
        {
            Assert.True(int_OrChain_285((int)v) == InSet(s_set285, v), $"int_OrChain_285({v}) set=[2..4] [9..9] [-44..-44] [4..4] [-11..-1] [-8..-4] [20..61]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set286 = [(9L, 15L), (13L, 22L), (14L, 25L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_OrChain_286(uint x) => (x >= 9u && x <= 15u) || (x >= 13u && x <= 22u) || (x >= 14u && x <= 25u);

    private static void Check286()
    {
        foreach (long v in Probe(s_set286, 0L, 4294967295L, false, 286))
        {
            Assert.True(uint_OrChain_286((uint)v) == InSet(s_set286, v), $"uint_OrChain_286({v}) set=[9..15] [13..22] [14..25]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set287 = [(28L, 32L), (9L, 16L), (64L, 100L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Ternary_287(long x) => (x >= 28L && x <= 32L) ? true : (x >= 9L && x <= 16L) ? true : (x >= 64L && x <= 100L) ? true : false;

    private static void Check287()
    {
        foreach (long v in Probe(s_set287, long.MinValue, 9223372036854775807L, false, 287))
        {
            Assert.True(long_Ternary_287((long)v) == InSet(s_set287, v), $"long_Ternary_287({v}) set=[28..32] [9..16] [64..100]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set288 = [(1024L, 1024L), (1058L, 1066L), (1007L, 1019L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Ternary_288(char x) => x == '\u0400' ? true : (x >= '\u0422' && x <= '\u042A') ? true : (x >= '\u03EF' && x <= '\u03FB') ? true : false;

    private static void Check288()
    {
        foreach (long v in Probe(s_set288, 0L, 65535L, true, 288))
        {
            Assert.True(char_Ternary_288((char)v) == InSet(s_set288, v), $"char_Ternary_288({v}) set=[1024..1024] [1058..1066] [1007..1019]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set289 = [(65L, 65L), (129L, 129L), (154L, 154L), (159L, 166L), (153L, 160L), (81L, 81L), (134L, 159L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Flipped_289(byte x) => 65 == x || 129 == x || x == 154 || (x > 158 && x < 167) || (x > 152 && x <= 160) || 81 == x || (x > 133 && x <= 159);

    private static void Check289()
    {
        foreach (long v in Probe(s_set289, 0L, 255L, true, 289))
        {
            Assert.True(byte_Flipped_289((byte)v) == InSet(s_set289, v), $"byte_Flipped_289({v}) set=[65..65] [129..129] [154..154] [159..166] [153..160] [81..81] [134..159]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set290 = [(110L, 110L), (117L, 119L), (125L, 125L), (122L, 122L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchExpr_290(sbyte x) => x switch
    {
        110 => true,
        >= 117 and <= 119 => true,
        122 => true,
        125 => true,
        _ => false,
    };

    private static void Check290()
    {
        foreach (long v in Probe(s_set290, (-128L), 127L, true, 290))
        {
            Assert.True(sbyte_SwitchExpr_290((sbyte)v) == InSet(s_set290, v), $"sbyte_SwitchExpr_290({v}) set=[110..110] [117..119] [125..125] [122..122]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set291 = [(-32752L, -32747L), (-32737L, -32737L), (-32759L, -32756L), (-32764L, -32764L), (-32753L, -32743L), (-32767L, -32767L), (-32755L, -32755L), (-32706L, -32683L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Subtractions_291(short x) => (uint)(x - (-32752)) <= 5u || x == (-32737) || (uint)(x - (-32759)) <= 3u || x == (-32764) || (uint)(x - (-32753)) <= 10u || x == (-32767) || x == (-32755) || (uint)(x - (-32706)) <= 23u;

    private static void Check291()
    {
        foreach (long v in Probe(s_set291, (-32768L), 32767L, true, 291))
        {
            Assert.True(short_Subtractions_291((short)v) == InSet(s_set291, v), $"short_Subtractions_291({v}) set=[-32752..-32747] [-32737..-32737] [-32759..-32756] [-32764..-32764] [-32753..-32743] [-32767..-32767] [-32755..-32755] [-32706..-32683]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set292 = [(88L, 99L), (76L, 76L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchStmt_292(ushort x)
    {
        switch (x)
        {
            case 76:
            case 88:
            case 89:
            case 90:
            case 91:
            case 92:
            case 93:
            case 94:
            case 95:
            case 96:
            case 97:
            case 98:
            case 99:
                return true;
            default:
                return false;
        }
    }

    private static void Check292()
    {
        foreach (long v in Probe(s_set292, 0L, 65535L, true, 292))
        {
            Assert.True(ushort_SwitchStmt_292((ushort)v) == InSet(s_set292, v), $"ushort_SwitchStmt_292({v}) set=[88..99] [76..76]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set293 = [(1069L, 1069L), (1078L, 1088L), (1080L, 1080L), (1097L, 1097L), (1050L, 1050L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_293(int x) => x is 1069 or >= 1078 and <= 1088 or 1080 or 1097 or 1050;

    private static void Check293()
    {
        foreach (long v in Probe(s_set293, (-2147483648L), 2147483647L, false, 293))
        {
            Assert.True(int_IsPattern_293((int)v) == InSet(s_set293, v), $"int_IsPattern_293({v}) set=[1069..1069] [1078..1088] [1080..1080] [1097..1097] [1050..1050]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set294 = [(0L, 8L), (4L, 13L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchStmt_294(uint x)
    {
        switch (x)
        {
            case >= 0u and <= 13u:
                return true;
            default:
                return false;
        }
    }

    private static void Check294()
    {
        foreach (long v in Probe(s_set294, 0L, 4294967295L, false, 294))
        {
            Assert.True(uint_SwitchStmt_294((uint)v) == InSet(s_set294, v), $"uint_SwitchStmt_294({v}) set=[0..8] [4..13]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set295 = [(1088L, 1088L), (1049L, 1049L), (1032L, 1044L), (1107L, 1118L), (1000L, 1010L), (1102L, 1110L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Ternary_295(long x) => x == 1088L ? true : x == 1049L ? true : (x >= 1032L && x <= 1044L) ? true : (x >= 1107L && x <= 1118L) ? true : (x >= 1000L && x <= 1010L) ? true : (x >= 1102L && x <= 1110L) ? true : false;

    private static void Check295()
    {
        foreach (long v in Probe(s_set295, long.MinValue, 9223372036854775807L, false, 295))
        {
            Assert.True(long_Ternary_295((long)v) == InSet(s_set295, v), $"long_Ternary_295({v}) set=[1088..1088] [1049..1049] [1032..1044] [1107..1118] [1000..1010] [1102..1110]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set296 = [(36L, 44L), (37L, 42L), (48L, 54L), (34L, 34L), (101L, 106L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Negated_296(char x) => !(x is >= '\u0024' and <= '\u002C' or >= '\u0025' and <= '\u002A' or >= '\u0030' and <= '\u0036' or '\u0022' or >= '\u0065' and <= '\u006A');

    private static void Check296()
    {
        foreach (long v in Probe(s_set296, 0L, 65535L, true, 296))
        {
            Assert.True(char_Negated_296((char)v) == !InSet(s_set296, v), $"char_Negated_296({v}) set=[36..44] [37..42] [48..54] [34..34] [101..106]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set297 = [(255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Flipped_297(byte x) => 255 == x || 255 == x || 255 == x || 255 == x || x == 255;

    private static void Check297()
    {
        foreach (long v in Probe(s_set297, 0L, 255L, true, 297))
        {
            Assert.True(byte_Flipped_297((byte)v) == InSet(s_set297, v), $"byte_Flipped_297({v}) set=[255..255] [255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set298 = [(-42L, -33L), (-29L, -21L), (-44L, -40L), (-45L, -45L), (-48L, -40L), (12L, 38L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Negated_298(sbyte x) => !(x is >= (-42) and <= (-33) or >= (-29) and <= (-21) or >= (-44) and <= (-40) or (-45) or >= (-48) and <= (-40) or >= 12 and <= 38);

    private static void Check298()
    {
        foreach (long v in Probe(s_set298, (-128L), 127L, true, 298))
        {
            Assert.True(sbyte_Negated_298((sbyte)v) == !InSet(s_set298, v), $"sbyte_Negated_298({v}) set=[-42..-33] [-29..-21] [-44..-40] [-45..-45] [-48..-40] [12..38]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set299 = [(68L, 68L), (70L, 71L), (52L, 55L), (51L, 51L), (-5L, 4L), (44L, 44L), (50L, 57L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchStmt_299(short x)
    {
        switch (x)
        {
            case (-5):
            case (-4):
            case (-3):
            case (-2):
            case (-1):
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 44:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
            case 68:
            case 70:
            case 71:
                return true;
            default:
                return false;
        }
    }

    private static void Check299()
    {
        foreach (long v in Probe(s_set299, (-32768L), 32767L, true, 299))
        {
            Assert.True(short_SwitchStmt_299((short)v) == InSet(s_set299, v), $"short_SwitchStmt_299({v}) set=[68..68] [70..71] [52..55] [51..51] [-5..4] [44..44] [50..57]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set300 = [(91L, 101L), (98L, 109L), (5L, 5L), (63L, 63L), (109L, 109L), (30L, 30L), (68L, 106L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchStmt_300(ushort x)
    {
        switch (x)
        {
            case 5:
            case 30:
            case 63:
            case >= 68 and <= 109:
                return true;
            default:
                return false;
        }
    }

    private static void Check300()
    {
        foreach (long v in Probe(s_set300, 0L, 65535L, true, 300))
        {
            Assert.True(ushort_SwitchStmt_300((ushort)v) == InSet(s_set300, v), $"ushort_SwitchStmt_300({v}) set=[91..101] [98..109] [5..5] [63..63] [109..109] [30..30] [68..106]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set301 = [(27L, 33L), (22L, 22L), (13L, 34L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Negated_301(int x) => !(x is >= 27 and <= 33 or 22 or >= 13 and <= 34);

    private static void Check301()
    {
        foreach (long v in Probe(s_set301, (-2147483648L), 2147483647L, false, 301))
        {
            Assert.True(int_Negated_301((int)v) == !InSet(s_set301, v), $"int_Negated_301({v}) set=[27..33] [22..22] [13..34]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set302 = [(103L, 103L), (50L, 50L), (90L, 100L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_OrChain_302(uint x) => x == 103u || x == 50u || (x >= 90u && x <= 100u);

    private static void Check302()
    {
        foreach (long v in Probe(s_set302, 0L, 4294967295L, false, 302))
        {
            Assert.True(uint_OrChain_302((uint)v) == InSet(s_set302, v), $"uint_OrChain_302({v}) set=[103..103] [50..50] [90..100]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set303 = [(-9223372036854775650L, -9223372036854775640L), (-9223372036854775721L, -9223372036854775721L), (-9223372036854775687L, -9223372036854775687L), (-9223372036854775759L, -9223372036854775757L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Tree_303(long x) => (x < (-9223372036854775687L) ? (x < (-9223372036854775721L) ? (x >= (-9223372036854775759L) && x <= (-9223372036854775757L)) : x == (-9223372036854775721L)) : (x < (-9223372036854775650L) ? x == (-9223372036854775687L) : (x >= (-9223372036854775650L) && x <= (-9223372036854775640L))));

    private static void Check303()
    {
        foreach (long v in Probe(s_set303, long.MinValue, 9223372036854775807L, false, 303))
        {
            Assert.True(long_Tree_303((long)v) == InSet(s_set303, v), $"long_Tree_303({v}) set=[-9223372036854775650..-9223372036854775640] [-9223372036854775721..-9223372036854775721] [-9223372036854775687..-9223372036854775687] [-9223372036854775759..-9223372036854775757]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set304 = [(129L, 129L), (124L, 133L), (117L, 121L), (117L, 129L), (134L, 139L), (196L, 196L), (173L, 173L), (163L, 186L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Flipped_304(char x) => '\u0081' == x || (x > '\u007B' && x < '\u0086') || (x >= '\u0075' && '\u0079' >= x) || (x > '\u0074' && x < '\u0082') || (x > '\u0085' && x < '\u008C') || '\u00C4' == x || x == '\u00AD' || ('\u00A3' <= x && x < '\u00BB');

    private static void Check304()
    {
        foreach (long v in Probe(s_set304, 0L, 65535L, true, 304))
        {
            Assert.True(char_Flipped_304((char)v) == InSet(s_set304, v), $"char_Flipped_304({v}) set=[129..129] [124..133] [117..121] [117..129] [134..139] [196..196] [173..173] [163..186]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set305 = [(143L, 143L), (131L, 141L), (114L, 116L), (145L, 145L), (167L, 211L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchExpr_305(byte x) => x switch
    {
        >= 114 and <= 116 => true,
        >= 131 and <= 141 => true,
        143 => true,
        145 => true,
        >= 167 and <= 211 => true,
        _ => false,
    };

    private static void Check305()
    {
        foreach (long v in Probe(s_set305, 0L, 255L, true, 305))
        {
            Assert.True(byte_SwitchExpr_305((byte)v) == InSet(s_set305, v), $"byte_SwitchExpr_305({v}) set=[143..143] [131..141] [114..116] [145..145] [167..211]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set306 = [(-50L, -40L), (-34L, -34L), (-46L, -38L), (5L, 15L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_SwitchStmt_306(sbyte x)
    {
        switch (x)
        {
            case (-50):
            case (-49):
            case (-48):
            case (-47):
            case (-46):
            case (-45):
            case (-44):
            case (-43):
            case (-42):
            case (-41):
            case (-40):
            case (-39):
            case (-38):
            case (-34):
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
                return true;
            default:
                return false;
        }
    }

    private static void Check306()
    {
        foreach (long v in Probe(s_set306, (-128L), 127L, true, 306))
        {
            Assert.True(sbyte_SwitchStmt_306((sbyte)v) == InSet(s_set306, v), $"sbyte_SwitchStmt_306({v}) set=[-50..-40] [-34..-34] [-46..-38] [5..15]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set307 = [(1053L, 1056L), (1060L, 1064L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Flipped_307(short x) => (x > 1052 && 1056 >= x) || (x >= 1060 && 1064 >= x);

    private static void Check307()
    {
        foreach (long v in Probe(s_set307, (-32768L), 32767L, true, 307))
        {
            Assert.True(short_Flipped_307((short)v) == InSet(s_set307, v), $"short_Flipped_307({v}) set=[1053..1056] [1060..1064]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set308 = [(108L, 108L), (128L, 128L), (112L, 123L), (165L, 188L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Flipped_308(ushort x) => 108 == x || 128 == x || (x > 111 && x <= 123) || (x > 164 && x < 189);

    private static void Check308()
    {
        foreach (long v in Probe(s_set308, 0L, 65535L, true, 308))
        {
            Assert.True(ushort_Flipped_308((ushort)v) == InSet(s_set308, v), $"ushort_Flipped_308({v}) set=[108..108] [128..128] [112..123] [165..188]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set309 = [(-2147483648L, -2147483638L), (42L, 50L), (70L, 72L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_OrChain_309(int x) => (x >= int.MinValue && x <= (-2147483638)) || (x >= 42 && x <= 50) || (x >= 70 && x <= 72);

    private static void Check309()
    {
        foreach (long v in Probe(s_set309, (-2147483648L), 2147483647L, false, 309))
        {
            Assert.True(int_OrChain_309((int)v) == InSet(s_set309, v), $"int_OrChain_309({v}) set=[-2147483648..-2147483638] [42..50] [70..72]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set310 = [(1008L, 1010L), (1055L, 1065L), (1064L, 1073L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Subtractions_310(uint x) => (x - 1008u) <= 2u || (x - 1055u) <= 10u || (x - 1064u) <= 9u;

    private static void Check310()
    {
        foreach (long v in Probe(s_set310, 0L, 4294967295L, false, 310))
        {
            Assert.True(uint_Subtractions_310((uint)v) == InSet(s_set310, v), $"uint_Subtractions_310({v}) set=[1008..1010] [1055..1065] [1064..1073]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set311 = [(1191L, 1194L), (1045L, 1045L), (1043L, 1055L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_311(long x) => (x >= 1191L && x <= 1194L) || x == 1045L || (x >= 1043L && x <= 1055L);

    private static void Check311()
    {
        foreach (long v in Probe(s_set311, long.MinValue, 9223372036854775807L, false, 311))
        {
            Assert.True(long_OrChain_311((long)v) == InSet(s_set311, v), $"long_OrChain_311({v}) set=[1191..1194] [1045..1045] [1043..1055]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set312 = [(82L, 88L), (83L, 90L), (67L, 67L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_312(char x) => (x >= '\u0052' && x <= '\u0058') || (x >= '\u0053' && x <= '\u005A') || x == '\u0043';

    private static void Check312()
    {
        foreach (long v in Probe(s_set312, 0L, 65535L, true, 312))
        {
            Assert.True(char_OrChain_312((char)v) == InSet(s_set312, v), $"char_OrChain_312({v}) set=[82..88] [83..90] [67..67]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set313 = [(244L, 255L), (145L, 150L), (120L, 124L), (99L, 104L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Branches_313(byte x)
    {
        if ((x >= 244 && x <= 255))
        {
            return true;
        }

        if ((x >= 145 && x <= 150))
        {
            return true;
        }

        if ((x >= 120 && x <= 124))
        {
            return true;
        }

        if ((x >= 99 && x <= 104))
        {
            return true;
        }

        return false;
    }

    private static void Check313()
    {
        foreach (long v in Probe(s_set313, 0L, 255L, true, 313))
        {
            Assert.True(byte_Branches_313((byte)v) == InSet(s_set313, v), $"byte_Branches_313({v}) set=[244..255] [145..150] [120..124] [99..104]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set314 = [(127L, 127L), (80L, 91L), (127L, 127L), (127L, 127L), (93L, 93L), (51L, 51L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Subtractions_314(sbyte x) => x == 127 || (uint)(x - 80) <= 11u || x == 127 || x == 127 || x == 93 || x == 51;

    private static void Check314()
    {
        foreach (long v in Probe(s_set314, (-128L), 127L, true, 314))
        {
            Assert.True(sbyte_Subtractions_314((sbyte)v) == InSet(s_set314, v), $"sbyte_Subtractions_314({v}) set=[127..127] [80..91] [127..127] [127..127] [93..93] [51..51]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set315 = [(-17L, -14L), (1L, 1L), (2L, 10L), (11L, 11L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Tree_315(short x) => (x < 2 ? (x < 1 ? (x >= (-17) && x <= (-14)) : x == 1) : (x < 11 ? (x >= 2 && x <= 10) : x == 11));

    private static void Check315()
    {
        foreach (long v in Probe(s_set315, (-32768L), 32767L, true, 315))
        {
            Assert.True(short_Tree_315((short)v) == InSet(s_set315, v), $"short_Tree_315({v}) set=[-17..-14] [1..1] [2..10] [11..11]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set316 = [(182L, 182L), (187L, 187L), (156L, 156L), (171L, 171L), (156L, 161L), (168L, 174L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_316(ushort x)
    {
        if (x == 182)
        {
            return true;
        }

        if (x == 187)
        {
            return true;
        }

        if (x == 156)
        {
            return true;
        }

        if (x == 171)
        {
            return true;
        }

        if ((x >= 156 && x <= 161))
        {
            return true;
        }

        if ((x >= 168 && x <= 174))
        {
            return true;
        }

        return false;
    }

    private static void Check316()
    {
        foreach (long v in Probe(s_set316, 0L, 65535L, true, 316))
        {
            Assert.True(ushort_Branches_316((ushort)v) == InSet(s_set316, v), $"ushort_Branches_316({v}) set=[182..182] [187..187] [156..156] [171..171] [156..161] [168..174]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set317 = [(1048625L, 1048625L), (1048596L, 1048596L), (1048628L, 1048629L), (1048663L, 1048669L), (1048653L, 1048653L), (1048672L, 1048672L), (1048661L, 1048667L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Subtractions_317(int x) => x == 1048625 || x == 1048596 || (uint)(x - 1048628) <= 1u || (uint)(x - 1048663) <= 6u || x == 1048653 || x == 1048672 || (uint)(x - 1048661) <= 6u;

    private static void Check317()
    {
        foreach (long v in Probe(s_set317, (-2147483648L), 2147483647L, false, 317))
        {
            Assert.True(int_Subtractions_317((int)v) == InSet(s_set317, v), $"int_Subtractions_317({v}) set=[1048625..1048625] [1048596..1048596] [1048628..1048629] [1048663..1048669] [1048653..1048653] [1048672..1048672] [1048661..1048667]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set318 = [(4294967206L, 4294967206L), (4294967199L, 4294967199L), (4294967210L, 4294967211L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_318(uint x) => x switch
    {
        4294967199u => true,
        4294967206u => true,
        >= 4294967210u and <= 4294967211u => true,
        _ => false,
    };

    private static void Check318()
    {
        foreach (long v in Probe(s_set318, 0L, 4294967295L, false, 318))
        {
            Assert.True(uint_SwitchExpr_318((uint)v) == InSet(s_set318, v), $"uint_SwitchExpr_318({v}) set=[4294967206..4294967206] [4294967199..4294967199] [4294967210..4294967211]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set319 = [(9223372036854775796L, 9223372036854775807L), (75L, 79L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_OrChain_319(long x) => (x >= 9223372036854775796L && x <= 9223372036854775807L) || (x >= 75L && x <= 79L);

    private static void Check319()
    {
        foreach (long v in Probe(s_set319, long.MinValue, 9223372036854775807L, false, 319))
        {
            Assert.True(long_OrChain_319((long)v) == InSet(s_set319, v), $"long_OrChain_319({v}) set=[9223372036854775796..9223372036854775807] [75..79]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set320 = [(33L, 38L), (76L, 83L), (64L, 99L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_IsPattern_320(char x) => x is >= '\u0021' and <= '\u0026' or >= '\u004C' and <= '\u0053' or >= '\u0040' and <= '\u0063';

    private static void Check320()
    {
        foreach (long v in Probe(s_set320, 0L, 65535L, true, 320))
        {
            Assert.True(char_IsPattern_320((char)v) == InSet(s_set320, v), $"char_IsPattern_320({v}) set=[33..38] [76..83] [64..99]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set321 = [(255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Ternary_321(byte x) => x == 255 ? true : x == 255 ? true : x == 255 ? true : x == 255 ? true : false;

    private static void Check321()
    {
        foreach (long v in Probe(s_set321, 0L, 255L, true, 321))
        {
            Assert.True(byte_Ternary_321((byte)v) == InSet(s_set321, v), $"byte_Ternary_321({v}) set=[255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set322 = [(-7L, -4L), (-13L, -4L), (-16L, -13L), (-20L, -20L), (45L, 62L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_OrChain_322(sbyte x) => (x >= (-7) && x <= (-4)) || (x >= (-13) && x <= (-4)) || (x >= (-16) && x <= (-13)) || x == (-20) || (x >= 45 && x <= 62);

    private static void Check322()
    {
        foreach (long v in Probe(s_set322, (-128L), 127L, true, 322))
        {
            Assert.True(sbyte_OrChain_322((sbyte)v) == InSet(s_set322, v), $"sbyte_OrChain_322({v}) set=[-7..-4] [-13..-4] [-16..-13] [-20..-20] [45..62]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set323 = [(32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L), (32767L, 32767L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchExpr_323(short x) => x switch
    {
        32767 => true,
        _ => false,
    };

    private static void Check323()
    {
        foreach (long v in Probe(s_set323, (-32768L), 32767L, true, 323))
        {
            Assert.True(short_SwitchExpr_323((short)v) == InSet(s_set323, v), $"short_SwitchExpr_323({v}) set=[32767..32767] [32767..32767] [32767..32767] [32767..32767] [32767..32767]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set324 = [(1L, 1L), (14L, 21L), (15L, 23L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_324(ushort x)
    {
        if (x == 1)
        {
            return true;
        }

        if ((x >= 14 && x <= 21))
        {
            return true;
        }

        if ((x >= 15 && x <= 23))
        {
            return true;
        }

        return false;
    }

    private static void Check324()
    {
        foreach (long v in Probe(s_set324, 0L, 65535L, true, 324))
        {
            Assert.True(ushort_Branches_324((ushort)v) == InSet(s_set324, v), $"ushort_Branches_324({v}) set=[1..1] [14..21] [15..23]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set325 = [(2147483549L, 2147483549L), (2147483562L, 2147483573L), (2147483614L, 2147483631L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchExpr_325(int x) => x switch
    {
        2147483549 => true,
        >= 2147483562 and <= 2147483573 => true,
        >= 2147483614 and <= 2147483631 => true,
        _ => false,
    };

    private static void Check325()
    {
        foreach (long v in Probe(s_set325, (-2147483648L), 2147483647L, false, 325))
        {
            Assert.True(int_SwitchExpr_325((int)v) == InSet(s_set325, v), $"int_SwitchExpr_325({v}) set=[2147483549..2147483549] [2147483562..2147483573] [2147483614..2147483631]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set326 = [(4294967207L, 4294967207L), (4294967295L, 4294967295L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Ternary_326(uint x) => x == 4294967207u ? true : x == 4294967295u ? true : false;

    private static void Check326()
    {
        foreach (long v in Probe(s_set326, 0L, 4294967295L, false, 326))
        {
            Assert.True(uint_Ternary_326((uint)v) == InSet(s_set326, v), $"uint_Ternary_326({v}) set=[4294967207..4294967207] [4294967295..4294967295]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set327 = [(77L, 77L), (84L, 87L), (-9223372036854775808L, -9223372036854775806L), (73L, 73L), (67L, 67L), (76L, 76L), (82L, 94L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_SwitchStmt_327(long x)
    {
        switch (x)
        {
            case long.MinValue:
            case (-9223372036854775807L):
            case (-9223372036854775806L):
            case 67L:
            case 73L:
            case 76L:
            case 77L:
            case 82L:
            case 83L:
            case 84L:
            case 85L:
            case 86L:
            case 87L:
            case 88L:
            case 89L:
            case 90L:
            case 91L:
            case 92L:
            case 93L:
            case 94L:
                return true;
            default:
                return false;
        }
    }

    private static void Check327()
    {
        foreach (long v in Probe(s_set327, long.MinValue, 9223372036854775807L, false, 327))
        {
            Assert.True(long_SwitchStmt_327((long)v) == InSet(s_set327, v), $"long_SwitchStmt_327({v}) set=[77..77] [84..87] [-9223372036854775808..-9223372036854775806] [73..73] [67..67] [76..76] [82..94]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set328 = [(117L, 117L), (102L, 102L), (104L, 115L), (67L, 70L), (80L, 85L), (128L, 156L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_OrChain_328(char x) => x == '\u0075' || x == '\u0066' || (x >= '\u0068' && x <= '\u0073') || (x >= '\u0043' && x <= '\u0046') || (x >= '\u0050' && x <= '\u0055') || (x >= '\u0080' && x <= '\u009C');

    private static void Check328()
    {
        foreach (long v in Probe(s_set328, 0L, 65535L, true, 328))
        {
            Assert.True(char_OrChain_328((char)v) == InSet(s_set328, v), $"char_OrChain_328({v}) set=[117..117] [102..102] [104..115] [67..70] [80..85] [128..156]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set329 = [(245L, 255L), (106L, 117L), (245L, 255L), (166L, 197L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Branches_329(byte x)
    {
        if ((x >= 245 && x <= 255))
        {
            return true;
        }

        if ((x >= 106 && x <= 117))
        {
            return true;
        }

        if ((x >= 245 && x <= 255))
        {
            return true;
        }

        if ((x >= 166 && x <= 197))
        {
            return true;
        }

        return false;
    }

    private static void Check329()
    {
        foreach (long v in Probe(s_set329, 0L, 255L, true, 329))
        {
            Assert.True(byte_Branches_329((byte)v) == InSet(s_set329, v), $"byte_Branches_329({v}) set=[245..255] [106..117] [245..255] [166..197]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set330 = [(113L, 123L), (109L, 109L), (126L, 127L), (118L, 126L), (109L, 109L), (109L, 120L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Branches_330(sbyte x)
    {
        if ((x >= 113 && x <= 123))
        {
            return true;
        }

        if (x == 109)
        {
            return true;
        }

        if ((x >= 126 && x <= 127))
        {
            return true;
        }

        if ((x >= 118 && x <= 126))
        {
            return true;
        }

        if (x == 109)
        {
            return true;
        }

        if ((x >= 109 && x <= 120))
        {
            return true;
        }

        return false;
    }

    private static void Check330()
    {
        foreach (long v in Probe(s_set330, (-128L), 127L, true, 330))
        {
            Assert.True(sbyte_Branches_330((sbyte)v) == InSet(s_set330, v), $"sbyte_Branches_330({v}) set=[113..123] [109..109] [126..127] [118..126] [109..109] [109..120]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set331 = [(32767L, 32767L), (-32768L, -32768L), (32767L, 32767L), (32767L, 32767L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_SwitchExpr_331(short x) => x switch
    {
        (-32768) => true,
        32767 => true,
        _ => false,
    };

    private static void Check331()
    {
        foreach (long v in Probe(s_set331, (-32768L), 32767L, true, 331))
        {
            Assert.True(short_SwitchExpr_331((short)v) == InSet(s_set331, v), $"short_SwitchExpr_331({v}) set=[32767..32767] [-32768..-32768] [32767..32767] [32767..32767]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set332 = [(110L, 110L), (103L, 103L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_SwitchStmt_332(ushort x)
    {
        switch (x)
        {
            case 103:
            case 110:
                return true;
            default:
                return false;
        }
    }

    private static void Check332()
    {
        foreach (long v in Probe(s_set332, 0L, 65535L, true, 332))
        {
            Assert.True(ushort_SwitchStmt_332((ushort)v) == InSet(s_set332, v), $"ushort_SwitchStmt_332({v}) set=[110..110] [103..103]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set333 = [(2147483553L, 2147483561L), (2147483596L, 2147483603L), (2147483602L, 2147483608L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_333(int x)
    {
        switch (x)
        {
            case 2147483553:
            case 2147483554:
            case 2147483555:
            case 2147483556:
            case 2147483557:
            case 2147483558:
            case 2147483559:
            case 2147483560:
            case 2147483561:
            case 2147483596:
            case 2147483597:
            case 2147483598:
            case 2147483599:
            case 2147483600:
            case 2147483601:
            case 2147483602:
            case 2147483603:
            case 2147483604:
            case 2147483605:
            case 2147483606:
            case 2147483607:
            case 2147483608:
                return true;
            default:
                return false;
        }
    }

    private static void Check333()
    {
        foreach (long v in Probe(s_set333, (-2147483648L), 2147483647L, false, 333))
        {
            Assert.True(int_SwitchStmt_333((int)v) == InSet(s_set333, v), $"int_SwitchStmt_333({v}) set=[2147483553..2147483561] [2147483596..2147483603] [2147483602..2147483608]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set334 = [(46L, 54L), (47L, 47L), (101L, 142L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_OrChain_334(uint x) => (x >= 46u && x <= 54u) || x == 47u || (x >= 101u && x <= 142u);

    private static void Check334()
    {
        foreach (long v in Probe(s_set334, 0L, 4294967295L, false, 334))
        {
            Assert.True(uint_OrChain_334((uint)v) == InSet(s_set334, v), $"uint_OrChain_334({v}) set=[46..54] [47..47] [101..142]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set335 = [(-9223372036854775808L, -9223372036854775800L), (82L, 82L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Negated_335(long x) => !(x is >= long.MinValue and <= (-9223372036854775800L) or 82L);

    private static void Check335()
    {
        foreach (long v in Probe(s_set335, long.MinValue, 9223372036854775807L, false, 335))
        {
            Assert.True(long_Negated_335((long)v) == !InSet(s_set335, v), $"long_Negated_335({v}) set=[-9223372036854775808..-9223372036854775800] [82..82]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set336 = [(2L, 2L), (1L, 1L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Subtractions_336(char x) => x == '\u0002' || x == '\u0001';

    private static void Check336()
    {
        foreach (long v in Probe(s_set336, 0L, 65535L, true, 336))
        {
            Assert.True(char_Subtractions_336((char)v) == InSet(s_set336, v), $"char_Subtractions_336({v}) set=[2..2] [1..1]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set337 = [(236L, 236L), (0L, 0L), (255L, 255L), (109L, 109L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Tree_337(byte x) => (x < 236 ? (x < 109 ? x == 0 : x == 109) : (x < 255 ? x == 236 : (x == 255 || x == 255)));

    private static void Check337()
    {
        foreach (long v in Probe(s_set337, 0L, 255L, true, 337))
        {
            Assert.True(byte_Tree_337((byte)v) == InSet(s_set337, v), $"byte_Tree_337({v}) set=[236..236] [0..0] [255..255] [109..109] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set338 = [(20L, 20L), (30L, 34L), (33L, 36L), (23L, 23L), (67L, 104L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_IsPattern_338(sbyte x) => x is 20 or >= 30 and <= 34 or >= 33 and <= 36 or 23 or >= 67 and <= 104;

    private static void Check338()
    {
        foreach (long v in Probe(s_set338, (-128L), 127L, true, 338))
        {
            Assert.True(sbyte_IsPattern_338((sbyte)v) == InSet(s_set338, v), $"sbyte_IsPattern_338({v}) set=[20..20] [30..34] [33..36] [23..23] [67..104]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set339 = [(-4L, -2L), (-1L, -1L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Tree_339(short x) => (x < (-1) ? (x >= (-4) && x <= (-2)) : x == (-1));

    private static void Check339()
    {
        foreach (long v in Probe(s_set339, (-32768L), 32767L, true, 339))
        {
            Assert.True(short_Tree_339((short)v) == InSet(s_set339, v), $"short_Tree_339({v}) set=[-4..-2] [-1..-1]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set340 = [(12399L, 12410L), (12381L, 12381L), (12359L, 12359L), (12369L, 12381L), (12414L, 12428L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_OrChain_340(ushort x) => (x >= 12399 && x <= 12410) || x == 12381 || x == 12359 || (x >= 12369 && x <= 12381) || (x >= 12414 && x <= 12428);

    private static void Check340()
    {
        foreach (long v in Probe(s_set340, 0L, 65535L, true, 340))
        {
            Assert.True(ushort_OrChain_340((ushort)v) == InSet(s_set340, v), $"ushort_OrChain_340({v}) set=[12399..12410] [12381..12381] [12359..12359] [12369..12381] [12414..12428]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set341 = [(-34L, -23L), (-28L, -25L), (-23L, -13L), (-25L, -25L), (-34L, -31L), (20L, 42L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Tree_341(int x) => (x < (-25) ? ((x >= (-34) && x <= (-23)) || (x >= (-34) && x <= (-31)) || (x >= (-28) && x <= (-25))) : ((x >= (-34) && x <= (-23)) || (x >= (-28) && x <= (-25)) || x == (-25) || (x >= (-23) && x <= (-13)) || (x >= 20 && x <= 42)));

    private static void Check341()
    {
        foreach (long v in Probe(s_set341, (-2147483648L), 2147483647L, false, 341))
        {
            Assert.True(int_Tree_341((int)v) == InSet(s_set341, v), $"int_Tree_341({v}) set=[-34..-23] [-28..-25] [-23..-13] [-25..-25] [-34..-31] [20..42]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set342 = [(1048581L, 1048587L), (1048617L, 1048618L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Subtractions_342(uint x) => (x - 1048581u) <= 6u || (x - 1048617u) <= 1u;

    private static void Check342()
    {
        foreach (long v in Probe(s_set342, 0L, 4294967295L, false, 342))
        {
            Assert.True(uint_Subtractions_342((uint)v) == InSet(s_set342, v), $"uint_Subtractions_342({v}) set=[1048581..1048587] [1048617..1048618]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set343 = [(63L, 72L), (199L, 204L), (109L, 109L), (104L, 104L), (44L, 56L), (163L, 163L), (-9223372036854775808L, -9223372036854775808L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Branches_343(long x)
    {
        if ((x >= 63L && x <= 72L))
        {
            return true;
        }

        if ((x >= 199L && x <= 204L))
        {
            return true;
        }

        if (x == 109L)
        {
            return true;
        }

        if (x == 104L)
        {
            return true;
        }

        if ((x >= 44L && x <= 56L))
        {
            return true;
        }

        if (x == 163L)
        {
            return true;
        }

        if (x == long.MinValue)
        {
            return true;
        }

        return false;
    }

    private static void Check343()
    {
        foreach (long v in Probe(s_set343, long.MinValue, 9223372036854775807L, false, 343))
        {
            Assert.True(long_Branches_343((long)v) == InSet(s_set343, v), $"long_Branches_343({v}) set=[63..72] [199..204] [109..109] [104..104] [44..56] [163..163] [-9223372036854775808..-9223372036854775808]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set344 = [(65453L, 65463L), (65436L, 65438L), (65446L, 65449L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchStmt_344(char x)
    {
        switch (x)
        {
            case '\uFF9C':
            case '\uFF9D':
            case '\uFF9E':
            case '\uFFA6':
            case '\uFFA7':
            case '\uFFA8':
            case '\uFFA9':
            case '\uFFAD':
            case '\uFFAE':
            case '\uFFAF':
            case '\uFFB0':
            case '\uFFB1':
            case '\uFFB2':
            case '\uFFB3':
            case '\uFFB4':
            case '\uFFB5':
            case '\uFFB6':
            case '\uFFB7':
                return true;
            default:
                return false;
        }
    }

    private static void Check344()
    {
        foreach (long v in Probe(s_set344, 0L, 65535L, true, 344))
        {
            Assert.True(char_SwitchStmt_344((char)v) == InSet(s_set344, v), $"char_SwitchStmt_344({v}) set=[65453..65463] [65436..65438] [65446..65449]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set345 = [(80L, 88L), (69L, 69L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_SwitchStmt_345(byte x)
    {
        switch (x)
        {
            case 69:
            case 80:
            case 81:
            case 82:
            case 83:
            case 84:
            case 85:
            case 86:
            case 87:
            case 88:
                return true;
            default:
                return false;
        }
    }

    private static void Check345()
    {
        foreach (long v in Probe(s_set345, 0L, 255L, true, 345))
        {
            Assert.True(byte_SwitchStmt_345((byte)v) == InSet(s_set345, v), $"byte_SwitchStmt_345({v}) set=[80..88] [69..69]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set346 = [(110L, 113L), (127L, 127L), (105L, 105L), (127L, 127L), (127L, 127L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_346(sbyte x) => (x < 127 ? (x < 110 ? x == 105 : (x >= 110 && x <= 113)) : (x == 127 || x == 127 || x == 127));

    private static void Check346()
    {
        foreach (long v in Probe(s_set346, (-128L), 127L, true, 346))
        {
            Assert.True(sbyte_Tree_346((sbyte)v) == InSet(s_set346, v), $"sbyte_Tree_346({v}) set=[110..113] [127..127] [105..105] [127..127] [127..127]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set347 = [(-31L, -31L), (-42L, -42L), (-49L, -42L), (-42L, -40L), (-50L, -39L), (-46L, -46L), (-37L, -37L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Tree_347(short x) => (x < (-42) ? ((x >= (-50) && x <= (-39)) || (x >= (-49) && x <= (-42)) || x == (-46)) : ((x >= (-50) && x <= (-39)) || (x >= (-49) && x <= (-42)) || x == (-42) || (x >= (-42) && x <= (-40)) || x == (-37) || x == (-31)));

    private static void Check347()
    {
        foreach (long v in Probe(s_set347, (-32768L), 32767L, true, 347))
        {
            Assert.True(short_Tree_347((short)v) == InSet(s_set347, v), $"short_Tree_347({v}) set=[-31..-31] [-42..-42] [-49..-42] [-42..-40] [-50..-39] [-46..-46] [-37..-37]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set348 = [(127L, 137L), (160L, 171L), (121L, 128L), (130L, 137L), (132L, 142L), (118L, 118L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Subtractions_348(ushort x) => (uint)(x - 127) <= 10u || (uint)(x - 160) <= 11u || (uint)(x - 121) <= 7u || (uint)(x - 130) <= 7u || (uint)(x - 132) <= 10u || x == 118;

    private static void Check348()
    {
        foreach (long v in Probe(s_set348, 0L, 65535L, true, 348))
        {
            Assert.True(ushort_Subtractions_348((ushort)v) == InSet(s_set348, v), $"ushort_Subtractions_348({v}) set=[127..137] [160..171] [121..128] [130..137] [132..142] [118..118]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set349 = [(74L, 86L), (61L, 61L), (63L, 68L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_349(int x)
    {
        switch (x)
        {
            case 61:
            case 63:
            case 64:
            case 65:
            case 66:
            case 67:
            case 68:
            case 74:
            case 75:
            case 76:
            case 77:
            case 78:
            case 79:
            case 80:
            case 81:
            case 82:
            case 83:
            case 84:
            case 85:
            case 86:
                return true;
            default:
                return false;
        }
    }

    private static void Check349()
    {
        foreach (long v in Probe(s_set349, (-2147483648L), 2147483647L, false, 349))
        {
            Assert.True(int_SwitchStmt_349((int)v) == InSet(s_set349, v), $"int_SwitchStmt_349({v}) set=[74..86] [61..61] [63..68]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set350 = [(164L, 176L), (158L, 158L), (151L, 160L), (188L, 188L), (152L, 152L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Subtractions_350(uint x) => (x - 164u) <= 12u || x == 158u || (x - 151u) <= 9u || x == 188u || x == 152u;

    private static void Check350()
    {
        foreach (long v in Probe(s_set350, 0L, 4294967295L, false, 350))
        {
            Assert.True(uint_Subtractions_350((uint)v) == InSet(s_set350, v), $"uint_Subtractions_350({v}) set=[164..176] [158..158] [151..160] [188..188] [152..152]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set351 = [(20L, 20L), (7L, 7L), (12L, 12L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Tree_351(long x) => (x < 12L ? x == 7L : (x < 20L ? x == 12L : x == 20L));

    private static void Check351()
    {
        foreach (long v in Probe(s_set351, long.MinValue, 9223372036854775807L, false, 351))
        {
            Assert.True(long_Tree_351((long)v) == InSet(s_set351, v), $"long_Tree_351({v}) set=[20..20] [7..7] [12..12]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set352 = [(38L, 38L), (44L, 49L), (44L, 49L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_SwitchExpr_352(char x) => x switch
    {
        '\u0026' => true,
        >= '\u002C' and <= '\u0031' => true,
        _ => false,
    };

    private static void Check352()
    {
        foreach (long v in Probe(s_set352, 0L, 65535L, true, 352))
        {
            Assert.True(char_SwitchExpr_352((char)v) == InSet(s_set352, v), $"char_SwitchExpr_352({v}) set=[38..38] [44..49] [44..49]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set353 = [(200L, 200L), (226L, 226L), (230L, 238L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Flipped_353(byte x) => x == 200 || 226 == x || (x > 229 && x < 239) || 255 == x;

    private static void Check353()
    {
        foreach (long v in Probe(s_set353, 0L, 255L, true, 353))
        {
            Assert.True(byte_Flipped_353((byte)v) == InSet(s_set353, v), $"byte_Flipped_353({v}) set=[200..200] [226..226] [230..238] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set354 = [(37L, 37L), (41L, 41L), (45L, 47L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Ternary_354(sbyte x) => x == 37 ? true : x == 41 ? true : (x >= 45 && x <= 47) ? true : false;

    private static void Check354()
    {
        foreach (long v in Probe(s_set354, (-128L), 127L, true, 354))
        {
            Assert.True(sbyte_Ternary_354((sbyte)v) == InSet(s_set354, v), $"sbyte_Ternary_354({v}) set=[37..37] [41..41] [45..47]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set355 = [(78L, 84L), (68L, 75L), (87L, 90L), (73L, 78L), (102L, 104L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_OrChain_355(short x) => (x >= 78 && x <= 84) || (x >= 68 && x <= 75) || (x >= 87 && x <= 90) || (x >= 73 && x <= 78) || (x >= 102 && x <= 104);

    private static void Check355()
    {
        foreach (long v in Probe(s_set355, (-32768L), 32767L, true, 355))
        {
            Assert.True(short_OrChain_355((short)v) == InSet(s_set355, v), $"short_OrChain_355({v}) set=[78..84] [68..75] [87..90] [73..78] [102..104]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set356 = [(12404L, 12405L), (12367L, 12369L), (12416L, 12425L), (12428L, 12439L), (12352L, 12354L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Subtractions_356(ushort x) => (uint)(x - 12404) <= 1u || (uint)(x - 12367) <= 2u || (uint)(x - 12416) <= 9u || (uint)(x - 12428) <= 11u || (uint)(x - 12352) <= 2u;

    private static void Check356()
    {
        foreach (long v in Probe(s_set356, 0L, 65535L, true, 356))
        {
            Assert.True(ushort_Subtractions_356((ushort)v) == InSet(s_set356, v), $"ushort_Subtractions_356({v}) set=[12404..12405] [12367..12369] [12416..12425] [12428..12439] [12352..12354]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set357 = [(12512L, 12519L), (12459L, 12459L), (12492L, 12498L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_SwitchStmt_357(int x)
    {
        switch (x)
        {
            case 12459:
            case 12492:
            case 12493:
            case 12494:
            case 12495:
            case 12496:
            case 12497:
            case 12498:
            case 12512:
            case 12513:
            case 12514:
            case 12515:
            case 12516:
            case 12517:
            case 12518:
            case 12519:
                return true;
            default:
                return false;
        }
    }

    private static void Check357()
    {
        foreach (long v in Probe(s_set357, (-2147483648L), 2147483647L, false, 357))
        {
            Assert.True(int_SwitchStmt_357((int)v) == InSet(s_set357, v), $"int_SwitchStmt_357({v}) set=[12512..12519] [12459..12459] [12492..12498]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set358 = [(31L, 42L), (140L, 140L), (43L, 51L), (102L, 113L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_358(uint x) => x switch
    {
        >= 31u and <= 42u => true,
        >= 43u and <= 51u => true,
        >= 102u and <= 113u => true,
        140u => true,
        _ => false,
    };

    private static void Check358()
    {
        foreach (long v in Probe(s_set358, 0L, 4294967295L, false, 358))
        {
            Assert.True(uint_SwitchExpr_358((uint)v) == InSet(s_set358, v), $"uint_SwitchExpr_358({v}) set=[31..42] [140..140] [43..51] [102..113]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set359 = [(71L, 73L), (68L, 70L), (80L, 85L), (59L, 68L), (9223372036854775807L, 9223372036854775807L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Subtractions_359(long x) => (ulong)(x - 71L) <= 2UL || (ulong)(x - 68L) <= 2UL || (ulong)(x - 80L) <= 5UL || (ulong)(x - 59L) <= 9UL || x == 9223372036854775807L;

    private static void Check359()
    {
        foreach (long v in Probe(s_set359, long.MinValue, 9223372036854775807L, false, 359))
        {
            Assert.True(long_Subtractions_359((long)v) == InSet(s_set359, v), $"long_Subtractions_359({v}) set=[71..73] [68..70] [80..85] [59..68] [9223372036854775807..9223372036854775807]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set360 = [(12477L, 12477L), (12464L, 12464L), (12354L, 12362L), (12468L, 12468L), (12379L, 12391L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Subtractions_360(char x) => x == '\u30BD' || x == '\u30B0' || (uint)(x - '\u3042') <= 8u || x == '\u30B4' || (uint)(x - '\u305B') <= 12u;

    private static void Check360()
    {
        foreach (long v in Probe(s_set360, 0L, 65535L, true, 360))
        {
            Assert.True(char_Subtractions_360((char)v) == InSet(s_set360, v), $"char_Subtractions_360({v}) set=[12477..12477] [12464..12464] [12354..12362] [12468..12468] [12379..12391]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set361 = [(211L, 211L), (123L, 123L), (179L, 179L), (112L, 151L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Negated_361(byte x) => !(x is 211 or 123 or 179 or >= 112 and <= 151);

    private static void Check361()
    {
        foreach (long v in Probe(s_set361, 0L, 255L, true, 361))
        {
            Assert.True(byte_Negated_361((byte)v) == !InSet(s_set361, v), $"byte_Negated_361({v}) set=[211..211] [123..123] [179..179] [112..151]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set362 = [(-111L, -111L), (-105L, -100L), (-94L, -94L), (-124L, -114L), (-124L, -112L), (-128L, -116L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Flipped_362(sbyte x) => (-111) == x || (x > (-106) && x < (-99)) || x == (-94) || (x > (-125) && x < (-113)) || (x > (-125) && x < (-111)) || (x >= (-128) && x < (-115));

    private static void Check362()
    {
        foreach (long v in Probe(s_set362, (-128L), 127L, true, 362))
        {
            Assert.True(sbyte_Flipped_362((sbyte)v) == InSet(s_set362, v), $"sbyte_Flipped_362({v}) set=[-111..-111] [-105..-100] [-94..-94] [-124..-114] [-124..-112] [-128..-116]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set363 = [(157L, 158L), (101L, 101L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Ternary_363(short x) => (x >= 157 && x <= 158) ? true : x == 101 ? true : false;

    private static void Check363()
    {
        foreach (long v in Probe(s_set363, (-32768L), 32767L, true, 363))
        {
            Assert.True(short_Ternary_363((short)v) == InSet(s_set363, v), $"short_Ternary_363({v}) set=[157..158] [101..101]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set364 = [(384L, 392L), (385L, 387L), (262L, 262L), (301L, 311L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_364(ushort x)
    {
        if ((x >= 384 && x <= 392))
        {
            return true;
        }

        if ((x >= 385 && x <= 387))
        {
            return true;
        }

        if (x == 262)
        {
            return true;
        }

        if ((x >= 301 && x <= 311))
        {
            return true;
        }

        return false;
    }

    private static void Check364()
    {
        foreach (long v in Probe(s_set364, 0L, 65535L, true, 364))
        {
            Assert.True(ushort_Branches_364((ushort)v) == InSet(s_set364, v), $"ushort_Branches_364({v}) set=[384..392] [385..387] [262..262] [301..311]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set365 = [(1048712L, 1048723L), (1048688L, 1048688L), (1048771L, 1048776L), (1048732L, 1048739L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_OrChain_365(int x) => (x >= 1048712 && x <= 1048723) || x == 1048688 || (x >= 1048771 && x <= 1048776) || (x >= 1048732 && x <= 1048739);

    private static void Check365()
    {
        foreach (long v in Probe(s_set365, (-2147483648L), 2147483647L, false, 365))
        {
            Assert.True(int_OrChain_365((int)v) == InSet(s_set365, v), $"int_OrChain_365({v}) set=[1048712..1048723] [1048688..1048688] [1048771..1048776] [1048732..1048739]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set366 = [(202L, 202L), (214L, 214L), (204L, 211L), (250L, 257L), (258L, 258L), (251L, 251L), (4294967295L, 4294967295L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Tree_366(uint x) => (x < 250u ? (x < 204u ? x == 202u : (x < 214u ? (x >= 204u && x <= 211u) : x == 214u)) : (x < 258u ? ((x >= 250u && x <= 257u) || x == 251u) : (x < 4294967295u ? x == 258u : x == 4294967295u)));

    private static void Check366()
    {
        foreach (long v in Probe(s_set366, 0L, 4294967295L, false, 366))
        {
            Assert.True(uint_Tree_366((uint)v) == InSet(s_set366, v), $"uint_Tree_366({v}) set=[202..202] [214..214] [204..211] [250..257] [258..258] [251..251] [4294967295..4294967295]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set367 = [(-9223372036854775808L, -9223372036854775806L), (134L, 145L), (-9223372036854775808L, -9223372036854775808L), (115L, 115L), (134L, 134L), (121L, 121L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Branches_367(long x)
    {
        if ((x >= long.MinValue && x <= (-9223372036854775806L)))
        {
            return true;
        }

        if ((x >= 134L && x <= 145L))
        {
            return true;
        }

        if (x == long.MinValue)
        {
            return true;
        }

        if (x == 115L)
        {
            return true;
        }

        if (x == 134L)
        {
            return true;
        }

        if (x == 121L)
        {
            return true;
        }

        return false;
    }

    private static void Check367()
    {
        foreach (long v in Probe(s_set367, long.MinValue, 9223372036854775807L, false, 367))
        {
            Assert.True(long_Branches_367((long)v) == InSet(s_set367, v), $"long_Branches_367({v}) set=[-9223372036854775808..-9223372036854775806] [134..145] [-9223372036854775808..-9223372036854775808] [115..115] [134..134] [121..121]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set368 = [(65440L, 65450L), (65535L, 65535L), (65444L, 65456L), (65535L, 65535L), (65535L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Flipped_368(char x) => (x > '\uFF9F' && x <= '\uFFAA') || '\uFFFF' == x || (x > '\uFFA3' && x < '\uFFB1') || '\uFFFF' == x || x == '\uFFFF';

    private static void Check368()
    {
        foreach (long v in Probe(s_set368, 0L, 65535L, true, 368))
        {
            Assert.True(char_Flipped_368((char)v) == InSet(s_set368, v), $"char_Flipped_368({v}) set=[65440..65450] [65535..65535] [65444..65456] [65535..65535] [65535..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set369 = [(112L, 113L), (152L, 162L), (80L, 80L), (136L, 140L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_IsPattern_369(byte x) => x is >= 112 and <= 113 or >= 152 and <= 162 or 80 or >= 136 and <= 140;

    private static void Check369()
    {
        foreach (long v in Probe(s_set369, 0L, 255L, true, 369))
        {
            Assert.True(byte_IsPattern_369((byte)v) == InSet(s_set369, v), $"byte_IsPattern_369({v}) set=[112..113] [152..162] [80..80] [136..140]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set370 = [(47L, 56L), (43L, 43L), (53L, 53L), (54L, 54L), (46L, 46L), (44L, 56L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_OrChain_370(sbyte x) => (x >= 47 && x <= 56) || x == 43 || x == 53 || x == 54 || x == 46 || (x >= 44 && x <= 56);

    private static void Check370()
    {
        foreach (long v in Probe(s_set370, (-128L), 127L, true, 370))
        {
            Assert.True(sbyte_OrChain_370((sbyte)v) == InSet(s_set370, v), $"sbyte_OrChain_370({v}) set=[47..56] [43..43] [53..53] [54..54] [46..46] [44..56]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set371 = [(1081L, 1081L), (1066L, 1067L), (1049L, 1049L), (1042L, 1051L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_IsPattern_371(short x) => x is 1081 or >= 1066 and <= 1067 or 1049 or >= 1042 and <= 1051;

    private static void Check371()
    {
        foreach (long v in Probe(s_set371, (-32768L), 32767L, true, 371))
        {
            Assert.True(short_IsPattern_371((short)v) == InSet(s_set371, v), $"short_IsPattern_371({v}) set=[1081..1081] [1066..1067] [1049..1049] [1042..1051]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set372 = [(68L, 80L), (33L, 40L), (84L, 90L), (102L, 111L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Subtractions_372(ushort x) => (uint)(x - 68) <= 12u || (uint)(x - 33) <= 7u || (uint)(x - 84) <= 6u || (uint)(x - 102) <= 9u;

    private static void Check372()
    {
        foreach (long v in Probe(s_set372, 0L, 65535L, true, 372))
        {
            Assert.True(ushort_Subtractions_372((ushort)v) == InSet(s_set372, v), $"ushort_Subtractions_372({v}) set=[68..80] [33..40] [84..90] [102..111]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set373 = [(2147483642L, 2147483647L), (110L, 110L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_IsPattern_373(int x) => x is >= 2147483642 and <= 2147483647 or 110;

    private static void Check373()
    {
        foreach (long v in Probe(s_set373, (-2147483648L), 2147483647L, false, 373))
        {
            Assert.True(int_IsPattern_373((int)v) == InSet(s_set373, v), $"int_IsPattern_373({v}) set=[2147483642..2147483647] [110..110]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set374 = [(84L, 84L), (96L, 107L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_SwitchExpr_374(uint x) => x switch
    {
        84u => true,
        >= 96u and <= 107u => true,
        _ => false,
    };

    private static void Check374()
    {
        foreach (long v in Probe(s_set374, 0L, 4294967295L, false, 374))
        {
            Assert.True(uint_SwitchExpr_374((uint)v) == InSet(s_set374, v), $"uint_SwitchExpr_374({v}) set=[84..84] [96..107]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set375 = [(24L, 24L), (27L, 28L), (-14L, -7L), (-10L, -9L), (38L, 48L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Flipped_375(long x) => 24L == x || (x > 26L && x <= 28L) || ((-14L) <= x && (-7L) >= x) || (x >= (-10L) && x < (-8L)) || (x >= 38L && x < 49L);

    private static void Check375()
    {
        foreach (long v in Probe(s_set375, long.MinValue, 9223372036854775807L, false, 375))
        {
            Assert.True(long_Flipped_375((long)v) == InSet(s_set375, v), $"long_Flipped_375({v}) set=[24..24] [27..28] [-14..-7] [-10..-9] [38..48]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set376 = [(65535L, 65535L), (65535L, 65535L), (65535L, 65535L), (65535L, 65535L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Subtractions_376(char x) => x == '\uFFFF' || x == '\uFFFF' || x == '\uFFFF' || x == '\uFFFF';

    private static void Check376()
    {
        foreach (long v in Probe(s_set376, 0L, 65535L, true, 376))
        {
            Assert.True(char_Subtractions_376((char)v) == InSet(s_set376, v), $"char_Subtractions_376({v}) set=[65535..65535] [65535..65535] [65535..65535] [65535..65535]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set377 = [(59L, 59L), (76L, 76L), (76L, 79L), (96L, 96L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Subtractions_377(byte x) => x == 59 || x == 76 || (uint)(x - 76) <= 3u || x == 96;

    private static void Check377()
    {
        foreach (long v in Probe(s_set377, 0L, 255L, true, 377))
        {
            Assert.True(byte_Subtractions_377((byte)v) == InSet(s_set377, v), $"byte_Subtractions_377({v}) set=[59..59] [76..76] [76..79] [96..96]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set378 = [(59L, 62L), (46L, 46L), (46L, 47L), (90L, 124L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_Tree_378(sbyte x) => (x < 59 ? (x == 46 || (x >= 46 && x <= 47)) : (x < 90 ? (x >= 59 && x <= 62) : (x >= 90 && x <= 124)));

    private static void Check378()
    {
        foreach (long v in Probe(s_set378, (-128L), 127L, true, 378))
        {
            Assert.True(sbyte_Tree_378((sbyte)v) == InSet(s_set378, v), $"sbyte_Tree_378({v}) set=[59..62] [46..46] [46..47] [90..124]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set379 = [(33L, 34L), (92L, 92L), (85L, 89L), (111L, 118L), (32760L, 32767L), (96L, 104L), (98L, 119L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Ternary_379(short x) => (x >= 33 && x <= 34) ? true : x == 92 ? true : (x >= 85 && x <= 89) ? true : (x >= 111 && x <= 118) ? true : (x >= 32760 && x <= 32767) ? true : (x >= 96 && x <= 104) ? true : (x >= 98 && x <= 119) ? true : false;

    private static void Check379()
    {
        foreach (long v in Probe(s_set379, (-32768L), 32767L, true, 379))
        {
            Assert.True(short_Ternary_379((short)v) == InSet(s_set379, v), $"short_Ternary_379({v}) set=[33..34] [92..92] [85..89] [111..118] [32760..32767] [96..104] [98..119]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set380 = [(62L, 62L), (86L, 97L), (85L, 85L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_IsPattern_380(ushort x) => x is 62 or >= 86 and <= 97 or 85;

    private static void Check380()
    {
        foreach (long v in Probe(s_set380, 0L, 65535L, true, 380))
        {
            Assert.True(ushort_IsPattern_380((ushort)v) == InSet(s_set380, v), $"ushort_IsPattern_380({v}) set=[62..62] [86..97] [85..85]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set381 = [(43L, 53L), (-2147483648L, -2147483639L), (43L, 43L), (49L, 49L), (48L, 48L), (96L, 125L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Negated_381(int x) => !(x is >= 43 and <= 53 or >= int.MinValue and <= (-2147483639) or 43 or 49 or 48 or >= 96 and <= 125);

    private static void Check381()
    {
        foreach (long v in Probe(s_set381, (-2147483648L), 2147483647L, false, 381))
        {
            Assert.True(int_Negated_381((int)v) == !InSet(s_set381, v), $"int_Negated_381({v}) set=[43..53] [-2147483648..-2147483639] [43..43] [49..49] [48..48] [96..125]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set382 = [(115L, 115L), (137L, 138L), (238L, 238L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_IsPattern_382(uint x) => x is 115u or >= 137u and <= 138u or 238u;

    private static void Check382()
    {
        foreach (long v in Probe(s_set382, 0L, 4294967295L, false, 382))
        {
            Assert.True(uint_IsPattern_382((uint)v) == InSet(s_set382, v), $"uint_IsPattern_382({v}) set=[115..115] [137..138] [238..238]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set383 = [(12425L, 12425L), (12484L, 12484L), (12490L, 12490L), (12491L, 12500L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Subtractions_383(long x) => x == 12425L || x == 12484L || x == 12490L || (ulong)(x - 12491L) <= 9UL;

    private static void Check383()
    {
        foreach (long v in Probe(s_set383, long.MinValue, 9223372036854775807L, false, 383))
        {
            Assert.True(long_Subtractions_383((long)v) == InSet(s_set383, v), $"long_Subtractions_383({v}) set=[12425..12425] [12484..12484] [12490..12490] [12491..12500]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set384 = [(12361L, 12363L), (12399L, 12406L), (12385L, 12390L), (12381L, 12391L), (12401L, 12402L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_384(char x)
    {
        if ((x >= '\u3049' && x <= '\u304B'))
        {
            return true;
        }

        if ((x >= '\u306F' && x <= '\u3076'))
        {
            return true;
        }

        if ((x >= '\u3061' && x <= '\u3066'))
        {
            return true;
        }

        if ((x >= '\u305D' && x <= '\u3067'))
        {
            return true;
        }

        if ((x >= '\u3071' && x <= '\u3072'))
        {
            return true;
        }

        return false;
    }

    private static void Check384()
    {
        foreach (long v in Probe(s_set384, 0L, 65535L, true, 384))
        {
            Assert.True(char_Branches_384((char)v) == InSet(s_set384, v), $"char_Branches_384({v}) set=[12361..12363] [12399..12406] [12385..12390] [12381..12391] [12401..12402]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set385 = [(57L, 57L), (62L, 71L), (55L, 57L), (59L, 63L), (117L, 156L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_IsPattern_385(byte x) => x is 57 or >= 62 and <= 71 or >= 55 and <= 57 or >= 59 and <= 63 or >= 117 and <= 156;

    private static void Check385()
    {
        foreach (long v in Probe(s_set385, 0L, 255L, true, 385))
        {
            Assert.True(byte_IsPattern_385((byte)v) == InSet(s_set385, v), $"byte_IsPattern_385({v}) set=[57..57] [62..71] [55..57] [59..63] [117..156]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set386 = [(127L, 127L), (127L, 127L), (95L, 107L), (127L, 127L), (127L, 127L), (71L, 81L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_IsPattern_386(sbyte x) => x is 127 or 127 or >= 95 and <= 107 or 127 or 127 or >= 71 and <= 81;

    private static void Check386()
    {
        foreach (long v in Probe(s_set386, (-128L), 127L, true, 386))
        {
            Assert.True(sbyte_IsPattern_386((sbyte)v) == InSet(s_set386, v), $"sbyte_IsPattern_386({v}) set=[127..127] [127..127] [95..107] [127..127] [127..127] [71..81]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set387 = [(37L, 37L), (48L, 48L), (51L, 52L), (65L, 73L), (44L, 54L), (51L, 51L), (38L, 38L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Subtractions_387(short x) => x == 37 || x == 48 || (uint)(x - 51) <= 1u || (uint)(x - 65) <= 8u || (uint)(x - 44) <= 10u || x == 51 || x == 38;

    private static void Check387()
    {
        foreach (long v in Probe(s_set387, (-32768L), 32767L, true, 387))
        {
            Assert.True(short_Subtractions_387((short)v) == InSet(s_set387, v), $"short_Subtractions_387({v}) set=[37..37] [48..48] [51..52] [65..73] [44..54] [51..51] [38..38]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set388 = [(10L, 18L), (14L, 14L), (9L, 9L), (6L, 15L), (4L, 4L), (10L, 17L), (12L, 12L), (70L, 114L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_IsPattern_388(ushort x) => x is >= 10 and <= 18 or 14 or 9 or >= 6 and <= 15 or 4 or >= 10 and <= 17 or 12 or >= 70 and <= 114;

    private static void Check388()
    {
        foreach (long v in Probe(s_set388, 0L, 65535L, true, 388))
        {
            Assert.True(ushort_IsPattern_388((ushort)v) == InSet(s_set388, v), $"ushort_IsPattern_388({v}) set=[10..18] [14..14] [9..9] [6..15] [4..4] [10..17] [12..12] [70..114]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set389 = [(76L, 86L), (65L, 65L), (82L, 94L), (74L, 77L), (75L, 85L), (73L, 85L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Subtractions_389(int x) => (uint)(x - 76) <= 10u || x == 65 || (uint)(x - 82) <= 12u || (uint)(x - 74) <= 3u || (uint)(x - 75) <= 10u || (uint)(x - 73) <= 12u;

    private static void Check389()
    {
        foreach (long v in Probe(s_set389, (-2147483648L), 2147483647L, false, 389))
        {
            Assert.True(int_Subtractions_389((int)v) == InSet(s_set389, v), $"int_Subtractions_389({v}) set=[76..86] [65..65] [82..94] [74..77] [75..85] [73..85]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set390 = [(65L, 65L), (58L, 58L), (62L, 62L), (4294967295L, 4294967295L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Flipped_390(uint x) => 65u == x || x == 58u || 62u == x || 4294967295u == x;

    private static void Check390()
    {
        foreach (long v in Probe(s_set390, 0L, 4294967295L, false, 390))
        {
            Assert.True(uint_Flipped_390((uint)v) == InSet(s_set390, v), $"uint_Flipped_390({v}) set=[65..65] [58..58] [62..62] [4294967295..4294967295]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set391 = [(202L, 207L), (245L, 256L), (222L, 233L), (165L, 206L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_Tree_391(long x) => (x < 222L ? ((x >= 165L && x <= 206L) || (x >= 202L && x <= 207L)) : (x < 245L ? (x >= 222L && x <= 233L) : (x >= 245L && x <= 256L)));

    private static void Check391()
    {
        foreach (long v in Probe(s_set391, long.MinValue, 9223372036854775807L, false, 391))
        {
            Assert.True(long_Tree_391((long)v) == InSet(s_set391, v), $"long_Tree_391({v}) set=[202..207] [245..256] [222..233] [165..206]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set392 = [(122L, 126L), (185L, 195L), (100L, 109L), (161L, 184L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool char_Branches_392(char x)
    {
        if ((x >= '\u007A' && x <= '\u007E'))
        {
            return true;
        }

        if ((x >= '\u00B9' && x <= '\u00C3'))
        {
            return true;
        }

        if ((x >= '\u0064' && x <= '\u006D'))
        {
            return true;
        }

        if ((x >= '\u00A1' && x <= '\u00B8'))
        {
            return true;
        }

        return false;
    }

    private static void Check392()
    {
        foreach (long v in Probe(s_set392, 0L, 65535L, true, 392))
        {
            Assert.True(char_Branches_392((char)v) == InSet(s_set392, v), $"char_Branches_392({v}) set=[122..126] [185..195] [100..109] [161..184]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set393 = [(255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L), (255L, 255L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool byte_Tree_393(byte x) => (x == 255 || x == 255 || x == 255 || x == 255 || x == 255);

    private static void Check393()
    {
        foreach (long v in Probe(s_set393, 0L, 255L, true, 393))
        {
            Assert.True(byte_Tree_393((byte)v) == InSet(s_set393, v), $"byte_Tree_393({v}) set=[255..255] [255..255] [255..255] [255..255] [255..255]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set394 = [(-30L, -19L), (-15L, -13L), (-101L, -91L), (17L, 26L), (-60L, -60L), (-55L, -44L), (-107L, -107L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool sbyte_OrChain_394(sbyte x) => (x >= (-30) && x <= (-19)) || (x >= (-15) && x <= (-13)) || (x >= (-101) && x <= (-91)) || (x >= 17 && x <= 26) || x == (-60) || (x >= (-55) && x <= (-44)) || x == (-107);

    private static void Check394()
    {
        foreach (long v in Probe(s_set394, (-128L), 127L, true, 394))
        {
            Assert.True(sbyte_OrChain_394((sbyte)v) == InSet(s_set394, v), $"sbyte_OrChain_394({v}) set=[-30..-19] [-15..-13] [-101..-91] [17..26] [-60..-60] [-55..-44] [-107..-107]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set395 = [(-8L, -8L), (1L, 9L), (17L, 17L), (0L, 5L), (7L, 7L), (16L, 23L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool short_Negated_395(short x) => !(x is (-8) or >= 1 and <= 9 or 17 or >= 0 and <= 5 or 7 or >= 16 and <= 23);

    private static void Check395()
    {
        foreach (long v in Probe(s_set395, (-32768L), 32767L, true, 395))
        {
            Assert.True(short_Negated_395((short)v) == !InSet(s_set395, v), $"short_Negated_395({v}) set=[-8..-8] [1..9] [17..17] [0..5] [7..7] [16..23]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set396 = [(50L, 52L), (65526L, 65535L), (1L, 1L), (1L, 2L), (62L, 68L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ushort_Branches_396(ushort x)
    {
        if ((x >= 50 && x <= 52))
        {
            return true;
        }

        if ((x >= 65526 && x <= 65535))
        {
            return true;
        }

        if (x == 1)
        {
            return true;
        }

        if ((x >= 1 && x <= 2))
        {
            return true;
        }

        if ((x >= 62 && x <= 68))
        {
            return true;
        }

        return false;
    }

    private static void Check396()
    {
        foreach (long v in Probe(s_set396, 0L, 65535L, true, 396))
        {
            Assert.True(ushort_Branches_396((ushort)v) == InSet(s_set396, v), $"ushort_Branches_396({v}) set=[50..52] [65526..65535] [1..1] [1..2] [62..68]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set397 = [(1048589L, 1048599L), (1048610L, 1048616L), (1048581L, 1048589L), (1048609L, 1048614L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool int_Negated_397(int x) => !(x is >= 1048589 and <= 1048599 or >= 1048610 and <= 1048616 or >= 1048581 and <= 1048589 or >= 1048609 and <= 1048614);

    private static void Check397()
    {
        foreach (long v in Probe(s_set397, (-2147483648L), 2147483647L, false, 397))
        {
            Assert.True(int_Negated_397((int)v) == !InSet(s_set397, v), $"int_Negated_397({v}) set=[1048589..1048599] [1048610..1048616] [1048581..1048589] [1048609..1048614]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set398 = [(134L, 141L), (117L, 117L), (113L, 113L), (121L, 128L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool uint_Flipped_398(uint x) => (x > 133u && x < 142u) || x == 117u || 113u == x || (x > 120u && x < 129u);

    private static void Check398()
    {
        foreach (long v in Probe(s_set398, 0L, 4294967295L, false, 398))
        {
            Assert.True(uint_Flipped_398((uint)v) == InSet(s_set398, v), $"uint_Flipped_398({v}) set=[134..141] [117..117] [113..113] [121..128]");
        }
    }

    private static readonly (long Lo, long Hi)[] s_set399 = [(-12L, -12L), (1L, 1L), (-16L, -16L), (-5L, -5L), (13L, 13L), (8L, 18L), (45L, 61L)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool long_IsPattern_399(long x) => x is (-12L) or 1L or (-16L) or (-5L) or 13L or >= 8L and <= 18L or >= 45L and <= 61L;

    private static void Check399()
    {
        foreach (long v in Probe(s_set399, long.MinValue, 9223372036854775807L, false, 399))
        {
            Assert.True(long_IsPattern_399((long)v) == InSet(s_set399, v), $"long_IsPattern_399({v}) set=[-12..-12] [1..1] [-16..-16] [-5..-5] [13..13] [8..18] [45..61]");
        }
    }

    [Fact]
    public static void Group0()
    {
        Check0();
        Check1();
        Check2();
        Check3();
        Check4();
        Check5();
        Check6();
        Check7();
        Check8();
        Check9();
        Check10();
        Check11();
        Check12();
        Check13();
        Check14();
        Check15();
        Check16();
        Check17();
        Check18();
        Check19();
    }

    [Fact]
    public static void Group1()
    {
        Check20();
        Check21();
        Check22();
        Check23();
        Check24();
        Check25();
        Check26();
        Check27();
        Check28();
        Check29();
        Check30();
        Check31();
        Check32();
        Check33();
        Check34();
        Check35();
        Check36();
        Check37();
        Check38();
        Check39();
    }

    [Fact]
    public static void Group2()
    {
        Check40();
        Check41();
        Check42();
        Check43();
        Check44();
        Check45();
        Check46();
        Check47();
        Check48();
        Check49();
        Check50();
        Check51();
        Check52();
        Check53();
        Check54();
        Check55();
        Check56();
        Check57();
        Check58();
        Check59();
    }

    [Fact]
    public static void Group3()
    {
        Check60();
        Check61();
        Check62();
        Check63();
        Check64();
        Check65();
        Check66();
        Check67();
        Check68();
        Check69();
        Check70();
        Check71();
        Check72();
        Check73();
        Check74();
        Check75();
        Check76();
        Check77();
        Check78();
        Check79();
    }

    [Fact]
    public static void Group4()
    {
        Check80();
        Check81();
        Check82();
        Check83();
        Check84();
        Check85();
        Check86();
        Check87();
        Check88();
        Check89();
        Check90();
        Check91();
        Check92();
        Check93();
        Check94();
        Check95();
        Check96();
        Check97();
        Check98();
        Check99();
    }

    [Fact]
    public static void Group5()
    {
        Check100();
        Check101();
        Check102();
        Check103();
        Check104();
        Check105();
        Check106();
        Check107();
        Check108();
        Check109();
        Check110();
        Check111();
        Check112();
        Check113();
        Check114();
        Check115();
        Check116();
        Check117();
        Check118();
        Check119();
    }

    [Fact]
    public static void Group6()
    {
        Check120();
        Check121();
        Check122();
        Check123();
        Check124();
        Check125();
        Check126();
        Check127();
        Check128();
        Check129();
        Check130();
        Check131();
        Check132();
        Check133();
        Check134();
        Check135();
        Check136();
        Check137();
        Check138();
        Check139();
    }

    [Fact]
    public static void Group7()
    {
        Check140();
        Check141();
        Check142();
        Check143();
        Check144();
        Check145();
        Check146();
        Check147();
        Check148();
        Check149();
        Check150();
        Check151();
        Check152();
        Check153();
        Check154();
        Check155();
        Check156();
        Check157();
        Check158();
        Check159();
    }

    [Fact]
    public static void Group8()
    {
        Check160();
        Check161();
        Check162();
        Check163();
        Check164();
        Check165();
        Check166();
        Check167();
        Check168();
        Check169();
        Check170();
        Check171();
        Check172();
        Check173();
        Check174();
        Check175();
        Check176();
        Check177();
        Check178();
        Check179();
    }

    [Fact]
    public static void Group9()
    {
        Check180();
        Check181();
        Check182();
        Check183();
        Check184();
        Check185();
        Check186();
        Check187();
        Check188();
        Check189();
        Check190();
        Check191();
        Check192();
        Check193();
        Check194();
        Check195();
        Check196();
        Check197();
        Check198();
        Check199();
    }

    [Fact]
    public static void Group10()
    {
        Check200();
        Check201();
        Check202();
        Check203();
        Check204();
        Check205();
        Check206();
        Check207();
        Check208();
        Check209();
        Check210();
        Check211();
        Check212();
        Check213();
        Check214();
        Check215();
        Check216();
        Check217();
        Check218();
        Check219();
    }

    [Fact]
    public static void Group11()
    {
        Check220();
        Check221();
        Check222();
        Check223();
        Check224();
        Check225();
        Check226();
        Check227();
        Check228();
        Check229();
        Check230();
        Check231();
        Check232();
        Check233();
        Check234();
        Check235();
        Check236();
        Check237();
        Check238();
        Check239();
    }

    [Fact]
    public static void Group12()
    {
        Check240();
        Check241();
        Check242();
        Check243();
        Check244();
        Check245();
        Check246();
        Check247();
        Check248();
        Check249();
        Check250();
        Check251();
        Check252();
        Check253();
        Check254();
        Check255();
        Check256();
        Check257();
        Check258();
        Check259();
    }

    [Fact]
    public static void Group13()
    {
        Check260();
        Check261();
        Check262();
        Check263();
        Check264();
        Check265();
        Check266();
        Check267();
        Check268();
        Check269();
        Check270();
        Check271();
        Check272();
        Check273();
        Check274();
        Check275();
        Check276();
        Check277();
        Check278();
        Check279();
    }

    [Fact]
    public static void Group14()
    {
        Check280();
        Check281();
        Check282();
        Check283();
        Check284();
        Check285();
        Check286();
        Check287();
        Check288();
        Check289();
        Check290();
        Check291();
        Check292();
        Check293();
        Check294();
        Check295();
        Check296();
        Check297();
        Check298();
        Check299();
    }

    [Fact]
    public static void Group15()
    {
        Check300();
        Check301();
        Check302();
        Check303();
        Check304();
        Check305();
        Check306();
        Check307();
        Check308();
        Check309();
        Check310();
        Check311();
        Check312();
        Check313();
        Check314();
        Check315();
        Check316();
        Check317();
        Check318();
        Check319();
    }

    [Fact]
    public static void Group16()
    {
        Check320();
        Check321();
        Check322();
        Check323();
        Check324();
        Check325();
        Check326();
        Check327();
        Check328();
        Check329();
        Check330();
        Check331();
        Check332();
        Check333();
        Check334();
        Check335();
        Check336();
        Check337();
        Check338();
        Check339();
    }

    [Fact]
    public static void Group17()
    {
        Check340();
        Check341();
        Check342();
        Check343();
        Check344();
        Check345();
        Check346();
        Check347();
        Check348();
        Check349();
        Check350();
        Check351();
        Check352();
        Check353();
        Check354();
        Check355();
        Check356();
        Check357();
        Check358();
        Check359();
    }

    [Fact]
    public static void Group18()
    {
        Check360();
        Check361();
        Check362();
        Check363();
        Check364();
        Check365();
        Check366();
        Check367();
        Check368();
        Check369();
        Check370();
        Check371();
        Check372();
        Check373();
        Check374();
        Check375();
        Check376();
        Check377();
        Check378();
        Check379();
    }

    [Fact]
    public static void Group19()
    {
        Check380();
        Check381();
        Check382();
        Check383();
        Check384();
        Check385();
        Check386();
        Check387();
        Check388();
        Check389();
        Check390();
        Check391();
        Check392();
        Check393();
        Check394();
        Check395();
        Check396();
        Check397();
        Check398();
        Check399();
    }
}
