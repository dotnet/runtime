// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Reuse compiler temporaries safely across mixed common/high-rank statements and live byrefs.
public sealed partial class HighRankArrayTempReuse
{
    private const MethodImplOptions Options = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;
    private volatile int _accumulator;
    private int _value, _observable, _bad32, _bad3;
    private string _trace = "";
    private int[,] _r2 = null!, _nested2 = null!;
    private int _i2_0;
    private int _i2_1;
    private int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] _r32 = null!, _nested32 = null!;
    private int _i32_0;
    private int _i32_1;
    private int _i32_2;
    private int _i32_3;
    private int _i32_4;
    private int _i32_5;
    private int _i32_6;
    private int _i32_7;
    private int _i32_8;
    private int _i32_9;
    private int _i32_10;
    private int _i32_11;
    private int _i32_12;
    private int _i32_13;
    private int _i32_14;
    private int _i32_15;
    private int _i32_16;
    private int _i32_17;
    private int _i32_18;
    private int _i32_19;
    private int _i32_20;
    private int _i32_21;
    private int _i32_22;
    private int _i32_23;
    private int _i32_24;
    private int _i32_25;
    private int _i32_26;
    private int _i32_27;
    private int _i32_28;
    private int _i32_29;
    private int _i32_30;
    private int _i32_31;
    private int[,,] _r3 = null!, _nested3 = null!;
    private int _i3_0;
    private int _i3_1;
    private int _i3_2;
    private int[,,,] _r4 = null!, _nested4 = null!;
    private int _i4_0;
    private int _i4_1;
    private int _i4_2;
    private int _i4_3;
    [MethodImpl(Options)]
    private int MixedGets()
    {
        _accumulator = 0;
        _accumulator = unchecked(_accumulator + _r2[_i2_0, _i2_1]);
        _accumulator = unchecked(_accumulator + _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31]);
        _accumulator = unchecked(_accumulator + _r3[_i3_0, _i3_1, _i3_2]);
        _accumulator = unchecked(_accumulator + _r4[_i4_0, _i4_1, _i4_2, _i4_3]);
        return _accumulator;
    }
    [MethodImpl(Options)]
    private int MixedSets()
    {
        _r2[_i2_0, _i2_1] = _value + 2;
        _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31] = _value + 32;
        _r3[_i3_0, _i3_1, _i3_2] = _value + 3;
        _r4[_i4_0, _i4_1, _i4_2, _i4_3] = _value + 4;
        return _value;
    }
    [MethodImpl(Options)]
    private int MixedAddresses()
    {
        ref int slot2 = ref _r2[_i2_0, _i2_1];
        slot2 = unchecked(slot2 + _value);
        ref int slot32 = ref _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31];
        slot32 = unchecked(slot32 + _value);
        ref int slot3 = ref _r3[_i3_0, _i3_1, _i3_2];
        slot3 = unchecked(slot3 + _value);
        ref int slot4 = ref _r4[_i4_0, _i4_1, _i4_2, _i4_3];
        slot4 = unchecked(slot4 + _value);
        return unchecked(slot2 + slot32 + slot3 + slot4);
    }
    [MethodImpl(Options)]
    private int MixedBranches(bool highFirst)
    {
        _accumulator = 0;
        if (highFirst)
        {
            _accumulator = unchecked(_accumulator + _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31]);
            _accumulator = unchecked(_accumulator + _r2[_i2_0, _i2_1]);
            _accumulator = unchecked(_accumulator + _r4[_i4_0, _i4_1, _i4_2, _i4_3]);
            _accumulator = unchecked(_accumulator + _r3[_i3_0, _i3_1, _i3_2]);
        }
        else
        {
            _accumulator = unchecked(_accumulator + _r2[_i2_0, _i2_1]);
            _accumulator = unchecked(_accumulator + _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31]);
            _accumulator = unchecked(_accumulator + _r3[_i3_0, _i3_1, _i3_2]);
            _accumulator = unchecked(_accumulator + _r4[_i4_0, _i4_1, _i4_2, _i4_3]);
        }
        _accumulator = unchecked(_accumulator + _r2[_i2_0, _i2_1]);
        return _accumulator;
    }
    [MethodImpl(Options)]
    private int MixedSiblings()
    {
        return unchecked(_r2[_i2_0, _i2_1] + _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31] + _r3[_i3_0, _i3_1, _i3_2] + _r4[_i4_0, _i4_1, _i4_2, _i4_3]);
    }
    [MethodImpl(Options)]
    private int MixedNested()
    {
        return _nested32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _nested3[_i3_0, _i3_1, _nested4[_i4_0, _i4_1, _i4_2, _nested2[_i2_0, _i2_1]]]];
    }
    [MethodImpl(Options)]
    private int CommonRefAcrossGC()
    {
        ref int live = ref _r2[_i2_0, _i2_1];
        int high = _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31];
        int common = _r3[_i3_0, _i3_1, _i3_2];
        _r4[_i4_0, _i4_1, _i4_2, _i4_3] = unchecked(high + common + _value);
        ForceGC();
        live = unchecked(live + high + common);
        return live;
    }
    [MethodImpl(Options)]
    private int HighRefAcrossGC()
    {
        ref int live = ref _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31];
        int common = _r2[_i2_0, _i2_1];
        int high = _r4[_i4_0, _i4_1, _i4_2, _i4_3];
        _r3[_i3_0, _i3_1, _i3_2] = unchecked(high + common + _value);
        ForceGC();
        live = unchecked(live + high + common);
        return live;
    }
    [MethodImpl(Options)]
    private int CommonRefAcrossHighFailure()
    {
        ref int live = ref _r2[_i2_0, _i2_1];
        int observable = _r3[_i3_0, _i3_1, _i3_2];
        try
        {
            _observable = _r4[_i4_0, _i4_1, _i4_2, _i4_3];
            _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _bad32] = _value;
            throw new Exception("Expected high-rank bounds failure.");
        }
        catch (IndexOutOfRangeException)
        {
            live = unchecked(live + observable + _observable);
            _trace += "C";
        }
        finally
        {
            _r3[_i3_0, _i3_1, _i3_2] = 902;
            _trace += "F";
        }
        return unchecked(live + observable);
    }
    [MethodImpl(Options)]
    private int HighRefAcrossCommonFailure()
    {
        ref int live = ref _r32[_i32_0, _i32_1, _i32_2, _i32_3, _i32_4, _i32_5, _i32_6, _i32_7, _i32_8, _i32_9, _i32_10, _i32_11, _i32_12, _i32_13, _i32_14, _i32_15, _i32_16, _i32_17, _i32_18, _i32_19, _i32_20, _i32_21, _i32_22, _i32_23, _i32_24, _i32_25, _i32_26, _i32_27, _i32_28, _i32_29, _i32_30, _i32_31];
        int observable = _r4[_i4_0, _i4_1, _i4_2, _i4_3];
        try
        {
            _observable = _r2[_i2_0, _i2_1];
            _r3[_i3_0, _i3_1, _bad3] = _value;
            throw new Exception("Expected common-rank bounds failure.");
        }
        catch (IndexOutOfRangeException)
        {
            live = unchecked(live + observable + _observable);
            _trace += "C";
        }
        finally
        {
            _r4[_i4_0, _i4_1, _i4_2, _i4_3] = 902;
            _trace += "F";
        }
        return unchecked(live + observable);
    }
    private void ConfigureTyped()
    {
        _r2 = (int[,])_arrays[2]; _nested2 = (int[,])_nestedArrays[2];
        _i2_0 = _indices[2][0];
        _i2_1 = _indices[2][1];
        _r32 = (int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,])_arrays[32]; _nested32 = (int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,])_nestedArrays[32];
        _i32_0 = _indices[32][0];
        _i32_1 = _indices[32][1];
        _i32_2 = _indices[32][2];
        _i32_3 = _indices[32][3];
        _i32_4 = _indices[32][4];
        _i32_5 = _indices[32][5];
        _i32_6 = _indices[32][6];
        _i32_7 = _indices[32][7];
        _i32_8 = _indices[32][8];
        _i32_9 = _indices[32][9];
        _i32_10 = _indices[32][10];
        _i32_11 = _indices[32][11];
        _i32_12 = _indices[32][12];
        _i32_13 = _indices[32][13];
        _i32_14 = _indices[32][14];
        _i32_15 = _indices[32][15];
        _i32_16 = _indices[32][16];
        _i32_17 = _indices[32][17];
        _i32_18 = _indices[32][18];
        _i32_19 = _indices[32][19];
        _i32_20 = _indices[32][20];
        _i32_21 = _indices[32][21];
        _i32_22 = _indices[32][22];
        _i32_23 = _indices[32][23];
        _i32_24 = _indices[32][24];
        _i32_25 = _indices[32][25];
        _i32_26 = _indices[32][26];
        _i32_27 = _indices[32][27];
        _i32_28 = _indices[32][28];
        _i32_29 = _indices[32][29];
        _i32_30 = _indices[32][30];
        _i32_31 = _indices[32][31];
        _r3 = (int[,,])_arrays[3]; _nested3 = (int[,,])_nestedArrays[3];
        _i3_0 = _indices[3][0];
        _i3_1 = _indices[3][1];
        _i3_2 = _indices[3][2];
        _r4 = (int[,,,])_arrays[4]; _nested4 = (int[,,,])_nestedArrays[4];
        _i4_0 = _indices[4][0];
        _i4_1 = _indices[4][1];
        _i4_2 = _indices[4][2];
        _i4_3 = _indices[4][3];
    }
}

public sealed partial class HighRankArrayTempReuse
{
    private static readonly int[] Ranks = [2, 32, 3, 4];
    private readonly Dictionary<int, Array> _arrays = new(), _expected = new(), _nestedArrays = new();
    private readonly Dictionary<int, int[]> _indices = new();
    private void Configure(bool nonzero)
    {
        foreach (int rank in Ranks)
        {
            (Array a, int[] indices) = Create(rank, nonzero);
            _arrays[rank] = a; _expected[rank] = (Array)a.Clone(); _indices[rank] = indices;
            _nestedArrays[rank] = (Array)a.Clone();
        }
        int destination = _indices[2][^1];
        foreach (int rank in new[] { 2, 4, 3, 32 })
        {
            int[] i = (int[])_indices[rank].Clone(); i[^1] = destination;
            int nextRank = rank == 2 ? 4 : rank == 4 ? 3 : rank == 3 ? 32 : 0;
            int value = nextRank == 0 ? 777 : _arrays[nextRank].GetLowerBound(nextRank - 1) + 4;
            _nestedArrays[rank].SetValue(value, i); destination = value;
        }
        ConfigureTyped(); _value = 37; _trace = ""; _observable = 0;
        _bad32 = _arrays[32].GetUpperBound(31) + 1; _bad3 = _arrays[3].GetUpperBound(2) + 1;
    }
    private static (Array, int[]) Create(int rank, bool nonzero)
    {
        int[] lengths = Enumerable.Repeat(1, rank).ToArray(); lengths[0] = 3; lengths[rank / 2] = 3; lengths[^1] = 8;
        int[] lower = Enumerable.Range(0, rank).Select(d => nonzero ? d - rank / 2 : 0).ToArray();
        Array a = Array.CreateInstance(typeof(int), lengths, lower); int[] i = (int[])lower.Clone();
        for (int offset = 0; offset < a.Length; offset++) { a.SetValue(97 * rank + 17 * offset + 7, i); Next(a, i); }
        i = (int[])lower.Clone(); i[0]++; if (rank / 2 < rank - 1) i[rank / 2]++; i[^1]++;
        return (a, i);
    }
    private int Original(int rank) => (int)_expected[rank].GetValue(_indices[rank])!;
    private void StoreExpected(int rank, int value) => _expected[rank].SetValue(value, _indices[rank]);
    private void VerifyAll()
    {
        foreach (bool nonzero in new[] { false, true })
        {
            if (nonzero && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                continue;
            }
            Configure(nonzero); Equal(Ranks.Sum(Original), MixedGets(), "mixed Gets"); CheckArrays();
            Configure(nonzero); Equal(37, MixedSets(), "mixed Sets return");
            foreach (int rank in Ranks) StoreExpected(rank, 37 + rank); CheckArrays();
            Configure(nonzero); int addressSum = Ranks.Sum(rank => Original(rank) + 37);
            Equal(addressSum, MixedAddresses(), "mixed Address return");
            foreach (int rank in Ranks) StoreExpected(rank, Original(rank) + 37); CheckArrays();
            foreach (bool branch in new[] { false, true })
            {
                Configure(nonzero); Equal(Ranks.Sum(Original) + Original(2), MixedBranches(branch), $"mixed branch {branch}"); CheckArrays();
            }
            Configure(nonzero); Equal(Ranks.Sum(Original), MixedSiblings(), "mixed siblings"); CheckArrays();
            Configure(nonzero); Equal(777, MixedNested(), "mixed nested"); CheckArrays();
            Configure(nonzero); int commonAfterGc = Original(2) + Original(32) + Original(3);
            int highStore = Original(32) + Original(3) + 37;
            Equal(commonAfterGc, CommonRefAcrossGC(), "common ref across GC");
            StoreExpected(2, commonAfterGc); StoreExpected(4, highStore); CheckArrays();
            Configure(nonzero); int highAfterGc = Original(32) + Original(2) + Original(4);
            int commonStore = Original(2) + Original(4) + 37;
            Equal(highAfterGc, HighRefAcrossGC(), "high ref across GC");
            StoreExpected(32, highAfterGc); StoreExpected(3, commonStore); CheckArrays();
            Configure(nonzero); int commonAfterEh = Original(2) + Original(3) + Original(4);
            Equal(commonAfterEh + Original(3), CommonRefAcrossHighFailure(), "common ref across high EH");
            Equal("CF", _trace, "common ref EH trace"); StoreExpected(2, commonAfterEh); StoreExpected(3, 902); CheckArrays();
            Configure(nonzero); int highAfterEh = Original(32) + Original(4) + Original(2);
            Equal(highAfterEh + Original(4), HighRefAcrossCommonFailure(), "high ref across common EH");
            Equal("CF", _trace, "high ref EH trace"); StoreExpected(32, highAfterEh); StoreExpected(4, 902); CheckArrays();
        }
        Console.WriteLine("PASS: mixed rank2/32/3/4 statement sequences, both branch arms, siblings/nesting, common/high byrefs across GC and EH; zero/nonzero bounds and full reflection oracles.");
    }
    private void CheckArrays()
    {
        foreach (int rank in Ranks)
        {
            Array actual = _arrays[rank], expected = _expected[rank];
            int[] i = Enumerable.Range(0, rank).Select(actual.GetLowerBound).ToArray();
            for (int offset = 0; offset < actual.Length; offset++)
            {
                Equal((int)expected.GetValue(i)!, (int)actual.GetValue(i)!, $"rank {rank}, row-major offset {offset}"); Next(actual, i);
            }
        }
    }
    private static void Next(Array a, int[] i)
    {
        for (int d = i.Length - 1; d >= 0; d--)
        {
            if (++i[d] <= a.GetUpperBound(d)) return; i[d] = a.GetLowerBound(d);
        }
    }
    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{label}: expected {expected}, got {actual}.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ForceGC() => GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
}

public sealed partial class HighRankArrayTempReuse
{
    [Fact]
    public static void MixedRanks() => new HighRankArrayTempReuse().VerifyAll();
}
