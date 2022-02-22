// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Common
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Const<T>(T t) => t;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Var<T>(T t) => t;

    public const MethodImplOptions Opt = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;
    public const MethodImplOptions NoOpt = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;
}

public static class Tests_len0_0
{
    const string cns = "";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len1_1
{
    const string cns = "a";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len1_2
{
    const string cns = "A";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len1_3
{
    const string cns = " ";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len2_4
{
    const string cns = "a-";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len2_5
{
    const string cns = "aa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len3_6
{
    const string cns = "aAa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len3_7
{
    const string cns = "aaA";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len4_8
{
    const string cns = "a-aa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len4_9
{
    const string cns = "aaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len5_10
{
    const string cns = "aaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len5_11
{
    const string cns = "aaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len6_12
{
    const string cns = "aaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len6_13
{
    const string cns = "aaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len7_14
{
    const string cns = "aaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len7_15
{
    const string cns = "aaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len8_16
{
    const string cns = "aaAAaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len8_17
{
    const string cns = "aaaaa-aa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len9_18
{
    const string cns = "aaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len9_19
{
    const string cns = "aaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len10_20
{
    const string cns = "aaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len10_21
{
    const string cns = "aaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len11_22
{
    const string cns = "aaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len11_23
{
    const string cns = "aaaAAaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len12_24
{
    const string cns = "aaaaaa-aaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len12_25
{
    const string cns = "aaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len13_26
{
    const string cns = "aaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len14_27
{
    const string cns = "aaaaaaaaaжжaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len15_28
{
    const string cns = "aaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len15_29
{
    const string cns = "aaaAAAaaaaaazzz";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len16_30
{
    const string cns = "aaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len17_31
{
    const string cns = "aaaaaaaaaaaaжaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len17_32
{
    const string cns = "aaaaaaAAAAaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len18_33
{
    const string cns = "aaaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len19_34
{
    const string cns = "aaaaaaaggggggggggaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len20_35
{
    const string cns = "aaaaaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len21_36
{
    const string cns = "aaaaaaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len22_37
{
    const string cns = "aaaaaaAAAAaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len23_38
{
    const string cns = "aaaaaaaaaaaaaaaaaaaaaa ";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len24_39
{
    const string cns = "aaaччччччччччaaaaжжaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len25_40
{
    const string cns = "aaaaaaaaaaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len26_41
{
    const string cns = "gggggggggggaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len27_42
{
    const string cns = "aaaaaaaaaaaaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len29_43
{
    const string cns = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len29_44
{
    const string cns = "aaaaa\aaaaaaaaaaaNNNNNNaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len31_45
{
    const string cns = "aaaaaaaaaaaaaaaaaaaaaaaaaa-aaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len32_46
{
    const string cns = "aaaaaaaaaaaaaa aaaaaaaaaaaaaaaaa";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

public static class Tests_len34_47
{
    const string cns = "aaaaaZZZZZZZaaaaaaaaaaaaaaaaaaaaa ";
    [MethodImpl(Common.NoOpt)]static bool Test_ref_0(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_0(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_1(string s) => Common.Var(cns) == s;
    [MethodImpl(Common.Opt)]static bool Test_tst_1(string s) => (cns) == s;

    [MethodImpl(Common.NoOpt)]static bool Test_ref_2(string s) => string.Equals(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_2(string s) => string.Equals(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_3(string s) => string.Equals(null, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_3(string s) => string.Equals(null, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_4(string s) => string.Equals((object)s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_4(string s) => string.Equals((object)s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_5(string s) => string.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_5(string s) => string.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_6(string s) => string.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_6(string s) => string.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_7(string s) => string.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_7(string s) => string.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_8(string s) => (s as object).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_8(string s) => (s as object).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_9(string s) => s.Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_9(string s) => s.Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_10(string s) => ((string)null).Equals(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_10(string s) => ((string)null).Equals((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_11(string s) => s.Equals(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_11(string s) => s.Equals((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_12(string s) => s.Equals(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_12(string s) => s.Equals((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_13(string s) => s.Equals(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_13(string s) => s.Equals((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_14(string s) => s == Common.Var(cns);
    [MethodImpl(Common.Opt)]static bool Test_tst_14(string s) => s == (cns);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_15(string s) => s.StartsWith(Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_15(string s) => s.StartsWith((cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_16(string s) => s.StartsWith(Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_16(string s) => s.StartsWith((cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_17(string s) => s.StartsWith(Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_17(string s) => s.StartsWith((cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_18(string s) => s.StartsWith(Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_18(string s) => s.StartsWith((cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_19(string s) => s.StartsWith(Common.Var(cns), true, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_19(string s) => s.StartsWith((cns), true, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_20(string s) => s.StartsWith(Common.Var(cns), false, null);
    [MethodImpl(Common.Opt)]static bool Test_tst_20(string s) => s.StartsWith((cns), false, null);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_21(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_21(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_22(string s) => MemoryExtensions.SequenceEqual<char>(Common.Var(cns), s);
    [MethodImpl(Common.Opt)]static bool Test_tst_22(string s) => MemoryExtensions.SequenceEqual<char>((cns), s);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_23(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_23(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_24(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_24(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_25(string s) => MemoryExtensions.Equals(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_25(string s) => MemoryExtensions.Equals(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_26(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.InvariantCulture);
    [MethodImpl(Common.Opt)]static bool Test_tst_26(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.InvariantCulture);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_27(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.Ordinal);
    [MethodImpl(Common.Opt)]static bool Test_tst_27(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.Ordinal);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_28(string s) => MemoryExtensions.StartsWith(s, Common.Var(cns), StringComparison.OrdinalIgnoreCase);
    [MethodImpl(Common.Opt)]static bool Test_tst_28(string s) => MemoryExtensions.StartsWith(s, (cns), StringComparison.OrdinalIgnoreCase);

    [MethodImpl(Common.NoOpt)]static bool Test_ref_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_29(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_30(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(1), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_31(string s) => MemoryExtensions.SequenceEqual<char>(s.AsSpan(0, 2), (cns));

    [MethodImpl(Common.NoOpt)]static bool Test_ref_32(string s) => MemoryExtensions.SequenceEqual<char>(s, Common.Var(cns));
    [MethodImpl(Common.Opt)]static bool Test_tst_32(string s) => MemoryExtensions.SequenceEqual<char>(s, (cns));

}

